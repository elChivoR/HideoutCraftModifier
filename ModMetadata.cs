using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;

namespace HideoutCraftModifier;

/// <summary>
/// Mod identity and web UI registration. IModBlazorMetadata tells SPT's built-in
/// Blazor server to mount this mod's pages and static assets (wwwroot).
/// HomePage = "/hcm" adds a link in SPT's mod dashboard.
/// </summary>
public record ModMetadata : IModMetadata, IModBlazorMetadata
{
    public string ModGuid { get; init; } = "com.elchivor.hideoutcraftmodifier";
    public string Name { get; init; } = "HideoutCraftModifier";
    public string Author { get; init; } = "elchivor";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.1.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;

    public string? WWWRootUrl { get; init; }
    public string? HomePage { get; init; } = "/hcm";
    public string? HomePageDescription { get; init; } = "Modify hideout crafting recipes: add, edit, or remove productions.";
}
