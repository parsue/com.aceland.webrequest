using System.Collections.Generic;
using AceLand.WebRequest.ProjectSetting;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public static string DefaultSection => 
            Settings.DefaultSection;
        public static string DefaultApiUrl => 
            Settings.DefaultApiUrl;
        public static string SectionApiUrl(string sectionName) =>
            Settings.SectionApiUrl(sectionName);
        public static IEnumerable<HeaderData> DefaultHeaders => 
            Settings.DefaultHeaders();
        public static IEnumerable<HeaderData> SectionHeaders(string sectionName) =>
            Settings.SectionHeaders(sectionName);
    }
}