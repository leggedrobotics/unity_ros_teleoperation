#if UNITY_EDITOR
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace LookingGlass {
    [InitializeOnLoad]
    public class PackageDependencyUtil {
        private const string NewtonsoftJsonPackageName = "com.unity.nuget.newtonsoft-json";

        static PackageDependencyUtil() {
            //NOTE: Our entire package (aka, our UnityPlugin) must fully compile, in order for this to even run:
            //So, we have to be able to conditionally-compile out the Newtonsoft.Json dependent lines throughout our codebase
            //at first, to let this automatic installation run.
            //Before Newtonsoft.Json is available, we can either throw exceptions or early return/return default values.
            _ = AutoInstallNewtonsoftJson();
        }

        private static async Task<bool> AutoInstallNewtonsoftJson() {
            ListRequest listRequest = Client.List(false, true);
            PackageCollection packages = await UPMUtility.AwaitRequest(listRequest);

            bool found = false;
            foreach (PackageInfo p in packages) {
                if (p.name == NewtonsoftJsonPackageName) {
                    found = true;
                    break;
                }
            }

            if (found)
                return false;

            string packageId = NewtonsoftJsonPackageName + "@2.0.0";
            Debug.Log("Installing " + packageId + " because the Looking Glass Plugin depends on it...");
            AddRequest addRequest = Client.Add(packageId);
            PackageInfo result = await UPMUtility.AwaitRequest(addRequest);
            return true;
        }
    }
}
#endif
