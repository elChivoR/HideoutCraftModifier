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
    private Dictionary<string, string>? _localeCache;

    public ModConfig Config => _config;

    public void Initialize()
    {
        _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _config = modHelper.GetJsonDataFromFile<ModConfig>(_modPath, "config.json") ?? new ModConfig();
        _localeCache = localeService.GetLocaleDb("en");
        ApplyConfig();
    }

    public string ResolveItemName(string templateId)
    {
        _localeCache ??= localeService.GetLocaleDb("en");
        return _localeCache.TryGetValue($"{templateId} Name", out var name) ? name : templateId;
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

    public void ModifyRecipe(string recipeId, RecipeModification mod)
    {
        var recipe = FindRecipe(recipeId);
        if (recipe is null) return;

        if (mod.ProductionTime.HasValue) recipe.ProductionTime = mod.ProductionTime.Value;
        if (mod.Count.HasValue) recipe.Count = mod.Count.Value;
        if (mod.ProductionLimitCount.HasValue) recipe.ProductionLimitCount = mod.ProductionLimitCount.Value;
        if (mod.Locked.HasValue) recipe.Locked = mod.Locked.Value;
        if (mod.Continuous.HasValue) recipe.Continuous = mod.Continuous.Value;
        if (mod.NeedFuelForAllProductionTime.HasValue) recipe.NeedFuelForAllProductionTime = mod.NeedFuelForAllProductionTime.Value;
        if (mod.IsEncoded.HasValue) recipe.IsEncoded = mod.IsEncoded.Value;
        if (mod.IsCodeProduction.HasValue) recipe.IsCodeProduction = mod.IsCodeProduction.Value;
        if (mod.Requirements is not null) recipe.Requirements = mod.Requirements.Select(ToRequirement).ToList();

        var existing = _config.Modifications.FirstOrDefault(m => m.RecipeId == recipeId);
        if (existing is not null)
            _config.Modifications.Remove(existing);

        mod.RecipeId = recipeId;
        _config.Modifications.Add(mod);

        SaveConfig();
    }

    public string AddRecipe(RecipeAddition addition)
    {
        var recipe = new HideoutProduction
        {
            Id = new MongoId(),
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

    public bool RemoveRecipe(string recipeId)
    {
        var recipes = hideoutTable.Production.Recipes;
        if (recipes is null) return false;

        var recipe = recipes.FirstOrDefault(r => ((string)r.Id) == recipeId);
        if (recipe is null) return false;

        var endProductId = (string)recipe.EndProduct;
        recipes.Remove(recipe);

        var addedRecipe = _config.Additions.FirstOrDefault(a => a.EndProduct == endProductId);
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
        foreach (var addition in _config.Additions)
        {
            var recipe = new HideoutProduction
            {
                Id = new MongoId(),
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
            recipes.Add(recipe);
            addedCount++;
        }

        if (removedCount + modifiedCount + addedCount > 0)
            logger.Success($"[HCM] Applied config: {addedCount} added, {modifiedCount} modified, {removedCount} removed");
    }

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
        return new RecipeViewModel
        {
            Id = recipe.Id,
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
        return (string)recipe.EndProduct == addition.EndProduct
               && recipe.AreaType?.ToString() == addition.AreaType;
    }

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
