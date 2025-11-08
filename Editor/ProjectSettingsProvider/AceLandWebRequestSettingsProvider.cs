using AceLand.Library.Editor.Providers;
using AceLand.WebRequest.Profiles;
using AceLand.WebRequest.ProjectSetting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AceLand.WebRequest.Editor.ProjectSettingsProvider
{
    public class AceLandWebRequestSettingsProvider : AceLandSettingsProvider
    {
        public const string SETTINGS_NAME = "Project/AceLand Packages/Web Request";
        
        private const int FIXED_PROPERTY_WIDTH = 400;
        private const int FIXED_SMALL_BUTTON_WIDTH = 18;
        private const int FIXED_KEY_WIDTH = 120;
        private const int EXPENDED_VALUE_WIDTH = 260;

        private AceLandWebRequestSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            Settings = AceLandWebRequestSettings.GetSerializedSettings();
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
                out var autoFillHeaders,
                out var requestTimeout, out var longRequestTimeout,
                out var requestRetry, out var retryInterval,
                out var apiSections, out var currentApiSection);

            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
            DrawFixWidthProperty(loggingLevel);
            DrawFixWidthProperty(resultLoggingLevel);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Checking Options", EditorStyles.boldLabel);
            DrawFixWidthProperty(checkJsonBeforeSend);
            DrawFixWidthProperty(forceHttpsScheme);
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Request Options (time unit: ms)", EditorStyles.boldLabel);
            DrawFixWidthProperty(requestTimeout);
            DrawFixWidthProperty(longRequestTimeout);
            requestRetry.intValue = EditorGUILayout.IntSlider(requestRetry.displayName, requestRetry.intValue,
                0, retryInterval.arraySize, GUILayout.Width(FIXED_PROPERTY_WIDTH));
            DrawArrayWithDefaultExpanded(retryInterval);
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Header Auto Fill", EditorStyles.boldLabel);
            DrawFixWidthProperty(addTimeInHeader);
            if (addTimeInHeader.boolValue)
                DrawFixWidthProperty(timeKey);
            DrawHeadersArrayWithDefaultExpanded(autoFillHeaders);
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("API Sections Profiles", EditorStyles.boldLabel);
            DrawApiSectionsProfiles(apiSections, currentApiSection);
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Default API Section", EditorStyles.boldLabel);
            DrawDefaultApiSections(currentApiSection);
            
            EditorGUILayout.Space(20f);
            
            if (EditorGUI.EndChangeCheck())
            {
                requestRetry.intValue = Mathf.Clamp(requestRetry.intValue, 0, retryInterval.arraySize);
                Undo.RecordObject(Settings.targetObject, "Apply Changes");
                Settings.ApplyModifiedProperties();
            }
            else
            {
                Settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        
        private void SerializedProperty(out SerializedProperty loggingLevel, out SerializedProperty resultLoggingLevel,
            out SerializedProperty checkJsonBeforeSend, out SerializedProperty forceHttpsScheme,
            out SerializedProperty addTimeInHeader, out SerializedProperty timeKey,
            out SerializedProperty autoFillHeaders, 
            out SerializedProperty requestTimeout, out SerializedProperty longRequestTimeout,
            out SerializedProperty requestRetry, out SerializedProperty retryInterval,
            out SerializedProperty apiSections, out SerializedProperty currentApiSection)
        {
            loggingLevel = Settings.FindProperty("loggingLevel");
            resultLoggingLevel = Settings.FindProperty("resultLoggingLevel");
            checkJsonBeforeSend = Settings.FindProperty("checkJsonBeforeSend");
            forceHttpsScheme = Settings.FindProperty("forceHttpsScheme");
            addTimeInHeader = Settings.FindProperty("addTimeInHeader");
            timeKey = Settings.FindProperty("timeKey");
            autoFillHeaders = Settings.FindProperty("autoFillHeaders");
            requestTimeout = Settings.FindProperty("requestTimeout");
            longRequestTimeout = Settings.FindProperty("longRequestTimeout");
            requestRetry = Settings.FindProperty("requestRetry");
            retryInterval = Settings.FindProperty("retryInterval");
            apiSections = Settings.FindProperty("apiSections");
            currentApiSection = Settings.FindProperty("currentApiSection");
        }

        private void DrawFixWidthProperty(SerializedProperty property)
        {
            if (property == null) return;
            EditorGUILayout.PropertyField(property, GUILayout.Width(FIXED_PROPERTY_WIDTH));
        } 
        
        private void DrawHeadersArrayWithDefaultExpanded(SerializedProperty arrayProperty)
        {
            if (!arrayProperty.isArray) return;
            
            EditorGUILayout.BeginHorizontal();
            
            var fieldLabel = $"{arrayProperty.displayName} ({arrayProperty.arraySize})";
            EditorGUILayout.LabelField(fieldLabel, GUILayout.Width(160));
            
            if (GUILayout.Button("+", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
            {
                GUI.FocusControl(null);
                arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
            }

            if (GUILayout.Button("-", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
            {
                GUI.FocusControl(null);
                arrayProperty.DeleteArrayElementAtIndex(arrayProperty.arraySize - 1);
                if (arrayProperty.arraySize == 0) arrayProperty.arraySize = 1;
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key", GUILayout.Width(FIXED_KEY_WIDTH));
            EditorGUILayout.LabelField("Value", GUILayout.Width(EXPENDED_VALUE_WIDTH));
            EditorGUILayout.EndHorizontal();
            
            for (var i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();

                var key = element.FindPropertyRelative("key");
                var value = element.FindPropertyRelative("value");
                
                EditorGUILayout.PropertyField(key, GUIContent.none, GUILayout.Width(FIXED_KEY_WIDTH));
                EditorGUILayout.PropertyField(value, GUIContent.none, GUILayout.Width(EXPENDED_VALUE_WIDTH));

                if (GUILayout.Button("-", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
                {
                    GUI.FocusControl(null);
                    arrayProperty.DeleteArrayElementAtIndex(i);
                    if (arrayProperty.arraySize == 0) arrayProperty.arraySize = 1;
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawArrayWithDefaultExpanded(SerializedProperty arrayProperty)
        {
            if (!arrayProperty.isArray) return;
            
            EditorGUILayout.BeginHorizontal();
            
            var fieldLabel = $"{arrayProperty.displayName} ({arrayProperty.arraySize})";
            EditorGUILayout.LabelField(fieldLabel, GUILayout.Width(160));
            
            if (GUILayout.Button("+", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
            {
                GUI.FocusControl(null);
                arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
            }
            if (GUILayout.Button("-", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
            {
                GUI.FocusControl(null);
                arrayProperty.DeleteArrayElementAtIndex(arrayProperty.arraySize - 1);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            
            for (var i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.PropertyField(element, GUIContent.none, GUILayout.Width(200));

                if (GUILayout.Button("-", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
                {
                    GUI.FocusControl(null);
                    arrayProperty.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawApiSectionsProfiles(SerializedProperty profiles, SerializedProperty currentProfile)
        {
            if (!profiles.isArray) return;
            
            EditorGUILayout.BeginHorizontal();
            
            var fieldLabel = $"{profiles.displayName} ({profiles.arraySize})";
            EditorGUILayout.LabelField(fieldLabel, GUILayout.Width(160));
            
            if (GUILayout.Button("+", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
            {
                GUI.FocusControl(null);
                profiles.InsertArrayElementAtIndex(profiles.arraySize);
            }
            if (GUILayout.Button("-", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
            {
                GUI.FocusControl(null);
                profiles.DeleteArrayElementAtIndex(profiles.arraySize - 1);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            
            for (var i = 0; i < profiles.arraySize; i++)
            {
                var element = profiles.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.PropertyField(element, GUIContent.none, GUILayout.Width(360));

                if (GUILayout.Button("-", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
                {
                    GUI.FocusControl(null);
                    profiles.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                if (GUILayout.Button("*", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
                {
                    GUI.FocusControl(null);
                    currentProfile.boxedValue = element.boxedValue;
                }

                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
        }

        private void DrawDefaultApiSections(SerializedProperty currentApiSection)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(true);
            if (currentApiSection.boxedValue != null)
            {
                var profile = currentApiSection.objectReferenceValue as ApiSectionsProfile;
                var profileSo = new SerializedObject(profile);
                var apiSection = profileSo.FindProperty("sectionName");
                var apiUrl = profileSo.FindProperty("apiUrl");
                DrawFixWidthProperty(apiSection);
                DrawFixWidthProperty(apiUrl);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Clear Current Section", GUILayout.Width(200)))
                    currentApiSection.objectReferenceValue = null;
            }
            else
            {
                EditorGUILayout.LabelField("Current Not Set", EditorStyles.boldLabel);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.indentLevel--;
        }
    }
}