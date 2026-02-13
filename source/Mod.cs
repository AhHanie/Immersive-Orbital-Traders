using HarmonyLib;
using UnityEngine;
using Verse;

namespace ImmersiveOrbitalTraders
{
    public sealed class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(Init, "ImmersiveOrbitalTraders.LoadingLabel", doAsynchronously: true, null);
        }

        public void Init()
        {
            GetSettings<ModSettings>();
            new Harmony("sk.iotframework").PatchAll();
        }

        public override string SettingsCategory()
        {
            return "ImmersiveOrbitalTraders.SettingsTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            ModSettingsWindow.Draw(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
