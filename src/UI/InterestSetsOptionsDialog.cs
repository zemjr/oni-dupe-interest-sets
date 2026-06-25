using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Database;
using PeterHan.PLib.UI;
using UnityEngine;

namespace InterestPicker.UI
{
    internal static class InterestSetsOptionsDialog
    {
        private const string ButtonAdd = "add";
        private const string ButtonSave = "save";
        private const string ButtonClose = "close";

        private static readonly List<CategoryEditorRow> Rows = new List<CategoryEditorRow>();
        private static TextStyleSetting removeButtonTextStyle;
        private static KScreen currentScreen;
        private static bool suppressClose;

        public static void Show()
        {
            try
            {
                ModConfig config = InterestPickerMod.LoadConfigForEditor() ?? InterestPickerMod.CreateDefaultConfig();
                Rows.Clear();
                if (config.CustomCategories != null)
                {
                    foreach (CustomCategoryConfig category in config.CustomCategories)
                        Rows.Add(CategoryEditorRow.FromConfig(category));
                }
                ShowDialog();
            }
            catch (Exception ex)
            {
                InterestPickerMod.Error("Failed to open category editor.", ex);
            }
        }

        private static void ShowDialog()
        {
            CloseCurrentDialog();
            PDialog dialog = new PDialog("DupeInterestSetsOptions")
            {
                Title = T(ModStrings.DialogTitle),
                Size = new Vector2(1100f, 660f),
                MaxSize = new Vector2(1280f, 760f),
                SortKey = 150f,
                DialogBackColor = PUITuning.Colors.OptionsBackground,
                DialogClosed = OnDialogClosed
            };

            dialog.Body.AddChild(BuildBody());
            dialog.AddButton(ButtonAdd, T(ModStrings.AddCategory), T(ModStrings.AddCategoryTooltip));
            dialog.AddButton(ButtonSave, T(ModStrings.Save), T(ModStrings.SaveTooltip));
            dialog.AddButton(ButtonClose, T(ModStrings.Close), T(ModStrings.CloseTooltip));
            GameObject dialogObject = dialog.Build();
            if (dialogObject.TryGetComponent(out currentScreen))
                currentScreen.Activate();
        }

        private static IUIComponent BuildBody()
        {
            PPanel body = new PPanel("EditorBody")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 8,
                Margin = new RectOffset(8, 8, 8, 8),
                FlexSize = Vector2.one
            };

            body.AddChild(new PLabel("Hint")
            {
                Text = T(ModStrings.Hint),
                TextStyle = PUITuning.Fonts.TextLightStyle,
                DynamicSize = true
            });

            PPanel content = new PPanel("RowsPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 6,
                Margin = new RectOffset(0, 0, 4, 4),
                FlexSize = Vector2.one
            };

            content.AddChild(BuildHeader());
            if (Rows.Count == 0)
            {
                content.AddChild(new PLabel("Empty")
                {
                    Text = T(ModStrings.Empty),
                    TextStyle = PUITuning.Fonts.TextLightStyle,
                    DynamicSize = true
                });
            }
            else
            {
                for (int i = 0; i < Rows.Count; i++)
                    content.AddChild(BuildRow(i, Rows[i]));
            }

            body.AddChild(new PScrollPane("RowsScroll")
            {
                Child = content,
                ScrollHorizontal = false,
                ScrollVertical = true,
                AlwaysShowVertical = true,
                FlexSize = Vector2.one,
                TrackSize = 12f
            }.SetKleiBlueColor());

            return body;
        }

        private static IUIComponent BuildHeader()
        {
            PGridPanel grid = CreateRowGrid("Header");
            grid.AddChild(HeaderLabel(""), Cell(0, 0));
            grid.AddChild(HeaderLabel(""), Cell(0, 1));
            grid.AddChild(HeaderLabel(T(ModStrings.CategoryName)), Cell(0, 2));
            grid.AddChild(HeaderLabel(T(ModStrings.Interest1)), Cell(0, 3));
            grid.AddChild(HeaderLabel(T(ModStrings.Interest2)), Cell(0, 4));
            grid.AddChild(HeaderLabel(T(ModStrings.Interest3)), Cell(0, 5));
            grid.AddChild(HeaderLabel(""), Cell(0, 6));
            return grid;
        }

        private static IUIComponent BuildRow(int index, CategoryEditorRow row)
        {
            PGridPanel grid = CreateRowGrid("CategoryRow" + index);
            grid.AddChild(new PButton("MoveUp" + index)
            {
                Text = "↑",
                ToolTip = T(ModStrings.MoveUpTooltip),
                OnClick = _ =>
                {
                    MoveRow(index, -1);
                    ShowDialog();
                }
            }.SetKleiBlueStyle(), Cell(0, 0));
            grid.AddChild(new PButton("MoveDown" + index)
            {
                Text = "↓",
                ToolTip = T(ModStrings.MoveDownTooltip),
                OnClick = _ =>
                {
                    MoveRow(index, 1);
                    ShowDialog();
                }
            }.SetKleiBlueStyle(), Cell(0, 1));
            grid.AddChild(new PTextField("DisplayName" + index)
            {
                Text = row.DisplayName,
                MaxLength = 64,
                MinWidth = 260,
                OnTextChanged = (_, text) => row.DisplayName = text
            }, Cell(0, 2));
            grid.AddChild(BuildInterestDropdown("InterestA" + index, row, 0), Cell(0, 3));
            grid.AddChild(BuildInterestDropdown("InterestB" + index, row, 1), Cell(0, 4));
            grid.AddChild(BuildInterestDropdown("InterestC" + index, row, 2), Cell(0, 5));
            grid.AddChild(new PSpacer()
            {
                PreferredSize = new Vector2(18f, 1f),
                FlexSize = Vector2.zero
            }, Cell(0, 6));
            PButton removeButton = new PButton("Remove" + index)
            {
                Text = T(ModStrings.Remove),
                ToolTip = T(ModStrings.RemoveTooltip),
                OnClick = _ =>
                {
                    Rows.RemoveAt(index);
                    ShowDialog();
                }
            }.SetKleiBlueStyle();
            removeButton.TextStyle = RemoveButtonTextStyle;
            grid.AddChild(removeButton, Cell(0, 7));
            return grid;
        }

        private static PComboBox<InterestOption> BuildInterestDropdown(string name, CategoryEditorRow row, int slot)
        {
            List<InterestOption> options = InterestOption.GetAvailable();
            InterestOption selected = options.FirstOrDefault(option => option.Id == row.Interests[slot]) ?? options[0];
            return new PComboBox<InterestOption>(name)
            {
                Content = options,
                InitialItem = selected,
                MaxRowsShown = 8,
                ToolTip = slot == 0 ? T(ModStrings.Required) : T(ModStrings.Optional),
                OnOptionSelected = (_, option) => row.Interests[slot] = option?.Id ?? string.Empty
            }.SetKleiBlueStyle().SetMinWidthInCharacters(15);
        }

        private static PGridPanel CreateRowGrid(string name)
        {
            return new PGridPanel(name)
                .AddColumn(new GridColumnSpec(36f))
                .AddColumn(new GridColumnSpec(36f))
                .AddColumn(new GridColumnSpec(290f))
                .AddColumn(new GridColumnSpec(150f))
                .AddColumn(new GridColumnSpec(150f))
                .AddColumn(new GridColumnSpec(150f))
                .AddColumn(new GridColumnSpec(18f))
                .AddColumn(new GridColumnSpec(105f))
                .AddRow(new GridRowSpec(0f));
        }

        private static PLabel HeaderLabel(string text)
        {
            return new PLabel("HeaderLabel")
            {
                Text = text,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                DynamicSize = true
            };
        }

        private static GridComponentSpec Cell(int row, int column)
        {
            return new GridComponentSpec(row, column)
            {
                Alignment = TextAnchor.MiddleLeft,
                Margin = new RectOffset(2, 2, 2, 2)
            };
        }

        private static void OnDialogClosed(string option)
        {
            try
            {
                if (suppressClose)
                    return;

                currentScreen = null;
                if (option == ButtonAdd)
                {
                    string name = T(ModStrings.NewCategory);
                    Rows.Add(new CategoryEditorRow
                    {
                        DisplayName = name
                    });
                    ShowDialog();
                }
                else if (option == ButtonSave)
                {
                    if (!TryCreateConfig(out ModConfig config, out string validationMessage))
                    {
                        PUIElements.ShowConfirmDialog(
                            null,
                            validationMessage,
                            ShowDialog,
                            null,
                            T(ModStrings.ValidationEdit),
                            T(ModStrings.Close));
                        return;
                    }

                    InterestPickerMod.SaveConfigFromEditor(config);
                    ShowRestartDialog();
                }
            }
            catch (Exception ex)
            {
                InterestPickerMod.Error("Failed to process category editor action.", ex);
            }
        }

        private static bool TryCreateConfig(out ModConfig config, out string validationMessage)
        {
            HashSet<string> availableInterestIds = new HashSet<string>(
                InterestOption.GetAvailable().Select(option => option.Id),
                StringComparer.OrdinalIgnoreCase);
            HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<CustomCategoryConfig> categories = new List<CustomCategoryConfig>();

            for (int i = 0; i < Rows.Count; i++)
            {
                CategoryEditorRow row = Rows[i];
                bool hasName = !string.IsNullOrWhiteSpace(row.DisplayName);
                bool hasAnyRawInterest = row.Interests.Any(value => !string.IsNullOrWhiteSpace(value));
                List<string> validInterests = row.GetValidInterests(availableInterestIds);

                if (!hasName && !hasAnyRawInterest)
                    continue;

                if (!hasName)
                {
                    config = null;
                    validationMessage = T(ModStrings.ValidationMissingName);
                    return false;
                }

                if (validInterests.Count == 0)
                {
                    config = null;
                    validationMessage = T(ModStrings.ValidationMissingInterest);
                    return false;
                }

                string id = GenerateUniqueId(row.DisplayName, usedIds);
                categories.Add(row.ToConfig(id, validInterests));
            }

            config = new ModConfig
            {
                Enabled = true,
                CustomCategories = categories
            };
            validationMessage = null;
            return true;
        }

        private static void ShowRestartDialog()
        {
            PUIElements.ShowConfirmDialog(
                null,
                T(ModStrings.RestartMessage),
                App.instance.Restart,
                null,
                T(ModStrings.RestartNow),
                T(ModStrings.Later));
        }

        private static void MoveRow(int index, int direction)
        {
            int target = index + direction;
            if (index < 0 || index >= Rows.Count || target < 0 || target >= Rows.Count)
                return;

            CategoryEditorRow row = Rows[index];
            Rows.RemoveAt(index);
            Rows.Insert(target, row);
        }

        private static string T(string key)
        {
            return ModStrings.Get(key);
        }

        private static void CloseCurrentDialog()
        {
            if (currentScreen == null)
                return;

            suppressClose = true;
            try
            {
                currentScreen.Deactivate();
            }
            finally
            {
                suppressClose = false;
                currentScreen = null;
            }
        }

        private static string GenerateId(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            StringBuilder builder = new StringBuilder(displayName.Length);
            string normalized = displayName.Normalize(NormalizationForm.FormD);
            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                if (c <= 127 && char.IsLetterOrDigit(c))
                    builder.Append(c);
            }

            return builder.ToString();
        }

        private static string GenerateUniqueId(string displayName, HashSet<string> usedIds)
        {
            string baseId = GenerateId(displayName);
            if (string.IsNullOrEmpty(baseId))
                baseId = "Category";

            string id = baseId;
            int suffix = 2;
            while (!usedIds.Add(id))
            {
                id = baseId + suffix;
                suffix++;
            }
            return id;
        }

        private static TextStyleSetting RemoveButtonTextStyle
        {
            get
            {
                if (removeButtonTextStyle == null)
                {
                    TextStyleSetting source = PUITuning.Fonts.UILightStyle;
                    removeButtonTextStyle = ScriptableObject.CreateInstance<TextStyleSetting>();
                    removeButtonTextStyle.Init(source.sdfFont, source.fontSize, new Color(1f, 0.25f, 0.22f), source.enableWordWrapping);
                    removeButtonTextStyle.style = source.style;
                }
                return removeButtonTextStyle;
            }
        }
    }

    internal sealed class CategoryEditorRow
    {
        public string DisplayName { get; set; } = string.Empty;
        public string[] Interests { get; } = new string[3];

        public static CategoryEditorRow FromConfig(CustomCategoryConfig config)
        {
            CategoryEditorRow row = new CategoryEditorRow
            {
                DisplayName = config?.DisplayName ?? string.Empty
            };
            if (config?.Interests != null)
            {
                for (int i = 0; i < config.Interests.Count && i < row.Interests.Length; i++)
                    row.Interests[i] = config.Interests[i] ?? string.Empty;
            }
            return row;
        }

        public List<string> GetValidInterests(HashSet<string> availableInterestIds)
        {
            return Interests
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Where(value => availableInterestIds.Contains(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public CustomCategoryConfig ToConfig(string id, List<string> interests)
        {
            return new CustomCategoryConfig
            {
                Id = id,
                DisplayName = DisplayName?.Trim(),
                Interests = interests
            };
        }
    }

    internal sealed class InterestOption : IListableOption
    {
        private static readonly List<string> VanillaOptionIds = new List<string>
        {
            "Mining",
            "Building",
            "Farming",
            "Ranching",
            "Cooking",
            "Art",
            "Research",
            "Rocketry",
            "Suits",
            "Hauling",
            "Technicals",
            "MedicalAid",
            "Basekeeping"
        };

        public static List<InterestOption> GetAvailable()
        {
            List<InterestOption> options = new List<InterestOption>
            {
                new InterestOption("", "-")
            };

            Db db = Db.Get();
            foreach (string id in VanillaOptionIds)
            {
                if (db == null)
                {
                    if (id != "Rocketry")
                        options.Add(new InterestOption(id, id));
                    continue;
                }

                SkillGroup skillGroup = db.SkillGroups?.TryGet(id);
                if (skillGroup != null && skillGroup.allowAsAptitude)
                    options.Add(new InterestOption(skillGroup.Id, ((IListableOption)skillGroup).GetProperName()));
            }

            return options;
        }

        public InterestOption(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }
        public string DisplayName { get; }

        public string GetProperName()
        {
            return DisplayName;
        }
    }
}
