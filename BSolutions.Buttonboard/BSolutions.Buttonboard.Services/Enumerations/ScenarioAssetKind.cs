namespace BSolutions.Buttonboard.Services.Enumerations
{
    /// <summary>
    /// Distinguishes between normal scenes and the special setup asset.
    /// </summary>
    public enum ScenarioAssetKind
    {
        /// <summary>
        /// The special "Setup.json" asset executed once during initialization/reset.
        /// </summary>
        Setup,

        /// <summary>
        /// A regular scene triggered via hardware buttons.
        /// </summary>
        Scene
    }
}
