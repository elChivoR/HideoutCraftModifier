using HideoutCraftModifier.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace HideoutCraftModifier;

/// <summary>
/// Entry point for the HCM mod. Runs after SPT has loaded all game tables (PostLoad + 1)
/// so that hideout recipes are available for modification.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class HcmPlugin(RecipeService recipeService) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        recipeService.Initialize();
        return Task.CompletedTask;
    }
}
