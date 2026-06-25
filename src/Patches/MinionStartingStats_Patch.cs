using System;
using HarmonyLib;

namespace InterestPicker.Patches
{
    [HarmonyPatch(typeof(MinionStartingStats), "GenerateAptitudes")]
    internal static class MinionStartingStats_Patch
    {
        private static bool Prefix(MinionStartingStats __instance, string guaranteedAptitudeID)
        {
            try
            {
                return InterestPickerMod.GenerateAptitudesPrefix(__instance, guaranteedAptitudeID);
            }
            catch (Exception ex)
            {
                InterestPickerMod.DisableForSession("Failed to apply custom aptitudes; falling back to vanilla behavior.", ex);
                return true;
            }
        }
    }
}
