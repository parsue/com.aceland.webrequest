using System;
using AceLand.Library.Extensions;
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
        
        public bool IsEmpty => key.IsNullOrEmptyOrWhiteSpace() || value.IsNullOrEmptyOrWhiteSpace();
        public string Key => key;
        public string Value => value;
    }
}