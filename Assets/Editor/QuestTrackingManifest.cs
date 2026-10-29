using System.IO;
using System.Xml;
using UnityEditor.Android;

/// <summary>Keep tracking declarations in the final OpenXR Android manifest.</summary>
public sealed class QuestTrackingManifest : IPostGenerateGradleAndroidProject
{
    private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

    public int callbackOrder => int.MaxValue;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // OpenXR does not run OVRManifestPreprocessor, so Meta project settings alone
        // do not guarantee the body permission reaches the generated Gradle project.
        var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
        var document = new XmlDocument();
        document.Load(manifestPath);
        EnsureEntry(document, "uses-permission", "com.oculus.permission.HAND_TRACKING");
        EnsureEntry(document, "uses-permission", "com.oculus.permission.BODY_TRACKING");
        EnsureEntry(document, "uses-feature", "oculus.software.handtracking", "false");
        EnsureEntry(document, "uses-feature", "com.oculus.software.body_tracking", "false");
        document.Save(manifestPath);
    }

    private static void EnsureEntry(XmlDocument document, string tag, string name, string required = null)
    {
        var root = document.DocumentElement;
        XmlElement entry = null;
        foreach (XmlNode node in root.ChildNodes)
        {
            if (node is XmlElement element && element.Name == tag &&
                element.GetAttribute("name", AndroidNamespace) == name)
            {
                entry = element;
                break;
            }
        }
        if (entry == null)
        {
            entry = document.CreateElement(tag);
            entry.SetAttribute("name", AndroidNamespace, name);
            root.AppendChild(entry);
        }
        if (required != null)
            entry.SetAttribute("required", AndroidNamespace, required);
    }
}
