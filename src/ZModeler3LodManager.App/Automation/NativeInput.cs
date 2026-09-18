using System.Runtime.InteropServices;
using System.Threading;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Raw Win32 input synthesis (SendInput) for interactions UI Automation patterns can't express
/// on their own. Alt+left-click marks/selects a Scene nodes browser row (e.g. for copy/paste);
/// ZModeler3's actual rename trigger is a plain double left-click, no modifier.
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
    private const uint WmLButtonDown = 0x0201;
    private const uint WmLButtonUp = 0x0202;
    private const uint WmLButtonDblClk = 0x0203;

    public static void SendEscape() => Send(KeyDown(VkEscape), KeyUp(VkEscape));

    public static void AltLeftClick(int screenX, int screenY)
    {
        SetCursorPos(screenX, screenY);
        Send(KeyDown(VkMenu), MouseDown(), MouseUp(), KeyUp(VkMenu));
    }

    /// <summary>A genuine double left-click (two clicks well within Windows' double-click time),
    /// no modifier - confirmed to be the reliable manual rename trigger, unlike Alt+click.</summary>
    public static void DoubleLeftClick(int screenX, int screenY)
    {
        SetCursorPos(screenX, screenY);
        Send(MouseDown(), MouseUp());
        Thread.Sleep(60);
        Send(MouseDown(), MouseUp());
    }

    /// <summary>
    /// Double-click sent directly to a window as WM_LBUTTONDOWN/UP/DBLCLK/UP messages, bypassing
    /// the OS's global cursor/double-click-timer path entirely. ZModeler3 appears to be one
    /// owner-drawn window (its docked panels aren't separate child HWNDs), so this targets the
    /// top-level window handle with coordinates converted to its client space.
    /// </summary>
    public static void DoubleLeftClickMessage(IntPtr windowHandle, int screenX, int screenY)
    {
        var lParam = ToClientLParam(windowHandle, screenX, screenY);
        var mkLButton = new IntPtr(0x0001);

        SendMessage(windowHandle, WmLButtonDown, mkLButton, lParam);
        SendMessage(windowHandle, WmLButtonUp, IntPtr.Zero, lParam);
        SendMessage(windowHandle, WmLButtonDblClk, mkLButton, lParam);
        SendMessage(windowHandle, WmLButtonUp, IntPtr.Zero, lParam);
    }

    /// <summary>
    /// A burst of several rapid single left-clicks (not one clean double-click) - the user found
    /// that manually "spamming" left-click is what reliably gets ZModeler3 into rename mode when
    /// a plain double-click doesn't. This mimics that: repeated down/up pairs a beat apart, with
    /// an explicit WM_LBUTTONDBLCLK worked into the middle of the burst for good measure.
    /// </summary>
    public static void SpamLeftClicksMessage(IntPtr windowHandle, int screenX, int screenY, int clickCount = 6)
    {
        var lParam = ToClientLParam(windowHandle, screenX, screenY);
        var mkLButton = new IntPtr(0x0001);

        for (var i = 0; i < clickCount; i++)
        {
            SendMessage(windowHandle, WmLButtonDown, mkLButton, lParam);
            SendMessage(windowHandle, WmLButtonUp, IntPtr.Zero, lParam);

            if (i == clickCount / 2)
            {
                SendMessage(windowHandle, WmLButtonDblClk, mkLButton, lParam);
                SendMessage(windowHandle, WmLButtonUp, IntPtr.Zero, lParam);
            }

            Thread.Sleep(70);
        }
    }

    private static IntPtr ToClientLParam(IntPtr windowHandle, int screenX, int screenY)
    {
        var point = new Point { X = screenX, Y = screenY };
        if (!ScreenToClient(windowHandle, ref point))
        {
            throw new InvalidOperationException($"ScreenToClient failed (Win32 error {Marshal.GetLastWin32Error()}).");
        }

        return new IntPtr((point.Y << 16) | (point.X & 0xFFFF));
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ScreenToClient(IntPtr hWnd, ref Point point);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>SendMessage-based clicks bypass the normal input queue, so - unlike a real or
    /// SendInput click - they don't activate the target window on their own. Call this first so
    /// the keyboard portion (SelectAllTypeAndCommit) actually reaches ZModeler3.</summary>
    public static void ActivateWindow(IntPtr windowHandle) => SetForegroundWindow(windowHandle);

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
