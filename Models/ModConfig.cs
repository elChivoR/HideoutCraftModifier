using System.Text.Json.Serialization;

namespace HideoutCraftModifier.Models;

public record ModConfig
{
    [JsonPropertyName("modifications")]
    public List<RecipeModification> Modifications { get; set; } = [];

    [JsonPropertyName("additions")]
    public List<RecipeAddition> Additions { get; set; } = [];

    [JsonPropertyName("removals")]
    public List<string> Removals { get; set; } = [];
}

public record RecipeModification
{
    [JsonPropertyName("recipeId")]
    public string RecipeId { get; set; } = "";

    [JsonPropertyName("productionTime")]
    public double? ProductionTime { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("productionLimitCount")]
    public int? ProductionLimitCount { get; set; }

    [JsonPropertyName("locked")]
    public bool? Locked { get; set; }

    [JsonPropertyName("continuous")]
    public bool? Continuous { get; set; }

    [JsonPropertyName("needFuelForAllProductionTime")]
    public bool? NeedFuelForAllProductionTime { get; set; }

    [JsonPropertyName("isEncoded")]
    public bool? IsEncoded { get; set; }

    [JsonPropertyName("isCodeProduction")]
    public bool? IsCodeProduction { get; set; }

    [JsonPropertyName("requirements")]
    public List<RequirementConfig>? Requirements { get; set; }
}

public record RecipeAddition
{
    [JsonPropertyName("areaType")]
    public string AreaType { get; set; } = "";

    [JsonPropertyName("endProduct")]
    public string EndProduct { get; set; } = "";

    [JsonPropertyName("productionTime")]
    public double ProductionTime { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;

    [JsonPropertyName("requirements")]
    public List<RequirementConfig> Requirements { get; set; } = [];
}

public record RequirementConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    [JsonPropertyName("areaType")]
    public int? AreaType { get; set; }

    [JsonPropertyName("requiredLevel")]
    public int? RequiredLevel { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("isFunctional")]
    public bool? IsFunctional { get; set; }

    [JsonPropertyName("questId")]
    public string? QuestId { get; set; }

    [JsonPropertyName("resource")]
    public int? Resource { get; set; }
}
