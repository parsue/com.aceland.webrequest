using System.Net;
using AceLand.EventDriven.Bus;

namespace AceLand.WebRequest.Events
{
    public interface IConnectionErrorEvent : IEvent<WebException>
    {
        void OnConnectionError(WebException webException);
    }
}