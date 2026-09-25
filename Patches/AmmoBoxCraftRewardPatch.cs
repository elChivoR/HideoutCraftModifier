using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Reflection.Patching;

namespace HideoutCraftModifier.Patches;

/// <summary>
/// SPT builds a non-stackable craft reward as a bare item with no children, so a recipe
/// whose end product is an ammo box hands out an empty box. Everywhere else SPT creates
/// ammo boxes (traders, flea, loot, mail, Scav Case, Cultist Circle) it fills them with
/// ItemHelper.AddCartridgesToAmmoBox; this postfix does the same for hideout crafts.
/// </summary>
public class AmmoBoxCraftRewardPatch : AbstractPatch
{
    // Harmony patch methods are static, so the DI singletons are handed over in Apply
    // rather than injected.
    private static ItemHelper? ItemHelper;
    private static ISptLogger<HcmPlugin>? Logger;

    /// <summary>
    /// Enables the patch. Returns false if Harmony did not apply it.
    /// Enable() must be called from this assembly: AbstractPatch silently ignores
    /// callers from any other assembly than the one that constructed the patch.
    /// </summary>
    public static bool Apply(ItemHelper itemHelper, ISptLogger<HcmPlugin> logger)
    {
        ItemHelper = itemHelper;
        Logger = logger;
        var patch = new AmmoBoxCraftRewardPatch();
        patch.Enable();
        return patch.IsActive;
    }

    protected override MethodBase? GetTargetMethod() =>
        AccessTools.Method(typeof(HideoutController), "UnstackRewardIntoValidSize");

    /// <summary>
    /// Runs after the reward list is built (including the clones made for Count > 1).
    /// Parameter names must match the original method's for Harmony to bind them.
    /// </summary>
    [PatchPostfix]
    public static void Postfix(HideoutProduction recipe, List<List<Item>> itemAndChildrenToSendToPlayer)
    {
        var itemHelper = ItemHelper;
        if (itemHelper is null) return;

        try
        {
            if (!itemHelper.IsOfBaseclass(recipe.EndProduct, BaseClasses.AMMO_BOX)) return;
            var (found, template) = itemHelper.GetItem(recipe.EndProduct);
            if (!found || template is null) return;

            // AddCartridgesToAmmoBox skips a box that already holds its cartridges
            foreach (var box in itemAndChildrenToSendToPlayer)
                itemHelper.AddCartridgesToAmmoBox(box, template);
        }
        catch (Exception ex)
        {
            // A box template without a usable cartridge slot: hand it out empty as SPT would,
            // rather than failing the whole take-production request.
            Logger?.Warning($"[HCM] Could not fill crafted ammo box {recipe.EndProduct}: {ex.Message}");
        }
    }
}
