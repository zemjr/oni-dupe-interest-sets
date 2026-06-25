using System;
using Database;
using HarmonyLib;

namespace InterestPicker.Patches
{
    [HarmonyPatch(typeof(CharacterContainer), "archetypeDropDownSort")]
    internal static class CharacterContainer_Patch
    {
        private static bool Prefix(IListableOption a, IListableOption b, ref int __result)
        {
            try
            {
                if (InterestPickerMod.DisabledDueToError)
                    return true;

                SkillGroup left = a as SkillGroup;
                SkillGroup right = b as SkillGroup;
                bool leftCustom = left != null && InterestPickerMod.IsCustomCategory(left.Id);
                bool rightCustom = right != null && InterestPickerMod.IsCustomCategory(right.Id);

                if (!leftCustom && !rightCustom)
                    return true;

                if (leftCustom && !rightCustom)
                {
                    __result = 1;
                    return false;
                }

                if (!leftCustom && rightCustom)
                {
                    __result = -1;
                    return false;
                }

                int leftOrder = InterestPickerMod.GetCustomCategoryOrder(left.Id);
                int rightOrder = InterestPickerMod.GetCustomCategoryOrder(right.Id);
                __result = rightOrder.CompareTo(leftOrder);
                if (__result == 0)
                    __result = string.Compare(b.GetProperName(), a.GetProperName(), StringComparison.CurrentCulture);
                return false;
            }
            catch (Exception ex)
            {
                InterestPickerMod.DisableForSession("Failed to sort dropdown; falling back to vanilla sort.", ex);
                return true;
            }
        }
    }
}
