namespace LumenRogue;

public static class WorldBuilder
{
    public static readonly Tile Grass = new('.', new Rgb(70, 122, 60), new Rgb(5, 12, 7), true, true, "grass");
    public static readonly Tile Road = new('·', new Rgb(130, 119, 92), new Rgb(12, 10, 8), true, true, "road");
    public static readonly Tile Tree = new('♣', new Rgb(38, 112, 46), new Rgb(3, 10, 5), false, false, "tree");
    public static readonly Tile Wall = new('█', new Rgb(105, 109, 118), new Rgb(9, 10, 14), false, false, "stone wall");
    public static readonly Tile DungeonWall = new('▓', new Rgb(99, 104, 116), new Rgb(7, 8, 12), false, false, "fractured vault wall", true, 18, 0.22);
    public static readonly Tile CrackedWall = new('▒', new Rgb(125, 112, 105), new Rgb(10, 8, 9), false, false, "cracked wall", true, 8, 0.25);
    public static readonly Tile Rubble = new('░', new Rgb(104, 95, 88), new Rgb(8, 7, 8), true, true, "rubble", false, 0, 0.18);
    public static readonly Tile Floor = new('.', new Rgb(116, 113, 104), new Rgb(8, 8, 10), true, true, "stone floor");
    public static readonly Tile WoodFloor = new('·', new Rgb(138, 98, 61), new Rgb(19, 10, 5), true, true, "wooden floor");
    public static readonly Tile Water = new('≈', new Rgb(45, 110, 170), new Rgb(2, 8, 18), false, true, "water");
    public static readonly Tile ShallowWater = new('~', new Rgb(58, 129, 184), new Rgb(3, 12, 22), true, true, "shallow water", false, 0, 0.3);
    public static readonly Tile CrystalFloor = new('·', new Rgb(92, 105, 155), new Rgb(7, 9, 19), true, true, "crystal-veined floor", false, 0, 0.35);
    public static readonly Tile Moss = new(';', new Rgb(74, 137, 78), new Rgb(5, 12, 7), true, true, "luminous moss", false, 0, 0.15);
    public static readonly Tile Down = new('>', new Rgb(225, 210, 158), new Rgb(15, 13, 9), true, true, "stairs down");
    public static readonly Tile Up = new('<', new Rgb(225, 210, 158), new Rgb(15, 13, 9), true, true, "stairs up");
    public static readonly Tile AshFloor = new('.', new Rgb(132, 112, 98), new Rgb(13, 9, 8), true, true, "ash-covered flagstone", false, 0, 0.12);
    public static readonly Tile DrownedFloor = new('~', new Rgb(67, 126, 151), new Rgb(3, 12, 18), true, true, "flooded reliquary floor", false, 0, 0.32);
    public static readonly Tile PrismFloor = new('·', new Rgb(116, 125, 190), new Rgb(7, 8, 22), true, true, "prismatic foundry glass", false, 0, 0.58);
    public static readonly Tile LightlessFloor = new(',', new Rgb(74, 70, 91), new Rgb(3, 3, 7), true, true, "lightless court stone", false, 0, 0.06);
    public static readonly Tile CinderFloor = new(';', new Rgb(154, 83, 48), new Rgb(18, 5, 2), true, true, "hot cinderstone", false, 0, 0.2);
    public static readonly Tile DrownedWall = new('▓', new Rgb(66, 99, 119), new Rgb(3, 8, 13), false, false, "waterlogged reliquary wall", true, 22, 0.3);
    public static readonly Tile PrismWall = new('█', new Rgb(105, 122, 185), new Rgb(5, 7, 19), false, false, "prismatic foundry wall", true, 26, 0.72);
    public static readonly Tile LightlessWall = new('▓', new Rgb(68, 63, 82), new Rgb(2, 2, 5), false, false, "lightless court wall", true, 24, 0.05);
    public static readonly Tile CinderWall = new('█', new Rgb(145, 66, 42), new Rgb(15, 4, 2), false, false, "furnace basalt", true, 30, 0.18);
    public static readonly Tile ClosedDoor = new('+', new Rgb(154, 108, 66), new Rgb(14, 7, 3), true, false, "closed door");
    public static readonly Tile LockedDoor = new('╬', new Rgb(184, 128, 65), new Rgb(14, 7, 3), true, false, "locked door");
    public static readonly Tile OpenDoor = new('/', new Rgb(132, 94, 62), new Rgb(8, 6, 5), true, true, "open door");
    public static readonly Tile Brazier = new('♨', new Rgb(255, 126, 30), new Rgb(25, 5, 1), true, true, "ancient brazier");
    public static readonly Tile Floodgate = new('╫', new Rgb(95, 155, 185), new Rgb(4, 10, 15), true, true, "floodgate wheel");
    public static readonly Tile PrismMachine = new('◊', new Rgb(115, 225, 255), new Rgb(5, 8, 24), true, true, "light prism");
    public static readonly Tile Generator = new('⚙', new Rgb(210, 185, 90), new Rgb(15, 10, 2), true, true, "vault generator");
    public static readonly Tile MountedWallTorch = new('╫', new Rgb(255, 126, 28), new Rgb(24, 8, 1), false, false, "mounted wall torch", true, 14, 0.2);
    public static readonly Tile FoodSupply = new('%', new Rgb(214, 168, 82), new Rgb(18, 11, 3), true, true, "provision cache");
    public static readonly Tile WaterSupply = new('≋', new Rgb(80, 188, 245), new Rgb(2, 12, 22), true, true, "fresh water source", false, 0, 0.42);
    public static readonly Tile EmptySupply = new('⌑', new Rgb(88, 82, 72), new Rgb(7, 6, 5), true, true, "empty supply cache");
    public static readonly Tile TallGrass = new(',', new Rgb(91, 137, 66), new Rgb(5, 13, 6), true, true, "wind-bent grass");
    public static readonly Tile Heather = new(';', new Rgb(128, 105, 126), new Rgb(9, 7, 12), true, true, "purple heather");
    public static readonly Tile WildTree = new('♠', new Rgb(42, 103, 48), new Rgb(3, 9, 5), false, false, "ashpine tree");
    public static readonly Tile Boulder = new('▲', new Rgb(102, 105, 98), new Rgb(8, 9, 8), false, true, "lichen-covered boulder");
    public static readonly Tile Bridge = new('═', new Rgb(151, 108, 64), new Rgb(14, 8, 4), true, true, "timber road bridge");
    public static readonly Tile RegionGate = new('=', new Rgb(220, 174, 92), new Rgb(18, 11, 4), true, true, "road beyond Emberfall");
    public static readonly Tile RuinWall = new('▓', new Rgb(104, 106, 96), new Rgb(7, 8, 7), false, false, "ivy-bound ruin wall", true, 16, 0.13);
    public static readonly Tile RuinFloor = new('·', new Rgb(102, 102, 86), new Rgb(7, 8, 6), true, true, "mossy ruin floor", false, 0, 0.12);

    public static Map BuildTown(int seed = 94117)
    {
        var random = new Random(seed);
        var map = new Map(100, 48, "Emberfall", true, Grass);
        map.Start = new Point(50, 23);

        for (var x = 0; x < map.Width; x++)
        {
            map.Set(new Point(x, 0), Tree);
            map.Set(new Point(x, map.Height - 1), Tree);
        }
        for (var y = 0; y < map.Height; y++)
        {
            map.Set(new Point(0, y), Tree);
            map.Set(new Point(map.Width - 1, y), Tree);
        }

        for (var x = 2; x < map.Width - 2; x++)
        for (var y = 21; y <= 25; y++) map.Set(new Point(x, y), Road);
        for (var y = 2; y < map.Height - 2; y++)
        for (var x = 48; x <= 52; x++) map.Set(new Point(x, y), Road);

        for (var y = 33; y <= 39; y++)
        for (var x = 67; x <= 78; x++) map.Set(new Point(x, y), Water);

        AddShop(map, 10, 10, 15, 8, "The Copper Kettle", new Rgb(255, 155, 55), [
            new ShopItem("healing potion", "Restore 14 HP", 12, g => g.AddItem(ItemKind.HealingPotion, 1)),
            new ShopItem("trail ration", "Restore food while resting", 8, g => g.AddItem(ItemKind.Ration, 1)),
            new ShopItem("sealed water flask", "Restore thirst", 7, g => g.AddItem(ItemKind.WaterFlask, 1)),
            new ShopItem("sun-fruit ambrosia", "Fully restore food, water and mana", 48, g => g.AddItem(ItemKind.Ambrosia, 1)),
            new ShopItem("waystone", "Return to the upward stair", 38, g => g.AddItem(ItemKind.Waystone, 1)),
            new ShopItem("wall torch", "Mount on an adjacent dungeon wall", 10, g => g.AddItem(ItemKind.Torch, 1)),
            new ShopItem("lantern oil", "Increase lantern radius", 28, g => { g.LanternRadius = Math.Min(14, g.LanternRadius + 1.5); g.AddMessage("Your lantern burns wider and cleaner."); })
        ]);

        AddShop(map, 72, 8, 17, 9, "Cinder & Sigil", new Rgb(185, 90, 255), [
            new ShopItem("mana tonic", "Restore 8 mana", 20, g => g.AddItem(ItemKind.ManaTonic, 1)),
            new ShopItem("focus elixir", "+4 spell power for 20 turns", 34, g => g.AddItem(ItemKind.FocusElixir, 1)),
            new ShopItem("greater healing elixir", "Restore at least 30 HP", 30, g => g.AddItem(ItemKind.GreaterHealingPotion, 1)),
            new ShopItem("blast prism", "Permanently enlarge explosions", 55, g => { g.BlastRadius = Math.Min(4, g.BlastRadius + 1); g.AddMessage("Your flame fractures into a wider bloom."); }),
            new ShopItem("alchemist bomb", "Inventory explosive; shatters walls", 25, g => g.AddItem(ItemKind.Bomb, 1)),
            new ShopItem("star-glass wick", "Brighter, warmer lantern light", 60, g => { g.LanternIntensity += 0.18; g.AddMessage("Starlight swims inside your lantern."); })
        ]);

        AddShop(map, 13, 31, 18, 9, "Brass Badger Smithy", new Rgb(255, 103, 35), [
            new ShopItem("iron longsword", "Main hand · ATK +3", 32, g => g.AddEquipment(EquipmentFactory.Create("iron longsword", EquipmentSlot.MainHand, '/', attack: 3, description: "Honest Emberfall steel"))),
            new ShopItem("throwing bomb", "Inventory explosive; shatters walls", 24, g => g.AddItem(ItemKind.Bomb, 1)),
            new ShopItem("smoke bomb", "Blind nearby creatures for two turns", 22, g => g.AddItem(ItemKind.SmokeBomb, 1)),
            new ShopItem("ironhide elixir", "+4 defense for 20 turns", 30, g => g.AddItem(ItemKind.GuardElixir, 1)),
            new ShopItem("dragonroot elixir", "+4 attack for 20 turns", 30, g => g.AddItem(ItemKind.MightElixir, 1)),
            new ShopItem("watchman's buckler", "Off hand · DEF +2", 38, g => g.AddEquipment(EquipmentFactory.Create("watchman's buckler", EquipmentSlot.OffHand, ']', defense: 2, description: "Oak faced with hammered iron"))),
            new ShopItem("royal brigandine", "Body · DEF +4", 72, g => g.AddEquipment(EquipmentFactory.ArmoryArmor()))
        ]);

        map.StairsDown = new Point(88, 38);
        map.Set(map.StairsDown, Down);

        AddProvision(map, new Point(30, 23), ProvisionKind.Food, "Emberfall community pantry", 4);
        AddProvision(map, new Point(65, 36), ProvisionKind.Water, "old town well", 6);

        var lamps = new[] { new Point(20, 23), new Point(35, 23), new Point(50, 12), new Point(50, 34), new Point(65, 23), new Point(80, 23) };
        for (var i = 0; i < lamps.Length; i++)
        {
            var p = lamps[i];
            map.Set(p, new Tile('†', new Rgb(220, 162, 74), new Rgb(20, 12, 3), true, true, "street lamp"));
            map.Lights.Add(new LightSource(p, new Rgb(255, 154, 55), 9, 1.25, 0.14, i * 1.73));
        }

        AddTownResidents(map);

        // Seeded gardens and windbreaks vary the town outskirts without ever
        // crossing its guaranteed roads or building approaches.
        for (var i = 0; i < 90; i++)
        {
            var p = new Point(random.Next(3, map.Width - 3), random.Next(3, map.Height - 3));
            if (!map[p].Walkable || map[p].Name != "grass" || map.Shops.Any(shop => shop.Position.DistanceSquared(p) < 16) ||
                map.Npcs.Any(npc => npc.Position.DistanceSquared(p) < 4)) continue;
            map.Set(p, random.Next(4) == 0 ? Tree : TallGrass with { Name = "Emberfall garden verge" });
        }

        var westGate = new Point(2, 23);
        map.Set(westGate, RegionGate with { Name = "west gate to the Ashwind March" });
        map.TravelPoints[westGate] = TravelKind.Wilderness;
        map.Landmarks[map.Start] = "Emberfall Crossroads";
        map.Landmarks[westGate] = "Emberfall West Gate";
        map.Landmarks[map.StairsDown] = "Emberfall Delve";
        foreach (var shop in map.Shops) map.Landmarks[shop.Position] = shop.Name;

        GuaranteeCriticalConnectivity(map, map.Start,
            map.Shops.Select(shop => shop.Position).Concat(map.Provisions.Keys).Concat(map.TravelPoints.Keys).Append(map.StairsDown));

        return map;
    }

    public static (Map Map, List<Actor> Monsters) BuildWilderness(int seed = 18427)
    {
        var random = new Random(seed);
        var map = new Map(240, 120, "Ashwind March", false, Grass) { Biome = BiomeKind.Wilds };
        map.Start = new Point(235, 60);

        for (var x = 0; x < map.Width; x++)
        {
            map.Set(new Point(x, 0), WildTree);
            map.Set(new Point(x, map.Height - 1), WildTree);
        }
        for (var y = 0; y < map.Height; y++)
        {
            map.Set(new Point(0, y), WildTree);
            map.Set(new Point(map.Width - 1, y), WildTree);
        }

        for (var i = 0; i < 5200; i++)
        {
            var p = new Point(random.Next(2, map.Width - 2), random.Next(2, map.Height - 2));
            if (p.DistanceSquared(map.Start) < 90) continue;
            var roll = random.Next(100);
            map.Set(p, roll < 58 ? TallGrass : roll < 79 ? Heather : roll < 95 ? WildTree : Boulder);
        }

        // The River Sere divides the march. Its ford is made legible by the
        // east-west road and bridge rather than hidden in decorative noise.
        for (var y = 1; y < map.Height - 1; y++)
        {
            var center = 118 + (int)Math.Round(Math.Sin(y * 0.15) * 5);
            for (var x = center - 1; x <= center + 1; x++)
            {
                var p = new Point(x, y);
                map.Set(p, x == center ? Water : ShallowWater);
                map.Elements[x, y].Wet = 80;
            }
        }

        for (var x = 2; x < map.Width - 1; x++)
        for (var y = 58; y <= 62; y++)
        {
            var p = new Point(x, y);
            map.Set(p, x is >= 109 and <= 127 ? Bridge : Road with { Name = "old west road" });
            map.Elements[x, y].Wet = 0;
        }

        var returnGate = new Point(238, 60);
        map.Set(returnGate, RegionGate with { Name = "road back to Emberfall" });
        map.TravelPoints[returnGate] = TravelKind.Town;

        CarveOutdoorRoad(map, new Point(198, 60), new Point(198, 32), 2);
        CarveOutdoorRoad(map, new Point(132, 60), new Point(132, 80), 2);
        CarveOutdoorRoad(map, new Point(57, 60), new Point(57, 62), 2);
        CarveOutdoorRoad(map, new Point(48, 60), new Point(48, 108), 2);
        CarveOutdoorRoad(map, new Point(198, 30), new Point(214, 17), 1);
        CarveOutdoorRoad(map, new Point(57, 62), new Point(72, 97), 1);
        CarveOutdoorRoad(map, new Point(132, 78), new Point(145, 102), 1);

        AddWildernessWatchtower(map);
        AddWildernessStandingStones(map);
        AddWildernessCamp(map);
        AddWildernessVillage(map, "Bramblewick", new Point(198, 30), "Bramblewick Trading Post", new Rgb(224, 158, 76),
            "elin_bramble", "Elin Bramble", "reeve", "The hedges keep the wind out. They are less successful with goblins.",
            "tom_wick", "Tom Wick", "goatherd", "If you hear bells beyond the north hedge, those are not my goats.");
        AddWildernessVillage(map, "Stonecross", new Point(132, 78), "Stonecross Forge", new Rgb(192, 126, 74),
            "dara_stone", "Dara Stone", "mason", "Every road in the March eventually asks Stonecross for repairs.",
            "vesk_hammer", "Vesk Hammer", "farrier", "Castle riders pay in silver. Travellers mostly pay in stories.");
        AddWildernessVillage(map, "Reedwater", new Point(57, 62), "Reedwater Remedies", new Rgb(84, 180, 174),
            "nella_reed", "Nella Reed", "herbalist", "The river gives medicine freely. It charges for carelessness.",
            "ori_fen", "Ori Fen", "fisher", "River Sere runs clear at dawn and tells lies by moonlight.");
        AddWildernessCastle(map);

        AddDungeonEntrance(map, new Point(214, 17), TravelKind.HollowrootMine, "descent into Hollowroot Mine");
        AddDungeonEntrance(map, new Point(72, 97), TravelKind.SunkenBarrow, "steps into the Sunken Barrow");
        AddDungeonEntrance(map, new Point(145, 102), TravelKind.MoonwellCavern, "mouth of Moonwell Cavern");

        AddProvision(map, new Point(158, 16), ProvisionKind.Water, "clear hillside spring", 8);
        AddProvision(map, new Point(204, 45), ProvisionKind.Food, "abandoned berry orchard", 6);
        foreach (var position in new[] { new Point(100, 94), new Point(151, 21), new Point(13, 48), new Point(181, 106) })
            if (!map[position].Walkable) map.Set(position, Grass with { Name = "secluded wilderness clearing" });
        map.GroundItems.Add(new GroundItem(new Point(25, 13), MakeItem(ItemKind.Waystone)));
        map.GroundItems.Add(new GroundItem(new Point(100, 94), MakeItem(ItemKind.ManaTonic)));
        map.GroundItems.Add(new GroundItem(new Point(151, 21), MakeItem(ItemKind.WaterFlask)));
        map.GroundItems.Add(new GroundItem(new Point(13, 48), MakeItem(ItemKind.Ration)));
        map.GroundItems.Add(new GroundItem(new Point(181, 106), MakeItem(ItemKind.SmokeBomb)));
        map.SpecialRoomNames.AddRange(["Old West Road", "River Sere", "Broken Watchtower", "Whispering Stones", "Wayfarer's Camp", "Berry Orchard",
            "Bramblewick", "Stonecross", "Reedwater", "Castle Valeward", "Hollowroot Mine", "Sunken Barrow", "Moonwell Cavern", "Castle Undercroft"]);
        map.Landmarks[returnGate] = "Emberfall Road";
        map.Landmarks[new Point(25, 18)] = "Broken Watchtower";
        map.Landmarks[new Point(100, 94)] = "Whispering Stones";
        map.Landmarks[new Point(170, 90)] = "Wayfarer's Camp";
        map.Landmarks[new Point(198, 32)] = "Bramblewick";
        map.Landmarks[new Point(132, 80)] = "Stonecross";
        map.Landmarks[new Point(57, 62)] = "Reedwater";
        map.Landmarks[new Point(48, 108)] = "Castle Valeward";
        map.Landmarks[new Point(214, 17)] = "Hollowroot Mine";
        map.Landmarks[new Point(72, 97)] = "Sunken Barrow";
        map.Landmarks[new Point(145, 102)] = "Moonwell Cavern";
        map.Landmarks[new Point(48, 90)] = "Castle Valeward Undercroft";

        GuaranteeCriticalConnectivity(map, map.Start,
            map.Shops.Select(shop => shop.Position).Concat(map.Provisions.Keys).Concat(map.TravelPoints.Keys));

        var templates = new[]
        {
            ('w', "thorn wolf", new Rgb(135, 145, 118), 11, 4, 1, 7, 11, MonsterAi.Hunter),
            ('b', "briar boar", new Rgb(147, 105, 72), 16, 5, 2, 10, 15, MonsterAi.Brute),
            ('g', "goblin scout", new Rgb(94, 174, 72), 10, 4, 1, 9, 13, MonsterAi.Skirmisher),
            ('r', "road brigand", new Rgb(170, 111, 80), 14, 5, 2, 14, 18, MonsterAi.Hunter),
            ('a', "marsh spider", new Rgb(91, 168, 119), 12, 5, 1, 10, 15, MonsterAi.Lurker),
            ('T', "mossback troll", new Rgb(91, 139, 83), 25, 7, 4, 22, 30, MonsterAi.Brute)
        };
        var monsters = new List<Actor>();
        var candidates = new List<Point>();
        for (var y = 3; y < map.Height - 3; y++)
        for (var x = 3; x < map.Width - 3; x++)
        {
            var p = new Point(x, y);
            if (map[p].Walkable && p.DistanceSquared(map.Start) > 240 && !map.Provisions.ContainsKey(p) &&
                !map.TravelPoints.ContainsKey(p) && !map.GroundItems.Any(item => item.Position == p) && map.Elements[x, y].Fire == 0)
                candidates.Add(p);
        }
        var safeCenters = new[] { map.Start, new Point(198, 30), new Point(132, 78), new Point(57, 62), new Point(48, 94), new Point(170, 90) };
        candidates.RemoveAll(point => safeCenters.Any(center => point.DistanceSquared(center) < 210) ||
            map[point].Name.Contains("road") || map[point].Name.Contains("bridge") || map[point].Name.Contains("Valeward"));
        for (var i = 0; i < 52 && candidates.Count > 0; i++)
        {
            var index = random.Next(candidates.Count);
            var p = candidates[index];
            candidates.RemoveAt(index);
            if (monsters.Any(monster => monster.Position.DistanceSquared(p) < 16)) { i--; continue; }
            var template = templates[random.Next(templates.Length)];
            var actor = new Actor
            {
                Position = p, Glyph = template.Item1, Name = template.Item2, Color = template.Item3,
                MaxHp = template.Item4, Hp = template.Item4, Attack = template.Item5, Defense = template.Item6,
                Gold = template.Item7, XpReward = template.Item8, Ai = template.Item9
            };
            ConfigureMonster(actor);
            monsters.Add(actor);
        }
        return (map, monsters);
    }

    public static (Map Map, List<Actor> Monsters) BuildSiteDungeon(string name, int seed, int biomeDepth)
    {
        var (map, monsters) = BuildDungeon(seed, biomeDepth);
        map.Name = name;
        (string Mechanic, string Boss, char Glyph, Rgb Color, MonsterFaction Faction, string Reward) identity = name switch
        {
            "Hollowroot Mine" => ("Root-choked galleries spread webs and reward controlled fire.", "Root-Crowned Matriarch", 'Q', new Rgb(104, 194, 92), MonsterFaction.Wild, "matriarch thornblade"),
            "Sunken Barrow" => ("Floodgates change conductive routes through drowned burial halls.", "Drowned Castellan", 'K', new Rgb(82, 159, 204), MonsterFaction.Undead, "castellan tideguard"),
            "Moonwell Cavern" => ("Prisms redirect charged light through reflective crystal chambers.", "Moonwell Wyrm", 'W', new Rgb(164, 122, 245), MonsterFaction.Construct, "moonwell star-ring"),
            _ => ("Ancient generators open sealed wings while waking oathbound dead.", "Oathless Regent", 'R', new Rgb(205, 184, 132), MonsterFaction.Undead, "regent's oathbreaker")
        };
        map.DungeonMechanic = identity.Mechanic;
        map.BossName = identity.Boss;
        var boss = monsters.OrderByDescending(monster => monster.XpReward + monster.MaxHp).First();
        boss.Name = identity.Boss;
        boss.Glyph = identity.Glyph;
        boss.Color = identity.Color;
        boss.Faction = identity.Faction;
        boss.IsBoss = true;
        boss.UniqueRewardName = identity.Reward;
        boss.MaxHp = (int)(boss.MaxHp * 1.7) + 12;
        boss.Hp = boss.MaxHp;
        boss.Attack += 3;
        boss.Defense += 2;
        boss.Gold += 45;
        boss.XpReward += 65;
        var formerEnd = map.StairsDown;
        boss.Position = formerEnd;
        boss.HomePosition = formerEnd;
        boss.TerritoryRadius = 14;
        map.Set(formerEnd, BiomeFloor(map.Biome));
        map.StairsDown = new Point(-1, -1);
        map.TravelPoints[map.StairsUp] = TravelKind.WildernessReturn;
        map.Set(map.StairsUp, Up with { Name = $"exit from {name}" });
        map.GroundItems.Add(new GroundItem(formerEnd, MakeItem(biomeDepth >= 3 ? ItemKind.GreaterHealingPotion : ItemKind.Ambrosia)));
        map.SpecialRoomNames.Add(name);
        map.Landmarks[map.StairsUp] = $"{name} Entrance";
        map.Landmarks[formerEnd] = $"Heart of {name}";
        return (map, monsters);
    }

    private static void AddDungeonEntrance(Map map, Point position, TravelKind destination, string name)
    {
        map.Set(position, Down with { Name = name });
        map.TravelPoints[position] = destination;
        map.Lights.Add(new LightSource(position, new Rgb(112, 138, 174), 6, 0.42, 0.02));
    }

    private static void CarveOutdoorRoad(Map map, Point from, Point to, int halfWidth)
    {
        void Paint(Point p)
        {
            if (!map.Contains(p)) return;
            var crossing = map[p].Name.Contains("water", StringComparison.OrdinalIgnoreCase);
            map.Set(p, crossing ? Bridge : Road with { Name = "wilderness road" });
            map.Elements[p.X, p.Y].Wet = 0;
        }
        for (var x = Math.Min(from.X, to.X); x <= Math.Max(from.X, to.X); x++)
        for (var offset = -halfWidth; offset <= halfWidth; offset++) Paint(new Point(x, from.Y + offset));
        for (var y = Math.Min(from.Y, to.Y); y <= Math.Max(from.Y, to.Y); y++)
        for (var offset = -halfWidth; offset <= halfWidth; offset++) Paint(new Point(to.X + offset, y));
    }

    private static void AddWildernessVillage(Map map, string village, Point center, string shopName, Rgb color,
        string firstId, string firstName, string firstRole, string firstGreeting,
        string secondId, string secondName, string secondRole, string secondGreeting)
    {
        for (var y = center.Y - 8; y <= center.Y + 8; y++)
        for (var x = center.X - 11; x <= center.X + 11; x++)
            if (map.Contains(new Point(x, y)) && map[new Point(x, y)].Walkable) map.Set(new Point(x, y), Grass with { Name = $"{village} village green" });
        var stock = village switch
        {
            "Stonecross" => new List<ShopItem>
            {
                new("throwing bomb", "Break stone and clustered enemies", 23, game => game.AddItem(ItemKind.Bomb, 1)),
                new("ironhide elixir", "+4 defense for 20 turns", 29, game => game.AddItem(ItemKind.GuardElixir, 1)),
                new("dragonroot elixir", "+4 attack for 20 turns", 29, game => game.AddItem(ItemKind.MightElixir, 1)),
                new("trail ration", "Farrier's road bread", 7, game => game.AddItem(ItemKind.Ration, 1))
            },
            "Reedwater" => new List<ShopItem>
            {
                new("healing potion", "Restore 14 HP", 11, game => game.AddItem(ItemKind.HealingPotion, 1)),
                new("mana tonic", "Restore 8 mana", 18, game => game.AddItem(ItemKind.ManaTonic, 1)),
                new("sealed water flask", "Clear River Sere water", 5, game => game.AddItem(ItemKind.WaterFlask, 1)),
                new("silver smoke bomb", "Disorient nearby creatures", 21, game => game.AddItem(ItemKind.SmokeBomb, 1))
            },
            _ => new List<ShopItem>
            {
                new("trail ration", "Locally packed food for the road", 7, game => game.AddItem(ItemKind.Ration, 1)),
                new("sealed water flask", "Clean village well water", 6, game => game.AddItem(ItemKind.WaterFlask, 1)),
                new("healing potion", "Restore 14 HP", 12, game => game.AddItem(ItemKind.HealingPotion, 1)),
                new("waystone", "Return to this region's entrance", 36, game => game.AddItem(ItemKind.Waystone, 1))
            }
        };
        AddShop(map, center.X - 6, center.Y - 6, 13, 8, shopName, color, stock);

        AddCottage(map, center + new Point(-11, 4), $"{village} west cottage");
        AddCottage(map, center + new Point(5, 4), $"{village} east cottage");
        var first = center + new Point(-9, 2);
        var second = center + new Point(9, 2);
        map.Npcs.Add(new TownNpc { Id = firstId, Name = firstName, Role = firstRole, Position = first, Color = color, Greeting = firstGreeting });
        map.Npcs.Add(new TownNpc { Id = secondId, Name = secondName, Role = secondRole, Position = second, Color = Rgb.Lerp(color, new Rgb(190, 215, 175), 0.4), Greeting = secondGreeting });
        AddProvision(map, center + new Point(9, -3), ProvisionKind.Water, $"{village} village well", 8);
        map.Lights.Add(new LightSource(center + new Point(-8, 0), new Rgb(255, 153, 60), 8, 1.0, 0.12, center.X));
        map.Lights.Add(new LightSource(center + new Point(8, 0), new Rgb(255, 153, 60), 8, 1.0, 0.12, center.Y));
    }

    private static void AddCottage(Map map, Point topLeft, string name)
    {
        for (var y = topLeft.Y; y < topLeft.Y + 5; y++)
        for (var x = topLeft.X; x < topLeft.X + 7; x++)
        {
            var border = x == topLeft.X || x == topLeft.X + 6 || y == topLeft.Y || y == topLeft.Y + 4;
            map.Set(new Point(x, y), border ? Wall with { Name = $"{name} wall" } : WoodFloor with { Name = name });
        }
        map.Set(topLeft + new Point(3, 0), OpenDoor with { Name = $"{name} doorway" });
    }

    private static void AddWildernessCastle(Map map)
    {
        const int left = 29, right = 67, top = 78, bottom = 108;
        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            var border = x is left or right || y is top or bottom;
            map.Set(new Point(x, y), border ? Wall with { Name = "Castle Valeward curtain wall" } : Grass with { Name = "Castle Valeward courtyard" });
        }
        map.Set(new Point(48, bottom), Road with { Name = "Castle Valeward gate" });
        for (var y = 84; y <= 98; y++)
        for (var x = 39; x <= 57; x++)
        {
            var border = x is 39 or 57 || y is 84 or 98;
            map.Set(new Point(x, y), border ? Wall with { Name = "Valeward keep wall" } : WoodFloor with { Name = "Valeward great hall" });
        }
        map.Set(new Point(48, 98), OpenDoor with { Name = "Valeward keep entrance" });
        AddDungeonEntrance(map, new Point(48, 90), TravelKind.CastleUndercroft, "stairs to Castle Valeward's undercroft");
        map.Npcs.Add(new TownNpc { Id = "captain_roen", Name = "Captain Roen", Role = "castle guard", Position = new Point(46, 103),
            Color = new Rgb(176, 188, 205), Greeting = "Valeward keeps the western road. The undercroft is older than the keep and less obedient." });
        map.Npcs.Add(new TownNpc { Id = "lady_sera", Name = "Lady Sera", Role = "castellan", Position = new Point(51, 94),
            Color = new Rgb(205, 174, 112), Greeting = "A castle is a promise made in stone. Ours has survived long enough to acquire conditions." });
        AddProvision(map, new Point(59, 102), ProvisionKind.Water, "Castle Valeward courtyard cistern", 10);
        foreach (var p in new[] { new Point(34, 103), new Point(62, 103), new Point(43, 96), new Point(53, 96) })
            map.Lights.Add(new LightSource(p, new Rgb(255, 144, 52), 9, 1.1, 0.12, p.X * 0.7));
    }

    private static void AddWildernessWatchtower(Map map)
    {
        for (var y = 8; y <= 18; y++)
        for (var x = 19; x <= 31; x++)
        {
            var border = x is 19 or 31 || y is 8 or 18;
            map.Set(new Point(x, y), border ? RuinWall : RuinFloor);
        }
        map.Set(new Point(25, 18), RuinFloor with { Name = "broken watchtower entrance" });
        map.Set(new Point(31, 11), Rubble);
        map.Lights.Add(new LightSource(new Point(25, 13), new Rgb(104, 135, 172), 6, 0.38, 0.03));
    }

    private static void AddWildernessStandingStones(Map map)
    {
        var center = new Point(100, 94);
        foreach (var delta in new[] { new Point(-5, 0), new Point(5, 0), new Point(0, -3), new Point(0, 3), new Point(-4, -2), new Point(4, 2) })
            map.Set(center + delta, new Tile('○', new Rgb(126, 130, 145), new Rgb(7, 8, 11), false, true, "weathered standing stone", false, 0, 0.28));
        map.Lights.Add(new LightSource(center, new Rgb(116, 105, 190), 8, 0.42, 0.02));
    }

    private static void AddWildernessCamp(Map map)
    {
        var fire = new Point(170, 90);
        for (var y = 87; y <= 93; y++)
        for (var x = 165; x <= 175; x++)
            if (map[new Point(x, y)].Walkable) map.Set(new Point(x, y), Grass);
        map.Set(fire, new Tile('♨', new Rgb(255, 132, 42), new Rgb(23, 7, 2), true, true, "banked wayfarer's campfire"));
        map.Elements[fire.X, fire.Y].Fire = 30;
        map.Lights.Add(new LightSource(fire, new Rgb(255, 123, 35), 9, 1.2, 0.18, 7.3));
        AddProvision(map, new Point(167, 90), ProvisionKind.Food, "wayfarer's shared cookpot", 4);
    }

    private static void AddTownResidents(Map map)
    {
        map.Npcs.AddRange([
            new TownNpc
            {
                Id = "mara_vey", Name = "Mara Vey", Role = "lamplighter", Position = new Point(47, 13),
                Color = new Rgb(255, 190, 72), QuestId = "borrowed_flame",
                Greeting = "Every lamp in Emberfall is a promise: someone will still be awake when you return."
            },
            new TownNpc
            {
                Id = "orra_flint", Name = "Orra Flint", Role = "smith", Position = new Point(20, 40),
                Color = new Rgb(235, 112, 55), QuestId = "ash_road_bounty",
                Greeting = "Bad steel bends. Bad roads get people killed. I can fix only one of those from here."
            },
            new TownNpc
            {
                Id = "neris_vale", Name = "Neris Vale", Role = "wellkeeper", Position = new Point(63, 36),
                Color = new Rgb(80, 188, 235), QuestId = "turn_the_tide",
                Greeting = "Water remembers every chamber it crosses. Lately the well has been remembering iron and old graves."
            },
            new TownNpc
            {
                Id = "tamsin_reed", Name = "Tamsin Reed", Role = "innkeeper", Position = new Point(15, 19),
                Color = new Rgb(226, 170, 102),
                Greeting = "The Copper Kettle has stew, dry chairs, and no questions until the second bowl."
            },
            new TownNpc
            {
                Id = "pell_arno", Name = "Pell Arno", Role = "courier", Position = new Point(46, 20),
                Color = new Rgb(144, 191, 126),
                Greeting = "Three letters for the lower road and nobody willing to carry them. Sensible town, Emberfall."
            },
            new TownNpc
            {
                Id = "sister_avel", Name = "Sister Avel", Role = "dawnwatch", Position = new Point(54, 27),
                Color = new Rgb(210, 205, 150),
                Greeting = "Come back by this road. That is the only blessing worth giving an adventurer."
            }
        ]);
    }

    private static void AddShop(Map map, int x, int y, int w, int h, string name, Rgb color, List<ShopItem> items)
    {
        for (var yy = y; yy < y + h; yy++)
        for (var xx = x; xx < x + w; xx++)
        {
            var border = xx == x || yy == y || xx == x + w - 1 || yy == y + h - 1;
            map.Set(new Point(xx, yy), border ? Wall : WoodFloor);
        }

        var door = new Point(x + w / 2, y + h - 1);
        map.Set(door, new Tile('$', color, new Rgb(20, 10, 4), true, true, $"entrance to {name}"));
        map.Shops.Add(new Shop { Name = name, Position = door, Color = color, Items = items });
        map.Lights.Add(new LightSource(door, color, 8, 1.1, 0.08, x * 0.3));
        map.Lights.Add(new LightSource(new Point(x + w / 2, y + h / 2), new Rgb(255, 128, 42), 7, 0.8, 0.1, y));
    }

    public static (Map Map, List<Actor> Monsters) BuildDungeon(int seed = 7331, int depth = 1)
    {
        var biome = BiomeForDepth(depth);
        var map = new Map(110, 58, $"{BiomeName(biome)} — Depth {depth}", false, BiomeWall(biome)) { Biome = biome };
        var random = new Random(seed);
        var rooms = new List<(int X, int Y, int W, int H)>();

        for (var attempts = 0; attempts < 220 && rooms.Count < 19; attempts++)
        {
            var w = random.Next(6, 13);
            var h = random.Next(5, 10);
            var x = random.Next(2, map.Width - w - 2);
            var y = random.Next(2, map.Height - h - 2);
            var room = (x, y, w, h);
            if (rooms.Any(r => x <= r.X + r.W + 1 && x + w + 1 >= r.X && y <= r.Y + r.H + 1 && y + h + 1 >= r.Y)) continue;

            for (var yy = y; yy < y + h; yy++)
            for (var xx = x; xx < x + w; xx++) map.Set(new Point(xx, yy), BiomeFloor(biome));

            if (rooms.Count > 0)
            {
                var previous = Center(rooms[^1]);
                var current = Center(room);
                if (random.Next(2) == 0)
                {
                    CarveHorizontal(map, previous.X, current.X, previous.Y);
                    CarveVertical(map, previous.Y, current.Y, current.X);
                }
                else
                {
                    CarveVertical(map, previous.Y, current.Y, previous.X);
                    CarveHorizontal(map, previous.X, current.X, current.Y);
                }
            }
            rooms.Add(room);
        }

        map.Start = Center(rooms[0]);
        map.StairsUp = map.Start;
        map.Set(map.StairsUp, Up);
        map.StairsDown = Center(rooms[^1]);
        map.Set(map.StairsDown, Down);

        AddSpecialRooms(map, rooms, random);
        ApplyBiomeFeatures(map, rooms, random);
        AddDoorsTrapsAndMachines(map, rooms, random);
        AddDungeonProvisions(map, rooms, random);

        for (var i = 1; i < rooms.Count; i += 2)
        {
            var r = rooms[i];
            var mounts = new[]
            {
                new Point(r.X - 1, r.Y + r.H / 2), new Point(r.X + r.W, r.Y + r.H / 2),
                new Point(r.X + r.W / 2, r.Y - 1), new Point(r.X + r.W / 2, r.Y + r.H)
            };
            var torch = mounts.Where(p => map.Contains(p) && !map[p].Walkable && !map.Doors.ContainsKey(p) && !map.WallTorches.ContainsKey(p))
                .OrderBy(_ => random.Next()).FirstOrDefault(new Point(-1, -1));
            if (!map.Contains(torch)) continue;
            if (map.Biome == BiomeKind.LightlessCourt && i % 4 != 1) continue;
            var torchColor = map.Biome == BiomeKind.DrownedReliquary ? new Rgb(70, 175, 235) :
                map.Biome == BiomeKind.PrismaticFoundry ? new Rgb(130, 105, 255) : new Rgb(255, 103, 25);
            map.WallTorches[torch] = new WallTorchFeature { Position = torch, UnderlyingWall = map[torch] };
            map.Set(torch, MountedWallTorch with { Foreground = torchColor });
            map.Lights.Add(new LightSource(torch, torchColor, random.Next(7, 11), 1.35, 0.22, random.NextDouble() * 20));
        }

        var monsters = new List<Actor>();
        var templates = new[]
        {
            ('r', "cave rat", new Rgb(168, 133, 96), 5, 2, 0, 4, 5, MonsterAi.Skirmisher, (Rgb?)null),
            ('b', "blind cave bat", new Rgb(151, 116, 172), 7, 3, 0, 6, 8, MonsterAi.Skirmisher, (Rgb?)null),
            ('k', "candle kobold", new Rgb(210, 155, 76), 8, 3, 1, 7, 9, MonsterAi.Skirmisher, (Rgb?)new Rgb(255, 170, 55)),
            ('g', "gutter goblin", new Rgb(93, 184, 76), 9, 3, 1, 8, 9, MonsterAi.Hunter, (Rgb?)null),
            ('j', "ochre slime", new Rgb(198, 182, 45), 14, 3, 2, 9, 12, MonsterAi.Lurker, (Rgb?)new Rgb(180, 215, 40)),
            ('a', "glassfang spider", new Rgb(115, 210, 165), 10, 5, 1, 11, 14, MonsterAi.Skirmisher, (Rgb?)new Rgb(45, 210, 135)),
            ('s', "ember skeleton", new Rgb(230, 215, 174), 12, 4, 2, 12, 14, MonsterAi.Hunter, (Rgb?)new Rgb(255, 78, 20)),
            ('m', "crystal myconid", new Rgb(92, 207, 180), 14, 4, 2, 14, 20, MonsterAi.Lurker, (Rgb?)new Rgb(50, 210, 190)),
            ('c', "ash cultist", new Rgb(195, 85, 115), 15, 5, 2, 16, 22, MonsterAi.Caster, (Rgb?)new Rgb(210, 35, 95)),
            ('o', "slag orc", new Rgb(139, 155, 72), 18, 6, 3, 16, 22, MonsterAi.Brute, (Rgb?)null),
            ('G', "vault gargoyle", new Rgb(145, 150, 165), 23, 7, 5, 22, 28, MonsterAi.Lurker, (Rgb?)null),
            ('S', "cinder salamander", new Rgb(245, 100, 28), 19, 7, 3, 20, 27, MonsterAi.Skirmisher, (Rgb?)new Rgb(255, 65, 8)),
            ('C', "cinder mage", new Rgb(242, 105, 52), 13, 5, 1, 18, 26, MonsterAi.Caster, (Rgb?)new Rgb(255, 70, 18)),
            ('h', "umbra hound", new Rgb(94, 70, 135), 21, 8, 3, 24, 31, MonsterAi.Hunter, (Rgb?)new Rgb(90, 45, 210)),
            ('T', "tunnel troll", new Rgb(99, 153, 98), 28, 8, 4, 25, 36, MonsterAi.Brute, (Rgb?)null),
            ('M', "hungry mimic", new Rgb(190, 128, 58), 30, 9, 4, 35, 42, MonsterAi.Lurker, (Rgb?)new Rgb(255, 145, 35)),
            ('w', "void whisper", new Rgb(114, 104, 205), 16, 6, 2, 22, 32, MonsterAi.Caster, (Rgb?)new Rgb(94, 68, 255)),
            ('W', "vault warden", new Rgb(110, 145, 235), 35, 9, 6, 40, 60, MonsterAi.Hunter, (Rgb?)new Rgb(65, 118, 255)),
            ('L', "crown lich", new Rgb(190, 205, 255), 38, 11, 6, 55, 76, MonsterAi.Caster, (Rgb?)new Rgb(90, 130, 255)),
            ('D', "furnace demon", new Rgb(245, 60, 25), 48, 13, 7, 70, 95, MonsterAi.Brute, (Rgb?)new Rgb(255, 45, 5))
        };

        for (var i = 2; i < rooms.Count; i++)
        {
            var room = rooms[i];
            var count = random.NextDouble() < 0.42 + depth * 0.06 ? 2 : 1;
            for (var n = 0; n < count; n++)
            {
                var p = new Point(random.Next(room.X + 1, room.X + room.W - 1), random.Next(room.Y + 1, room.Y + room.H - 1));
                if (p == map.StairsDown || map.Provisions.ContainsKey(p) || monsters.Any(m => m.Position == p)) continue;
                var tier = i == rooms.Count - 1
                    ? Math.Min(templates.Length - 1, 9 + depth * 2)
                    : Math.Min(templates.Length - 1, i / 3 + random.Next(4) + (depth - 1) * 2);
                var allowed = BiomeMonsterNames(map.Biome);
                var candidates = templates.Select((template, index) => (template, index))
                    .Where(entry => allowed.Contains(entry.template.Item2) && entry.index <= tier + 3).ToList();
                var selected = i == rooms.Count - 1
                    ? templates.Select((template, index) => (template, index)).Where(entry => allowed.Contains(entry.template.Item2)).Last()
                    : candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : (template: templates[tier], index: tier);
                var t = selected.template;
                var hp = t.Item4 + (depth - 1) * 5;
                var actor = new Actor
                {
                    Position = p, Glyph = t.Item1, Name = t.Item2, Color = t.Item3, MaxHp = hp, Hp = hp,
                    Attack = t.Item5 + depth - 1, Defense = t.Item6 + (depth - 1) / 2,
                    Gold = t.Item7 + depth * 2, XpReward = t.Item8 + depth * 4, Ai = t.Item9, GlowColor = t.Item10
                };
                ConfigureMonster(actor);
                monsters.Add(actor);
            }
        }

        for (var i = 1; i < rooms.Count; i += 3)
        {
            var p = Center(rooms[i]);
            if (p == map.StairsDown || map.Provisions.ContainsKey(p) || monsters.Any(m => m.Position == p)) continue;
            map.GroundItems.Add(new GroundItem(p, RandomLoot(random)));
        }

        GuaranteeCriticalConnectivity(map, map.StairsUp,
            map.Provisions.Keys.Concat(map.Machines.Keys).Append(map.StairsDown));

        return (map, monsters);
    }

    public static bool IsReachable(Map map, Point start, Point goal)
    {
        if (!map.Contains(start) || !map.Contains(goal) || !map[start].Walkable || !map[goal].Walkable) return false;
        var open = new Queue<Point>();
        var visited = new HashSet<Point> { start };
        open.Enqueue(start);
        var directions = new[] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) };
        while (open.Count > 0)
        {
            var p = open.Dequeue();
            if (p == goal) return true;
            foreach (var direction in directions)
            {
                var next = p + direction;
                if (map.Contains(next) && map[next].Walkable && visited.Add(next)) open.Enqueue(next);
            }
        }
        return false;
    }

    private static void GuaranteeCriticalConnectivity(Map map, Point start, IEnumerable<Point> destinations)
    {
        foreach (var destination in destinations.Where(map.Contains).Distinct())
        {
            map.CriticalPoints.Add(destination);
            if (IsReachable(map, start, destination)) continue;
            CarveGuaranteedConnection(map, start, destination);
            if (!IsReachable(map, start, destination))
                throw new InvalidOperationException($"World generation failed to connect {map.Name} at {destination}.");
        }
    }

    private static void CarveGuaranteedConnection(Map map, Point start, Point destination)
    {
        void Open(Point p)
        {
            if (!map.Contains(p) || p == destination) return;
            var tile = map.IsOutdoor
                ? map[p].Name.Contains("water", StringComparison.OrdinalIgnoreCase) ? Bridge : Road with { Name = "guaranteed connecting road" }
                : BiomeFloor(map.Biome);
            map.Set(p, tile);
            map.Doors.Remove(p);
            map.Elements[p.X, p.Y] = new ElementCell();
        }
        var cursor = start;
        while (cursor.X != destination.X)
        {
            cursor = cursor with { X = cursor.X + Math.Sign(destination.X - cursor.X) };
            Open(cursor);
        }
        while (cursor.Y != destination.Y)
        {
            cursor = cursor with { Y = cursor.Y + Math.Sign(destination.Y - cursor.Y) };
            Open(cursor);
        }
    }

    private static void AddDungeonProvisions(Map map, List<(int X, int Y, int W, int H)> rooms, Random random)
    {
        var candidates = rooms.Skip(1).Select(Center)
            .Where(position => map.Contains(position) && map[position].Walkable && position != map.StairsDown &&
                !map.Machines.ContainsKey(position) && !map.GroundItems.Any(item => item.Position == position))
            .OrderBy(_ => random.Next()).ToList();
        if (candidates.Count < 2) return;
        var foodName = map.Biome switch
        {
            BiomeKind.DrownedReliquary => "sealed reliquary larder",
            BiomeKind.PrismaticFoundry => "foundry workers' ration locker",
            BiomeKind.LightlessCourt => "edible duskcap colony",
            BiomeKind.CrownOfCinders => "charred root cache",
            _ => "ash-sealed provision chest"
        };
        var waterName = map.Biome switch
        {
            BiomeKind.DrownedReliquary => "clear reliquary cistern",
            BiomeKind.PrismaticFoundry => "crystal condensation basin",
            BiomeKind.LightlessCourt => "cold blackwater seep",
            BiomeKind.CrownOfCinders => "steam condenser",
            _ => "vault condensation jar"
        };
        AddProvision(map, candidates[0], ProvisionKind.Food, foodName, random.Next(1, 4));
        AddProvision(map, candidates[1], ProvisionKind.Water, waterName, random.Next(2, 5));
        map.GroundItems.Add(new GroundItem(candidates.Count > 2 ? candidates[2] : candidates[0], MakeItem(ItemKind.Ration)));
        map.GroundItems.Add(new GroundItem(candidates.Count > 3 ? candidates[3] : candidates[1], MakeItem(ItemKind.WaterFlask)));
    }

    private static void AddProvision(Map map, Point position, ProvisionKind kind, string name, int charges)
    {
        map.Provisions[position] = new ProvisionSource { Position = position, Kind = kind, Name = name, Charges = charges };
        map.Set(position, kind == ProvisionKind.Food ? FoodSupply : WaterSupply);
    }

    private static void AddSpecialRooms(Map map, List<(int X, int Y, int W, int H)> rooms, Random random)
    {
        if (rooms.Count < 12) return;
        var chosen = new[] { 3, 7, 11, Math.Min(15, rooms.Count - 2) }.Distinct().ToArray();
        for (var kind = 0; kind < chosen.Length; kind++)
        {
            var room = rooms[chosen[kind]];
            switch (kind)
            {
                case 0:
                    map.SpecialRoomNames.Add("prismatic crystal grotto");
                    PaintRoom(map, room, CrystalFloor);
                    for (var n = 0; n < 4; n++)
                    {
                        var p = new Point(random.Next(room.X + 1, room.X + room.W - 1), random.Next(room.Y + 1, room.Y + room.H - 1));
                        map.Set(p, new Tile('*', new Rgb(92, 196, 255), new Rgb(5, 13, 25), false, true, "resonant crystal", true, 6, 0.65));
                        map.Lights.Add(new LightSource(p, new Rgb(45, 135, 255), 8, 1.15, 0.05, n, 0.5, 0.3, 0.2));
                    }
                    map.GroundItems.Add(new GroundItem(Center(room), MakeItem(ItemKind.Crystal)));
                    break;
                case 1:
                    map.SpecialRoomNames.Add("drowned moon shrine");
                    PaintRoom(map, room, ShallowWater);
                    var altar = Center(room);
                    map.Set(altar, new Tile('_', new Rgb(180, 215, 240), new Rgb(6, 15, 25), true, true, "moon altar", false, 0, 0.55));
                    map.Lights.Add(new LightSource(altar, new Rgb(110, 185, 255), 11, 1.35, 0.02, 4, 0.6, 0.35, 0.24));
                    map.GroundItems.Add(new GroundItem(altar, MakeItem(ItemKind.ManaTonic)));
                    break;
                case 2:
                    map.SpecialRoomNames.Add("collapsed royal armory");
                    for (var n = 0; n < 9; n++)
                    {
                        var p = new Point(random.Next(room.X, room.X + room.W), random.Next(room.Y, room.Y + room.H));
                        map.Set(p, n < 3 ? CrackedWall : Rubble);
                    }
                    map.GroundItems.Add(new GroundItem(Center(room), MakeItem(ItemKind.Weapon)));
                    break;
                default:
                    map.SpecialRoomNames.Add("garden of whispering fungi");
                    PaintRoom(map, room, Moss);
                    for (var n = 0; n < 5; n++)
                    {
                        var p = new Point(random.Next(room.X + 1, room.X + room.W - 1), random.Next(room.Y + 1, room.Y + room.H - 1));
                        map.Lights.Add(new LightSource(p, new Rgb(70, 255, 125), 5, 0.55, 0.12, n * 2.2, 0.45, 0.18, 0.15));
                    }
                    map.GroundItems.Add(new GroundItem(Center(room), MakeItem(ItemKind.Ration)));
                    break;
            }
        }
        map.Set(map.StairsUp, Up);
        map.Set(map.StairsDown, Down);
    }

    public static BiomeKind BiomeForDepth(int depth) => Math.Clamp(depth, 1, 5) switch
    {
        1 => BiomeKind.AshenVaults,
        2 => BiomeKind.DrownedReliquary,
        3 => BiomeKind.PrismaticFoundry,
        4 => BiomeKind.LightlessCourt,
        _ => BiomeKind.CrownOfCinders
    };

    public static string BiomeName(BiomeKind biome) => biome switch
    {
        BiomeKind.AshenVaults => "Ashen Vaults",
        BiomeKind.DrownedReliquary => "Drowned Reliquary",
        BiomeKind.PrismaticFoundry => "Prismatic Foundry",
        BiomeKind.LightlessCourt => "Lightless Court",
        BiomeKind.CrownOfCinders => "Crown of Cinders",
        _ => "Emberfall"
    };

    private static Tile BiomeFloor(BiomeKind biome) => biome switch
    {
        BiomeKind.AshenVaults => AshFloor,
        BiomeKind.DrownedReliquary => DrownedFloor,
        BiomeKind.PrismaticFoundry => PrismFloor,
        BiomeKind.LightlessCourt => LightlessFloor,
        BiomeKind.CrownOfCinders => CinderFloor,
        _ => Floor
    };

    private static Tile BiomeWall(BiomeKind biome) => biome switch
    {
        BiomeKind.DrownedReliquary => DrownedWall,
        BiomeKind.PrismaticFoundry => PrismWall,
        BiomeKind.LightlessCourt => LightlessWall,
        BiomeKind.CrownOfCinders => CinderWall,
        _ => DungeonWall
    };

    private static void ApplyBiomeFeatures(Map map, List<(int X, int Y, int W, int H)> rooms, Random random)
    {
        if (map.Biome == BiomeKind.DrownedReliquary)
            for (var y = 0; y < map.Height; y++) for (var x = 0; x < map.Width; x++)
                if (map[new Point(x, y)].Walkable) map.Elements[x, y].Wet = 120;
        var candidates = rooms.Skip(1).SelectMany(room =>
            Enumerable.Range(room.X + 1, Math.Max(1, room.W - 2))
                .SelectMany(x => Enumerable.Range(room.Y + 1, Math.Max(1, room.H - 2)).Select(y => new Point(x, y))))
            .Where(p => map.Contains(p) && map[p].Walkable && p != map.StairsUp && p != map.StairsDown).ToList();
        var featureCount = Math.Min(candidates.Count, 34 + (int)map.Biome * 4);
        for (var i = 0; i < featureCount; i++)
        {
            var p = candidates[random.Next(candidates.Count)];
            var element = map.Elements[p.X, p.Y];
            switch (map.Biome)
            {
                case BiomeKind.AshenVaults:
                    if (i % 3 == 0) element.Oil = 60; else element.Smoke = 18;
                    break;
                case BiomeKind.DrownedReliquary:
                    element.Wet = 120;
                    if (i % 5 == 0) map.Set(p, ShallowWater);
                    break;
                case BiomeKind.PrismaticFoundry:
                    if (i % 4 == 0) element.Electric = 2;
                    else map.Set(p, CrystalFloor);
                    break;
                case BiomeKind.LightlessCourt:
                    if (i % 2 == 0) element.Web = 80; else element.Poison = 24;
                    if (i % 5 == 0) element.Smoke = 24;
                    break;
                case BiomeKind.CrownOfCinders:
                    if (i % 3 == 0) element.Fire = 10; else element.Oil = 70;
                    break;
            }
        }
    }

    private static void AddDoorsTrapsAndMachines(Map map, List<(int X, int Y, int W, int H)> rooms, Random random)
    {
        var chokePoints = new List<Point>();
        for (var y = 2; y < map.Height - 2; y++)
        for (var x = 2; x < map.Width - 2; x++)
        {
            var p = new Point(x, y);
            if (!map[p].Walkable || p.DistanceSquared(map.StairsUp) < 16 || p.DistanceSquared(map.StairsDown) < 9) continue;
            var horizontal = !map[new Point(x, y - 1)].Walkable && !map[new Point(x, y + 1)].Walkable &&
                map[new Point(x - 1, y)].Walkable && map[new Point(x + 1, y)].Walkable;
            var vertical = !map[new Point(x - 1, y)].Walkable && !map[new Point(x + 1, y)].Walkable &&
                map[new Point(x, y - 1)].Walkable && map[new Point(x, y + 1)].Walkable;
            if (horizontal || vertical) chokePoints.Add(p);
        }
        foreach (var p in chokePoints.OrderBy(_ => random.Next()).Take(9))
        {
            var roll = random.NextDouble();
            var state = roll < 0.18 ? DoorState.Secret : roll < 0.38 ? DoorState.Locked : DoorState.Closed;
            map.Doors[p] = new DoorFeature { Position = p, State = state };
            map.Set(p, state switch { DoorState.Locked => LockedDoor, DoorState.Secret => BiomeWall(map.Biome) with { Walkable = true, Name = "suspicious wall" }, _ => ClosedDoor });
        }

        var openFloor = rooms.Skip(1).SelectMany(room =>
            Enumerable.Range(room.X + 1, Math.Max(1, room.W - 2))
                .SelectMany(x => Enumerable.Range(room.Y + 1, Math.Max(1, room.H - 2)).Select(y => new Point(x, y))))
            .Where(p => map.Contains(p) && map[p].Walkable && !map.Doors.ContainsKey(p) && p != map.StairsDown).ToList();
        for (var i = 0; i < Math.Min(12, openFloor.Count); i++)
        {
            var p = openFloor[random.Next(openFloor.Count)];
            if (map.Traps.ContainsKey(p)) continue;
            var kinds = map.Biome switch
            {
                BiomeKind.DrownedReliquary or BiomeKind.PrismaticFoundry => new[] { TrapKind.ShockPlate, TrapKind.Alarm, TrapKind.Collapse },
                BiomeKind.LightlessCourt => new[] { TrapKind.PoisonVent, TrapKind.Alarm, TrapKind.Collapse },
                _ => new[] { TrapKind.FlameJet, TrapKind.Alarm, TrapKind.Collapse }
            };
            map.Traps[p] = new TrapFeature { Position = p, Kind = kinds[random.Next(kinds.Length)] };
        }

        var machineKind = map.Biome switch
        {
            BiomeKind.DrownedReliquary => MachineKind.Floodgate,
            BiomeKind.PrismaticFoundry => MachineKind.Prism,
            BiomeKind.LightlessCourt => MachineKind.Generator,
            _ => MachineKind.Brazier
        };
        foreach (var room in rooms.Skip(2).Where((_, index) => index % 5 == 0).Take(4))
        {
            var p = Center(room);
            if (p == map.StairsDown || map.Doors.ContainsKey(p)) continue;
            map.Machines[p] = new MachineFeature { Position = p, Kind = machineKind };
            var tile = machineKind switch { MachineKind.Floodgate => Floodgate, MachineKind.Prism => PrismMachine, MachineKind.Generator => Generator, _ => Brazier };
            map.Set(p, tile);
            if (machineKind is MachineKind.Brazier or MachineKind.Prism or MachineKind.Generator)
            {
                var color = machineKind == MachineKind.Prism ? new Rgb(105, 155, 255) : machineKind == MachineKind.Generator ? new Rgb(220, 190, 75) : new Rgb(255, 78, 18);
                map.Lights.Add(new LightSource(p, color, 9, 1.2, machineKind == MachineKind.Brazier ? 0.18 : 0.04, random.NextDouble() * 10));
            }
        }
        map.Set(map.StairsUp, Up);
        map.Set(map.StairsDown, Down);
    }

    private static HashSet<string> BiomeMonsterNames(BiomeKind biome) => biome switch
    {
        BiomeKind.AshenVaults => ["cave rat", "candle kobold", "gutter goblin", "ember skeleton", "ash cultist", "slag orc", "cinder salamander", "cinder mage"],
        BiomeKind.DrownedReliquary => ["cave rat", "ochre slime", "glassfang spider", "crystal myconid", "slag orc", "tunnel troll", "void whisper"],
        BiomeKind.PrismaticFoundry => ["glassfang spider", "crystal myconid", "vault gargoyle", "cinder mage", "hungry mimic", "vault warden"],
        BiomeKind.LightlessCourt => ["blind cave bat", "glassfang spider", "ash cultist", "vault gargoyle", "umbra hound", "hungry mimic", "void whisper", "crown lich"],
        _ => ["ember skeleton", "ash cultist", "slag orc", "cinder salamander", "cinder mage", "tunnel troll", "vault warden", "crown lich", "furnace demon"]
    };

    private static void ConfigureMonster(Actor actor)
    {
        var name = actor.Name;
        if (name.Contains("rat") || name.Contains("bat")) { actor.Faction = MonsterFaction.Vermin; actor.Senses = MonsterSense.Noise | MonsterSense.Vibration; }
        else if (name.Contains("kobold") || name.Contains("goblin") || name.Contains("orc")) { actor.Faction = MonsterFaction.Goblinoid; actor.CanOpenDoors = true; actor.Senses = MonsterSense.Sight | MonsterSense.Noise; }
        else if (name.Contains("skeleton") || name.Contains("lich")) { actor.Faction = MonsterFaction.Undead; actor.CanOpenDoors = name.Contains("lich"); actor.Senses = MonsterSense.Sight | MonsterSense.Light; actor.LightSensitive = true; }
        else if (name.Contains("cultist") || name.Contains("mage")) { actor.Faction = MonsterFaction.Cult; actor.CanOpenDoors = true; actor.Senses = MonsterSense.Sight | MonsterSense.Noise; }
        else if (name.Contains("warden") || name.Contains("gargoyle")) { actor.Faction = MonsterFaction.Construct; actor.Senses = MonsterSense.Sight | MonsterSense.Light; actor.LightSensitive = name.Contains("gargoyle"); }
        else if (name.Contains("whisper") || name.Contains("hound")) { actor.Faction = MonsterFaction.Void; actor.Senses = MonsterSense.Noise | MonsterSense.Scent | MonsterSense.Light; actor.LightSensitive = true; }
        else if (name.Contains("demon") || name.Contains("salamander")) { actor.Faction = MonsterFaction.Infernal; actor.Senses = MonsterSense.Sight | MonsterSense.Light; actor.AttractedToLight = true; actor.CanOpenDoors = name.Contains("demon"); }
        else
        {
            actor.Faction = MonsterFaction.Wild;
            actor.Senses = name.Contains("spider") ? MonsterSense.Vibration | MonsterSense.Sight : MonsterSense.Sight | MonsterSense.Scent;
            actor.LightSensitive = name.Contains("spider");
        }
        actor.HomePosition = actor.Position;
        actor.TerritoryRadius = actor.Ai switch { MonsterAi.Lurker => 7, MonsterAi.Brute => 9, MonsterAi.Skirmisher => 13, _ => 11 };
        actor.ActiveAtNight = name.Contains("wolf") || name.Contains("spider") || name.Contains("bat") ||
            name.Contains("hound") || name.Contains("whisper") || name.Contains("undead") || name.Contains("skeleton") || name.Contains("lich");
        actor.ActiveByDay = !name.Contains("bat") && !name.Contains("whisper") && !name.Contains("hound");
    }

    private static void PaintRoom(Map map, (int X, int Y, int W, int H) room, Tile tile)
    {
        for (var y = room.Y; y < room.Y + room.H; y++)
        for (var x = room.X; x < room.X + room.W; x++) map.Set(new Point(x, y), tile);
    }

    public static ItemStack MakeItem(ItemKind kind) => kind switch
    {
        ItemKind.HealingPotion => new ItemStack { Kind = kind, Name = "healing potion", Description = "Restores 14 HP", Glyph = '!', Color = new Rgb(245, 58, 76) },
        ItemKind.GreaterHealingPotion => new ItemStack { Kind = kind, Name = "greater healing elixir", Description = "Restores at least 30 HP", Glyph = '!', Color = new Rgb(255, 35, 92) },
        ItemKind.Ration => new ItemStack { Kind = kind, Name = "trail ration", Description = "Restores 38 food", Glyph = '%', Color = new Rgb(196, 151, 82) },
        ItemKind.WaterFlask => new ItemStack { Kind = kind, Name = "water flask", Description = "Restores 45 water", Glyph = '!', Color = new Rgb(72, 169, 245) },
        ItemKind.ManaTonic => new ItemStack { Kind = kind, Name = "mana tonic", Description = "Restores 8 mana", Glyph = '!', Color = new Rgb(152, 89, 255) },
        ItemKind.Bomb => new ItemStack { Kind = kind, Name = "alchemist bomb", Description = "Directional explosive; destroys terrain", Glyph = '*', Color = new Rgb(255, 112, 30) },
        ItemKind.Torch => new ItemStack { Kind = kind, Name = "wall torch", Description = "Use, then choose an adjacent wall to mount it", Glyph = '/', Color = new Rgb(255, 148, 45) },
        ItemKind.MightElixir => new ItemStack { Kind = kind, Name = "dragonroot elixir", Description = "+4 attack for 20 turns", Glyph = '!', Color = new Rgb(255, 92, 30) },
        ItemKind.GuardElixir => new ItemStack { Kind = kind, Name = "ironhide elixir", Description = "+4 defense for 20 turns", Glyph = '!', Color = new Rgb(160, 180, 195) },
        ItemKind.FocusElixir => new ItemStack { Kind = kind, Name = "focus elixir", Description = "+4 spell power for 20 turns", Glyph = '!', Color = new Rgb(190, 85, 255) },
        ItemKind.SmokeBomb => new ItemStack { Kind = kind, Name = "silver smoke bomb", Description = "Disorients nearby monsters", Glyph = '*', Color = new Rgb(176, 185, 195) },
        ItemKind.Waystone => new ItemStack { Kind = kind, Name = "waystone", Description = "Teleports to the upward stair", Glyph = '◇', Color = new Rgb(90, 150, 255) },
        ItemKind.Ambrosia => new ItemStack { Kind = kind, Name = "sun-fruit ambrosia", Description = "Fully restores food, water and mana", Glyph = '%', Color = new Rgb(255, 205, 55) },
        ItemKind.BeastHide => new ItemStack { Kind = kind, Name = "treated beast hide", Description = "Crafting material from wilderness beasts", Glyph = '≈', Color = new Rgb(154, 116, 78) },
        ItemKind.VenomGland => new ItemStack { Kind = kind, Name = "venom gland", Description = "Reactive alchemical crafting material", Glyph = '●', Color = new Rgb(96, 205, 92) },
        ItemKind.GoblinScrap => new ItemStack { Kind = kind, Name = "goblin iron scrap", Description = "Salvage used for bombs and field gear", Glyph = '⌁', Color = new Rgb(151, 145, 121) },
        ItemKind.RelicShard => new ItemStack { Kind = kind, Name = "relic shard", Description = "Old charged material used in advanced crafting", Glyph = '◇', Color = new Rgb(112, 174, 225) },
        ItemKind.BossRelic => new ItemStack { Kind = kind, Name = "boss relic", Description = "A singular trophy accepted by master crafters", Glyph = '◆', Color = new Rgb(255, 196, 82) },
        ItemKind.Weapon => EquipmentFactory.ArmoryWeapon(),
        ItemKind.Armor => EquipmentFactory.ArmoryArmor(),
        _ => new ItemStack { Kind = ItemKind.Crystal, Name = "prismatic crystal", Description = "Grants 30 XP and restores mana", Glyph = '*', Color = new Rgb(85, 205, 255) }
    };

    private static ItemStack RandomLoot(Random random)
    {
        var choices = new[] { ItemKind.HealingPotion, ItemKind.GreaterHealingPotion, ItemKind.Ration, ItemKind.WaterFlask,
            ItemKind.ManaTonic, ItemKind.Bomb, ItemKind.SmokeBomb, ItemKind.Armor, ItemKind.Torch, ItemKind.MightElixir,
            ItemKind.GuardElixir, ItemKind.FocusElixir, ItemKind.Waystone, ItemKind.Ambrosia };
        return MakeItem(choices[random.Next(choices.Length)]);
    }

    private static Point Center((int X, int Y, int W, int H) r) => new(r.X + r.W / 2, r.Y + r.H / 2);

    private static void CarveHorizontal(Map map, int x1, int x2, int y)
    {
        for (var x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++) map.Set(new Point(x, y), BiomeFloor(map.Biome));
    }

    private static void CarveVertical(Map map, int y1, int y2, int x)
    {
        for (var y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++) map.Set(new Point(x, y), BiomeFloor(map.Biome));
    }
}
