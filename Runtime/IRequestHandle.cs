using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AceLand.WebRequest
{
    public interface IRequestHandle
    {
        HttpResponseMessage Response { get; }
        JToken Result { get; }
        Task<T> Send<T>();
        Task<JToken> Send();
        void Cancel();
        void Dispose();
    }
}