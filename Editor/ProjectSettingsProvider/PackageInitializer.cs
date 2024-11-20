using AceLand.WebRequest.ProjectSetting;
using UnityEditor;

namespace AceLand.WebRequest.Editor.ProjectSettingsProvider
{
    [InitializeOnLoad]
    public static class PackageInitializer
    {
        static PackageInitializer()
        {
            AceLandWebRequestSettings.GetSerializedSettings();
        }
    }
}