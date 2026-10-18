namespace LumenRogue;

public readonly record struct Point(int X, int Y)
{
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public int DistanceSquared(Point other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return dx * dx + dy * dy;
    }
}

public readonly record struct Rgb(int R, int G, int B)
{
    public static readonly Rgb Black = new(0, 0, 0);
    public static readonly Rgb White = new(255, 255, 255);

    public Rgb Scale(double amount) => new(
        Math.Clamp((int)(R * amount), 0, 255),
        Math.Clamp((int)(G * amount), 0, 255),
        Math.Clamp((int)(B * amount), 0, 255));

    public static Rgb Lerp(Rgb a, Rgb b, double t) => new(
        (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t),
        (int)(a.B + (b.B - a.B) * t));
}

public sealed record Tile(char Glyph, Rgb Foreground, Rgb Background, bool Walkable, bool Transparent, string Name,
    bool Destructible = false, int Toughness = 0, double Reflectivity = 0.08);

public sealed record LightSource(Point Position, Rgb Color, double Radius, double Intensity, double Flicker = 0,
    double Phase = 0, double Softness = 0.28, double Diffraction = 0.12, double Bounce = 0.08);

public enum RaceKind { Human, Dwarf, Elf, Ashborn }
public enum ClassKind { Vanguard, Arcanist, Ranger, Lightbearer }
public enum MonsterAi { Hunter, Brute, Skirmisher, Caster, Lurker }
public enum MonsterIntentKind { Idle, Roam, Advance, Strike, Shoot, Retreat, Guard, Sleep }
public enum PlayerReactionKind { Block, Dodge, Ward }
public enum BiomeKind { Emberfall, Wilds, AshenVaults, DrownedReliquary, PrismaticFoundry, LightlessCourt, CrownOfCinders }
public enum MonsterFaction { Vermin, Goblinoid, Undead, Wild, Cult, Construct, Void, Infernal }
public enum TravelKind { Wilderness, Town, HollowrootMine, SunkenBarrow, MoonwellCavern, CastleUndercroft, WildernessReturn }
[Flags] public enum MonsterSense { None = 0, Sight = 1, Noise = 2, Scent = 4, Vibration = 8, Light = 16 }
public enum DoorState { Closed, Open, Locked, Secret, Broken }
public enum TrapKind { FlameJet, ShockPlate, PoisonVent, Collapse, Alarm }
public enum MachineKind { Brazier, Floodgate, Prism, Generator }
public enum ItemKind
{
    HealingPotion, Ration, WaterFlask, ManaTonic, Bomb, Torch, Weapon, Armor, Crystal,
    GreaterHealingPotion, MightElixir, GuardElixir, FocusElixir, SmokeBomb, Waystone, Ambrosia,
    BeastHide, VenomGland, GoblinScrap, RelicShard, BossRelic
}
public enum SpellKind
{
    Fireball, FrostLance, ChainLightning, LuminousNova, Stonebreak,
    ArcaneBeam, Meteor, SeismicWave, ShieldFlare, ThornVolley,
    ShadowBolt, SunLance, HealingLight,
    FlameCleave, GroundQuake, BattleCry,
    IceStorm, Blink, PrismaticOrb,
    Multishot, PoisonCloud, MarkedShot,
    SolarRay, Sanctuary, Smite
}
public enum LevelRewardKind { Vitality, Might, Guard, Arcana, Radiance, LearnSpell }
public enum ProvisionKind { Food, Water }
public enum EquipmentSlot { MainHand, OffHand, Head, Body, Hands, Feet, Ring, Amulet }
public enum ItemRarity { Common, Uncommon, Rare, Epic }
public enum SurfaceMarkKind { Blood, Scorch, Frost, WetFootprint, Soot, CrystalShard, Crack }
public enum FlourishKind
{
    Impact, CriticalImpact, Steam, FrostCrack, ElectricBranch, PoisonCurl, EmberCollapse,
    DustFall, CrystalShatter, SanctuaryHalo, StairArrival, SkillBloom
}
public enum ArcStyle { Lightning, Beam, Flame, Thorn, Arrow, Solar, Seismic, Blink }
public enum QuestObjectiveKind { DefeatFaction, MountDungeonTorch, OperateMachine, ClearDungeon, DiscoverLandmark, GatherMaterial }
public enum QuestStatus { Available, Active, ReadyToTurnIn, Completed }

public sealed record SurfaceMark(Point Position, SurfaceMarkKind Kind, char Glyph, Rgb Color, int CreatedTurn);

public sealed class TownNpc
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public required Point Position { get; set; }
    public char Glyph { get; init; } = '☺';
    public required Rgb Color { get; init; }
    public required string Greeting { get; init; }
    public string? QuestId { get; init; }
    public Point? HomePosition { get; set; }
    public Point? WorkPosition { get; set; }
    public Point? EveningPosition { get; set; }
    public string ScheduleState { get; set; } = "working";
}

public sealed class Quest
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string GiverId { get; init; }
    public required string Description { get; init; }
    public required string ObjectiveText { get; init; }
    public required QuestObjectiveKind ObjectiveKind { get; init; }
    public MonsterFaction? TargetFaction { get; init; }
    public MachineKind? TargetMachine { get; init; }
    public string? TargetLocation { get; init; }
    public ItemKind? TargetItem { get; init; }
    public string? PrerequisiteQuestId { get; init; }
    public string? ConsequenceFlag { get; init; }
    public List<string> RequiredFlags { get; init; } = [];
    public int TargetCount { get; init; } = 1;
    public int Progress { get; set; }
    public bool Accepted { get; set; }
    public bool TurnedIn { get; set; }
    public int RewardGold { get; init; }
    public int RewardXp { get; init; }
    public ItemKind? RewardItem { get; init; }
    public int RewardItemQuantity { get; init; } = 1;
    public string AcceptDialogue { get; init; } = "Then we have an agreement.";
    public string ProgressDialogue { get; init; } = "The work is not finished yet.";
    public string CompletionDialogue { get; init; } = "You have done Emberfall a service.";
    public QuestStatus Status => TurnedIn ? QuestStatus.Completed : !Accepted ? QuestStatus.Available :
        Progress >= TargetCount ? QuestStatus.ReadyToTurnIn : QuestStatus.Active;
    public string ProgressText => $"{Math.Min(Progress, TargetCount)}/{TargetCount}";
}

public sealed class Actor
{
    public required Point Position { get; set; }
    public required char Glyph { get; set; }
    public required Rgb Color { get; set; }
    public required string Name { get; set; }
    public required int MaxHp { get; set; }
    public int Hp { get; set; }
    public required int Attack { get; set; }
    public int Defense { get; set; }
    public int Gold { get; set; }
    public int XpReward { get; set; }
    public MonsterAi Ai { get; set; } = MonsterAi.Hunter;
    public int FrozenTurns { get; set; }
    public int PoisonTurns { get; set; }
    public int WebbedTurns { get; set; }
    public int AlertTurns { get; set; }
    public Point? LastKnownPlayer { get; set; }
    public MonsterFaction Faction { get; set; } = MonsterFaction.Wild;
    public MonsterSense Senses { get; set; } = MonsterSense.Sight | MonsterSense.Noise;
    public bool CanOpenDoors { get; set; }
    public bool LightSensitive { get; set; }
    public bool AttractedToLight { get; set; }
    public Rgb? GlowColor { get; set; }
    public bool IsBoss { get; set; }
    public string? UniqueRewardName { get; set; }
    public MonsterIntentKind Intent { get; set; } = MonsterIntentKind.Idle;
    public Point HomePosition { get; set; }
    public int TerritoryRadius { get; set; } = 10;
    public bool ActiveAtNight { get; set; }
    public bool ActiveByDay { get; set; } = true;
    public bool RewardsClaimed { get; set; }
    public bool IsAlive => Hp > 0;
}

public sealed class ItemStack
{
    public required ItemKind Kind { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required char Glyph { get; init; }
    public required Rgb Color { get; init; }
    public int Quantity { get; set; } = 1;
    public EquipmentSlot? Slot { get; init; }
    public ItemRarity Rarity { get; init; } = ItemRarity.Common;
    public int AttackBonus { get; init; }
    public int DefenseBonus { get; init; }
    public int SpellBonus { get; init; }
    public double LightBonus { get; init; }
    public bool Equipped { get; set; }
    public bool IsEquippable => Slot is not null;
    public bool IsMaterial => Kind is ItemKind.BeastHide or ItemKind.VenomGland or ItemKind.GoblinScrap or ItemKind.RelicShard or ItemKind.BossRelic;

    public string BonusText
    {
        get
        {
            var parts = new List<string>();
            if (AttackBonus != 0) parts.Add($"ATK +{AttackBonus}");
            if (DefenseBonus != 0) parts.Add($"DEF +{DefenseBonus}");
            if (SpellBonus != 0) parts.Add($"SPELL +{SpellBonus}");
            if (LightBonus > 0) parts.Add($"LIGHT +{LightBonus:0.0}");
            return parts.Count == 0 ? Description : string.Join("  ", parts);
        }
    }

    public ItemStack Copy(int quantity = 1) => new()
    {
        Kind = Kind, Name = Name, Description = Description, Glyph = Glyph, Color = Color, Quantity = quantity,
        Slot = Slot, Rarity = Rarity, AttackBonus = AttackBonus, DefenseBonus = DefenseBonus,
        SpellBonus = SpellBonus, LightBonus = LightBonus, Equipped = Equipped
    };
}

public sealed record GroundItem(Point Position, ItemStack Item);

public sealed record LevelChoice(LevelRewardKind Kind, string Name, string Description, SpellKind? Spell = null);

public sealed record CraftingIngredient(ItemKind Kind, int Quantity);

public sealed class CraftingRecipe
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required List<CraftingIngredient> Ingredients { get; init; }
    public required ItemStack Result { get; init; }
    public int GoldCost { get; init; }
}

public sealed class SkillNode
{
    public required string Id { get; init; }
    public required string Branch { get; init; }
    public required int BranchIndex { get; init; }
    public required int Tier { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int RequiredLevel { get; init; }
    public string? PrerequisiteId { get; init; }
    public required LevelRewardKind Reward { get; init; }
    public SpellKind? Spell { get; init; }
    public int Rank { get; set; }
    public int MaxRank { get; init; } = 3;
    public bool Purchased => Rank > 0;
    public bool Maxed => Rank >= MaxRank;
}

public sealed class ProvisionSource
{
    public required Point Position { get; init; }
    public required ProvisionKind Kind { get; init; }
    public required string Name { get; init; }
    public int Charges { get; set; }
}

public sealed record ShopItem(string Name, string Description, int Price, Action<Game> Purchase);

public sealed class Shop
{
    public required string Name { get; init; }
    public required Point Position { get; init; }
    public required Rgb Color { get; init; }
    public required List<ShopItem> Items { get; init; }
}

public sealed class ElementCell
{
    public int Wet { get; set; }
    public int Oil { get; set; }
    public int Fire { get; set; }
    public int Frost { get; set; }
    public int Electric { get; set; }
    public int Poison { get; set; }
    public int Smoke { get; set; }
    public int Web { get; set; }
    public bool Active => Wet + Oil + Fire + Frost + Electric + Poison + Smoke + Web > 0;
}

public sealed class DoorFeature
{
    public required Point Position { get; init; }
    public DoorState State { get; set; }
}

public sealed class TrapFeature
{
    public required Point Position { get; init; }
    public required TrapKind Kind { get; init; }
    public bool Revealed { get; set; }
    public bool Armed { get; set; } = true;
}

public sealed class MachineFeature
{
    public required Point Position { get; init; }
    public required MachineKind Kind { get; init; }
    public bool Active { get; set; } = true;
}

public sealed class WallTorchFeature
{
    public required Point Position { get; init; }
    public required Tile UnderlyingWall { get; init; }
}

public sealed class Map
{
    public int Width { get; }
    public int Height { get; }
    public string Name { get; set; }
    public bool IsTown { get; }
    public bool IsOutdoor => IsTown || Biome == BiomeKind.Wilds;
    public BiomeKind Biome { get; set; }
    public Tile[,] Tiles { get; }
    public bool[,] Explored { get; }
    public ElementCell[,] Elements { get; }
    public List<LightSource> Lights { get; } = [];
    public HashSet<Point> ExtinguishedLights { get; } = [];
    public List<Shop> Shops { get; } = [];
    public List<TownNpc> Npcs { get; } = [];
    public List<GroundItem> GroundItems { get; } = [];
    public List<string> SpecialRoomNames { get; } = [];
    public Dictionary<Point, DoorFeature> Doors { get; } = [];
    public Dictionary<Point, TrapFeature> Traps { get; } = [];
    public Dictionary<Point, MachineFeature> Machines { get; } = [];
    public Dictionary<Point, WallTorchFeature> WallTorches { get; } = [];
    public Dictionary<Point, ProvisionSource> Provisions { get; } = [];
    public Dictionary<Point, SurfaceMark> SurfaceMarks { get; } = [];
    public Dictionary<Point, TravelKind> TravelPoints { get; } = [];
    public Dictionary<Point, string> Landmarks { get; } = [];
    public List<Point> CriticalPoints { get; } = [];
    public string DungeonMechanic { get; set; } = "";
    public string BossName { get; set; } = "";
    private readonly Dictionary<Point, int> _integrity = [];
    public Point Start { get; set; }
    public Point StairsDown { get; set; } = new(-1, -1);
    public Point StairsUp { get; set; } = new(-1, -1);

    public Map(int width, int height, string name, bool isTown, Tile fill)
    {
        Width = width;
        Height = height;
        Name = name;
        IsTown = isTown;
        Biome = isTown ? BiomeKind.Emberfall : BiomeKind.AshenVaults;
        Tiles = new Tile[width, height];
        Explored = new bool[width, height];
        Elements = new ElementCell[width, height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            Tiles[x, y] = fill;
            Elements[x, y] = new ElementCell();
            if (fill.Destructible) _integrity[new Point(x, y)] = fill.Toughness;
        }
    }

    public bool Contains(Point p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
    public Tile this[Point p] => Tiles[p.X, p.Y];
    public void Set(Point p, Tile tile)
    {
        Tiles[p.X, p.Y] = tile;
        if (tile.Destructible) _integrity[p] = tile.Toughness;
        else _integrity.Remove(p);
    }

    public bool DamageTerrain(Point p, int damage, Tile rubble)
    {
        if (!Contains(p) || !Tiles[p.X, p.Y].Destructible) return false;
        var remaining = _integrity.GetValueOrDefault(p, Tiles[p.X, p.Y].Toughness) - damage;
        if (remaining > 0)
        {
            _integrity[p] = remaining;
            var tile = Tiles[p.X, p.Y];
            Tiles[p.X, p.Y] = tile with { Glyph = remaining <= tile.Toughness / 2 ? '▒' : tile.Glyph };
            return false;
        }
        if (WallTorches.Remove(p))
        {
            ExtinguishedLights.Remove(p);
            Lights.RemoveAll(light => light.Position == p);
        }
        Set(p, rubble);
        return true;
    }

    public bool HasLineOfSight(Point from, Point to)
    {
        var x0 = from.X;
        var y0 = from.Y;
        var x1 = to.X;
        var y1 = to.Y;
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            if (x0 == x1 && y0 == y1) return true;
            if (!(x0 == from.X && y0 == from.Y) && !Tiles[x0, y0].Transparent) return false;
            var e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
            if (!Contains(new Point(x0, y0))) return false;
        }
    }
}

public abstract class VisualEffect
{
    public required double StartedAt { get; init; }
    public abstract bool IsFinished(double now);
    public abstract IEnumerable<LightSource> GetLights(double now);
}

public sealed class ProjectileEffect : VisualEffect
{
    public required List<Point> Path { get; init; }
    public SpellKind Spell { get; init; } = SpellKind.Fireball;
    public bool IsBomb { get; init; }
    public Rgb OuterColor { get; init; } = new(255, 92, 12);
    public Rgb CoreColor { get; init; } = new(255, 244, 205);
    public char Glyph { get; init; } = '●';
    public double StepSeconds { get; init; } = 0.055;
    public bool Resolved { get; set; }
    public Point Current(double now) => Path[Math.Clamp((int)((now - StartedAt) / StepSeconds), 0, Path.Count - 1)];
    public override bool IsFinished(double now) => now - StartedAt >= Path.Count * StepSeconds;

    public override IEnumerable<LightSource> GetLights(double now)
    {
        var p = Current(now);
        yield return new LightSource(p, OuterColor, 9.5, 2.35, 0.12, StartedAt * 9, 0.42, 0.2, 0.12);
        yield return new LightSource(p, CoreColor, 3.2, 3.1, 0, 0, 0.5, 0.18, 0.1);
    }
}

public sealed class ExplosionEffect : VisualEffect
{
    public required Point Position { get; init; }
    public Rgb HotColor { get; init; } = new(255, 255, 240);
    public Rgb CoolColor { get; init; } = new(255, 48, 3);
    public int Seed { get; init; }
    public double Duration { get; init; } = 1.15;
    public override bool IsFinished(double now) => now - StartedAt >= Duration;

    public override IEnumerable<LightSource> GetLights(double now)
    {
        var t = Math.Clamp((now - StartedAt) / Duration, 0, 1);
        var flash = Math.Pow(1 - t, 1.35);
        var color = Rgb.Lerp(HotColor, CoolColor, Math.Pow(t, 0.65));
        yield return new LightSource(Position, color, 6 + t * 11, 6.2 * flash + 0.18, 0.1, StartedAt, 0.55, 0.3, 0.18);
        if (t < 0.16) yield return new LightSource(Position, Rgb.White, 20, 7.5 * (1 - t / 0.16), 0, 0, 0.7, 0.35, 0.22);
    }
}

public sealed class ArcEffect : VisualEffect
{
    public required List<Point> Points { get; init; }
    public Rgb Color { get; init; } = new(125, 205, 255);
    public ArcStyle Style { get; init; } = ArcStyle.Lightning;
    public double Duration { get; init; } = 0.42;
    public override bool IsFinished(double now) => now - StartedAt >= Duration;
    public override IEnumerable<LightSource> GetLights(double now)
    {
        var fade = Math.Max(0, 1 - (now - StartedAt) / Duration);
        foreach (var p in Points) yield return new LightSource(p, Color, 5, 1.8 * fade, 0.15, p.X * 0.7, 0.5, 0.25, 0.1);
    }
}

public sealed class FlourishEffect : VisualEffect
{
    public required Point Position { get; init; }
    public FlourishKind Kind { get; init; }
    public Point Direction { get; init; }
    public Rgb Color { get; init; } = Rgb.White;
    public Rgb SecondaryColor { get; init; } = new(255, 120, 45);
    public int Seed { get; init; }
    public double Duration { get; init; } = 0.45;
    public double Radius { get; init; } = 4;
    public override bool IsFinished(double now) => now - StartedAt >= Duration;

    public override IEnumerable<LightSource> GetLights(double now)
    {
        var t = Math.Clamp((now - StartedAt) / Duration, 0, 1);
        var fade = 1 - t;
        if (Kind is FlourishKind.Impact or FlourishKind.CriticalImpact)
            yield return new LightSource(Position, Color, Kind == FlourishKind.CriticalImpact ? 3.5 : 2.2,
                (Kind == FlourishKind.CriticalImpact ? 0.8 : 0.32) * fade, 0, Seed, 0.3, 0.08, 0.05);
        else if (Kind is FlourishKind.ElectricBranch or FlourishKind.SkillBloom or FlourishKind.SanctuaryHalo or FlourishKind.StairArrival)
            yield return new LightSource(Position, Color, Radius + t * Radius, 1.35 * fade, 0.05, Seed, 0.5, 0.22, 0.14);
        else if (Kind is FlourishKind.Steam or FlourishKind.EmberCollapse or FlourishKind.CrystalShatter)
            yield return new LightSource(Position, SecondaryColor, Radius, 0.45 * fade, 0.08, Seed, 0.4, 0.15, 0.1);
    }
}

public sealed class SmokeEffect : VisualEffect
{
    public required Point Position { get; init; }
    public int Seed { get; init; }
    public double Duration { get; init; } = 2.4;
    public override bool IsFinished(double now) => now - StartedAt >= Duration;
    public override IEnumerable<LightSource> GetLights(double now) { yield break; }
}
