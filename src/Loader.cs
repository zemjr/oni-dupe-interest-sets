using HarmonyLib;
using KMod;
using PeterHan.PLib.Options;

namespace InterestPicker
{
    public sealed class Loader : UserMod2
    {
        public const string Version = "0.2.0";

        public override void OnLoad(Harmony harmony)
        {
            InterestPickerMod.ModPath = path;
            InterestPickerMod.ModStaticId = mod.staticID;
            InterestPickerMod.ModLabel = mod.label;
            InterestPickerMod.Log("Mod loaded - version " + Version);
            ModStrings.Register();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            base.OnLoad(harmony);
        }
    }
}
