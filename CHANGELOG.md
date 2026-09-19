# Changelog

All notable changes to this project will be documented in this file.

## [v1.9.0]

### Added
- **Quest search:** Quest requirements are now picked by searching the quest name instead of pasting its ID

### Fixed
- **Quest-unlocked recipes now work:** recipes with a Quest requirement get a matching craft-unlock reward on that quest, so they unlock when the quest is completed (quests already finished unlock on the next game start) and the quest's reward list shows the craft icon
- Saving a recipe with a Quest requirement turns on *Locked* automatically, since SPT only unlocks locked recipes; a warning is shown when SPT can't tell the recipe apart from other recipes unlocked by the same quest
- Craft time now has a 10s minimum. Very short times let the product be taken before the server finished the craft, which duplicated it and kicked the player to the main menu. Existing configs are raised to the minimum on startup
- New and cloned recipes now keep their *Locked*, *Continuous*, *Needs fuel*, *Encoded*, *Code production* and limit settings right away instead of only after a server restart

## [v1.8.0]

### Added
- **Bulk restore:** the *Removed* tab now has a *Select* button that activates multi-select mode, letting you restore multiple deleted recipes at once with *Restore selected (N)* or restore everything in one shot with *Restore all (N)*
- **Clone & clean up:** after a batch clone a dialog asks whether to also delete the originals, so you can keep only the clones without having to hunt down and remove each source recipe manually

## [v1.7.0]

### Added
- **Batch clone:** activate multi-select mode with the new *Select* button to pick multiple recipes and clone them all to another station in one click
- **Batch delete:** delete multiple selected recipes at once from the same multi-select action bar
- After batch-deleting SPT original recipes, the UI automatically switches to the *Removed* tab so they can be reviewed or restored
- Info tooltip on the *Removed* tab explaining what appears there and the restore behaviour

### Fixed
- *Clone to station* dropdown now only lists stations visible in your station bar (consistent with the rest of the UI)
- Delete toast notifications now use a red (Error) style instead of yellow (Warning) for better readability

## [v1.6.0]

### Added
- Station filter bar: click the **+** card to open a station picker modal with images and icon fallbacks
- Station filter bar: each station card now has an **×** button to remove it from the bar
- Station bar is seeded automatically from stations that have at least one recipe on first run (no more empty bar after install)

### Changed
- Station names throughout the UI now use human-readable locale strings ("Intelligence Center" instead of "IntelligenceCenter")
- Replaced the old station settings dialog with the inline add/remove UX directly on the bar
- Add-station picker is now a modal grid instead of a dropdown (avoids clipping issues)

## [v1.5.0]

### Added
- New Craft dialog: output quantity field
- New Craft dialog: production time field with live human-readable chip (e.g. "2h 30m")
- New Craft dialog: station dropdown now shows only stations that have actual crafting recipes in SPT (data-driven, no hardcoded list)
- New Craft dialog: station names are now human-readable ("Intelligence Center" instead of "IntelligenceCenter")
- New Craft dialog: "Create Craft" button is disabled until an output item is selected

### Fixed
- Output item search no longer shows abstract parent/category items (e.g. "Assault rifle" node) — only concrete, stash-storable items are returned

## [v1.4.0]

### Added
- Restore original SPT recipes from the UI without restarting the server

### Changed
- All UI strings now use English regardless of the SPT server language setting

## [v1.3.0]

### Added
- Recipe list sorting (by name, station, or production time)
- Station filter replaced with a card carousel
- Recipe list split into Default and Custom tabs
- In-UI notification when a newer mod version is available on GitHub
- Mod version badge in the dashboard header
- Warn about unsaved recipe edits before navigating away

### Changed
- Accent color standardized to amber across all controls
- Station filter cards are more compact
- Detail panel: Production and Flags sections placed side by side
- Save row pinned to the bottom of the detail panel
- Recipe list rows no longer show raw recipe IDs

### Fixed
- White text illegible on filled amber controls
- Duplicate item name no longer shown under Resource requirements

## [v1.2.0]

### Changed
- Updated to SPTushonka 4.1.3 packages

### Fixed
- Editing a user-added (custom) recipe now updates the addition record in place instead of creating a duplicate modification entry

## [v1.1.2]

### Fixed
- Recipe ID now persisted in addition records so custom recipes survive server restarts

## [v1.1.1]

### Fixed
- Default `config.json` is created automatically on first run if missing

## [v1.1.0]

### Added
- Tooltips on production fields (time, count, production limit)

## [v1.0.0]

### Added
- Initial release: split-panel Blazor UI for viewing and editing hideout crafting recipes
- CRUD operations applied directly to SPT's in-memory recipe table (no server restart needed)
- Server-side icon caching to avoid repeated CDN requests
- Item search autocomplete in the edit panel
- Mod zip package generated automatically on build
