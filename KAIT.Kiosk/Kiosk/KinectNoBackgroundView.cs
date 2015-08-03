using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KAIT.Kiosk.Kiosk
{
    public class KinectNoBackgroundView
    {

        [DllImport("BackgroundRemoval")]
        public static extern int brInitialize(IntPtr kinectSensor, out int textureWidth, out int textureHeight);

        [DllImport("BackgroundRemoval")]
        public static extern int brGetNextAvailableFrame(byte[] textureData, int textureDataLen);

        [DllImport("BackgroundRemoval")]
        public static extern void brRelease();

        private int width = 0;
        private int height = 0;

        private bool bStarted = false;
        private byte[] displayPixels = null;

        public void Start()
        {
            int result = brInitialize(IntPtr.Zero, out width, out height);
            if (result > 0)
                bStarted = true;
            Debug.WriteLine("^^^BackgroundRenderFrame: " + bStarted);
            displayPixels = new byte[4 * width * height];
            Array.Clear(displayPixels, 0, displayPixels.Length);
        }

        public byte[] RenderFrame()
        {
            if (!bStarted)
                return null;

            if (brGetNextAvailableFrame(displayPixels, displayPixels.Length) != 0)
            {
                return displayPixels;
            }

            return null;
        }

        public void Stop()
        {
            if (!bStarted)
                brRelease();
        }
    }

}
