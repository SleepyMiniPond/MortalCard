using System.Collections.Generic;
using System.Linq;
using MortalGame.GameModel;

namespace MortalGame.GameView
{
    public static class CardDescriptionText
    {
        public static string Format(CardInfo info, LocalizeLibrary library, Dictionary<string, string> templateValues = null)
        {
            var baseValues = templateValues ?? info.GetTemplateValues();
            var fragments = new List<string>();
            if (!info.DescriptionFragmentsOnly)
                fragments.Add(library.Get(LocalizeTitleInfoType.Card, info.CardDataID).Info.ReplaceTemplateKeys(baseValues));
            foreach (var fragment in info.DescriptionFragments)
            {
                var values = new Dictionary<string, string>(baseValues);
                foreach (var pair in fragment.Values) values[pair.Key] = pair.Value;
                if (fragment.Rules != null)
                    foreach (var pair in fragment.Rules)
                        values[pair.Key] = library.Get(LocalizeInfoType.UI, pair.Value).Info.ReplaceTemplateKeys(baseValues);
                fragments.Add(library.Get(LocalizeInfoType.UI, fragment.Key).Info.ReplaceTemplateKeys(values));
            }
            return string.Join("\n", fragments.Where(text => !string.IsNullOrWhiteSpace(text)));
        }
    }
}
