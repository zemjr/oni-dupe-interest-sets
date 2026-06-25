using System.Collections.Generic;
using Newtonsoft.Json;

namespace InterestPicker
{
    public sealed class ModConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("customCategories")]
        public List<CustomCategoryConfig> CustomCategories { get; set; } = new List<CustomCategoryConfig>();
    }

    public sealed class CustomCategoryConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("interests")]
        public List<string> Interests { get; set; } = new List<string>();
    }
}
