namespace LookingGlass.Toolkit {
    /// <summary>
    /// Allows you to easily initialize the major systems in LKG Toolkit.
    /// </summary>
    public static class LKGToolkitController {
        private static bool initialized = false;

        public static void Initialize(ILogger logger, IHttpSender httpSender) {
            if (initialized)
                return;
            initialized = true;

            ServiceLocator locator = new();
            locator.AddSystem(logger);
            locator.AddSystem(httpSender);
            ServiceLocator.Instance = locator;
        }

        public static void Uninitialize() {
            if (!initialized)
                return;
            initialized = false;

            ServiceLocator.Instance.Dispose();
            ServiceLocator.Instance = null;
        }
    }
}
