namespace Cthangover.Mods.ScenarioStudio
{
    using Cthangover.Core.Games.LaunchParameters;

    /// <summary>Selects the root directory of the scenario studio overlay.</summary>
    public sealed class StudioLayerDirectoryParameter : StringLaunchParameter
    {
        /// <summary>Creates a definition with the default value.</summary>
        public StudioLayerDirectoryParameter() { }

        /// <summary>Parses the value supplied for this parameter.</summary>
        public StudioLayerDirectoryParameter(string value) : base(value) { }

        /// <summary>Canonical argument key.</summary>
        public const string Name = "studio-layer";

        /// <inheritdoc />
        public override string Key => Name;

        /// <inheritdoc />
        public override string Documentation => "Selects the root directory of the scenario studio overlay.";
    }
}
