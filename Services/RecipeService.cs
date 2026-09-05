using System.Reflection;
using HideoutCraftModifier.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;

namespace HideoutCraftModifier.Services;

/// <summary>
/// Core service that manages CRUD operations on hideout crafting recipes.
/// Operates directly on SPT's in-memory HideoutTable so changes take effect
/// immediately without a server restart. Persists user changes to config.json
/// so they survive restarts.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class RecipeService(
    ISptLogger<RecipeService> logger,
    HideoutTable hideoutTable,
    LocaleService localeService,
    ModHelper modHelper,
    JsonUtil jsonUtil)
{
    private ModConfig _config = new();
    private string _modPath = "";
    // Cached to avoid calling localeService.GetLocaleDb("en") on every item name resolution,
    // which was causing ~500ms delays when rendering the full recipe table.
    private Dictionary<string, string>? _localeCache;
    // Snapshot of all original SPT recipes taken before ApplyConfig() removes/modifies any.
    // Used to restore deleted or modified recipes without a server restart.
    private Dictionary<string, HideoutProduction> _originalRecipes = [];

    public ModConfig Config => _config;

    /// <summary>
    /// Loads config.json, caches English locale DB, and applies saved modifications
    /// to SPT's in-memory recipe list. Called once during server startup.
    /// </summary>
    public void Initialize()
    {
        _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var configPath = Path.Combine(_modPath, "config.json");
        if (!File.Exists(configPath))
        {
            File.WriteAllText(configPath, jsonUtil.Serialize(new ModConfig(), true));
            logger.Success("[HCM] Created default config.json");
        }
        _config = modHelper.GetJsonDataFromFile<ModConfig>(_modPath, "config.json") ?? new ModConfig();
        // Force English locale regardless of user's SPT language setting
        _localeCache = localeService.GetLocaleDb("en");
        // Deep-clone before ApplyConfig so the snapshot is not mutated when ApplyConfig
        // modifies the live recipe objects in place (same references otherwise).
        _originalRecipes = hideoutTable.Production.Recipes?
            .ToDictionary(
                r => (string)r.Id,
                r => jsonUtil.Deserialize<HideoutProduction>(jsonUtil.Serialize(r))!)
            ?? [];
        ApplyConfig();
    }

    /// <summary>
    /// Resolves a template ID to a human-readable item name using SPT's locale database.
    /// Falls back to the raw template ID if no translation is found.
    /// </summary>
    public string ResolveItemName(string templateId)
    {
        _localeCache ??= localeService.GetLocaleDb("en");
        // SPT locale keys follow the pattern "{templateId} Name"
        return _localeCache.TryGetValue($"{templateId} Name", out var name) ? name : templateId;
    }

    public List<ItemSearchResult> SearchItems(string query, int maxResults = 15)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return [];
        _localeCache ??= localeService.GetLocaleDb("en");

        return _localeCache
            .Where(kv => kv.Key.EndsWith(" Name") && kv.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .Select(kv => new ItemSearchResult
            {
                TemplateId = kv.Key[..^5], // Remove " Name" suffix
                Name = kv.Value
            })
            .ToList();
    }

    public List<RecipeViewModel> GetAllRecipes()
    {
        var recipes = hideoutTable.Production.Recipes;
        if (recipes is null) return [];

        return recipes.Select(r => ToViewModel(r, _config.Additions.Any(a => IsAddedRecipe(r, a)))).ToList();
    }

    public RecipeViewModel? GetRecipe(string recipeId)
    {
        var recipe = FindRecipe(recipeId);
        if (recipe is null) return null;
        return ToViewModel(recipe, _config.Additions.Any(a => IsAddedRecipe(recipe, a)));
    }

    /// <summary>
    /// Modifies an existing recipe in memory and persists the change to config.json.
    /// Only non-null fields in the modification are applied (partial update).
    /// </summary>
    public void ModifyRecipe(string recipeId, RecipeModification mod)
    {
        var recipe = FindRecipe(recipeId);
        if (recipe is null) return;

        // Apply only the fields that were explicitly set (nullable pattern for partial updates)
        if (mod.ProductionTime.HasValue) recipe.ProductionTime = mod.ProductionTime.Value;
        if (mod.Count.HasValue) recipe.Count = mod.Count.Value;
        if (mod.ProductionLimitCount.HasValue) recipe.ProductionLimitCount = mod.ProductionLimitCount.Value;
        if (mod.Locked.HasValue) recipe.Locked = mod.Locked.Value;
        if (mod.Continuous.HasValue) recipe.Continuous = mod.Continuous.Value;
        if (mod.NeedFuelForAllProductionTime.HasValue) recipe.NeedFuelForAllProductionTime = mod.NeedFuelForAllProductionTime.Value;
        if (mod.IsEncoded.HasValue) recipe.IsEncoded = mod.IsEncoded.Value;
        if (mod.IsCodeProduction.HasValue) recipe.IsCodeProduction = mod.IsCodeProduction.Value;
        if (mod.Requirements is not null) recipe.Requirements = mod.Requirements.Select(ToRequirement).ToList();

        var addition = _config.Additions.FirstOrDefault(a => a.Id == recipeId);
        if (addition is not null)
        {
            if (mod.ProductionTime.HasValue) addition.ProductionTime = mod.ProductionTime.Value;
            if (mod.Count.HasValue) addition.Count = mod.Count.Value;
            if (mod.ProductionLimitCount.HasValue) addition.ProductionLimitCount = mod.ProductionLimitCount.Value;
            if (mod.Locked.HasValue) addition.Locked = mod.Locked.Value;
            if (mod.Continuous.HasValue) addition.Continuous = mod.Continuous.Value;
            if (mod.NeedFuelForAllProductionTime.HasValue) addition.NeedFuelForAllProductionTime = mod.NeedFuelForAllProductionTime.Value;
            if (mod.IsEncoded.HasValue) addition.IsEncoded = mod.IsEncoded.Value;
            if (mod.IsCodeProduction.HasValue) addition.IsCodeProduction = mod.IsCodeProduction.Value;
            if (mod.Requirements is not null) addition.Requirements = mod.Requirements;
        }
        else
        {
            var existing = _config.Modifications.FirstOrDefault(m => m.RecipeId == recipeId);
            if (existing is not null)
                _config.Modifications.Remove(existing);
            mod.RecipeId = recipeId;
            _config.Modifications.Add(mod);
        }

        SaveConfig();
    }

    /// <summary>
    /// Creates a new recipe, injects it into SPT's in-memory table, and persists it.
    /// Returns the generated MongoId so the UI can select the new recipe.
    /// </summary>
    public string AddRecipe(RecipeAddition addition)
    {
        var recipeId = new MongoId();
        addition.Id = (string)recipeId;
        var recipe = new HideoutProduction
        {
            Id = recipeId,
            AreaType = Enum.Parse<HideoutAreas>(addition.AreaType),
            ProductionTime = addition.ProductionTime,
            EndProduct = new MongoId(addition.EndProduct),
            Count = addition.Count,
            Requirements = addition.Requirements.Select(ToRequirement).ToList(),
            Locked = false,
            Continuous = false,
            NeedFuelForAllProductionTime = false,
            IsEncoded = false,
            IsCodeProduction = false
        };

        hideoutTable.Production.Recipes!.Add(recipe);

        _config.Additions.Add(addition);
        SaveConfig();

        logger.Success($"[HCM] Added recipe: {ResolveItemName(addition.EndProduct)} in {addition.AreaType}");
        return recipe.Id;
    }

    /// <summary>
    /// Removes a recipe from memory. If it was user-added, removes from additions.
    /// If it was an original SPT recipe, tracks the ID in removals so it stays
    /// removed on next server start.
    /// </summary>
    public bool RemoveRecipe(string recipeId)
    {
        var recipes = hideoutTable.Production.Recipes;
        if (recipes is null) return false;

        var recipe = recipes.FirstOrDefault(r => ((string)r.Id) == recipeId);
        if (recipe is null) return false;

        recipes.Remove(recipe);

        // If this was a user-added recipe, just remove it from additions.
        // Otherwise, track the original recipe ID in removals for persistence.
        var addedRecipe = _config.Additions.FirstOrDefault(a => a.Id == recipeId);
        if (addedRecipe is not null)
        {
            _config.Additions.Remove(addedRecipe);
        }
        else
        {
            if (!_config.Removals.Contains(recipeId))
                _config.Removals.Add(recipeId);
        }

        var modification = _config.Modifications.FirstOrDefault(m => m.RecipeId == recipeId);
        if (modification is not null)
            _config.Modifications.Remove(modification);

        SaveConfig();
        logger.Success($"[HCM] Removed recipe {recipeId}");
        return true;
    }

    public bool IsModified(string recipeId) =>
        _config.Modifications.Any(m => m.RecipeId == recipeId);

    /// <summary>
    /// Returns ViewModels for all original SPT recipes that the user has deleted,
    /// so the UI can display and restore them.
    /// </summary>
    public List<RecipeViewModel> GetRemovedRecipes()
    {
        return _config.Removals
            .Select(id => _originalRecipes.TryGetValue(id, out var r) ? ToViewModel(r, false) : null)
            .Where(vm => vm is not null)
            .Cast<RecipeViewModel>()
            .ToList();
    }

    /// <summary>
    /// Restores a recipe to its original SPT state.
    /// For deleted recipes: re-adds to the live table and removes from Removals.
    /// For modified recipes: reverts live fields to snapshot values and removes from Modifications.
    /// </summary>
    /// <summary>
    /// Returns true if the recipe has any user-applied changes (modification or removal).
    /// </summary>
    public bool HasUserChanges(string recipeId) =>
        _config.Removals.Contains(recipeId) || _config.Modifications.Any(m => m.RecipeId == recipeId);

    /// <summary>
    /// Restores a recipe to its original SPT values. Returns true if changes were actually
    /// reverted, false if there was nothing to restore (recipe already at original state).
    /// </summary>
    public bool RestoreRecipe(string recipeId)
    {
        if (!_originalRecipes.TryGetValue(recipeId, out var original)) return false;

        var recipes = hideoutTable.Production.Recipes;
        if (recipes is null) return false;

        var didSomething = false;

        // Restore deleted recipe
        if (_config.Removals.Contains(recipeId))
        {
            _config.Removals.Remove(recipeId);
            recipes.Add(original);
            didSomething = true;
        }

        // Revert modifications by copying original field values back onto the live recipe
        var modification = _config.Modifications.FirstOrDefault(m => m.RecipeId == recipeId);
        if (modification is not null)
        {
            _config.Modifications.Remove(modification);
            var live = recipes.FirstOrDefault(r => (string)r.Id == recipeId);
            if (live is not null)
            {
                live.ProductionTime = original.ProductionTime;
                live.Count = original.Count;
                live.ProductionLimitCount = original.ProductionLimitCount;
                live.Locked = original.Locked;
                live.Continuous = original.Continuous;
                live.NeedFuelForAllProductionTime = original.NeedFuelForAllProductionTime;
                live.IsEncoded = original.IsEncoded;
                live.IsCodeProduction = original.IsCodeProduction;
                live.Requirements = original.Requirements;
            }
            didSomething = true;
        }

        if (!didSomething) return false;

        SaveConfig();
        logger.Success($"[HCM] Restored recipe {recipeId}");
        return true;
    }

    /// <summary>
    /// Applies saved config to SPT's in-memory recipe list on startup.
    /// Order matters: removals first, then modifications, then additions.
    /// </summary>
    private void ApplyConfig()
    {
        var recipes = hideoutTable.Production.Recipes;
        if (recipes is null) return;

        var removedCount = 0;
        foreach (var recipeId in _config.Removals)
        {
            var recipe = recipes.FirstOrDefault(r => ((string)r.Id) == recipeId);
            if (recipe is not null)
            {
                recipes.Remove(recipe);
                removedCount++;
            }
        }

        var modifiedCount = 0;
        foreach (var mod in _config.Modifications)
        {
            var recipe = recipes.FirstOrDefault(r => ((string)r.Id) == mod.RecipeId);
            if (recipe is null) continue;

            if (mod.ProductionTime.HasValue) recipe.ProductionTime = mod.ProductionTime.Value;
            if (mod.Count.HasValue) recipe.Count = mod.Count.Value;
            if (mod.ProductionLimitCount.HasValue) recipe.ProductionLimitCount = mod.ProductionLimitCount.Value;
            if (mod.Locked.HasValue) recipe.Locked = mod.Locked.Value;
            if (mod.Continuous.HasValue) recipe.Continuous = mod.Continuous.Value;
            if (mod.NeedFuelForAllProductionTime.HasValue) recipe.NeedFuelForAllProductionTime = mod.NeedFuelForAllProductionTime.Value;
            if (mod.IsEncoded.HasValue) recipe.IsEncoded = mod.IsEncoded.Value;
            if (mod.IsCodeProduction.HasValue) recipe.IsCodeProduction = mod.IsCodeProduction.Value;
            if (mod.Requirements is not null) recipe.Requirements = mod.Requirements.Select(ToRequirement).ToList();

            modifiedCount++;
        }

        var addedCount = 0;
        var needsSave = false;
        foreach (var addition in _config.Additions)
        {
            if (string.IsNullOrEmpty(addition.Id))
            {
                addition.Id = (string)new MongoId();
                needsSave = true;
            }
            var recipe = new HideoutProduction
            {
                Id = new MongoId(addition.Id),
                AreaType = Enum.Parse<HideoutAreas>(addition.AreaType),
                ProductionTime = addition.ProductionTime,
                EndProduct = new MongoId(addition.EndProduct),
                Count = addition.Count,
                ProductionLimitCount = addition.ProductionLimitCount,
                Requirements = addition.Requirements.Select(ToRequirement).ToList(),
                Locked = addition.Locked,
                Continuous = addition.Continuous,
                NeedFuelForAllProductionTime = addition.NeedFuelForAllProductionTime,
                IsEncoded = addition.IsEncoded,
                IsCodeProduction = addition.IsCodeProduction
            };
            recipes.Add(recipe);
            addedCount++;
        }
        if (needsSave) SaveConfig();

        if (removedCount + modifiedCount + addedCount > 0)
            logger.Success($"[HCM] Applied config: {addedCount} added, {modifiedCount} modified, {removedCount} removed");
    }

    /// <summary>
    /// Persists config to disk on a background thread to avoid blocking the UI.
    /// </summary>
    private void SaveConfig()
    {
        var configPath = Path.Combine(_modPath, "config.json");
        var json = jsonUtil.Serialize(_config, true);
        Task.Run(() => File.WriteAllTextAsync(configPath, json));
    }

    private HideoutProduction? FindRecipe(string recipeId)
    {
        return hideoutTable.Production.Recipes?.FirstOrDefault(r => ((string)r.Id) == recipeId);
    }

    private RecipeViewModel ToViewModel(HideoutProduction recipe, bool isCustom)
    {
        var endProductId = (string)recipe.EndProduct;
        var recipeId = (string)recipe.Id;
        return new RecipeViewModel
        {
            Id = recipeId,
            AreaType = recipe.AreaType ?? HideoutAreas.NotSet,
            EndProductId = endProductId,
            EndProductName = ResolveItemName(endProductId),
            ProductionTime = recipe.ProductionTime ?? 0,
            Count = recipe.Count ?? 1,
            ProductionLimitCount = recipe.ProductionLimitCount ?? 0,
            Locked = recipe.Locked ?? false,
            Continuous = recipe.Continuous ?? false,
            NeedFuelForAllProductionTime = recipe.NeedFuelForAllProductionTime ?? false,
            IsEncoded = recipe.IsEncoded ?? false,
            IsCodeProduction = recipe.IsCodeProduction ?? false,
            IsCustom = isCustom,
            IsModified = !isCustom && IsModified(recipeId),
            Requirements = recipe.Requirements?.Select(r => new RequirementViewModel
            {
                Type = r.Type ?? "",
                TemplateId = r.TemplateId,
                ItemName = r.TemplateId is not null ? ResolveItemName(r.TemplateId) : null,
                AreaType = r.AreaType,
                RequiredLevel = r.RequiredLevel,
                Count = r.Count,
                IsFunctional = r.IsFunctional,
                QuestId = r.QuestId,
                Resource = r.Resource
            }).ToList() ?? []
        };
    }

    private static bool IsAddedRecipe(HideoutProduction recipe, RecipeAddition addition)
    {
        // Match on the unique recipe Id, not EndProduct/AreaType — otherwise a custom
        // recipe for an item/area that also has a default recipe would flag both as custom.
        return !string.IsNullOrEmpty(addition.Id) && (string)recipe.Id == addition.Id;
    }

    /// <summary>
    /// Maps a config requirement to SPT's Requirement model.
    /// MongoId requires explicit null casting because its parameterless constructor
    /// generates a new ID instead of representing null.
    /// </summary>
    private static Requirement ToRequirement(RequirementConfig config)
    {
        return new Requirement
        {
            Type = config.Type,
            TemplateId = config.TemplateId is not null ? new MongoId(config.TemplateId) : (MongoId?)null,
            AreaType = config.AreaType,
            RequiredLevel = config.RequiredLevel,
            Count = config.Count,
            IsFunctional = config.IsFunctional ?? false,
            IsEncoded = false,
            QuestId = config.QuestId is not null ? new MongoId(config.QuestId) : (MongoId?)null,
            Resource = config.Resource
        };
    }
}
