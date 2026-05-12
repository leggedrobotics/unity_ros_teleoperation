using LookingGlass.Toolkit;
using UnityEngine;

namespace LookingGlass {
    public class UnityLKGToolkitBootstrapper : ILKGToolkitBootstrapper {
        public UnityLKGToolkitBootstrapper() { }

        public void Bootstrap(ServiceLocator locator) {
            locator.AddSystem<IHttpSender>(new UnityHttpSender());
            locator.AddSystem<LookingGlass.Toolkit.ILogger>(new UnityLogger());
        }
    }
}
