using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Database;
using Klei.AI;
using KMod;
using Newtonsoft.Json;
using TUNING;
using UnityEngine;
using Attribute = Klei.AI.Attribute;

namespace InterestPicker
{
    internal static class InterestPickerMod
    {
        private const string Prefix = "[DupeInterestSets] ";
        private static readonly Dictionary<string, RegisteredCategory> Categories = new Dictionary<string, RegisteredCategory>(StringComparer.Ordinal);
        private static readonly HashSet<string> CustomSkillGroupIds = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> CustomCategoryOrder = new Dictionary<string, int>(StringComparer.Ordinal);

        public static string ModPath { get; set; }
        public static string ModStaticId { get; set; }
        public static Label ModLabel { get; set; }
        public static bool DisabledDueToError { get; private set; }
        public static string DisableReason { get; private set; }
        public static bool DisableWarningShown { get; set; }

        public static bool HasRegisteredCategories => !DisabledDueToError && Categories.Count > 0;

        public static void Log(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message, Exception exception)
        {
            Debug.LogError(Prefix + message + "\n" + exception);
        }

        public static string GetModVersion()
        {
            try
            {
                string modInfoPath = Path.Combine(ModPath ?? string.Empty, "mod_info.yaml");
                if (!File.Exists(modInfoPath))
                    return "unknown";

                foreach (string line in File.ReadAllLines(modInfoPath))
                {
                    string trimmed = line.Trim();
                    if (!trimmed.StartsWith("version:", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string version = trimmed.Substring("version:".Length).Trim().Trim('"', '\'');
                    return string.IsNullOrWhiteSpace(version) ? "unknown" : version;
                }
            }
            catch (Exception ex)
            {
                Warn("Failed to read version from mod_info.yaml: " + ex.Message);
            }

            return "unknown";
        }

        public static void DisableForSession(string reason, Exception exception)
        {
            if (DisabledDueToError)
                return;

            DisabledDueToError = true;
            DisableReason = reason;
            ClearRegisteredState();
            Error(reason + " Disabling Dupe Interest Sets for this session. Vanilla behavior will continue. Check Steam Workshop or GitHub for an updated version. If the problem continues, report it with Player.log and your ONI build number.", exception);
        }

        public static bool IsCustomCategory(string id)
        {
            return !DisabledDueToError && id != null && CustomSkillGroupIds.Contains(id);
        }

        public static int GetCustomCategoryOrder(string id)
        {
            return id != null && CustomCategoryOrder.TryGetValue(id, out int order)
                ? order
                : int.MaxValue;
        }

        public static bool TryGetCategory(string id, out RegisteredCategory category)
        {
            if (DisabledDueToError || id == null)
            {
                category = null;
                return false;
            }
            return Categories.TryGetValue(id, out category);
        }

        public static void RegisterConfiguredSkillGroups()
        {
            ClearRegisteredState();

            if (DisabledDueToError)
                return;

            ModConfig config = LoadConfig();
            if (config == null)
                return;

            if (!config.Enabled)
            {
                Log("Config enabled=false; no custom categories will be registered.");
                return;
            }

            if (config.CustomCategories == null || config.CustomCategories.Count == 0)
            {
                Warn("Config has no customCategories; no custom categories will be registered.");
                return;
            }

            Db db = Db.Get();
            if (db?.SkillGroups?.resources == null)
                throw new InvalidOperationException("Db.SkillGroups was not available during Db.Initialize postfix.");

            Dictionary<string, SkillGroup> vanillaSkillGroups = db.SkillGroups.resources
                .Where(group => group != null && group.allowAsAptitude)
                .ToDictionary(group => group.Id, group => group, StringComparer.OrdinalIgnoreCase);

            if (vanillaSkillGroups.Count == 0)
                throw new InvalidOperationException("No vanilla SkillGroups were available as aptitudes.");

            HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < config.CustomCategories.Count; i++)
            {
                try
                {
                    RegisterCategory(db, vanillaSkillGroups, seenIds, config.CustomCategories[i], i);
                }
                catch (Exception ex)
                {
                    Error("Failed to validate/register custom category.", ex);
                }
            }

            Log("Categorias custom válidas registradas: " + Categories.Count);
        }

        public static void ClearRegisteredState()
        {
            Categories.Clear();
            CustomSkillGroupIds.Clear();
            CustomCategoryOrder.Clear();
        }

        public static ModConfig LoadConfigForEditor()
        {
            try
            {
                string configPath = GetConfigPath();
                return File.Exists(configPath)
                    ? JsonConvert.DeserializeObject<ModConfig>(File.ReadAllText(configPath))
                    : null;
            }
            catch (Exception ex)
            {
                Error("Failed to read Config.json in the editor.", ex);
                return null;
            }
        }

        public static ModConfig CreateDefaultConfig()
        {
            return new ModConfig
            {
                Enabled = true,
                CustomCategories = new List<CustomCategoryConfig>()
            };
        }

        public static void SaveConfigFromEditor(ModConfig config)
        {
            string configPath = GetConfigPath();
            File.WriteAllText(configPath, JsonConvert.SerializeObject(config ?? CreateDefaultConfig(), Formatting.Indented));
            Log("Config salvo pelo editor: " + configPath);
        }

        private static string GetConfigPath()
        {
            string root = !string.IsNullOrEmpty(ModPath)
                ? ModPath
                : Path.GetDirectoryName(typeof(InterestPickerMod).Assembly.Location);
            return Path.Combine(root, "Config.json");
        }

        private static ModConfig LoadConfig()
        {
            try
            {
                string configPath = GetConfigPath();

                if (!File.Exists(configPath))
                {
                    Warn("Config.json not found at " + configPath + "; mod will stay inactive.");
                    return null;
                }

                ModConfig config = JsonConvert.DeserializeObject<ModConfig>(File.ReadAllText(configPath));
                Log("Config loaded: " + (config?.CustomCategories?.Count ?? 0) + " declared category/categories.");
                return config;
            }
            catch (Exception ex)
            {
                Error("Failed to read Config.json; mod will stay inactive.", ex);
                return null;
            }
        }

        private static void RegisterCategory(
            Db db,
            Dictionary<string, SkillGroup> vanillaSkillGroups,
            HashSet<string> seenIds,
            CustomCategoryConfig rawCategory,
            int order)
        {
            if (rawCategory == null)
            {
                Warn("Null category found in Config.json; skipping.");
                return;
            }

            string id = (rawCategory.Id ?? string.Empty).Trim();
            string displayName = (rawCategory.DisplayName ?? string.Empty).Trim();

            if (!IsValidId(id))
            {
                Warn("Skipping category with invalid id: '" + rawCategory.Id + "'. Use only letters and numbers, no spaces.");
                return;
            }

            if (!seenIds.Add(id))
            {
                Warn("Skipping duplicate category: " + id);
                return;
            }

            if (db.SkillGroups.TryGet(id) != null)
            {
                Warn("Category '" + id + "' conflicts with an existing SkillGroup; skipping.");
                return;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                Warn("Category '" + id + "' has no displayName; skipping.");
                return;
            }

            if (rawCategory.Interests == null || rawCategory.Interests.Count == 0)
            {
                Warn("Category '" + id + "' has no interests; skipping.");
                return;
            }

            if (rawCategory.Interests.Count > 3)
            {
                Warn("Category '" + id + "' has more than 3 interests; skipping.");
                return;
            }

            List<SkillGroup> interests = new List<SkillGroup>();
            HashSet<string> interestIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string configuredInterest in rawCategory.Interests)
            {
                string interestId = (configuredInterest ?? string.Empty).Trim();
                if (!vanillaSkillGroups.TryGetValue(interestId, out SkillGroup skillGroup))
                {
                    Warn("Category '" + id + "' contains unavailable or invalid interest: " + configuredInterest + "; skipping category.");
                    return;
                }

                if (!interestIds.Add(skillGroup.Id))
                {
                    Warn("Category '" + id + "' contains duplicate interest: " + skillGroup.Id + "; skipping category.");
                    return;
                }

                interests.Add(skillGroup);
            }

            SkillGroup first = interests[0];
            SkillGroup customSkillGroup = new SkillGroup(id, first.choreGroupID ?? string.Empty, displayName, first.choreGroupIcon, first.archetypeIcon)
            {
                relevantAttributes = UniqueAttributes(interests),
                requiredChoreGroups = interests.SelectMany(group => group.requiredChoreGroups ?? new List<string>())
                    .Where(value => !string.IsNullOrEmpty(value))
                    .Distinct()
                    .ToList(),
                allowAsAptitude = true
            };

            db.SkillGroups.Add(customSkillGroup);
            Strings.Add("STRINGS.DUPLICANTS.SKILLGROUPS." + id.ToUpperInvariant() + ".NAME", displayName);

            RegisteredCategory category = new RegisteredCategory(id, displayName, interests.Select(group => group.Id).ToList());
            Categories[id] = category;
            CustomSkillGroupIds.Add(id);
            CustomCategoryOrder[id] = order;
            RegisterTraitCompatibility(id, category.InterestIds);

            Log("Registered category: " + id + " -> " + string.Join(", ", category.InterestIds.ToArray()));
        }

        private static bool IsValidId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            for (int i = 0; i < id.Length; i++)
            {
                if (!char.IsLetterOrDigit(id[i]))
                    return false;
            }
            return true;
        }

        private static List<Attribute> UniqueAttributes(List<SkillGroup> groups)
        {
            Dictionary<string, Attribute> attributes = new Dictionary<string, Attribute>(StringComparer.Ordinal);
            foreach (SkillGroup group in groups)
            {
                if (group.relevantAttributes == null)
                    continue;
                foreach (Attribute attribute in group.relevantAttributes)
                {
                    if (attribute != null && !attributes.ContainsKey(attribute.Id))
                        attributes.Add(attribute.Id, attribute);
                }
            }
            return attributes.Values.ToList();
        }

        private static void RegisterTraitCompatibility(string customId, List<string> interestIds)
        {
            AddUnionEntry(DUPLICANTSTATS.ARCHETYPE_TRAIT_EXCLUSIONS, customId, interestIds);
            AddUnionEntry(DUPLICANTSTATS.ARCHETYPE_BIONIC_TRAIT_COMPATIBILITY, customId, interestIds);
        }

        private static void AddUnionEntry(Dictionary<string, List<string>> table, string customId, List<string> interestIds)
        {
            HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
            foreach (string interestId in interestIds)
            {
                if (table.TryGetValue(interestId, out List<string> entries))
                {
                    foreach (string entry in entries)
                        values.Add(entry);
                }
            }
            table[customId] = values.ToList();
        }

        public static bool GenerateAptitudesPrefix(MinionStartingStats stats, string guaranteedAptitudeID)
        {
            if (DisabledDueToError || !HasRegisteredCategories)
                return true;

            if (stats?.personality?.model == BionicMinionConfig.MODEL)
                return true;

            if (stats?.skillAptitudes == null)
                throw new InvalidOperationException("MinionStartingStats.skillAptitudes was not available.");

            Db db = Db.Get();
            if (db?.SkillGroups?.resources == null)
                throw new InvalidOperationException("Db.SkillGroups was not available during aptitude generation.");

            stats.skillAptitudes.Clear();

            if (TryGetCategory(guaranteedAptitudeID, out RegisteredCategory category))
            {
                foreach (string interestId in category.InterestIds)
                {
                    SkillGroup interest = db.SkillGroups.TryGet(interestId);
                    if (interest != null)
                        stats.skillAptitudes[interest] = DUPLICANTSTATS.APTITUDE_BONUS;
                }
                return false;
            }

            // Custom SkillGroups are UI markers. Vanilla random rolls must choose only real interests.
            int count = UnityEngine.Random.Range(1, 4);
            List<SkillGroup> pool = db.SkillGroups.resources
                .Where(group => group.allowAsAptitude && !IsCustomCategory(group.Id))
                .ToList();
            Shuffle(pool);

            if (!string.IsNullOrEmpty(guaranteedAptitudeID))
            {
                SkillGroup guaranteed = db.SkillGroups.TryGet(guaranteedAptitudeID);
                if (guaranteed != null && !IsCustomCategory(guaranteed.Id))
                {
                    stats.skillAptitudes[guaranteed] = DUPLICANTSTATS.APTITUDE_BONUS;
                    pool.Remove(guaranteed);
                    count--;
                }
            }

            for (int i = 0; i < count && i < pool.Count; i++)
                stats.skillAptitudes[pool[i]] = DUPLICANTSTATS.APTITUDE_BONUS;

            return false;
        }

        private static void Shuffle<T>(IList<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T current = values[i];
                values[i] = values[j];
                values[j] = current;
            }
        }
    }

    internal sealed class RegisteredCategory
    {
        public RegisteredCategory(string id, string displayName, List<string> interestIds)
        {
            Id = id;
            DisplayName = displayName;
            InterestIds = interestIds;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public List<string> InterestIds { get; }
    }
}
