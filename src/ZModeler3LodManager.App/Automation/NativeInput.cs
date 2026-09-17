using System.Runtime.InteropServices;

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
    private const uint MouseEventFLeftDown = 0x0002;
    private const uint MouseEventFLeftUp = 0x0004;
    private const ushort VkMenu = 0x12; // Alt

    public static void AltLeftClick(int screenX, int screenY)
    {
        SetCursorPos(screenX, screenY);

        var inputs = new[]
        {
            KeyDown(VkMenu),
            MouseDown(),
            MouseUp(),
            KeyUp(VkMenu),
        };

        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
        {
            throw new InvalidOperationException($"SendInput failed (Win32 error {Marshal.GetLastWin32Error()}).");
        }
    }

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
