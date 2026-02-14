using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ImmersiveOrbitalTraders
{
    public class OrbitalTraderCharacterDef : Def
    {
        public GraphicData portraitGraphicData;
        public List<TraderKindDef> allowedTraderKinds;
        public RulePackDef loreRulePack;
        public string loreText;
        public Gender gender = Gender.Male;
        public bool disabled = false;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (portraitGraphicData != null && portraitGraphicData.graphicClass == null)
            {
                portraitGraphicData.graphicClass = typeof(Graphic_Single);
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string item in base.ConfigErrors())
            {
                yield return item;
            }

            if (portraitGraphicData == null)
            {
                yield return "portraitGraphicData is required.";
                yield break;
            }

            if (string.IsNullOrWhiteSpace(portraitGraphicData.texPath))
            {
                yield return "portraitGraphicData.texPath is required.";
            }

            if (loreRulePack == null && string.IsNullOrWhiteSpace(loreText))
            {
                yield return "Either loreRulePack or loreText is required.";
            }

            if (allowedTraderKinds != null)
            {
                for (int i = 0; i < allowedTraderKinds.Count; i++)
                {
                    if (allowedTraderKinds[i] == null)
                    {
                        yield return "allowedTraderKinds contains a null entry.";
                    }
                }
            }
        }
    }
}
