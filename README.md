# Dupe Interest Sets

[![ONI Update Watchdog](https://github.com/zemjr/oni-dupe-interest-sets/actions/workflows/oni-update-watchdog.yml/badge.svg)](https://github.com/zemjr/oni-dupe-interest-sets/actions/workflows/oni-update-watchdog.yml)

Dupe Interest Sets adds configurable interest-set categories to the duplicant reroll dropdown in Oxygen Not Included.

Each custom category represents a fixed set of 1 to 3 duplicant interests. Vanilla categories are left untouched and continue to work normally.

The mod ships with no extra categories enabled by default. Add your own sets from the in-game options screen or by editing `Config.json`.

## Features

- Adds custom interest sets to the duplicant reroll dropdown.
- Supports 1, 2, or 3 interests per custom category.
- Keeps vanilla categories unchanged.
- Keeps traits, stress reactions, attributes, and other random generation behavior vanilla.
- Provides an in-game configuration screen using PLib.
- Lets you reorder custom categories in the editor; the dropdown follows that order after restart.
- Includes English and Brazilian Portuguese UI translations.
- Saves settings to `Config.json`.

## In-Game Configuration

1. Open the Oxygen Not Included Mods menu.
2. Enable `Dupe Interest Sets`.
3. Restart the game if prompted.
4. Return to the Mods menu and open the mod options.
5. Click `Edit Categories`.
6. Add, remove, edit, or reorder custom categories.
7. Save and restart the game to rebuild the duplicant dropdown.

Restarting is required because the game registers duplicant skill groups during database initialization.

## Manual Configuration

You can also edit `Config.json` directly in the mod folder.

Default config:

```json
{
  "enabled": true,
  "customCategories": []
}
```

Example:

```json
{
  "enabled": true,
  "customCategories": [
    {
      "id": "BuilderResearcher",
      "displayName": "Builder Researcher",
      "interests": ["Building", "Research"]
    },
    {
      "id": "FarmCookRanch",
      "displayName": "Farm Cook Ranch",
      "interests": ["Farming", "Cooking", "Ranching"]
    }
  ]
}
```

Rules:

- `enabled: false` disables the mod behavior.
- `id` must be unique and contain only letters and numbers.
- `displayName` is shown in the dropdown.
- `interests` must contain 1 to 3 valid interest IDs.
- Invalid categories are skipped and logged.

## Valid Interest IDs

```text
Mining       -> Digging
Building     -> Building
Farming      -> Farming
Ranching     -> Ranching
Cooking      -> Cooking
Art          -> Decorating
Research     -> Researching
Rocketry     -> Rocketry
Suits        -> Suit Wearing
Hauling      -> Supplying
Technicals   -> Operating
MedicalAid   -> Doctoring
Basekeeping  -> Tidying
```

`Rocketry` only appears in the in-game editor and validates in `Config.json` when the game registers it as an available interest, such as when the related space content is active.

## Translation

Built-in translation files live in `translations/`:

```text
translations/en.json
translations/pt-BR.json
```

English is the fallback language. Dupe Interest Sets registers every UI string under this prefix:

```text
STRINGS.UI.DUPEINTERESTSETS.
```

External translation mods can override UI text by registering the same keys with `Strings.Add(...)` after this mod loads.

Example:

```csharp
Strings.Add(
    "STRINGS.UI.DUPEINTERESTSETS.DIALOG.SAVE",
    "Translated Save Text");
```

Custom category names are written by the player and are not translated. Interest names come from Oxygen Not Included's own `SkillGroup` names, so they follow the active game language.

## Logging

Logs use this prefix:

```text
[DupeInterestSets]
```

Oxygen Not Included writes logs to:

```text
%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log
```

## Build Notes

The current Oxygen Not Included assemblies target .NET Framework 4.8, so the project builds as `net48`.

On Windows with the default Steam install path:

```powershell
dotnet build .\src\InterestPicker.csproj -c Release
```

On Linux or macOS, point `OniManagedDir` to the game's `OxygenNotIncluded_Data/Managed` folder:

```bash
dotnet build ./src/InterestPicker.csproj -c Release -p:OniManagedDir="/path/to/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
```

Local install command:

```powershell
.\scripts\install-local.ps1
```

The local install script is Windows-oriented and preserves an existing `Config.json`. It only creates the default config when the target mod folder does not already have one.

## ONI Update Watchdog

The repository includes a GitHub Actions watchdog that checks the Oxygen Not Included public Steam build once per day.

Files:

```text
.github/oni-watch.json
.github/workflows/oni-update-watchdog.yml
scripts/check-oni-update.ps1
```

The script compares the current public Steam build ID with the baseline in `.github/oni-watch.json`. If the build changes, the workflow opens a compatibility-check issue with a manual checklist.

Run it locally:

```powershell
pwsh ./scripts/check-oni-update.ps1 -OutputPath oni-watch-result.json
```

The watchdog does not prove compatibility automatically. It only detects that ONI changed and creates a reminder to rebuild and verify the Harmony patches.

## Known Limitations

- Changes require a game restart to affect the duplicant dropdown.
- The mod focuses only on interest-set categories.
- Bionic duplicants use special vanilla generation logic; the mod is primarily intended for regular duplicants.
