using AceLand.WebRequest.ProjectSetting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AceLand.WebRequest.Editor.ProjectSettingsProvider
{
    public class AceLandWebRequestSettingsProvider : SettingsProvider
    {
        public const string SETTINGS_NAME = "Project/AceLand Web Request";
        private SerializedObject _settings;

        private AceLandWebRequestSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings = AceLandWebRequestSettings.GetSerializedSettings();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new AceLandWebRequestSettingsProvider(SETTINGS_NAME, SettingsScope.Project);
            return provider;
        }

        public override void OnGUI(string searchContext)
        {
            SerializedProperty(out var loggingLevel, out var resultLoggingLevel,
                out var checkJsonBeforeSend, out var forceHttpsScheme,
                out var addTimeInHeader, out var timeKey,
                out var requestTimeout, out var longRequestTimeout,
                out var requestRetry, out var retryInterval);

            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(loggingLevel, GUILayout.Width(400));
            EditorGUILayout.PropertyField(resultLoggingLevel, GUILayout.Width(400));

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Checking Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(checkJsonBeforeSend, GUILayout.Width(400));
            EditorGUILayout.PropertyField(forceHttpsScheme, GUILayout.Width(400));
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Header Auto Fill", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(addTimeInHeader, GUILayout.Width(400));
            if (addTimeInHeader.boolValue)
                EditorGUILayout.PropertyField(timeKey, GUILayout.Width(400));
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Request Options (time unit: ms)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(requestTimeout, GUILayout.Width(400));
            EditorGUILayout.PropertyField(longRequestTimeout, GUILayout.Width(400));
            requestRetry.intValue = EditorGUILayout.IntSlider(requestRetry.displayName, requestRetry.intValue,
                0, retryInterval.arraySize, GUILayout.Width(400));
            
            DrawArrayWithDefaultExpanded(retryInterval);
            
            if (!EditorGUI.EndChangeCheck()) return;

            requestRetry.intValue = Mathf.Clamp(requestRetry.intValue, 0, retryInterval.arraySize);
            _settings.ApplyModifiedPropertiesWithoutUndo();
        }


        private void SerializedProperty(out SerializedProperty loggingLevel, out SerializedProperty resultLoggingLevel,
            out SerializedProperty checkJsonBeforeSend, out SerializedProperty forceHttpsScheme,
            out SerializedProperty addTimeInHeader, out SerializedProperty timeKey,
            out SerializedProperty requestTimeout, out SerializedProperty longRequestTimeout,
            out SerializedProperty requestRetry, out SerializedProperty retryInterval)
        {
            loggingLevel = _settings.FindProperty("loggingLevel");
            resultLoggingLevel = _settings.FindProperty("resultLoggingLevel");
            checkJsonBeforeSend = _settings.FindProperty("checkJsonBeforeSend");
            forceHttpsScheme = _settings.FindProperty("forceHttpsScheme");
            addTimeInHeader = _settings.FindProperty("addTimeInHeader");
            timeKey = _settings.FindProperty("timeKey");
            requestTimeout = _settings.FindProperty("requestTimeout");
            longRequestTimeout = _settings.FindProperty("longRequestTimeout");
            requestRetry = _settings.FindProperty("requestRetry");
            retryInterval = _settings.FindProperty("retryInterval");
        }

        private void DrawArrayWithDefaultExpanded(SerializedProperty arrayProperty)
        {
            if (!arrayProperty.isArray) return;
            
            EditorGUILayout.BeginHorizontal();
            
            var fieldLabel = $"{arrayProperty.displayName} ({arrayProperty.arraySize})";
            EditorGUILayout.LabelField(fieldLabel, GUILayout.Width(160));
            
            if (GUILayout.Button("+", GUILayout.Width(36)))
            {
                arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
            }
            if (GUILayout.Button("-", GUILayout.Width(36)))
            {
                arrayProperty.DeleteArrayElementAtIndex(arrayProperty.arraySize - 1);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            
            for (var i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.PropertyField(element, GUIContent.none, GUILayout.Width(200));

                if (GUILayout.Button("-", GUILayout.Width(36)))
                {
                    arrayProperty.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
        }
    }
}