
namespace Bhaptics.SDK2 
{
    
    public static class BhapticsLogManager
    {
        public static void Log(string format)
        {
            // UnityEngine.Debug.Log("[bHaptics]" + format);
            return;
        }
        public static void LogFormat(string format, params object[] args)
        {
            // UnityEngine.Debug.LogFormat("[bHaptics]" + format, args);
            return;
        }
        public static void LogErrorFormat(string format, params object[] args)
        {
            // UnityEngine.Debug.LogErrorFormat("[bHaptics]" + format, args);
            return;
        }
        public static void LogError(string format)
        {
            // UnityEngine.Debug.LogErrorFormat("[bHaptics]" + format);
            return;
        }
    }

}
