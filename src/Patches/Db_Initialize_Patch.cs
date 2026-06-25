using System;
using Database;
using HarmonyLib;

namespace InterestPicker.Patches
{
    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    internal static class Db_Initialize_Patch
    {
        private static void Postfix()
        {
            try
            {
                InterestPickerMod.RegisterConfiguredSkillGroups();
            }
            catch (Exception ex)
            {
                InterestPickerMod.DisableForSession("Unexpected error while registering custom SkillGroups.", ex);
            }
        }
    }
}
