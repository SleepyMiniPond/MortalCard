using System;
using MortalGame.Exceptions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presenter;
using NUnit.Framework;
using Optional;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;

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

        [Test]
        public void CommitBattleCompletion_ReplacesAllyAfterCompleteApply()
        {
            var source = _context.Ally;
            var originalDeck = source.Deck;
            var form = new PersistentCardFormState("commit-form", "commit-target");
            var completion = new AllyBattleCompletion(source.Identity, 17, 6, new[]
            {
                new CardInstanceChangeSet(source.Deck[0].InstanceGuid, form.Some())
            });

            _context.CommitBattleCompletion(completion);

            Assert.That(_context.Ally, Is.Not.SameAs(source));
            Assert.That(_context.Ally.CurrentHealth, Is.EqualTo(17));
            Assert.That(_context.Ally.CurrentDisposition, Is.EqualTo(6));
            Assert.That(_context.Ally.CurrentEnergy, Is.EqualTo(5));
            Assert.That(_context.Ally.Deck[0].PersistentFormState, Is.EqualTo(form.Some()));
            Assert.That(source.Deck, Is.SameAs(originalDeck));
            Assert.That(source.Deck[0].PersistentFormState.HasValue, Is.False);
            Assert.That(source.CurrentHealth, Is.EqualTo(30));
            Assert.That(source.CurrentDisposition, Is.EqualTo(3));
        }

        [Test]
        public void CommitBattleCompletion_InvalidLastCardKeepsEntireOriginalState()
        {
            var source = _context.Ally;
            source.Deck.Add(source.Deck[0] with { InstanceGuid = Guid.NewGuid() });
            var originalDeck = source.Deck;
            var originalCards = source.Deck.ToArray();
            var completion = new AllyBattleCompletion(source.Identity, 17, 6, new[]
            {
                new CardInstanceChangeSet(source.Deck[0].InstanceGuid,
                    new PersistentCardFormState("valid-first", "target").Some()),
                new CardInstanceChangeSet(Guid.NewGuid(), default)
            });

            var error = Assert.Throws<MissingBattleCompletionCardException>(() => _context.CommitBattleCompletion(completion));
            Assert.That(error.InstanceGuid, Is.EqualTo(source.Deck[1].InstanceGuid));

            Assert.That(_context.Ally, Is.SameAs(source));
            Assert.That(_context.Ally.Deck, Is.SameAs(originalDeck));
            Assert.That(_context.Ally.Deck, Is.EqualTo(originalCards));
            Assert.That(_context.Ally.CurrentHealth, Is.EqualTo(30));
            Assert.That(_context.Ally.CurrentDisposition, Is.EqualTo(3));
            Assert.That(_context.Ally.CurrentEnergy, Is.EqualTo(5));
            Assert.That(_context.Ally.Deck[0].PersistentFormState.HasValue, Is.False);
        }

        [Test]
        public void CommitBattleCompletion_AfterResetRejectsPreviousRun()
        {
            var oldAlly = _context.Ally;
            var completion = new AllyBattleCompletion(oldAlly.Identity, 17, 6, oldAlly.Deck.Select(card =>
                new CardInstanceChangeSet(card.InstanceGuid, card.PersistentFormState)).ToArray());
            _context.ResetRun();
            var newAlly = _context.Ally;

            var error = Assert.Throws<BattleCompletionSourceMismatchException>(() => _context.CommitBattleCompletion(completion));
            Assert.That(error.ExpectedIdentity, Is.EqualTo(newAlly.Identity));
            Assert.That(error.ActualIdentity, Is.EqualTo(oldAlly.Identity));

            Assert.That(_context.Ally, Is.SameAs(newAlly));
            Assert.That(newAlly.Identity, Is.Not.EqualTo(oldAlly.Identity));
            Assert.That(newAlly.CurrentHealth, Is.EqualTo(30));
        }

        [UnityTest]
        public IEnumerator RetryThenQuit_ReturnsToMenuWithoutAnotherBattle() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Retry));
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
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
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Retry));
            scenes.Results.Enqueue(new GameplayWinResult());
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "battle", "map", "battle", "menu" }));
            Assert.That(scenes.Attempts[2].Ally.Identity, Is.EqualTo(scenes.Attempts[0].Ally.Identity));
        });

        [UnityTest]
        public IEnumerator TwoWins_FinishesRouteAndReturnsToMenu() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.BattleRun = (setting, token) => UniTask.FromResult(
                _Victory(setting, scenes.Attempts.Count == 1 ? 17 : 9, 6));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle", "map", "battle", "menu" }));
            Assert.That(scenes.Attempts.Select(s => s.StageID), Is.EqualTo(new[] { "TemporaryStage1", "TemporaryStage2" }));
            Assert.That(scenes.Attempts[1].Enemy, Is.SameAs(scenes.Attempts[0].Enemy));
            Assert.That(scenes.Attempts[1].Ally.Identity, Is.EqualTo(scenes.Attempts[0].Ally.Identity));
            Assert.That(scenes.Attempts[1].Ally.CurrentHealth, Is.EqualTo(17));
            Assert.That(_context.Ally.CurrentHealth, Is.EqualTo(9), "第二勝仍須先提交再回選單");
        });

        [UnityTest]
        public IEnumerator SecondStageRetry_PreservesStageAndSeed() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.Results.Enqueue(new GameplayWinResult());
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Retry));
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
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
            scenes.Results.Enqueue(new GameplayWinResult());
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Restart));
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
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
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Retry));
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
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
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Retry));
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Restart));
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
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
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
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
            scenes.Results.Enqueue(new GameplayWinResult());
            scenes.Results.Enqueue(new GameplayLoseResult(LoseReactionType.Quit));
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
                scenes.Results.Enqueue(new GameplayWinResult());
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

        [UnityTest]
        public IEnumerator Victory_WaitsForContinueAndCleanupBeforeCommit() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var continued = new UniTaskCompletionSource();
            var cleaned = new UniTaskCompletionSource();
            var scenes = new FakeScenes(stop, _context);
            var cleanupStarted = false;
            scenes.BattleRun = async (setting, token) =>
            {
                if (scenes.Attempts.Count > 1)
                    return new GameplayResultCommand(new GameplayLoseResult(LoseReactionType.Quit), default);
                await continued.Task.AttachExternalCancellation(token);
                cleanupStarted = true;
                await cleaned.Task.AttachExternalCancellation(token);
                return _Victory(setting, 17, 6);
            };
            var run = new GameFlow(_context, scenes).Run(stop.Token).Preserve();
            var original = _context.Ally;
            try
            {
                Assert.That(scenes.Attempts, Has.Count.EqualTo(1));
                Assert.That(_context.Ally.CurrentHealth, Is.EqualTo(30));
                continued.TrySetResult();
                await UniTask.WaitUntil(() => cleanupStarted, cancellationToken: stop.Token);
                Assert.That(_context.Ally, Is.SameAs(original), "按繼續後仍須等待場景清理");
                cleaned.TrySetResult();
                await run.SuppressCancellationThrow();
                Assert.That(_context.Ally, Is.Not.SameAs(original));
                Assert.That(_context.Ally.CurrentHealth, Is.EqualTo(17));
                Assert.That(_context.Ally.CurrentDisposition, Is.EqualTo(6));
                Assert.That(_context.Ally.CurrentEnergy, Is.EqualTo(5));
                Assert.That(scenes.MapAllies[1], Is.SameAs(_context.Ally));
                Assert.That(scenes.Attempts[1].Ally.CurrentHealth, Is.EqualTo(17));
                Assert.That(scenes.Attempts[1].Ally.Deck[0].PersistentFormState,
                    Is.EqualTo(new PersistentCardFormState("victory-form", "flow-form").Some()));
            }
            finally { stop.Cancel(); await run.SuppressCancellationThrow(); }
        });

        [UnityTest]
        public IEnumerator VictoryThenSecondStageRetry_UsesCommittedOpeningState() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.BattleRun = (setting, token) =>
            {
                if (scenes.Attempts.Count == 1) return UniTask.FromResult(_Victory(setting, 17, 6));
                if (scenes.Attempts.Count == 2) setting.Ally.Deck.Clear();
                return UniTask.FromResult(new GameplayResultCommand(new GameplayLoseResult(
                    scenes.Attempts.Count == 2 ? LoseReactionType.Retry : LoseReactionType.Quit), default));
            };
            await _RunUntilCancelled(scenes, stop);
            var opening = scenes.Attempts[1];
            var retry = scenes.Attempts[2];
            Assert.That(retry.StageID, Is.EqualTo("TemporaryStage2"));
            Assert.That(retry.RandomSeed, Is.EqualTo(opening.RandomSeed));
            Assert.That(retry.Enemy, Is.SameAs(opening.Enemy));
            Assert.That(retry.Ally.CurrentHealth, Is.EqualTo(17));
            Assert.That(retry.Ally.CurrentDisposition, Is.EqualTo(6));
            Assert.That(retry.Ally.CurrentEnergy, Is.EqualTo(5));
            Assert.That(retry.Ally.Deck.Single().InstanceGuid, Is.EqualTo(_context.Ally.Deck.Single().InstanceGuid));
            Assert.That(retry.Ally.Deck[0].PersistentFormState,
                Is.EqualTo(new PersistentCardFormState("victory-form", "flow-form").Some()));
            Assert.That(_context.Ally.CurrentHealth, Is.EqualTo(17), "失敗與 Quit 不覆寫上一場已提交狀態");
        });

        [UnityTest]
        public IEnumerator VictoryThenRestart_RestoresInitialRunInsteadOfCommittedState() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            scenes.BattleRun = (setting, token) => UniTask.FromResult(scenes.Attempts.Count == 1
                ? _Victory(setting, 17, 6)
                : new GameplayResultCommand(new GameplayLoseResult(scenes.Attempts.Count == 2
                    ? LoseReactionType.Restart : LoseReactionType.Quit), default));
            await _RunUntilCancelled(scenes, stop);
            Assert.That(scenes.Attempts[2].StageID, Is.EqualTo("TemporaryStage1"));
            Assert.That(scenes.Attempts[2].Ally.Identity, Is.Not.EqualTo(scenes.Attempts[0].Ally.Identity));
            Assert.That(scenes.Attempts[2].Ally.CurrentHealth, Is.EqualTo(30));
            Assert.That(scenes.Attempts[2].Ally.CurrentDisposition, Is.EqualTo(3));
            Assert.That(scenes.Attempts[2].Ally.CurrentEnergy, Is.EqualTo(5));
            Assert.That(scenes.Attempts[2].Ally.Deck[0].PersistentFormState.HasValue, Is.False);
        });

        [UnityTest]
        public IEnumerator VictoryCanceledAtReturn_DoesNotCommit() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            AllyInstance original = null;
            scenes.BattleRun = (setting, token) =>
            {
                original = _context.Ally;
                var victory = _Victory(setting, 17, 6);
                stop.Cancel();
                return UniTask.FromResult(victory);
            };
            await _RunUntilCancelled(scenes, stop);
            Assert.That(_context.Ally, Is.SameAs(original));
            Assert.That(_context.Ally.CurrentHealth, Is.EqualTo(30));
            Assert.That(_context.Ally.Deck[0].PersistentFormState.HasValue, Is.False);
            Assert.That(scenes.Trace.Last(), Is.EqualTo("battle"));
        });

        [UnityTest]
        public IEnumerator VictoryCleanupException_DoesNotCommitOrAdvance() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            var expected = new InvalidOperationException("測試場景結果清理失敗");
            AllyInstance original = null;
            scenes.BattleRun = async (setting, token) =>
            {
                original = _context.Ally;
                try { await UniTask.Yield(); return _Victory(setting, 17, 6); }
                finally { throw expected; }
            };
            Exception actual = null;
            try { await new GameFlow(_context, scenes).Run(stop.Token); }
            catch (InvalidOperationException error) { actual = error; }
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(_context.Ally, Is.SameAs(original));
            Assert.That(scenes.Trace, Is.EqualTo(new[] { "menu", "map", "battle" }));
        });

        [UnityTest]
        public IEnumerator NextSceneException_KeepsAlreadyCommittedVictory() => UniTask.ToCoroutine(async () =>
        {
            using var stop = new CancellationTokenSource();
            var scenes = new FakeScenes(stop, _context);
            var expected = new InvalidOperationException("測試下一張地圖載入失敗");
            scenes.BattleRun = (setting, token) => UniTask.FromResult(_Victory(setting, 17, 6));
            scenes.OnLoad = name =>
            {
                if (name == "map" && scenes.Attempts.Count == 1) throw expected;
            };
            Exception actual = null;
            try { await new GameFlow(_context, scenes).Run(stop.Token); }
            catch (InvalidOperationException error) { actual = error; }
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(_context.Ally.CurrentHealth, Is.EqualTo(17));
            Assert.That(_context.Ally.CurrentDisposition, Is.EqualTo(6));
        });

        [UnityTest]
        public IEnumerator InvalidVictoryOutput_DoesNotCommitOrAdvance() => UniTask.ToCoroutine(async () =>
        {
            foreach (var missing in new[] { true, false })
            {
                using var stop = new CancellationTokenSource();
                var scenes = new FakeScenes(stop, _context);
                AllyInstance original = null;
                scenes.BattleRun = (setting, token) =>
                {
                    original = _context.Ally;
                    return UniTask.FromResult(new GameplayResultCommand(new GameplayWinResult(), missing
                        ? Option.None<AllyBattleCompletion>()
                        : new AllyBattleCompletion(setting.Ally.Identity, 17, 6, Array.Empty<CardInstanceChangeSet>()).Some()));
                };
                Exception failure = null;
                try { await new GameFlow(_context, scenes).Run(stop.Token); }
                catch (InvalidOperationException error) { failure = error; }
                if (missing)
                    Assert.That(failure, Is.TypeOf<MissingBattleCompletionException>());
                else
                {
                    Assert.That(failure, Is.TypeOf<BattleCompletionCardCountMismatchException>());
                    var countError = (BattleCompletionCardCountMismatchException)failure;
                    Assert.That(countError.ExpectedCount, Is.EqualTo(original.Deck.Count));
                    Assert.That(countError.ActualCount, Is.Zero);
                }
                Assert.That(_context.Ally, Is.SameAs(original));
                Assert.That(scenes.Trace.Last(), Is.EqualTo("battle"));
            }
        });

        [UnityTest]
        public IEnumerator NextBattle_CommittedStateReachesModelAndSceneViews() => UniTask.ToCoroutine(async () =>
        {
            _context.CommitBattleCompletion(new AllyBattleCompletion(_context.Ally.Identity, 17, 6,
                _context.Ally.Deck.Select(card => new CardInstanceChangeSet(card.InstanceGuid,
                    new PersistentCardFormState("victory-form", "flow-form").Some())).ToArray()));
            var localize = new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>
                {
                    [LocalizeTitleInfoType.Player] = new Dictionary<string, LocalizeTitleInfoData>
                    {
                        ["flow-player"] = new("玩家", ""), ["next-enemy"] = new("敵人", "")
                    },
                    [LocalizeTitleInfoType.KeyWord] = new Dictionary<string, LocalizeTitleInfoData>
                    {
                        ["flow-disposition"] = new("好感度階段", "")
                    },
                    [LocalizeTitleInfoType.Card] = new Dictionary<string, LocalizeTitleInfoData>
                    {
                        ["flow-card"] = new("原卡", ""), ["flow-form"] = new("延續形態", "")
                    }
                }, new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>());
            var gameContext = GameContextTestBuilder.CreateContextManager(
                cardLibrary: new CardLibrary(new Dictionary<string, CardData>
                {
                    ["flow-card"] = new StandardCardData { ID = "flow-card", Themes = Array.Empty<CardTheme>() },
                    ["flow-form"] = new StandardCardData { ID = "flow-form", Cost = 2, Power = 9, Themes = Array.Empty<CardTheme>() }
                }),
                dispositionLibrary: new DispositionLibrary(new[] { new DispositionData("flow-disposition", 10, 0, 0) }),
                localizeLibrary: localize);
            var setting = new GameStageSetting("next-battle", 1, _context.Ally,
                new EnemyData
                {
                    EnemyID = "next-enemy", SelectedCardMaxCount = 0,
                    PlayerData = new PlayerData
                    {
                        NameKey = "next-enemy", InitialHealth = 20, MaxHealth = 20, MaxEnergy = 3,
                        HandCardMaxCount = 5, Deck = _Create<DeckScriptable>()
                    }
                });
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Additive);
            using var stop = new CancellationTokenSource();
            UniTask<Option<BattleResult>> run = default;
            var started = false;
            try
            {
                var objects = scene.GetRootGameObjects();
                var view = objects.SelectMany(go => go.GetComponentsInChildren<GameplayView>(true)).Single();
                var win = objects.SelectMany(go => go.GetComponentsInChildren<GameResultWinPanel>(true)).Single();
                var lose = objects.SelectMany(go => go.GetComponentsInChildren<GameResultLosePanel>(true)).Single();
                var presenter = new GameplayPresenter(view, win, lose, setting, gameContext);
                var manager = (GameplayManager)typeof(GameplayPresenter).GetField("_gameplayManager",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(presenter);
                var viewModel = (GameViewModel)typeof(GameplayPresenter).GetField("_gameInfoModel",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(presenter);
                run = manager.StartBattle(stop.Token).Preserve();
                started = true;
                var events = manager.PopAllEvents();
                var round = events.OfType<RoundStartEvent>().Single();
                Assert.That(round.Player.MainCharacter.CurrentHealth, Is.EqualTo(17));
                Assert.That(round.Player.CurrentEnergy, Is.EqualTo(5));
                Assert.That(round.Player.DispositionManager.CurrentDisposition, Is.EqualTo(6));
                var card = events.OfType<AddCardEvent>().Single(evt => evt.Faction == Faction.Ally).CardInfo;
                view.Render(events.Where(evt => evt is RoundStartEvent || evt is AddCardEvent).ToArray(), presenter);
                Assert.That(card.CardDataID, Is.EqualTo("flow-form"));
                Assert.That(viewModel.GetCardInfoOrNone(card.Identity).HasValue, Is.True);
                Assert.That(viewModel.ObservableDispositionInfo.Value.CurrentDisposition, Is.EqualTo(6));

                var info = _Reference<AllyInfoView>(view, "_allyInfoView");
                Assert.That(_Reference<TextMeshProUGUI>(_Reference<HealthBarView>(info, "_healthBarView"), "_hpText").text,
                    Is.EqualTo("17"));
                Assert.That(_Reference<TextMeshProUGUI>(_Reference<EnergyBarView>(info, "_energyBarView"), "_energyText").text,
                    Is.EqualTo("5"));
                var disposition = _Reference<DispositionView>(info, "_dispositionView");
                Assert.That(_Reference<UnityEngine.UI.Image>(disposition, "_image").fillAmount,
                    Is.EqualTo(6f / 9f).Within(0.0001f));
                var cardView = _Reference<CardView>(((IGameplayView)view).FocusCardDetailView, "_cardView");
                cardView.RenderSnapshot(card);
                Assert.That(_Reference<TextMeshProUGUI>(cardView, "_title").text, Is.EqualTo("延續形態"));
                Assert.That(_Reference<TextMeshProUGUI>(cardView, "_cost").text, Is.EqualTo("2"));
            }
            finally
            {
                stop.Cancel();
                if (started)
                {
                    for (var i = 0; i < 100 && run.Status == UniTaskStatus.Pending; i++)
                    {
                        EditorApplication.QueuePlayerLoopUpdate();
                        await UniTask.Yield();
                    }
                    await run.SuppressCancellationThrow();
                }
                EditorSceneManager.CloseScene(scene, true);
            }
        });

        private static T _Reference<T>(UnityEngine.Object owner, string name) where T : UnityEngine.Object
            => (T)new SerializedObject(owner).FindProperty(name).objectReferenceValue;

        private static GameplayResultCommand _Victory(GameStageSetting setting, int health, int disposition)
            => new(new GameplayWinResult(), new AllyBattleCompletion(setting.Ally.Identity, health, disposition,
                setting.Ally.Deck.Select(card => new CardInstanceChangeSet(card.InstanceGuid,
                    new PersistentCardFormState("victory-form", "flow-form").Some())).ToArray()).Some());

        private async UniTask _RunUntilCancelled(FakeScenes scenes, CancellationTokenSource stop)
        {
            try
            {
                await new GameFlow(_context, scenes).Run(stop.Token);
                Assert.Fail("主流程應持續到取消。");
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }

        [UnityTest]
        public IEnumerator UnsupportedSceneOutput_DoesNotAdvance() => UniTask.ToCoroutine(async () =>
        {
            foreach (var invalidMap in new[] { true, false })
            {
                using var stop = new CancellationTokenSource();
                var scenes = new FakeScenes(stop, _context);
                if (invalidMap) scenes.MapResults.Enqueue((LevelMapReactionType)int.MaxValue);
                else scenes.Results.Enqueue(new GameplayLoseResult((LoseReactionType)int.MaxValue));
                Exception failure = null;
                try { await new GameFlow(_context, scenes).Run(stop.Token); }
                catch (InvalidOperationException error) { failure = error; }
                if (invalidMap)
                {
                    Assert.That(failure, Is.TypeOf<UnsupportedLevelMapReactionException>());
                    Assert.That(((UnsupportedLevelMapReactionException)failure).ReactionType,
                        Is.EqualTo((LevelMapReactionType)int.MaxValue));
                    Assert.That(scenes.Trace.Last(), Is.EqualTo("map"));
                    Assert.That(scenes.Attempts, Is.Empty);
                }
                else
                {
                    Assert.That(failure, Is.TypeOf<UnsupportedGameplayResultException>());
                    Assert.That(((UnsupportedGameplayResultException)failure).Result,
                        Is.EqualTo(new GameplayLoseResult((LoseReactionType)int.MaxValue)));
                    Assert.That(scenes.Trace.Last(), Is.EqualTo("battle"));
                    Assert.That(scenes.Attempts.Count, Is.EqualTo(1));
                }
            }
        });

        private T _Create<T>() where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            _objects.Add(obj);
            return obj;
        }

        private sealed class FakeScenes : ISceneLoader, IMenuScene, ILevelMapScene, IGameplayScene
        {
            private readonly CancellationTokenSource _stop;
            private readonly Context _context;
            private int _menus;
            public int AllowedMenus = 1;
            public string CancelAt;
            public Action<GameStageSetting> OnBattle;
            public Action<string> OnLoad;
            public Func<GameStageSetting, CancellationToken, UniTask<GameplayResultCommand>> BattleRun;
            public readonly List<string> Trace = new();
            public readonly List<string> Operations = new();
            public readonly List<GameStageSetting> Attempts = new();
            public readonly List<AllyInstance> MapAllies = new();
            public readonly Queue<GameplayResult> Results = new();
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
                if (BattleRun != null) return BattleRun(setting, cancellationToken);
                Assert.That(Results.Count, Is.GreaterThan(0), "不得多執行一次戰鬥。");
                var result = Results.Dequeue();
                var completion = result is GameplayWinResult
                    ? new AllyBattleCompletion(setting.Ally.Identity, setting.Ally.CurrentHealth,
                        setting.Ally.CurrentDisposition, setting.Ally.Deck.Select(card =>
                            new CardInstanceChangeSet(card.InstanceGuid, card.PersistentFormState)).ToArray()).Some()
                    : Option.None<AllyBattleCompletion>();
                return UniTask.FromResult(new GameplayResultCommand(result, completion));
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
