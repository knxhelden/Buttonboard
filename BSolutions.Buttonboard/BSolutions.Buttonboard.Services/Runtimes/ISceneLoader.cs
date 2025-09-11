namespace BSolutions.Buttonboard.Services.Runtimes
{
    public interface ISceneLoader
    {
        /// <summary>
        /// Starts the loader: performs an initial scan/load and then enables hot-reload.
        /// </summary>
        void Start();

        /// <summary>
        /// Gets the current definition of a scene (file name without extension).
        /// </summary>
        bool TryGet(string sceneKey, out SceneDefinition? scene);
    }
}
