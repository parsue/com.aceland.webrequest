using System;
using System.Collections.Generic;

namespace AceLand.WebRequest.Editor.ProjectSettingsProvider
{
    [Serializable]
    internal class ApiSectionData
    {
        private readonly Dictionary<string, ApiSection> _sections = new();

        public ApiSection this[string key] => _sections[key];
        public int Count => _sections.Count;

        public IEnumerable<(string section, ApiSection api)> Get()
        {
            if (Count == 0) yield break;

            foreach (var keyValue in _sections)
                yield return (keyValue.Key, keyValue.Value);
        }
        
        public void AddSection(string section, string domain, string version) =>
            _sections.TryAdd(section, new ApiSection{ domain = domain, version = version});

        public void RemoveSection(string section) =>
            _sections.Remove(section);

        public void Clear() => _sections.Clear();
    }
}