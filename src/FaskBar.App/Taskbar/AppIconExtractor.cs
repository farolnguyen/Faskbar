using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FaskBar.App.Taskbar;

/// <summary>
/// Lay icon thuc cua mot app qua "shell:AppsFolder\<AppId>" + IShellItemImageFactory.
/// Hoat dong cho ca app exe thong thuong va app UWP, vi AppsFolder gop chung theo AppUserModelID.
/// </summary>
public static class AppIconExtractor
{
    public static BitmapSource? TryGetIcon(string appId, int size = 32)
    {
        var parsingName = $@"shell:AppsFolder\{appId}";

        var hr = NativeMethods.SHCreateItemFromParsingName(
            parsingName, IntPtr.Zero, typeof(NativeMethods.IShellItemImageFactory).GUID, out var factoryObj);

        if (hr != 0 || factoryObj is not NativeMethods.IShellItemImageFactory factory)
        {
            return null;
        }

        try
        {
            var hr2 = factory.GetImage(
                new NativeMethods.SIZE { cx = size, cy = size },
                NativeMethods.SIIGBF.SIIGBF_ICONONLY | NativeMethods.SIIGBF.SIIGBF_RESIZETOFIT,
                out var hBitmap);

            if (hr2 != 0 || hBitmap == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(factory);
        }
    }

    private static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHCreateItemFromParsingName(
            string pszPath,
            IntPtr pbc,
            Guid riid,
            out object ppv);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
        }

        [Flags]
        public enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10,
        }

        [ComImport]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
        }
    }
}
