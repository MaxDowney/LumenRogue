namespace LumenRogue;

public enum GameMode { CharacterCreation, Playing, Help, Shopping, Inventory, Stats, Spellbook, Map, Dialogue, QuestLog, Inspect, Targeting, SkillTree, FastTravel, Crafting, Lore, QuitConfirm, GameOver }

public sealed partial class Game
{
    private readonly Random _random;
    private readonly Map _town;
    private readonly Map _wilderness;
    private readonly List<Map> _dungeons = [];
    private readonly Dictionary<TravelKind, Map> _siteDungeons = [];
    private readonly Dictionary<Map, List<Actor>> _monstersByMap = [];
    private readonly Queue<string> _messages = new();
    private bool _pendingEnemyTurn;
    private int _turns;
    private int _creationStage;
    private RaceKind _chosenRace = RaceKind.Human;
    private SpellKind? _pendingSpell;
    private bool _pendingBomb;
    private bool _pendingWallTorch;
    private readonly List<SpellKind> _learnedSpells = [];
    private GameMode _modeBeforeQuit = GameMode.Playing;
    private Point _noiseOrigin;
    private double _visualNow;
    private Point _wildernessReturnPosition;

    public Map CurrentMap { get; private set; }
    public List<Actor> Monsters => _monstersByMap.GetValueOrDefault(CurrentMap, []);
    public List<VisualEffect> Effects { get; } = [];
    public List<ItemStack> Inventory { get; } = [];
    public Actor Player { get; }
    public GameMode Mode { get; private set; }
    public Shop? ActiveShop { get; private set; }
    public bool AwaitingDirection { get; private set; }
    public bool QuitRequested { get; private set; }
    public RaceKind Race { get; private set; } = RaceKind.Human;
    public ClassKind Class { get; private set; } = ClassKind.Vanguard;
    public int Strength { get; private set; } = 10;
    public int Dexterity { get; private set; } = 10;
    public int Intellect { get; private set; } = 10;
    public int Constitution { get; private set; } = 10;
    public int Wisdom { get; private set; } = 10;
    public int InnateSpellPower { get; private set; }
    public int WardTurns { get; private set; }
    public int MightTurns { get; private set; }
    public int GuardTurns { get; private set; }
    public int FocusTurns { get; private set; }
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int ExperienceToLevel => 45 + (Level - 1) * 40;
    public int Food { get; private set; } = 100;
    public int Water { get; private set; } = 100;
    public int Mana { get; set; }
    public int MaxMana { get; private set; } = 6;
    public double LanternRadius { get; set; } = 8.5;
    public double LanternIntensity { get; set; } = 1.0;
    public bool LanternLit { get; private set; } = true;
    public int BlastRadius { get; set; } = 2;
    public int TurnCount => _turns;
    public int WorldSeed { get; }
    public const int MinutesPerTurn = 6;
    public const int TurnsPerDay = 1440 / MinutesPerTurn;
    public int WorldMinutes => (7 * 60 + _turns * MinutesPerTurn) % 1440;
    public string WorldTimeLabel => $"{WorldMinutes / 60:00}:{WorldMinutes % 60:00}";
    public double OutdoorDaylight
    {
        get
        {
            var phase = WorldMinutes / 1440.0;
            var solarAltitude = Math.Sin((phase - 0.25) * Math.PI * 2);
            return Math.Clamp((solarAltitude + 0.1) / 1.1, 0, 1);
        }
    }
    public double OutdoorAmbientLight => 0.05 + Math.Pow(OutdoorDaylight, 0.72) * 0.82;
    public string OutdoorPeriod => WorldMinutes switch
    {
        < 300 => "deep night",
        < 420 => "dawn",
        < 660 => "morning",
        < 1020 => "day",
        < 1140 => "dusk",
        _ => "night"
    };
    public string LatestMessage => _messages.Count == 0 ? "" : _messages.Last();
    public bool Busy => Effects.Any(e => e is ProjectileEffect or ExplosionEffect or ArcEffect);
    public int Potions => Inventory.Where(i => i.Kind == ItemKind.HealingPotion).Sum(i => i.Quantity);
    public int CreationStage => _creationStage;
    public RaceKind ChosenRace => _chosenRace;
    public int CurrentDepth => CurrentMap.IsOutdoor ? 0 : IsSiteDungeon ? 1 : _dungeons.IndexOf(CurrentMap) + 1;
    public int DungeonDepthCount => _dungeons.Count;
    public bool IsWilderness => CurrentMap == _wilderness;
    public bool IsSiteDungeon => _siteDungeons.Values.Contains(CurrentMap);
    public int SiteDungeonCount => _siteDungeons.Count;
    public int MenuSelection { get; private set; }
    public int TotalAttack => Player.Attack + Inventory.Where(i => i.Equipped).Sum(i => i.AttackBonus) + (MightTurns > 0 ? 4 : 0);
    public int TotalDefense => Player.Defense + Inventory.Where(i => i.Equipped).Sum(i => i.DefenseBonus) + (WardTurns > 0 ? 4 : 0) + (GuardTurns > 0 ? 4 : 0);
    public int SpellPower => InnateSpellPower + Inventory.Where(i => i.Equipped).Sum(i => i.SpellBonus) + (FocusTurns > 0 ? 4 : 0);
    public double EffectiveLanternRadius => LanternRadius + Inventory.Where(i => i.Equipped).Sum(i => i.LightBonus);
    public Point? CursorPosition { get; private set; }
    public string CursorDescription => CursorPosition is { } p ? DescribeAt(p) : "";
    public List<SkillNode> SkillTree { get; private set; } = [];
    public int SkillPoints { get; private set; }
    public const int SkillBranchCount = 4;
    public const int SkillTierCount = 6;
    public int NoiseLevel { get; private set; }
    public string DetectionState { get; private set; } = "HIDDEN";
    public double PlayerExposure => GameplayLightAt(Player.Position);
    public int ControlGraceTurns { get; private set; }
    public int ElectricRecoveryTurns { get; internal set; }
    public PlayerReactionKind? PreparedReaction { get; private set; }
    public bool LoadRequested { get; set; }
    public List<Quest> Quests { get; } = [];
    public HashSet<string> WorldFlags { get; } = [];
    public HashSet<string> DiscoveredLandmarks { get; } = [];
    public HashSet<string> CompletedDungeons { get; } = [];
    public List<string> LoreEntries { get; } = [];
    public TownNpc? ActiveNpc { get; private set; }
    public IReadOnlyList<Quest> JournalQuests => Quests.Where(quest => quest.Accepted)
        .OrderBy(quest => quest.Status == QuestStatus.ReadyToTurnIn ? 0 : quest.Status == QuestStatus.Active ? 1 : 2)
        .ThenBy(quest => quest.Title).ToList();

    public Game(bool skipCharacterCreation = false, int? worldSeed = null)
    {
        WorldSeed = worldSeed ?? 94117;
        _random = new Random(DerivedSeed(1));
        _town = WorldBuilder.BuildTown(DerivedSeed(11));
        _monstersByMap[_town] = [];
        var (wilderness, wildernessMonsters) = WorldBuilder.BuildWilderness(DerivedSeed(23));
        _wilderness = wilderness;
        _monstersByMap[_wilderness] = wildernessMonsters;
        var siteDefinitions = new[]
        {
            (TravelKind.HollowrootMine, "Hollowroot Mine", DerivedSeed(101), 1),
            (TravelKind.SunkenBarrow, "Sunken Barrow", DerivedSeed(211), 2),
            (TravelKind.MoonwellCavern, "Moonwell Cavern", DerivedSeed(307), 3),
            (TravelKind.CastleUndercroft, "Castle Valeward Undercroft", DerivedSeed(401), 4)
        };
        foreach (var definition in siteDefinitions)
        {
            var (site, siteMonsters) = WorldBuilder.BuildSiteDungeon(definition.Item2, definition.Item3, definition.Item4);
            _siteDungeons[definition.Item1] = site;
            _monstersByMap[site] = siteMonsters;
        }
        for (var depth = 1; depth <= 5; depth++)
        {
            var (map, monsters) = WorldBuilder.BuildDungeon(DerivedSeed(700 + depth * 37), depth);
            _dungeons.Add(map);
            _monstersByMap[map] = monsters;
        }
        CurrentMap = _town;
        InitializeQuests();
        InitializeNpcSchedules();
        Player = new Actor
        {
            Position = _town.Start, Glyph = '@', Name = "wayfarer", Color = new Rgb(245, 239, 211),
            MaxHp = 30, Hp = 30, Attack = 5, Defense = 1, Gold = 32
        };
        if (skipCharacterCreation) ApplyCharacter(RaceKind.Human, ClassKind.Vanguard);
        else
        {
            Mode = GameMode.CharacterCreation;
            AddMessage("Choose the ancestry whose light you carry.");
        }
    }

    private int DerivedSeed(int salt)
    {
        unchecked
        {
            var value = (uint)WorldSeed;
            value ^= (uint)salt * 0x9E3779B9u;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            return (int)(value & 0x7fffffff);
        }
    }

    private IEnumerable<Map> AllMaps()
    {
        yield return _town;
        yield return _wilderness;
        foreach (var map in _dungeons) yield return map;
        foreach (var map in _siteDungeons.Values) yield return map;
    }

    public void HandleKey(ConsoleKeyInfo key, double now)
    {
        _visualNow = now;
        if (Mode == GameMode.QuitConfirm) { HandleQuitConfirmation(key); return; }
        if (key.Key == ConsoleKey.Q && (key.Modifiers & ConsoleModifiers.Control) != 0) { BeginQuitConfirmation(); return; }
        if (Mode == GameMode.CharacterCreation) { HandleCreationKey(key); return; }
        if (Mode == GameMode.Help) { Mode = GameMode.Playing; return; }
        if (Mode == GameMode.GameOver) { if (key.Key is ConsoleKey.Q or ConsoleKey.Escape) BeginQuitConfirmation(); return; }
        if (Mode == GameMode.Shopping) { HandleShopKey(key); return; }
        if (Mode == GameMode.Inventory) { HandleInventoryKey(key, now); return; }
        if (Mode == GameMode.Stats) { HandleCharacterKey(key); return; }
        if (Mode == GameMode.Spellbook) { HandleSpellbookKey(key, now); return; }
        if (Mode == GameMode.Map) { if (key.Key is ConsoleKey.M or ConsoleKey.Escape or ConsoleKey.Q) Mode = GameMode.Playing; return; }
        if (Mode == GameMode.Dialogue) { HandleDialogueKey(key); return; }
        if (Mode == GameMode.QuestLog) { HandleQuestLogKey(key); return; }
        if (Mode == GameMode.Inspect) { HandleCursorKey(key, now, false); return; }
        if (Mode == GameMode.Targeting) { HandleCursorKey(key, now, true); return; }
        if (Mode == GameMode.SkillTree) { HandleSkillTreeKey(key); return; }
        if (Mode == GameMode.FastTravel) { HandleFastTravelKey(key); return; }
        if (Mode == GameMode.Crafting) { HandleCraftingKey(key); return; }
        if (Mode == GameMode.Lore) { HandleLoreKey(key); return; }

        if (key.Key is ConsoleKey.F1 or ConsoleKey.Oem2) { Mode = GameMode.Help; CancelAiming(); return; }
        if (key.Key == ConsoleKey.Escape)
        {
            if (AwaitingDirection) { CancelAiming(); AddMessage("Aim cancelled."); }
            else BeginQuitConfirmation();
            return;
        }
        if (Busy) return;
        if (Player.FrozenTurns > 0)
        {
            // Any hard control effect can consume at most one player action.
            // Clearing the full stack here prevents several sources from
            // queuing a run of turns where the player cannot respond.
            Player.FrozenTurns = 0;
            ControlGraceTurns = Math.Max(ControlGraceTurns, 3);
            CommitTurn("You shake off the shock. Control effects cannot catch you again for two turns.");
            return;
        }

        var direction = DirectionFromKey(key);
        if (AwaitingDirection)
        {
            if (direction is { } aim)
            {
                if (_pendingBomb) ThrowBomb(aim, now);
                else if (_pendingWallTorch) PlaceWallTorch(aim);
            }
            return;
        }
        if (direction is { } move) { TryMove(move, now); return; }

        switch (key.Key)
        {
            case ConsoleKey.F: BeginSpell(KnownSpells.FirstOrDefault(), now); break;
            case ConsoleKey.Z: Mode = GameMode.Spellbook; MenuSelection = 0; break;
            case ConsoleKey.I: Mode = GameMode.Inventory; MenuSelection = 0; break;
            case ConsoleKey.C: Mode = GameMode.Stats; MenuSelection = 0; break;
            case ConsoleKey.M: Mode = GameMode.Map; break;
            case ConsoleKey.F2: OpenFastTravel(); break;
            case ConsoleKey.F3: OpenCrafting(); break;
            case ConsoleKey.F4: OpenLore(); break;
            case ConsoleKey.F5: SaveGame(DefaultSavePath); break;
            case ConsoleKey.F9: RequestLoad(); break;
            case ConsoleKey.F6: CycleReaction(); break;
            case ConsoleKey.Tab: OpenQuestLog(); break;
            case ConsoleKey.O: ToggleLantern(); break;
            case ConsoleKey.X: BeginInspect(); break;
            case ConsoleKey.V: SearchSurroundings(); break;
            case ConsoleKey.T: OpenSkillTree(); break;
            case ConsoleKey.G: PickUpItem(); break;
            case ConsoleKey.P: UseFirst(ItemKind.HealingPotion, now); break;
            case ConsoleKey.R: Rest(); break;
            case ConsoleKey.E:
            case ConsoleKey.Enter: Interact(); break;
            case ConsoleKey.Spacebar:
            case ConsoleKey.OemPeriod: CommitTurn("You listen to the fire breathe."); break;
            case ConsoleKey.Q: BeginQuitConfirmation(); break;
        }
    }

    private void BeginQuitConfirmation()
    {
        _modeBeforeQuit = Mode;
        Mode = GameMode.QuitConfirm;
        MenuSelection = 0; // Leaving is destructive, so default to the safe choice.
    }

    private void HandleQuitConfirmation(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow or ConsoleKey.A or ConsoleKey.D or ConsoleKey.H or ConsoleKey.L or ConsoleKey.Tab)
        { MenuSelection = 1 - MenuSelection; return; }
        if (key.Key == ConsoleKey.Y) { QuitRequested = true; return; }
        if (key.Key is ConsoleKey.N or ConsoleKey.Escape or ConsoleKey.Q)
        { Mode = _modeBeforeQuit; MenuSelection = 0; return; }
        if (key.Key is not ConsoleKey.Enter and not ConsoleKey.Spacebar) return;
        if (MenuSelection == 1) QuitRequested = true;
        else { Mode = _modeBeforeQuit; MenuSelection = 0; }
    }

    private void HandleCreationKey(ConsoleKeyInfo key)
    {
        var selection = NumericSelection(key);
        if (selection is < 0 or > 3) return;
        if (_creationStage == 0)
        {
            _chosenRace = (RaceKind)selection;
            _creationStage = 1;
            AddMessage($"{_chosenRace} blood. Now choose a calling.");
        }
        else ApplyCharacter(_chosenRace, (ClassKind)selection);
    }

    private void ApplyCharacter(RaceKind race, ClassKind chosenClass)
    {
        Race = race;
        Class = chosenClass;
        Strength = Dexterity = Intellect = Constitution = Wisdom = 10;
        switch (race)
        {
            case RaceKind.Human: Strength++; Dexterity++; Intellect++; Constitution++; Wisdom++; break;
            case RaceKind.Dwarf: Strength += 2; Constitution += 4; Dexterity--; break;
            case RaceKind.Elf: Dexterity += 3; Intellect += 3; Constitution--; break;
            case RaceKind.Ashborn: Intellect += 2; Wisdom += 2; Constitution += 2; LanternIntensity += 0.15; break;
        }
        switch (chosenClass)
        {
            case ClassKind.Vanguard: Strength += 4; Constitution += 3; Player.Attack = 7; Player.Defense = 3; MaxMana = 7; break;
            case ClassKind.Arcanist: Intellect += 5; Wisdom += 2; Player.Attack = 3; Player.Defense = 0; MaxMana = 18; break;
            case ClassKind.Ranger: Dexterity += 5; Wisdom += 2; Player.Attack = 6; Player.Defense = 1; MaxMana = 11; LanternRadius += 1; break;
            case ClassKind.Lightbearer: Wisdom += 5; Constitution += 2; Player.Attack = 5; Player.Defense = 2; MaxMana = 14; LanternIntensity += 0.18; break;
        }
        Player.MaxHp = 20 + Constitution + (chosenClass == ClassKind.Vanguard ? 8 : 0);
        Player.Hp = Player.MaxHp;
        Mana = MaxMana;
        Mode = GameMode.Playing;
        AddItem(ItemKind.HealingPotion, 2, false);
        AddItem(ItemKind.Ration, 2, false);
        AddItem(ItemKind.WaterFlask, 2, false);
        if (chosenClass == ClassKind.Arcanist) AddItem(ItemKind.ManaTonic, 1, false);
        if (chosenClass == ClassKind.Vanguard) AddItem(ItemKind.Bomb, 1, false);
        var starterGear = chosenClass switch
        {
            ClassKind.Vanguard => EquipmentFactory.Create("militia broadsword", EquipmentSlot.MainHand, '/', attack: 2, description: "A dependable broad Emberfall blade"),
            ClassKind.Arcanist => EquipmentFactory.Create("apprentice emberstaff", EquipmentSlot.MainHand, '/', spell: 2, light: 0.2, description: "A copper-bound focus for young spellcasters"),
            ClassKind.Ranger => EquipmentFactory.Create("ashwood shortbow", EquipmentSlot.MainHand, '}', attack: 2, description: "Compact enough for narrow passages"),
            _ => EquipmentFactory.Create("dawnwatch mace", EquipmentSlot.MainHand, '†', attack: 1, spell: 1, light: 0.25, description: "A ceremonial weapon with a practical weight")
        };
        starterGear.Equipped = true;
        Inventory.Add(starterGear);
        _learnedSpells.Clear();
        _learnedSpells.AddRange(chosenClass switch
        {
            ClassKind.Vanguard => [SpellKind.Stonebreak, SpellKind.ShieldFlare],
            ClassKind.Arcanist => [SpellKind.ArcaneBeam, SpellKind.Fireball],
            ClassKind.Ranger => [SpellKind.ThornVolley, SpellKind.ShadowBolt],
            _ => [SpellKind.SunLance, SpellKind.HealingLight]
        });
        SkillTree = BuildSkillTree(chosenClass);
        RefreshDetectionState();
        DiscoverNearbyLandmarks();
        AddMessage($"A {Race.ToString().ToLowerInvariant()} {Class.ToString().ToLowerInvariant()} enters Emberfall. Your light is your own.");
    }

    public void UpdateEffects(double now)
    {
        _visualNow = now;
        foreach (var projectile in Effects.OfType<ProjectileEffect>().Where(p => !p.Resolved && p.IsFinished(now)).ToList())
        {
            projectile.Resolved = true;
            ResolveProjectile(projectile, now);
        }
        Effects.RemoveAll(effect => effect.IsFinished(now));
        if (!Busy && _pendingEnemyTurn)
        {
            _pendingEnemyTurn = false;
            AdvanceEnvironment(now);
            if (Player.IsAlive) AdvanceMonsters(now);
            EndTurnMaintenance();
        }
    }

    private void ResolveProjectile(ProjectileEffect projectile, double now)
    {
        var impact = projectile.Path[^1];
        if (projectile.IsBomb)
        {
            ResolveExplosion(impact, 14 + Strength / 2, 3, 32);
            ApplySpellElements(SpellKind.Fireball, impact, 3);
            MakeNoise(16, impact);
            Effects.Add(new ExplosionEffect { Position = impact, StartedAt = now, Seed = _random.Next(), Duration = 1.35 });
            Effects.Add(new SmokeEffect { Position = impact, StartedAt = now, Seed = _random.Next() });
            AddMessage("The bomb ruptures stone in a boiling sphere of fire and pressure!");
        }
        else if (projectile.Spell == SpellKind.FrostLance)
        {
            ApplySpellElements(SpellKind.FrostLance, impact, 1);
            MarkSurface(impact, SurfaceMarkKind.Frost, '◇', new Rgb(130, 205, 235));
            Effects.Add(new FlourishEffect { Position = impact, StartedAt = now, Seed = _random.Next(), Kind = FlourishKind.FrostCrack,
                Color = new Rgb(220, 255, 255), SecondaryColor = new Rgb(45, 130, 255), Duration = 0.7, Radius = 4 });
            var monster = Monsters.FirstOrDefault(m => m.IsAlive && m.Position == impact);
            if (monster is not null)
            {
                var damage = 8 + Intellect / 2 + SpellSkillBonus(SpellKind.FrostLance) + _random.Next(4);
                monster.Hp -= damage;
                monster.FrozenTurns = 3;
                AddMessage($"Rime locks around the {monster.Name}: {damage} damage, frozen.");
                CheckKilled(monster);
            }
            Effects.Add(new ExplosionEffect { Position = impact, StartedAt = now, Seed = _random.Next(), Duration = 0.58,
                HotColor = new Rgb(235, 255, 255), CoolColor = new Rgb(30, 115, 255) });
        }
        else if (projectile.Spell is SpellKind.ShadowBolt or SpellKind.SunLance or SpellKind.PrismaticOrb or SpellKind.MarkedShot)
        {
            var monster = Monsters.FirstOrDefault(m => m.IsAlive && m.Position == impact);
            if (monster is not null)
            {
                var baseDamage = projectile.Spell switch
                {
                    SpellKind.ShadowBolt => 9 + Dexterity / 2,
                    SpellKind.SunLance => 10 + Wisdom / 2,
                    SpellKind.PrismaticOrb => 13 + Intellect / 2,
                    _ => 12 + Dexterity / 2 + TotalAttack / 2
                };
                monster.Hp -= Math.Max(2, baseDamage + SpellPower + SpellSkillBonus(projectile.Spell) - monster.Defense);
                CheckKilled(monster);
            }
            var sun = projectile.Spell == SpellKind.SunLance;
            var prism = projectile.Spell == SpellKind.PrismaticOrb;
            if (sun) ApplySpellElements(SpellKind.SolarRay, impact, 1);
            if (prism)
            {
                MarkSurface(impact, SurfaceMarkKind.CrystalShard, '⋄', new Rgb(120, 215, 230));
                Effects.Add(new FlourishEffect { Position = impact, StartedAt = now, Seed = _random.Next(), Kind = FlourishKind.CrystalShatter,
                    Color = new Rgb(150, 255, 235), SecondaryColor = new Rgb(255, 75, 220), Duration = 0.75, Radius = 5 });
            }
            Effects.Add(new ExplosionEffect { Position = impact, StartedAt = now, Seed = _random.Next(), Duration = 0.48,
                HotColor = prism ? new Rgb(120, 255, 240) : sun ? Rgb.White : new Rgb(220, 155, 255),
                CoolColor = prism ? new Rgb(255, 70, 220) : sun ? new Rgb(255, 190, 35) : new Rgb(65, 20, 135) });
        }
        else
        {
            ResolveExplosion(impact, 8 + Intellect / 3 + SpellPower + SpellSkillBonus(SpellKind.Fireball) + TotalAttack / 2, BlastRadius, 11);
            ApplySpellElements(SpellKind.Fireball, impact, BlastRadius);
            MakeNoise(14, impact);
            Effects.Add(new ExplosionEffect { Position = impact, StartedAt = now, Seed = _random.Next() });
            Effects.Add(new SmokeEffect { Position = impact, StartedAt = now, Seed = _random.Next() });
        }
    }

    private void TryMove(Point delta, double now)
    {
        if (Player.WebbedTurns > 0)
        {
            Player.WebbedTurns = 0;
            ControlGraceTurns = Math.Max(ControlGraceTurns, 3);
            var web = CurrentMap.Elements[Player.Position.X, Player.Position.Y];
            web.Web = Math.Max(0, web.Web - 8);
            MakeNoise(4);
            CommitTurn("You tear free of the web. You have two turns before anything can restrain you again.");
            return;
        }
        var target = Player.Position + delta;
        var resident = CurrentMap.Npcs.FirstOrDefault(npc => npc.Position == target);
        if (resident is not null) { BeginDialogue(resident); return; }
        if (CurrentMap.Contains(target) && HandleDoorCollision(target)) return;
        if (!CurrentMap.Contains(target) || !CurrentMap[target].Walkable)
        {
            var obstacle = CurrentMap.Contains(target) ? CurrentMap[target].Name : "edge of the world";
            AddMessage($"The {obstacle} bars your way. Stonebreak or bombs can shatter marked walls.");
            return;
        }
        if (CurrentMap.TravelPoints.TryGetValue(target, out var travel))
        {
            TravelTo(travel, Player.Position);
            return;
        }
        var monster = Monsters.FirstOrDefault(m => m.IsAlive && m.Position == target);
        if (monster is not null) { MakeNoise(7, target); Attack(Player, monster, now); CommitTurn(null); return; }
        var origin = Player.Position;
        Player.Position = target;
        var surface = CurrentMap.Elements[target.X, target.Y];
        var originSurface = CurrentMap.Elements[origin.X, origin.Y];
        if (surface.Wet > 0 || originSurface.Wet > 0 || CurrentMap[origin].Name.Contains("water", StringComparison.OrdinalIgnoreCase))
            MarkSurface(origin, SurfaceMarkKind.WetFootprint, (_turns + origin.X + origin.Y) % 2 == 0 ? '·' : '˙', new Rgb(74, 145, 185));
        MakeNoise(CurrentMap[target].Name.Contains("rubble") ? 5 : surface.Wet > 0 ? 3 : 2, target);
        TriggerTrap(target);
        CommitTurn(null);
        var loot = CurrentMap.GroundItems.FirstOrDefault(i => i.Position == Player.Position);
        var shop = CurrentMap.Shops.FirstOrDefault(s => s.Position == Player.Position);
        if (loot is not null) AddMessage($"A {loot.Item.Name} lies here. Press G to pick it up.");
        else if (shop is not null) AddMessage($"{shop.Name}. Press E to enter.");
        else if (Player.Position == CurrentMap.StairsDown) AddMessage("Stairs descend into darkness. Press E.");
        else if (Player.Position == CurrentMap.StairsUp) AddMessage("Stairs rise toward Emberfall. Press E.");
    }

    private void TravelTo(TravelKind destination, Point? returnPosition = null)
    {
        Effects.Clear();
        if (destination == TravelKind.CastleUndercroft && !WorldFlags.Contains("valeward_commissioned"))
        {
            AddMessage("Valeward's undercroft remains sealed. The castle requires the trust of all three March villages.");
            return;
        }
        if (_siteDungeons.TryGetValue(destination, out var site))
        {
            _wildernessReturnPosition = returnPosition ?? _wilderness.Start;
            CurrentMap = site;
            Player.Position = site.StairsUp;
            AddMessage($"You enter {site.Name}. {site.DungeonMechanic}");
        }
        else if (destination == TravelKind.WildernessReturn)
        {
            var departed = CurrentMap.Name;
            CurrentMap = _wilderness;
            Player.Position = _wildernessReturnPosition;
            AddMessage($"You emerge from {departed} into the open March.");
        }
        else if (destination == TravelKind.Wilderness)
        {
            CurrentMap = _wilderness;
            Player.Position = _wilderness.Start;
            AddMessage("You pass Emberfall's west gate. The Ashwind March opens beneath a wide, unsettled sky.");
        }
        else
        {
            CurrentMap = _town;
            Player.Position = new Point(3, 23);
            AddMessage("The west road carries you back beneath Emberfall's lamplights.");
        }
        NoiseLevel = 0;
        RefreshDetectionState();
        DiscoverNearbyLandmarks();
        BeginArrivalFlourish();
    }

    private void CommitTurn(string? message)
    {
        if (message is not null) AddMessage(message);
        _turns++;
        AdvanceEnvironment(_turns / 10.0);
        if (Player.IsAlive) AdvanceMonsters(_turns / 10.0);
        EndTurnMaintenance();
    }

    private void EndTurnMaintenance()
    {
        if (WardTurns > 0) WardTurns--;
        if (MightTurns > 0) MightTurns--;
        if (GuardTurns > 0) GuardTurns--;
        if (FocusTurns > 0) FocusTurns--;
        if (ControlGraceTurns > 0) ControlGraceTurns--;
        if (ElectricRecoveryTurns > 0) ElectricRecoveryTurns--;
        NoiseLevel = Math.Max(0, NoiseLevel - 3);
        if (WorldMinutes == 360) AddMessage("Dawn reaches the March. The roads brighten and long shadows draw back.");
        else if (WorldMinutes == 1080) AddMessage("The sun slips below the ash pines. Lamps kindle across the roads and villages.");
        RefreshDetectionState();
        UpdateNpcSchedules();
        DiscoverNearbyLandmarks();
        if (_turns > 0 && _turns % (CurrentMap.IsTown ? 8 : 5) == 0) Food = Math.Max(0, Food - 1);
        if (_turns > 0 && _turns % (CurrentMap.IsTown ? 7 : 4) == 0) Water = Math.Max(0, Water - 1);
        if (_turns > 0 && _turns % 9 == 0 && Mana < MaxMana) Mana++;
        if (Food == 0 && _turns % 4 == 0) { Player.Hp--; AddMessage("Hunger gnaws at your strength."); }
        if (Water == 0 && _turns % 3 == 0) { Player.Hp -= 2; AddMessage("Thirst burns through you."); }
        if (Player.Hp <= 0)
        {
            Player.Hp = 0;
            Mode = GameMode.GameOver;
            AddMessage("Your lantern gutters out in the deep.");
        }
    }

    private void Rest()
    {
        if (!CurrentMap.IsTown && Monsters.Any(m => m.IsAlive && m.AlertTurns > 0 && m.Position.DistanceSquared(Player.Position) <= 144))
        {
            AddMessage("You cannot rest while hostile eyes reflect your lantern.");
            return;
        }
        var recovered = 0;
        var manaRecovered = 0;
        for (var i = 0; i < 6 && Player.IsAlive; i++)
        {
            _turns++;
            MakeNoise(1);
            AdvanceEnvironment(_turns / 10.0);
            if (Player.IsAlive) AdvanceMonsters(_turns / 10.0);
            var heal = Math.Min(1 + Wisdom / 8, Player.MaxHp - Player.Hp);
            var magic = Math.Min(1 + Intellect / 12, MaxMana - Mana);
            Player.Hp += heal;
            Mana += magic;
            recovered += heal;
            manaRecovered += magic;
            EndTurnMaintenance();
            if (Player.Hp == Player.MaxHp && Mana == MaxMana) break;
        }
        Food = Math.Max(0, Food - 2);
        Water = Math.Max(0, Water - 3);
        AddMessage($"You rest beneath the lantern: +{recovered} HP, +{manaRecovered} mana. Time and provisions pass.");
    }

    private void BeginSpell(SpellKind spell, double now)
    {
        if (!KnownSpells.Contains(spell)) { AddMessage("Your calling has not taught you that spell."); return; }
        var cost = SpellCost(spell);
        if (Mana < cost) { AddMessage($"You need {cost} mana for {SpellName(spell)}."); return; }
        if (spell is SpellKind.LuminousNova or SpellKind.ShieldFlare or SpellKind.HealingLight or SpellKind.BattleCry or SpellKind.Sanctuary)
        { CastImmediateSpell(spell, now); return; }
        _pendingSpell = spell;
        CursorPosition = NearestVisibleMonster()?.Position ?? Player.Position + new Point(1, 0);
        if (!CurrentMap.Contains(CursorPosition.Value)) CursorPosition = Player.Position;
        Mode = GameMode.Targeting;
        AddMessage($"Target {SpellName(spell)} — move cursor, Tab cycles enemies, Enter casts.");
    }

    private void CastSpellAt(SpellKind spell, Point target, double now)
    {
        var range = SpellRange(spell);
        if (Chebyshev(Player.Position, target) > range) { AddMessage($"Target is beyond {range} tiles."); return; }
        if (!HasClearSight(Player.Position, target) && spell is not SpellKind.Stonebreak and not SpellKind.SeismicWave)
        { AddMessage("No clear line of effect reaches that target."); return; }
        Mode = GameMode.Playing; CursorPosition = null; _pendingSpell = null;
        if (spell == SpellKind.Blink)
        {
            if (!CurrentMap[target].Walkable || Monsters.Any(m => m.IsAlive && m.Position == target) ||
                CurrentMap.Doors.TryGetValue(target, out var blinkDoor) && blinkDoor.State is not DoorState.Open and not DoorState.Broken)
            { AddMessage("The destination rejects your sigil."); return; }
            var origin = Player.Position;
            Player.Position = target;
            SpendSpellTurn(spell);
            Effects.Add(new ArcEffect { Points = Line(origin, target), StartedAt = now, Color = new Rgb(200, 105, 255), Duration = 0.42, Style = ArcStyle.Blink });
            AddMessage("Space folds into a violet seam; you step through.");
            return;
        }
        if (spell == SpellKind.ChainLightning) { CastChainLightning(target, now); return; }
        if (spell is SpellKind.Stonebreak or SpellKind.SeismicWave) { CastStonebreak(target, now, spell == SpellKind.SeismicWave); return; }
        if (spell is SpellKind.GroundQuake or SpellKind.IceStorm or SpellKind.PoisonCloud or SpellKind.Smite)
        {
            CastBurstSpell(spell, target, now);
            return;
        }
        if (spell == SpellKind.Meteor)
        {
            SpendSpellTurn(spell);
            ResolveExplosion(target, 15 + Intellect / 2 + SpellPower + SpellSkillBonus(spell), 4, 24);
            ApplySpellElements(SpellKind.Meteor, target, 4);
            MakeNoise(18, target);
            Effects.Add(new ExplosionEffect { Position = target, StartedAt = now, Seed = _random.Next(), Duration = 1.5 });
            Effects.Add(new SmokeEffect { Position = target, StartedAt = now, Seed = _random.Next() });
            AddMessage("A star tears through the unseen ceiling and becomes an incandescent ruin!");
            return;
        }
        if (spell is SpellKind.ArcaneBeam or SpellKind.ThornVolley or SpellKind.FlameCleave or SpellKind.Multishot or SpellKind.SolarRay)
        {
            var points = Line(Player.Position, target);
            var victims = spell switch
            {
                SpellKind.ArcaneBeam or SpellKind.SolarRay => Monsters.Where(m => m.IsAlive && points.Contains(m.Position)).Take(spell == SpellKind.SolarRay ? 5 : 2).ToList(),
                SpellKind.FlameCleave => Monsters.Where(m => m.IsAlive && m.Position.DistanceSquared(Player.Position) <= 10 &&
                    IsInDirection(Player.Position, m.Position, new Point(Math.Sign(target.X - Player.Position.X), Math.Sign(target.Y - Player.Position.Y)))).ToList(),
                SpellKind.Multishot => Monsters.Where(m => m.IsAlive && Chebyshev(m.Position, target) <= 2).Take(4).ToList(),
                _ => Monsters.Where(m => m.IsAlive && m.Position.DistanceSquared(target) <= 5).ToList()
            };
            foreach (var monster in victims)
            {
                var baseDamage = spell switch
                {
                    SpellKind.ArcaneBeam => 8 + Intellect / 2,
                    SpellKind.FlameCleave => 8 + Strength / 2 + TotalAttack / 2,
                    SpellKind.Multishot => 7 + Dexterity / 2 + TotalAttack / 3,
                    SpellKind.SolarRay => 12 + Wisdom / 2 + SpellPower,
                    _ => 7 + Dexterity / 2
                };
                var damage = baseDamage + SpellPower + SpellSkillBonus(spell) - monster.Defense;
                monster.Hp -= Math.Max(2, damage); CheckKilled(monster);
            }
            if (spell is SpellKind.FlameCleave or SpellKind.SolarRay)
                foreach (var p in points) ApplySpellElements(spell, p, spell == SpellKind.FlameCleave ? 1 : 0);
            SpendSpellTurn(spell);
            Effects.Add(new ArcEffect { Points = points, StartedAt = now,
                Color = spell switch { SpellKind.ArcaneBeam => new Rgb(185, 95, 255), SpellKind.FlameCleave => new Rgb(255, 90, 20),
                    SpellKind.SolarRay => new Rgb(255, 232, 110), SpellKind.Multishot => new Rgb(145, 245, 95), _ => new Rgb(80, 225, 105) },
                Duration = 0.48,
                Style = spell switch { SpellKind.FlameCleave => ArcStyle.Flame, SpellKind.SolarRay => ArcStyle.Solar,
                    SpellKind.Multishot => ArcStyle.Arrow, SpellKind.ThornVolley => ArcStyle.Thorn, _ => ArcStyle.Beam } });
            AddMessage(spell switch { SpellKind.ArcaneBeam => "A violet line cuts matter into light.", SpellKind.FlameCleave => "Your weapon draws a roaring crescent of flame.",
                SpellKind.Multishot => "Ghost-arrows converge from impossible angles.", SpellKind.SolarRay => "A pillar of noon burns down the corridor.", _ => "A fan of spectral thorns tears through the target ground." });
            return;
        }
        var path = BuildPathToTarget(target, range);
        if (path.Count == 0) { AddMessage("The spell collapses before it can form."); return; }
        Mana -= SpellCost(spell);
        _turns++;
        MakeNoise(6);
        _pendingEnemyTurn = true;
        var frost = spell == SpellKind.FrostLance;
        var shadow = spell == SpellKind.ShadowBolt;
        var sun = spell == SpellKind.SunLance;
        var prism = spell == SpellKind.PrismaticOrb;
        var marked = spell == SpellKind.MarkedShot;
        Effects.Add(new ProjectileEffect
        {
            Path = path, StartedAt = now, Spell = spell,
            OuterColor = frost ? new Rgb(42, 135, 255) : shadow ? new Rgb(105, 45, 210) : sun ? new Rgb(255, 210, 62) :
                prism ? new Rgb(90, 255, 215) : marked ? new Rgb(120, 255, 85) : new Rgb(255, 78, 8),
            CoreColor = frost ? new Rgb(220, 255, 255) : shadow ? new Rgb(220, 165, 255) : prism ? new Rgb(255, 125, 245) : new Rgb(255, 248, 218),
            Glyph = frost ? '◆' : sun ? '✦' : marked ? '➤' : '●', StepSeconds = frost ? 0.035 : 0.052
        });
        AddMessage($"{SpellName(spell)} streaks toward {DescribeAt(target)}.");
    }

    private void CastChainLightning(Point target, double now)
    {
        var first = Monsters.Where(m => m.IsAlive).OrderBy(m => m.Position.DistanceSquared(target)).FirstOrDefault();
        var candidates = first is null ? [] : Monsters.Where(m => m.IsAlive)
            .OrderBy(m => m == first ? -1 : m.Position.DistanceSquared(first.Position)).ToList();
        if (candidates.Count == 0) { AddMessage("The lightning finds no living conductor."); return; }
        Mana -= SpellCost(SpellKind.ChainLightning);
        _turns++;
        _pendingEnemyTurn = true;
        var points = new List<Point>();
        var origin = Player.Position;
        foreach (var monster in candidates.Take(3))
        {
            points.AddRange(Line(origin, monster.Position));
            var damage = 6 + Intellect / 2 + SpellPower + SpellSkillBonus(SpellKind.ChainLightning) + _random.Next(5);
            monster.Hp -= damage;
            AddMessage($"Lightning brands the {monster.Name} for {damage}.");
            CheckKilled(monster);
            origin = monster.Position;
        }
        foreach (var p in points.Distinct()) ApplySpellElements(SpellKind.ChainLightning, p, 0);
        MakeNoise(11, target);
        Effects.Add(new ArcEffect { Points = points.Distinct().ToList(), StartedAt = now, Color = new Rgb(100, 195, 255), Style = ArcStyle.Lightning });
    }

    private void CastStonebreak(Point target, double now, bool seismic)
    {
        var direction = new Point(Math.Sign(target.X - Player.Position.X), Math.Sign(target.Y - Player.Position.Y));
        var points = new List<Point>();
        var cursor = Player.Position;
        var hit = false;
        for (var i = 0; i < 10; i++)
        {
            cursor += direction;
            if (!CurrentMap.Contains(cursor)) break;
            points.Add(cursor);
            var monster = Monsters.FirstOrDefault(m => m.IsAlive && m.Position == cursor);
            if (monster is not null) { monster.Hp -= (seismic ? 13 : 7) + Strength / 2 + SpellSkillBonus(seismic ? SpellKind.SeismicWave : SpellKind.Stonebreak); CheckKilled(monster); hit = true; if (!seismic) break; }
            if (!CurrentMap[cursor].Transparent) { hit = CurrentMap.DamageTerrain(cursor, (seismic ? 50 : 30) + Strength, WorldBuilder.Rubble); if (!seismic) break; }
        }
        var spell = seismic ? SpellKind.SeismicWave : SpellKind.Stonebreak;
        Mana -= SpellCost(spell);
        _turns++;
        MakeNoise(seismic ? 13 : 8);
        _pendingEnemyTurn = true;
        Effects.Add(new ArcEffect { Points = points, StartedAt = now, Color = new Rgb(220, 172, 95), Duration = 0.5, Style = ArcStyle.Seismic });
        AddMessage(hit ? "The fault-line splits stone into smoking rubble!" : "The fault-line hammers through the vault.");
    }

    private void CastImmediateSpell(SpellKind spell, double now)
    {
        var mastery = SpellSkillBonus(spell);
        Mana -= SpellCost(spell);
        _turns++;
        _pendingEnemyTurn = true;
        if (spell == SpellKind.HealingLight)
        {
            var healed = Math.Min(Player.MaxHp - Player.Hp, 12 + Wisdom / 2 + SpellPower + mastery);
            Player.Hp += healed; Effects.Add(new ExplosionEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Duration = 0.55,
                HotColor = Rgb.White, CoolColor = new Rgb(85, 255, 145) }); AddMessage($"Healing light restores {healed} HP.");
        }
        else if (spell == SpellKind.ShieldFlare)
        {
            WardTurns = 7 + mastery / 2; Effects.Add(new ExplosionEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Duration = 0.5,
                HotColor = Rgb.White, CoolColor = new Rgb(90, 155, 255) }); AddMessage($"A hard shell of light forms around you: +4 defense for {WardTurns} turns.");
        }
        else if (spell == SpellKind.BattleCry)
        {
            MightTurns = 10 + mastery / 2; GuardTurns = 10 + mastery / 2;
            Effects.Add(new ExplosionEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Duration = 0.6,
                HotColor = new Rgb(255, 236, 170), CoolColor = new Rgb(210, 50, 20) });
            Effects.Add(new FlourishEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Kind = FlourishKind.DustFall,
                Color = new Rgb(210, 175, 125), SecondaryColor = new Rgb(120, 78, 45), Duration = 1.2, Radius = 7 });
            AddMessage($"Your battle cry shakes dust from the vault: +4 attack and defense for {MightTurns} turns.");
        }
        else if (spell == SpellKind.Sanctuary)
        {
            WardTurns = 12 + mastery / 2;
            Player.Hp = Math.Min(Player.MaxHp, Player.Hp + 8 + Wisdom / 3 + mastery);
            Effects.Add(new ExplosionEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Duration = 0.85,
                HotColor = Rgb.White, CoolColor = new Rgb(255, 205, 80) });
            Effects.Add(new FlourishEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Kind = FlourishKind.SanctuaryHalo,
                Color = new Rgb(255, 240, 175), SecondaryColor = new Rgb(255, 178, 58), Duration = 1.55, Radius = 6 });
            AddMessage($"A sanctuary of warm gold seals wounds and turns blades for {WardTurns} turns.");
        }
        else
        {
            ResolveExplosion(Player.Position, 7 + Wisdom / 2 + SpellPower + mastery, 3, 10);
            ApplySpellElements(SpellKind.LuminousNova, Player.Position, 3);
            Effects.Add(new ExplosionEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Duration = 0.9,
                HotColor = Rgb.White, CoolColor = new Rgb(255, 204, 62) }); AddMessage("A noiseless sun unfolds from your lantern.");
        }
    }

    private void SpendSpellTurn(SpellKind spell) { Mana -= SpellCost(spell); _turns++; MakeNoise(spell == SpellKind.Blink ? 3 : 8); _pendingEnemyTurn = true; }

    private void CastBurstSpell(SpellKind spell, Point target, double now)
    {
        var radius = spell switch { SpellKind.GroundQuake => 3, SpellKind.IceStorm => 3, SpellKind.PoisonCloud => 3, _ => 2 };
        var victims = Monsters.Where(m => m.IsAlive && Chebyshev(m.Position, target) <= radius).ToList();
        foreach (var monster in victims)
        {
            var damage = spell switch
            {
                SpellKind.GroundQuake => 11 + Strength / 2 + SpellPower + SpellSkillBonus(spell),
                SpellKind.IceStorm => 8 + Intellect / 2 + SpellPower + SpellSkillBonus(spell),
                SpellKind.PoisonCloud => 4 + Dexterity / 3 + SpellPower + SpellSkillBonus(spell),
                _ => 15 + Wisdom / 2 + SpellPower + SpellSkillBonus(spell)
            };
            monster.Hp -= Math.Max(2, damage - monster.Defense);
            if (spell == SpellKind.IceStorm) monster.FrozenTurns = 2;
            if (spell == SpellKind.PoisonCloud) monster.PoisonTurns = 6;
            CheckKilled(monster);
        }
        if (spell == SpellKind.GroundQuake)
            for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++)
            { var p = target + new Point(x, y); if (CurrentMap.Contains(p)) CurrentMap.DamageTerrain(p, 25 + Strength, WorldBuilder.Rubble); }
        ApplySpellElements(spell, target, radius);
        if (spell is SpellKind.IceStorm or SpellKind.GroundQuake)
            for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++)
            {
                var p = target + new Point(x, y);
                if (!CurrentMap.Contains(p) || !CurrentMap[p].Walkable || (x * 13 + y * 7 + _turns) % 3 != 0) continue;
                MarkSurface(p, spell == SpellKind.IceStorm ? SurfaceMarkKind.Frost : SurfaceMarkKind.Crack,
                    spell == SpellKind.IceStorm ? '◇' : '⌁', spell == SpellKind.IceStorm ? new Rgb(135, 205, 235) : new Rgb(112, 86, 65));
            }
        SpendSpellTurn(spell);
        var cool = spell switch { SpellKind.GroundQuake => new Rgb(150, 75, 28), SpellKind.IceStorm => new Rgb(45, 145, 255),
            SpellKind.PoisonCloud => new Rgb(75, 205, 65), _ => new Rgb(255, 178, 35) };
        Effects.Add(new ExplosionEffect { Position = target, StartedAt = now, Seed = _random.Next(), Duration = 1.0, HotColor = Rgb.White, CoolColor = cool });
        if (spell == SpellKind.PoisonCloud) Effects.Add(new SmokeEffect { Position = target, StartedAt = now, Seed = _random.Next(), Duration = 4.5 });
        AddMessage(spell switch { SpellKind.GroundQuake => "The floor bucks; walls burst into rubble.", SpellKind.IceStorm => "A blue-white blizzard crystallizes the chamber.",
            SpellKind.PoisonCloud => "Venom-green fog coils around every breath.", _ => "Judgment falls as a bell-shaped detonation of light." });
    }

    private void ThrowBomb(Point direction, double now)
    {
        AwaitingDirection = false;
        _pendingBomb = false;
        _pendingSpell = null;
        var path = BuildProjectilePath(direction, 10, true);
        if (path.Count == 0) { AddItem(ItemKind.Bomb, 1, false); AddMessage("You lower the bomb; there is no throwing arc."); return; }
        _turns++;
        MakeNoise(3);
        _pendingEnemyTurn = true;
        Effects.Add(new ProjectileEffect { Path = path, StartedAt = now, IsBomb = true, Glyph = 'o', StepSeconds = 0.065,
            OuterColor = new Rgb(255, 94, 20), CoreColor = new Rgb(255, 205, 92) });
        AddMessage("The bomb's fuse scribbles sparks through the air.");
    }

    private List<Point> BuildProjectilePath(Point direction, int range, bool stopAtMonster)
    {
        var path = new List<Point>();
        var cursor = Player.Position;
        for (var i = 0; i < range; i++)
        {
            var next = cursor + direction;
            if (!CurrentMap.Contains(next) || !CurrentMap[next].Transparent) break;
            path.Add(next);
            cursor = next;
            if (stopAtMonster && Monsters.Any(m => m.IsAlive && m.Position == cursor)) break;
        }
        return path;
    }

    private List<Point> BuildPathToTarget(Point target, int range)
    {
        var result = new List<Point>();
        foreach (var p in Line(Player.Position, target).Skip(1).Take(range))
        {
            if (!CurrentMap.Contains(p) || !CurrentMap[p].Transparent) break;
            result.Add(p);
            if (p == target || Monsters.Any(m => m.IsAlive && m.Position == p)) break;
        }
        return result;
    }

    private void ResolveExplosion(Point center, int baseDamage, int radius, int terrainDamage)
    {
        MarkSurface(center, SurfaceMarkKind.Scorch, '◉', new Rgb(76, 37, 28));
        var hit = false;
        foreach (var monster in Monsters.Where(m => m.IsAlive && m.Position.DistanceSquared(center) <= radius * radius).ToList())
        {
            var damage = Math.Max(2, baseDamage + _random.Next(5) - monster.Defense);
            monster.Hp -= damage;
            hit = true;
            AddMessage($"The blast engulfs the {monster.Name} for {damage} damage.");
            CheckKilled(monster);
        }
        var destroyed = 0;
        for (var y = center.Y - radius - 1; y <= center.Y + radius + 1; y++)
        for (var x = center.X - radius - 1; x <= center.X + radius + 1; x++)
        {
            var p = new Point(x, y);
            if (p.DistanceSquared(center) > (radius + 1) * (radius + 1)) continue;
            if (CurrentMap.Contains(p) && CurrentMap[p].Walkable && (p.X * 17 + p.Y * 31 + _turns) % 4 == 0)
                MarkSurface(p, SurfaceMarkKind.Soot, '·', new Rgb(64, 50, 48));
            if (CurrentMap.DamageTerrain(p, terrainDamage, WorldBuilder.Rubble))
            {
                destroyed++;
                MarkSurface(p, CurrentMap.Biome == BiomeKind.PrismaticFoundry ? SurfaceMarkKind.CrystalShard : SurfaceMarkKind.Crack,
                    CurrentMap.Biome == BiomeKind.PrismaticFoundry ? '⋄' : '⌁',
                    CurrentMap.Biome == BiomeKind.PrismaticFoundry ? new Rgb(105, 185, 225) : new Rgb(112, 94, 76));
            }
        }
        foreach (var door in CurrentMap.Doors.Values.Where(door => door.Position.DistanceSquared(center) <= radius * radius && door.State is not DoorState.Open and not DoorState.Broken))
        {
            door.State = DoorState.Broken;
            CurrentMap.Set(door.Position, WorldBuilder.OpenDoor with { Glyph = '░', Name = "blasted doorway" });
        }
        foreach (var trap in CurrentMap.Traps.Values.Where(trap => trap.Position.DistanceSquared(center) <= radius * radius))
        { trap.Revealed = true; trap.Armed = false; }
        if (!hit && destroyed == 0) AddMessage("The blast rolls away in a thunder of colored light.");
        if (destroyed > 0) AddMessage($"{destroyed} section{(destroyed == 1 ? "" : "s")} of vault wall collapse into rubble.");
    }

    private void AdvanceMonsters(double now)
    {
        if (CurrentMap.IsTown) { RefreshDetectionState(); return; }
        foreach (var monster in Monsters.Where(m => m.IsAlive).ToList())
        {
            var ecologicallyActive = CurrentMap.IsOutdoor && (monster.ActiveAtNight ? OutdoorDaylight < 0.35 : monster.ActiveByDay || OutdoorDaylight < 0.72);
            if (CurrentMap.IsOutdoor && !ecologicallyActive && monster.AlertTurns <= 0)
            {
                monster.Intent = MonsterIntentKind.Sleep;
                continue;
            }
            if (monster.PoisonTurns > 0)
            {
                monster.PoisonTurns--;
                monster.Hp -= 2 + SpellPower / 3;
                if (!monster.IsAlive) { AddMessage($"Venom consumes the {monster.Name}."); CheckKilled(monster); continue; }
            }
            if (monster.FrozenTurns > 0) { monster.FrozenTurns--; continue; }
            if (monster.WebbedTurns > 0) { monster.WebbedTurns--; continue; }
            if (monster.Ai == MonsterAi.Brute && _turns % 2 == 0) continue;

            var rival = Monsters.FirstOrDefault(other => other.IsAlive && other != monster && other.Faction != monster.Faction &&
                Chebyshev(other.Position, monster.Position) <= 1 && monster.AlertTurns <= 0);
            if (rival is not null)
            {
                var damage = Math.Max(1, monster.Attack - rival.Defense + _random.Next(2));
                rival.Hp -= damage;
                if (!rival.IsAlive && HasClearSight(Player.Position, rival.Position)) AddMessage($"The {monster.Name} tears down the {rival.Name}.");
                continue;
            }

            if (MonsterDetectsPlayer(monster))
            {
                monster.LastKnownPlayer = Player.Position;
                monster.AlertTurns = monster.AttractedToLight ? 12 : 8;
                foreach (var ally in Monsters.Where(ally => ally.IsAlive && ally != monster && ally.Faction == monster.Faction &&
                    ally.Position.DistanceSquared(monster.Position) <= 64))
                { ally.LastKnownPlayer = Player.Position; ally.AlertTurns = Math.Max(ally.AlertTurns, 4); }
            }
            else if (monster.AlertTurns > 0) monster.AlertTurns--;

            var ownLight = GameplayLightAt(monster.Position);
            if (monster.LightSensitive && ownLight > 1.05)
            {
                monster.Hp--;
                if (!monster.IsAlive) { CheckKilled(monster); continue; }
            }
            if (monster.Name.Contains("kobold"))
            {
                var torch = CurrentMap.Lights.FirstOrDefault(light => light.Position.DistanceSquared(monster.Position) <= 2 &&
                    !CurrentMap.ExtinguishedLights.Contains(light.Position));
                if (torch is not null)
                {
                    CurrentMap.ExtinguishedLights.Add(torch.Position);
                    if (HasClearSight(Player.Position, monster.Position)) AddMessage("The candle kobold pinches out a nearby flame.");
                    continue;
                }
            }
            if (monster.Name.Contains("spider") && _turns % 4 == 0)
                CurrentMap.Elements[monster.Position.X, monster.Position.Y].Web = 18;
            if (monster.Faction == MonsterFaction.Infernal)
                CurrentMap.Elements[monster.Position.X, monster.Position.Y].Fire = Math.Max(CurrentMap.Elements[monster.Position.X, monster.Position.Y].Fire, 3);
            if (monster.Name.Contains("lich") && _turns % 7 == 0)
            {
                var corpse = Monsters.FirstOrDefault(other => !other.IsAlive && other != monster && other.Position.DistanceSquared(monster.Position) <= 36);
                if (corpse is not null)
                {
                    corpse.Hp = Math.Max(1, corpse.MaxHp / 3); corpse.XpReward = 0; corpse.Gold = 0; corpse.AlertTurns = 6; corpse.LastKnownPlayer = Player.Position;
                    if (HasClearSight(Player.Position, corpse.Position)) AddMessage($"The crown lich drags the {corpse.Name} back into motion!");
                }
            }

            var pursuit = monster.LastKnownPlayer;
            if (pursuit is null && monster.AttractedToLight)
                pursuit = CurrentMap.Lights.Where(light => !CurrentMap.ExtinguishedLights.Contains(light.Position))
                    .OrderBy(light => light.Position.DistanceSquared(monster.Position)).Select(light => (Point?)light.Position).FirstOrDefault();
            var destination = pursuit ?? Player.Position;
            var dx = destination.X - monster.Position.X;
            var dy = destination.Y - monster.Position.Y;
            var distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
            var seesPlayer = monster.AlertTurns > 0 && HasClearSight(monster.Position, Player.Position);
            if (monster.Intent == MonsterIntentKind.Strike && (!seesPlayer || distance > 1)) monster.Intent = MonsterIntentKind.Advance;
            if (monster.Intent == MonsterIntentKind.Shoot && (!seesPlayer || distance is < 3 or > 8)) monster.Intent = MonsterIntentKind.Advance;
            if (monster.Ai == MonsterAi.Caster && seesPlayer && distance is >= 3 and <= 8)
            {
                if (monster.Intent != MonsterIntentKind.Shoot)
                {
                    monster.Intent = MonsterIntentKind.Shoot;
                    if (HasClearSight(Player.Position, monster.Position)) AddMessage($"The {monster.Name} gathers witch-light — ranged attack next turn.");
                    continue;
                }
                var damage = Math.Max(1, monster.Attack - TotalDefense / 2 + _random.Next(3));
                damage = ApplyPreparedReaction(damage, true);
                Player.Hp -= damage;
                Effects.Add(new ArcEffect { Points = Line(monster.Position, Player.Position), StartedAt = now,
                    Color = monster.GlowColor ?? new Rgb(175, 80, 255), Duration = 0.32, Style = ArcStyle.Beam });
                Effects.Add(new FlourishEffect { Position = Player.Position, Direction = new Point(Math.Sign(Player.Position.X - monster.Position.X), Math.Sign(Player.Position.Y - monster.Position.Y)),
                    StartedAt = now, Seed = _random.Next(), Kind = FlourishKind.Impact, Color = new Rgb(230, 175, 255), Duration = 0.25 });
                AddMessage($"The {monster.Name} lashes you with witch-light for {damage}.");
                monster.Intent = MonsterIntentKind.Guard;
                if (!Player.IsAlive) return;
                continue;
            }
            if (seesPlayer && distance <= 1)
            {
                if (monster.Intent != MonsterIntentKind.Strike)
                {
                    monster.Intent = MonsterIntentKind.Strike;
                    AddMessage($"The {monster.Name} draws back to strike — react with F6 or move away.");
                    continue;
                }
                Attack(monster, Player, now);
                monster.Intent = MonsterIntentKind.Guard;
                if (!Player.IsAlive) return;
                continue;
            }
            Point delta;
            if (monster.AlertTurns > 0 || pursuit is not null)
            {
                var retreat = monster.Ai == MonsterAi.Skirmisher && distance < 4 ? -1 : 1;
                monster.Intent = retreat < 0 ? MonsterIntentKind.Retreat : MonsterIntentKind.Advance;
                if (Math.Abs(dx) > Math.Abs(dy)) delta = new Point(Math.Sign(dx) * retreat, 0);
                else delta = new Point(0, Math.Sign(dy) * retreat);
            }
            else
            {
                if (CurrentMap.IsOutdoor && monster.Position.DistanceSquared(monster.HomePosition) > monster.TerritoryRadius * monster.TerritoryRadius)
                {
                    monster.Intent = MonsterIntentKind.Retreat;
                    var homeDx = monster.HomePosition.X - monster.Position.X;
                    var homeDy = monster.HomePosition.Y - monster.Position.Y;
                    delta = Math.Abs(homeDx) > Math.Abs(homeDy) ? new Point(Math.Sign(homeDx), 0) : new Point(0, Math.Sign(homeDy));
                }
                else
                {
                if (_random.NextDouble() > (monster.Ai == MonsterAi.Lurker ? 0.08 : 0.24)) continue;
                monster.Intent = MonsterIntentKind.Roam;
                delta = CardinalDirections[_random.Next(CardinalDirections.Length)];
                }
            }
            var target = monster.Position + delta;
            if (CurrentMap.Doors.TryGetValue(target, out var door) && door.State is DoorState.Closed or DoorState.Locked or DoorState.Secret)
            {
                if (door.State == DoorState.Secret) continue;
                if (monster.CanOpenDoors && door.State == DoorState.Closed)
                { door.State = DoorState.Open; CurrentMap.Set(target, WorldBuilder.OpenDoor); }
                else if (monster.Ai == MonsterAi.Brute && door.State == DoorState.Locked)
                { door.State = DoorState.Broken; CurrentMap.Set(target, WorldBuilder.OpenDoor with { Glyph = '░', Name = "smashed door" }); MakeNoise(9, target); }
                continue;
            }
            if (CurrentMap.Contains(target) && CurrentMap[target].Walkable && target != Player.Position &&
                !Monsters.Any(m => m.IsAlive && m != monster && m.Position == target)) monster.Position = target;
            if (monster.LastKnownPlayer == monster.Position && !seesPlayer) monster.LastKnownPlayer = null;
        }
        RefreshDetectionState();
    }

    private void Attack(Actor attacker, Actor defender, double now)
    {
        var attackValue = attacker == Player ? TotalAttack : attacker.Attack;
        var defenseValue = defender == Player ? TotalDefense : defender.Defense;
        var damage = Math.Max(1, attackValue + _random.Next(-1, 3) - defenseValue);
        if (defender == Player) damage = ApplyPreparedReaction(damage, false);
        defender.Hp -= damage;
        var critical = damage >= Math.Max(7, defender.MaxHp / 3);
        var direction = new Point(Math.Sign(defender.Position.X - attacker.Position.X), Math.Sign(defender.Position.Y - attacker.Position.Y));
        Effects.Add(new FlourishEffect
        {
            Position = defender.Position, Direction = direction, StartedAt = now, Seed = _random.Next(),
            Kind = critical ? FlourishKind.CriticalImpact : FlourishKind.Impact,
            Color = defender == Player ? new Rgb(255, 92, 72) : new Rgb(255, 238, 195),
            SecondaryColor = new Rgb(220, 48, 42), Duration = critical ? 0.28 : 0.18, Radius = critical ? 3.5 : 2
        });
        if (defender != Player)
            MarkSurface(defender.Position, SurfaceMarkKind.Blood, damage > 7 ? '∴' : '·', new Rgb(125, 24, 28));
        if (attacker == Player)
        {
            AddMessage(defender.Hp <= 0 ? $"You strike down the {defender.Name}." : $"You cut the {defender.Name} for {damage} ({defender.Hp}/{defender.MaxHp}).");
            CheckKilled(defender);
        }
        else AddMessage($"The {attacker.Name} hits you for {damage} damage.");
    }

    private void CheckKilled(Actor monster)
    {
        if (monster.Hp > 0 || monster.RewardsClaimed) return;
        monster.Hp = 0;
        monster.RewardsClaimed = true;
        MarkSurface(monster.Position, monster.Faction is MonsterFaction.Construct
            ? SurfaceMarkKind.CrystalShard : SurfaceMarkKind.Blood,
            monster.Faction is MonsterFaction.Construct ? '⋄' : '∴',
            monster.Faction is MonsterFaction.Construct ? new Rgb(100, 175, 205) : new Rgb(112, 20, 25));
        Player.Gold += monster.Gold;
        RecordQuestProgress(QuestObjectiveKind.DefeatFaction, faction: monster.Faction);
        var material = monster.Faction switch
        {
            MonsterFaction.Goblinoid => ItemKind.GoblinScrap,
            MonsterFaction.Wild or MonsterFaction.Vermin => monster.Name.Contains("spider") ? ItemKind.VenomGland : ItemKind.BeastHide,
            MonsterFaction.Undead or MonsterFaction.Construct => ItemKind.RelicShard,
            _ => (ItemKind?)null
        };
        if (material is { } materialKind && (_random.NextDouble() < 0.72 || monster.IsBoss))
            AddItem(materialKind, monster.IsBoss ? 2 : 1, false);
        if (monster.IsBoss)
        {
            CompletedDungeons.Add(CurrentMap.Name);
            WorldFlags.Add($"dungeon:{CurrentMap.Name}");
            AddItem(ItemKind.BossRelic, 1, false);
            var unique = CreateBossReward(monster);
            CurrentMap.GroundItems.Add(new GroundItem(monster.Position, unique));
            RecordQuestProgress(QuestObjectiveKind.ClearDungeon);
            AddMessage($"DUNGEON CLEARED: {CurrentMap.Name}. {monster.UniqueRewardName} falls free.");
        }
        GrantExperience(monster.XpReward);
        if (_random.NextDouble() < 0.58)
        {
            var drop = _random.NextDouble() < 0.72
                ? EquipmentFactory.MonsterDrop(_random, monster, Level)
                : WorldBuilder.MakeItem(RandomDropKind());
            CurrentMap.GroundItems.Add(new GroundItem(monster.Position, drop));
            AddMessage($"The {monster.Name} drops {RarityName(drop)}{drop.Name}.");
        }
        AddMessage($"The {monster.Name} falls. +{monster.XpReward} XP, +{monster.Gold} gold.");
    }

    private static ItemStack CreateBossReward(Actor boss) => boss.Name switch
    {
        "Root-Crowned Matriarch" => EquipmentFactory.Create("matriarch thornblade", EquipmentSlot.MainHand, '/', attack: 5, spell: 1, rarity: ItemRarity.Epic, description: "A living edge cut from Hollowroot's crown"),
        "Drowned Castellan" => EquipmentFactory.Create("castellan tideguard", EquipmentSlot.OffHand, ']', defense: 5, rarity: ItemRarity.Epic, description: "A barrow shield that turns water and iron"),
        "Moonwell Wyrm" => EquipmentFactory.Create("moonwell star-ring", EquipmentSlot.Ring, '○', spell: 5, light: 1.2, rarity: ItemRarity.Epic, description: "A ring carrying a cold reflected moon"),
        _ => EquipmentFactory.Create("regent's oathbreaker", EquipmentSlot.MainHand, '†', attack: 6, defense: 2, rarity: ItemRarity.Epic, description: "Valeward steel freed from an ancient oath")
    };

    internal bool TryApplyPlayerStun(string source)
    {
        if (ControlGraceTurns > 0 || Player.FrozenTurns > 0) return false;
        // Duration is intentionally capped at one skipped action. Recovery is
        // granted when that action is consumed in HandleKey.
        Player.FrozenTurns = 1;
        AddMessage($"{source} disrupts you—but cannot chain into a stun lock.");
        return true;
    }

    public void GrantExperience(int amount)
    {
        Experience += amount;
        while (Experience >= ExperienceToLevel)
        {
            Experience -= ExperienceToLevel;
            Level++;
            SkillPoints += 2;
            Player.Hp = Player.MaxHp; Mana = MaxMana;
            Effects.Add(new FlourishEffect { Position = Player.Position, StartedAt = _visualNow, Seed = _random.Next(),
                Kind = FlourishKind.SkillBloom, Color = new Rgb(255, 220, 92), SecondaryColor = new Rgb(125, 155, 255), Duration = 1.4, Radius = 8 });
            OpenSkillTree();
            AddMessage($"LEVEL {Level}! You gain 2 skill points. Invest ranks or save them.");
            break;
        }
    }

    private List<SkillNode> BuildSkillTree(ClassKind characterClass) => SkillTreeFactory.Build(characterClass);

    private List<SkillNode> BuildLegacySkillTree(ClassKind characterClass)
    {
        var nodes = new List<SkillNode>();
        var levelGates = new[] { 2, 3, 5, 7 };
        void Branch(string branch, int branchIndex, params (string Name, string Description, LevelRewardKind Reward, SpellKind? Spell)[] entries)
        {
            for (var tier = 0; tier < entries.Length; tier++)
            {
                var entry = entries[tier];
                nodes.Add(new SkillNode
                {
                    Id = $"{branchIndex}:{tier}", Branch = branch, BranchIndex = branchIndex, Tier = tier,
                    Name = entry.Name, Description = entry.Description, RequiredLevel = levelGates[tier],
                    PrerequisiteId = tier == 0 ? null : $"{branchIndex}:{tier - 1}", Reward = entry.Reward, Spell = entry.Spell
                });
            }
        }

        switch (characterClass)
        {
            case ClassKind.Vanguard:
                Branch("STEEL", 0,
                    ("Tempered Edge", "+2 base attack", LevelRewardKind.Might, null),
                    ("Flame Cleave", SpellDescription(SpellKind.FlameCleave), LevelRewardKind.LearnSpell, SpellKind.FlameCleave),
                    ("Executioner", "+2 base attack", LevelRewardKind.Might, null),
                    ("War Incarnate", "+8 max HP", LevelRewardKind.Vitality, null));
                Branch("BULWARK", 1,
                    ("Iron Skin", "+2 base defense", LevelRewardKind.Guard, null),
                    ("Deep Reserves", "+8 max HP", LevelRewardKind.Vitality, null),
                    ("Battle Cry", SpellDescription(SpellKind.BattleCry), LevelRewardKind.LearnSpell, SpellKind.BattleCry),
                    ("Unbroken", "+8 max HP", LevelRewardKind.Vitality, null));
                Branch("EARTHFIRE", 2,
                    ("Seismic Wave", SpellDescription(SpellKind.SeismicWave), LevelRewardKind.LearnSpell, SpellKind.SeismicWave),
                    ("Fireball", SpellDescription(SpellKind.Fireball), LevelRewardKind.LearnSpell, SpellKind.Fireball),
                    ("Luminous Nova", SpellDescription(SpellKind.LuminousNova), LevelRewardKind.LearnSpell, SpellKind.LuminousNova),
                    ("Ground Quake", SpellDescription(SpellKind.GroundQuake), LevelRewardKind.LearnSpell, SpellKind.GroundQuake));
                break;
            case ClassKind.Arcanist:
                Branch("ARCANA", 0,
                    ("Expanded Mind", "+5 maximum mana", LevelRewardKind.Arcana, null),
                    ("Prismatic Orb", SpellDescription(SpellKind.PrismaticOrb), LevelRewardKind.LearnSpell, SpellKind.PrismaticOrb),
                    ("Deep Current", "+5 maximum mana", LevelRewardKind.Arcana, null),
                    ("Archmagus", "+2 spell power", LevelRewardKind.Radiance, null));
                Branch("RUNES", 1,
                    ("Runic Blood", "+8 maximum HP", LevelRewardKind.Vitality, null),
                    ("Mana Ward", "+2 base defense", LevelRewardKind.Guard, null),
                    ("Blink", SpellDescription(SpellKind.Blink), LevelRewardKind.LearnSpell, SpellKind.Blink),
                    ("Perfect Ward", "+2 base defense", LevelRewardKind.Guard, null));
                Branch("ELEMENTS", 2,
                    ("Frost Lance", SpellDescription(SpellKind.FrostLance), LevelRewardKind.LearnSpell, SpellKind.FrostLance),
                    ("Chain Lightning", SpellDescription(SpellKind.ChainLightning), LevelRewardKind.LearnSpell, SpellKind.ChainLightning),
                    ("Meteor", SpellDescription(SpellKind.Meteor), LevelRewardKind.LearnSpell, SpellKind.Meteor),
                    ("Ice Storm", SpellDescription(SpellKind.IceStorm), LevelRewardKind.LearnSpell, SpellKind.IceStorm));
                break;
            case ClassKind.Ranger:
                Branch("HUNT", 0,
                    ("Deadeye", "+2 base attack", LevelRewardKind.Might, null),
                    ("Multishot", SpellDescription(SpellKind.Multishot), LevelRewardKind.LearnSpell, SpellKind.Multishot),
                    ("Heartseeker", "+2 base attack", LevelRewardKind.Might, null),
                    ("Apex Hunter", "+8 maximum HP", LevelRewardKind.Vitality, null));
                Branch("SURVIVAL", 1,
                    ("Trail Hardened", "+8 maximum HP", LevelRewardKind.Vitality, null),
                    ("Barkskin", "+2 base defense", LevelRewardKind.Guard, null),
                    ("Poison Cloud", SpellDescription(SpellKind.PoisonCloud), LevelRewardKind.LearnSpell, SpellKind.PoisonCloud),
                    ("Ghost Step", "+2 base defense", LevelRewardKind.Guard, null));
                Branch("GLOOM", 2,
                    ("Frost Lance", SpellDescription(SpellKind.FrostLance), LevelRewardKind.LearnSpell, SpellKind.FrostLance),
                    ("Chain Lightning", SpellDescription(SpellKind.ChainLightning), LevelRewardKind.LearnSpell, SpellKind.ChainLightning),
                    ("Fireball", SpellDescription(SpellKind.Fireball), LevelRewardKind.LearnSpell, SpellKind.Fireball),
                    ("Marked Shot", SpellDescription(SpellKind.MarkedShot), LevelRewardKind.LearnSpell, SpellKind.MarkedShot));
                break;
            default:
                Branch("DEVOTION", 0,
                    ("Living Radiance", "+2 spell power", LevelRewardKind.Radiance, null),
                    ("Solar Ray", SpellDescription(SpellKind.SolarRay), LevelRewardKind.LearnSpell, SpellKind.SolarRay),
                    ("Consecration", "+2 spell power", LevelRewardKind.Radiance, null),
                    ("Beacon Soul", "+5 maximum mana", LevelRewardKind.Arcana, null));
                Branch("AEGIS", 1,
                    ("Sacred Ward", "+2 base defense", LevelRewardKind.Guard, null),
                    ("Martyr's Heart", "+8 maximum HP", LevelRewardKind.Vitality, null),
                    ("Sanctuary", SpellDescription(SpellKind.Sanctuary), LevelRewardKind.LearnSpell, SpellKind.Sanctuary),
                    ("Undying Flame", "+8 maximum HP", LevelRewardKind.Vitality, null));
                Branch("MIRACLES", 2,
                    ("Luminous Nova", SpellDescription(SpellKind.LuminousNova), LevelRewardKind.LearnSpell, SpellKind.LuminousNova),
                    ("Shield Flare", SpellDescription(SpellKind.ShieldFlare), LevelRewardKind.LearnSpell, SpellKind.ShieldFlare),
                    ("Chain Lightning", SpellDescription(SpellKind.ChainLightning), LevelRewardKind.LearnSpell, SpellKind.ChainLightning),
                    ("Smite", SpellDescription(SpellKind.Smite), LevelRewardKind.LearnSpell, SpellKind.Smite));
                break;
        }
        return nodes;
    }

    private void OpenSkillTree()
    {
        Mode = GameMode.SkillTree;
        var available = SkillTree.FindIndex(CanPurchaseSkill);
        MenuSelection = available >= 0 ? available : Math.Clamp(MenuSelection, 0, Math.Max(0, SkillTree.Count - 1));
    }

    private void HandleSkillTreeKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.T or ConsoleKey.Q) { Mode = GameMode.Playing; return; }
        var node = SkillTree[Math.Clamp(MenuSelection, 0, SkillTree.Count - 1)];
        var branch = node.BranchIndex; var tier = node.Tier;
        if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K) tier = Math.Max(0, tier - 1);
        else if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J) tier = Math.Min(SkillTierCount - 1, tier + 1);
        else if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.A or ConsoleKey.H) branch = (branch + SkillBranchCount - 1) % SkillBranchCount;
        else if (key.Key is ConsoleKey.RightArrow or ConsoleKey.D or ConsoleKey.L) branch = (branch + 1) % SkillBranchCount;
        else if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar) { PurchaseSkill(node); return; }
        else return;
        MenuSelection = branch * SkillTierCount + tier;
    }

    public bool CanPurchaseSkill(SkillNode node)
    {
        if (node.Maxed || SkillPoints <= 0 || Level < node.RequiredLevel) return false;
        return node.PrerequisiteId is null || SkillTree.Any(other => other.Id == node.PrerequisiteId && other.Purchased);
    }

    public string SkillLockReason(SkillNode node)
    {
        if (node.Maxed) return "MAX RANK";
        if (Level < node.RequiredLevel) return $"REQUIRES LEVEL {node.RequiredLevel}";
        if (node.PrerequisiteId is not null && !SkillTree.Any(other => other.Id == node.PrerequisiteId && other.Purchased)) return "PREREQUISITE LOCKED";
        if (SkillPoints <= 0) return "NO SKILL POINTS";
        return node.Rank > 0 ? "RANK UP" : "AVAILABLE";
    }

    private void PurchaseSkill(SkillNode node)
    {
        if (!CanPurchaseSkill(node)) { AddMessage(SkillLockReason(node)); return; }
        var learnedNewSpell = node.Spell is { } offeredSpell && !_learnedSpells.Contains(offeredSpell);
        SkillPoints--; node.Rank++;
        switch (node.Reward)
        {
            case LevelRewardKind.LearnSpell when node.Spell is { } spell: if (!_learnedSpells.Contains(spell)) _learnedSpells.Add(spell); break;
            case LevelRewardKind.Vitality: Player.MaxHp += 4; Constitution++; break;
            case LevelRewardKind.Might: Player.Attack++; Strength++; break;
            case LevelRewardKind.Guard: Player.Defense++; break;
            case LevelRewardKind.Arcana: MaxMana += 3; Intellect++; break;
            case LevelRewardKind.Radiance: InnateSpellPower++; LanternRadius += 0.25; break;
        }
        Player.Hp = Player.MaxHp; Mana = MaxMana;
        Effects.Add(new FlourishEffect { Position = Player.Position, StartedAt = _visualNow, Seed = _random.Next(),
            Kind = FlourishKind.SkillBloom, Color = new Rgb(255, 218, 82), SecondaryColor = new Rgb(110, 175, 255), Duration = 1.15, Radius = 7 });
        AddMessage(learnedNewSpell
            ? $"You learn {node.Name}. Further ranks increase its power."
            : $"{node.Name} reaches rank {node.Rank}/{node.MaxRank}: {node.Description}.");
    }

    public int SpellInvestment(SpellKind spell) => SkillTree.Where(node => node.Spell == spell).Sum(node => node.Rank);
    public int SpellSkillBonus(SpellKind spell) => SpellInvestment(spell) * 2;

    private void PickUpItem()
    {
        var ground = CurrentMap.GroundItems.FirstOrDefault(i => i.Position == Player.Position);
        if (ground is null) { AddMessage("There is nothing here to pick up."); return; }
        AddInventoryItem(ground.Item.Copy(ground.Item.Quantity), false);
        CurrentMap.GroundItems.Remove(ground);
        CommitTurn($"You pick up {ground.Item.Quantity}× {ground.Item.Name}.");
    }

    public void AddItem(ItemKind kind, int quantity = 1, bool announce = true)
    {
        if (kind is ItemKind.Weapon or ItemKind.Armor)
        {
            AddEquipment(WorldBuilder.MakeItem(kind), announce);
            return;
        }
        var existing = Inventory.FirstOrDefault(i => i.Kind == kind);
        if (existing is null) Inventory.Add(WorldBuilder.MakeItem(kind).Copy(quantity));
        else existing.Quantity += quantity;
        if (WorldBuilder.MakeItem(kind).IsMaterial) RecordQuestProgress(QuestObjectiveKind.GatherMaterial);
        if (announce) AddMessage($"Added {quantity}× {WorldBuilder.MakeItem(kind).Name} to your inventory.");
    }

    public void AddEquipment(ItemStack item, bool announce = true) => AddInventoryItem(item, announce);

    private void AddInventoryItem(ItemStack item, bool announce)
    {
        if (!item.IsEquippable)
        {
            var existing = Inventory.FirstOrDefault(i => !i.IsEquippable && i.Kind == item.Kind);
            if (existing is not null) existing.Quantity += item.Quantity;
            else Inventory.Add(item);
            if (item.IsMaterial) RecordQuestProgress(QuestObjectiveKind.GatherMaterial);
        }
        else
        {
            item.Equipped = false;
            item.Quantity = 1;
            Inventory.Add(item);
        }
        if (announce) AddMessage($"Added {RarityName(item)}{item.Name} to your inventory.");
    }

    private void HandleInventoryKey(ConsoleKeyInfo key, double now)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.I or ConsoleKey.Q) { Mode = GameMode.Playing; return; }
        if (Inventory.Count == 0) return;
        if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K)
        { MenuSelection = (MenuSelection - 1 + Inventory.Count) % Inventory.Count; return; }
        if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J)
        { MenuSelection = (MenuSelection + 1) % Inventory.Count; return; }
        if (key.Key == ConsoleKey.D) { DropSelectedItem(); return; }
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar or ConsoleKey.E)
        { UseItem(Inventory[Math.Clamp(MenuSelection, 0, Inventory.Count - 1)], now); return; }
        var index = NumericSelection(key);
        if (index >= 0 && index < Inventory.Count) { MenuSelection = index; UseItem(Inventory[index], now); }
    }

    private void UseFirst(ItemKind kind, double now)
    {
        var item = Inventory.FirstOrDefault(i => i.Kind == kind);
        if (item is null) { AddMessage($"You have no {WorldBuilder.MakeItem(kind).Name}."); return; }
        UseItem(item, now);
    }

    private void UseItem(ItemStack item, double now)
    {
        if (item.IsMaterial) { AddMessage($"{item.Name} is a crafting material. Visit a settlement workshop and press F3."); return; }
        if (item.IsEquippable) { ToggleEquipment(item); return; }
        string message;
        switch (item.Kind)
        {
            case ItemKind.HealingPotion:
                if (Player.Hp >= Player.MaxHp) { AddMessage("You are already unhurt."); return; }
                var healed = Math.Min(14 + Constitution / 4, Player.MaxHp - Player.Hp);
                Player.Hp += healed;
                message = $"The potion restores {healed} HP.";
                break;
            case ItemKind.GreaterHealingPotion:
                if (Player.Hp >= Player.MaxHp) { AddMessage("You are already unhurt."); return; }
                var greaterHeal = Math.Min(30 + Constitution / 2, Player.MaxHp - Player.Hp);
                Player.Hp += greaterHeal; message = $"The crimson elixir restores {greaterHeal} HP."; break;
            case ItemKind.Ration: Food = Math.Min(100, Food + 38); message = "You eat a dense ration. Food restored."; break;
            case ItemKind.WaterFlask: Water = Math.Min(100, Water + 45); message = "Cold water clears the dust from your throat."; break;
            case ItemKind.ManaTonic: Mana = Math.Min(MaxMana, Mana + 8); message = "Violet current refills your spellwork."; break;
            case ItemKind.MightElixir: MightTurns = 20; message = "Dragonroot heat floods your limbs: +4 attack for 20 turns."; break;
            case ItemKind.GuardElixir: GuardTurns = 20; message = "Your skin hardens like smoked iron: +4 defense for 20 turns."; break;
            case ItemKind.FocusElixir: FocusTurns = 20; message = "Every mote of light snaps into focus: +4 spell power for 20 turns."; break;
            case ItemKind.SmokeBomb:
                Effects.Add(new SmokeEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Duration = 5.0 });
                foreach (var monster in Monsters.Where(m => m.IsAlive && Chebyshev(m.Position, Player.Position) <= 4)) monster.FrozenTurns = Math.Max(monster.FrozenTurns, 2);
                message = "Silver smoke floods the passage; nearby creatures lose two turns."; break;
            case ItemKind.Waystone:
                Player.Position = IsWilderness ? CurrentMap.Start : CurrentMap.IsTown ? CurrentMap.Start : CurrentMap.StairsUp;
                Effects.Add(new ExplosionEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(), Duration = 0.65,
                    HotColor = Rgb.White, CoolColor = new Rgb(95, 145, 255) });
                Effects.Add(new FlourishEffect { Position = Player.Position, StartedAt = now, Seed = _random.Next(),
                    Kind = FlourishKind.StairArrival, Color = new Rgb(95, 145, 255), Duration = 0.8, Radius = 12 });
                message = "The waystone collapses distance and returns you to the upward stair."; break;
            case ItemKind.Ambrosia:
                Food = 100; Water = 100; Mana = MaxMana;
                message = "Sun-fruit ambrosia restores food, water, and mana completely."; break;
            case ItemKind.Bomb:
                Consume(item); Mode = GameMode.Playing; _pendingBomb = true; AwaitingDirection = true;
                AddMessage("Aim the bomb — choose a direction."); return;
            case ItemKind.Torch:
                Consume(item); Mode = GameMode.Playing; _pendingWallTorch = true; AwaitingDirection = true;
                AddMessage("Mount wall torch — choose an adjacent wall."); return;
            default: GrantExperience(30); Mana = MaxMana; message = "The crystal dissolves into knowledge and cold light."; break;
        }
        Consume(item);
        if (Mode != GameMode.SkillTree) Mode = GameMode.Playing;
        CommitTurn(message);
    }

    private void Consume(ItemStack item) { item.Quantity--; if (item.Quantity <= 0) Inventory.Remove(item); }

    private void ToggleEquipment(ItemStack item)
    {
        if (item.Slot is null) return;
        if (item.Equipped)
        {
            item.Equipped = false;
            AddMessage($"You remove the {item.Name}.");
        }
        else
        {
            var previous = Inventory.FirstOrDefault(i => i.Equipped && i.Slot == item.Slot);
            if (previous is not null) previous.Equipped = false;
            item.Equipped = true;
            AddMessage(previous is null
                ? $"You equip the {item.Name}. {item.BonusText}."
                : $"You replace {previous.Name} with {item.Name}. {item.BonusText}.");
        }
        Mode = GameMode.Playing;
        CommitTurn(null);
    }

    private void DropSelectedItem()
    {
        if (Inventory.Count == 0) return;
        var item = Inventory[Math.Clamp(MenuSelection, 0, Inventory.Count - 1)];
        var dropped = item.Copy(1);
        dropped.Equipped = false;
        CurrentMap.GroundItems.Add(new GroundItem(Player.Position, dropped));
        if (item.IsEquippable || item.Quantity == 1) Inventory.Remove(item);
        else item.Quantity--;
        MenuSelection = Math.Clamp(MenuSelection, 0, Math.Max(0, Inventory.Count - 1));
        Mode = GameMode.Playing;
        CommitTurn($"You drop the {item.Name}.");
    }

    private void Interact()
    {
        var resident = CurrentMap.Npcs.Where(npc => Chebyshev(npc.Position, Player.Position) <= 1)
            .OrderBy(npc => npc.Position.DistanceSquared(Player.Position)).FirstOrDefault();
        if (resident is not null) { BeginDialogue(resident); return; }
        if (CurrentMap.TravelPoints.TryGetValue(Player.Position, out var travel))
        { TravelTo(travel, Player.Position); return; }
        if (Player.Position == CurrentMap.StairsDown)
        {
            if (CurrentMap == _town)
            {
                CurrentMap = _dungeons[0];
                Player.Position = CurrentMap.StairsUp;
                RefreshDetectionState();
                BeginArrivalFlourish();
                AddMessage($"You descend to depth 1 of {_dungeons.Count}. Strange chambers wait below.");
            }
            else if (CurrentDepth < _dungeons.Count)
            {
                CurrentMap = _dungeons[CurrentDepth];
                Player.Position = CurrentMap.StairsUp;
                RefreshDetectionState();
                BeginArrivalFlourish();
                AddMessage($"You descend to {CurrentMap.Name}. The dark grows heavier.");
            }
            else AddMessage("The final stair ends before a door of seamless black glass.");
            return;
        }
        if (Player.Position == CurrentMap.StairsUp && !CurrentMap.IsTown)
        {
            if (CurrentDepth == 1)
            {
                CurrentMap = _town; Player.Position = _town.StairsDown; RefreshDetectionState(); BeginArrivalFlourish();
                AddMessage("Cool night air meets you on the road to Emberfall.");
            }
            else
            {
                CurrentMap = _dungeons[CurrentDepth - 2];
                Player.Position = CurrentMap.StairsDown;
                RefreshDetectionState();
                BeginArrivalFlourish();
                AddMessage($"You climb back to {CurrentMap.Name}.");
            }
            return;
        }
        var wallTorch = CurrentMap.WallTorches.Values.FirstOrDefault(entry => Chebyshev(entry.Position, Player.Position) <= 1);
        if (wallTorch is not null) { TakeWallTorch(wallTorch); return; }
        var provision = CurrentMap.Provisions.Values.FirstOrDefault(entry => Chebyshev(entry.Position, Player.Position) <= 1);
        if (provision is not null) { GatherProvision(provision); return; }
        var machine = CurrentMap.Machines.Values.FirstOrDefault(entry => Chebyshev(entry.Position, Player.Position) <= 1);
        if (machine is not null) { OperateMachine(machine); return; }
        var nearbyDoor = CurrentMap.Doors.Values.FirstOrDefault(entry => Chebyshev(entry.Position, Player.Position) <= 1 && entry.State == DoorState.Open);
        if (nearbyDoor is not null)
        {
            if (Player.Position == nearbyDoor.Position || Monsters.Any(monster => monster.IsAlive && monster.Position == nearbyDoor.Position))
            { AddMessage("Something occupies the doorway."); return; }
            nearbyDoor.State = DoorState.Closed; CurrentMap.Set(nearbyDoor.Position, WorldBuilder.ClosedDoor);
            CommitTurn("You pull the door closed, cutting the line of sight."); return;
        }
        var shop = CurrentMap.Shops.FirstOrDefault(s => s.Position == Player.Position);
        if (shop is null) { AddMessage("There is nothing here to use."); return; }
        if (!IsShopOpen(shop)) { AddMessage($"{shop.Name} is shuttered. Trading hours are 07:00–18:00; the residents have gone home."); return; }
        ActiveShop = shop; Mode = GameMode.Shopping; MenuSelection = 0;
    }

    private void HandleShopKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.E or ConsoleKey.Q)
        {
            Mode = GameMode.Playing; ActiveShop = null; AddMessage("You step back into the lantern light."); return;
        }
        if (ActiveShop is null) return;
        if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K)
        { MenuSelection = (MenuSelection - 1 + ActiveShop.Items.Count) % ActiveShop.Items.Count; return; }
        if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J)
        { MenuSelection = (MenuSelection + 1) % ActiveShop.Items.Count; return; }
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        { PurchaseShopItem(MenuSelection); return; }
        var index = NumericSelection(key);
        if (index < 0 || index >= ActiveShop.Items.Count) return;
        MenuSelection = index;
        PurchaseShopItem(index);
    }

    private void PurchaseShopItem(int index)
    {
        if (ActiveShop is null || index < 0 || index >= ActiveShop.Items.Count) return;
        var item = ActiveShop.Items[index];
        if (Player.Gold < item.Price) { AddMessage("Not enough gold. The shopkeeper taps the price tag."); return; }
        Player.Gold -= item.Price; item.Purchase(this);
    }

    private void HandleSpellbookKey(ConsoleKeyInfo key, double now)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.Z or ConsoleKey.Q) { Mode = GameMode.Playing; return; }
        if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K)
        { MenuSelection = (MenuSelection - 1 + KnownSpells.Count) % KnownSpells.Count; return; }
        if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J)
        { MenuSelection = (MenuSelection + 1) % KnownSpells.Count; return; }
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        { var selected = KnownSpells[MenuSelection]; Mode = GameMode.Playing; BeginSpell(selected, now); return; }
        var index = NumericSelection(key);
        if (index >= 0 && index < KnownSpells.Count) { MenuSelection = index; var spell = KnownSpells[index]; Mode = GameMode.Playing; BeginSpell(spell, now); }
    }

    private void BeginInspect()
    {
        CursorPosition = Player.Position;
        Mode = GameMode.Inspect;
        AddMessage("Inspect mode — move cursor, Tab cycles visible creatures, X/Esc closes.");
    }

    private void HandleCursorKey(ConsoleKeyInfo key, double now, bool targeting)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.X or ConsoleKey.Q)
        { Mode = GameMode.Playing; CursorPosition = null; _pendingSpell = null; return; }
        if (key.Key == ConsoleKey.Tab)
        {
            CycleMonsterTarget();
            return;
        }
        var direction = DirectionFromKey(key);
        if (direction is { } delta && CursorPosition is { } cursor)
        {
            var next = cursor + delta;
            if (CurrentMap.Contains(next)) CursorPosition = next;
            return;
        }
        if ((key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar) && targeting && CursorPosition is { } target && _pendingSpell is { } spell)
            CastSpellAt(spell, target, now);
    }

    private void CycleMonsterTarget()
    {
        var visible = Monsters.Where(m => m.IsAlive && HasClearSight(Player.Position, m.Position))
            .OrderBy(m => m.Position.DistanceSquared(Player.Position)).ToList();
        if (visible.Count == 0) { AddMessage("No visible creatures to target."); return; }
        var index = CursorPosition is { } p ? visible.FindIndex(m => m.Position == p) : -1;
        CursorPosition = visible[(index + 1) % visible.Count].Position;
    }

    private Actor? NearestVisibleMonster() => Monsters.Where(m => m.IsAlive && HasClearSight(Player.Position, m.Position))
        .OrderBy(m => m.Position.DistanceSquared(Player.Position)).FirstOrDefault();

    public string DescribeAt(Point p)
    {
        if (!CurrentMap.Contains(p)) return "the edge of the known world";
        if (p == Player.Position) return $"you — {Race} {Class}, level {Level}";
        var resident = CurrentMap.Npcs.FirstOrDefault(npc => npc.Position == p);
        if (resident is not null)
        {
            var quest = QuestForNpc(resident);
            var questState = quest?.Status switch
            {
                QuestStatus.Available => "quest available",
                QuestStatus.Active => $"quest in progress {quest.ProgressText}",
                QuestStatus.ReadyToTurnIn => "quest ready to turn in",
                QuestStatus.Completed => "quest completed",
                _ => "resident"
            };
            return $"{resident.Name} · {resident.Role} · {resident.ScheduleState} · {questState} · bump or press E to talk";
        }
        var monster = Monsters.FirstOrDefault(m => m.IsAlive && m.Position == p);
        if (monster is not null)
        {
            if (monster.AlertTurns <= 0 && (monster.Name.Contains("mimic") || monster.Name.Contains("gargoyle"))) monster.AlertTurns = 1;
            return $"{monster.Name} · HP {monster.Hp}/{monster.MaxHp} · ATK {monster.Attack} · DEF {monster.Defense} · {monster.Ai} · {monster.Faction} · intent {monster.Intent.ToString().ToUpperInvariant()} · {(monster.AlertTurns > 0 ? "alert" : "unaware")}";
        }
        var loot = CurrentMap.GroundItems.FirstOrDefault(i => i.Position == p);
        if (loot is not null) return $"{RarityName(loot.Item)}{loot.Item.Name} · {loot.Item.BonusText}";
        if (CurrentMap.WallTorches.ContainsKey(p))
        {
            var flame = CurrentMap.ExtinguishedLights.Contains(p) ? "extinguished" : "lit";
            return $"{flame} mounted wall torch · press E while adjacent to take it";
        }
        if (CurrentMap.Provisions.TryGetValue(p, out var provision))
            return $"{provision.Name} · {provision.Charges} serving{(provision.Charges == 1 ? "" : "s")} left · press E while adjacent";
        if (CurrentMap.Doors.TryGetValue(p, out var door)) return $"{door.State.ToString().ToLowerInvariant()} door · blocks sight when closed";
        if (CurrentMap.Traps.TryGetValue(p, out var trap) && trap.Revealed) return $"{TrapName(trap.Kind)} · {(trap.Armed ? "armed" : "spent")}";
        if (CurrentMap.Machines.TryGetValue(p, out var machine)) return $"{machine.Kind} · {(machine.Active ? "active" : "inactive")} · press E while adjacent";
        if (CurrentMap.TravelPoints.TryGetValue(p, out var travel))
            return travel switch
            {
                TravelKind.Wilderness => "west gate · walk through to explore the Ashwind March",
                TravelKind.Town => "east road · walk through to return to Emberfall",
                TravelKind.WildernessReturn => $"exit from {CurrentMap.Name} · walk through to return to the March",
                _ => $"entrance to {TravelName(travel)} · walk through to explore"
            };
        var tile = CurrentMap[p];
        var state = tile.Destructible ? " · destructible" : tile.Walkable ? " · passable" : " · blocks movement";
        var elements = ElementDescription(CurrentMap.Elements[p.X, p.Y]);
        return tile.Name + state + (elements == "clear ground" ? "" : $" · {elements}");
    }

    public static string TravelName(TravelKind travel) => travel switch
    {
        TravelKind.HollowrootMine => "Hollowroot Mine",
        TravelKind.SunkenBarrow => "Sunken Barrow",
        TravelKind.MoonwellCavern => "Moonwell Cavern",
        TravelKind.CastleUndercroft => "Castle Valeward Undercroft",
        TravelKind.WildernessReturn => "the Ashwind March",
        TravelKind.Town => "Emberfall",
        _ => "the Ashwind March"
    };

    private void HandleCharacterKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.C or ConsoleKey.Q) { Mode = GameMode.Playing; return; }
        if (key.Key == ConsoleKey.I) { Mode = GameMode.Inventory; MenuSelection = 0; return; }
        if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K or ConsoleKey.LeftArrow or ConsoleKey.A or ConsoleKey.H)
        { MenuSelection = (MenuSelection - 1 + EquipmentSlots.Length) % EquipmentSlots.Length; return; }
        if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J or ConsoleKey.RightArrow or ConsoleKey.D or ConsoleKey.L)
        { MenuSelection = (MenuSelection + 1) % EquipmentSlots.Length; return; }
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            var slot = EquipmentSlots[MenuSelection];
            var item = Inventory.FirstOrDefault(i => i.Equipped && i.Slot == slot);
            if (item is not null) { item.Equipped = false; AddMessage($"You remove the {item.Name}."); }
        }
    }

    public static readonly EquipmentSlot[] EquipmentSlots = Enum.GetValues<EquipmentSlot>();

    private void CancelAiming()
    {
        if (_pendingBomb) AddItem(ItemKind.Bomb, 1, false);
        if (_pendingWallTorch) AddItem(ItemKind.Torch, 1, false);
        AwaitingDirection = false;
        _pendingSpell = null;
        _pendingBomb = false;
        _pendingWallTorch = false;
    }

    public List<SpellKind> KnownSpells => _learnedSpells.ToList();

    public List<(SpellKind Spell, int Level)> ClassSpellTree => Class switch
    {
        ClassKind.Vanguard => [(SpellKind.Stonebreak, 1), (SpellKind.ShieldFlare, 1), (SpellKind.SeismicWave, 2), (SpellKind.FlameCleave, 3),
            (SpellKind.Fireball, 3), (SpellKind.BattleCry, 5), (SpellKind.LuminousNova, 5), (SpellKind.GroundQuake, 7)],
        ClassKind.Arcanist => [(SpellKind.ArcaneBeam, 1), (SpellKind.Fireball, 1), (SpellKind.FrostLance, 2), (SpellKind.PrismaticOrb, 3),
            (SpellKind.ChainLightning, 3), (SpellKind.Blink, 5), (SpellKind.Meteor, 5), (SpellKind.IceStorm, 7)],
        ClassKind.Ranger => [(SpellKind.ThornVolley, 1), (SpellKind.ShadowBolt, 1), (SpellKind.FrostLance, 2), (SpellKind.Multishot, 3),
            (SpellKind.ChainLightning, 4), (SpellKind.PoisonCloud, 5), (SpellKind.Fireball, 6), (SpellKind.MarkedShot, 7)],
        _ => [(SpellKind.SunLance, 1), (SpellKind.HealingLight, 1), (SpellKind.LuminousNova, 2), (SpellKind.SolarRay, 3),
            (SpellKind.ShieldFlare, 3), (SpellKind.Sanctuary, 5), (SpellKind.ChainLightning, 5), (SpellKind.Smite, 7)]
    };

    public int SpellCost(SpellKind spell)
    {
        var baseCost = spell switch
        {
            SpellKind.Fireball => 3, SpellKind.FrostLance => 2, SpellKind.ChainLightning => 5,
            SpellKind.LuminousNova => 6, SpellKind.Meteor => 9, SpellKind.ArcaneBeam => 3,
            SpellKind.SeismicWave => 6, SpellKind.ShieldFlare => 4, SpellKind.ThornVolley => 3,
            SpellKind.ShadowBolt => 2, SpellKind.SunLance => 3, SpellKind.HealingLight => 5,
            SpellKind.FlameCleave => 4, SpellKind.GroundQuake => 7, SpellKind.BattleCry => 5,
            SpellKind.IceStorm => 7, SpellKind.Blink => 4, SpellKind.PrismaticOrb => 5,
            SpellKind.Multishot => 4, SpellKind.PoisonCloud => 5, SpellKind.MarkedShot => 5,
            SpellKind.SolarRay => 6, SpellKind.Sanctuary => 7, SpellKind.Smite => 7, _ => 3
        };
        var classCost = Class == ClassKind.Arcanist ? Math.Max(1, baseCost - 1) : baseCost;
        return Math.Max(1, classCost - (SpellInvestment(spell) >= 3 ? 1 : 0));
    }

    public static string SpellName(SpellKind spell) => spell switch
    {
        SpellKind.Fireball => "Fireball", SpellKind.FrostLance => "Frost Lance", SpellKind.ChainLightning => "Chain Lightning",
        SpellKind.LuminousNova => "Luminous Nova", SpellKind.Stonebreak => "Stonebreak", SpellKind.ArcaneBeam => "Arcane Beam",
        SpellKind.Meteor => "Meteor", SpellKind.SeismicWave => "Seismic Wave", SpellKind.ShieldFlare => "Shield Flare",
        SpellKind.ThornVolley => "Thorn Volley", SpellKind.ShadowBolt => "Shadow Bolt", SpellKind.SunLance => "Sun Lance",
        SpellKind.HealingLight => "Healing Light", SpellKind.FlameCleave => "Flame Cleave", SpellKind.GroundQuake => "Ground Quake",
        SpellKind.BattleCry => "Battle Cry", SpellKind.IceStorm => "Ice Storm", SpellKind.Blink => "Blink",
        SpellKind.PrismaticOrb => "Prismatic Orb", SpellKind.Multishot => "Multishot", SpellKind.PoisonCloud => "Poison Cloud",
        SpellKind.MarkedShot => "Marked Shot", SpellKind.SolarRay => "Solar Ray", SpellKind.Sanctuary => "Sanctuary", _ => "Smite"
    };

    public static string SpellDescription(SpellKind spell) => spell switch
    {
        SpellKind.Fireball => "travelling fire; blast and terrain damage",
        SpellKind.FrostLance => "fast ice ray; freezes its target",
        SpellKind.ChainLightning => "arcs through up to three visible enemies",
        SpellKind.LuminousNova => "radiant blast centered on you",
        SpellKind.Stonebreak => "directed fault-line that shatters one wall",
        SpellKind.ArcaneBeam => "piercing violet ray that strikes two enemies",
        SpellKind.Meteor => "targeted four-tile inferno; devastating terrain damage",
        SpellKind.SeismicWave => "fault-line that continues through creatures and walls",
        SpellKind.ShieldFlare => "+4 defense for seven turns",
        SpellKind.ThornVolley => "spectral thorns burst around a target tile",
        SpellKind.ShadowBolt => "cheap precise projectile empowered by dexterity",
        SpellKind.SunLance => "radiant projectile empowered by wisdom",
        SpellKind.HealingLight => "restore health with living light",
        SpellKind.FlameCleave => "short fiery cone empowered by weapon damage",
        SpellKind.GroundQuake => "wide tectonic burst that pulverizes terrain",
        SpellKind.BattleCry => "+4 attack and defense for ten turns",
        SpellKind.IceStorm => "targeted blizzard; freezes every creature caught",
        SpellKind.Blink => "teleport to a visible open tile",
        SpellKind.PrismaticOrb => "high-damage projectile with chromatic impact",
        SpellKind.Multishot => "four ghost-arrows converge around a target",
        SpellKind.PoisonCloud => "lingering venom that damages for six turns",
        SpellKind.MarkedShot => "heavy projectile empowered by bow and dexterity",
        SpellKind.SolarRay => "piercing corridor of concentrated daylight",
        SpellKind.Sanctuary => "heal and gain a twelve-turn ward",
        _ => "detonate focused judgment around a target"
    };

    public static int SpellRange(SpellKind spell) => spell switch
    {
        SpellKind.Meteor => 14, SpellKind.ChainLightning => 16, SpellKind.FrostLance => 18,
        SpellKind.Stonebreak or SpellKind.SeismicWave or SpellKind.GroundQuake => 10,
        SpellKind.FlameCleave => 4, SpellKind.Blink => 9, SpellKind.IceStorm or SpellKind.PoisonCloud or SpellKind.Smite => 12,
        SpellKind.LuminousNova or SpellKind.ShieldFlare or SpellKind.HealingLight or SpellKind.BattleCry or SpellKind.Sanctuary => 0,
        _ => 15
    };

    public IEnumerable<LightSource> GetDynamicLights(double now)
    {
        var lanternColor = Race == RaceKind.Ashborn ? new Rgb(255, 116, 65) : new Rgb(255, 176, 82);
        if (LanternLit) yield return new LightSource(Player.Position, lanternColor, EffectiveLanternRadius, LanternIntensity, 0.075, 13.37, 0.48, 0.2, 0.14);
        foreach (var monster in Monsters.Where(m => m.IsAlive && m.GlowColor is not null))
            yield return new LightSource(monster.Position, monster.GlowColor!.Value, 5.5, 0.7, 0.12, monster.Position.X, 0.4, 0.2, 0.1);
        for (var y = 0; y < CurrentMap.Height; y++)
        for (var x = 0; x < CurrentMap.Width; x++)
            if (CurrentMap.Elements[x, y].Fire > 0)
                yield return new LightSource(new Point(x, y), new Rgb(255, 92, 18), 7, 1.0, 0.2, x * 0.9 + y, 0.45, 0.18, 0.12);
        foreach (var light in Effects.SelectMany(effect => effect.GetLights(now))) yield return light;
    }

    public char? EffectGlyphAt(Point p, double now, out Rgb color)
    {
        foreach (var projectile in Effects.OfType<ProjectileEffect>())
        {
            if (projectile.Current(now) == p) { color = projectile.CoreColor; return projectile.Glyph; }
            var current = projectile.Current(now);
            var index = projectile.Path.IndexOf(current);
            for (var trail = 1; trail <= 3; trail++)
            {
                if (index < trail || projectile.Path[index - trail] != p) continue;
                color = projectile.Spell == SpellKind.PrismaticOrb
                    ? (trail % 3) switch { 0 => new Rgb(255, 85, 220), 1 => new Rgb(80, 235, 255), _ => new Rgb(230, 245, 110) }
                    : projectile.OuterColor.Scale(0.82 - trail * 0.16);
                return projectile.Spell switch
                {
                    SpellKind.FrostLance => trail == 1 ? '◇' : '·',
                    SpellKind.PrismaticOrb => trail == 1 ? '✧' : '·',
                    SpellKind.MarkedShot => trail == 1 ? '›' : '·',
                    SpellKind.ShadowBolt => '∙',
                    _ => trail == 1 ? '•' : '·'
                };
            }
        }
        foreach (var arc in Effects.OfType<ArcEffect>())
        {
            if (arc.Points.Contains(p) && Hash(p, (int)(now * 45)) % 3 != 0)
            {
                color = arc.Color;
                return arc.Style switch
                {
                    ArcStyle.Flame => Hash(p, (int)(now * 60)) % 2 == 0 ? '⌁' : '▲',
                    ArcStyle.Thorn => Hash(p, (int)(now * 60)) % 2 == 0 ? '✣' : '›',
                    ArcStyle.Arrow => Hash(p, (int)(now * 60)) % 2 == 0 ? '➤' : '›',
                    ArcStyle.Solar => Hash(p, (int)(now * 60)) % 2 == 0 ? '═' : '✦',
                    ArcStyle.Seismic => Hash(p, (int)(now * 60)) % 2 == 0 ? '⌁' : '╱',
                    ArcStyle.Blink => Hash(p, (int)(now * 60)) % 2 == 0 ? '∿' : '·',
                    ArcStyle.Beam => Hash(p, (int)(now * 60)) % 2 == 0 ? '─' : '╱',
                    _ => Hash(p, (int)(now * 60)) % 2 == 0 ? 'ϟ' : '╱'
                };
            }
        }
        foreach (var flourish in Effects.OfType<FlourishEffect>())
        {
            var t = Math.Clamp((now - flourish.StartedAt) / flourish.Duration, 0, 1);
            var dx = p.X - flourish.Position.X;
            var dy = (p.Y - flourish.Position.Y) * 1.8;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var hash = Hash(p, flourish.Seed + (int)(now * 24));
            if (flourish.Kind is FlourishKind.Impact or FlourishKind.CriticalImpact)
            {
                if (flourish.Kind == FlourishKind.CriticalImpact && p == flourish.Position && t < 0.22)
                { color = flourish.Color; return '✦'; }
                // Normal hits read as a brief local light/color response. A rare
                // single mote off the occupied cell adds direction without
                // replacing the combatants with a large impact symbol.
                if (distance is >= 1 and <= 2.2 && hash % 17 == 0 && t < 0.45)
                { color = Rgb.Lerp(flourish.Color, flourish.SecondaryColor, t); return '·'; }
            }
            else if (flourish.Kind == FlourishKind.Steam && distance < 1 + t * flourish.Radius && hash % 3 != 0)
            { color = flourish.Color.Scale(1 - t * 0.55); return t < 0.45 ? '░' : '˙'; }
            else if (flourish.Kind == FlourishKind.FrostCrack && Math.Abs(distance - t * flourish.Radius) < 0.8 && hash % 3 != 0)
            { color = Rgb.Lerp(flourish.Color, flourish.SecondaryColor, t); return hash % 2 == 0 ? '◇' : '⌁'; }
            else if (flourish.Kind == FlourishKind.ElectricBranch && distance <= flourish.Radius * t + 1 && hash % 5 == 0)
            { color = flourish.Color; return hash % 2 == 0 ? 'ϟ' : '╱'; }
            else if (flourish.Kind == FlourishKind.PoisonCurl && distance <= 1 + t * flourish.Radius && hash % 4 == 0)
            { color = Rgb.Lerp(flourish.Color, flourish.SecondaryColor, t); return hash % 2 == 0 ? '∿' : '░'; }
            else if (flourish.Kind == FlourishKind.EmberCollapse && distance <= flourish.Radius * (1 - t * 0.35) && hash % 5 == 0)
            { color = Rgb.Lerp(flourish.Color, flourish.SecondaryColor, t); return t < 0.55 ? '✦' : '·'; }
            else if (flourish.Kind == FlourishKind.DustFall && Math.Abs(dx) <= flourish.Radius && dy >= -2 && dy <= flourish.Radius * t && hash % 9 == 0)
            { color = flourish.Color.Scale(1 - t * 0.45); return hash % 2 == 0 ? '·' : '˙'; }
            else if (flourish.Kind == FlourishKind.CrystalShatter && Math.Abs(distance - t * flourish.Radius) < 1.1 && hash % 3 != 0)
            { color = hash % 2 == 0 ? flourish.Color : flourish.SecondaryColor; return hash % 2 == 0 ? '✧' : '⋄'; }
            else if (flourish.Kind == FlourishKind.SanctuaryHalo && Math.Abs(distance - flourish.Radius * (1 - t * 0.78)) < 0.75)
            { color = Rgb.Lerp(flourish.Color, flourish.SecondaryColor, t); return hash % 2 == 0 ? '○' : '✦'; }
            else if (flourish.Kind == FlourishKind.StairArrival && Math.Abs(distance - t * flourish.Radius * 1.8) < 0.7 && hash % 3 != 0)
            { color = flourish.Color; return hash % 2 == 0 ? '·' : '✧'; }
            else if (flourish.Kind == FlourishKind.SkillBloom && Math.Abs(distance - t * flourish.Radius) < 0.9 && hash % 3 != 0)
            { color = hash % 2 == 0 ? flourish.Color : flourish.SecondaryColor; return hash % 4 == 0 ? '★' : '✦'; }
        }
        foreach (var explosion in Effects.OfType<ExplosionEffect>())
        {
            var t = Math.Clamp((now - explosion.StartedAt) / explosion.Duration, 0, 1);
            var dx = p.X - explosion.Position.X; var dy = (p.Y - explosion.Position.Y) * 1.8;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var shockRadius = t * 10;
            if (t < 0.58 && Math.Abs(distance - shockRadius) < 0.8)
            { color = Rgb.Lerp(explosion.HotColor, explosion.CoolColor, t); return Hash(p, explosion.Seed) % 2 == 0 ? '✦' : '○'; }
            var fireRadius = 1.2 + Math.Sin(Math.Min(1, t * 1.7) * Math.PI) * 4.1;
            if (distance <= fireRadius && Hash(p, explosion.Seed + (int)(now * 24)) % 4 != 0)
            {
                color = Rgb.Lerp(explosion.HotColor, explosion.CoolColor, Math.Min(1, t * 1.25));
                return (Hash(p, explosion.Seed + (int)(now * 31)) % 4) switch { 0 => '░', 1 => '▒', 2 => '▓', _ => '●' };
            }
            for (var i = 0; i < 18; i++)
            {
                var angle = (explosion.Seed * 0.0001 + i * 2.399) % (Math.PI * 2);
                var speed = 4 + Math.Abs(Hash(new Point(i, explosion.Seed), i) % 8);
                var spark = new Point(explosion.Position.X + (int)Math.Round(Math.Cos(angle) * speed * t),
                    explosion.Position.Y + (int)Math.Round(Math.Sin(angle) * speed * t / 1.8));
                if (spark == p && t < 0.8) { color = Rgb.Lerp(new Rgb(255, 248, 178), explosion.CoolColor, t); return t < 0.4 ? '✦' : '·'; }
            }
        }
        foreach (var smoke in Effects.OfType<SmokeEffect>())
        {
            var t = Math.Clamp((now - smoke.StartedAt) / smoke.Duration, 0, 1);
            for (var i = 0; i < 12; i++)
            {
                var angle = i * 2.399 + smoke.Seed * 0.00003; var radius = t * (2 + i % 5);
                var cloud = new Point(smoke.Position.X + (int)Math.Round(Math.Cos(angle) * radius),
                    smoke.Position.Y + (int)Math.Round(Math.Sin(angle) * radius / 1.8 - t * 2));
                if (cloud == p && Hash(p, i + (int)(now * 4)) % 3 != 0)
                { color = new Rgb(72, 66, 68).Scale(1 - t * 0.55); return i % 2 == 0 ? '░' : '▒'; }
            }
        }
        color = Rgb.White; return null;
    }

    public void AddMessage(string message) { _messages.Enqueue(message); while (_messages.Count > 8) _messages.Dequeue(); }

    private void MarkSurface(Point position, SurfaceMarkKind kind, char glyph, Rgb color)
    {
        if (!CurrentMap.Contains(position) || !CurrentMap[position].Walkable) return;
        CurrentMap.SurfaceMarks[position] = new SurfaceMark(position, kind, glyph, color, _turns);
        if (CurrentMap.SurfaceMarks.Count <= 180) return;
        var oldest = CurrentMap.SurfaceMarks.Values.OrderBy(mark => mark.CreatedTurn).First();
        CurrentMap.SurfaceMarks.Remove(oldest.Position);
    }

    private void BeginArrivalFlourish()
    {
        var color = CurrentMap.Biome switch
        {
            BiomeKind.Emberfall => new Rgb(255, 178, 86),
            BiomeKind.Wilds => new Rgb(126, 174, 104),
            BiomeKind.AshenVaults => new Rgb(215, 105, 55),
            BiomeKind.DrownedReliquary => new Rgb(70, 175, 235),
            BiomeKind.PrismaticFoundry => new Rgb(180, 105, 255),
            BiomeKind.LightlessCourt => new Rgb(100, 82, 150),
            _ => new Rgb(255, 72, 22)
        };
        Effects.Clear();
        Effects.Add(new FlourishEffect { Position = Player.Position, StartedAt = _visualNow, Seed = _random.Next(),
            Kind = FlourishKind.StairArrival, Color = color, SecondaryColor = Rgb.Black, Duration = 1.05, Radius = 18 });
    }

    private ItemKind RandomDropKind()
    {
        var choices = new[] { ItemKind.HealingPotion, ItemKind.GreaterHealingPotion, ItemKind.Ration, ItemKind.WaterFlask,
            ItemKind.ManaTonic, ItemKind.Bomb, ItemKind.SmokeBomb, ItemKind.MightElixir, ItemKind.GuardElixir,
            ItemKind.FocusElixir, ItemKind.Torch, ItemKind.Waystone, ItemKind.Ambrosia };
        return choices[_random.Next(choices.Length)];
    }

    public static string RarityName(ItemStack item) => item.Rarity == ItemRarity.Common ? "" : $"{item.Rarity.ToString().ToLowerInvariant()} ";

    private static bool IsInDirection(Point from, Point to, Point direction)
    {
        var dx = to.X - from.X; var dy = to.Y - from.Y;
        return dx * direction.X + dy * direction.Y > 0 && Math.Abs(dx * direction.Y - dy * direction.X) <= Math.Max(Math.Abs(dx), Math.Abs(dy));
    }

    private static int Chebyshev(Point a, Point b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    private static List<Point> Line(Point from, Point to)
    {
        var result = new List<Point>(); var x = from.X; var y = from.Y;
        var dx = Math.Abs(to.X - x); var sx = x < to.X ? 1 : -1;
        var dy = -Math.Abs(to.Y - y); var sy = y < to.Y ? 1 : -1; var error = dx + dy;
        while (true)
        {
            result.Add(new Point(x, y)); if (x == to.X && y == to.Y) break;
            var e2 = 2 * error; if (e2 >= dy) { error += dy; x += sx; } if (e2 <= dx) { error += dx; y += sy; }
        }
        return result;
    }

    private static readonly Point[] CardinalDirections = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];
    private static Point? DirectionFromKey(ConsoleKeyInfo key) => key.Key switch
    {
        ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K => new Point(0, -1), ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J => new Point(0, 1),
        ConsoleKey.LeftArrow or ConsoleKey.A or ConsoleKey.H => new Point(-1, 0), ConsoleKey.RightArrow or ConsoleKey.D or ConsoleKey.L => new Point(1, 0),
        ConsoleKey.Y or ConsoleKey.Home => new Point(-1, -1), ConsoleKey.U or ConsoleKey.PageUp => new Point(1, -1),
        ConsoleKey.B or ConsoleKey.End => new Point(-1, 1), ConsoleKey.N or ConsoleKey.PageDown => new Point(1, 1), _ => null
    };
    private static int NumericSelection(ConsoleKeyInfo key) => key.Key switch
    {
        ConsoleKey.D1 or ConsoleKey.NumPad1 => 0, ConsoleKey.D2 or ConsoleKey.NumPad2 => 1, ConsoleKey.D3 or ConsoleKey.NumPad3 => 2,
        ConsoleKey.D4 or ConsoleKey.NumPad4 => 3, ConsoleKey.D5 or ConsoleKey.NumPad5 => 4, ConsoleKey.D6 or ConsoleKey.NumPad6 => 5,
        ConsoleKey.D7 or ConsoleKey.NumPad7 => 6, ConsoleKey.D8 or ConsoleKey.NumPad8 => 7, ConsoleKey.D9 or ConsoleKey.NumPad9 => 8, _ => -1
    };
    private static int Hash(Point p, int seed) { unchecked { return (p.X * 73856093) ^ (p.Y * 19349663) ^ (seed * 83492791); } }
}
