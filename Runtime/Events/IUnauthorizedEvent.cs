using AceLand.EventDriven.Bus;
using AceLand.WebRequest.Exceptions;

namespace AceLand.WebRequest.Events
{
    public interface IUnauthorizedEvent : IEvent<UnauthorizedException>
    {
        void OnUnauthorized(UnauthorizedException unauthorizedException);
    }
}