using System.Text.Json.Serialization;

namespace HideoutCraftModifier.Models;

/// <summary>
/// Root config persisted to config.json. Tracks three types of user changes:
/// modifications (edits to existing recipes), additions (new recipes),
/// and removals (IDs of deleted original recipes).
/// </summary>
public record ModConfig
{
    [JsonPropertyName("modifications")]
    public List<RecipeModification> Modifications { get; set; } = [];

    [JsonPropertyName("additions")]
    public List<RecipeAddition> Additions { get; set; } = [];

    [JsonPropertyName("removals")]
    public List<string> Removals { get; set; } = [];
}

/// <summary>
/// Partial update for an existing recipe. Nullable fields mean "don't change".
/// </summary>
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
    [JsonPropertyName("id")]
    public string? Id { get; set; }

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

/// <summary>
/// Represents a single requirement for a recipe. The Type field determines
/// which other fields are relevant:
///   - "Item": TemplateId + Count
///   - "Tool": TemplateId + IsFunctional
///   - "Area": AreaType (int enum) + RequiredLevel
///   - "QuestComplete": QuestId
///   - "Resource": TemplateId + Resource (amount)
/// </summary>
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
