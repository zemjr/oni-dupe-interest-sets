using System;
using HarmonyLib;
using KMod;
using PeterHan.PLib.UI;

namespace InterestPicker.Patches
{
    [HarmonyPatch(typeof(ModsScreen), "OnToggleClicked")]
    internal static class ModsScreen_Patch
    {
        private static void Postfix(Label mod)
        {
            try
            {
                ShowDisabledWarningIfNeeded();

                Mod target = Global.Instance.modManager.FindMod(mod);
                if (target == null || !target.IsEnabledForActiveDlc())
                    return;

                if (!string.Equals(target.staticID, InterestPickerMod.ModStaticId, StringComparison.Ordinal))
                    return;

                PUIElements.ShowConfirmDialog(
                    null,
                    "Dupe Interest Sets was enabled, but the duplicant dropdown is only rebuilt after restarting the game.",
                    App.instance.Restart,
                    null,
                    "Restart Now",
                    "Later");
            }
            catch (Exception ex)
            {
                InterestPickerMod.Error("Failed to show restart warning after enabling the mod.", ex);
            }
        }

        private static void ShowDisabledWarningIfNeeded()
        {
            if (!InterestPickerMod.DisabledDueToError || InterestPickerMod.DisableWarningShown)
                return;

            InterestPickerMod.DisableWarningShown = true;
            PUIElements.ShowConfirmDialog(
                null,
                ModStrings.Get(ModStrings.DisabledMessage),
                null,
                null,
                ModStrings.Get(ModStrings.Ok),
                null);
        }
    }
}
