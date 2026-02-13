using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ImmersiveOrbitalTraders
{
    public static class OrbitalTraderCharacterManager
    {
        private static readonly Dictionary<TraderKindDef, List<OrbitalTraderCharacterDef>> DefsByTraderKind = new Dictionary<TraderKindDef, List<OrbitalTraderCharacterDef>>();
        private static readonly List<OrbitalTraderCharacterDef> GlobalDefs = new List<OrbitalTraderCharacterDef>();

        private static bool cachesBuilt;
        private static int cachedDefCount = -1;

        public static void RebuildCaches()
        {
            DefsByTraderKind.Clear();
            GlobalDefs.Clear();

            List<OrbitalTraderCharacterDef> allDefs = DefDatabase<OrbitalTraderCharacterDef>.AllDefsListForReading;
            cachedDefCount = allDefs.Count;

            for (int i = 0; i < allDefs.Count; i++)
            {
                OrbitalTraderCharacterDef characterDef = allDefs[i];

                List<TraderKindDef> allowedKinds = characterDef.allowedTraderKinds;
                if (allowedKinds == null || allowedKinds.Count == 0)
                {
                    GlobalDefs.Add(characterDef);
                    continue;
                }

                for (int k = 0; k < allowedKinds.Count; k++)
                {
                    TraderKindDef kindDef = allowedKinds[k];

                    if (!DefsByTraderKind.TryGetValue(kindDef, out List<OrbitalTraderCharacterDef> list))
                    {
                        list = new List<OrbitalTraderCharacterDef>();
                        DefsByTraderKind[kindDef] = list;
                    }

                    if (!list.Contains(characterDef))
                    {
                        list.Add(characterDef);
                    }
                }
            }

            cachesBuilt = true;
            Logger.Message("Character cache rebuilt. defs=" + cachedDefCount + ", globals=" + GlobalDefs.Count + ", mappedKinds=" + DefsByTraderKind.Count + ".");
        }

        public static OrbitalTraderCharacterDef GetRandomCharacter(TraderKindDef traderKind)
        {
            EnsureCachesCurrent();

            List<OrbitalTraderCharacterDef> candidates = new List<OrbitalTraderCharacterDef>(GlobalDefs.Count + 8);
            AddUnique(candidates, GlobalDefs);

            if (DefsByTraderKind.TryGetValue(traderKind, out List<OrbitalTraderCharacterDef> specific))
            {
                AddUnique(candidates, specific);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates.RandomElement();
        }

        private static void EnsureCachesCurrent()
        {
            int defCountNow = DefDatabase<OrbitalTraderCharacterDef>.AllDefsListForReading.Count;
            if (!cachesBuilt || defCountNow != cachedDefCount)
            {
                RebuildCaches();
            }
        }

        private static void WarnGraphicFailure(OrbitalTraderCharacterDef characterDef, string reason)
        {
            Log.Warning("[ImmersiveOrbitalTraders] Character def '" + characterDef.defName + "' cannot render portrait: " + reason);
        }

        private static void AddUnique(List<OrbitalTraderCharacterDef> target, List<OrbitalTraderCharacterDef> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                OrbitalTraderCharacterDef item = source[i];
                if (item != null && !target.Contains(item))
                {
                    target.Add(item);
                }
            }
        }
    }
}
