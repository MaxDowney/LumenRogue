namespace LumenRogue;

public sealed partial class Game
{
    public double GameplayLightAt(Point p)
    {
        if (!CurrentMap.Contains(p)) return 0;
        var light = CurrentMap.IsOutdoor ? OutdoorAmbientLight * (0.55 + OutdoorSunVisibilityAt(p) * 0.45) : CurrentMap.Biome switch
        {
            BiomeKind.PrismaticFoundry => 0.08,
            BiomeKind.CrownOfCinders => 0.07,
            BiomeKind.LightlessCourt => 0.01,
            _ => 0.035
        };
        foreach (var source in CurrentMap.Lights)
        {
            if (CurrentMap.ExtinguishedLights.Contains(source.Position)) continue;
            var distance = Math.Sqrt(source.Position.DistanceSquared(p));
            if (distance >= source.Radius || !HasClearLightPath(source.Position, p)) continue;
            light += source.Intensity * Math.Pow(1 - distance / source.Radius, 1.7) * 0.72;
        }
        var playerDistance = Math.Sqrt(Player.Position.DistanceSquared(p));
        if (LanternLit && playerDistance < EffectiveLanternRadius && HasClearLightPath(Player.Position, p))
            light += LanternIntensity * Math.Pow(1 - playerDistance / EffectiveLanternRadius, 1.6);
        for (var y = Math.Max(0, p.Y - 10); y <= Math.Min(CurrentMap.Height - 1, p.Y + 10); y++)
        for (var x = Math.Max(0, p.X - 10); x <= Math.Min(CurrentMap.Width - 1, p.X + 10); x++)
        {
            var fire = CurrentMap.Elements[x, y].Fire;
            if (fire <= 0) continue;
            var source = new Point(x, y);
            var distance = Math.Sqrt(source.DistanceSquared(p));
            if (distance < 7 && HasClearLightPath(source, p)) light += Math.Pow(1 - distance / 7, 1.5) * 0.75;
        }
        var smoke = CurrentMap.Elements[p.X, p.Y].Smoke;
        if (smoke > 0) light *= 0.38;
        return Math.Clamp(light, 0, 1.8);
    }

    public bool HasClearSight(Point from, Point to)
    {
        if (!CurrentMap.HasLineOfSight(from, to)) return false;
        return !Line(from, to).Skip(1).SkipLast(1).Any(p => CurrentMap.Elements[p.X, p.Y].Smoke > 0);
    }

    internal static bool TileCastsLightShadow(Tile tile) => !tile.Transparent || !tile.Walkable;

    public HashSet<Point> DynamicShadowCasters()
    {
        var result = Monsters.Where(monster => monster.IsAlive).Select(monster => monster.Position).ToHashSet();
        foreach (var npc in CurrentMap.Npcs) result.Add(npc.Position);
        result.Add(Player.Position);
        return result;
    }

    public bool HasClearLightPath(Point from, Point to)
    {
        if (!CurrentMap.Contains(from) || !CurrentMap.Contains(to)) return false;
        var dynamicCasters = DynamicShadowCasters();
        foreach (var point in Line(from, to).Skip(1).SkipLast(1))
        {
            if (TileCastsLightShadow(CurrentMap[point]) || dynamicCasters.Contains(point) || CurrentMap.Elements[point.X, point.Y].Smoke > 0)
                return false;
        }
        return true;
    }

    public double OutdoorSunVisibilityAt(Point target)
    {
        if (!CurrentMap.IsOutdoor || OutdoorDaylight <= 0.01) return 1;
        var dayProgress = Math.Clamp((WorldMinutes - 360) / 720.0, 0, 1);
        var towardSunX = Math.Cos(dayProgress * Math.PI);
        var towardSunY = -0.58 - Math.Sin(dayProgress * Math.PI) * 0.34;
        var dynamicCasters = DynamicShadowCasters();
        var clearSamples = 0;
        foreach (var offset in new[] { -0.42, 0.0, 0.42 })
        {
            var clear = true;
            for (var step = 1; step <= 14; step++)
            {
                var x = target.X + towardSunX * step - towardSunY * offset;
                var y = target.Y + towardSunY * step + towardSunX * offset;
                var point = new Point((int)Math.Round(x), (int)Math.Round(y));
                if (!CurrentMap.Contains(point)) break;
                if (TileCastsLightShadow(CurrentMap[point]) || dynamicCasters.Contains(point)) { clear = false; break; }
            }
            if (clear) clearSamples++;
        }
        return clearSamples / 3.0;
    }

    private void MakeNoise(int amount, Point? origin = null)
    {
        if (amount <= NoiseLevel) return;
        NoiseLevel = amount;
        _noiseOrigin = origin ?? Player.Position;
    }

    private void ToggleLantern()
    {
        LanternLit = !LanternLit;
        MakeNoise(1);
        CommitTurn(LanternLit ? "You uncover the lantern. Warm light spills outward." : "You shutter the lantern and let darkness close around you.");
    }

    private void PlaceWallTorch(Point direction)
    {
        var target = Player.Position + direction;
        AwaitingDirection = false;
        _pendingWallTorch = false;
        if (!CurrentMap.Contains(target) || CurrentMap[target].Walkable || CurrentMap.Doors.ContainsKey(target) ||
            CurrentMap.Machines.ContainsKey(target) || CurrentMap.WallTorches.ContainsKey(target))
        {
            AddItem(ItemKind.Torch, 1, false);
            AddMessage("A wall torch needs a bare adjacent wall.");
            return;
        }

        var underlying = CurrentMap[target];
        CurrentMap.WallTorches[target] = new WallTorchFeature { Position = target, UnderlyingWall = underlying };
        CurrentMap.Set(target, WorldBuilder.MountedWallTorch);
        CurrentMap.ExtinguishedLights.Remove(target);
        CurrentMap.Lights.Add(new LightSource(target, new Rgb(255, 103, 25), 9, 1.3, 0.2,
            target.X * 0.73 + target.Y));
        MakeNoise(2, target);
        CommitTurn("You mount and light the wall torch.");
        if (!CurrentMap.IsOutdoor) RecordQuestProgress(QuestObjectiveKind.MountDungeonTorch);
    }

    private void TakeWallTorch(WallTorchFeature wallTorch)
    {
        var position = wallTorch.Position;
        CurrentMap.WallTorches.Remove(position);
        CurrentMap.ExtinguishedLights.Remove(position);
        CurrentMap.Lights.RemoveAll(light => light.Position == position);
        CurrentMap.Set(position, wallTorch.UnderlyingWall);
        AddItem(ItemKind.Torch, 1, false);
        MakeNoise(2, position);
        CommitTurn("You lift the wall torch from its bracket and stow it.");
    }

    private void GatherProvision(ProvisionSource source)
    {
        if (source.Charges <= 0)
        {
            AddMessage($"The {source.Name} has been picked clean.");
            return;
        }
        source.Charges--;
        var itemKind = source.Kind == ProvisionKind.Food ? ItemKind.Ration : ItemKind.WaterFlask;
        AddItem(itemKind, 1, false);
        if (source.Charges == 0) CurrentMap.Set(source.Position, WorldBuilder.EmptySupply);
        MakeNoise(1, source.Position);
        CommitTurn(source.Kind == ProvisionKind.Food
            ? $"You recover a trail ration from the {source.Name}. {source.Charges} serving{(source.Charges == 1 ? "" : "s")} remain."
            : $"You fill a sealed flask at the {source.Name}. {source.Charges} serving{(source.Charges == 1 ? "" : "s")} remain.");
    }

    private bool MonsterDetectsPlayer(Actor monster)
    {
        var distance = Math.Max(Math.Abs(monster.Position.X - Player.Position.X), Math.Abs(monster.Position.Y - Player.Position.Y));
        var exposure = GameplayLightAt(Player.Position);
        var sees = monster.Senses.HasFlag(MonsterSense.Sight) && HasClearSight(monster.Position, Player.Position) &&
            distance <= 4 + (int)Math.Round(exposure * (monster.LightSensitive ? 13 : 9));
        var seesLight = monster.Senses.HasFlag(MonsterSense.Light) && HasClearSight(monster.Position, Player.Position) &&
            exposure > (monster.LightSensitive ? 0.18 : 0.42) && distance <= 16;
        var hears = monster.Senses.HasFlag(MonsterSense.Noise) && NoiseLevel > 0 &&
            monster.Position.DistanceSquared(_noiseOrigin) <= NoiseLevel * NoiseLevel * 3;
        var vibration = monster.Senses.HasFlag(MonsterSense.Vibration) && NoiseLevel >= 2 && distance <= 10;
        var scent = monster.Senses.HasFlag(MonsterSense.Scent) && distance <= 7;
        return sees || seesLight || hears || vibration || scent;
    }

    private void RefreshDetectionState()
    {
        if (CurrentMap.IsTown) { DetectionState = "SAFE"; return; }
        var hunters = Monsters.Count(monster => monster.IsAlive && monster.AlertTurns > 0);
        DetectionState = hunters > 0 ? $"HUNTED {hunters}" : PlayerExposure > 0.62 ? "EXPOSED" : PlayerExposure > 0.24 ? "DIM" : "HIDDEN";
    }

    private bool HandleDoorCollision(Point target)
    {
        if (!CurrentMap.Doors.TryGetValue(target, out var door) || door.State is DoorState.Open or DoorState.Broken) return false;
        if (door.State == DoorState.Secret)
        {
            AddMessage("The wall feels hollow. Search nearby with V.");
            return true;
        }
        if (door.State == DoorState.Locked)
        {
            MakeNoise(6, target);
            if (Strength + _random.Next(8) >= 17)
            {
                door.State = DoorState.Broken;
                CurrentMap.Set(target, WorldBuilder.OpenDoor with { Glyph = '░', Name = "splintered door" });
                CommitTurn("You shoulder through the lock in a burst of timber and iron.");
            }
            else CommitTurn("The locked door booms but holds.");
            return true;
        }
        door.State = DoorState.Open;
        CurrentMap.Set(target, WorldBuilder.OpenDoor);
        MakeNoise(2, target);
        CommitTurn("You open the door.");
        return true;
    }

    private void SearchSurroundings()
    {
        var found = new List<string>();
        foreach (var door in CurrentMap.Doors.Values.Where(door => door.State == DoorState.Secret &&
                     Chebyshev(door.Position, Player.Position) <= 2).ToList())
        {
            door.State = DoorState.Closed;
            CurrentMap.Set(door.Position, WorldBuilder.ClosedDoor);
            found.Add("a secret door");
        }
        foreach (var trap in CurrentMap.Traps.Values.Where(trap => !trap.Revealed && Chebyshev(trap.Position, Player.Position) <= 2))
        {
            trap.Revealed = true;
            found.Add($"a {TrapName(trap.Kind)}");
        }
        MakeNoise(1);
        CommitTurn(found.Count == 0 ? "You study every seam and shadow, finding nothing." : $"You discover {string.Join(" and ", found.Distinct())}.");
    }

    private void TriggerTrap(Point position)
    {
        if (!CurrentMap.Traps.TryGetValue(position, out var trap) || !trap.Armed) return;
        trap.Revealed = true;
        trap.Armed = trap.Kind == TrapKind.Alarm;
        var element = CurrentMap.Elements[position.X, position.Y];
        switch (trap.Kind)
        {
            case TrapKind.FlameJet:
                Player.Hp -= 7; element.Fire = Math.Max(element.Fire, 7); element.Smoke = Math.Max(element.Smoke, 8);
                AddMessage("A hidden nozzle erupts—7 fire damage!"); MakeNoise(10, position); break;
            case TrapKind.ShockPlate:
                Player.Hp -= element.Wet > 0 ? 11 : 6; element.Electric = 3;
                ElectricRecoveryTurns = Math.Max(ElectricRecoveryTurns, 3);
                AddMessage("Blue current snaps through the pressure plate! You can act normally and have time to leave the charge."); MakeNoise(7, position); break;
            case TrapKind.PoisonVent:
                element.Poison = 12; element.Smoke = 10; Player.Hp -= 3;
                AddMessage("A poison vent sighs open beneath your boot."); MakeNoise(4, position); break;
            case TrapKind.Collapse:
                Player.Hp -= 8; element.Smoke = 8;
                foreach (var delta in CardinalDirections)
                { var p = position + delta; if (CurrentMap.Contains(p) && CurrentMap[p].Walkable && p != Player.Position) CurrentMap.Set(p, WorldBuilder.Rubble); }
                AddMessage("The ceiling collapses in a crushing roar—8 damage!"); MakeNoise(12, position); break;
            default:
                foreach (var monster in Monsters.Where(monster => monster.IsAlive)) { monster.LastKnownPlayer = Player.Position; monster.AlertTurns = Math.Max(monster.AlertTurns, 8); }
                AddMessage("An alarm rune screams; the whole floor knows where you are."); MakeNoise(18, position); break;
        }
    }

    private void OperateMachine(MachineFeature machine)
    {
        machine.Active = !machine.Active;
        var p = machine.Position;
        switch (machine.Kind)
        {
            case MachineKind.Brazier:
                if (machine.Active) { CurrentMap.ExtinguishedLights.Remove(p); CurrentMap.Elements[p.X, p.Y].Fire = 30; }
                else { CurrentMap.ExtinguishedLights.Add(p); CurrentMap.Elements[p.X, p.Y].Fire = 0; }
                CommitTurn(machine.Active ? "The brazier catches, throwing predatory shadows." : "You smother the brazier. Darkness pours into the room."); break;
            case MachineKind.Floodgate:
                for (var y = Math.Max(0, p.Y - 4); y <= Math.Min(CurrentMap.Height - 1, p.Y + 4); y++)
                for (var x = Math.Max(0, p.X - 6); x <= Math.Min(CurrentMap.Width - 1, p.X + 6); x++)
                    if (CurrentMap[new Point(x, y)].Walkable) CurrentMap.Elements[x, y].Wet = machine.Active ? 80 : 0;
                CommitTurn(machine.Active ? "The floodgate opens; black water spreads across the chamber." : "The floodgate grinds shut and hidden drains begin to drink."); break;
            case MachineKind.Prism:
                if (machine.Active) CurrentMap.ExtinguishedLights.Remove(p); else CurrentMap.ExtinguishedLights.Add(p);
                for (var x = Math.Max(0, p.X - 7); x <= Math.Min(CurrentMap.Width - 1, p.X + 7); x++) CurrentMap.Elements[x, p.Y].Electric = machine.Active ? 3 : 0;
                CommitTurn(machine.Active ? "The prism wakes, splitting a charged rainbow down the hall." : "The prism folds into dull glass."); break;
            default:
                if (machine.Active) CurrentMap.ExtinguishedLights.Remove(p); else CurrentMap.ExtinguishedLights.Add(p);
                if (machine.Active)
                    foreach (var door in CurrentMap.Doors.Values.Where(door => door.State == DoorState.Locked && door.Position.DistanceSquared(p) <= 144))
                    { door.State = DoorState.Open; CurrentMap.Set(door.Position, WorldBuilder.OpenDoor); }
                CommitTurn(machine.Active ? "The generator wakes. Nearby locks release with iron sighs." : "The generator winds down into darkness."); break;
        }
        RecordQuestProgress(QuestObjectiveKind.OperateMachine, machine: machine.Kind);
    }

    private void AdvanceEnvironment(double now)
    {
        if (CurrentMap.IsTown) { RefreshDetectionState(); return; }
        var ignite = new List<Point>();
        var electrify = new List<Point>();
        var reactionBudget = 18;
        for (var y = 0; y < CurrentMap.Height; y++)
        for (var x = 0; x < CurrentMap.Width; x++)
        {
            var p = new Point(x, y); var cell = CurrentMap.Elements[x, y];
            if (!cell.Active) continue;
            if (cell.Wet > 0 && cell.Fire > 0)
            {
                if (reactionBudget-- > 0) Effects.Add(new FlourishEffect { Position = p, StartedAt = now, Seed = x * 971 + y * 313,
                    Kind = FlourishKind.Steam, Color = new Rgb(220, 225, 225), SecondaryColor = new Rgb(255, 150, 75), Duration = 0.9, Radius = 3 });
                cell.Fire = 0; cell.Smoke = Math.Max(cell.Smoke, 4);
            }
            if (cell.Frost > 0 && cell.Wet > 0)
            {
                if ((_turns + x + y) % 4 == 0 && reactionBudget-- > 0)
                    Effects.Add(new FlourishEffect { Position = p, StartedAt = now, Seed = x * 479 + y * 733,
                        Kind = FlourishKind.FrostCrack, Color = new Rgb(215, 250, 255), SecondaryColor = new Rgb(55, 145, 245), Duration = 0.65, Radius = 3 });
                cell.Frost = Math.Max(cell.Frost, 8);
            }
            if (cell.Fire > 0)
            {
                cell.Smoke = Math.Max(cell.Smoke, 5);
                if (cell.Oil > 0) { cell.Fire = Math.Max(cell.Fire, 8); cell.Oil = Math.Max(0, cell.Oil - 4); }
                if ((_turns + x * 7 + y * 11) % 5 == 0)
                    foreach (var delta in CardinalDirections)
                    { var q = p + delta; if (CurrentMap.Contains(q) && CurrentMap.Elements[q.X, q.Y].Oil > 0) ignite.Add(q); }
            }
            if (cell.Electric > 0 && cell.Wet > 0)
            {
                if ((_turns + x * 3 + y) % 5 == 0 && reactionBudget-- > 0)
                    Effects.Add(new FlourishEffect { Position = p, StartedAt = now, Seed = x * 193 + y * 991,
                        Kind = FlourishKind.ElectricBranch, Color = new Rgb(135, 220, 255), SecondaryColor = Rgb.White, Duration = 0.48, Radius = 4 });
                foreach (var delta in CardinalDirections)
                { var q = p + delta; if (CurrentMap.Contains(q) && CurrentMap.Elements[q.X, q.Y].Wet > 0) electrify.Add(q); }
            }
            if (cell.Poison > 0 && cell.Smoke > 0 && (_turns + x * 5 + y * 7) % 11 == 0 && reactionBudget-- > 0)
                Effects.Add(new FlourishEffect { Position = p, StartedAt = now, Seed = x * 811 + y * 151,
                    Kind = FlourishKind.PoisonCurl, Color = new Rgb(105, 225, 75), SecondaryColor = new Rgb(65, 105, 50), Duration = 1.2, Radius = 3 });
            if (cell.Fire > 0 && cell.Web > 0 && reactionBudget-- > 0)
                Effects.Add(new FlourishEffect { Position = p, StartedAt = now, Seed = x * 227 + y * 601,
                    Kind = FlourishKind.EmberCollapse, Color = new Rgb(255, 215, 115), SecondaryColor = new Rgb(255, 75, 18), Duration = 0.8, Radius = 3 });
            ApplyElementDamageAt(p, cell, now);
            if (cell.Wet > 0) cell.Wet--;
            if (cell.Fire > 0) cell.Fire--;
            if (cell.Frost > 0) cell.Frost--;
            if (cell.Electric > 0) cell.Electric--;
            if (cell.Poison > 0) cell.Poison--;
            if (cell.Smoke > 0) cell.Smoke--;
            if (cell.Web > 0 && _turns % 3 == 0) cell.Web--;
        }
        foreach (var p in ignite.Distinct()) CurrentMap.Elements[p.X, p.Y].Fire = Math.Max(CurrentMap.Elements[p.X, p.Y].Fire, 7);
        foreach (var p in electrify.Distinct()) CurrentMap.Elements[p.X, p.Y].Electric = Math.Max(CurrentMap.Elements[p.X, p.Y].Electric, 2);
        RefreshDetectionState();
    }

    private void ApplyElementDamageAt(Point p, ElementCell cell, double now)
    {
        if (Player.Position == p)
        {
            // Electricity lands in discrete pulses rather than denying input or
            // damaging on every simulation step. Two recovery turns give the
            // player a reliable opportunity to leave conductive water.
            var electric = 0;
            if (cell.Electric > 0 && ElectricRecoveryTurns <= 0)
            {
                electric = cell.Wet > 0 ? 6 : 4;
                ElectricRecoveryTurns = 3;
            }
            var damage = (cell.Fire > 0 ? 3 : 0) + electric + (cell.Poison > 0 ? 2 : 0);
            if (damage > 0)
            {
                Player.Hp -= damage;
                Effects.Add(new FlourishEffect { Position = p, StartedAt = now, Seed = _turns * 997 + p.X,
                    Kind = FlourishKind.Impact, Color = cell.Electric > 0 ? new Rgb(135, 220, 255) : cell.Fire > 0 ? new Rgb(255, 145, 55) : new Rgb(125, 235, 82),
                    SecondaryColor = new Rgb(220, 45, 35), Duration = 0.28 });
                AddMessage($"The {ElementDescription(cell)} deals {damage} damage.");
            }
            if (cell.Web > 0 && ControlGraceTurns <= 0) Player.WebbedTurns = Math.Max(Player.WebbedTurns, 1);
        }
        foreach (var monster in Monsters.Where(monster => monster.IsAlive && monster.Position == p).ToList())
        {
            var fire = cell.Fire > 0 && !monster.Name.Contains("salamander") && !monster.Name.Contains("demon") ? 3 : 0;
            var electric = cell.Electric > 0 ? (cell.Wet > 0 ? 7 : 3) : 0;
            var poison = cell.Poison > 0 && monster.Faction is not MonsterFaction.Undead and not MonsterFaction.Construct ? 2 : 0;
            monster.Hp -= fire + electric + poison;
            if (!monster.IsAlive) { AddMessage($"The {monster.Name} is claimed by the {ElementDescription(cell)}."); CheckKilled(monster); }
        }
    }

    private void ApplySpellElements(SpellKind spell, Point center, int radius)
    {
        for (var y = -radius; y <= radius; y++)
        for (var x = -radius; x <= radius; x++)
        {
            var p = center + new Point(x, y);
            if (!CurrentMap.Contains(p) || Chebyshev(center, p) > radius) continue;
            var cell = CurrentMap.Elements[p.X, p.Y];
            if (spell is SpellKind.Fireball or SpellKind.Meteor or SpellKind.FlameCleave)
            {
                if (cell.Web > 0) Effects.Add(new FlourishEffect { Position = p, StartedAt = _visualNow, Seed = p.X * 443 + p.Y * 919,
                    Kind = FlourishKind.EmberCollapse, Color = new Rgb(255, 225, 135), SecondaryColor = new Rgb(255, 78, 18), Duration = 0.75, Radius = 3 });
                cell.Fire = Math.Max(cell.Fire, 8); cell.Frost = 0;
                if ((p.X * 7 + p.Y * 13 + _turns) % 3 == 0) MarkSurface(p, SurfaceMarkKind.Scorch, '·', new Rgb(78, 42, 32));
            }
            if (spell is SpellKind.FrostLance or SpellKind.IceStorm)
            {
                cell.Frost = Math.Max(cell.Frost, 10); cell.Fire = 0;
                if ((p.X * 11 + p.Y * 5 + _turns) % 3 == 0) MarkSurface(p, SurfaceMarkKind.Frost, '◇', new Rgb(130, 205, 235));
            }
            if (spell == SpellKind.PoisonCloud) { cell.Poison = Math.Max(cell.Poison, 12); cell.Smoke = Math.Max(cell.Smoke, 12); }
            if (spell is SpellKind.SolarRay or SpellKind.Smite or SpellKind.LuminousNova) { cell.Poison = 0; cell.Smoke = 0; cell.Web = 0; }
            if (spell == SpellKind.ChainLightning) cell.Electric = Math.Max(cell.Electric, cell.Wet > 0 ? 4 : 2);
        }
    }

    public static string ElementDescription(ElementCell cell)
    {
        var parts = new List<string>();
        if (cell.Fire > 0) parts.Add("fire");
        if (cell.Electric > 0) parts.Add("electric current");
        if (cell.Poison > 0) parts.Add("poison");
        if (cell.Frost > 0) parts.Add("frost");
        if (cell.Wet > 0) parts.Add("water");
        if (cell.Oil > 0) parts.Add("oil");
        if (cell.Smoke > 0) parts.Add("smoke");
        if (cell.Web > 0) parts.Add("webbing");
        return parts.Count == 0 ? "clear ground" : string.Join(", ", parts);
    }

    public static string TrapName(TrapKind kind) => kind switch
    {
        TrapKind.FlameJet => "flame jet", TrapKind.ShockPlate => "shock plate", TrapKind.PoisonVent => "poison vent",
        TrapKind.Collapse => "collapse trigger", _ => "alarm rune"
    };

    public char? EnvironmentGlyphAt(Point p, out Rgb color)
    {
        if (!CurrentMap.Contains(p)) { color = Rgb.White; return null; }
        var cell = CurrentMap.Elements[p.X, p.Y];
        if (cell.Fire > 0) { color = cell.Oil > 0 ? new Rgb(255, 72, 8) : new Rgb(255, 142, 35); return (_turns + p.X + p.Y) % 2 == 0 ? '▲' : '♨'; }
        if (cell.Electric > 0) { color = new Rgb(105, 205, 255); return 'ϟ'; }
        if (cell.Frost > 0) { color = new Rgb(165, 225, 255); return '◇'; }
        if (cell.Poison > 0) { color = new Rgb(105, 215, 72); return '░'; }
        if (cell.Smoke > 0) { color = new Rgb(92, 86, 94); return '▒'; }
        if (cell.Web > 0) { color = new Rgb(190, 198, 205); return '⌁'; }
        if (cell.Oil > 0) { color = new Rgb(85, 68, 48); return '≈'; }
        if (cell.Wet > 0) { color = new Rgb(70, 145, 195); return '~'; }
        color = Rgb.White; return null;
    }
}
