using AceLand.EventDriven.Bus;
using AceLand.WebRequest.Exceptions;

namespace AceLand.WebRequest.Events
{
    public interface IServerErrorEvent : IEvent<ServerErrorException>
    {
        void OnServerError(ServerErrorException serverErrorException);
    }
}