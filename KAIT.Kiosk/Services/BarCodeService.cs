using System.Threading.Tasks;
using System.Drawing;
using Zen.Barcode;
using Inception.Common.Interfaces;


namespace KinectKiosk.Services
{
    public class BarCodeService : IBarcodeGenerator
    {
        
        CodeQrBarcodeDraw _barcode = new CodeQrBarcodeDraw();

        public Task<Image> GetBarcodeAsync(string data)
        {
            return Task.Factory.StartNew<Image>(() =>
            {
                return _barcode.Draw(data, 3, 3);
            });
        }
    }
}
