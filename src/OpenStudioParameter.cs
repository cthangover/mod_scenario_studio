namespace Cthangover.Mods.ScenarioStudio
{
    using Cthangover.Core.Games.LaunchParameters;

    /// <summary>Opens the scenario studio dock on the first scene.</summary>
    public sealed class OpenStudioParameter : SwitchLaunchParameter
    {
        /// <summary>Creates a definition with the default value.</summary>
        public OpenStudioParameter() { }

        /// <summary>Parses the value supplied for this parameter.</summary>
        public OpenStudioParameter(string value) : base(value) { }

        /// <summary>Canonical argument key.</summary>
        public const string Name = "studio";

        /// <inheritdoc />
        public override string Key => Name;

        /// <inheritdoc />
        public override string Documentation => "Opens the scenario studio dock on the first scene.";
    }
}
