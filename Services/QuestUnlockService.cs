using HideoutCraftModifier.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Commerce;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils.Json;

namespace HideoutCraftModifier.Services;

/// <summary>
/// Keeps quest "ProductionScheme" rewards in sync with recipes that have a QuestComplete
/// requirement. SPT only unlocks a locked recipe when a quest reward of that type resolves
/// to it (on quest completion, and retroactively for finished quests on game start via
/// ProfileFixerService), so a requirement alone never unlocks anything. The same reward is
/// what the client renders as the "unlocks craft" icon in the quest's reward list.
/// Rewards are injected in memory only and rebuilt from scratch on every sync.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class QuestUnlockService(
    ISptLogger<QuestUnlockService> logger,
    TemplateTable templateTable,
    HideoutTable hideoutTable,
    RewardHelper rewardHelper,
    LocaleService localeService)
{
    // Rewards this mod added, so they can be taken out again before each resync.
    private readonly List<(List<Reward> List, Reward Reward)> _injected = [];
    // recipeId -> reason its quest unlock can't work. Surfaced in the UI.
    private Dictionary<string, string> _problems = [];
    private Dictionary<string, string>? _localeCache;

    public string? GetProblem(string recipeId) => _problems.GetValueOrDefault(recipeId);

    public bool QuestExists(string? questId) =>
        questId is { Length: 24 } && templateTable.Quests.ContainsKey(new MongoId(questId));

    public string? ResolveQuestName(string? questId)
    {
        if (!QuestExists(questId)) return null;
        _localeCache ??= localeService.GetLocaleDb("en");
        return _localeCache.TryGetValue($"{questId} name", out var name)
            ? name
            : templateTable.Quests[new MongoId(questId!)].QuestName ?? questId;
    }

    public List<QuestSearchResult> SearchQuests(string query, int maxResults = 15)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return [];
        _localeCache ??= localeService.GetLocaleDb("en");

        return templateTable.Quests.Keys
            .Select(id => (string)id)
            .Select(id => new QuestSearchResult { QuestId = id, Name = ResolveQuestName(id) ?? id })
            .Where(q => q.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || q.QuestId == query)
            .OrderBy(q => q.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Removes previously injected rewards, then adds one ProductionScheme reward for every
    /// quest-locked recipe that no existing reward of its quest already unlocks.
    /// </summary>
    public void Sync()
    {
        foreach (var (list, reward) in _injected)
            list.Remove(reward);
        _injected.Clear();

        var problems = new Dictionary<string, string>();
        var recipes = hideoutTable.Production.Recipes ?? [];

        var byQuest = recipes
            .SelectMany(r => (r.Requirements ?? [])
                .Where(req => req.Type == "QuestComplete" && req.QuestId.HasValue)
                .Select(req => (QuestId: req.QuestId!.Value, Recipe: r)))
            .GroupBy(x => x.QuestId, x => x.Recipe);

        var injectedCount = 0;
        foreach (var group in byQuest)
        {
            var questId = group.Key;
            if (!templateTable.Quests.TryGetValue(questId, out var quest))
            {
                foreach (var recipe in group)
                    problems[recipe.Id] = $"Quest {questId} does not exist.";
                continue;
            }

            var rewards = quest.Rewards ??= new Dictionary<string, List<Reward>>();
            if (!rewards.TryGetValue("Success", out var successRewards) || successRewards is null)
                rewards["Success"] = successRewards = [];

            // A recipe is already covered when one of the quest's own rewards resolves to it
            // and shows its end product (otherwise the quest reward icon would be misleading).
            var covered = new HashSet<MongoId>();
            foreach (var reward in rewards.Values.Where(l => l is not null).SelectMany(l => l)
                         .Where(rw => rw.Type == RewardType.ProductionScheme))
            {
                var match = Match(reward, questId);
                if (match.Count == 1 && match[0].EndProduct == reward.Items?.FirstOrDefault()?.Template)
                    covered.Add(match[0].Id);
                else if (match.Count > 1)
                    foreach (var recipe in match.Where(m => group.Contains(m)))
                        problems[recipe.Id] = $"Another unlock reward of \"{ResolveQuestName(questId)}\" became ambiguous. " +
                                              "Use a different end product, station or station level than the other recipes unlocked by this quest.";
            }

            foreach (var recipe in group.Where(r => !covered.Contains(r.Id) && r.AreaType.HasValue))
            {
                var reward = BuildReward(recipe);
                successRewards.Add(reward);

                var match = Match(reward, questId);
                if (match.Count == 1 && match[0].Id == recipe.Id)
                {
                    _injected.Add((successRewards, reward));
                    injectedCount++;
                }
                else
                {
                    successRewards.Remove(reward);
                    problems[recipe.Id] = $"SPT can't tell this recipe apart from the other recipes unlocked by \"{ResolveQuestName(questId)}\". " +
                                          "Make sure it is Locked and differs in end product, station or station level.";
                }
            }
        }

        foreach (var (recipeId, message) in problems.Where(p => !_problems.ContainsKey(p.Key)))
            logger.Warning($"[HCM] Quest unlock for recipe {recipeId} won't work: {message}");
        _problems = problems;

        logger.Debug($"[HCM] Quest unlock sync: {injectedCount} reward(s) injected, {problems.Count} problem(s)");
    }

    private List<HideoutProduction> Match(Reward reward, MongoId questId)
    {
        try
        {
            return rewardHelper.GetRewardProductionMatch(reward, questId);
        }
        catch (Exception ex)
        {
            logger.Debug($"[HCM] Reward match failed for quest {questId}: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Mirrors the shape of vanilla ProductionScheme rewards: "traderId" holds the hideout
    /// area, "loyaltyLevel" the required station level, and the item is only for display.
    /// </summary>
    private static Reward BuildReward(HideoutProduction recipe)
    {
        var areaLevel = recipe.Requirements?
            .FirstOrDefault(req => req.Type == "Area" && req.AreaType == (int?)recipe.AreaType)?.RequiredLevel
            ?? recipe.Requirements?.FirstOrDefault(req => req.Type == "Area")?.RequiredLevel
            ?? 1;
        var itemId = new MongoId();

        return new Reward
        {
            Id = new MongoId(),
            Type = RewardType.ProductionScheme,
            TraderId = new StringOrInt(null, (int)recipe.AreaType!.Value),
            LoyaltyLevel = areaLevel,
            Target = itemId.ToString(),
            Items =
            [
                new Item
                {
                    Id = itemId,
                    Template = recipe.EndProduct,
                    Upd = new Upd { StackObjectsCount = recipe.Count ?? 1 }
                }
            ],
            GameMode = ["regular", "pve"],
            AvailableInGameEditions = [],
            IsHidden = false,
            Unknown = false
        };
    }
}
