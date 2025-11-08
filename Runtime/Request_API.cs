using System.Collections.Generic;
using AceLand.WebRequest.ProjectSetting;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public static string CurrentSection => 
            Settings.CurrentSection;
        public static string ApiUrl => 
            Settings.ApiUrl;
        public static IEnumerable<HeaderData> DefaultHeaders => 
            Settings.DefaultHeaders();
        public static IEnumerable<HeaderData> SectionHeaders(string sectionName) =>
            Settings.SectionHeaders(sectionName);
    }
}