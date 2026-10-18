namespace LumenRogue;

public sealed partial class Game
{
    private static readonly string[] LandmarkLore =
    [
        "Broken Watchtower|Its signal bowl points toward Valeward, not Emberfall. The old wardens watched the castle.",
        "Whispering Stones|The stones repeat promises spoken nearby. Three village oaths are cut into their buried faces.",
        "Wayfarer's Camp|Fresh ash beneath old cooking stones proves the March is travelled even when its roads look empty.",
        "Hollowroot Mine|The roots here grew around iron tools, as though the earth learned to hold a weapon.",
        "Sunken Barrow|Water entered the tomb after the floodgates were built. Someone drowned it deliberately.",
        "Moonwell Cavern|The well reflects stars at noon and an empty sky at midnight.",
        "Castle Valeward|Every village road converges on Valeward, but its gate bears three different masons' marks."
    ];

    public IReadOnlyList<CraftingRecipe> CraftingRecipes => BuildCraftingRecipes();

    public IReadOnlyList<(string Name, Map Map, Point Position)> FastTravelOptions => AllMaps()
        .Where(map => map.IsOutdoor)
        .SelectMany(map => map.Landmarks.Select(entry => (entry.Value, map, entry.Key)))
        .Where(option => DiscoveredLandmarks.Contains(option.Value))
        .DistinctBy(option => option.Value)
        .OrderBy(option => option.Value)
        .ToList();

    private void InitializeNpcSchedules()
    {
        foreach (var map in AllMaps())
        foreach (var npc in map.Npcs)
        {
            npc.WorkPosition = npc.Position;
            var stableOffset = npc.Id.Aggregate(17, (value, character) => unchecked(value * 31 + character));
            npc.HomePosition = FindSchedulePoint(map, npc.Position, new Point(Math.Abs(stableOffset % 5) - 2, 4));
            npc.EveningPosition = FindSchedulePoint(map, npc.Position, new Point(2, 1));
        }
        UpdateNpcSchedules();
    }

    private static Point FindSchedulePoint(Map map, Point origin, Point offset)
    {
        var preferred = origin + offset;
        for (var radius = 0; radius <= 5; radius++)
        for (var y = -radius; y <= radius; y++)
        for (var x = -radius; x <= radius; x++)
        {
            var point = preferred + new Point(x, y);
            if (map.Contains(point) && map[point].Walkable && !map.TravelPoints.ContainsKey(point)) return point;
        }
        return origin;
    }

    private void UpdateNpcSchedules()
    {
        var schedule = WorldMinutes switch
        {
            < 420 => "sleeping",
            < 1080 => "working",
            < 1260 => "at supper",
            _ => "sleeping"
        };
        foreach (var map in AllMaps())
        foreach (var npc in map.Npcs)
        {
            npc.ScheduleState = schedule;
            var destination = schedule switch
            {
                "working" => npc.WorkPosition,
                "at supper" => npc.EveningPosition,
                _ => npc.HomePosition
            } ?? npc.Position;
            if (map[destination].Walkable && !map.Npcs.Any(other => other != npc && other.Position == destination))
                npc.Position = destination;
        }
    }

    public bool IsShopOpen(Shop shop) => WorldMinutes is >= 420 and < 1080;

    private void DiscoverNearbyLandmarks()
    {
        foreach (var landmark in CurrentMap.Landmarks.Where(entry => entry.Key.DistanceSquared(Player.Position) <= 25))
        {
            if (!DiscoveredLandmarks.Add(landmark.Value)) continue;
            RecordQuestProgress(QuestObjectiveKind.DiscoverLandmark);
            var lore = LandmarkLore.FirstOrDefault(entry => entry.StartsWith(landmark.Value + "|", StringComparison.Ordinal));
            if (lore is not null)
            {
                var text = lore[(lore.IndexOf('|') + 1)..];
                LoreEntries.Add($"{landmark.Value}: {text}");
            }
            Experience += 3;
            AddMessage($"LANDMARK DISCOVERED: {landmark.Value}. +3 XP{(lore is null ? "" : " · lore added (F4)")}");
        }
    }

    private void OpenFastTravel()
    {
        DiscoverNearbyLandmarks();
        if (!CurrentMap.IsOutdoor) { AddMessage("Fast travel is only possible beneath the open sky."); return; }
        if (Monsters.Any(monster => monster.IsAlive && monster.AlertTurns > 0 && monster.Position.DistanceSquared(Player.Position) < 225))
        { AddMessage("Hostile pursuit makes the road unsafe."); return; }
        if (FastTravelOptions.Count == 0) { AddMessage("Discover a road or settlement before travelling by landmark."); return; }
        Mode = GameMode.FastTravel;
        MenuSelection = 0;
    }

    private void HandleFastTravelKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.F2 or ConsoleKey.Q) { Mode = GameMode.Playing; return; }
        var options = FastTravelOptions;
        if (options.Count == 0) { Mode = GameMode.Playing; return; }
        if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K)
        { MenuSelection = (MenuSelection - 1 + options.Count) % options.Count; return; }
        if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J)
        { MenuSelection = (MenuSelection + 1) % options.Count; return; }
        if (key.Key is not ConsoleKey.Enter and not ConsoleKey.Spacebar) return;
        var destination = options[Math.Clamp(MenuSelection, 0, options.Count - 1)];
        CurrentMap = destination.Map;
        Player.Position = destination.Position;
        Food = Math.Max(0, Food - 3);
        Water = Math.Max(0, Water - 5);
        _turns += 6;
        Mode = GameMode.Playing;
        NoiseLevel = 0;
        UpdateNpcSchedules();
        BeginArrivalFlourish();
        AddMessage($"You follow known roads to {destination.Name}. One hour and provisions pass.");
    }

    private void OpenCrafting()
    {
        var settlement = CurrentMap.IsOutdoor && (CurrentMap.IsTown || CurrentMap.Npcs.Any(npc => npc.Position.DistanceSquared(Player.Position) <= 225));
        if (!settlement) { AddMessage("Crafting needs a settlement workbench; return to a town, village, or castle courtyard."); return; }
        Mode = GameMode.Crafting;
        MenuSelection = 0;
    }

    private void HandleCraftingKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.F3 or ConsoleKey.Q) { Mode = GameMode.Playing; return; }
        var recipes = CraftingRecipes;
        if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K)
        { MenuSelection = (MenuSelection - 1 + recipes.Count) % recipes.Count; return; }
        if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J)
        { MenuSelection = (MenuSelection + 1) % recipes.Count; return; }
        if (key.Key is not ConsoleKey.Enter and not ConsoleKey.Spacebar) return;
        Craft(recipes[Math.Clamp(MenuSelection, 0, recipes.Count - 1)]);
    }

    private void Craft(CraftingRecipe recipe)
    {
        if (Player.Gold < recipe.GoldCost || recipe.Ingredients.Any(ingredient => Inventory.Where(item => item.Kind == ingredient.Kind).Sum(item => item.Quantity) < ingredient.Quantity))
        { AddMessage("You lack the listed materials or workshop fee."); return; }
        Player.Gold -= recipe.GoldCost;
        foreach (var ingredient in recipe.Ingredients) RemoveItems(ingredient.Kind, ingredient.Quantity);
        AddInventoryItem(recipe.Result.Copy(recipe.Result.Quantity), false);
        _turns++;
        AddMessage($"You craft {recipe.Result.Name}. The materials now have a purpose.");
    }

    private void RemoveItems(ItemKind kind, int quantity)
    {
        foreach (var item in Inventory.Where(item => item.Kind == kind).ToList())
        {
            var taken = Math.Min(quantity, item.Quantity);
            item.Quantity -= taken;
            quantity -= taken;
            if (item.Quantity <= 0) Inventory.Remove(item);
            if (quantity <= 0) break;
        }
    }

    private static List<CraftingRecipe> BuildCraftingRecipes() =>
    [
        new() { Name = "field medicine", Description = "A reliable healing draught.", Ingredients = [new(ItemKind.BeastHide, 1), new(ItemKind.VenomGland, 1)], Result = WorldBuilder.MakeItem(ItemKind.GreaterHealingPotion), GoldCost = 4 },
        new() { Name = "March bomb", Description = "Scrap-cased explosive for stone and crowds.", Ingredients = [new(ItemKind.GoblinScrap, 2)], Result = WorldBuilder.MakeItem(ItemKind.Bomb), GoldCost = 5 },
        new() { Name = "relic ward", Description = "An uncommon off-hand charm against the deep.", Ingredients = [new(ItemKind.RelicShard, 3)], Result = EquipmentFactory.Create("relic ward", EquipmentSlot.OffHand, ']', defense: 3, spell: 1, rarity: ItemRarity.Uncommon, description: "Old silver reset around a March waystone"), GoldCost = 18 },
        new() { Name = "wayfarer's kit", Description = "Two rations prepared from hide and salvage.", Ingredients = [new(ItemKind.BeastHide, 1), new(ItemKind.GoblinScrap, 1)], Result = new ItemStack { Kind = ItemKind.Ration, Name = "trail ration", Description = "Food packed for a long road", Glyph = '%', Color = new Rgb(214, 168, 82), Quantity = 2 }, GoldCost = 2 },
        new() { Name = "oathbound lantern", Description = "A rare amulet carrying a boss relic's light.", Ingredients = [new(ItemKind.BossRelic, 1), new(ItemKind.RelicShard, 4)], Result = EquipmentFactory.Create("oathbound lantern", EquipmentSlot.Amulet, '¤', defense: 2, spell: 3, light: 1.5, rarity: ItemRarity.Epic, description: "A victory made useful instead of ornamental"), GoldCost = 40 }
    ];

    private void OpenLore()
    {
        Mode = GameMode.Lore;
        MenuSelection = 0;
    }

    private void HandleLoreKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.F4 or ConsoleKey.Q) Mode = GameMode.Playing;
    }

    private void CycleReaction()
    {
        PreparedReaction = PreparedReaction switch
        {
            null => PlayerReactionKind.Block,
            PlayerReactionKind.Block => PlayerReactionKind.Dodge,
            PlayerReactionKind.Dodge => PlayerReactionKind.Ward,
            _ => null
        };
        AddMessage(PreparedReaction switch
        {
            PlayerReactionKind.Block => "Reaction readied: BLOCK reduces the next physical hit.",
            PlayerReactionKind.Dodge => "Reaction readied: DODGE may avoid the next attack entirely.",
            PlayerReactionKind.Ward => "Reaction readied: WARD halves the next ranged or magical hit.",
            _ => "Reaction stance cleared."
        });
    }

    private int ApplyPreparedReaction(int damage, bool ranged)
    {
        var reaction = PreparedReaction;
        if (reaction == PlayerReactionKind.Dodge && _random.Next(100) < Math.Clamp(35 + Dexterity, 45, 75))
        { PreparedReaction = null; AddMessage("You read the intent and dodge cleanly."); return 0; }
        if (reaction == PlayerReactionKind.Dodge) PreparedReaction = null;
        if (reaction == PlayerReactionKind.Block && !ranged)
        {
            PreparedReaction = null;
            var reduced = Math.Min(damage, 2 + TotalDefense / 3);
            AddMessage($"Your block absorbs {reduced} damage.");
            return damage - reduced;
        }
        if (reaction == PlayerReactionKind.Ward && ranged)
        {
            PreparedReaction = null;
            AddMessage("Your ward folds the witch-light away.");
            return damage / 2;
        }
        return damage;
    }
}
