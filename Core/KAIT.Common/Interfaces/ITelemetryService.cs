using KAIT.Common.Services.Messages;
using System.Threading.Tasks;

namespace KAIT.Common.Services.Communication
{
    public interface ITelemetryService
    {
        Task OpenAsync();
        Task SendAsync<T>(T payload) where T : ITrackingData;
    }
}
