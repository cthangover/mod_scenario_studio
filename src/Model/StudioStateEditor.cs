namespace Cthangover.Mods.ScenarioStudio
{
    /// <summary>How a value of the "State" tab can be changed in place; <see cref="None"/> is read-only.</summary>
    public enum StudioStateEditor
    {
        None,
        DialogVariable,
        Inventory,
        Quest,
        /// <summary>A story flag of the running object's scope (<c>self.&lt;name&gt;</c>): the value is the full flag key.</summary>
        SelfFlag,
    }
}
