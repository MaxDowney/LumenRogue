namespace LumenRogue;

public static class SelfTests
{
    public static int Run()
    {
        try
        {
            var town = WorldBuilder.BuildTown();
            Assert(town.IsTown, "town identity");
            Assert(town.Shops.Count == 3, "three town shops");
            Assert(town[town.StairsDown].Glyph == '>', "town dungeon stairs");
            Assert(town.Lights.Count >= 10, "town light sources");
            Assert(town.Provisions.Values.Select(source => source.Kind).Distinct().Count() == 2, "town has reliable food and water sources");
            Assert(town.Npcs.Count >= 6 && town.Npcs.Select(npc => npc.Position).Distinct().Count() == town.Npcs.Count,
                "Emberfall has distinct resident positions");
            Assert(town.Npcs.Count(npc => npc.QuestId is not null) == 3 && town.Npcs.All(npc => town[npc.Position].Walkable),
                "town residents include three reachable quest givers");
            Assert(town.TravelPoints.Values.Contains(TravelKind.Wilderness), "Emberfall has a walkable west gate to the wilderness");

            var clockGame = new Game(skipCharacterCreation: true);
            var morningLight = clockGame.OutdoorAmbientLight;
            Assert(clockGame.WorldMinutes == 420 && clockGame.WorldTimeLabel == "07:00" && clockGame.OutdoorPeriod == "morning",
                "world clock begins at a legible morning hour");
            Assert(Renderer.VisionFalloff(clockGame, 32) > 0,
                "morning daylight already extends outdoor sight beyond dungeon range");
            var noonGame = new Game(skipCharacterCreation: true);
            for (var turn = 0; turn < 50; turn++)
                noonGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), turn);
            Assert(noonGame.WorldTimeLabel == "12:00" && noonGame.OutdoorAmbientLight > 0.85 &&
                Renderer.VisionFalloff(noonGame, 45) > 0,
                "midday is substantially brighter and exposes a much wider outdoor horizon");
            for (var turn = 0; turn < 110; turn++)
                clockGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), turn);
            Assert(clockGame.WorldTimeLabel == "18:00" && clockGame.OutdoorPeriod == "dusk" &&
                clockGame.LatestMessage.Contains("sun slips"), "turns advance the clock into a signposted sunset");
            for (var turn = 0; turn < 20; turn++)
                clockGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), 110 + turn);
            Assert(clockGame.WorldTimeLabel == "20:00" && clockGame.OutdoorPeriod == "night" &&
                clockGame.OutdoorAmbientLight < morningLight * 0.4 && Renderer.VisionFalloff(clockGame, 30) == 0,
                "night materially reduces deterministic outdoor illumination and sight range");
            for (var turn = 0; turn < 110; turn++)
                clockGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), 130 + turn);
            Assert(clockGame.WorldTimeLabel == "07:00" && Math.Abs(clockGame.OutdoorAmbientLight - morningLight) < 0.001,
                "240 turns complete one stable 24-hour cycle");

            var (wilderness, wildernessMonsters) = WorldBuilder.BuildWilderness();
            var returnRoad = wilderness.TravelPoints.Single(entry => entry.Value == TravelKind.Town).Key;
            Assert(wilderness.Biome == BiomeKind.Wilds && wilderness.IsOutdoor && !wilderness.IsTown,
                "wilderness is a distinct outdoor region");
            Assert(wilderness.Width == 240 && wilderness.Height == 120,
                "the Ashwind March is an expansive persistent overworld");
            Assert(wildernessMonsters.Count >= 48 && wildernessMonsters.Select(monster => monster.Name).Distinct().Count() >= 5,
                "wilderness has a varied hostile ecology");
            Assert(wildernessMonsters.All(monster => !wilderness.Provisions.ContainsKey(monster.Position) &&
                !wilderness.TravelPoints.ContainsKey(monster.Position) && !wilderness.GroundItems.Any(item => item.Position == monster.Position)),
                "wilderness creatures do not obscure exits, supplies, or loot");
            Assert(wilderness.Provisions.Values.Select(source => source.Kind).Distinct().Count() == 2 && wilderness.GroundItems.Count >= 4,
                "wilderness exploration provides both survival resources and discoverable loot");
            Assert(wilderness.SpecialRoomNames.Count >= 6 && IsReachable(wilderness, wilderness.Start, returnRoad),
                "wilderness landmarks share a connected route home");
            Assert(wilderness.Shops.Count == 3 && wilderness.Npcs.Count >= 8 &&
                wilderness.Npcs.All(npc => wilderness[npc.Position].Walkable) &&
                new[] { "Bramblewick", "Stonecross", "Reedwater" }.All(name => wilderness.SpecialRoomNames.Contains(name)),
                "three inhabited villages provide residents and working local shops");
            Assert(CountNamedTiles(wilderness, "Castle Valeward") >= 100 && wilderness.SpecialRoomNames.Contains("Castle Valeward"),
                "Castle Valeward is a substantial traversable overworld structure");
            Assert(wilderness.TravelPoints.Keys.Count(position => wilderness[position].Glyph == '>') == 4,
                "four distinct wilderness entrances lead to independent dungeon sites");
            Assert(CountNamedTiles(wilderness, "bridge") >= 20 && CountNamedTiles(wilderness, "ruin") >= 20,
                "the Ashwind March contains legible bridge and ruin landmarks");

            var travelGame = new Game(skipCharacterCreation: true);
            var townGate = travelGame.CurrentMap.TravelPoints.Single(entry => entry.Value == TravelKind.Wilderness).Key;
            travelGame.Player.Position = townGate + new Point(1, 0);
            var travelTurn = travelGame.TurnCount;
            travelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false), 0);
            Assert(travelGame.IsWilderness && travelGame.CurrentDepth == 0 && travelGame.Player.Position == travelGame.CurrentMap.Start &&
                travelGame.TurnCount == travelTurn, "walking through the west gate enters wilderness without corrupting dungeon depth");
            var wildReturn = travelGame.CurrentMap.TravelPoints.Single(entry => entry.Value == TravelKind.Town).Key;
            travelGame.Player.Position = wildReturn + new Point(-1, 0);
            travelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            Assert(travelGame.CurrentMap.IsTown && !travelGame.IsWilderness,
                "the wilderness return road leads safely back to Emberfall");

            travelGame.Player.Position = townGate + new Point(1, 0);
            travelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false), 0);
            var mineEntrance = travelGame.CurrentMap.TravelPoints.Single(entry => entry.Value == TravelKind.HollowrootMine).Key;
            var mineApproach = mineEntrance + new Point(-1, 0);
            Assert(travelGame.CurrentMap[mineApproach].Walkable, "Hollowroot Mine has a reachable road approach");
            travelGame.Player.Position = mineApproach;
            travelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            Assert(travelGame.IsSiteDungeon && travelGame.CurrentMap.Name == "Hollowroot Mine" && travelGame.CurrentDepth == 1 &&
                travelGame.SiteDungeonCount == 4, "site entrance opens one of four persistent independent dungeons");
            var persistentMine = travelGame.CurrentMap;
            persistentMine.Explored[persistentMine.StairsUp.X, persistentMine.StairsUp.Y] = true;
            travelGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            Assert(travelGame.IsWilderness && travelGame.Player.Position == mineApproach,
                "leaving a site dungeon restores the exact wilderness approach position");
            travelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            Assert(ReferenceEquals(travelGame.CurrentMap, persistentMine) &&
                travelGame.CurrentMap.Explored[travelGame.CurrentMap.StairsUp.X, travelGame.CurrentMap.StairsUp.Y],
                "re-entering a site dungeon preserves its exploration state");
            travelGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);

            var questGame = new Game(skipCharacterCreation: true);
            var mara = questGame.CurrentMap.Npcs.Single(npc => npc.Id == "mara_vey");
            var torchQuest = questGame.QuestForNpc(mara)!;
            var waterQuest = questGame.Quests.Single(quest => quest.Id == "turn_the_tide");
            questGame.RecordQuestProgress(QuestObjectiveKind.OperateMachine, machine: MachineKind.Floodgate);
            Assert(waterQuest.Progress == 0, "unaccepted quests do not progress invisibly");
            questGame.Player.Position = mara.Position + new Point(-1, 0);
            var dialogueTurn = questGame.TurnCount;
            questGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            Assert(questGame.Mode == GameMode.Dialogue && questGame.ActiveNpc == mara && questGame.TurnCount == dialogueTurn,
                "adjacent interaction opens resident dialogue without passing a turn");
            var torchCount = questGame.Inventory.Where(item => item.Kind == ItemKind.Torch).Sum(item => item.Quantity);
            questGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(torchQuest.Status == QuestStatus.Active &&
                questGame.Inventory.Where(item => item.Kind == ItemKind.Torch).Sum(item => item.Quantity) == torchCount + 1,
                "accepting the lamplighter quest supplies its required wall torch");
            questGame.HandleKey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false), 0);
            Assert(questGame.Mode == GameMode.QuestLog && questGame.JournalQuests.Contains(torchQuest) && questGame.TurnCount == dialogueTurn,
                "quest journal opens freely and lists accepted work");
            questGame.HandleKey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false), 0);
            questGame.RecordQuestProgress(QuestObjectiveKind.MountDungeonTorch);
            Assert(torchQuest.Status == QuestStatus.ReadyToTurnIn && questGame.LatestMessage.Contains("return to Mara Vey"),
                "objective completion directs the player back to the named giver");
            var rewardGold = questGame.Player.Gold;
            questGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            questGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(torchQuest.Status == QuestStatus.Completed && questGame.Player.Gold == rewardGold + torchQuest.RewardGold &&
                questGame.Inventory.Any(item => item.Kind == torchQuest.RewardItem),
                "returning to the giver completes the quest and grants every reward");

            var (dungeon, monsters) = WorldBuilder.BuildDungeon();
            Assert(!dungeon.IsTown, "dungeon identity");
            Assert(monsters.Count >= 12, "dungeon monster population");
            Assert(dungeon.Lights.Count >= 5, "dungeon torch population");
            Assert(dungeon.WallTorches.Count >= 4, "dungeon wall torch population");
            Assert(dungeon.WallTorches.Keys.All(position => !dungeon[position].Walkable), "wall torches are mounted on walls");
            Assert(dungeon.WallTorches.Keys.All(position => CardinalNeighbors(position)
                .Any(neighbor => dungeon.Contains(neighbor) && dungeon[neighbor].Walkable)), "wall torches are reachable from adjacent floor");
            var fragileTorch = dungeon.WallTorches.Keys.First();
            Assert(dungeon.DamageTerrain(fragileTorch, 99, WorldBuilder.Rubble) &&
                !dungeon.WallTorches.ContainsKey(fragileTorch) && !dungeon.Lights.Any(light => light.Position == fragileTorch),
                "destroyed wall torch removes its bracket and light");
            Assert(dungeon.SpecialRoomNames.Count >= 4, "special dungeon rooms");
            Assert(dungeon.GroundItems.Count >= 6, "dungeon loot population");
            Assert(monsters.Select(m => m.Name).Distinct().Count() >= 5, "varied monster roster");
            Assert(IsReachable(dungeon, dungeon.StairsUp, dungeon.StairsDown), "connected dungeon stairs");
            var (deepDungeon, deepMonsters) = WorldBuilder.BuildDungeon(7331, 5);
            Assert(deepDungeon.Name.Contains("Depth 5") && deepMonsters.Average(m => m.Attack) > monsters.Average(m => m.Attack), "deeper dungeon scales monsters");
            var biomeLevels = Enumerable.Range(1, 5).Select(depth => WorldBuilder.BuildDungeon(9000 + depth, depth)).ToList();
            Assert(biomeLevels.Select(level => level.Map.Biome).Distinct().Count() == 5, "each depth uses a distinct procedural biome");
            Assert(biomeLevels.Select(level => level.Map[new Point(1, 1)].Name).Distinct().Count() == 5, "biomes generate distinct wall materials");
            Assert(biomeLevels.All(level => IsReachable(level.Map, level.Map.StairsUp, level.Map.StairsDown)), "every biome preserves a connected stair route");
            Assert(biomeLevels.All(level => level.Map.Doors.Count >= 3 && level.Map.Traps.Count >= 8 && level.Map.Machines.Count >= 2), "biomes generate doors traps and machinery");
            Assert(biomeLevels.All(level => level.Map.Provisions.Values.Any(source => source.Kind == ProvisionKind.Food) &&
                level.Map.Provisions.Values.Any(source => source.Kind == ProvisionKind.Water)), "every biome generates food and water sources");
            Assert(biomeLevels.All(level => level.Map.GroundItems.Any(item => item.Item.Kind == ItemKind.Ration) &&
                level.Map.GroundItems.Any(item => item.Item.Kind == ItemKind.WaterFlask)), "every floor contains portable food and water");
            Assert(CountCells(biomeLevels[0].Map, cell => cell.Oil > 0) > 0 &&
                CountCells(biomeLevels[1].Map, cell => cell.Wet > 0) > 100 &&
                CountCells(biomeLevels[2].Map, cell => cell.Electric > 0) > 0 &&
                CountCells(biomeLevels[3].Map, cell => cell.Web > 0 || cell.Poison > 0) > 0 &&
                CountCells(biomeLevels[4].Map, cell => cell.Fire > 0) > 0, "each biome receives its elemental terrain signature");
            Assert(biomeLevels.SelectMany(level => level.Monsters).All(monster => monster.Senses != MonsterSense.None), "generated monsters have configured senses and ecology");
            var fullRoster = Enumerable.Range(1, 5)
                .SelectMany(depth => WorldBuilder.BuildDungeon(7331 + depth * 997, depth).Monsters)
                .Select(monster => monster.Name).Distinct().ToList();
            Assert(fullRoster.Count >= 15, "expanded monsters appear across dungeon depths");
            Assert(deepMonsters.Any(monster => monster.Name is "crown lich" or "furnace demon"), "deep floor apex monsters");
            Assert(Enum.GetValues<SpellKind>().Length == 25, "twenty-five distinct spells configured");
            Assert(Enum.GetValues<ItemKind>().Length == 21, "expanded item and crafting-material roster configured");

            var creator = new Game();
            Assert(creator.Mode == GameMode.CharacterCreation, "character creator opens first");
            creator.HandleKey(new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false), 0);
            creator.HandleKey(new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false), 0);
            Assert(creator.Race == RaceKind.Ashborn && creator.Class == ClassKind.Arcanist, "race and class selection");

            var game = new Game(skipCharacterCreation: true);
            Assert(game.Inventory.Count >= 4, "class starting inventory");
            Assert(game.KnownSpells.Count >= 2 && game.ClassSpellTree.Count == 8, "expanded class spell progression tree");
            Assert(game.SkillTree.Count(node => node.Spell is not null) >= 6, "each class tree carries six spell unlocks");
            Assert(game.SkillTree.Count == 24 && game.SkillTree.Select(node => node.Branch).Distinct().Count() == 4 &&
                game.SkillTree.All(node => node.MaxRank == 3), "class progression uses four six-tier multi-rank branches");
            Assert(game.DungeonDepthCount == 5, "five connected dungeon depths configured");
            Assert(game.Inventory.Any(i => i.Equipped && i.Slot == EquipmentSlot.MainHand), "class starts with equipped weapon");
            Assert(game.TotalAttack > game.Player.Attack, "equipped weapon contributes attack");
            game.UpdateEffects(100);
            Assert(game.TurnCount == 0, "visual clock does not advance turns");

            game.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), 1);
            Assert(game.TurnCount == 1, "waiting advances exactly one turn");

            var systemsGame = new Game(skipCharacterCreation: true);
            systemsGame.Player.Position = systemsGame.CurrentMap.StairsDown;
            systemsGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            Assert(systemsGame.CurrentMap.Biome == BiomeKind.AshenVaults, "stairs enter the biome-generated first depth");
            Assert(systemsGame.Effects.OfType<FlourishEffect>().Any(effect => effect.Kind == FlourishKind.StairArrival),
                "stairs create a cosmetic biome-arrival reveal");
            var deterministicLight = systemsGame.GameplayLightAt(systemsGame.Player.Position);
            Assert(Math.Abs(deterministicLight - systemsGame.GameplayLightAt(systemsGame.Player.Position)) < 0.00001, "gameplay illumination is deterministic between turns");
            var lampTurn = systemsGame.TurnCount;
            systemsGame.HandleKey(new ConsoleKeyInfo('o', ConsoleKey.O, false, false, false), 0);
            Assert(!systemsGame.LanternLit && systemsGame.TurnCount == lampTurn + 1 && systemsGame.GameplayLightAt(systemsGame.Player.Position) < deterministicLight, "shuttering lantern changes exposure and costs a turn");
            Assert(Game.TileCastsLightShadow(WorldBuilder.Boulder) && WorldBuilder.Boulder.Transparent &&
                !Game.TileCastsLightShadow(WorldBuilder.Grass),
                "solid obstacles cast lighting shadows without becoming line-of-sight blockers");
            var shadowGame = new Game(skipCharacterCreation: true, worldSeed: 90421);
            if (shadowGame.LanternLit) shadowGame.HandleKey(new ConsoleKeyInfo('o', ConsoleKey.O, false, false, false), 0);
            var shadowLight = shadowGame.CurrentMap.Lights.First(light =>
                CardinalNeighbors(light.Position).Any(first => shadowGame.CurrentMap.Contains(first) && shadowGame.CurrentMap[first].Walkable));
            var shadowDirection = new[] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) }
                .First(direction =>
                {
                    var first = shadowLight.Position + direction;
                    var second = first + direction;
                    return shadowGame.CurrentMap.Contains(second) && shadowGame.CurrentMap[first].Walkable && shadowGame.CurrentMap[second].Walkable &&
                        shadowGame.CurrentMap[first].Transparent && shadowGame.CurrentMap[second].Transparent && second != shadowGame.Player.Position;
                });
            var shadowBlockerPosition = shadowLight.Position + shadowDirection;
            var shadowTarget = shadowBlockerPosition + shadowDirection;
            var unblockedLight = shadowGame.GameplayLightAt(shadowTarget);
            shadowGame.Monsters.Add(new Actor { Position = shadowBlockerPosition, Glyph = 's', Color = Rgb.White, Name = "shadow test creature", MaxHp = 1, Hp = 1, Attack = 1 });
            Assert(shadowGame.HasClearSight(shadowLight.Position, shadowTarget) &&
                !shadowGame.HasClearLightPath(shadowLight.Position, shadowTarget) &&
                shadowGame.GameplayLightAt(shadowTarget) < unblockedLight,
                "a living creature casts a gameplay-light shadow while ordinary sight passes through its cell");
            var fireCell = systemsGame.CurrentMap.Elements[systemsGame.Player.Position.X, systemsGame.Player.Position.Y];
            fireCell.Fire = 3;
            var hpBeforeFire = systemsGame.Player.Hp;
            systemsGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), 0);
            Assert(systemsGame.Player.Hp < hpBeforeFire && fireCell.Smoke > 0, "persistent fire damages actors and creates smoke");

            var secret = systemsGame.CurrentMap.Doors.Values.First();
            secret.State = DoorState.Secret;
            systemsGame.Player.Position = secret.Position;
            systemsGame.HandleKey(new ConsoleKeyInfo('v', ConsoleKey.V, false, false, false), 0);
            Assert(secret.State == DoorState.Closed, "search reveals generated secret doors");
            var machine = systemsGame.CurrentMap.Machines.Values.First();
            systemsGame.Player.Position = machine.Position;
            var machineState = machine.Active;
            systemsGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            Assert(machine.Active != machineState, "biome machinery can be operated in place");

            var supplyGame = new Game(skipCharacterCreation: true);
            var foodSource = supplyGame.CurrentMap.Provisions.Values.First(source => source.Kind == ProvisionKind.Food);
            supplyGame.Player.Position = foodSource.Position;
            var rationCount = supplyGame.Inventory.Where(item => item.Kind == ItemKind.Ration).Sum(item => item.Quantity);
            var foodCharges = foodSource.Charges;
            supplyGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            Assert(foodSource.Charges == foodCharges - 1 &&
                supplyGame.Inventory.Where(item => item.Kind == ItemKind.Ration).Sum(item => item.Quantity) == rationCount + 1,
                "food source yields a portable ration");
            var waterSource = supplyGame.CurrentMap.Provisions.Values.First(source => source.Kind == ProvisionKind.Water);
            supplyGame.Player.Position = waterSource.Position;
            var flaskCount = supplyGame.Inventory.Where(item => item.Kind == ItemKind.WaterFlask).Sum(item => item.Quantity);
            supplyGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            Assert(supplyGame.Inventory.Where(item => item.Kind == ItemKind.WaterFlask).Sum(item => item.Quantity) == flaskCount + 1,
                "water source fills a portable flask");

            var footprintGame = new Game(skipCharacterCreation: true);
            var footprintOrigin = footprintGame.Player.Position;
            var footprintTarget = CardinalNeighbors(footprintOrigin).First(point => footprintGame.CurrentMap.Contains(point) && footprintGame.CurrentMap[point].Walkable);
            footprintGame.CurrentMap.Elements[footprintOrigin.X, footprintOrigin.Y].Wet = 4;
            footprintGame.HandleKey(KeyForDirection(new Point(footprintTarget.X - footprintOrigin.X, footprintTarget.Y - footprintOrigin.Y)), 0);
            Assert(footprintGame.CurrentMap.SurfaceMarks.TryGetValue(footprintOrigin, out var wetMark) && wetMark.Kind == SurfaceMarkKind.WetFootprint,
                "walking off wet ground leaves a bounded cosmetic footprint");

            var torchGame = new Game(skipCharacterCreation: true);
            torchGame.Player.Position = torchGame.CurrentMap.StairsDown;
            torchGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            var mountedTorch = torchGame.CurrentMap.WallTorches.Values.First();
            var torchNeighbor = CardinalNeighbors(mountedTorch.Position).First(position =>
                torchGame.CurrentMap.Contains(position) && torchGame.CurrentMap[position].Walkable &&
                !torchGame.Monsters.Any(monster => monster.IsAlive && monster.Position == position));
            torchGame.Player.Position = torchNeighbor;
            var torchesBeforeTaking = torchGame.Inventory.Where(item => item.Kind == ItemKind.Torch).Sum(item => item.Quantity);
            torchGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            Assert(!torchGame.CurrentMap.WallTorches.ContainsKey(mountedTorch.Position) &&
                torchGame.Inventory.Where(item => item.Kind == ItemKind.Torch).Sum(item => item.Quantity) == torchesBeforeTaking + 1,
                "adjacent wall torch can be taken into inventory");
            var torchIndex = torchGame.Inventory.FindIndex(item => item.Kind == ItemKind.Torch);
            torchGame.HandleKey(new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false), 0);
            for (var i = 0; i < torchIndex; i++)
                torchGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false), 0);
            torchGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(torchGame.AwaitingDirection, "using a carried wall torch enters mounting mode");
            var torchDirection = new Point(mountedTorch.Position.X - torchNeighbor.X, mountedTorch.Position.Y - torchNeighbor.Y);
            torchGame.HandleKey(KeyForDirection(torchDirection), 0);
            Assert(torchGame.CurrentMap.WallTorches.ContainsKey(mountedTorch.Position) &&
                !torchGame.CurrentMap[mountedTorch.Position].Walkable, "carried wall torch remounts on an adjacent wall");

            var door = systemsGame.CurrentMap.Doors.Values.First();
            door.State = DoorState.Closed;
            systemsGame.CurrentMap.Set(door.Position, WorldBuilder.ClosedDoor);
            var doorNeighbor = CardinalNeighbors(door.Position).First(point => systemsGame.CurrentMap.Contains(point) && systemsGame.CurrentMap[point].Walkable && point != door.Position);
            systemsGame.Player.Position = doorNeighbor; systemsGame.Player.WebbedTurns = 0;
            var doorDelta = new Point(door.Position.X - doorNeighbor.X, door.Position.Y - doorNeighbor.Y);
            systemsGame.HandleKey(KeyForDirection(doorDelta), 0);
            Assert(door.State == DoorState.Open, "walking into a closed door opens it as a turn-based action");

            var trap = systemsGame.CurrentMap.Traps.Values.First();
            trap.Armed = true; trap.Revealed = false;
            var trapNeighbor = CardinalNeighbors(trap.Position).First(point => systemsGame.CurrentMap.Contains(point) && systemsGame.CurrentMap[point].Walkable);
            systemsGame.Player.Position = trapNeighbor; systemsGame.Player.WebbedTurns = 0;
            var trapDelta = new Point(trap.Position.X - trapNeighbor.X, trap.Position.Y - trapNeighbor.Y);
            systemsGame.HandleKey(KeyForDirection(trapDelta), 0);
            Assert(trap.Revealed, "stepping onto a generated trap reveals and triggers it");

            var controlGame = new Game(skipCharacterCreation: true);
            Assert(controlGame.TryApplyPlayerStun("A test shock") && !controlGame.TryApplyPlayerStun("A second shock"),
                "stacked control sources collapse into one pending skipped action");
            var controlTurn = controlGame.TurnCount;
            controlGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), 0);
            Assert(controlGame.TurnCount == controlTurn + 1 && controlGame.Player.FrozenTurns == 0 && controlGame.ControlGraceTurns == 2,
                "hard control consumes at most one action and grants a two-turn recovery window");
            Assert(!controlGame.TryApplyPlayerStun("A chained shock"), "control immunity rejects immediate reapplication");

            var webGame = new Game(skipCharacterCreation: true);
            webGame.Player.Position = webGame.CurrentMap.StairsDown;
            webGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            foreach (var enemy in webGame.Monsters) enemy.Hp = 0;
            var webOrigin = webGame.Player.Position;
            var webTarget = CardinalNeighbors(webOrigin).First(point => webGame.CurrentMap.Contains(point) && webGame.CurrentMap[point].Walkable &&
                !webGame.CurrentMap.Doors.ContainsKey(point) && !webGame.CurrentMap.Traps.ContainsKey(point));
            var webDirection = new Point(webTarget.X - webOrigin.X, webTarget.Y - webOrigin.Y);
            webGame.CurrentMap.Elements[webOrigin.X, webOrigin.Y].Web = 18;
            webGame.Player.WebbedTurns = 1;
            webGame.HandleKey(KeyForDirection(webDirection), 0);
            Assert(webGame.Player.Position == webOrigin && webGame.Player.WebbedTurns == 0 && webGame.ControlGraceTurns == 2,
                "escaping web spends one action and starts control recovery");
            webGame.HandleKey(KeyForDirection(webDirection), 0);
            Assert(webGame.Player.Position == webTarget, "web cannot immediately reapply and prevent the escape move");

            var electricGame = new Game(skipCharacterCreation: true);
            electricGame.Player.Position = electricGame.CurrentMap.StairsDown;
            electricGame.HandleKey(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), 0);
            foreach (var enemy in electricGame.Monsters) enemy.Hp = 0;
            var charged = electricGame.CurrentMap.Elements[electricGame.Player.Position.X, electricGame.Player.Position.Y];
            charged.Fire = charged.Frost = charged.Poison = charged.Smoke = charged.Web = charged.Oil = 0;
            charged.Wet = 10; charged.Electric = 3;
            var hpBeforeShock = electricGame.Player.Hp;
            electricGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), 0);
            var hpAfterShock = electricGame.Player.Hp;
            Assert(hpBeforeShock - hpAfterShock == 6 && electricGame.ElectricRecoveryTurns == 2 && electricGame.Player.FrozenTurns == 0,
                "electrified water pulses damage without stunning the player");
            electricGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), 0);
            Assert(electricGame.Player.Hp == hpAfterShock, "electrical recovery prevents consecutive-turn hazard damage");

            if (!systemsGame.LanternLit) systemsGame.HandleKey(new ConsoleKeyInfo('o', ConsoleKey.O, false, false, false), 0);
            var sentry = systemsGame.Monsters.First(monster => monster.IsAlive && monster.Senses.HasFlag(MonsterSense.Sight));
            foreach (var other in systemsGame.Monsters.Where(monster => monster != sentry)) other.Hp = 0;
            var sentryPosition = CardinalNeighbors(systemsGame.Player.Position).First(point => systemsGame.CurrentMap.Contains(point) && systemsGame.CurrentMap[point].Walkable);
            sentry.Position = sentryPosition; sentry.AlertTurns = 0; sentry.LastKnownPlayer = null;
            systemsGame.Effects.Clear();
            systemsGame.HandleKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false), 0);
            Assert(sentry.AlertTurns > 0 && sentry.LastKnownPlayer == systemsGame.Player.Position, "illuminated player is detected and remembered by sighted monsters");

            var spellGame = new Game();
            spellGame.HandleKey(new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false), 0);
            spellGame.HandleKey(new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false), 0);
            spellGame.HandleKey(new ConsoleKeyInfo('z', ConsoleKey.Z, false, false, false), 2);
            spellGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false), 2);
            spellGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 2);
            Assert(spellGame.Mode == GameMode.Targeting && spellGame.CursorPosition is not null, "spell opens cursor targeting mode");
            spellGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 2.01);
            spellGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 2.02);
            Assert(spellGame.Effects.OfType<ProjectileEffect>().Any(), "targeted fireball projectile created");
            Assert(spellGame.TurnCount == 1 && spellGame.Mana == spellGame.MaxMana - spellGame.SpellCost(SpellKind.Fireball), "targeted spell consumes one action and correct mana");
            spellGame.UpdateEffects(20);
            Assert(spellGame.Effects.OfType<ExplosionEffect>().Any(), "projectile becomes explosion");
            spellGame.UpdateEffects(30);
            Assert(!spellGame.Effects.Any(), "effects expire cleanly");
            Assert(spellGame.TurnCount == 1, "effect animation does not add turns");

            var terrainMap = new Map(3, 3, "test", false, WorldBuilder.Floor);
            var wall = new Point(1, 1);
            terrainMap.Set(wall, WorldBuilder.CrackedWall);
            Assert(terrainMap.DamageTerrain(wall, 99, WorldBuilder.Rubble) && terrainMap[wall].Walkable, "destructible terrain becomes rubble");

            var beforeRest = game.TurnCount;
            game.HandleKey(new ConsoleKeyInfo('r', ConsoleKey.R, false, false, false), 31);
            Assert(game.TurnCount > beforeRest, "rest advances time");

            Assert(Renderer.VisionFalloff(5) == 1, "vision is clear nearby");
            Assert(Renderer.VisionFalloff(18) is > 0 and < 1, "vision fades through outer band");
            Assert(Renderer.VisionFalloff(30) == 0, "vision reaches darkness smoothly");
            var minimumPanel = Renderer.MenuPanelDimensions(60, 20);
            Assert(minimumPanel == (56, 16), "menus keep a stable frame at the minimum supported terminal size");
            var wrappedDescription = Renderer.WrapPanelText(
                " A deliberately long equipment description wraps cleanly without dropping its final comparison values.", 54);
            Assert(wrappedDescription.Length >= 2 && wrappedDescription.All(line => line.Length <= 54) &&
                wrappedDescription.Any(line => line.Contains("comparison")) && wrappedDescription.Any(line => line.Contains("values")),
                "menu text wraps instead of being truncated");

            var mapGame = new Game(skipCharacterCreation: true);
            mapGame.CurrentMap.Explored[mapGame.Player.Position.X, mapGame.Player.Position.Y] = true;
            var mapTurn = mapGame.TurnCount;
            mapGame.HandleKey(new ConsoleKeyInfo('m', ConsoleKey.M, false, false, false), 0);
            Assert(mapGame.Mode == GameMode.Map && mapGame.TurnCount == mapTurn, "explored map opens without advancing time");
            var mapLines = Renderer.BuildExploredMapLines(mapGame, 50, 12);
            Assert(mapLines.Length == 12 && mapLines.Any(line => line.Contains('@')), "scaled explored map preserves player landmark");
            Assert(mapLines.Sum(line => line.Count(glyph => glyph != ' ')) == 1, "unexplored terrain remains hidden on area map");
            mapGame.HandleKey(new ConsoleKeyInfo('m', ConsoleKey.M, false, false, false), 0);
            Assert(mapGame.Mode == GameMode.Playing && mapGame.TurnCount == mapTurn, "area map closes without advancing time");

            game.HandleKey(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false), 31);
            Assert(game.Mode == GameMode.Inspect && game.CursorDescription.Contains("you"), "inspect cursor describes player");
            game.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false), 31);

            var quitGame = new Game(skipCharacterCreation: true);
            quitGame.HandleKey(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false), 0);
            Assert(quitGame.Mode == GameMode.QuitConfirm && !quitGame.QuitRequested && quitGame.MenuSelection == 0, "quit opens safe-default confirmation modal");
            quitGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(quitGame.Mode == GameMode.Playing && !quitGame.QuitRequested, "default quit response returns to game");
            quitGame.HandleKey(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false), 0);
            quitGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            quitGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(quitGame.QuitRequested, "explicit yes confirms quit");

            var levelGame = new Game(skipCharacterCreation: true);
            var baseStonebreakCost = levelGame.SpellCost(SpellKind.Stonebreak);
            levelGame.GrantExperience(45);
            Assert(levelGame.Level == 2 && levelGame.Mode == GameMode.SkillTree && levelGame.SkillPoints == 2 && levelGame.SkillTree.Count == 24, "level up opens expanded persistent class skill tree");
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(levelGame.SkillPoints == 0 && levelGame.SkillTree[12].Rank == 2 && levelGame.SpellSkillBonus(SpellKind.Stonebreak) == 4,
                "multiple points rank up a spell node and increase its power");
            levelGame.GrantExperience(85);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(levelGame.SpellInvestment(SpellKind.Stonebreak) == 3 && levelGame.SpellCost(SpellKind.Stonebreak) == baseStonebreakCost - 1,
                "third spell rank grants its mana discount");
            levelGame.GrantExperience(125);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false), 0);
            levelGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(levelGame.KnownSpells.Contains(SpellKind.SeismicWave) && levelGame.SkillTree[14].Purchased,
                "linked prerequisite tiers unlock a new class spell");

            var goblin = new Actor
            {
                Position = new Point(1, 1), Glyph = 'g', Color = new Rgb(80, 180, 70), Name = "gutter goblin",
                MaxHp = 8, Hp = 8, Attack = 3, XpReward = 12
            };
            var monsterGear = EquipmentFactory.MonsterDrop(new Random(42), goblin, 2);
            Assert(monsterGear.IsEquippable && (monsterGear.Name.Contains("goblin") || monsterGear.Name.Contains("buckler")), "monster-themed equipment drop");
            Assert(monsterGear.Copy().Slot == monsterGear.Slot, "equipment copy preserves slot and bonuses");
            var demon = new Actor
            {
                Position = new Point(1, 1), Glyph = 'D', Color = new Rgb(245, 60, 25), Name = "furnace demon",
                MaxHp = 48, Hp = 48, Attack = 13, XpReward = 95
            };
            var demonGear = EquipmentFactory.MonsterDrop(new Random(4), demon, 8);
            Assert(demonGear.Name.Contains("furnace-heart") && demonGear.Rarity == ItemRarity.Epic, "apex monster themed epic drop");
            Assert(WorldBuilder.MakeItem(ItemKind.Ambrosia).Description.Contains("food") &&
                WorldBuilder.MakeItem(ItemKind.SmokeBomb).Description.Contains("monsters"), "new consumables have gameplay definitions");

            var equipGame = new Game(skipCharacterCreation: true);
            var rareBlade = EquipmentFactory.Create("test starblade", EquipmentSlot.MainHand, '†', attack: 9, spell: 2, rarity: ItemRarity.Rare);
            equipGame.AddEquipment(rareBlade, false);
            var rareIndex = equipGame.Inventory.IndexOf(rareBlade);
            equipGame.HandleKey(new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false), 0);
            for (var i = 0; i < rareIndex; i++) equipGame.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false), 0);
            equipGame.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(rareBlade.Equipped && equipGame.TotalAttack >= equipGame.Player.Attack + 9, "cursor inventory equips and replaces gear");

            var deterministicA = WorldBuilder.BuildWilderness(73451).Map;
            var deterministicB = WorldBuilder.BuildWilderness(73451).Map;
            var deterministicC = WorldBuilder.BuildWilderness(73452).Map;
            Assert(MapFingerprint(deterministicA) == MapFingerprint(deterministicB),
                "the same seed reproduces the same wilderness map");
            Assert(MapFingerprint(deterministicA) != MapFingerprint(deterministicC),
                "different seeds produce materially different wilderness maps");

            foreach (var seed in new[] { 1, 7, 42, 94117, 128003, 761239, 19088743, int.MaxValue - 19 })
            {
                var seededTown = WorldBuilder.BuildTown(seed);
                Assert(seededTown.CriticalPoints.All(point => WorldBuilder.IsReachable(seededTown, seededTown.Start, point)),
                    $"seed {seed} town critical routes remain reachable");
                var seededWilds = WorldBuilder.BuildWilderness(seed).Map;
                Assert(seededWilds.CriticalPoints.All(point => WorldBuilder.IsReachable(seededWilds, seededWilds.Start, point)),
                    $"seed {seed} wilderness settlements, castle, supplies, and dungeons remain reachable");
                for (var depth = 1; depth <= 5; depth++)
                {
                    var seededDungeon = WorldBuilder.BuildDungeon(seed + depth * 97, depth).Map;
                    Assert(seededDungeon.CriticalPoints.All(point => WorldBuilder.IsReachable(seededDungeon, seededDungeon.Start, point)),
                        $"seed {seed} depth {depth} cannot isolate a critical objective");
                }
            }

            var sites = new[]
            {
                WorldBuilder.BuildSiteDungeon("Hollowroot Mine", 101, 1),
                WorldBuilder.BuildSiteDungeon("Sunken Barrow", 211, 2),
                WorldBuilder.BuildSiteDungeon("Moonwell Cavern", 307, 3),
                WorldBuilder.BuildSiteDungeon("Castle Valeward Undercroft", 401, 4)
            };
            Assert(sites.All(site => !string.IsNullOrWhiteSpace(site.Map.DungeonMechanic) &&
                site.Monsters.Count(monster => monster.IsBoss) == 1 &&
                site.Map.CriticalPoints.All(point => WorldBuilder.IsReachable(site.Map, site.Map.Start, point))),
                "every site has a distinct boss, authored procedural rule, and reachable heart");

            var systemsPolish = new Game(skipCharacterCreation: true, worldSeed: 55021);
            Assert(systemsPolish.DiscoveredLandmarks.Contains("Emberfall Crossroads"),
                "the starting landmark is discovered intentionally");
            systemsPolish.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.F2, false, false, false), 0);
            Assert(systemsPolish.Mode == GameMode.FastTravel && systemsPolish.FastTravelOptions.Count > 0,
                "discovered outdoor landmarks populate the fast-travel map");
            systemsPolish.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false), 0);
            systemsPolish.AddItem(ItemKind.BeastHide, 1, false);
            systemsPolish.AddItem(ItemKind.VenomGland, 1, false);
            var medicines = systemsPolish.Inventory.Count(item => item.Kind == ItemKind.GreaterHealingPotion);
            systemsPolish.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.F3, false, false, false), 0);
            systemsPolish.HandleKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), 0);
            Assert(systemsPolish.Inventory.Count(item => item.Kind == ItemKind.GreaterHealingPotion) == medicines + 1,
                "settlement crafting consumes meaningful creature materials");
            systemsPolish.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false), 0);
            systemsPolish.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.F6, false, false, false), 0);
            Assert(systemsPolish.PreparedReaction == PlayerReactionKind.Block,
                "the player can explicitly prepare a response to telegraphed attacks");

            var savePath = Path.Combine(Path.GetTempPath(), $"lumenrogue-selftest-{Guid.NewGuid():N}.json");
            try
            {
                systemsPolish.Player.Gold = 417;
                systemsPolish.SaveGame(savePath);
                Assert(File.Exists(savePath), $"save file created ({systemsPolish.LatestMessage})");
                var loaded = Game.LoadGame(savePath);
                Assert(loaded.WorldSeed == systemsPolish.WorldSeed && loaded.Player.Gold == 417 &&
                    loaded.TurnCount == systemsPolish.TurnCount && loaded.DiscoveredLandmarks.SetEquals(systemsPolish.DiscoveredLandmarks) &&
                    loaded.Inventory.Select(item => (item.Kind, item.Quantity, item.Name)).SequenceEqual(systemsPolish.Inventory.Select(item => (item.Kind, item.Quantity, item.Name))),
                    "save/load preserves seed, time, character, inventory, and discoveries");
            }
            finally { if (File.Exists(savePath)) File.Delete(savePath); }

            Console.WriteLine("LumenRogue self-test: expanded gameplay checks passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"LumenRogue self-test failed: {exception.Message}");
            return 1;
        }
    }

    private static bool IsReachable(Map map, Point start, Point goal)
    {
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

    private static int CountCells(Map map, Func<ElementCell, bool> predicate)
    {
        var count = 0;
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            if (predicate(map.Elements[x, y])) count++;
        return count;
    }

    private static int CountNamedTiles(Map map, string fragment)
    {
        var count = 0;
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            if (map[new Point(x, y)].Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)) count++;
        return count;
    }

    private static ulong MapFingerprint(Map map)
    {
        const ulong offset = 1469598103934665603;
        const ulong prime = 1099511628211;
        var hash = offset;
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
        {
            hash ^= map[new Point(x, y)].Glyph;
            hash *= prime;
        }
        return hash;
    }

    private static IEnumerable<Point> CardinalNeighbors(Point point)
    {
        yield return point + new Point(1, 0);
        yield return point + new Point(-1, 0);
        yield return point + new Point(0, 1);
        yield return point + new Point(0, -1);
    }

    private static ConsoleKeyInfo KeyForDirection(Point direction) => direction switch
    {
        { X: 1, Y: 0 } => new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false),
        { X: -1, Y: 0 } => new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false),
        { X: 0, Y: 1 } => new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false),
        _ => new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false)
    };

    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
}
