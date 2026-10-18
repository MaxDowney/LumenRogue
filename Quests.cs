namespace LumenRogue;

public sealed partial class Game
{
    private void InitializeQuests()
    {
        Quests.AddRange([
            new Quest
            {
                Id = "ash_road_bounty", Title = "Ash-road Bounty", GiverId = "orra_flint",
                Description = "Goblinoid raiders are testing the lower road and learning which town lamps go dark first.",
                ObjectiveText = "Defeat 3 goblinoid creatures beyond Emberfall.", ObjectiveKind = QuestObjectiveKind.DefeatFaction,
                TargetFaction = MonsterFaction.Goblinoid, TargetCount = 3, RewardGold = 45, RewardXp = 35,
                RewardItem = ItemKind.GuardElixir,
                AcceptDialogue = "Bring the road three fewer raiders. I'll keep your payment beside the forge.",
                ProgressDialogue = "They're counting our lamps. Make sure three of them stop counting.",
                CompletionDialogue = "Good. A quiet road is honest work, even when it takes a blade."
            },
            new Quest
            {
                Id = "borrowed_flame", Title = "A Borrowed Flame", GiverId = "mara_vey",
                Description = "Mara wants proof that Emberfall's light can take root below, not merely be carried through the dark.",
                ObjectiveText = "Mount the supplied wall torch on a bare dungeon wall.", ObjectiveKind = QuestObjectiveKind.MountDungeonTorch,
                RewardGold = 30, RewardXp = 25, RewardItem = ItemKind.FocusElixir,
                AcceptDialogue = "Take this town flame below. Give it a wall, not just a hand to tremble in.",
                ProgressDialogue = "The bracket needs dungeon stone. Any bare wall beside safe footing will do.",
                CompletionDialogue = "Then there is one more place in the dark that belongs to the living."
            },
            new Quest
            {
                Id = "turn_the_tide", Title = "Turn the Tide", GiverId = "neris_vale",
                Description = "A drowned mechanism beneath Emberfall is fouling the old well. Neris needs its floodgate moved once to break the stagnant pressure.",
                ObjectiveText = "Operate a floodgate in the Drowned Reliquary.", ObjectiveKind = QuestObjectiveKind.OperateMachine,
                TargetMachine = MachineKind.Floodgate, RewardGold = 35, RewardXp = 30,
                RewardItem = ItemKind.WaterFlask, RewardItemQuantity = 2,
                AcceptDialogue = "Find the old floodgate below and turn it. Open or shut matters less than making the water move.",
                ProgressDialogue = "The mechanism lies in the drowned depth. Listen for water pushing against iron.",
                CompletionDialogue = "The well cleared before dawn. Here—water that no longer tastes of rust."
            },
            new Quest
            {
                Id = "bramblewick_scouts", Title = "Bells Beyond the Hedge", GiverId = "elin_bramble",
                Description = "Goblin scouts are testing Bramblewick's hedges and counting its livestock paths.",
                ObjectiveText = "Defeat 3 goblinoids in the Ashwind March.", ObjectiveKind = QuestObjectiveKind.DefeatFaction,
                TargetFaction = MonsterFaction.Goblinoid, TargetCount = 3, RewardGold = 32, RewardXp = 28,
                RewardItem = ItemKind.Ration, RewardItemQuantity = 2,
                AcceptDialogue = "Thin their scouts and the village can breathe long enough to find the source.",
                CompletionDialogue = "The bells are ours again. Now we know where the raiders disappear."
            },
            new Quest
            {
                Id = "hollowroot_heart", Title = "The Root Below", GiverId = "elin_bramble", PrerequisiteQuestId = "bramblewick_scouts",
                Description = "The scouts retreat into Hollowroot Mine, where something has taught the roots to hunt.",
                ObjectiveText = "Defeat the boss of Hollowroot Mine.", ObjectiveKind = QuestObjectiveKind.ClearDungeon,
                TargetLocation = "Hollowroot Mine", RewardGold = 75, RewardXp = 70, RewardItem = ItemKind.BossRelic,
                ConsequenceFlag = "bramblewick_safe",
                AcceptDialogue = "Follow them below and cut out whatever has nested beneath our fields.",
                CompletionDialogue = "The hedges have gone still. Bramblewick will remember who gave it back the road."
            },
            new Quest
            {
                Id = "stones_remember", Title = "What the Stones Remember", GiverId = "dara_stone",
                Description = "Stonecross masons found an old road-mark matching the standing stones south of the river.",
                ObjectiveText = "Discover the Whispering Stones.", ObjectiveKind = QuestObjectiveKind.DiscoverLandmark,
                TargetLocation = "Whispering Stones", RewardGold = 26, RewardXp = 24, RewardItem = ItemKind.RelicShard,
                AcceptDialogue = "Find the old ring and read the road before weather takes the last marks.",
                CompletionDialogue = "A burial seal. That explains the subsidence—and gives us a path into the barrow."
            },
            new Quest
            {
                Id = "barrow_oath", Title = "The Road's Old Oath", GiverId = "dara_stone", PrerequisiteQuestId = "stones_remember",
                Description = "The Sunken Barrow is undermining Stonecross and waking beneath the repaired road.",
                ObjectiveText = "Defeat the boss of the Sunken Barrow.", ObjectiveKind = QuestObjectiveKind.ClearDungeon,
                TargetLocation = "Sunken Barrow", RewardGold = 80, RewardXp = 78, RewardItem = ItemKind.GuardElixir,
                ConsequenceFlag = "stonecross_restored",
                AcceptDialogue = "Put the old castellan back to rest. We'll make the road hold above him.",
                CompletionDialogue = "The foundations settled before you reached the gate. Stonecross stands square again."
            },
            new Quest
            {
                Id = "reedwater_pressure", Title = "Pressure in the Sere", GiverId = "nella_reed",
                Description = "Reedwater's medicinal pools are backing up with metallic water from below.",
                ObjectiveText = "Operate a floodgate in a drowned dungeon.", ObjectiveKind = QuestObjectiveKind.OperateMachine,
                TargetMachine = MachineKind.Floodgate, RewardGold = 30, RewardXp = 30, RewardItem = ItemKind.WaterFlask, RewardItemQuantity = 2,
                AcceptDialogue = "Move one of the old gates. The river needs somewhere else to put its anger.",
                CompletionDialogue = "The reeds are green at their roots again. The source is deeper, in Moonwell."
            },
            new Quest
            {
                Id = "moonwell_source", Title = "The False Moon", GiverId = "nella_reed", PrerequisiteQuestId = "reedwater_pressure",
                Description = "Charged crystal in Moonwell Cavern is poisoning the river's night reflection.",
                ObjectiveText = "Defeat the boss of Moonwell Cavern.", ObjectiveKind = QuestObjectiveKind.ClearDungeon,
                TargetLocation = "Moonwell Cavern", RewardGold = 85, RewardXp = 82, RewardItem = ItemKind.FocusElixir,
                ConsequenceFlag = "reedwater_pure",
                AcceptDialogue = "Break the thing wearing the moon's reflection. Bring the river its own face back.",
                CompletionDialogue = "The Sere ran silver, then clear. Reedwater's remedies will be stronger for it."
            },
            new Quest
            {
                Id = "valeward_commission", Title = "The Western Compact", GiverId = "captain_roen",
                Description = "With the three March settlements secured, Valeward can finally open its sealed undercroft.",
                ObjectiveText = "Earn the trust of Bramblewick, Stonecross, and Reedwater.", ObjectiveKind = QuestObjectiveKind.DiscoverLandmark,
                TargetLocation = "Castle Valeward", RequiredFlags = ["bramblewick_safe", "stonecross_restored", "reedwater_pure"],
                RewardGold = 60, RewardXp = 55, ConsequenceFlag = "valeward_commissioned",
                AcceptDialogue = "The villages vouch for you. Report to the keep and the undercroft commission is yours.",
                CompletionDialogue = "The compact is witnessed. Valeward's sealed stair is now under your authority."
            },
            new Quest
            {
                Id = "undercroft_regent", Title = "The Oathless Regent", GiverId = "lady_sera", PrerequisiteQuestId = "valeward_commission",
                Description = "An oathbound ruler beneath Valeward is feeding on every promise the castle fails to keep.",
                ObjectiveText = "Defeat the boss of Castle Valeward Undercroft.", ObjectiveKind = QuestObjectiveKind.ClearDungeon,
                TargetLocation = "Castle Valeward Undercroft", RewardGold = 140, RewardXp = 130, RewardItem = ItemKind.BossRelic,
                ConsequenceFlag = "valeward_secured",
                AcceptDialogue = "Go below as the March's witness. End the regent's claim without accepting its crown.",
                CompletionDialogue = "The keep is quiet in a way I have never heard. Valeward is finally ours, not his."
            }
        ]);
    }

    public Quest? QuestForNpc(TownNpc npc)
    {
        var offered = Quests.Where(quest => quest.GiverId == npc.Id).ToList();
        return offered.FirstOrDefault(quest => quest.Accepted && !quest.TurnedIn) ??
            offered.FirstOrDefault(quest => !quest.Accepted && QuestUnlocked(quest)) ??
            offered.LastOrDefault(quest => quest.TurnedIn);
    }

    public TownNpc? QuestGiver(Quest quest) => AllMaps().SelectMany(map => map.Npcs).FirstOrDefault(npc => npc.Id == quest.GiverId);

    private bool QuestUnlocked(Quest quest) =>
        (quest.PrerequisiteQuestId is null || Quests.Any(candidate => candidate.Id == quest.PrerequisiteQuestId && candidate.TurnedIn)) &&
        quest.RequiredFlags.All(WorldFlags.Contains);

    private void BeginDialogue(TownNpc npc)
    {
        ActiveNpc = npc;
        Mode = GameMode.Dialogue;
        MenuSelection = 0;
    }

    private void HandleDialogueKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.E or ConsoleKey.Q)
        {
            CloseDialogue();
            return;
        }
        if (key.Key is not ConsoleKey.Enter and not ConsoleKey.Spacebar || ActiveNpc is null) return;

        var npc = ActiveNpc;
        var quest = QuestForNpc(npc);
        if (quest is null)
        {
            CloseDialogue();
            AddMessage($"{npc.Name} returns to the rhythm of the town.");
            return;
        }

        switch (quest.Status)
        {
            case QuestStatus.Available:
                quest.Accepted = true;
                if (quest.Id == "borrowed_flame") AddItem(ItemKind.Torch, 1, false);
                ReconcileQuestProgress(quest);
                CloseDialogue();
                AddMessage($"QUEST ACCEPTED: {quest.Title} — {quest.ObjectiveText}");
                break;
            case QuestStatus.Active:
                CloseDialogue();
                AddMessage($"{quest.Title}: {quest.ProgressText}. {quest.ProgressDialogue}");
                break;
            case QuestStatus.ReadyToTurnIn:
                TurnInQuest(quest, npc);
                break;
            default:
                CloseDialogue();
                AddMessage($"{npc.Name}: {quest.CompletionDialogue}");
                break;
        }
    }

    private void CloseDialogue()
    {
        ActiveNpc = null;
        Mode = GameMode.Playing;
        MenuSelection = 0;
    }

    private void TurnInQuest(Quest quest, TownNpc npc)
    {
        quest.TurnedIn = true;
        if (quest.ConsequenceFlag is { } flag)
        {
            WorldFlags.Add(flag);
            ApplyWorldConsequence(flag);
        }
        Player.Gold += quest.RewardGold;
        if (quest.RewardItem is { } item) AddItem(item, quest.RewardItemQuantity, false);
        CloseDialogue();
        AddMessage($"QUEST COMPLETE: {quest.Title} — {QuestRewardText(quest)}. {npc.Name}: {quest.CompletionDialogue}");
        GrantExperience(quest.RewardXp);
    }

    private void OpenQuestLog()
    {
        Mode = GameMode.QuestLog;
        MenuSelection = 0;
    }

    private void HandleQuestLogKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Tab or ConsoleKey.Escape or ConsoleKey.Q)
        {
            Mode = GameMode.Playing;
            MenuSelection = 0;
            return;
        }
        var quests = JournalQuests;
        if (quests.Count == 0) return;
        if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W or ConsoleKey.K)
            MenuSelection = (MenuSelection - 1 + quests.Count) % quests.Count;
        else if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S or ConsoleKey.J)
            MenuSelection = (MenuSelection + 1) % quests.Count;
    }

    internal void RecordQuestProgress(QuestObjectiveKind kind, MonsterFaction? faction = null, MachineKind? machine = null)
    {
        foreach (var quest in Quests.Where(quest => quest.Accepted && !quest.TurnedIn && quest.Progress < quest.TargetCount))
        {
            var matches = quest.ObjectiveKind == kind && kind switch
            {
                QuestObjectiveKind.DefeatFaction => quest.TargetFaction == faction,
                QuestObjectiveKind.MountDungeonTorch => true,
                QuestObjectiveKind.OperateMachine => quest.TargetMachine == machine,
                QuestObjectiveKind.ClearDungeon => quest.TargetLocation is not null && CompletedDungeons.Contains(quest.TargetLocation),
                QuestObjectiveKind.DiscoverLandmark => quest.TargetLocation is not null && DiscoveredLandmarks.Contains(quest.TargetLocation),
                QuestObjectiveKind.GatherMaterial => quest.TargetItem is not null && Inventory.Where(item => item.Kind == quest.TargetItem).Sum(item => item.Quantity) >= quest.TargetCount,
                _ => false
            };
            if (!matches) continue;
            quest.Progress = quest.ObjectiveKind == QuestObjectiveKind.GatherMaterial && quest.TargetItem is { } item
                ? Math.Min(quest.TargetCount, Inventory.Where(stack => stack.Kind == item).Sum(stack => stack.Quantity))
                : Math.Min(quest.TargetCount, quest.Progress + 1);
            var giver = QuestGiver(quest)?.Name ?? "the quest giver";
            AddMessage(quest.Progress >= quest.TargetCount
                ? $"QUEST READY: {quest.Title} — return to {giver}."
                : $"QUEST: {quest.Title} — {quest.ProgressText}.");
        }
    }

    private void ReconcileQuestProgress(Quest quest)
    {
        var complete = quest.ObjectiveKind switch
        {
            QuestObjectiveKind.ClearDungeon => quest.TargetLocation is not null && CompletedDungeons.Contains(quest.TargetLocation),
            QuestObjectiveKind.DiscoverLandmark => quest.TargetLocation is not null && DiscoveredLandmarks.Contains(quest.TargetLocation),
            QuestObjectiveKind.GatherMaterial => quest.TargetItem is not null && Inventory.Where(item => item.Kind == quest.TargetItem).Sum(item => item.Quantity) >= quest.TargetCount,
            _ => false
        };
        if (complete) quest.Progress = quest.TargetCount;
    }

    private void ApplyWorldConsequence(string flag)
    {
        switch (flag)
        {
            case "bramblewick_safe":
                foreach (var monster in _monstersByMap[_wilderness].Where(monster => monster.Faction == MonsterFaction.Goblinoid))
                    monster.TerritoryRadius = Math.Max(4, monster.TerritoryRadius - 4);
                AddRegionalLights(new Point(198, 30), new Rgb(255, 176, 72));
                break;
            case "stonecross_restored":
                for (var x = 70; x <= 185; x += 23)
                    _wilderness.Lights.Add(new LightSource(new Point(x, 60), new Rgb(255, 154, 60), 8, 0.9, 0.1, x));
                break;
            case "reedwater_pure":
                foreach (var source in _wilderness.Provisions.Values.Where(source => source.Kind == ProvisionKind.Water))
                    source.Charges = Math.Max(source.Charges, 8);
                break;
            case "valeward_secured":
                AddRegionalLights(new Point(48, 94), new Rgb(255, 205, 110));
                break;
        }
    }

    private void AddRegionalLights(Point center, Rgb color)
    {
        foreach (var offset in new[] { new Point(-7, 0), new Point(7, 0), new Point(0, -5), new Point(0, 5) })
        {
            var p = center + offset;
            if (_wilderness.Contains(p) && _wilderness[p].Walkable)
                _wilderness.Lights.Add(new LightSource(p, color, 8, 0.95, 0.1, p.X + p.Y));
        }
    }

    public string QuestRewardText(Quest quest)
    {
        var parts = new List<string>();
        if (quest.RewardGold > 0) parts.Add($"{quest.RewardGold} gold");
        if (quest.RewardXp > 0) parts.Add($"{quest.RewardXp} XP");
        if (quest.RewardItem is { } item)
        {
            var reward = WorldBuilder.MakeItem(item);
            parts.Add($"{quest.RewardItemQuantity}× {reward.Name}");
        }
        return string.Join(" · ", parts);
    }
}
