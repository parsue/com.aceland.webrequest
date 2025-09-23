using System.IO;
using AceLand.Library.CSV;
using AceLand.Library.Editor.Providers;
using AceLand.Library.Extensions;
using AceLand.Library.Models;
using AceLand.WebRequest.ProjectSetting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AceLand.WebRequest.Editor.ProjectSettingsProvider
{
    public class AceLandWebRequestSettingsProvider : AceLandSettingsProvider
    {
        public const string SETTINGS_NAME = "Project/AceLand Packages/Web Request";
        
        private const string EDITOR_PATH = "Assets/Editor";
        private const string ACELAND_FOLDER = "AceLand";
        private const string API_DATA_ASSET = "api_sections.asset";
        private const int FIXED_PROPERTY_WIDTH = 400;
        private const int FIXED_SECTION_WIDTH = 80;
        private const int FIXED_DOMAIN_WIDTH = 220;
        private const int FIXED_VERSION_WIDTH = 60;
        private const int FIXED_SMALL_BUTTON_WIDTH = 18;
        private const int FIXED_NORMAL_BUTTON_WIDTH = 80;
        private const int FIXED_KEY_WIDTH = 120;
        private const int FIXED_VALUE_WIDTH = 200;
        
        private string FileFolder => $"{EDITOR_PATH}/{ACELAND_FOLDER}";
        private string AssetPath => $"{FileFolder}/{API_DATA_ASSET}";
        private bool FolderExist => Directory.Exists(FileFolder);
        private bool AssetExist => File.Exists(AssetPath);
        
        private ApiSectionData _oriSection = new();
        private ApiSectionData _section = new();
        private string _newSection; 
        private string _newDomain; 
        private string _newVersion; 

        private AceLandWebRequestSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            Settings = AceLandWebRequestSettings.GetSerializedSettings();
            InitApiSections();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new AceLandWebRequestSettingsProvider(SETTINGS_NAME, SettingsScope.Project);
            return provider;
        }

        private void InitApiSections()
        {
            if (!FolderExist || !AssetExist) return;

            var apiAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath);
            
            if (apiAsset.text.IsNullOrEmptyOrWhiteSpace()) return;

            CreateSectionFromCvs(apiAsset.ReadAsCsvData());
        }

        public override void OnGUI(string searchContext)
        {
            SerializedProperty(out var loggingLevel, out var resultLoggingLevel,
                out var checkJsonBeforeSend, out var forceHttpsScheme,
                out var addTimeInHeader, out var timeKey,
                out var autoFillHeaders,
                out var requestTimeout, out var longRequestTimeout,
                out var requestRetry, out var retryInterval,
                out var apiUrl,
                out var apiSection, out var apiDomain, out var apiVersion);

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
            EditorGUILayout.LabelField("Header Auto Fill", EditorStyles.boldLabel);
            DrawFixWidthProperty(addTimeInHeader);
            if (addTimeInHeader.boolValue)
                DrawFixWidthProperty(timeKey);
            DrawHeadersArrayWithDefaultExpanded(autoFillHeaders);
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Request Options (time unit: ms)", EditorStyles.boldLabel);
            DrawFixWidthProperty(requestTimeout);
            DrawFixWidthProperty(longRequestTimeout);
            requestRetry.intValue = EditorGUILayout.IntSlider(requestRetry.displayName, requestRetry.intValue,
                0, retryInterval.arraySize, GUILayout.Width(FIXED_PROPERTY_WIDTH));
            DrawArrayWithDefaultExpanded(retryInterval);
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Current API Section", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawApiTitle();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(apiSection.stringValue, GUILayout.Width(FIXED_SECTION_WIDTH));
            EditorGUILayout.TextField(apiDomain.stringValue, GUILayout.Width(FIXED_DOMAIN_WIDTH));
            EditorGUILayout.TextField(apiVersion.stringValue, GUILayout.Width(FIXED_VERSION_WIDTH));
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("-", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
                ClearCurrentApiSection(apiSection, apiDomain, apiVersion);
            EditorGUILayout.EndHorizontal();
            var api = (apiDomain.stringValue.IsNullOrEmptyOrWhiteSpace() ? "" : $"{apiDomain.stringValue}") +
                               (apiVersion.stringValue.IsNullOrEmptyOrWhiteSpace() ? "" : $"/{apiVersion.stringValue}");
            apiUrl.stringValue = api;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Api Url", GUILayout.Width(64));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("https://" + apiUrl.stringValue, GUILayout.Width(FIXED_PROPERTY_WIDTH - 64 - 14));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("API Section Editor", EditorStyles.boldLabel);
            if (!FolderExist || !AssetExist) DrawCreateApiContent();
            else DrawApiContent(apiSection, apiDomain, apiVersion);
            
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

        private void ClearCurrentApiSection(SerializedProperty section, SerializedProperty domain, SerializedProperty version)
        {
            section.stringValue = string.Empty;
            domain.stringValue = string.Empty;
            version.stringValue = string.Empty;
        }

        private void DrawCreateApiContent()
        {
            if (GUILayout.Button("Create API Section File", GUILayout.Width(200)))
                CreateApiDataFile();
            
            EditorGUILayout.LabelField($"** File will be created in {AssetPath}");
        }

        private void DrawApiContent(SerializedProperty apiSection, SerializedProperty apiDomain, SerializedProperty apiVersion)
        {
            EditorGUI.indentLevel++;
            
            DrawApiTitle();
            DrawApiSections(apiSection, apiDomain, apiVersion);

            EditorGUILayout.BeginHorizontal();
            _newSection = EditorGUILayout.TextField(_newSection, GUILayout.Width(FIXED_SECTION_WIDTH));
            _newDomain = EditorGUILayout.TextField(_newDomain, GUILayout.Width(FIXED_DOMAIN_WIDTH));
            _newVersion = EditorGUILayout.TextField(_newVersion, GUILayout.Width(FIXED_VERSION_WIDTH));
            if (GUILayout.Button("+", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
                AddApiSection();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Width(FIXED_NORMAL_BUTTON_WIDTH)))
            {
                GUI.FocusControl(null);
                OverwriteTextAsset();
            }
            if (GUILayout.Button("Restore", GUILayout.Width(FIXED_NORMAL_BUTTON_WIDTH)))
            {
                GUI.FocusControl(null);
                _section = _oriSection.DeepClone();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"** File will located in {AssetPath}");
            EditorGUI.indentLevel--;
        }

        private void DrawApiTitle()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Section", GUILayout.Width(FIXED_SECTION_WIDTH));
            EditorGUILayout.LabelField("Domain", GUILayout.Width(FIXED_DOMAIN_WIDTH));
            EditorGUILayout.LabelField("Ver", GUILayout.Width(FIXED_VERSION_WIDTH));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawApiSections(SerializedProperty apiSection, SerializedProperty apiDomain, SerializedProperty apiVersion)
        {
            foreach (var (section, api) in _section.Get())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(section, GUILayout.Width(80));
                EditorGUI.EndDisabledGroup();
                api.domain = EditorGUILayout.TextField(api.domain, GUILayout.Width(220));
                api.version = EditorGUILayout.TextField(api.version, GUILayout.Width(60));
                if (GUILayout.Button("-", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
                {
                    RemoveApiSection(section);
                    break;
                }
                if (GUILayout.Button("*", GUILayout.Width(FIXED_SMALL_BUTTON_WIDTH)))
                {
                    GUI.FocusControl(null);
                    apiSection.stringValue = VerifyDomainText(section);
                    apiDomain.stringValue = VerifyDomainText(api.domain);
                    apiVersion.stringValue = VerifyDomainText(api.version);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFixWidthProperty(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, GUILayout.Width(FIXED_PROPERTY_WIDTH));
        } 
        
        private void SerializedProperty(out SerializedProperty loggingLevel, out SerializedProperty resultLoggingLevel,
            out SerializedProperty checkJsonBeforeSend, out SerializedProperty forceHttpsScheme,
            out SerializedProperty addTimeInHeader, out SerializedProperty timeKey,
            out SerializedProperty autoFillHeaders, 
            out SerializedProperty requestTimeout, out SerializedProperty longRequestTimeout,
            out SerializedProperty requestRetry, out SerializedProperty retryInterval,
            out SerializedProperty apiUrl, 
            out SerializedProperty apiSection, out SerializedProperty apiDomain, out SerializedProperty apiVersion)
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
            apiUrl = Settings.FindProperty("apiUrl");
            apiSection = Settings.FindProperty("apiSection");
            apiDomain = Settings.FindProperty("apiDomain");
            apiVersion = Settings.FindProperty("apiVersion");
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
            EditorGUILayout.LabelField("Value", GUILayout.Width(FIXED_VALUE_WIDTH));
            EditorGUILayout.EndHorizontal();
            
            for (var i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();

                var key = element.FindPropertyRelative("key");
                var value = element.FindPropertyRelative("value");
                
                EditorGUILayout.PropertyField(key, GUIContent.none, GUILayout.Width(FIXED_KEY_WIDTH));
                EditorGUILayout.PropertyField(value, GUIContent.none, GUILayout.Width(FIXED_VALUE_WIDTH));

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

        private void CreateApiDataFile()
        {
            if (!AssetDatabase.IsValidFolder(EDITOR_PATH))
                AssetDatabase.CreateFolder("Assets", "Editor");
            if (!AssetDatabase.IsValidFolder(FileFolder))
                AssetDatabase.CreateFolder(EDITOR_PATH, ACELAND_FOLDER);

            AssetDatabase.CreateAsset(new TextAsset(string.Empty), AssetPath);
            Debug.Log($"API Section File is created : {AssetPath}");
        }

        private void AddApiSection()
        {
            GUI.FocusControl(null);
            
            if (_newSection.IsNullOrEmptyOrWhiteSpace())
            {
                Debug.LogWarning("Section cannot be empty");
                return;
            }
            if (_newDomain.IsNullOrEmptyOrWhiteSpace())
            {
                Debug.LogWarning("Domain cannot be empty");
                return;
            }
            
            _newSection = VerifyDomainText(_newSection);
            _newDomain = VerifyDomainText(_newDomain);
            _newVersion = VerifyDomainText(_newVersion);
            
            _section.AddSection(_newSection, _newDomain, _newVersion);
            _newSection = string.Empty;
            _newDomain = string.Empty;
            _newVersion = string.Empty;
        }

        private void RemoveApiSection(string section)
        {
            GUI.FocusControl(null);
            if (section.IsNullOrEmptyOrWhiteSpace()) return;
            
            _section.RemoveSection(section);
        }

        private string VerifyDomainText(string text)
        {
            text = text.Replace(" ", "");
            text = text.Replace('\\', '/');
            text = text.Replace("https:", "");
            text = text.Replace("http:", "");
            text = text.Trim(':');
            text = text.Trim('/');

            return text;
        }

        private void OverwriteTextAsset()
        {
            _oriSection = _section.DeepClone();
            var csv = CreateCvsFromData(_section);
            AssetDatabase.CreateAsset(new TextAsset(csv), AssetPath);
            Debug.Log("API Section Data is updated.");
        }

        private void CreateSectionFromCvs(CsvData csv)
        {
            _section.Clear();
            
            foreach (var line in csv.Lines)
            {
                if (line.Length != 3) continue;

                var section = line[0];
                var domain = line[1];
                var version = line[2];
                
                _section.AddSection(section, domain, version);
            }

            _oriSection = _section.DeepClone();
        }

        private string CreateCvsFromData(ApiSectionData data)
        {
            var csv = string.Empty;
            foreach (var (section, api) in data.Get())
            {
                if (!csv.IsNullOrEmptyOrWhiteSpace())
                    csv += "\n";
                csv += $"{VerifyDomainText(section)},{VerifyDomainText(api.domain)},{VerifyDomainText(api.version)}";
            }

            return csv;
        }
    }
}