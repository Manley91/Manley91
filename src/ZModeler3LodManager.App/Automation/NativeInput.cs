using System.Runtime.InteropServices;
using System.Threading;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Raw Win32 input synthesis (SendInput) for interactions UI Automation patterns can't express
/// on their own - specifically, ZModeler3's rename trigger is Alt+left-click, a modifier-held
/// mouse click, which has no equivalent automation pattern.
/// </summary>
internal static class NativeInput
{
    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint KeyEventFKeyUp = 0x0002;
    private const uint KeyEventFUnicode = 0x0004;
    private const uint MouseEventFLeftDown = 0x0002;
    private const uint MouseEventFLeftUp = 0x0004;
    private const ushort VkMenu = 0x12; // Alt
    private const ushort VkControl = 0x11;
    private const ushort VkReturn = 0x0D;
    private const ushort VkEscape = 0x1B;
    private const ushort VkA = 0x41;

    public static void SendEscape() => Send(KeyDown(VkEscape), KeyUp(VkEscape));

    public static void AltLeftClick(int screenX, int screenY)
    {
        SetCursorPos(screenX, screenY);
        Send(KeyDown(VkMenu), MouseDown(), MouseUp(), KeyUp(VkMenu));
    }

    /// <summary>Current system cursor position, so callers can restore it after a click.</summary>
    public static (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out var point);
        return (point.X, point.Y);
    }

    public static void MoveCursorTo(int screenX, int screenY) => SetCursorPos(screenX, screenY);

    /// <summary>Select-all (Ctrl+A) then type <paramref name="text"/> then Enter, in whatever
    /// control currently has focus. Characters are sent as raw Unicode (KEYEVENTF_UNICODE), so
    /// this works regardless of keyboard layout and for characters with no virtual-key code.</summary>
    public static void SelectAllTypeAndCommit(string text)
    {
        Send(KeyDown(VkControl), KeyDown(VkA), KeyUp(VkA), KeyUp(VkControl));
        Thread.Sleep(80);

        foreach (var ch in text)
        {
            Send(UnicodeKeyDown(ch), UnicodeKeyUp(ch));
        }

        Thread.Sleep(80);
        Send(KeyDown(VkReturn), KeyUp(VkReturn));
    }

    private static void Send(params Input[] inputs)
    {
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
        {
            throw new InvalidOperationException($"SendInput failed (Win32 error {Marshal.GetLastWin32Error()}).");
        }
    }

    private static Input UnicodeKeyDown(char ch) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = 0, ScanCode = ch, Flags = KeyEventFUnicode } },
    };

    private static Input UnicodeKeyUp(char ch) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = 0, ScanCode = ch, Flags = KeyEventFUnicode | KeyEventFKeyUp } },
    };

    private static Input KeyDown(ushort vk) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = vk, Flags = 0 } },
    };

    private static Input KeyUp(ushort vk) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = vk, Flags = KeyEventFKeyUp } },
    };

    private static Input MouseDown() => new()
    {
        Type = InputMouse,
        Data = new InputUnion { Mouse = new MouseInput { Flags = MouseEventFLeftDown } },
    };

    private static Input MouseUp() => new()
    {
        Type = InputMouse,
        Data = new InputUnion { Mouse = new MouseInput { Flags = MouseEventFLeftUp } },
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInputStructure);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out Point point);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
