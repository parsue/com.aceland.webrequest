using AceLand.WebRequest.Profiles;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public static ApiSectionsProfile DefaultSection =>
            Settings.DefaultSection;
        
        public static string DefaultApiUrl => 
            Settings.DefaultSection.ApiUrl;
    }
}