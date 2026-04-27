using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using AceLand.Library.Attribute;
using AceLand.Library.Extensions;
using AceLand.WebRequest.ProjectSetting;
using UnityEngine;

namespace AceLand.WebRequest.Profiles
{
    [CreateAssetMenu(fileName = "Api Sections Profile", menuName = "AceLand/Api Sections Profile")]
    public class ApiSectionsProfile : ScriptableObject
    {
        [SerializeField, ReadOnlyField] private string sectionName;
        
        [Header("Auto Fill Headers")]
        [SerializeField] private List<HeaderData> headers = new();
        
        [Header("API Domain")]
        [SerializeField] private bool useHttps;
        [SerializeField] private string domain;
        [SerializeField] private string apiVersion;
        [SerializeField, ReadOnlyField] private string apiUrl;
        
        [Header("Private Root CA")]
        [SerializeField, ReadOnlyField] private string rootCaFingerprint;
        
        public string SectionName => sectionName;
        public string ApiUrl => apiUrl;
        public IEnumerable<HeaderData> Headers => headers;
        public string RootCaFingerprint => rootCaFingerprint;

        private void OnEnable()
        {
            sectionName = name;
        }

        private void OnValidate()
        {
            sectionName = this.name;
            
            var http = useHttps ? "https://" : "http://";
            var url = VerifyDomainText(domain);
            apiUrl = (domain.IsNullOrEmptyOrWhiteSpace() ? "" : $"{http}{url}") +
                          (apiVersion.IsNullOrEmptyOrWhiteSpace() ? "" : $"/{apiVersion}");
        }

        private string VerifyDomainText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            text = text.Replace(" ", "");
            text = text.Replace('\\', '/');
            text = text.Replace("http:", "");
            text = text.Replace("https:", "");
            text = text.Trim(':');
            text = text.Trim('/');

            return text;
        }

        [InspectorButton(Expanded = true, Mode = InspectorButtonMode.DisabledInPlayMode)]
        private void LoadRootCA(string fullPathToPemFile)
        {
            if (fullPathToPemFile.IsNullOrEmptyOrWhiteSpace())
                return;
            
            if (!File.Exists(fullPathToPemFile))
            {
                Debug.LogWarning($"{fullPathToPemFile} not found");
                return;
            }

            if (!Path.HasExtension(fullPathToPemFile) || Path.GetExtension(fullPathToPemFile) != ".pem")
            {
                Debug.LogWarning("File is not a PEM file");
                return;
            }
            
            var fileBytes = File.ReadAllBytes(fullPathToPemFile);
            var cert = new X509Certificate2(fileBytes);

            rootCaFingerprint = cert.GetCertHashString();
            
            Debug.Log($"Loaded root ca certificate: {rootCaFingerprint}");
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [InspectorButton(Mode = InspectorButtonMode.DisabledInPlayMode)]
        private void RemoveRootCA()
        {
            rootCaFingerprint = string.Empty;
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
