using System.Text;

namespace LumenRogue;

public sealed class Renderer
{
    private int _lastWidth;
    private int _lastHeight;
    private double _displayedHp = -1;
    private double _displayedMana = -1;
    private double _lastUiNow;
    private string _lastMessage = "";
    private double _messageChangedAt;

    public void Draw(Game game, double now)
    {
        int width;
        int height;
        try
        {
            width = Math.Max(1, Console.WindowWidth);
            height = Math.Max(1, Console.WindowHeight);
        }
        catch
        {
            width = 100;
            height = 35;
        }

        if (width < 60 || height < 20)
        {
            Console.Write($"\x1b[H\x1b[2J\x1b[38;2;255;180;80mLUMENROGUE\x1b[0m needs a terminal at least 60×20. Current: {width}×{height}");
            return;
        }

        var mapHeight = height - 5;
        var camera = CameraOffset(game, now);
        var viewportX = Math.Clamp(game.Player.Position.X - width / 2 + camera.X, 0, Math.Max(0, game.CurrentMap.Width - width));
        var viewportY = Math.Clamp(game.Player.Position.Y - mapHeight / 2 + camera.Y, 0, Math.Max(0, game.CurrentMap.Height - mapHeight));
        var drawWidth = Math.Min(width, game.CurrentMap.Width);
        var drawHeight = Math.Min(mapHeight, game.CurrentMap.Height);

        var sb = new StringBuilder(width * height * 5);
        sb.Append("\x1b[H");
        AppendTopUi(sb, game, width, now);
        sb.Append('\n');
        var lights = game.CurrentMap.Lights.Where(light => !game.CurrentMap.ExtinguishedLights.Contains(light.Position)).Concat(game.GetDynamicLights(now)).ToList();
        var shadowCasters = game.DynamicShadowCasters();
        var ambient = GetAmbient(game);
        Rgb? currentFg = null;
        Rgb? currentBg = null;

        for (var sy = 0; sy < mapHeight; sy++)
        {
            for (var sx = 0; sx < width; sx++)
            {
                if (sx >= drawWidth || sy >= drawHeight)
                {
                    AppendCell(sb, ' ', Rgb.Black, Rgb.Black, ref currentFg, ref currentBg);
                    continue;
                }

                var p = new Point(viewportX + sx, viewportY + sy);
                var sightDx = p.X - game.Player.Position.X;
                var sightDy = (p.Y - game.Player.Position.Y) * 1.65;
                var sightDistance = Math.Sqrt(sightDx * sightDx + sightDy * sightDy);
                var sightStrength = VisionFalloff(game, sightDistance);
                var visible = sightStrength > 0 && game.HasClearSight(game.Player.Position, p);
                if (visible && sightStrength > 0.035) game.CurrentMap.Explored[p.X, p.Y] = true;
                if (!game.CurrentMap.Explored[p.X, p.Y])
                {
                    AppendCell(sb, ' ', Rgb.Black, Rgb.Black, ref currentFg, ref currentBg);
                    continue;
                }

                var tile = game.CurrentMap[p];
                var glyph = tile.Glyph;
                var fg = tile.Foreground;
                var bg = tile.Background;

                if (visible)
                {
                    if (game.CurrentMap.SurfaceMarks.TryGetValue(p, out var mark))
                    { glyph = mark.Glyph; fg = mark.Color; }
                    var ambientGlyph = AmbientGlyphAt(game, p, tile, now, out var ambientColor);
                    if (ambientGlyph is not null) { glyph = ambientGlyph.Value; fg = ambientColor; }
                    var environmentGlyph = game.EnvironmentGlyphAt(p, out var environmentColor);
                    if (environmentGlyph is not null) { glyph = environmentGlyph.Value; fg = environmentColor; }
                    if (game.CurrentMap.Traps.TryGetValue(p, out var trap) && trap.Revealed)
                    { glyph = '^'; fg = trap.Armed ? new Rgb(255, 95, 62) : new Rgb(115, 105, 102); }
                    var groundItem = game.CurrentMap.GroundItems.FirstOrDefault(i => i.Position == p);
                    if (groundItem is not null) { glyph = groundItem.Item.Glyph; fg = groundItem.Item.Color; }
                    if (game.CurrentMap.WallTorches.ContainsKey(p) && game.CurrentMap.ExtinguishedLights.Contains(p))
                        fg = new Rgb(90, 82, 75);
                    var resident = game.CurrentMap.Npcs.FirstOrDefault(npc => npc.Position == p);
                    if (resident is not null)
                    {
                        glyph = resident.Glyph;
                        fg = resident.Color;
                        var questStatus = game.QuestForNpc(resident)?.Status;
                        if (questStatus is QuestStatus.Available or QuestStatus.ReadyToTurnIn)
                        {
                            var pulse = 0.18 + (0.5 + 0.5 * Math.Sin(now * 4.2 + p.X * 0.3)) * 0.3;
                            var signal = questStatus == QuestStatus.ReadyToTurnIn ? new Rgb(120, 245, 145) : new Rgb(255, 210, 82);
                            fg = Rgb.Lerp(fg, signal, pulse);
                        }
                    }
                    var monster = sightStrength > 0.07 ? game.Monsters.FirstOrDefault(m => m.IsAlive && m.Position == p) : null;
                    if (monster is not null)
                    {
                        glyph = monster.Name.Contains("mimic") && monster.AlertTurns <= 0 ? '*' :
                            monster.Name.Contains("gargoyle") && monster.AlertTurns <= 0 && game.GameplayLightAt(p) < 0.3 ? '▓' : monster.Glyph;
                        fg = glyph == monster.Glyph ? monster.Color : tile.Foreground;
                        AnimateMonster(monster, now, ref glyph, ref fg);
                    }
                    if (game.Player.Position == p) { glyph = game.Player.Glyph; fg = game.Player.Color; }
                    var effectGlyph = game.EffectGlyphAt(p, now, out var effectColor);
                    if (effectGlyph is not null) { glyph = effectGlyph.Value; fg = effectColor; }
                    if (game.CursorPosition == p)
                    {
                        bg = game.Mode == GameMode.Targeting ? new Rgb(82, 22, 18) : new Rgb(24, 62, 82);
                        fg = Rgb.White;
                    }

                    var cellAmbient = ambient;
                    if (game.CurrentMap.IsOutdoor && game.OutdoorDaylight > 0.01)
                    {
                        var sunVisibility = DirectionalSunVisibility(game.CurrentMap, p, shadowCasters, game.WorldMinutes);
                        cellAmbient = ambient.Scale(0.55 + sunVisibility * 0.45);
                    }
                    var illumination = CalculateLight(game.CurrentMap, p, lights, cellAmbient, shadowCasters, now);
                    fg = ApplyLight(fg, illumination);
                    bg = ApplyLight(bg, illumination.Scale(0.72));
                    ApplySurfaceResponse(game.CurrentMap, p, tile, illumination, now, ref glyph, ref fg, ref bg);

                    // Fade the entire composited cell, including creatures and FX,
                    // through a wide penumbra instead of clipping at one radius.
                    // The small floor matches explored memory and hides the seam.
                    var perception = 0.025 + sightStrength * 0.975;
                    fg = fg.Scale(perception);
                    bg = bg.Scale(perception);
                    ApplyArrivalReveal(game, p, now, ref fg, ref bg);
                    ApplyDamageVignette(game, p, now, ref fg, ref bg);
                }
                else
                {
                    fg = fg.Scale(0.025);
                    bg = bg.Scale(0.018);
                }
                AppendCell(sb, glyph, fg, bg, ref currentFg, ref currentBg);
            }
            if (sy < mapHeight - 1) sb.Append('\n');
        }

        sb.Append("\x1b[0m");
        AppendBottomUi(sb, game, width, now);

        if (game.Mode == GameMode.Help) AppendHelpOverlay(sb, width, height);
        else if (game.Mode == GameMode.Shopping) AppendShopOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.CharacterCreation) AppendCreationOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.Inventory) AppendInventoryOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.Stats) AppendStatsOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.Spellbook) AppendSpellbookOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.Map) AppendMapOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.Dialogue) AppendDialogueOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.QuestLog) AppendQuestLogOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.SkillTree) AppendSkillTreeOverlay(sb, game, width, height, now);
        else if (game.Mode == GameMode.FastTravel) AppendFastTravelOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.Crafting) AppendCraftingOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.Lore) AppendLoreOverlay(sb, game, width, height);
        else if (game.Mode == GameMode.QuitConfirm) AppendQuitConfirmation(sb, game, width, height);
        else if (game.Mode == GameMode.GameOver) AppendGameOver(sb, width, height);

        if (_lastWidth != width || _lastHeight != height)
        {
            sb.Insert(0, "\x1b[2J");
            _lastWidth = width;
            _lastHeight = height;
        }
        // DEC synchronized-output mode asks capable terminals (including modern
        // Windows Terminal) to hold this complete ANSI frame and present it at once.
        // Unsupported terminals safely ignore the private-mode sequence.
        Console.Write("\x1b[?2026h" + sb + "\x1b[?2026l");
    }

    public static double VisionFalloff(double distance)
        => VisionFalloff(distance, 10.5, 27.0);

    public static double VisionFalloff(Game game, double distance)
    {
        if (!game.CurrentMap.IsOutdoor) return VisionFalloff(distance);
        var solarReach = Math.Pow(game.OutdoorDaylight, 0.62);
        var fullClarityRadius = 9.0 + solarReach * 10.0;
        var outerVisionRadius = 24.0 + solarReach * 26.0;
        return VisionFalloff(distance, fullClarityRadius, outerVisionRadius);
    }

    private static double VisionFalloff(double distance, double fullClarityRadius, double outerVisionRadius)
    {
        if (distance <= fullClarityRadius) return 1;
        if (distance >= outerVisionRadius) return 0;
        var t = (distance - fullClarityRadius) / (outerVisionRadius - fullClarityRadius);
        // Inverted smootherstep: zero first and second derivatives at both ends,
        // avoiding both a visible shoulder and a hard outer contour.
        var smooth = t * t * t * (t * (t * 6 - 15) + 10);
        return 1 - smooth;
    }

    private static Rgb GetAmbient(Game game)
    {
        var map = game.CurrentMap;
        if (!map.IsOutdoor) return new Rgb(7, 8, 14);
        var sun = game.OutdoorDaylight;
        var night = new Rgb(7, 12, 29);
        var day = new Rgb(190, 202, 176);
        var dawn = new Rgb(166, 86, 52);
        var baseLight = Rgb.Lerp(night, day, Math.Pow(sun, 0.68));
        var sunrise = Math.Max(0, 1 - Math.Abs(game.WorldMinutes - 360) / 105.0);
        var sunset = Math.Max(0, 1 - Math.Abs(game.WorldMinutes - 1080) / 115.0);
        var horizon = Math.Max(sunrise, sunset);
        return new Rgb(
            Math.Min(225, baseLight.R + (int)(dawn.R * horizon * 0.32)),
            Math.Min(215, baseLight.G + (int)(dawn.G * horizon * 0.15)),
            Math.Min(195, baseLight.B + (int)(dawn.B * horizon * 0.05)));
    }

    private static Rgb CalculateLight(Map map, Point p, List<LightSource> lights, Rgb ambient, IReadOnlySet<Point> shadowCasters, double now)
    {
        double r = ambient.R / 255.0;
        double g = ambient.G / 255.0;
        double b = ambient.B / 255.0;

        foreach (var light in lights)
        {
            var dx = p.X - light.Position.X;
            var dy = (p.Y - light.Position.Y) * 1.8;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > light.Radius) continue;
            var (visibility, fringe) = SampleVisibility(map, light.Position, p, light.Softness, shadowCasters);
            if (visibility <= 0 && fringe <= 0) continue;

            var noise = 1.0;
            if (light.Flicker > 0)
            {
                var flame = Math.Sin(now * 17.3 + light.Phase) * 0.55 + Math.Sin(now * 31.7 + light.Phase * 2.1) * 0.3 + Math.Sin(now * 7.1) * 0.15;
                noise += flame * light.Flicker;
            }
            var falloff = Math.Pow(1 - distance / light.Radius, 1.72) * light.Intensity * noise;
            var materialBounce = map[p].Reflectivity * light.Bounce * (1 - visibility) * falloff;
            var diffracted = fringe * light.Diffraction * falloff;
            r += light.Color.R / 255.0 * (falloff * visibility + materialBounce + diffracted * 1.35);
            g += light.Color.G / 255.0 * (falloff * visibility + materialBounce + diffracted * 0.72);
            b += light.Color.B / 255.0 * (falloff * visibility + materialBounce + diffracted * 1.12);
        }
        return new Rgb((int)(Math.Min(1.8, r) * 255), (int)(Math.Min(1.8, g) * 255), (int)(Math.Min(1.8, b) * 255));
    }

    private static (double Visibility, double Fringe) SampleVisibility(Map map, Point source, Point target, double softness, IReadOnlySet<Point> shadowCasters)
    {
        if (source == target) return (1, 0);
        var offsets = new (double X, double Y)[]
        {
            (0, 0), (-softness, 0), (softness, 0), (0, -softness), (0, softness)
        };
        var passed = 0;
        foreach (var offset in offsets)
            if (TraceSubCellRay(map, source.X + 0.5 + offset.X, source.Y + 0.5 + offset.Y,
                    target.X + 0.5 - offset.X * 0.4, target.Y + 0.5 - offset.Y * 0.4, shadowCasters)) passed++;
        var visibility = passed / (double)offsets.Length;
        var fringe = 4 * visibility * (1 - visibility);
        if (visibility == 0)
        {
            var neighbors = new[] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1),
                new Point(1, 1), new Point(-1, 1), new Point(1, -1), new Point(-1, -1) };
            var edgePaths = neighbors.Count(delta =>
            {
                var neighbor = target + delta;
                return map.Contains(neighbor) && map[neighbor].Transparent && map.HasLineOfSight(source, neighbor);
            });
            if (edgePaths > 0) fringe = Math.Min(0.42, edgePaths * 0.075);
        }
        return (visibility, fringe);
    }

    private static bool TraceSubCellRay(Map map, double sx, double sy, double tx, double ty, IReadOnlySet<Point> shadowCasters)
    {
        var dx = tx - sx;
        var dy = ty - sy;
        var sourceCell = new Point((int)Math.Floor(sx), (int)Math.Floor(sy));
        var targetCell = new Point((int)Math.Floor(tx), (int)Math.Floor(ty));
        var steps = Math.Max(2, (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)) * 2.5));
        for (var i = 1; i < steps; i++)
        {
            var t = i / (double)steps;
            var p = new Point((int)Math.Floor(sx + dx * t), (int)Math.Floor(sy + dy * t));
            if (!map.Contains(p)) return false;
            if (p != sourceCell && p != targetCell &&
                (Game.TileCastsLightShadow(map[p]) || shadowCasters.Contains(p) || map.Elements[p.X, p.Y].Smoke > 0)) return false;
        }
        return true;
    }

    private static double DirectionalSunVisibility(Map map, Point target, IReadOnlySet<Point> shadowCasters, int worldMinutes)
    {
        var dayProgress = Math.Clamp((worldMinutes - 360) / 720.0, 0, 1);
        var towardSunX = Math.Cos(dayProgress * Math.PI);
        var towardSunY = -0.58 - Math.Sin(dayProgress * Math.PI) * 0.34;
        var clearSamples = 0;
        foreach (var offset in new[] { -0.42, 0.0, 0.42 })
        {
            var clear = true;
            for (var step = 1; step <= 14; step++)
            {
                var x = target.X + towardSunX * step - towardSunY * offset;
                var y = target.Y + towardSunY * step + towardSunX * offset;
                var point = new Point((int)Math.Round(x), (int)Math.Round(y));
                if (!map.Contains(point)) break;
                if (Game.TileCastsLightShadow(map[point]) || shadowCasters.Contains(point)) { clear = false; break; }
            }
            if (clear) clearSamples++;
        }
        return clearSamples / 3.0;
    }

    private static Point CameraOffset(Game game, double now)
    {
        double strength = 0;
        var seed = 0;
        foreach (var explosion in game.Effects.OfType<ExplosionEffect>())
        {
            var t = Math.Clamp((now - explosion.StartedAt) / explosion.Duration, 0, 1);
            if (t < 0.42 && 1 - t * 2.1 > strength) { strength = 1 - t * 2.1; seed = explosion.Seed; }
        }
        if (strength < 0.34) return new Point(0, 0);
        var frame = (int)(now * 32);
        return new Point(VisualHash(frame, seed, 17) % 3 - 1, VisualHash(seed, frame, 43) % 3 - 1);
    }

    private static char? AmbientGlyphAt(Game game, Point p, Tile tile, double now, out Rgb color)
    {
        color = Rgb.White;
        var frame = (int)(now * 6);
        var slowFrame = (int)(now * 2.5);
        var floor = tile.Walkable;

        // Creatures disturb the air around themselves: myconids shed spores,
        // infernal bodies cast embers, and resting gargoyles lose stone dust.
        foreach (var monster in game.Monsters.Where(monster => monster.IsAlive && monster.Position.DistanceSquared(p) <= 18))
        {
            var movingHash = VisualHash(p.X + frame, p.Y - frame, monster.Position.X * 31 + monster.Position.Y);
            if (monster.Name.Contains("myconid") && movingHash % 71 == 0)
            { color = new Rgb(85, 215, 178); return '∙'; }
            if (monster.Faction == MonsterFaction.Infernal && movingHash % 59 == 0)
            { color = new Rgb(255, 105, 28); return frame % 2 == 0 ? '˙' : '·'; }
            if (monster.Name.Contains("gargoyle") && movingHash % 103 == 0)
            { color = new Rgb(135, 132, 140); return '˙'; }
        }

        var secretNearby = game.CurrentMap.Doors.Values.FirstOrDefault(door => door.State == DoorState.Secret &&
            door.Position.DistanceSquared(game.Player.Position) <= 18 && door.Position.DistanceSquared(p) <= 2);
        if (secretNearby is not null && VisualHash(p.X, p.Y, slowFrame) % 13 == 0)
        { color = new Rgb(138, 122, 105); return p == secretNearby.Position ? '·' : '˙'; }

        switch (game.CurrentMap.Biome)
        {
            case BiomeKind.Emberfall:
                // Emberfall is deliberately still. Its lamps animate through the
                // lighting model, but unexplained free-floating particles do not
                // compete with interactable objects or hazards.
                break;
            case BiomeKind.Wilds:
                // Outdoor motion belongs to identifiable materials: grass leans
                // with the wind and exposed water ripples. Nothing floats free.
                if (tile.Name.Contains("wind-bent grass") && VisualHash(p.X, p.Y, slowFrame) % 7 == 0)
                { color = new Rgb(112, 151, 76); return slowFrame % 2 == 0 ? ',' : '`'; }
                if ((tile.Name.Contains("water") || tile.Name.Contains("river")) && VisualHash(p.X, p.Y, slowFrame) % 19 == 0)
                { color = new Rgb(82, 157, 196); return slowFrame % 2 == 0 ? '~' : '≈'; }
                break;
            case BiomeKind.AshenVaults:
                if (floor && VisualHash(p.X + frame / 2, p.Y - frame, 211) % 83 == 0)
                { color = new Rgb(142, 125, 116); return frame % 2 == 0 ? '·' : '˙'; }
                break;
            case BiomeKind.DrownedReliquary:
                var wet = game.CurrentMap.Elements[p.X, p.Y].Wet > 0 || tile.Name.Contains("water", StringComparison.OrdinalIgnoreCase);
                if (wet && VisualHash(p.X, p.Y, slowFrame) % 47 == 0)
                { color = new Rgb(92, 183, 225); return slowFrame % 2 == 0 ? '°' : '~'; }
                if (floor && VisualHash(p.X, p.Y + frame, 307) % 137 == 0)
                { color = new Rgb(105, 158, 190); return '˙'; }
                break;
            case BiomeKind.PrismaticFoundry:
                if ((tile.Walkable || tile.Name.Contains("resonant crystal") || tile.Name.Contains("light prism")) &&
                    tile.Reflectivity > 0.25 && VisualHash(p.X, p.Y, slowFrame) % 79 == 0)
                { color = (slowFrame % 3) switch { 0 => new Rgb(100, 235, 255), 1 => new Rgb(255, 100, 225), _ => new Rgb(225, 245, 145) }; return '✧'; }
                break;
            case BiomeKind.LightlessCourt:
                if (floor && VisualHash(p.X - slowFrame, p.Y + frame / 2, 419) % 101 == 0)
                { color = new Rgb(100, 88, 135); return frame % 2 == 0 ? '∙' : '·'; }
                break;
            case BiomeKind.CrownOfCinders:
                if (floor && VisualHash(p.X + slowFrame, p.Y - frame, 557) % 67 == 0)
                { color = new Rgb(245, 92, 28); return frame % 3 == 0 ? '⌇' : '˙'; }
                break;
        }

        if ((tile.Name.Contains("rubble") || tile.Name.Contains("cracked")) && VisualHash(p.X, p.Y + frame, 659) % 211 == 0)
        { color = new Rgb(112, 100, 91); return '˙'; }
        return null;
    }

    private static void AnimateMonster(Actor monster, double now, ref char glyph, ref Rgb color)
    {
        var phase = now * (monster.Ai == MonsterAi.Lurker ? 2.4 : 4.2) + monster.Position.X * 0.43 + monster.Position.Y * 0.71;
        var pulse = 0.88 + Math.Sin(phase) * 0.1;
        color = color.Scale(pulse);
        if (monster.GlowColor is { } glow) color = Rgb.Lerp(color, glow, 0.08 + Math.Max(0, Math.Sin(phase)) * 0.12);
        if (monster.Name.Contains("slime") && (int)(now * 3 + monster.Position.X) % 4 == 0) glyph = '≈';
        else if (monster.Name.Contains("spider") && (int)(now * 7 + monster.Position.Y) % 5 == 0) glyph = 'x';
        else if (monster.Name.Contains("whisper") && (int)(now * 5 + monster.Position.X) % 6 == 0) glyph = '∿';
        if (monster.AlertTurns > 0 && (int)(now * 8) % 4 == 0) color = Rgb.Lerp(color, Rgb.White, 0.22);
    }

    private static void ApplySurfaceResponse(Map map, Point p, Tile tile, Rgb illumination, double now,
        ref char glyph, ref Rgb fg, ref Rgb bg)
    {
        var cell = map.Elements[p.X, p.Y];
        var shimmer = 0.5 + 0.5 * Math.Sin(now * 4.2 + p.X * 0.61 + p.Y * 1.17);
        if (cell.Wet > 0 || tile.Name.Contains("water", StringComparison.OrdinalIgnoreCase))
        {
            var reflected = new Rgb(illumination.B / 2, illumination.G / 2, illumination.R / 3);
            bg = Rgb.Lerp(bg, reflected, 0.08 + shimmer * 0.12);
            fg = Rgb.Lerp(fg, illumination, shimmer * 0.06);
        }
        var intentionallyReflective = tile.Walkable &&
            (map.Biome == BiomeKind.PrismaticFoundry || tile.Name.Contains("crystal") || tile.Name.Contains("glass") || tile.Name.Contains("altar"));
        if (intentionallyReflective)
        {
            fg = Rgb.Lerp(fg, illumination, Math.Min(0.22, tile.Reflectivity * (0.1 + shimmer * 0.25)));
            if (VisualHash(p.X, p.Y, (int)(now * 7)) % 29 == 0)
                fg = Rgb.Lerp(fg, (p.X + p.Y) % 2 == 0 ? new Rgb(255, 90, 210) : new Rgb(75, 220, 255), 0.28);
        }
        if (cell.Web > 0 && illumination.R + illumination.G + illumination.B > 390 && shimmer > 0.82)
            fg = Rgb.Lerp(fg, Rgb.White, 0.32);
        if (cell.Smoke > 0) glyph = (int)(now * 4 + p.X + p.Y) % 3 == 0 ? '░' : '▒';
        if (cell.Fire > 0) glyph = ((int)(now * 9 + p.X * 2 + p.Y) % 3) switch { 0 => '▲', 1 => '♨', _ => '░' };
    }

    private static void ApplyArrivalReveal(Game game, Point p, double now, ref Rgb fg, ref Rgb bg)
    {
        var arrival = game.Effects.OfType<FlourishEffect>().LastOrDefault(effect => effect.Kind == FlourishKind.StairArrival);
        if (arrival is null) return;
        var t = Math.Clamp((now - arrival.StartedAt) / arrival.Duration, 0, 1);
        var dx = p.X - arrival.Position.X;
        var dy = (p.Y - arrival.Position.Y) * 1.7;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var edge = t * arrival.Radius * 1.8;
        var reveal = Math.Clamp((edge - distance + 2.5) / 3.5, 0.015, 1);
        fg = Rgb.Lerp(arrival.Color.Scale(0.08), fg, reveal);
        bg = bg.Scale(reveal);
    }

    private static void ApplyDamageVignette(Game game, Point p, double now, ref Rgb fg, ref Rgb bg)
    {
        var hit = game.Effects.OfType<FlourishEffect>().LastOrDefault(effect =>
            effect.Kind is FlourishKind.Impact or FlourishKind.CriticalImpact && effect.Position == game.Player.Position);
        if (hit is null) return;
        var t = Math.Clamp((now - hit.StartedAt) / hit.Duration, 0, 1);
        var dx = p.X - game.Player.Position.X;
        var dy = (p.Y - game.Player.Position.Y) * 1.7;
        var edge = Math.Clamp((Math.Sqrt(dx * dx + dy * dy) - 6) / 20, 0, 1) * (1 - t);
        if (edge <= 0) return;
        fg = Rgb.Lerp(fg, new Rgb(Math.Min(255, fg.R + 65), fg.G / 2, fg.B / 2), edge * 0.42);
        bg = Rgb.Lerp(bg, new Rgb(70, 4, 5), edge * 0.5);
    }

    private static int VisualHash(int x, int y, int seed)
    {
        unchecked
        {
            var value = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            value = (value ^ (value >> 13)) * 1274126177;
            return (int)((value ^ (value >> 16)) & 0x7fffffff);
        }
    }

    private static Rgb ApplyLight(Rgb source, Rgb light)
    {
        static int Channel(int sourceChannel, int lightChannel)
        {
            var lit = sourceChannel * (lightChannel / 150.0);
            var bloom = Math.Max(0, lightChannel - 255) * 0.28;
            return Math.Clamp((int)(lit + bloom), 0, 255);
        }
        return new Rgb(Channel(source.R, light.R), Channel(source.G, light.G), Channel(source.B, light.B));
    }

    private static void AppendCell(StringBuilder sb, char glyph, Rgb fg, Rgb bg, ref Rgb? currentFg, ref Rgb? currentBg)
    {
        // Tiny per-frame flicker changes previously produced thousands of distinct
        // SGR commands. Six-bit channel quantization still yields 262,144 colors,
        // while greatly reducing traffic and stabilizing adjacent gradients.
        fg = Quantize(fg);
        bg = Quantize(bg);
        if (currentFg != fg)
        {
            sb.Append("\x1b[38;2;").Append(fg.R).Append(';').Append(fg.G).Append(';').Append(fg.B).Append('m');
            currentFg = fg;
        }
        if (currentBg != bg)
        {
            sb.Append("\x1b[48;2;").Append(bg.R).Append(';').Append(bg.G).Append(';').Append(bg.B).Append('m');
            currentBg = bg;
        }
        sb.Append(glyph);
    }

    private static Rgb Quantize(Rgb color)
    {
        static int Q(int channel) => Math.Clamp(((channel + 2) / 4) * 4, 0, 255);
        return new Rgb(Q(color.R), Q(color.G), Q(color.B));
    }

    private void AppendTopUi(StringBuilder sb, Game game, int width, double now)
    {
        if (_displayedHp < 0) { _displayedHp = game.Player.Hp; _displayedMana = game.Mana; _lastUiNow = now; }
        var dt = Math.Clamp(now - _lastUiNow, 0, 0.2);
        _lastUiNow = now;
        if (game.Player.Hp >= _displayedHp) _displayedHp = game.Player.Hp;
        else _displayedHp = Math.Max(game.Player.Hp, _displayedHp - Math.Max(5, game.Player.MaxHp * 0.42) * dt);
        if (game.Mana >= _displayedMana) _displayedMana = Math.Min(game.Mana, _displayedMana + Math.Max(4, game.MaxMana * 0.8) * dt);
        else _displayedMana = Math.Max(game.Mana, _displayedMana - Math.Max(7, game.MaxMana * 1.4) * dt);
        var clock = game.CurrentMap.IsOutdoor ? $"{game.OutdoorPeriod} {game.WorldTimeLabel}" : $"below · {game.WorldTimeLabel}";
        var title = $" ┤ {game.CurrentMap.Name} ├─ {clock} ─ {game.Race} {game.Class} ─ LV {game.Level} ─ SP {game.SkillPoints} ─ TURN {game.TurnCount} · SEED {game.WorldSeed} ";
        var huntedPulse = game.DetectionState.StartsWith("HUNTED") ? (int)(30 + Math.Max(0, Math.Sin(now * 8)) * 65) : 0;
        sb.Append("\x1b[48;2;12;12;18m\x1b[38;2;").Append(220 + huntedPulse / 3).Append(';').Append(190 - huntedPulse).Append(';').Append(120 - huntedPulse / 2).Append('m')
            .Append(Pad(title, width, game.DetectionState.StartsWith("HUNTED") && (int)(now * 5) % 2 == 0 ? '═' : '─'));

        var hpColor = game.Player.Hp < game.Player.MaxHp / 3 ? "255;70;60" : "110;235;115";
        var reaction = game.PreparedReaction?.ToString().ToUpperInvariant() ?? "NONE";
        var stats = $" HEALTH {LagBar(game.Player.Hp, _displayedHp, game.Player.MaxHp, 14)} {game.Player.Hp}/{game.Player.MaxHp}   MANA {ShimmerBar(game.Mana, _displayedMana, game.MaxMana, 12, now)} {game.Mana}/{game.MaxMana}   {game.DetectionState} · REACT {reaction} · LAMP {(game.LanternLit ? "ON" : "OFF")}";
        sb.Append('\n').Append("\x1b[48;2;7;8;12m\x1b[38;2;").Append(hpColor).Append('m').Append(Pad(stats, width));

        var location = game.IsWilderness ? "REGION WILDERNESS" : game.IsSiteDungeon ? "SITE DUNGEON" : game.CurrentDepth == 0 ? "REGION TOWN" : $"DEPTH {game.CurrentDepth}/{game.DungeonDepthCount}";
        var survival = $" HUNGER {Bar(game.Food, 100, 10)} {game.Food}%   THIRST {Bar(game.Water, 100, 10)} {game.Water}%   XP {Bar(game.Experience, game.ExperienceToLevel, 8)}   {location}";
        sb.Append('\n').Append("\x1b[48;2;7;8;12m\x1b[38;2;120;185;220m").Append(Pad(survival, width));
    }

    private void AppendBottomUi(StringBuilder sb, Game game, int width, double now)
    {
        var cursorText = game.Mode is GameMode.Targeting or GameMode.Inspect ? game.CursorDescription : null;
        var message = game.AwaitingDirection ? " AIMING: choose a direction · Esc cancels " : " " + (cursorText ?? game.LatestMessage);
        if (message != _lastMessage) { _lastMessage = message; _messageChangedAt = now; }
        var notice = Math.Clamp(1 - (now - _messageChangedAt) / 0.65, 0, 1);
        var messageColor = Rgb.Lerp(new Rgb(225, 218, 198), new Rgb(255, 238, 145), notice);
        sb.Append('\n').Append("\x1b[48;2;7;8;12m\x1b[38;2;").Append(messageColor.R).Append(';').Append(messageColor.G).Append(';').Append(messageColor.B).Append('m').Append(Pad(message, width));
        var controls = game.Mode switch
        {
            GameMode.Targeting => " [ARROWS] move target  [TAB] next creature  [ENTER] cast  [ESC] cancel ",
            GameMode.Inspect => " [ARROWS] inspect  [TAB] next creature  [X/ESC] close ",
            GameMode.Map => " [M/ESC] close explored map ",
            GameMode.Dialogue => " [ENTER] respond  [E/ESC] leave conversation ",
            GameMode.QuestLog => " [UP/DOWN] select quest  [TAB/ESC] close journal ",
            GameMode.FastTravel => " [UP/DOWN] destination  [ENTER] travel  [F2/ESC] close ",
            GameMode.Crafting => " [UP/DOWN] recipe  [ENTER] craft  [F3/ESC] close ",
            GameMode.Lore => " [F4/ESC] close lore journal ",
            _ => " [MOVE] arrows/HJKL  [TAB] quests  [E] use  [M] map  [F2] travel  [F3] craft  [F4] lore  [F6] react "
        };
        sb.Append('\n').Append("\x1b[48;2;7;8;12m\x1b[38;2;125;132;145m").Append(Pad(controls, width)).Append("\x1b[0m");
    }

    private static string Pad(string value, int width, char fill = ' ')
    {
        if (value.Length > width) return value[..width];
        return value.PadRight(width, fill);
    }

    private static string Bar(int value, int maximum, int width)
    {
        var filled = maximum <= 0 ? 0 : Math.Clamp((int)Math.Round(value / (double)maximum * width), 0, width);
        return new string('█', filled) + new string('░', width - filled);
    }

    private static string LagBar(int value, double displayed, int maximum, int width)
    {
        var filled = maximum <= 0 ? 0 : Math.Clamp((int)Math.Round(value / (double)maximum * width), 0, width);
        var ghost = maximum <= 0 ? filled : Math.Clamp((int)Math.Round(displayed / maximum * width), filled, width);
        return new string('█', filled) + new string('▓', ghost - filled) + new string('░', width - ghost);
    }

    private static string ShimmerBar(int value, double displayed, int maximum, int width, double now)
    {
        var filled = maximum <= 0 ? 0 : Math.Clamp((int)Math.Round(displayed / maximum * width), 0, width);
        if (filled == 0) return new string('░', width);
        var edge = (int)(now * 6) % 2 == 0 ? '█' : '▓';
        return new string('█', Math.Max(0, filled - 1)) + edge + new string('░', width - filled);
    }

    private static void AppendHelpOverlay(StringBuilder sb, int width, int height)
    {
        if (height < 35 || width < 82)
        {
            AppendPanel(sb,
            [
                " LUMENROGUE — CONTROLS ", "",
                " Move arrows/WASD/HJKL · diagonals Y/U/B/N",
                " F quick spell · Z spellbook · T skills · I pack · C character",
                " M map · F2 fast travel · F3 craft · F4 lore · F5 save · F9 load",
                " F6 cycles Block/Dodge/Ward reactions; enemy attacks telegraph first",
                " Bump into residents or E nearby to talk · G pick up · R rest · Space wait",
                " The March has 3 villages, Castle Valeward, and 4 dungeon sites.",
                " Q/Escape asks to quit", "",
                " Light, noise, smoke, doors, traps, and elemental terrain affect AI.",
                " Outdoors, each turn advances 6 minutes; daylight changes visibility.",
                " Control effects cannot chain; breaking free grants two safe turns.",
                " Press any key to return."
            ], width, height, new Rgb(255, 177, 68));
            return;
        }
        var lines = new[]
        {
            " LUMENROGUE — CONTROLS ",
            "",
            " Movement    Arrow keys, WASD, or HJKL",
            " Diagonals   Y U B N (or Home/PgUp/End/PgDn)",
            " Quick spell F (first learned class spell)",
            " Spellbook   Z; select, aim cursor, Enter casts",
            " Skill tree  T; arrows follow branches, Enter invests a rank",
            " Pack/Gear   I · arrows select · Enter equips/uses · D drops",
            " Pick up     G while standing over an item",
            " Rest        R (six safe turns; heals and restores mana)",
            " Statistics  C",
            " Area map    M (explored terrain and discovered landmarks)",
            " Quest log   Tab; arrows select an accepted quest",
            " Fast travel F2; discovered outdoor landmarks, time and supplies",
            " Crafting    F3 at a settlement workbench; monster materials matter",
            " Lore        F4; discoveries record meaningful world history",
            " Save/Load   F5 quick-save · F9 quick-load",
            " Reaction    F6 cycles Block, Dodge, Ward; read enemy intent first",
            " Wilderness  west = gate; 3 villages, Castle Valeward, 4 dungeon sites",
            " Lantern     O; shutter/uncover your light (one turn)",
            " Inspect     X; move cursor or Tab through creatures",
            " Search      V; reveal nearby secret doors and traps (one turn)",
            " Potion      P (quick use)",
            " Interact    E/Enter (residents, shops, supplies, stairs, machinery)",
            " Wait        Space or .",
            " Quit        Q or Escape",
            "",
            " The world clock advances six minutes per turn; rest advances six turns.",
            " Outdoor daylight changes visibility, detection, and lantern value.",
            " Flames, projectiles, and explosions animate without moving monsters.",
            " Hard control costs at most one action, then grants two safe turns.",
            " Electrified water pulses damage but never removes player input.",
            " Light uses sampled rays, soft penumbrae, material bounce,",
            " diffraction-like corner leakage, and chromatic edge fringes.",
            "",
            " Press any key to return."
        };
        AppendPanel(sb, lines, width, height, new Rgb(255, 177, 68));
    }

    private static void AppendShopOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var shop = game.ActiveShop!;
        var compact = height < 25;
        var lines = new List<string> { $" {shop.Name.ToUpperInvariant()} ", $" Purse: {game.Player.Gold} gold", "" };
        for (var i = 0; i < shop.Items.Count; i++)
        {
            var item = shop.Items[i];
            var marker = i == game.MenuSelection ? "\u0001" : "";
            var pointer = i == game.MenuSelection ? "▶" : " ";
            lines.Add($"{marker} {pointer} {i + 1}. {item.Name,-24} {item.Price,4} gold");
            if (!compact) lines.Add($"    {item.Description}");
        }
        if (compact && shop.Items.Count > 0) lines.Add($" {shop.Items[game.MenuSelection].Description}");
        lines.Add("");
        lines.Add($" ↑↓ select · Enter buy · 1–{shop.Items.Count} quick buy · E/Esc leave");
        AppendPanel(sb, lines.ToArray(), width, height, shop.Color);
    }

    private static void AppendCreationOverlay(StringBuilder sb, Game game, int width, int height)
    {
        string[] lines = game.CreationStage == 0
            ? [
                " CREATE YOUR WAYFARER — CHOOSE A RACE ", "",
                " 1. Human    balanced; +1 to every attribute",
                " 2. Dwarf    powerful constitution and strength",
                " 3. Elf      exceptional dexterity and intellect",
                " 4. Ashborn  wisdom, magic, and an innately brighter flame",
                "", " Press 1–4"
            ]
            : [
                $" {game.ChosenRace.ToString().ToUpperInvariant()} — CHOOSE A CLASS ", "",
                " 1. Vanguard    durable fighter; fire and stone magic",
                " 2. Arcanist    greatest mana; learns every spell",
                " 3. Ranger      keen sight; frost and lightning",
                " 4. Lightbearer radiant magic; powerful lantern",
                "", " Press 1–4"
            ];
        AppendPanel(sb, lines, width, height, new Rgb(255, 174, 66), compactDialog: true);
    }

    private static void AppendInventoryOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var equipped = game.Inventory.Where(i => i.Equipped).ToDictionary(i => i.Slot!.Value, i => i);
        string Slot(EquipmentSlot slot) => equipped.TryGetValue(slot, out var item) ? item.Name : "—";
        var compact = height < 27 || width < 82;
        static string Fit(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
        var lines = new List<string> { " PACK & EQUIPMENT ",
            $" ATK {game.TotalAttack}   DEF {game.TotalDefense}   SPELL {game.SpellPower}   LIGHT {game.EffectiveLanternRadius:0.0}" };
        lines.Add(compact
            ? $" MAIN {Fit(Slot(EquipmentSlot.MainHand), 15)} · OFF {Fit(Slot(EquipmentSlot.OffHand), 15)}"
            : $" HAND {Slot(EquipmentSlot.MainHand)}  ·  OFF {Slot(EquipmentSlot.OffHand)}");
        lines.Add(compact
            ? $" BODY {Fit(Slot(EquipmentSlot.Body), 15)} · HEAD {Fit(Slot(EquipmentSlot.Head), 15)}"
            : $" BODY {Slot(EquipmentSlot.Body)}  ·  HEAD {Slot(EquipmentSlot.Head)}");
        lines.Add(" ──────────────────────────────────────────────────");
        if (game.Inventory.Count == 0) lines.Add(" Your pack is empty.");
        var visibleCount = compact ? Math.Clamp(height - 16, 3, 5) : Math.Clamp(height - 14, 4, 9);
        var start = Math.Clamp(game.MenuSelection - visibleCount / 2, 0, Math.Max(0, game.Inventory.Count - visibleCount));
        for (var i = start; i < game.Inventory.Count && i < start + visibleCount; i++)
        {
            var item = game.Inventory[i];
            var marker = i == game.MenuSelection ? "\u0001" : "";
            var pointer = i == game.MenuSelection ? "▶" : " ";
            var state = item.Equipped ? "[E]" : item.IsEquippable ? $"[{ShortSlot(item.Slot!.Value)}]" : $"×{item.Quantity}";
            var rarity = item.Rarity == ItemRarity.Common ? "" : $"{item.Rarity} ";
            lines.Add(compact
                ? $"{marker} {pointer} {RarityMark(item.Rarity)} {item.Glyph} {Fit(rarity + item.Name, 28),-28} {state}"
                : $"{marker} {pointer} {RarityMark(item.Rarity)} {item.Glyph} {rarity}{item.Name,-24} {state,-5} {item.BonusText}");
        }
        if (game.Inventory.Count > 0)
        {
            var selected = game.Inventory[Math.Clamp(game.MenuSelection, 0, game.Inventory.Count - 1)];
            lines.Add(" ──────────────────────────────────────────────────");
            lines.Add($" {selected.Description}");
            lines.Add(selected.IsEquippable ? $" {EquipmentComparison(game, selected)}" : " Consumable · Enter to use");
        }
        lines.Add(" ↑↓ select · Enter equip/use · D drop · I/Esc close");
        AppendPanel(sb, lines.ToArray(), width, height, new Rgb(110, 205, 170));
    }

    private static string ShortSlot(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.MainHand => "MAIN", EquipmentSlot.OffHand => "OFF", EquipmentSlot.Head => "HEAD",
        EquipmentSlot.Body => "BODY", EquipmentSlot.Hands => "HANDS", EquipmentSlot.Feet => "FEET",
        EquipmentSlot.Ring => "RING", _ => "AMUL"
    };

    private static char RarityMark(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => '·', ItemRarity.Uncommon => '◆', ItemRarity.Rare => '✦', _ => '★'
    };

    private static string EquipmentComparison(Game game, ItemStack candidate)
    {
        if (candidate.Equipped) return $"Equipped in {candidate.Slot} · Enter to remove";
        var current = game.Inventory.FirstOrDefault(i => i.Equipped && i.Slot == candidate.Slot);
        var attack = candidate.AttackBonus - (current?.AttackBonus ?? 0);
        var defense = candidate.DefenseBonus - (current?.DefenseBonus ?? 0);
        var spell = candidate.SpellBonus - (current?.SpellBonus ?? 0);
        var light = candidate.LightBonus - (current?.LightBonus ?? 0);
        static string Delta(int value) => value >= 0 ? $"+{value}" : value.ToString();
        static string LightDelta(double value) => value >= 0 ? $"+{value:0.0}" : value.ToString("0.0");
        var versus = current is null ? "empty slot" : current.Name;
        return $"vs {versus}: ATK {Delta(attack)}  DEF {Delta(defense)}  SPELL {Delta(spell)}  LIGHT {LightDelta(light)}";
    }

    private static void AppendStatsOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var selectedSlot = Game.EquipmentSlots[Math.Clamp(game.MenuSelection, 0, Game.EquipmentSlots.Length - 1)];
        var selectedItem = game.Inventory.FirstOrDefault(i => i.Equipped && i.Slot == selectedSlot);
        if (height < 27 || width < 82)
        {
            static string Fit(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
            var compactLines = new List<string>
            {
                $" CHARACTER — {game.Race.ToString().ToUpperInvariant()} {game.Class.ToString().ToUpperInvariant()} — LEVEL {game.Level} ",
                $" HP {game.Player.Hp}/{game.Player.MaxHp}  ATK {game.TotalAttack}  DEF {game.TotalDefense}  SPELL {game.SpellPower}  LIGHT {game.EffectiveLanternRadius:0.0}"
            };
            for (var i = 0; i < Game.EquipmentSlots.Length; i += 2)
            {
                string Entry(int index)
                {
                    var slot = Game.EquipmentSlots[index];
                    var item = game.Inventory.FirstOrDefault(entry => entry.Equipped && entry.Slot == slot);
                    return $"{(index == game.MenuSelection ? '▶' : ' ')}[{ShortSlot(slot),-5}] {Fit(item?.Name ?? "—", 13),-13}";
                }
                var selectedPair = game.MenuSelection == i || game.MenuSelection == i + 1;
                compactLines.Add((selectedPair ? "\u0001" : "") + " " + Entry(i) + "  " + Entry(i + 1));
            }
            compactLines.Add(" ──────────────────────────────────────────────────");
            compactLines.Add($" {selectedSlot.ToString().ToUpperInvariant()}: {(selectedItem?.Description ?? "Empty equipment slot.")}");
            compactLines.Add(selectedItem is null ? " Open the pack to equip an item." : $" {selectedItem.Rarity} · {selectedItem.BonusText}");
            compactLines.Add($" STR {game.Strength}  DEX {game.Dexterity}  INT {game.Intellect}  CON {game.Constitution}  WIS {game.Wisdom}  GOLD {game.Player.Gold}");
            compactLines.Add(" ↑↓/←→ select slot · Enter remove · I open pack · C/Esc close");
            AppendPanel(sb, compactLines.ToArray(), width, height, new Rgb(226, 194, 105));
            return;
        }
        var lines = new List<string>
        {
            $" CHARACTER ─ {game.Race.ToString().ToUpperInvariant()} {game.Class.ToString().ToUpperInvariant()} ─ LEVEL {game.Level} ",
            $" HP {game.Player.Hp}/{game.Player.MaxHp}  ATK {game.TotalAttack}  DEF {game.TotalDefense}  SPELL {game.SpellPower}  LIGHT {game.EffectiveLanternRadius:0.0}",
            "                   ┌─────────┐",
            $"                   │    @    │        SELECTED: {selectedSlot.ToString().ToUpperInvariant()}",
            "                   └─────────┘"
        };
        for (var i = 0; i < Game.EquipmentSlots.Length; i++)
        {
            var slot = Game.EquipmentSlots[i];
            var item = game.Inventory.FirstOrDefault(entry => entry.Equipped && entry.Slot == slot);
            var marker = i == game.MenuSelection ? "\u0001" : "";
            var pointer = i == game.MenuSelection ? "▶" : " ";
            var rarity = item is null ? "" : item.Rarity == ItemRarity.Common ? "" : $"{item.Rarity} ";
            lines.Add($"{marker} {pointer} [{ShortSlot(slot),-5}] {(item is null ? "— empty —" : rarity + item.Name),-32} {(item?.BonusText ?? "")}");
        }
        lines.Add(" ─────────────────────────────────────────────────────────────");
        lines.Add(selectedItem is null ? " Empty equipment slot. Open the pack to equip an item." : $" {selectedItem.Description}");
        lines.Add(selectedItem is null ? "" : $" {selectedItem.Rarity} · {selectedItem.BonusText}");
        lines.Add($" STR {game.Strength}  DEX {game.Dexterity}  INT {game.Intellect}  CON {game.Constitution}  WIS {game.Wisdom}  GOLD {game.Player.Gold}");
        lines.Add(" ↑↓/←→ select slot · Enter remove · I open pack · C/Esc close");
        AppendPanel(sb, lines.ToArray(), width, height, new Rgb(226, 194, 105));
    }

    private static string EquippedName(Game game, EquipmentSlot slot) =>
        game.Inventory.FirstOrDefault(i => i.Equipped && i.Slot == slot)?.Name ?? "—";

    private static void AppendSpellbookOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var compact = height < 28;
        if (compact)
        {
            var compactLines = new List<string> { $" {game.Class.ToString().ToUpperInvariant()} SPELLBOOK — MANA {game.Mana}/{game.MaxMana} ", "" };
            var visible = Math.Min(4, game.KnownSpells.Count);
            var start = Math.Clamp(game.MenuSelection - visible / 2, 0, Math.Max(0, game.KnownSpells.Count - visible));
            for (var i = start; i < game.KnownSpells.Count && i < start + visible; i++)
            {
                var spell = game.KnownSpells[i];
                var marker = i == game.MenuSelection ? "\u0001" : "";
                compactLines.Add($"{marker} {(i == game.MenuSelection ? '▶' : ' ')} {i + 1}. {Game.SpellName(spell),-20} {game.SpellCost(spell)} mana · rank {game.SpellInvestment(spell)}/3");
            }
            var selected = game.KnownSpells[Math.Clamp(game.MenuSelection, 0, game.KnownSpells.Count - 1)];
            compactLines.Add(" ──────────────────────────────────────────────────");
            compactLines.Add($" {Game.SpellDescription(selected)}");
            var locked = game.ClassSpellTree.Where(entry => !game.KnownSpells.Contains(entry.Spell)).Take(2).ToList();
            foreach (var entry in locked) compactLines.Add($" ◇ {Game.SpellName(entry.Spell)} — learnable at level {entry.Level}");
            var remaining = game.ClassSpellTree.Count(entry => !game.KnownSpells.Contains(entry.Spell)) - locked.Count;
            if (remaining > 0) compactLines.Add($" … and {remaining} later class spell{(remaining == 1 ? "" : "s")}");
            compactLines.Add(" ↑↓ select · Enter target/cast · number quick cast · Z/Esc close");
            AppendPanel(sb, compactLines.ToArray(), width, height, new Rgb(130, 155, 255));
            return;
        }
        var lines = new List<string> { $" {game.Class.ToString().ToUpperInvariant()} SPELLBOOK — MANA {game.Mana}/{game.MaxMana} ", "" };
        for (var i = 0; i < game.KnownSpells.Count; i++)
        {
            var spell = game.KnownSpells[i];
            var marker = i == game.MenuSelection ? "\u0001" : "";
            var pointer = i == game.MenuSelection ? "▶" : " ";
            lines.Add($"{marker} {pointer} {i + 1}. {Game.SpellName(spell),-22} {game.SpellCost(spell)} mana  mastery {game.SpellInvestment(spell)}/3");
            lines.Add($"    {Game.SpellDescription(spell)}");
        }
        lines.Add(" ── CLASS PATH ─────────────────────────────────────");
        foreach (var locked in game.ClassSpellTree.Where(entry => !game.KnownSpells.Contains(entry.Spell)))
            lines.Add($"  ◇ {Game.SpellName(locked.Spell),-22} learnable from level {locked.Level}");
        lines.Add(""); lines.Add(" ↑↓ select · Enter target/cast · number quick cast · Z/Esc close");
        AppendPanel(sb, lines.ToArray(), width, height, new Rgb(130, 155, 255));
    }

    private static void AppendDialogueOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var npc = game.ActiveNpc;
        if (npc is null) return;
        var lines = new List<string>
        {
            $" {npc.Name.ToUpperInvariant()} — {npc.Role.ToUpperInvariant()} ",
            $" Schedule: {npc.ScheduleState}",
            $" “{npc.Greeting}”"
        };
        var quest = game.QuestForNpc(npc);
        if (quest is not null)
        {
            lines.Add("");
            lines.Add($" ── {quest.Title.ToUpperInvariant()} ──");
            lines.Add(quest.Status switch
            {
                QuestStatus.Available => $" {quest.Description}",
                QuestStatus.Active => $" “{quest.ProgressDialogue}”",
                QuestStatus.ReadyToTurnIn => $" “{quest.CompletionDialogue}”",
                _ => $" “{quest.CompletionDialogue}”"
            });
            lines.Add($" Objective: {quest.ObjectiveText}");
            if (quest.Accepted) lines.Add($" Progress: {quest.ProgressText} · {QuestStatusLabel(quest.Status)}");
            lines.Add($" Reward: {game.QuestRewardText(quest)}");
            lines.Add("");
            lines.Add(quest.Status switch
            {
                QuestStatus.Available => " Enter accept quest · E/Esc leave",
                QuestStatus.Active => " Enter acknowledge · E/Esc leave",
                QuestStatus.ReadyToTurnIn => " Enter claim reward · E/Esc leave",
                _ => " Enter or E/Esc leave"
            });
        }
        else
        {
            lines.Add("");
            lines.Add(" Enter or E/Esc leave");
        }
        AppendPanel(sb, lines.ToArray(), width, height, npc.Color, compactDialog: true);
    }

    private static void AppendQuestLogOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var quests = game.JournalQuests;
        var lines = new List<string> { " EMBERFALL JOURNAL ", " Accepted work and the people waiting on it", "" };
        if (quests.Count == 0)
        {
            lines.Add(" No accepted quests.");
            lines.Add(" Talk to Emberfall residents. Quest-givers carry a restrained golden pulse.");
            lines.Add("");
            lines.Add(" Tab/Esc close");
            AppendPanel(sb, lines.ToArray(), width, height, new Rgb(205, 170, 85), compactDialog: true);
            return;
        }

        for (var i = 0; i < quests.Count; i++)
        {
            var quest = quests[i];
            var selected = i == Math.Clamp(game.MenuSelection, 0, quests.Count - 1);
            var marker = selected ? "\u0001" : "";
            var pointer = selected ? "▶" : " ";
            lines.Add($"{marker} {pointer} {quest.Title,-24} {QuestStatusLabel(quest.Status),-16} {quest.ProgressText}");
        }

        var active = quests[Math.Clamp(game.MenuSelection, 0, quests.Count - 1)];
        var giver = game.QuestGiver(active);
        lines.Add(" ─────────────────────────────────────────────────────────────");
        lines.Add($" {active.Description}");
        lines.Add($" Objective: {active.ObjectiveText}");
        lines.Add($" Progress: {active.ProgressText} · {QuestStatusLabel(active.Status)}");
        lines.Add($" Giver: {giver?.Name ?? "unknown"}, {giver?.Role ?? "resident"}");
        lines.Add($" Reward: {game.QuestRewardText(active)}");
        if (active.Status == QuestStatus.ReadyToTurnIn) lines.Add($" Return to {giver?.Name ?? "the quest giver"} in Emberfall.");
        lines.Add("");
        lines.Add(" ↑↓ select · Tab/Esc close · The journal does not pass a turn");
        AppendPanel(sb, lines.ToArray(), width, height, new Rgb(205, 170, 85));
    }

    private static string QuestStatusLabel(QuestStatus status) => status switch
    {
        QuestStatus.Available => "AVAILABLE",
        QuestStatus.Active => "IN PROGRESS",
        QuestStatus.ReadyToTurnIn => "RETURN",
        _ => "COMPLETED"
    };

    private static void AppendMapOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var compact = width < 80 || height < 24;
        var mapRows = BuildExploredMapLines(game, Math.Max(20, width - 8), Math.Max(6, height - (compact ? 14 : 13)));
        var explored = 0;
        for (var y = 0; y < game.CurrentMap.Height; y++)
        for (var x = 0; x < game.CurrentMap.Width; x++)
            if (game.CurrentMap.Explored[x, y]) explored++;
        var coverage = explored * 100.0 / (game.CurrentMap.Width * game.CurrentMap.Height);
        var lines = new List<string>
        {
            compact ? $" MAP — {game.CurrentMap.Name.ToUpperInvariant()} " : $" CARTOGRAPHER'S VIEW — {game.CurrentMap.Name.ToUpperInvariant()} ",
            $" Explored {coverage:0.0}% · {game.CurrentMap.Width}×{game.CurrentMap.Height} tiles",
            ""
        };
        lines.AddRange(mapRows.Select(row => " " + row));
        lines.Add("");
        if (compact)
        {
            lines.Add(" @ you  n resident  = travel  ◆ landmark  < up  > down  $ shop  * item");
            lines.Add(" + door  ! trap  & machine  i torch  § supply");
            lines.Add(" ≈ water  ▓ wall  ^ hazard  · floor");
        }
        else
        {
            lines.Add(" @ you   n resident   = travel   ◆ landmark   < up   > down   $ shop   * item");
            lines.Add(" ▓ wall   · floor   + door   ! trap   & machinery   i torch   § provisions   ^ hazard");
        }
        lines.Add(" M/Esc close · Opening the map does not pass a turn");
        AppendPanel(sb, lines.ToArray(), width, height, new Rgb(80, 205, 220));
    }

    public static string[] BuildExploredMapLines(Game game, int maxWidth, int maxHeight)
    {
        var map = game.CurrentMap;
        var columns = Math.Clamp(maxWidth, 1, map.Width);
        var rows = Math.Clamp(maxHeight, 1, map.Height);
        var result = new string[rows];
        for (var outputY = 0; outputY < rows; outputY++)
        {
            var y0 = outputY * map.Height / rows;
            var y1 = Math.Max(y0 + 1, (outputY + 1) * map.Height / rows);
            var line = new StringBuilder(columns);
            for (var outputX = 0; outputX < columns; outputX++)
            {
                var x0 = outputX * map.Width / columns;
                var x1 = Math.Max(x0 + 1, (outputX + 1) * map.Width / columns);
                var player = false; var npc = false; var travel = false; var up = false; var down = false; var shop = false; var item = false;
                var door = false; var trap = false; var machine = false; var wallTorch = false; var provision = false; var hazard = false; var landmark = false;
                var known = 0; var walls = 0; var floors = 0; var water = 0; var destructible = 0;
                for (var y = y0; y < y1 && y < map.Height; y++)
                for (var x = x0; x < x1 && x < map.Width; x++)
                {
                    var p = new Point(x, y);
                    if (p == game.Player.Position) player = true;
                    if (!map.Explored[x, y]) continue;
                    known++;
                    if (p == map.StairsUp) up = true;
                    if (p == map.StairsDown) down = true;
                    if (map.Shops.Any(entry => entry.Position == p)) shop = true;
                    if (map.Npcs.Any(entry => entry.Position == p)) npc = true;
                    if (map.TravelPoints.ContainsKey(p)) travel = true;
                    if (map.Landmarks.TryGetValue(p, out var landmarkName) && game.DiscoveredLandmarks.Contains(landmarkName)) landmark = true;
                    if (map.GroundItems.Any(entry => entry.Position == p)) item = true;
                    if (map.Doors.ContainsKey(p)) door = true;
                    if (map.Traps.TryGetValue(p, out var feature) && feature.Revealed) trap = true;
                    if (map.Machines.ContainsKey(p)) machine = true;
                    if (map.WallTorches.ContainsKey(p)) wallTorch = true;
                    if (map.Provisions.TryGetValue(p, out var supply) && supply.Charges > 0) provision = true;
                    var element = map.Elements[x, y];
                    if (element.Fire > 0 || element.Electric > 0 || element.Poison > 0) hazard = true;
                    var tile = map[p];
                    if (tile.Name.Contains("water", StringComparison.OrdinalIgnoreCase)) water++;
                    if (tile.Destructible) destructible++;
                    if (tile.Walkable) floors++; else walls++;
                }
                line.Append(player ? '@' : npc ? 'n' : travel ? '=' : landmark ? '◆' : shop ? '$' : down ? '>' : up ? '<' : trap ? '!' : machine ? '&' : wallTorch ? 'i' : provision ? '§' : item ? '*' : door ? '+' : hazard ? '^' : known == 0 ? ' ' :
                    destructible > 0 && destructible >= floors ? '▒' : water > 0 && water >= floors / 2 ? '≈' : walls > floors ? '▓' : '·');
            }
            result[outputY] = line.ToString();
        }
        return result;
    }

    private static void AppendFastTravelOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var options = game.FastTravelOptions;
        var lines = new List<string> { " WAYFARER'S ROAD MAP ", " Travel only between discovered outdoor landmarks · cost: 1 hour, 3 food, 5 water", "" };
        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var selected = i == Math.Clamp(game.MenuSelection, 0, Math.Max(0, options.Count - 1));
            lines.Add($"{(selected ? "\u0001 ▶" : "  ")} {option.Name,-34} {option.Map.Name}");
        }
        lines.Add("");
        lines.Add(" ↑↓ select · Enter travel · F2/Esc close · unavailable during hostile pursuit");
        AppendPanel(sb, lines.ToArray(), width, height, new Rgb(104, 190, 205));
    }

    private static void AppendCraftingOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var recipes = game.CraftingRecipes;
        var lines = new List<string> { " SETTLEMENT WORKBENCH ", $" Workshop purse: {game.Player.Gold} gold · Materials become equipment and expedition supplies", "" };
        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            var selected = i == Math.Clamp(game.MenuSelection, 0, recipes.Count - 1);
            var ingredients = string.Join(", ", recipe.Ingredients.Select(ingredient => $"{ingredient.Quantity} {WorldBuilder.MakeItem(ingredient.Kind).Name}"));
            lines.Add($"{(selected ? "\u0001 ▶" : "  ")} {recipe.Name,-22} {ingredients} · {recipe.GoldCost}g");
            if (selected) lines.Add($"    {recipe.Description} Produces: {recipe.Result.Quantity}× {recipe.Result.Name}");
        }
        lines.Add("");
        lines.Add(" ↑↓ select · Enter craft · F3/Esc close · crafting passes one turn");
        AppendPanel(sb, lines.ToArray(), width, height, new Rgb(214, 158, 82));
    }

    private static void AppendLoreOverlay(StringBuilder sb, Game game, int width, int height)
    {
        var lines = new List<string> { " MARCH LORE ", $" {game.DiscoveredLandmarks.Count} landmarks discovered · knowledge rewards attentive exploration", "" };
        if (game.LoreEntries.Count == 0) lines.Add(" No lore recorded. Seek ruins, old stones, dungeons, and Valeward.");
        else foreach (var entry in game.LoreEntries) { lines.Add(" ◆ " + entry); lines.Add(""); }
        lines.Add(" F4/Esc close · lore does not pass a turn");
        AppendPanel(sb, lines.ToArray(), width, height, new Rgb(174, 144, 222));
    }

    private static void AppendSkillTreeOverlay(StringBuilder sb, Game game, int width, int height, double now)
    {
        var lines = new List<string>
        {
            $" {game.Class.ToString().ToUpperInvariant()} SKILL TREE ─ LEVEL {game.Level} ─ {game.SkillPoints} SKILL POINT{(game.SkillPoints == 1 ? "" : "S")} ",
            " Four branches · 6 linked tiers · 3 ranks per node · one parent rank opens the next tier"
        };
        var selected = game.SkillTree[Math.Clamp(game.MenuSelection, 0, game.SkillTree.Count - 1)];
        static char State(Game game, SkillNode node) => node.Maxed ? '●' : node.Purchased ? '◉' : game.CanPurchaseSkill(node) ? '◆' : '◇';
        static string ShortName(string name, int length) => name.Length <= length ? name : name[..(length - 1)] + "…";

        if (width >= 92 && height >= 25)
        {
            var headers = Enumerable.Range(0, Game.SkillBranchCount)
                .Select(branch => game.SkillTree[branch * Game.SkillTierCount].Branch)
                .Select(branch => $"┌─ {ShortName(branch, 14),-14} ─┐");
            lines.Add(" " + string.Join("  ", headers));
            for (var tier = 0; tier < Game.SkillTierCount; tier++)
            {
                if (tier > 0) lines.Add("         │                     │                     │                     │");
                var cells = new List<string>();
                var selectedRow = false;
                for (var branch = 0; branch < Game.SkillBranchCount; branch++)
                {
                    var index = branch * Game.SkillTierCount + tier;
                    var node = game.SkillTree[index];
                    var pointer = index == game.MenuSelection ? '▶' : ' ';
                    selectedRow |= index == game.MenuSelection;
                    cells.Add($"{pointer}{State(game, node)} {ShortName(node.Name, 11),-11} {node.Rank}/{node.MaxRank}");
                }
                lines.Add((selectedRow ? "\u0001" : "") + " " + string.Join("  ", cells));
            }
        }
        else
        {
            if (height < 25) lines.RemoveAt(1);
            lines.Add($" BRANCH {selected.BranchIndex + 1}/{Game.SkillBranchCount}: {selected.Branch}");
            for (var tier = 0; tier < Game.SkillTierCount; tier++)
            {
                var node = game.SkillTree[selected.BranchIndex * Game.SkillTierCount + tier];
                var marker = node == selected ? "\u0001" : "";
                var pointer = node == selected ? '▶' : ' ';
                lines.Add($"{marker} {pointer} {State(game, node)} T{tier + 1} {node.Name,-21} rank {node.Rank}/{node.MaxRank}  Lv {node.RequiredLevel}");
            }
        }
        lines.Add(" ─────────────────────────────────────────────────────────────");
        lines.Add($" {selected.Name} [{selected.Rank}/{selected.MaxRank}]: {selected.Description}");
        lines.Add($" {game.SkillLockReason(selected)}{(selected.Spell is { } spell ? $" · spell mastery bonus +{game.SpellSkillBonus(spell)}" : "")}");
        lines.Add(" ↑↓ follow branch · ←→ change branch · Enter invest point · T/Esc close");
        var bloom = game.Effects.OfType<FlourishEffect>().LastOrDefault(effect => effect.Kind == FlourishKind.SkillBloom);
        var accent = new Rgb(255, 202, 72);
        if (bloom is not null)
        {
            var t = Math.Clamp((now - bloom.StartedAt) / bloom.Duration, 0, 1);
            accent = Rgb.Lerp(accent, t < 0.45 ? Rgb.White : bloom.SecondaryColor, Math.Sin(t * Math.PI) * 0.55);
        }
        AppendPanel(sb, lines.ToArray(), width, height, accent);
    }

    private static void AppendGameOver(StringBuilder sb, int width, int height)
    {
        AppendPanel(sb, [" YOUR LIGHT IS GONE ", "", "The Ashen Vaults remember every spark.", "", "Press Q or Escape to leave."], width, height, new Rgb(255, 55, 35), compactDialog: true);
    }

    private static void AppendQuitConfirmation(StringBuilder sb, Game game, int width, int height)
    {
        var no = game.MenuSelection == 0 ? "\u0001 ▶  NO — KEEP PLAYING" : "    NO — KEEP PLAYING";
        var yes = game.MenuSelection == 1 ? "\u0001 ▶  YES — QUIT" : "    YES — QUIT";
        AppendPanel(sb,
        [
            " QUIT LUMENROGUE? ",
            "",
            " Your current expedition will be lost.",
            "",
            no,
            yes,
            "",
            " ←→ select · Enter confirm · Y/N shortcut · Esc cancel"
        ], width, height, new Rgb(255, 105, 62), compactDialog: true);
    }

    private readonly record struct PanelLine(string Text, bool Highlighted = false);

    internal static (int Width, int Height) MenuPanelDimensions(int screenWidth, int screenHeight) =>
        (Math.Max(50, screenWidth - 4), Math.Max(3, screenHeight - 4));

    internal static string[] WrapPanelText(string text, int width) =>
        WrapPanelLines([text], width).Select(line => line.Text).ToArray();

    private static void AppendPanel(StringBuilder sb, string[] lines, int screenWidth, int screenHeight, Rgb accent,
        bool compactDialog = false)
    {
        // Menus own a stable viewport so changing selection never changes their
        // frame. Confirmations remain compact because they contain no variable
        // descriptions. Both layouts wrap rather than silently truncating text.
        var menuDimensions = MenuPanelDimensions(screenWidth, screenHeight);
        var panelWidth = compactDialog ? Math.Max(50, Math.Min(screenWidth - 4, 68)) : menuDimensions.Width;
        var contentWidth = panelWidth - 2;
        var body = WrapPanelLines(lines.Take(Math.Max(0, lines.Length - 1)), contentWidth);
        List<PanelLine> footer = lines.Length == 0 ? [] : WrapPanelLines([lines[^1]], contentWidth);

        int innerHeight;
        if (compactDialog)
            innerHeight = Math.Min(screenHeight - 4, Math.Max(1, body.Count + footer.Count));
        else
            innerHeight = menuDimensions.Height - 2;

        var availableBody = Math.Max(0, innerHeight - footer.Count);
        if (body.Count > availableBody)
        {
            var roomForContent = Math.Max(0, availableBody - 1);
            var head = Math.Min(body.Count, (roomForContent + 1) / 2);
            var tail = Math.Min(body.Count - head, roomForContent - head);
            var clipped = new List<PanelLine>(availableBody);
            clipped.AddRange(body.Take(head));
            if (availableBody > 0) clipped.Add(new PanelLine(" … content condensed for this terminal height … "));
            if (tail > 0) clipped.AddRange(body.TakeLast(tail));
            body = clipped;
        }

        var display = new List<PanelLine>(innerHeight);
        display.AddRange(body);
        while (display.Count + footer.Count < innerHeight) display.Add(new PanelLine(""));
        display.AddRange(footer.Take(Math.Max(0, innerHeight - display.Count)));

        var panelHeight = innerHeight + 2;
        var left = Math.Max(1, (screenWidth - panelWidth) / 2 + 1);
        var top = Math.Max(1, (screenHeight - panelHeight) / 2 + 1);
        sb.Append("\x1b[").Append(top).Append(';').Append(left).Append('H');
        sb.Append("\x1b[48;2;8;8;13m\x1b[38;2;").Append(accent.R).Append(';').Append(accent.G).Append(';').Append(accent.B).Append('m');
        sb.Append('┌').Append(new string('─', contentWidth)).Append('┐');
        for (var i = 0; i < display.Count; i++)
        {
            sb.Append("\x1b[").Append(top + i + 1).Append(';').Append(left).Append('H').Append('│');
            var line = display[i];
            if (line.Highlighted)
                sb.Append("\x1b[1m\x1b[48;2;").Append(accent.R / 4).Append(';').Append(accent.G / 4).Append(';').Append(accent.B / 4)
                    .Append("m\x1b[38;2;245;245;235m");
            sb.Append(line.Text.PadRight(contentWidth));
            if (line.Highlighted)
                sb.Append("\x1b[0m\x1b[48;2;8;8;13m\x1b[38;2;").Append(accent.R).Append(';').Append(accent.G).Append(';').Append(accent.B).Append('m');
            sb.Append('│');
        }
        sb.Append("\x1b[").Append(top + panelHeight - 1).Append(';').Append(left).Append('H');
        sb.Append('└').Append(new string('─', contentWidth)).Append('┘').Append("\x1b[0m");
    }

    private static List<PanelLine> WrapPanelLines(IEnumerable<string> source, int width)
    {
        var result = new List<PanelLine>();
        foreach (var raw in source)
        {
            var highlighted = raw.StartsWith('\u0001');
            var text = highlighted ? raw[1..] : raw;
            if (text.Length == 0) { result.Add(new PanelLine("", highlighted)); continue; }
            var indentLength = Math.Min(text.TakeWhile(char.IsWhiteSpace).Count(), Math.Max(0, width - 4));
            var continuation = new string(' ', Math.Min(width - 1, indentLength + 2));
            var remaining = text;
            while (remaining.Length > width)
            {
                var breakAt = remaining.LastIndexOf(' ', width - 1, width);
                if (breakAt <= indentLength + 2) breakAt = width;
                result.Add(new PanelLine(remaining[..breakAt].TrimEnd(), highlighted));
                remaining = continuation + remaining[breakAt..].TrimStart();
            }
            result.Add(new PanelLine(remaining, highlighted));
        }
        return result;
    }
}
