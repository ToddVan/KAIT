using System;
using System.Reactive.Subjects;

namespace KAIT.Common.Interfaces
{
    public interface IDepthService
    {
        IntPtr DepthFrameDataSize { get; }
        uint BytesPerPixel { get; }
        int PixelWidth { get;  }
        int PixelHeight { get; }
        Subject<byte[]> DepthBytes { get; }
        void Start();
        void Stop();
    }
}
