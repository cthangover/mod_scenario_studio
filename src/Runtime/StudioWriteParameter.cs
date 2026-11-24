namespace Cthangover.Mods.ScenarioStudio
{
    using Cthangover.Core.Games.LaunchParameters;

    /// <summary>Selects the overlay as the write target for scenario edits.</summary>
    public sealed class StudioWriteParameter : LaunchParameter<bool>
    {
        /// <summary>Creates a definition with normal write routing.</summary>
        public StudioWriteParameter() { }

        /// <summary>Enables forced overlay writes for the layer value.</summary>
        public StudioWriteParameter(string value) : base(value == "layer", value == "layer") { }

        /// <inheritdoc />
        public override string Key => "studio-write";

        /// <inheritdoc />
        public override string Documentation => "Routes all scenario studio edits to the overlay when set to layer.";

        /// <inheritdoc />
        public override string Format => "layer";
    }
}
