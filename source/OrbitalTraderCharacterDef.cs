using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ImmersiveOrbitalTraders
{
    public class OrbitalTraderCharacterDef : Def
    {
        public GraphicData portraitGraphicData;
        public List<TraderKindDef> allowedTraderKinds;
        public string loreText;

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

            if (string.IsNullOrWhiteSpace(loreText))
            {
                yield return "loreText is required.";
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
