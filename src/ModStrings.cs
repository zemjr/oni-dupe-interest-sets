using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace InterestPicker
{
    internal static class ModStrings
    {
        public const string Root = "STRINGS.UI.DUPEINTERESTSETS.";

        public const string OptionsEditCategories = Root + "OPTIONS.EDIT_CATEGORIES";
        public const string OptionsEditCategoriesTooltip = Root + "OPTIONS.EDIT_CATEGORIES_TOOLTIP";

        public const string DialogTitle = Root + "DIALOG.TITLE";
        public const string AddCategory = Root + "DIALOG.ADD_CATEGORY";
        public const string AddCategoryTooltip = Root + "DIALOG.ADD_CATEGORY_TOOLTIP";
        public const string Save = Root + "DIALOG.SAVE";
        public const string SaveTooltip = Root + "DIALOG.SAVE_TOOLTIP";
        public const string Close = Root + "DIALOG.CLOSE";
        public const string CloseTooltip = Root + "DIALOG.CLOSE_TOOLTIP";
        public const string Hint = Root + "DIALOG.HINT";
        public const string Empty = Root + "DIALOG.EMPTY";
        public const string CategoryName = Root + "DIALOG.CATEGORY_NAME";
        public const string Interest1 = Root + "DIALOG.INTEREST_1";
        public const string Interest2 = Root + "DIALOG.INTEREST_2";
        public const string Interest3 = Root + "DIALOG.INTEREST_3";
        public const string MoveUpTooltip = Root + "DIALOG.MOVE_UP_TOOLTIP";
        public const string MoveDownTooltip = Root + "DIALOG.MOVE_DOWN_TOOLTIP";
        public const string Remove = Root + "DIALOG.REMOVE";
        public const string RemoveTooltip = Root + "DIALOG.REMOVE_TOOLTIP";
        public const string Required = Root + "DIALOG.REQUIRED";
        public const string Optional = Root + "DIALOG.OPTIONAL";
        public const string ValidationMissingName = Root + "DIALOG.VALIDATION_MISSING_NAME";
        public const string ValidationMissingInterest = Root + "DIALOG.VALIDATION_MISSING_INTEREST";
        public const string ValidationEdit = Root + "DIALOG.VALIDATION_EDIT";
        public const string RestartMessage = Root + "DIALOG.RESTART_MESSAGE";
        public const string RestartNow = Root + "DIALOG.RESTART_NOW";
        public const string Later = Root + "DIALOG.LATER";
        public const string NewCategory = Root + "DIALOG.NEW_CATEGORY";

        private static readonly Dictionary<string, string> Fallbacks = new Dictionary<string, string>(StringComparer.Ordinal);

        public static void Register()
        {
            try
            {
                Fallbacks.Clear();
                LoadLanguageFile("en", true);

                string languageCode = GetCurrentLanguageCode();
                if (!string.IsNullOrWhiteSpace(languageCode) && !IsEnglish(languageCode))
                    LoadLanguageFile(languageCode, false);
            }
            catch (Exception ex)
            {
                InterestPickerMod.Error("Failed to load translations; English UI strings will be used where available.", ex);
            }
        }

        public static string Get(string key)
        {
            try
            {
                string value = Strings.Get(key);
                if (!string.IsNullOrEmpty(value) && value != key)
                    return value;
            }
            catch
            {
                // Fall back below; missing UI strings should never break the options screen.
            }

            return Fallbacks.TryGetValue(key, out string fallback) ? fallback : key;
        }

        private static void LoadLanguageFile(string languageCode, bool isFallback)
        {
            string path = FindLanguageFile(languageCode);
            if (path == null)
            {
                if (isFallback)
                    InterestPickerMod.Warn("Missing fallback translation file: en.json");
                return;
            }

            Dictionary<string, string> translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
            if (translations == null)
                return;

            foreach (KeyValuePair<string, string> translation in translations)
            {
                if (string.IsNullOrWhiteSpace(translation.Key) || translation.Value == null)
                    continue;

                if (isFallback)
                    Fallbacks[translation.Key] = translation.Value;

                Strings.Add(translation.Key, translation.Value);
            }

            InterestPickerMod.Log("Loaded translation file: " + Path.GetFileName(path));
        }

        private static string FindLanguageFile(string languageCode)
        {
            string translationsPath = Path.Combine(InterestPickerMod.ModPath ?? string.Empty, "translations");
            if (!Directory.Exists(translationsPath))
                return null;

            string normalizedCode = NormalizeLanguageCode(languageCode);
            List<string> candidates = Directory.GetFiles(translationsPath, "*.json").ToList();
            string exactMatch = candidates
                .FirstOrDefault(file => NormalizeLanguageCode(Path.GetFileNameWithoutExtension(file)) == normalizedCode);
            if (exactMatch != null)
                return exactMatch;

            if (normalizedCode == "pt")
            {
                return candidates
                    .FirstOrDefault(file => NormalizeLanguageCode(Path.GetFileNameWithoutExtension(file)) == "pt-br");
            }

            return null;
        }

        private static string GetCurrentLanguageCode()
        {
            try
            {
                return Localization.GetCurrentLanguageCode();
            }
            catch
            {
                return "en";
            }
        }

        private static bool IsEnglish(string languageCode)
        {
            return NormalizeLanguageCode(languageCode) == "en";
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return (languageCode ?? string.Empty).Trim().Replace('_', '-').ToLowerInvariant();
        }
    }
}
