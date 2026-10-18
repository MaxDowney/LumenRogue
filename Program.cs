using System.Diagnostics;
using System.Text;

namespace LumenRogue;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            Environment.ExitCode = SelfTests.Run();
            return;
        }

        Console.OutputEncoding = Encoding.UTF8;
        Console.TreatControlCAsInput = true;

        var seedIndex = Array.FindIndex(args, argument => argument.Equals("--seed", StringComparison.OrdinalIgnoreCase));
        var seed = seedIndex >= 0 && seedIndex + 1 < args.Length && int.TryParse(args[seedIndex + 1], out var requestedSeed)
            ? requestedSeed
            : Random.Shared.Next(1, int.MaxValue);
        var loadIndex = Array.FindIndex(args, argument => argument.Equals("--load", StringComparison.OrdinalIgnoreCase));
        var loadPath = loadIndex >= 0 && loadIndex + 1 < args.Length ? args[loadIndex + 1] : null;
        Game game;
        try { game = loadPath is not null ? Game.LoadGame(loadPath) : new Game(worldSeed: seed); }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not load save: {exception.Message}");
            game = new Game(worldSeed: seed);
        }
        var renderer = new Renderer();
        var clock = Stopwatch.StartNew();

        Console.Write("\x1b[?1049h\x1b[?25l\x1b[2J");
        try
        {
            while (!game.QuitRequested)
            {
                var now = clock.Elapsed.TotalSeconds;
                game.UpdateEffects(now);

                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    game.HandleKey(key, now);
                    if (game.LoadRequested)
                    {
                        try { game = Game.LoadGame(Game.DefaultSavePath); renderer = new Renderer(); }
                        catch (Exception exception) { game.LoadRequested = false; game.AddMessage($"Load failed: {exception.Message}"); }
                    }
                }

                renderer.Draw(game, now);
                // About 22 FPS is plenty for flame and projectile motion, and gives
                // slower terminal emulators time to present each synchronized frame.
                Thread.Sleep(45);
            }
        }
        finally
        {
            Console.Write("\x1b[0m\x1b[?25h\x1b[?1049l");
        }
    }
}
