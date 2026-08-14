using SPTarkov.Server.Core.Models.Enums.Hideout;

namespace HideoutCraftModifier.Models;

public class RecipeViewModel
{
    public string Id { get; set; } = "";
    public HideoutAreas AreaType { get; set; }
    public string AreaName => AreaType.ToString();
    public string EndProductId { get; set; } = "";
    public string EndProductName { get; set; } = "";
    public double ProductionTime { get; set; }
    public int Count { get; set; } = 1;
    public int ProductionLimitCount { get; set; }
    public bool Locked { get; set; }
    public bool Continuous { get; set; }
    public bool NeedFuelForAllProductionTime { get; set; }
    public bool IsEncoded { get; set; }
    public bool IsCodeProduction { get; set; }
    public List<RequirementViewModel> Requirements { get; set; } = [];
    public bool IsCustom { get; set; }
}

public class RequirementViewModel
{
    public string Type { get; set; } = "";
    public string? TemplateId { get; set; }
    public string? ItemName { get; set; }
    public int? AreaType { get; set; }
    public string? AreaName => AreaType.HasValue ? ((HideoutAreas)AreaType.Value).ToString() : null;
    public int? RequiredLevel { get; set; }
    public int? Count { get; set; }
    public bool? IsFunctional { get; set; }
    public string? QuestId { get; set; }
    public int? Resource { get; set; }
}
