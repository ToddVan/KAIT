using Microsoft.Kinect.Face;

namespace Extensions
{
    public static class RectlExtensions
    {
        public static double Width (this RectI rect)
        {
            return rect.Right - rect.Left;
        }

        public static double Height(this RectI rect)
        {
            return rect.Bottom - rect.Top;
        }
    }
}
