using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AceLand.WebRequest
{
    public interface IRequestHandle
    {
        Task<JToken> Send();
        void Cancel();
        void Dispose();
    }
}