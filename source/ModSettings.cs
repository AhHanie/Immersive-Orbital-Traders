using Verse;

namespace ImmersiveOrbitalTraders
{
    public class ModSettings : Verse.ModSettings
    {
        public static bool AllowTraderCaravans;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref AllowTraderCaravans, "allowTraderCaravans", false);
        }
    }
}
