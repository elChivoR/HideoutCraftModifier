# Changelog

All notable changes to this project will be documented in this file.

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
