using Inception.Common.Services.Messages;
using System.Threading.Tasks;

namespace Inception.Common.Services.Communication
{
    public interface ITelemetryService
    {
        Task OpenAsync();
        Task SendAsync<T>(T payload) where T : ITrackingData;
    }
}
