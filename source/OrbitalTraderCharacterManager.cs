using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace ImmersiveOrbitalTraders
{
    public static class OrbitalTraderCharacterManager
    {
        private static readonly Dictionary<TraderKindDef, List<OrbitalTraderCharacterDef>> DefsByTraderKind = new Dictionary<TraderKindDef, List<OrbitalTraderCharacterDef>>();
        private static readonly List<OrbitalTraderCharacterDef> GlobalDefs = new List<OrbitalTraderCharacterDef>();
        private static readonly Vector2 PortraitTextureSize = new Vector2(256f, 256f);

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
                if (characterDef.disabled)
                {
                    continue;
                }

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
        }

        public static OrbitalTraderCharacterDef GetRandomCharacter(TraderKindDef traderKind, Gender? preferredGender = null)
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

            if (preferredGender.HasValue)
            {
                List<OrbitalTraderCharacterDef> genderMatches = new List<OrbitalTraderCharacterDef>();
                for (int i = 0; i < candidates.Count; i++)
                {
                    OrbitalTraderCharacterDef candidate = candidates[i];
                    if (candidate != null && candidate.gender == preferredGender.Value)
                    {
                        genderMatches.Add(candidate);
                    }
                }

                if (genderMatches.Count > 0)
                {
                    return genderMatches.RandomElement();
                }
            }

            return candidates.RandomElement();
        }

        public static string ResolveLoreText(ITrader trader, OrbitalTraderCharacterDef characterDef, out string generatedName, string preferredName = null)
        {
            generatedName = string.IsNullOrWhiteSpace(preferredName)
                ? GenerateTraderName(trader, characterDef)
                : preferredName;

            string resolvedFromRulePack = ResolveFromRulePack(characterDef.loreRulePack, trader, characterDef, generatedName);
            if (!string.IsNullOrWhiteSpace(resolvedFromRulePack))
            {
                return resolvedFromRulePack;
            }

            if (!string.IsNullOrWhiteSpace(characterDef.loreText))
            {
                return characterDef.loreText;
            }

            return string.Empty;
        }

        public static bool TryGenerateFactionPawnPortrait(ITrader trader, out Texture portraitTexture)
        {
            return TryGenerateFactionPawnData(trader, out portraitTexture, out _, out _);
        }

        public static bool TryGenerateFactionPawnData(ITrader trader, out Texture portraitTexture, out string generatedName, out Gender generatedGender)
        {
            portraitTexture = null;
            generatedName = null;
            generatedGender = Gender.None;
            Faction faction = trader.Faction;
            PawnKindDef pawnKind = faction.RandomPawnKind();
            Pawn generatedPawn = null;
            Rand.PushState(trader?.RandomPriceFactorSeed ?? Rand.Int);

            try
            {
                generatedPawn = PawnGenerator.GeneratePawn(pawnKind, faction);
                if (generatedPawn == null)
                {
                    return false;
                }

                generatedName = generatedPawn.Name.ToStringFull;
                generatedGender = generatedPawn.gender;
                portraitTexture = GetPawnPortraitTexture(generatedPawn);

                return portraitTexture != null;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                return false;
            }
            finally
            {
                Rand.PopState();
                if (generatedPawn != null)
                {
                    try
                    {
                        generatedPawn.Destroy(DestroyMode.Vanish);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void EnsureCachesCurrent()
        {
            int defCountNow = DefDatabase<OrbitalTraderCharacterDef>.AllDefsListForReading.Count;
            if (!cachesBuilt || defCountNow != cachedDefCount)
            {
                RebuildCaches();
            }
        }

        private static string ResolveFromRulePack(RulePackDef rulePack, ITrader trader, OrbitalTraderCharacterDef characterDef, string generatedName)
        {
            if (rulePack == null)
            {
                return null;
            }

            GrammarRequest request = new GrammarRequest();
            request.Includes.Add(rulePack);
            request.Rules.Add(new Rule_String("name", generatedName));
            request.Rules.Add(new Rule_String("faction", ResolveFactionName(trader)));
            request.Rules.Add(new Rule_String("startYear", ResolveStartYear(trader, characterDef)));
            request.Constants["name"] = generatedName;
            request.Constants["faction"] = ResolveFactionName(trader);
            request.Constants["startYear"] = ResolveStartYear(trader, characterDef);

            try
            {
                return GrammarResolver.Resolve("root", request, null, forceLog: false, untranslatedRootKeyword: null, extraTags: null, outTags: null, capitalizeFirstSentence: true);
            }
            catch
            {
                return null;
            }
        }

        private static string GenerateTraderName(ITrader trader, OrbitalTraderCharacterDef characterDef)
        {
            int seed = GetLoreSeed(trader, characterDef);
            Rand.PushState(seed);

            try
            {
                NameTriple generatedTriple = PawnBioAndNameGenerator.GeneratePawnName_Shuffled(PawnNameCategory.HumanStandard, characterDef.gender, null, forceNoNick: false);
                string generatedName = generatedTriple.First + " " + generatedTriple.Last;
                if (string.IsNullOrWhiteSpace(generatedName))
                {
                    return "Unnamed Trader";
                }

                return generatedName;
            }
            catch
            {
                return "Unnamed Trader";
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static string ResolveFactionName(ITrader trader)
        {
            if (trader?.Faction != null && !string.IsNullOrWhiteSpace(trader.Faction.Name))
            {
                return trader.Faction.Name;
            }

            return "independent operators";
        }

        private static string ResolveStartYear(ITrader trader, OrbitalTraderCharacterDef characterDef)
        {
            int currentYear = GenDate.Year(Find.TickManager.TicksAbs, 0f);
            int seed = GetLoreSeed(trader, characterDef);
            int yearsAgo = Math.Abs(seed % 12) + 1;
            int startYear = currentYear - yearsAgo;
            return startYear.ToString();
        }

        private static int GetLoreSeed(ITrader trader, OrbitalTraderCharacterDef characterDef)
        {
            int seed = 23;
            if (trader != null)
            {
                seed = Gen.HashCombineInt(seed, trader.RandomPriceFactorSeed);
            }

            if (characterDef != null && !string.IsNullOrEmpty(characterDef.defName))
            {
                seed = Gen.HashCombineInt(seed, characterDef.defName.GetHashCode());
            }

            return seed;
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

        private static Texture GetPawnPortraitTexture(Pawn pawn)
        {
            try
            {
                return PortraitsCache.Get(
                    pawn,
                    PortraitTextureSize,
                    Rot4.South,
                    Vector3.zero,
                    1f,
                    supersample: true,
                    compensateForUIScale: true,
                    renderHeadgear: true,
                    renderClothes: true,
                    overrideApparelColors: null,
                    overrideHairColor: null,
                    stylingStation: false,
                    healthStateOverride: null);
            }
            catch
            {
                return null;
            }
        }
    }
}
