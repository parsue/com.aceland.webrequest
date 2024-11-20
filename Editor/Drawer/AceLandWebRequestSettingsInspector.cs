using AceLand.Library.Editor;
using AceLand.WebRequest.ProjectSetting;
using UnityEditor;

namespace AceLand.WebRequest.Editor.Drawer
{
    [CustomEditor(typeof(AceLandWebRequestSettings))]
    public class AceLandWebRequestSettingsInspector : UnityEditor.Editor
    {   
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorHelper.DrawAllPropertiesAsDisabled(serializedObject);
        }
    }
}