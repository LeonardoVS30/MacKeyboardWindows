using System;
using System.Runtime.InteropServices;

namespace MacKeyboardWindows.Services
{
    // Enumeración completa de los Códigos de Tecla Virtuales de Windows
    public enum VirtualKeyCode : ushort
    {
        // Letters
        VK_A = 0x41, VK_B = 0x42, VK_C = 0x43, VK_D = 0x44, VK_E = 0x45, VK_F = 0x46, VK_G = 0x47, VK_H = 0x48, VK_I = 0x49, VK_J = 0x4A, VK_K = 0x4B, VK_L = 0x4C, VK_M = 0x4D, VK_N = 0x4E, VK_O = 0x4F, VK_P = 0x50, VK_Q = 0x51, VK_R = 0x52, VK_S = 0x53, VK_T = 0x54, VK_U = 0x55, VK_V = 0x56, VK_W = 0x57, VK_X = 0x58, VK_Y = 0x59, VK_Z = 0x5A,

        // Digits
        VK_0 = 0x30, VK_1 = 0x31, VK_2 = 0x32, VK_3 = 0x33, VK_4 = 0x34, VK_5 = 0x35, VK_6 = 0x36, VK_7 = 0x37, VK_8 = 0x38, VK_9 = 0x39,

        // Function Keys
        F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74, F6 = 0x75, F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,

        // Special Keys
        LSHIFT = 0xA0, RSHIFT = 0xA1, LCONTROL = 0xA2, RCONTROL = 0xA3, LMENU = 0xA4, RMENU = 0xA5, LWIN = 0x5B, RWIN = 0x5C,
        RETURN = 0x0D, // Enter
        ESCAPE = 0x1B,
        BACK = 0x08,   // Backspace
        TAB = 0x09,
        CAPITAL = 0x14, // Caps Lock
        SPACE = 0x20,
        PRINT_SCREEN = 0x2C,
        SCROLL = 0x91,
        PAUSE = 0x13,

        // OEM Keys
        OEM_1 = 0xBA,      // ';:' for US
        OEM_PLUS = 0xBB,   // '+'
        OEM_COMMA = 0xBC,  // ','
        OEM_MINUS = 0xBD,  // '-'
        OEM_PERIOD = 0xBE, // '.'
        OEM_2 = 0xBF,      // '/?'
        OEM_3 = 0xC0,      // '`~'
        OEM_4 = 0xDB,      // '[{'
        OEM_5 = 0xDC,      // '\|'
        OEM_6 = 0xDD,      // ']}'
        OEM_7 = 0xDE,      // ''"'
        OEM_102 = 0xE2,    // '<>' en teclados no US // <-- AÑADE ESTA LÍNEA
    }

    public class KeyboardService
    {
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
        // --- FIN DE LAS ESTRUCTURAS A AÑADIR ---

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public void SimulateKeyPress(VirtualKeyCode keyCode)
        {
            var inputs = new INPUT[]
            {
                // Key down
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)keyCode,
                            wScan = 0,
                            dwFlags = 0,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                },
                // Key up
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)keyCode,
                            wScan = 0,
                            dwFlags = KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}