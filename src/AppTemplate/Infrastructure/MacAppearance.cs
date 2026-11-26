using System.Runtime.InteropServices;

namespace AppTemplate.Infrastructure;

/// <summary>
/// On macOS the title bar follows the system appearance, not the app's theme, so a dark app on a light Mac gets a
/// light title bar. This hands the app's theme to AppKit. https://github.com/unoplatform/uno/issues/24667
/// </summary>
public static class MacAppearance
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    public static void Apply(ElementTheme theme)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var name = NSString(theme == ElementTheme.Dark ? "NSAppearanceNameDarkAqua" : "NSAppearanceNameAqua");
        var appearance = Send(objc_getClass("NSAppearance"), sel_registerName("appearanceNamed:"), name);
        var app = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
        Send(app, sel_registerName("setAppearance:"), appearance);
    }

    private static nint NSString(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            return Send(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    [DllImport(ObjC)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector, nint argument);
}
