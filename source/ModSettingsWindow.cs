using UnityEngine;
using Verse;

namespace ImmersiveOrbitalTraders
{
    public static class ModSettingsWindow
    {
        public static void Draw(Rect parent)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(parent);

            listing.CheckboxLabeled("ImmersiveOrbitalTraders.Settings.AllowTraderCaravans.Label".Translate(), ref ModSettings.AllowTraderCaravans, "ImmersiveOrbitalTraders.Settings.AllowTraderCaravans.Tooltip".Translate());
            listing.End();
        }
    }
}
