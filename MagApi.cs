using System.Runtime.InteropServices;

namespace fovia
{
    public static class MagApi
    {
        [DllImport("Magnification.dll", SetLastError = true)]
        public static extern bool MagInitialize();

        [DllImport("Magnification.dll", SetLastError = true)]
        public static extern bool MagUninitialize();

        [DllImport("Magnification.dll", SetLastError = true)]
        public static extern bool MagSetFullscreenTransform(float magLevel, int xOffset, int yOffset);

        [DllImport("Magnification.dll", SetLastError = true)]
        public static extern bool MagSetInputTransform(bool fEnabled, ref RECT pRectSource, ref RECT pRectDest);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}