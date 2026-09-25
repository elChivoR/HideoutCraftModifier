using HideoutCraftModifier.Patches;
using HideoutCraftModifier.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;

namespace HideoutCraftModifier;

/// <summary>
/// Entry point for the HCM mod. Runs after SPT has loaded all game tables (PostLoad + 1)
/// so that hideout recipes are available for modification.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class HcmPlugin(ISptLogger<HcmPlugin> logger, RecipeService recipeService, ItemHelper itemHelper) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        recipeService.Initialize();
        EnableAmmoBoxPatch();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Makes crafts that output an ammo box hand it out full instead of empty.
    /// A failure here only loses that fix, so it must not stop the mod from loading.
    /// </summary>
    private void EnableAmmoBoxPatch()
    {
        const string failure = "[HCM] Could not patch ammo box craft rewards, crafted ammo boxes will stay empty";
        try
        {
            if (!AmmoBoxCraftRewardPatch.Apply(itemHelper, logger))
                logger.Warning(failure);
        }
        catch (Exception ex)
        {
            logger.Warning($"{failure}: {ex.Message}");
        }
    }
}
