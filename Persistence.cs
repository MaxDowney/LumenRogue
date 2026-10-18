using System.Text.Json;

namespace LumenRogue;

public sealed partial class Game
{
    public static string DefaultSavePath => Path.Combine(Environment.CurrentDirectory, "lumenrogue-save.json");

    public void RequestLoad()
    {
        if (!File.Exists(DefaultSavePath)) { AddMessage("No quick-save exists yet. Press F5 to create one."); return; }
        LoadRequested = true;
    }

    public void SaveGame(string path)
    {
        try
        {
            var state = new SaveState
            {
                Version = 1, Seed = WorldSeed, CurrentMap = CurrentMap.Name, Turns = _turns,
                Race = Race, Class = Class, Strength = Strength, Dexterity = Dexterity, Intellect = Intellect,
                Constitution = Constitution, Wisdom = Wisdom, Level = Level, Experience = Experience,
                Food = Food, Water = Water, Mana = Mana, MaxMana = MaxMana, SkillPoints = SkillPoints,
                LanternRadius = LanternRadius, LanternIntensity = LanternIntensity, LanternLit = LanternLit,
                BlastRadius = BlastRadius, InnateSpellPower = InnateSpellPower,
                WardTurns = WardTurns, MightTurns = MightTurns, GuardTurns = GuardTurns, FocusTurns = FocusTurns,
                Player = ActorData.From(Player), Inventory = Inventory.Select(ItemData.From).ToList(),
                LearnedSpells = _learnedSpells.ToList(), WorldFlags = WorldFlags.ToList(),
                DiscoveredLandmarks = DiscoveredLandmarks.ToList(), CompletedDungeons = CompletedDungeons.ToList(),
                LoreEntries = LoreEntries.ToList(), SkillRanks = SkillTree.ToDictionary(node => node.Id, node => node.Rank),
                Quests = Quests.Select(QuestData.From).ToList(), Maps = AllMaps().Select(map => MapData.From(map, _monstersByMap[map])).ToList()
            };
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            AddMessage($"Expedition saved: {Path.GetFileName(path)} · seed {WorldSeed}.");
        }
        catch (Exception exception) { AddMessage($"Save failed: {exception.Message}"); }
    }

    public static Game LoadGame(string path)
    {
        var state = JsonSerializer.Deserialize<SaveState>(File.ReadAllText(path)) ?? throw new InvalidDataException("Save file is empty.");
        if (state.Version != 1) throw new InvalidDataException($"Unsupported save version {state.Version}.");
        var game = new Game(skipCharacterCreation: true, worldSeed: state.Seed);
        game._turns = state.Turns;
        game.Race = state.Race; game.Class = state.Class;
        game.Strength = state.Strength; game.Dexterity = state.Dexterity; game.Intellect = state.Intellect;
        game.Constitution = state.Constitution; game.Wisdom = state.Wisdom;
        game.Level = state.Level; game.Experience = state.Experience; game.Food = state.Food; game.Water = state.Water;
        game.Mana = state.Mana; game.MaxMana = state.MaxMana; game.SkillPoints = state.SkillPoints;
        game.LanternRadius = state.LanternRadius; game.LanternIntensity = state.LanternIntensity;
        game.LanternLit = state.LanternLit; game.BlastRadius = state.BlastRadius; game.InnateSpellPower = state.InnateSpellPower;
        game.WardTurns = state.WardTurns; game.MightTurns = state.MightTurns; game.GuardTurns = state.GuardTurns; game.FocusTurns = state.FocusTurns;
        state.Player.Apply(game.Player);
        game.Inventory.Clear(); game.Inventory.AddRange(state.Inventory.Select(item => item.ToItem()));
        game._learnedSpells.Clear(); game._learnedSpells.AddRange(state.LearnedSpells);
        game.WorldFlags.Clear(); game.WorldFlags.UnionWith(state.WorldFlags);
        game.DiscoveredLandmarks.Clear(); game.DiscoveredLandmarks.UnionWith(state.DiscoveredLandmarks);
        game.CompletedDungeons.Clear(); game.CompletedDungeons.UnionWith(state.CompletedDungeons);
        game.LoreEntries.Clear(); game.LoreEntries.AddRange(state.LoreEntries);
        foreach (var node in game.SkillTree) node.Rank = state.SkillRanks.GetValueOrDefault(node.Id);
        foreach (var questData in state.Quests)
        {
            var quest = game.Quests.FirstOrDefault(candidate => candidate.Id == questData.Id);
            if (quest is null) continue;
            quest.Progress = questData.Progress; quest.Accepted = questData.Accepted; quest.TurnedIn = questData.TurnedIn;
        }
        var maps = game.AllMaps().ToDictionary(map => map.Name);
        foreach (var mapData in state.Maps)
            if (maps.TryGetValue(mapData.Name, out var map)) mapData.Apply(map, game._monstersByMap[map]);
        if (game.WorldFlags.Contains("bramblewick_safe")) game.AddRegionalLights(new Point(198, 30), new Rgb(255, 176, 72));
        if (game.WorldFlags.Contains("stonecross_restored"))
            for (var x = 70; x <= 185; x += 23)
                game._wilderness.Lights.Add(new LightSource(new Point(x, 60), new Rgb(255, 154, 60), 8, 0.9, 0.1, x));
        if (game.WorldFlags.Contains("valeward_secured")) game.AddRegionalLights(new Point(48, 94), new Rgb(255, 205, 110));
        game.CurrentMap = maps.GetValueOrDefault(state.CurrentMap, game._town);
        game.Mode = GameMode.Playing;
        game.UpdateNpcSchedules();
        game.RefreshDetectionState();
        game.AddMessage($"Expedition loaded · seed {game.WorldSeed} · {game.CurrentMap.Name} · {game.WorldTimeLabel}.");
        return game;
    }

    private sealed class SaveState
    {
        public int Version { get; set; }
        public int Seed { get; set; }
        public string CurrentMap { get; set; } = "Emberfall";
        public int Turns { get; set; }
        public RaceKind Race { get; set; }
        public ClassKind Class { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Intellect { get; set; }
        public int Constitution { get; set; }
        public int Wisdom { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int Food { get; set; }
        public int Water { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public int SkillPoints { get; set; }
        public double LanternRadius { get; set; }
        public double LanternIntensity { get; set; }
        public bool LanternLit { get; set; }
        public int BlastRadius { get; set; }
        public int InnateSpellPower { get; set; }
        public int WardTurns { get; set; }
        public int MightTurns { get; set; }
        public int GuardTurns { get; set; }
        public int FocusTurns { get; set; }
        public ActorData Player { get; set; } = new();
        public List<ItemData> Inventory { get; set; } = [];
        public List<SpellKind> LearnedSpells { get; set; } = [];
        public List<string> WorldFlags { get; set; } = [];
        public List<string> DiscoveredLandmarks { get; set; } = [];
        public List<string> CompletedDungeons { get; set; } = [];
        public List<string> LoreEntries { get; set; } = [];
        public Dictionary<string, int> SkillRanks { get; set; } = [];
        public List<QuestData> Quests { get; set; } = [];
        public List<MapData> Maps { get; set; } = [];
    }

    private sealed class QuestData
    {
        public string Id { get; set; } = "";
        public int Progress { get; set; }
        public bool Accepted { get; set; }
        public bool TurnedIn { get; set; }
        public static QuestData From(Quest quest) => new() { Id = quest.Id, Progress = quest.Progress, Accepted = quest.Accepted, TurnedIn = quest.TurnedIn };
    }

    private sealed class ItemData
    {
        public ItemKind Kind { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public char Glyph { get; set; }
        public Rgb Color { get; set; }
        public int Quantity { get; set; }
        public EquipmentSlot? Slot { get; set; }
        public ItemRarity Rarity { get; set; }
        public int AttackBonus { get; set; }
        public int DefenseBonus { get; set; }
        public int SpellBonus { get; set; }
        public double LightBonus { get; set; }
        public bool Equipped { get; set; }
        public static ItemData From(ItemStack item) => new() { Kind = item.Kind, Name = item.Name, Description = item.Description, Glyph = item.Glyph, Color = item.Color, Quantity = item.Quantity, Slot = item.Slot, Rarity = item.Rarity, AttackBonus = item.AttackBonus, DefenseBonus = item.DefenseBonus, SpellBonus = item.SpellBonus, LightBonus = item.LightBonus, Equipped = item.Equipped };
        public ItemStack ToItem() => new() { Kind = Kind, Name = Name, Description = Description, Glyph = Glyph, Color = Color, Quantity = Quantity, Slot = Slot, Rarity = Rarity, AttackBonus = AttackBonus, DefenseBonus = DefenseBonus, SpellBonus = SpellBonus, LightBonus = LightBonus, Equipped = Equipped };
    }

    private sealed class ActorData
    {
        public Point Position { get; set; }
        public char Glyph { get; set; }
        public Rgb Color { get; set; }
        public string Name { get; set; } = "";
        public int MaxHp { get; set; }
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Gold { get; set; }
        public int XpReward { get; set; }
        public MonsterAi Ai { get; set; }
        public int FrozenTurns { get; set; }
        public int PoisonTurns { get; set; }
        public int WebbedTurns { get; set; }
        public int AlertTurns { get; set; }
        public Point? LastKnownPlayer { get; set; }
        public MonsterFaction Faction { get; set; }
        public MonsterSense Senses { get; set; }
        public bool CanOpenDoors { get; set; }
        public bool LightSensitive { get; set; }
        public bool AttractedToLight { get; set; }
        public Rgb? GlowColor { get; set; }
        public bool IsBoss { get; set; }
        public string? UniqueRewardName { get; set; }
        public MonsterIntentKind Intent { get; set; }
        public Point HomePosition { get; set; }
        public int TerritoryRadius { get; set; }
        public bool ActiveAtNight { get; set; }
        public bool ActiveByDay { get; set; }
        public bool RewardsClaimed { get; set; }
        public static ActorData From(Actor actor) => new() { Position = actor.Position, Glyph = actor.Glyph, Color = actor.Color, Name = actor.Name, MaxHp = actor.MaxHp, Hp = actor.Hp, Attack = actor.Attack, Defense = actor.Defense, Gold = actor.Gold, XpReward = actor.XpReward, Ai = actor.Ai, FrozenTurns = actor.FrozenTurns, PoisonTurns = actor.PoisonTurns, WebbedTurns = actor.WebbedTurns, AlertTurns = actor.AlertTurns, LastKnownPlayer = actor.LastKnownPlayer, Faction = actor.Faction, Senses = actor.Senses, CanOpenDoors = actor.CanOpenDoors, LightSensitive = actor.LightSensitive, AttractedToLight = actor.AttractedToLight, GlowColor = actor.GlowColor, IsBoss = actor.IsBoss, UniqueRewardName = actor.UniqueRewardName, Intent = actor.Intent, HomePosition = actor.HomePosition, TerritoryRadius = actor.TerritoryRadius, ActiveAtNight = actor.ActiveAtNight, ActiveByDay = actor.ActiveByDay, RewardsClaimed = actor.RewardsClaimed };
        public Actor ToActor() { var actor = new Actor { Position = Position, Glyph = Glyph, Color = Color, Name = Name, MaxHp = MaxHp, Hp = Hp, Attack = Attack }; Apply(actor); return actor; }
        public void Apply(Actor actor) { actor.Position = Position; actor.Glyph = Glyph; actor.Color = Color; actor.Name = Name; actor.MaxHp = MaxHp; actor.Hp = Hp; actor.Attack = Attack; actor.Defense = Defense; actor.Gold = Gold; actor.XpReward = XpReward; actor.Ai = Ai; actor.FrozenTurns = FrozenTurns; actor.PoisonTurns = PoisonTurns; actor.WebbedTurns = WebbedTurns; actor.AlertTurns = AlertTurns; actor.LastKnownPlayer = LastKnownPlayer; actor.Faction = Faction; actor.Senses = Senses; actor.CanOpenDoors = CanOpenDoors; actor.LightSensitive = LightSensitive; actor.AttractedToLight = AttractedToLight; actor.GlowColor = GlowColor; actor.IsBoss = IsBoss; actor.UniqueRewardName = UniqueRewardName; actor.Intent = Intent; actor.HomePosition = HomePosition; actor.TerritoryRadius = TerritoryRadius; actor.ActiveAtNight = ActiveAtNight; actor.ActiveByDay = ActiveByDay; actor.RewardsClaimed = RewardsClaimed; }
    }

    private sealed class MapData
    {
        public string Name { get; set; } = "";
        public List<int> Explored { get; set; } = [];
        public List<ActorData> Monsters { get; set; } = [];
        public List<ItemDataAt> GroundItems { get; set; } = [];
        public Dictionary<string, int> ProvisionCharges { get; set; } = [];
        public Dictionary<string, DoorState> Doors { get; set; } = [];
        public Dictionary<string, bool> Machines { get; set; } = [];
        public static MapData From(Map map, List<Actor> monsters)
        {
            var data = new MapData { Name = map.Name };
            for (var y = 0; y < map.Height; y++) for (var x = 0; x < map.Width; x++) if (map.Explored[x, y]) data.Explored.Add(y * map.Width + x);
            data.GroundItems = map.GroundItems.Select(item => new ItemDataAt { Position = item.Position, Item = ItemData.From(item.Item) }).ToList();
            data.ProvisionCharges = map.Provisions.ToDictionary(entry => Key(entry.Key), entry => entry.Value.Charges);
            data.Doors = map.Doors.ToDictionary(entry => Key(entry.Key), entry => entry.Value.State);
            data.Machines = map.Machines.ToDictionary(entry => Key(entry.Key), entry => entry.Value.Active);
            data.Monsters = monsters.Select(ActorData.From).ToList();
            return data;
        }
        public void Apply(Map map, List<Actor> monsters)
        {
            foreach (var index in Explored) if (index >= 0 && index < map.Width * map.Height) map.Explored[index % map.Width, index / map.Width] = true;
            if (Monsters.Count > 0) { monsters.Clear(); monsters.AddRange(Monsters.Select(monster => monster.ToActor())); }
            map.GroundItems.Clear(); map.GroundItems.AddRange(GroundItems.Select(item => new GroundItem(item.Position, item.Item.ToItem())));
            foreach (var entry in map.Provisions) if (ProvisionCharges.TryGetValue(Key(entry.Key), out var charges)) entry.Value.Charges = charges;
            foreach (var entry in map.Doors)
            {
                if (!Doors.TryGetValue(Key(entry.Key), out var state)) continue;
                entry.Value.State = state;
                map.Set(entry.Key, state switch
                {
                    DoorState.Open => WorldBuilder.OpenDoor,
                    DoorState.Broken => WorldBuilder.OpenDoor with { Glyph = '░', Name = "broken doorway" },
                    DoorState.Locked => WorldBuilder.LockedDoor,
                    _ => WorldBuilder.ClosedDoor
                });
            }
            foreach (var entry in map.Machines) if (Machines.TryGetValue(Key(entry.Key), out var active)) entry.Value.Active = active;
        }
        public static string Key(Point point) => $"{point.X},{point.Y}";
    }

    private sealed class ItemDataAt { public Point Position { get; set; } public ItemData Item { get; set; } = new(); }
}
