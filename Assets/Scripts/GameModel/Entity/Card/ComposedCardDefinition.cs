using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MortalGame.GameData;
using Sirenix.Serialization;

namespace MortalGame.GameModel
{
    /// <summary>本場動態卡片的獨立定義；對外只提供可獨立修改的深複本。</summary>
    public sealed class ComposedCardDefinition
    {
        [Serializable]
        private sealed class Content
        {
            public StandardCardData Data;
            public List<CardEffectOption> Options;
        }

        private readonly Content _content;
        public string TemplateCardDataId => _content.Data.ID;

        private ComposedCardDefinition(Content content)
        {
            _content = content;
        }

        public static ComposedCardDefinition Create(
            StandardCardData template, IEnumerable<CardEffectOption> selectedOptions)
        {
            var content = (Content)SerializationUtility.CreateCopy(new Content
            {
                Data = template,
                Options = selectedOptions.ToList()
            });
            foreach (var option in content.Options)
            {
                content.Data.Effects.AddRange(option.Effects);
                foreach (var pair in option.TriggeredEffects)
                {
                    var previous = content.Data.TriggeredEffects.TryGetValue(pair.Key, out var effects)
                        ? effects : Array.Empty<ConditionalCardEffect>();
                    content.Data.TriggeredEffects[pair.Key] = previous.Concat(pair.Value).ToArray();
                }
            }
            return new ComposedCardDefinition(content);
        }

        public ComposedCardSnapshot CreateSnapshot()
        {
            // 一起複製以保留描述來源與實際效果的物件關聯，不暴露私有配置。
            var copy = (Content)SerializationUtility.CreateCopy(_content);
            return new ComposedCardSnapshot(copy.Data, copy.Options.ToImmutableArray());
        }
    }

    public sealed record ComposedCardSnapshot(
        StandardCardData Data,
        ImmutableArray<CardEffectOption> SelectedOptions);
}
