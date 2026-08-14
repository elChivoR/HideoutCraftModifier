# HideoutCraftModifier (HCM)

SPT 4.1.2 server mod that lets you manage hideout crafting recipes through a web UI integrated into SPT's built-in Blazor server.

## Features

- **View all recipes** — filterable by station and searchable by item name, station, or ID
- **Edit existing recipes** — production time, output count, limit, requirements, and flags (locked, continuous, etc.)
- **Add new recipes** — pick a station and output item, then configure requirements inline
- **Remove recipes** — with confirmation dialog
- **Live changes** — modifications apply immediately to SPT's in-memory data, no server restart needed
- **Persistent config** — all changes are saved to `config.json` and reapplied on server start
- **Item icons** — loaded from [tarkov.dev](https://tarkov.dev) CDN for visual reference
- **English item names** — resolved from SPT's locale database

## Requirements

- SPT 4.1.2
- .NET 10.0 SDK

## Building

```bash
dotnet build HideoutCraftModifier.csproj
```

The output DLL goes to `bin/Debug/HideoutCraftModifier/HideoutCraftModifier.dll`.

## Installation

Extract the release zip into your SPT server's mods directory:

```
SPT_Runtime/
  user/
    mods/
      HideoutCraftModifier/
        HideoutCraftModifier.dll
        config.json
```

## Usage

1. Start the SPT server
2. Open the SPT web dashboard in your browser
3. Click on "HideoutCraftModifier" in the mods list, or navigate to `/hcm`
4. Use the left panel to browse/search recipes, click one to edit it in the right panel
5. Click "Save Changes" to apply and persist modifications

## Project Structure

```
HideoutCraftModifier/
├── HideoutCraftModifier.cs    # Entry point (IOnLoad) — initializes the service after SPT loads
├── ModMetadata.cs             # Mod identity + Blazor web registration (IModBlazorMetadata)
├── config.json                # Persisted user changes (modifications, additions, removals)
├── Models/
│   ├── ModConfig.cs           # Config serialization models for persistence
│   └── RecipeViewModel.cs     # View models for the Blazor UI
├── Services/
│   └── RecipeService.cs       # Core service — CRUD on in-memory recipes + config persistence
├── Web/
│   ├── _imports.razor         # Blazor using directives
│   ├── Layouts/
│   │   └── HcmLayout.razor    # Page layout with header
│   └── Pages/
│       ├── Home.razor         # Main split-panel UI (recipe list + inline editor)
│       ├── AddRecipeDialog.razor   # Minimal dialog for new recipe (station + output item)
│       └── ConfirmDialog.razor     # Generic confirmation dialog
└── wwwroot/
    └── css/
        └── hcm.css            # Custom styles for the split-panel layout
```

## How It Works

### Live In-Memory Modification

SPT loads all hideout recipes into `HideoutTable.Production.Recipes` at startup. This mod operates directly on that in-memory list — when you edit a recipe through the web UI, the change takes effect immediately for any client that queries the server. No restart required.

### Config Persistence

User changes are tracked in three categories in `config.json`:

- **`modifications`** — edits to existing recipes (partial updates, only changed fields are stored)
- **`additions`** — completely new recipes created by the user
- **`removals`** — IDs of original SPT recipes that were deleted

On server startup, `ApplyConfig()` replays these changes in order: removals → modifications → additions.

### Requirement Types

Each recipe requirement has a `type` that determines its behavior:

| Type | Fields Used | Description |
|------|-------------|-------------|
| `Item` | `templateId`, `count` | Consumes X items from stash |
| `Tool` | `templateId`, `isFunctional` | Requires tool in stash (not consumed) |
| `Area` | `areaType`, `requiredLevel` | Requires hideout area at minimum level |
| `QuestComplete` | `questId` | Requires quest completion |
| `Resource` | `templateId`, `resource` | Consumes resource amount (e.g., water, fuel) |

### Item Icons

Icons are loaded from the tarkov.dev CDN using the pattern:
```
https://assets.tarkov.dev/{templateId}-icon.webp
```

### NuGet Packages

| Package | Purpose |
|---------|---------|
| `SPTarkov.Common` | Shared types (logging, etc.) |
| `SPTarkov.DI` | Dependency injection (`[Injectable]` attribute) |
| `SPTarkov.Server.Core` | Game models, services, and tables |
| `SPTarkov.Server.Web` | Blazor integration (`IModBlazorMetadata`) |
| `MudBlazor` | UI component library (dark theme) |

## License

MIT
