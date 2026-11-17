namespace Cthangover.Mods.ScenarioStudio.Tests.Projection
{
    using Cthangover.Core.Mods;
    using Godot;

    /// <summary>Only the new projection activation mounts this driver; the historical live suite is never invoked.</summary>
    public sealed class StudioProjectionTestInitializer : IModInitializer
    {
        private static bool queued;

        /// <inheritdoc />
        public void OnModLoaded(string modId)
        {
        }

        /// <inheritdoc />
        public void OnModResourcesReady()
        {
            if (queued)
            {
                return;
            }

            foreach (var arg in OS.GetCmdlineUserArgs())
            {
                if (arg == "--itest=studio_projection")
                {
                    queued = true;
                    ModRoot.AddDeferred(new StudioProjectionDriverNode());
                    return;
                }
            }
        }
    }
}
