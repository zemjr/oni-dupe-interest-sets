using System;
using InterestPicker.UI;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace InterestPicker
{
    [RestartRequired]
    [ConfigFile("DupeInterestSetsOptions.json", true)]
    [ModInfo("https://github.com/peterhaneve/ONIMods/tree/master/PLib")]
    public sealed class ModOptions
    {
        [JsonIgnore]
        [Option(ModStrings.OptionsEditCategories, ModStrings.OptionsEditCategoriesTooltip)]
        public Action<object> EditCategories
        {
            get { return _ => InterestSetsOptionsDialog.Show(); }
            set { }
        }
    }
}
