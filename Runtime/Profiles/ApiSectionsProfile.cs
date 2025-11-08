using System.Collections.Generic;
using AceLand.Library.Attribute;
using AceLand.Library.Extensions;
using AceLand.WebRequest.ProjectSetting;
using UnityEngine;

namespace AceLand.WebRequest.Profiles
{
    [CreateAssetMenu(fileName = "Api Sections Profile", menuName = "AceLand/Api Sections Profile")]
    public class ApiSectionsProfile : ScriptableObject
    {
        [SerializeField] private string sectionName;
        
        [Header("Auto Fill Headers")]
        [SerializeField] private List<HeaderData> headers = new();
        
        [Header("API Domain")]
        [SerializeField] private bool useHttps;
        [SerializeField] private string domain;
        [SerializeField] private string apiVersion;
        [SerializeField, ReadOnlyField] private string apiUrl;
        
        public string SectionName => sectionName;
        public string ApiUrl => apiUrl;
        public IEnumerable<HeaderData> Headers => headers;

        private void OnValidate()
        {
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
    }
}