namespace Cthangover.Mods.ScenarioStudio
{
    using Cthangover.Core.Games;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Utils;

    /// <summary>
    /// Lifecycle entry of the studio mod. When resources are ready it mounts
    /// the edit layer (<see cref="StudioLayer"/>) and parks <see cref="StudioLive"/>
    /// at <c>/root</c>, so the debugger, the journal and the rollback snapshots
    /// work from the first scene on, before the dock is ever opened. The launch
    /// flag <c>--studio</c> additionally opens the dock with the first scene
    /// (<c>debug_scenario_studio.bat</c>). Nodes are added deferred because
    /// resources-ready fires before the shell scene exists.
    /// </summary>
    public class StudioLaunchInitializer : IModInitializer
    {

        public void OnModLoaded(string modId) { }

        public void OnModResourcesReady()
        {
            if (StudioLive.Instance != null)
                return;

            var args = GameLaunchArgs.Current;
            var layer = new StudioLayer(args);
            layer.Mount();

            ModRoot.AddDeferred(new StudioLive(layer));

            if (!args.Contains<OpenStudioParameter>())
                return;
            GameLogger.Log("UI", "Scenario studio: --studio given, the dock opens with the first scene");
            ModRoot.AddDeferred(new StudioAutoOpenNode());
        }
    }
}
