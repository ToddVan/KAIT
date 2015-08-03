using Inception.Common.Services.Messages;

namespace Inception.Common.Services.Communication
{
    public interface ILocalCommunicationService 
    {
        void Send<T>(T payload, string remoteServer = ".", int timeout = 1000) where T: ITrackingData;
    }
}
