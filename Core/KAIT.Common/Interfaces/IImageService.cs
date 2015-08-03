using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace KAIT.Common.Interfaces
{
    public interface IImageService
    {
        WriteableBitmap Photo { get; }
        void Start();
        void Stop();
        Task<string> TakePhoto(bool stop = false);
    }
}
