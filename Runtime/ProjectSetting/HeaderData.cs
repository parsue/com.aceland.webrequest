using System;
using UnityEngine;

namespace AceLand.WebRequest.ProjectSetting
{
    [Serializable]
    public class HeaderData
    {
        public HeaderData(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
        
        [SerializeField] private string key;
        [SerializeField] private string value;
        
        public bool IsEmpty => string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value);
        public string Key => key;
        public string Value => value;
    }
}