namespace LumenRogue;

public static class EquipmentFactory
{
    public static ItemStack Create(string name, EquipmentSlot slot, char glyph, int attack = 0, int defense = 0,
        int spell = 0, double light = 0, ItemRarity rarity = ItemRarity.Common, string? description = null)
    {
        var kind = slot is EquipmentSlot.MainHand or EquipmentSlot.OffHand ? ItemKind.Weapon : ItemKind.Armor;
        return new ItemStack
        {
            Kind = kind, Name = name, Description = description ?? "Wearable equipment", Glyph = glyph,
            Color = RarityColor(rarity), Slot = slot, Rarity = rarity, AttackBonus = attack,
            DefenseBonus = defense, SpellBonus = spell, LightBonus = light
        };
    }

    public static ItemStack MonsterDrop(Random random, Actor monster, int playerLevel)
    {
        var rarity = RollRarity(random, monster.XpReward, playerLevel);
        var scale = 1 + (int)rarity;
        ItemStack item;
        var name = monster.Name;
        if (name.Contains("goblin"))
            item = random.Next(2) == 0
                ? Create("notched goblin dirk", EquipmentSlot.MainHand, '/', 1 + scale, rarity: rarity, description: "Quick, ugly, and expertly balanced")
                : Create("pot-lid buckler", EquipmentSlot.OffHand, ']', defense: 1 + scale / 2, rarity: rarity, description: "Dented metal with stubborn straps");
        else if (name.Contains("skeleton"))
            item = random.Next(2) == 0
                ? Create("ember-bone sabre", EquipmentSlot.MainHand, '/', 2 + scale, spell: scale / 2, rarity: rarity, description: "Warm bone sings when swung")
                : Create("grinning skullcap", EquipmentSlot.Head, '[', defense: 1 + scale, rarity: rarity, description: "Carved from lacquered vault-bone");
        else if (name.Contains("orc"))
            item = random.Next(2) == 0
                ? Create("slag-toothed axe", EquipmentSlot.MainHand, '¶', 3 + scale, rarity: rarity, description: "A heavy crescent of black iron")
                : Create("riveted slag coat", EquipmentSlot.Body, '[', defense: 2 + scale, rarity: rarity, description: "Crude plates that turn honest steel");
        else if (name.Contains("myconid"))
            item = Create("sporeglass ring", EquipmentSlot.Ring, '°', spell: 1 + scale, light: 0.25 * scale, rarity: rarity, description: "Cold bioluminescence pulses within it");
        else if (name.Contains("mage"))
            item = random.Next(2) == 0
                ? Create("cinder-channel staff", EquipmentSlot.MainHand, '/', attack: scale, spell: 2 + scale, light: 0.2 * scale, rarity: rarity, description: "A charred focus wrapped in copper")
                : Create("ash-sigil amulet", EquipmentSlot.Amulet, '♀', spell: 2 + scale, rarity: rarity, description: "Its sigil smoulders without heat");
        else if (name.Contains("troll"))
            item = random.Next(2) == 0
                ? Create("trollbone maul", EquipmentSlot.MainHand, '╫', 4 + scale, rarity: rarity, description: "Massive, blunt, and hard to argue with")
                : Create("moss-hide boots", EquipmentSlot.Feet, '∩', defense: 1 + scale, rarity: rarity, description: "They soften every footfall");
        else if (name.Contains("whisper"))
            item = Create("voidglass band", EquipmentSlot.Ring, '°', spell: 2 + scale, defense: scale / 2,
                light: 0.15 * scale, rarity: rarity, description: "Starlight bends wrongly across its surface");
        else if (name.Contains("warden"))
            item = random.Next(2) == 0
                ? Create("warden's sunblade", EquipmentSlot.MainHand, '†', 5 + scale, spell: 1 + scale,
                    light: 0.3 * scale, rarity: rarity < ItemRarity.Rare ? ItemRarity.Rare : rarity, description: "A royal edge that remembers daylight")
                : Create("vaultwarden cuirass", EquipmentSlot.Body, '[', defense: 4 + scale,
                    rarity: rarity < ItemRarity.Rare ? ItemRarity.Rare : rarity, description: "Blue steel engraved with dead oaths");
        else if (name.Contains("kobold"))
            item = Create("kobold candle-spear", EquipmentSlot.MainHand, '†', attack: 2 + scale, light: 0.2 * scale, rarity: rarity, description: "Its tallow flame never blows out");
        else if (name.Contains("spider"))
            item = Create("glassfang bracers", EquipmentSlot.Hands, '{', attack: scale, defense: scale, rarity: rarity, description: "Venomous crystal hooks line the wrists");
        else if (name.Contains("slime"))
            item = Create("ochre membrane", EquipmentSlot.Body, '[', defense: 1 + scale, spell: scale / 2, rarity: rarity, description: "It flows aside before a blow lands");
        else if (name.Contains("cultist"))
            item = Create("blood-script hood", EquipmentSlot.Head, '[', spell: 2 + scale, rarity: rarity, description: "Forbidden verses crawl across the lining");
        else if (name.Contains("gargoyle"))
            item = Create("gargoyle plate", EquipmentSlot.Body, '[', defense: 3 + scale, rarity: rarity, description: "Architectural stone jointed with black chain");
        else if (name.Contains("salamander"))
            item = Create("salamander-scale boots", EquipmentSlot.Feet, '∩', defense: 1 + scale, light: 0.2 * scale, rarity: rarity, description: "Each footprint glows for a heartbeat");
        else if (name.Contains("hound"))
            item = Create("umbra-fang amulet", EquipmentSlot.Amulet, '♀', attack: scale, spell: 1 + scale, rarity: rarity, description: "A shadow bites down inside the fang");
        else if (name.Contains("mimic"))
            item = Create("mimic-hide satchel ring", EquipmentSlot.Ring, '°', attack: scale, defense: scale, spell: scale, rarity: rarity, description: "It occasionally clicks its tiny teeth");
        else if (name.Contains("lich"))
            item = Create("dead king's diadem", EquipmentSlot.Head, '^', spell: 4 + scale, light: 0.3 * scale,
                rarity: rarity < ItemRarity.Rare ? ItemRarity.Rare : rarity, description: "Cold stars orbit its broken crown");
        else if (name.Contains("demon"))
            item = Create("furnace-heart greatblade", EquipmentSlot.MainHand, '╫', attack: 6 + scale, spell: 2 + scale, light: 0.4 * scale,
                rarity: ItemRarity.Epic, description: "A weapon forged around a still-beating coal");
        else if (name.Contains("bat") || name.Contains("rat"))
            item = Create(name.Contains("bat") ? "cavewing gloves" : "rat-hide boots",
                name.Contains("bat") ? EquipmentSlot.Hands : EquipmentSlot.Feet, name.Contains("bat") ? '{' : '∩',
                defense: scale, rarity: rarity, description: "Light armour stitched from dungeon hide");
        else
            item = Create("vault iron sword", EquipmentSlot.MainHand, '/', 2 + scale, rarity: rarity, description: "Serviceable steel from a forgotten armoury");

        return item;
    }

    public static ItemStack ArmoryWeapon() => Create("royal watch longsword", EquipmentSlot.MainHand, '/', 4,
        rarity: ItemRarity.Uncommon, description: "A balanced blade recovered from the royal armoury");

    public static ItemStack ArmoryArmor() => Create("royal watch brigandine", EquipmentSlot.Body, '[', defense: 4,
        rarity: ItemRarity.Uncommon, description: "Overlapping plates beneath faded red cloth");

    public static Rgb RarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => new Rgb(196, 201, 207),
        ItemRarity.Uncommon => new Rgb(92, 220, 126),
        ItemRarity.Rare => new Rgb(82, 155, 255),
        _ => new Rgb(213, 92, 255)
    };

    private static ItemRarity RollRarity(Random random, int reward, int level)
    {
        var roll = random.NextDouble() + Math.Min(0.28, reward / 240.0) + Math.Min(0.12, level * 0.015);
        if (roll > 1.18) return ItemRarity.Epic;
        if (roll > 0.91) return ItemRarity.Rare;
        if (roll > 0.58) return ItemRarity.Uncommon;
        return ItemRarity.Common;
    }
}
