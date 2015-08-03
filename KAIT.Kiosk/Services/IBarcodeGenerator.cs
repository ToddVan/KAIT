using System.Threading.Tasks;
using System.Drawing;

namespace KAIT.Common.Interfaces
{
    public interface IBarcodeGenerator
    {
        Task<Image> GetBarcodeAsync(string data);
    }
}
