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
    }
}