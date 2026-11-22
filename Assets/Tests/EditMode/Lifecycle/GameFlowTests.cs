using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.Presenter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class GameFlowTests
    {
        private readonly List<UnityEngine.Object> _objects = new();
        private Context _context;

        [SetUp]
        public void SetUp()
        {
            var loaderObject = new GameObject(nameof(GameFlowTests));
            _objects.Add(loaderObject);
            var loader = loaderObject.AddComponent<ScriptableDataLoader>();
            var catalog = _Create<GameContentCatalog>();
            var players = _Create<AllPlayerScriptable>();
            var ally = _Create<AllyScriptable>();
            var enemy = _Create<EnemyScriptable>();
            var deck = _Create<DeckScriptable>();
            var card = _Create<StandardCardDataScriptable>();
            card.Data.ID = "flow-card";
            deck.Cards = new[] { card };
            ally.Ally = new AllyData
            {
                InitialDisposition = 3,
                PlayerData = new PlayerData
                {
                    NameKey = "flow-player", InitialHealth = 30, MaxHealth = 40,
                    MaxEnergy = 5, HandCardMaxCount = 7, Deck = deck
                }
            };
            players.AllyObject = ally;
            players.EnemyObjects = new[] { enemy };
            var excel = _Create<ExcelDatas>();
            excel.Disposition = new() { new DispositionExcelData { Id = "flow-disposition", Range = 10 } };
            excel.LocalizeCard = new();
            excel.LocalizeCardBuff = new();
            excel.LocalizeKeyWord = new();
            excel.LocalizePlayerBuff = new();
            excel.LocalizePlayer = new();
            excel.LocalizeUI = new();
            var serialized = new SerializedObject(loader);
            serialized.FindProperty("_gameContentCatalog").objectReferenceValue = catalog;
            serialized.FindProperty("_allPlayerScriptable").objectReferenceValue = players;
            serialized.FindProperty("_excelDatasScriptable").objectReferenceValue = excel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _context = new Context(loader);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects) UnityEngine.Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator RetryThenQuit_ReturnsToMenuWithoutAnotherBattle() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.Results.Enqueue(_Lose(LoseReactionType.Retry));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "battle", "menu" }));
            Assert.That(scenes.Operations, Is.EqualTo(new[]
            {
                "load-menu", "run-menu", "load-map", "run-map",
                "load-battle", "run-battle", "load-battle", "run-battle",
                "load-menu", "run-menu"
            }));
        });

        [UnityTest]
        public IEnumerator RetryThenWin_ReturnsToMapWithoutAnotherRetry() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.Results.Enqueue(_Lose(LoseReactionType.Retry));
            scenes.Results.Enqueue(new GameplayResultCommand(new GameplayWinResult(), Array.Empty<CardInstanceChangeSet>()));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "battle", "map", "battle", "menu" }));
            Assert.That(scenes.Attempts[2].Ally.Identity, Is.EqualTo(scenes.Attempts[0].Ally.Identity));
        });

        [UnityTest]
        public IEnumerator TwoWins_FinishesRouteAndReturnsToMenu() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.Results.Enqueue(new GameplayResultCommand(new GameplayWinResult(), Array.Empty<CardInstanceChangeSet>()));
            scenes.Results.Enqueue(new GameplayResultCommand(new GameplayWinResult(), Array.Empty<CardInstanceChangeSet>()));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "map", "battle", "menu" }));
            Assert.That(scenes.Attempts.Select(s => s.StageID), Is.EqualTo(new[] { "TemporaryStage1", "TemporaryStage2" }));
            Assert.That(scenes.Attempts[1].Enemy, Is.SameAs(scenes.Attempts[0].Enemy));
            Assert.That(scenes.Attempts[1].Ally.Identity, Is.EqualTo(scenes.Attempts[0].Ally.Identity));
        });

        [UnityTest]
        public IEnumerator SecondStageRetry_PreservesStageAndSeed() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.Results.Enqueue(new GameplayResultCommand(new GameplayWinResult(), Array.Empty<CardInstanceChangeSet>()));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Retry));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Attempts.Select(s => s.StageID),
                Is.EqualTo(new[] { "TemporaryStage1", "TemporaryStage2", "TemporaryStage2" }));
            Assert.That(scenes.Attempts[2].RandomSeed, Is.EqualTo(scenes.Attempts[1].RandomSeed));
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "map", "battle", "battle", "menu" }));
        });

        [UnityTest]
        public IEnumerator SecondStageRestart_ResetsRouteAndPlayer() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.Results.Enqueue(new GameplayResultCommand(new GameplayWinResult(), Array.Empty<CardInstanceChangeSet>()));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Restart));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Attempts.Select(s => s.StageID),
                Is.EqualTo(new[] { "TemporaryStage1", "TemporaryStage2", "TemporaryStage1" }));
            Assert.That(scenes.Attempts[2].Ally.Identity, Is.Not.EqualTo(scenes.Attempts[1].Ally.Identity));
        });

        [UnityTest]
        public IEnumerator Retry_PreservesSetupAndIsolatesAttemptDecks() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            Guid originalCard = default;
            scenes.OnBattle = setting =>
            {
                if (scenes.Attempts.Count == 1)
                {
                    originalCard = setting.Ally.Deck[0].InstanceGuid;
                    setting.Ally.Deck.Clear();
                }
            };
            scenes.Results.Enqueue(_Lose(LoseReactionType.Retry));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            var first = scenes.Attempts[0];
            var retry = scenes.Attempts[1];
            Assert.That(retry.RandomSeed, Is.EqualTo(first.RandomSeed));
            Assert.That(retry.StageID, Is.EqualTo(first.StageID));
            Assert.That(retry.Enemy, Is.SameAs(first.Enemy));
            Assert.That(retry.Ally.Identity, Is.EqualTo(first.Ally.Identity));
            Assert.That(retry.Ally.CurrentHealth, Is.EqualTo(30));
            Assert.That(retry.Ally.CurrentDisposition, Is.EqualTo(3));
            Assert.That(retry.Ally.CurrentEnergy, Is.EqualTo(5));
            Assert.That(retry.Ally.Deck.Single().InstanceGuid, Is.EqualTo(originalCard));
            Assert.That(_context.Ally.Deck.Single().InstanceGuid, Is.EqualTo(originalCard));
            var a = new BattleBuidler(_context).ConstructGameContextManager(first.RandomSeed);
            var b = new BattleBuidler(_context).ConstructGameContextManager(retry.RandomSeed);
            Assert.That(a.GameRandom, Is.Not.SameAs(b.GameRandom));
            Assert.That(Enumerable.Range(0, 12).Select(_ => a.GameRandom.Range(0, 100)).ToArray(),
                Is.EqualTo(Enumerable.Range(0, 12).Select(_ => b.GameRandom.Range(0, 100)).ToArray()));
        });

        [UnityTest]
        public IEnumerator RetryThenRestartThenQuit_ResetsRunAndReturnsToMenu() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.OnBattle = setting => _context.Ally.Deck.Clear();
            scenes.Results.Enqueue(_Lose(LoseReactionType.Retry));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Restart));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "battle", "map", "battle", "menu" }));
            Assert.That(scenes.Attempts[2].Ally.Identity, Is.Not.EqualTo(scenes.Attempts[0].Ally.Identity));
            Assert.That(scenes.Attempts[2].Ally.Deck.Single().InstanceGuid,
                Is.Not.EqualTo(scenes.Attempts[1].Ally.Deck.Single().InstanceGuid));
            Assert.That(scenes.Attempts[2].Ally.CurrentHealth, Is.EqualTo(30));
            Assert.That(scenes.Attempts[2].Ally.CurrentEnergy, Is.EqualTo(5));
            Assert.That(scenes.Attempts[2].Ally.CurrentDisposition, Is.EqualTo(3));
        });

        [UnityTest]
        public IEnumerator QuitThenStart_CreatesNewRun() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context) { AllowedMenus = 2 };
            scenes.OnBattle = setting => _context.Ally.Deck.Clear();
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "menu", "map", "battle", "menu" }));
            Assert.That(scenes.Attempts[1].Ally.Identity, Is.Not.EqualTo(scenes.Attempts[0].Ally.Identity));
            Assert.That(scenes.Attempts[1].Ally.Deck.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator MapFinishAndFail_ReturnToMenu() => UniTask.ToCoroutine(async () =>
        {
            foreach (var reaction in new[] { LevelMapReactionType.Finish, LevelMapReactionType.Fail })
            {
                using var stop = new CancellationTokenSource();
                var scenes = new FakeScenes(stop, _context);
                scenes.MapResults.Enqueue(reaction);
                await _RunUntilCancelled(scenes, stop);
                Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "menu" }));
            }
        });

        [UnityTest]
        public IEnumerator MapRestart_ResetsRunBeforeNextMap() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.MapResults.Enqueue(LevelMapReactionType.StartGamePlay);
            scenes.MapResults.Enqueue(LevelMapReactionType.Restart);
            scenes.Results.Enqueue(new GameplayResultCommand(new GameplayWinResult(), Array.Empty<CardInstanceChangeSet>()));
            scenes.Results.Enqueue(_Lose(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.MapAllies[2].Identity, Is.Not.EqualTo(scenes.MapAllies[1].Identity));
            Assert.That(scenes.Attempts.Select(s => s.StageID), Is.EqualTo(new[] { "TemporaryStage1", "TemporaryStage1" }));
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "map", "map", "battle", "menu" }));
        });

        [UnityTest]
        public IEnumerator CancellationAtSceneBoundaries_DoesNotContinue() => UniTask.ToCoroutine(async () =>
        {
            foreach (var boundary in new[] { "menu", "map", "battle" })
            {
                using var stop = new CancellationTokenSource();
                var scenes = new FakeScenes(stop, _context) { CancelAt = boundary };
                scenes.Results.Enqueue(new GameplayResultCommand(new GameplayWinResult(), Array.Empty<CardInstanceChangeSet>()));
                await _RunUntilCancelled(scenes, stop);
                Assert.That(scenes.Trace.Last(), Is.EqualTo(boundary));
            }
        });

        [UnityTest]
        public IEnumerator CancellationDuringLoading_DoesNotRunLoadedScene() => UniTask.ToCoroutine(async () =>
        {
            foreach (var scene in new[] { "menu", "map", "battle" })
            {
                using var stop = new CancellationTokenSource();
                var scenes = new FakeScenes(stop, _context) { CancelAt = "load-" + scene };
                await _RunUntilCancelled(scenes, stop);
                Assert.That(scenes.Operations.Last(), Is.EqualTo("load-" + scene));
                Assert.That(scenes.Trace, Does.Not.Contain(scene));
            }
        });

        [UnityTest]
        public IEnumerator LoadingException_PropagatesWithoutRunningScene() => UniTask.ToCoroutine(async () =>
        {
            foreach (var scene in new[] { "menu", "map", "battle" })
            {
                using var stop = new CancellationTokenSource();
                var expected = new InvalidOperationException("測試載入失敗");
                var scenes = new FakeScenes(stop, _context)
                {
                    OnLoad = name => { if (name == scene) throw expected; }
                };
                try
                {
                    await new GameFlow(_context, scenes).Run(stop.Token);
                    Assert.Fail("應傳遞載入例外。");
                }
                catch (InvalidOperationException actual) { Assert.That(actual, Is.SameAs(expected)); }
                Assert.That(scenes.Operations.Last(), Is.EqualTo("load-" + scene));
                Assert.That(scenes.Trace, Does.Not.Contain(scene));
            }
        });

        [UnityTest]
        public IEnumerator AlreadyCancelled_DoesNotLoadOrResetRun() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            var original = _context.Ally;
            stop.Cancel();
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Trace, Is.Empty);
            Assert.That(scenes.Operations, Is.Empty);
            Assert.That(_context.Ally, Is.SameAs(original));
        });

        [UnityTest]
        public IEnumerator BattleException_PropagatesWithoutLoadingAnotherScene() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var expected = new InvalidOperationException("測試戰鬥失敗");
            var scenes = new FakeScenes(stop, _context) { OnBattle = _ => throw expected };
            try
            {
                await new GameFlow(_context, scenes).Run(stop.Token);
                Assert.Fail("應傳遞戰鬥例外。");
            }
            catch (InvalidOperationException actual) { Assert.That(actual, Is.SameAs(expected)); }
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle" }));
        });

        private async UniTask _RunUntilCancelled(FakeScenes scenes, CancellationTokenSource stop)
        {
            try
            {
                await new GameFlow(_context, scenes).Run(stop.Token);
                Assert.Fail("主流程應持續到取消。");
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }

        private T _Create<T>() where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            _objects.Add(obj);
            return obj;
        }

        private static GameplayResultCommand _Lose(LoseReactionType reaction) =>
            new(new GameplayLoseResult(reaction), Array.Empty<CardInstanceChangeSet>());

        private sealed class FakeScenes : ISceneLoader, IMenuScene, ILevelMapScene, IGameplayScene
        {
            private readonly CancellationTokenSource _stop;
            private readonly Context _context;
            private int _menus;
            public int AllowedMenus = 1;
            public string CancelAt;
            public Action<GameStageSetting> OnBattle;
            public Action<string> OnLoad;
            public readonly List<string> Trace = new();
            public readonly List<string> Operations = new();
            public readonly List<GameStageSetting> Attempts = new();
            public readonly List<AllyInstance> MapAllies = new();
            public readonly Queue<GameplayResultCommand> Results = new();
            public readonly Queue<LevelMapReactionType> MapResults = new();

            public FakeScenes(CancellationTokenSource stop, Context context)
            {
                _stop = stop;
                _context = context;
            }

            public UniTask<IMenuScene> LoadMenuScene(CancellationToken cancellationToken)
            {
                _Load("menu");
                return UniTask.FromResult<IMenuScene>(this);
            }

            public UniTask<ILevelMapScene> LoadLevelMapScene(CancellationToken cancellationToken)
            {
                _Load("map");
                return UniTask.FromResult<ILevelMapScene>(this);
            }

            public UniTask<IGameplayScene> LoadGameplayScene(CancellationToken cancellationToken)
            {
                _Load("battle");
                return UniTask.FromResult<IGameplayScene>(this);
            }

            UniTask IMenuScene.Run(CancellationToken cancellationToken)
            {
                _Visit("menu");
                if (++_menus > AllowedMenus) _stop.Cancel();
                return UniTask.CompletedTask;
            }

            UniTask<LevelMapCommand> ILevelMapScene.Run(CancellationToken cancellationToken)
            {
                _Visit("map");
                MapAllies.Add(_context.Ally);
                return UniTask.FromResult(new LevelMapCommand(MapResults.Count > 0
                    ? MapResults.Dequeue() : LevelMapReactionType.StartGamePlay));
            }

            public UniTask<GameplayResultCommand> Run(
                Context context, GameStageSetting setting, CancellationToken cancellationToken)
            {
                _Visit("battle");
                Attempts.Add(setting);
                OnBattle?.Invoke(setting);
                Assert.That(Results.Count, Is.GreaterThan(0), "不得多執行一次戰鬥。");
                return UniTask.FromResult(Results.Dequeue());
            }

            private void _Visit(string scene)
            {
                Operations.Add("run-" + scene);
                Trace.Add(scene);
                Assert.That(Trace.Count, Is.LessThan(20), "流程不得無限重複。");
                if (CancelAt == scene) _stop.Cancel();
            }

            private void _Load(string scene)
            {
                Operations.Add("load-" + scene);
                if (CancelAt == "load-" + scene) _stop.Cancel();
                OnLoad?.Invoke(scene);
            }
        }
    }
}
