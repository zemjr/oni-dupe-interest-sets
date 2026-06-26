using HarmonyLib;
using KMod;
using PeterHan.PLib.Options;

namespace InterestPicker
{
    public sealed class Loader : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            InterestPickerMod.ModPath = path;
            InterestPickerMod.ModStaticId = mod.staticID;
            InterestPickerMod.ModLabel = mod.label;
            InterestPickerMod.Log("Mod loaded - version " + InterestPickerMod.GetModVersion());
            ModStrings.Register();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            base.OnLoad(harmony);
        }
    }
}
