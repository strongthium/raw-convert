using System.Runtime.InteropServices;

namespace Strongthium.RawConvert;

/// <summary>An image of 8-bit red, green and blue, a row after another from the top, with no padding.</summary>
internal sealed record RgbImage(int Width, int Height, byte[] Pixels)
{
    public int Stride => Width * 3;
}

/// <summary>
/// Decodes a file with the Windows Imaging Component, Windows' own image decoders. A CR2 is read by Microsoft's Raw
/// Image Extension, which develops the raw sensor data (demosaic, white balance, colour) into an sRGB picture. It gives
/// back the picture as the camera's sensor holds it: not turned by the camera's orientation tag.
/// </summary>
internal static class Wic
{
    private static readonly Guid FactoryClass = new("cacaf262-9370-4615-a13b-9f5539da4c0a");
    private static readonly Guid Rgb24 = new("6fddc324-4e03-4bfe-b185-3d77768dc90d");
    private const uint GenericRead = 0x80000000;

    /// <summary>WINCODEC_ERR_COMPONENTNOTFOUND: Windows has no decoder for the file.</summary>
    public const int NoDecoder = unchecked((int)0x88982F50);

    /// <summary>WINCODEC_ERR_UNKNOWNIMAGEFORMAT: no decoder recognised what's in the file.</summary>
    public const int UnknownFormat = unchecked((int)0x88982F07);

    public static RgbImage Decode(string path)
    {
        var factory = (IWICImagingFactory)Activator.CreateInstance(Type.GetTypeFromCLSID(FactoryClass, throwOnError: true)!)!;
        IWICBitmapDecoder? decoder = null;
        IWICBitmapFrameDecode? frame = null;
        IWICFormatConverter? converter = null;
        try
        {
            decoder = factory.CreateDecoderFromFilename(path, IntPtr.Zero, GenericRead, 0);
            frame = decoder.GetFrame(0);
            frame.GetSize(out var width, out var height);
            converter = factory.CreateFormatConverter();
            var format = Rgb24;
            converter.Initialize(frame, ref format, 0, IntPtr.Zero, 0, 0);
            var stride = checked((int)width * 3);
            var pixels = GC.AllocateUninitializedArray<byte>(checked(stride * (int)height));
            converter.CopyPixels(IntPtr.Zero, (uint)stride, (uint)pixels.Length, pixels);
            return new RgbImage((int)width, (int)height, pixels);
        }
        finally
        {
            // The decoder holds the whole raw file and its developed picture: let it go now, not when the GC gets to it.
            Release(converter);
            Release(frame);
            Release(decoder);
            Release(factory);
        }
    }

    private static void Release(object? com)
    {
        if (com is not null) Marshal.FinalReleaseComObject(com);
    }

    // The interfaces of wincodec.h, each with its methods in the order of its vtable. A method that's never called here
    // keeps its place with no parameters. An array is a plain pointer to its bytes, which COM interop would otherwise
    // pass as a SAFEARRAY. COM interop doesn't follow inheritance, so IWICBitmapSource's methods are
    // repeated in the interfaces built on it.

    [ComImport, Guid("ec5ec8a9-c395-4314-9c77-54d7a935ff70"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICImagingFactory
    {
        IWICBitmapDecoder CreateDecoderFromFilename([MarshalAs(UnmanagedType.LPWStr)] string filename, IntPtr vendor, uint access, int options);
        void CreateDecoderFromStream();
        void CreateDecoderFromFileHandle();
        void CreateComponentInfo();
        void CreateDecoder();
        void CreateEncoder();
        void CreatePalette();
        IWICFormatConverter CreateFormatConverter();
    }

    [ComImport, Guid("9edde9e7-8dee-47ea-99df-e6faf2ed44bf"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICBitmapDecoder
    {
        void QueryCapability();
        void Initialize();
        void GetContainerFormat();
        void GetDecoderInfo();
        void CopyPalette();
        void GetMetadataQueryReader();
        void GetPreview();
        void GetColorContexts();
        void GetThumbnail();
        void GetFrameCount();
        IWICBitmapFrameDecode GetFrame(uint index);
    }

    [ComImport, Guid("00000120-a8f2-4877-ba0a-fd2b6645fb94"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICBitmapSource
    {
        void GetSize(out uint width, out uint height);
        void GetPixelFormat();
        void GetResolution();
        void CopyPalette();
        void CopyPixels(IntPtr rectangle, uint stride, uint size, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buffer);
    }

    [ComImport, Guid("3b16811b-6a43-4ec9-a813-3d930c13b940"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICBitmapFrameDecode : IWICBitmapSource
    {
        new void GetSize(out uint width, out uint height);
        new void GetPixelFormat();
        new void GetResolution();
        new void CopyPalette();
        new void CopyPixels(IntPtr rectangle, uint stride, uint size, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buffer);
        void GetMetadataQueryReader();
        void GetColorContexts();
        void GetThumbnail();
    }

    [ComImport, Guid("00000301-a8f2-4877-ba0a-fd2b6645fb94"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICFormatConverter : IWICBitmapSource
    {
        new void GetSize(out uint width, out uint height);
        new void GetPixelFormat();
        new void GetResolution();
        new void CopyPalette();
        new void CopyPixels(IntPtr rectangle, uint stride, uint size, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buffer);
        void Initialize(IWICBitmapSource source, ref Guid format, int dither, IntPtr palette, double alphaThreshold, int paletteTranslate);
        void CanConvert();
    }
}
