namespace LumenRogue;

public static class SkillTreeFactory
{
    private sealed record Definition(string Name, LevelRewardKind Reward, SpellKind? Spell = null);

    public static List<SkillNode> Build(ClassKind characterClass)
    {
        var nodes = new List<SkillNode>();
        switch (characterClass)
        {
            case ClassKind.Vanguard:
                AddBranch(nodes, "STEEL", 0,
                    Might("Tempered Edge"), Guard("Measured Riposte"), Spell("Flame Cleave", SpellKind.FlameCleave),
                    Might("Relentless"), Might("Executioner"), Vitality("War Incarnate"));
                AddBranch(nodes, "BULWARK", 1,
                    Guard("Iron Skin"), Vitality("Deep Reserves"), Spell("Shield Flare", SpellKind.ShieldFlare),
                    Guard("Immovable"), Spell("Battle Cry", SpellKind.BattleCry), Vitality("Unbroken"));
                AddBranch(nodes, "EARTHFIRE", 2,
                    Spell("Stonebreak", SpellKind.Stonebreak), Radiance("Emberblood"), Spell("Seismic Wave", SpellKind.SeismicWave),
                    Spell("Fireball", SpellKind.Fireball), Spell("Luminous Nova", SpellKind.LuminousNova), Spell("Ground Quake", SpellKind.GroundQuake));
                AddBranch(nodes, "COMMAND", 3,
                    Arcana("Battle Rhythm"), Vitality("Hard March"), Might("Weapon Master"),
                    Guard("Hold the Line"), Radiance("Beacon Banner"), Might("Warlord"));
                break;
            case ClassKind.Arcanist:
                AddBranch(nodes, "ARCANA", 0,
                    Arcana("Expanded Mind"), Spell("Arcane Beam", SpellKind.ArcaneBeam), Spell("Prismatic Orb", SpellKind.PrismaticOrb),
                    Arcana("Deep Current"), Spell("Blink", SpellKind.Blink), Radiance("Archmagus"));
                AddBranch(nodes, "RUNES", 1,
                    Vitality("Runic Blood"), Guard("Mana Ward"), Arcana("Sigil Memory"),
                    Guard("Reactive Glyph"), Radiance("Spell Mirror"), Guard("Perfect Ward"));
                AddBranch(nodes, "ELEMENTS", 2,
                    Spell("Fireball", SpellKind.Fireball), Spell("Frost Lance", SpellKind.FrostLance), Spell("Chain Lightning", SpellKind.ChainLightning),
                    Spell("Meteor", SpellKind.Meteor), Spell("Ice Storm", SpellKind.IceStorm), Radiance("Elemental Crown"));
                AddBranch(nodes, "AETHER", 3,
                    Radiance("Aether Sense"), Arcana("Reservoir"), Vitality("Glass Soul"),
                    Radiance("Overchannel"), Arcana("Endless Formula"), Radiance("Aether Ascendant"));
                break;
            case ClassKind.Ranger:
                AddBranch(nodes, "HUNT", 0,
                    Might("Deadeye"), Spell("Thorn Volley", SpellKind.ThornVolley), Spell("Multishot", SpellKind.Multishot),
                    Might("Heartseeker"), Spell("Marked Shot", SpellKind.MarkedShot), Might("Apex Hunter"));
                AddBranch(nodes, "SURVIVAL", 1,
                    Vitality("Trail Hardened"), Guard("Barkskin"), Vitality("Forager"),
                    Spell("Poison Cloud", SpellKind.PoisonCloud), Guard("Ghost Step"), Vitality("Never Lost"));
                AddBranch(nodes, "GLOOM", 2,
                    Spell("Shadow Bolt", SpellKind.ShadowBolt), Spell("Frost Lance", SpellKind.FrostLance), Spell("Chain Lightning", SpellKind.ChainLightning),
                    Spell("Fireball", SpellKind.Fireball), Radiance("Night Eyes"), Guard("Gloom Walker"));
                AddBranch(nodes, "WILD", 3,
                    Might("Wild Instinct"), Arcana("Deep Breath"), Guard("Thick Hide"),
                    Radiance("Predator's Light"), Might("Pack Hunter"), Vitality("Primal Legend"));
                break;
            default:
                AddBranch(nodes, "DEVOTION", 0,
                    Radiance("Living Radiance"), Spell("Sun Lance", SpellKind.SunLance), Spell("Solar Ray", SpellKind.SolarRay),
                    Radiance("Consecration"), Spell("Luminous Nova", SpellKind.LuminousNova), Arcana("Beacon Soul"));
                AddBranch(nodes, "AEGIS", 1,
                    Guard("Sacred Ward"), Vitality("Martyr's Heart"), Spell("Shield Flare", SpellKind.ShieldFlare),
                    Guard("Golden Bulwark"), Spell("Sanctuary", SpellKind.Sanctuary), Vitality("Undying Flame"));
                AddBranch(nodes, "MIRACLES", 2,
                    Spell("Healing Light", SpellKind.HealingLight), Arcana("Font of Grace"), Spell("Chain Lightning", SpellKind.ChainLightning),
                    Radiance("Wrathful Light"), Spell("Smite", SpellKind.Smite), Radiance("Living Miracle"));
                AddBranch(nodes, "PILGRIM", 3,
                    Vitality("Pilgrim's Vigor"), Guard("Steady Flame"), Arcana("Deep Faith"),
                    Might("Dawn March"), Radiance("Halo"), Vitality("Saint of Embers"));
                break;
        }
        return nodes;
    }

    private static void AddBranch(List<SkillNode> nodes, string branch, int branchIndex, params Definition[] definitions)
    {
        var levelGates = new[] { 2, 3, 4, 5, 7, 9 };
        for (var tier = 0; tier < definitions.Length; tier++)
        {
            var definition = definitions[tier];
            nodes.Add(new SkillNode
            {
                Id = $"{branchIndex}:{tier}", Branch = branch, BranchIndex = branchIndex, Tier = tier,
                Name = definition.Name, Description = Description(definition), RequiredLevel = levelGates[tier],
                PrerequisiteId = tier == 0 ? null : $"{branchIndex}:{tier - 1}", Reward = definition.Reward,
                Spell = definition.Spell, MaxRank = 3
            });
        }
    }

    private static string Description(Definition definition) => definition.Reward switch
    {
        LevelRewardKind.Vitality => "+4 maximum HP and +1 Constitution each rank",
        LevelRewardKind.Might => "+1 base attack and +1 Strength each rank",
        LevelRewardKind.Guard => "+1 base defense each rank",
        LevelRewardKind.Arcana => "+3 maximum mana and +1 Intellect each rank",
        LevelRewardKind.Radiance => "+1 spell power and +0.25 light radius each rank",
        _ => Game.SpellDescription(definition.Spell!.Value) + " Each rank adds spell power; rank 3 reduces mana cost."
    };

    private static Definition Vitality(string name) => new(name, LevelRewardKind.Vitality);
    private static Definition Might(string name) => new(name, LevelRewardKind.Might);
    private static Definition Guard(string name) => new(name, LevelRewardKind.Guard);
    private static Definition Arcana(string name) => new(name, LevelRewardKind.Arcana);
    private static Definition Radiance(string name) => new(name, LevelRewardKind.Radiance);
    private static Definition Spell(string name, SpellKind spell) => new(name, LevelRewardKind.LearnSpell, spell);
}
