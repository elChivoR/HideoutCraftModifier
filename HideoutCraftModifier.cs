using HideoutCraftModifier.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace HideoutCraftModifier;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class HcmPlugin(RecipeService recipeService) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        recipeService.Initialize();
        return Task.CompletedTask;
    }
}
