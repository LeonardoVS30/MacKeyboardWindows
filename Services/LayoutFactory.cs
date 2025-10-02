using MacKeyboardWindows.Models;
using Key = System.Windows.Input.Key;

namespace MacKeyboardWindows.Services
{
    public static class LayoutFactory
    {
        public static KeyboardLayout GetLayout(string name)
        {
            return name.ToUpper() switch
            {
                "ES" => GetSpanishLayout(),
                "US" => GetUSLayout(),
                _ => GetSpanishLayout(),
            };
        }

        private static KeyboardLayout GetSpanishLayout()
        {
            return new KeyboardLayout
            {
                // Fila 1 (Números) - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "º", ShiftDisplayText = "ª", KeyCode = VirtualKeyCode.OEM_3, WpfKey = Key.Oem3 },
                    new KeyModel { DisplayText = "1", ShiftDisplayText = "!", KeyCode = VirtualKeyCode.VK_1, WpfKey = Key.D1 },
                    new KeyModel { DisplayText = "2", ShiftDisplayText = "\"", KeyCode = VirtualKeyCode.VK_2, WpfKey = Key.D2 },
                    new KeyModel { DisplayText = "3", ShiftDisplayText = "·", KeyCode = VirtualKeyCode.VK_3, WpfKey = Key.D3 },
                    new KeyModel { DisplayText = "4", ShiftDisplayText = "$", KeyCode = VirtualKeyCode.VK_4, WpfKey = Key.D4 },
                    new KeyModel { DisplayText = "5", ShiftDisplayText = "%", KeyCode = VirtualKeyCode.VK_5, WpfKey = Key.D5 },
                    new KeyModel { DisplayText = "6", ShiftDisplayText = "&", KeyCode = VirtualKeyCode.VK_6, WpfKey = Key.D6 },
                    new KeyModel { DisplayText = "7", ShiftDisplayText = "/", KeyCode = VirtualKeyCode.VK_7, WpfKey = Key.D7 },
                    new KeyModel { DisplayText = "8", ShiftDisplayText = "(", KeyCode = VirtualKeyCode.VK_8, WpfKey = Key.D8 },
                    new KeyModel { DisplayText = "9", ShiftDisplayText = ")", KeyCode = VirtualKeyCode.VK_9, WpfKey = Key.D9 },
                    new KeyModel { DisplayText = "0", ShiftDisplayText = "=", KeyCode = VirtualKeyCode.VK_0, WpfKey = Key.D0 },
                    new KeyModel { DisplayText = "'", ShiftDisplayText = "?", KeyCode = VirtualKeyCode.OEM_MINUS, WpfKey = Key.OemMinus },
                    new KeyModel { DisplayText = "¡", ShiftDisplayText = "¿", KeyCode = VirtualKeyCode.OEM_PLUS, WpfKey = Key.OemPlus },
                    new KeyModel { DisplayText = "\uE756", KeyCode = VirtualKeyCode.BACK, WpfKey = Key.Back, WidthFactor = 2.0 }
                },
                // Fila 2 (QWERTY) - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "Tab", KeyCode = VirtualKeyCode.TAB, WpfKey = Key.Tab, WidthFactor = 1.5 },
                    new KeyModel { DisplayText = "q", KeyCode = VirtualKeyCode.VK_Q, WpfKey = Key.Q, IsLetter = true },
                    new KeyModel { DisplayText = "w", KeyCode = VirtualKeyCode.VK_W, WpfKey = Key.W, IsLetter = true },
                    new KeyModel { DisplayText = "e", KeyCode = VirtualKeyCode.VK_E, WpfKey = Key.E, IsLetter = true },
                    new KeyModel { DisplayText = "r", KeyCode = VirtualKeyCode.VK_R, WpfKey = Key.R, IsLetter = true },
                    new KeyModel { DisplayText = "t", KeyCode = VirtualKeyCode.VK_T, WpfKey = Key.T, IsLetter = true },
                    new KeyModel { DisplayText = "y", KeyCode = VirtualKeyCode.VK_Y, WpfKey = Key.Y, IsLetter = true },
                    new KeyModel { DisplayText = "u", KeyCode = VirtualKeyCode.VK_U, WpfKey = Key.U, IsLetter = true },
                    new KeyModel { DisplayText = "i", KeyCode = VirtualKeyCode.VK_I, WpfKey = Key.I, IsLetter = true },
                    new KeyModel { DisplayText = "o", KeyCode = VirtualKeyCode.VK_O, WpfKey = Key.O, IsLetter = true },
                    new KeyModel { DisplayText = "p", KeyCode = VirtualKeyCode.VK_P, WpfKey = Key.P, IsLetter = true },
                    new KeyModel { DisplayText = "`", ShiftDisplayText = "^", KeyCode = VirtualKeyCode.OEM_4, WpfKey = Key.Oem4 },
                    new KeyModel { DisplayText = "+", ShiftDisplayText = "*", KeyCode = VirtualKeyCode.OEM_6, WpfKey = Key.Oem6 },
                    new KeyModel { DisplayText = "Enter", KeyCode = VirtualKeyCode.RETURN, WpfKey = Key.Return, WidthFactor = 1.5 }
                },
                // Fila 3 (ASDF) - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "Bloq\nMayus", KeyCode = VirtualKeyCode.CAPITAL, WpfKey = Key.Capital, WidthFactor = 1.75 },
                    new KeyModel { DisplayText = "a", KeyCode = VirtualKeyCode.VK_A, WpfKey = Key.A, IsLetter = true },
                    new KeyModel { DisplayText = "s", KeyCode = VirtualKeyCode.VK_S, WpfKey = Key.S, IsLetter = true },
                    new KeyModel { DisplayText = "d", KeyCode = VirtualKeyCode.VK_D, WpfKey = Key.D, IsLetter = true },
                    new KeyModel { DisplayText = "f", KeyCode = VirtualKeyCode.VK_F, WpfKey = Key.F, IsLetter = true },
                    new KeyModel { DisplayText = "g", KeyCode = VirtualKeyCode.VK_G, WpfKey = Key.G, IsLetter = true },
                    new KeyModel { DisplayText = "h", KeyCode = VirtualKeyCode.VK_H, WpfKey = Key.H, IsLetter = true },
                    new KeyModel { DisplayText = "j", KeyCode = VirtualKeyCode.VK_J, WpfKey = Key.J, IsLetter = true },
                    new KeyModel { DisplayText = "k", KeyCode = VirtualKeyCode.VK_K, WpfKey = Key.K, IsLetter = true },
                    new KeyModel { DisplayText = "l", KeyCode = VirtualKeyCode.VK_L, WpfKey = Key.L, IsLetter = true },
                    new KeyModel { DisplayText = "ñ", KeyCode = VirtualKeyCode.OEM_1, WpfKey = Key.Oem1, IsLetter = true },
                    new KeyModel { DisplayText = "´", ShiftDisplayText = "¨", KeyCode = VirtualKeyCode.OEM_7, WpfKey = Key.Oem7 },
                    new KeyModel { DisplayText = "ç", ShiftDisplayText = "Ç", KeyCode = VirtualKeyCode.OEM_5, WpfKey = Key.Oem5 },
                    new KeyModel { DisplayText = "Enter", KeyCode = VirtualKeyCode.RETURN, WpfKey = Key.Return, WidthFactor = 1.25 }
                },
                // Fila 4 (ZXCV) - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "Shift", KeyCode = VirtualKeyCode.LSHIFT, WpfKey = Key.LeftShift, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "<", ShiftDisplayText = ">", KeyCode = VirtualKeyCode.OEM_102, WpfKey = Key.Oem102 },
                    new KeyModel { DisplayText = "z", KeyCode = VirtualKeyCode.VK_Z, WpfKey = Key.Z, IsLetter = true },
                    new KeyModel { DisplayText = "x", KeyCode = VirtualKeyCode.VK_X, WpfKey = Key.X, IsLetter = true },
                    new KeyModel { DisplayText = "c", KeyCode = VirtualKeyCode.VK_C, WpfKey = Key.C, IsLetter = true },
                    new KeyModel { DisplayText = "v", KeyCode = VirtualKeyCode.VK_V, WpfKey = Key.V, IsLetter = true },
                    new KeyModel { DisplayText = "b", KeyCode = VirtualKeyCode.VK_B, WpfKey = Key.B, IsLetter = true },
                    new KeyModel { DisplayText = "n", KeyCode = VirtualKeyCode.VK_N, WpfKey = Key.N, IsLetter = true },
                    new KeyModel { DisplayText = "m", KeyCode = VirtualKeyCode.VK_M, WpfKey = Key.M, IsLetter = true },
                    new KeyModel { DisplayText = ",", ShiftDisplayText = ";", KeyCode = VirtualKeyCode.OEM_COMMA, WpfKey = Key.OemComma },
                    new KeyModel { DisplayText = ".", ShiftDisplayText = ":", KeyCode = VirtualKeyCode.OEM_PERIOD, WpfKey = Key.OemPeriod },
                    new KeyModel { DisplayText = "-", ShiftDisplayText = "_", KeyCode = VirtualKeyCode.OEM_2, WpfKey = Key.Oem2 },
                    new KeyModel { DisplayText = "Shift", KeyCode = VirtualKeyCode.RSHIFT, WpfKey = Key.RightShift, WidthFactor = 2.75 }
                },
                // Fila 5 (Inferior) - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "Ctrl", KeyCode = VirtualKeyCode.LCONTROL, WpfKey = Key.LeftCtrl, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "\uE770", KeyCode = VirtualKeyCode.LWIN, WpfKey = Key.LWin, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "Alt", KeyCode = VirtualKeyCode.LMENU, WpfKey = Key.LeftAlt, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "Space", KeyCode = VirtualKeyCode.SPACE, WpfKey = Key.Space, WidthFactor = 6.25 },
                    new KeyModel { DisplayText = "Alt Gr", KeyCode = VirtualKeyCode.RMENU, WpfKey = Key.RightAlt, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "\uE770", KeyCode = VirtualKeyCode.RWIN, WpfKey = Key.RWin, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "Ctrl", KeyCode = VirtualKeyCode.RCONTROL, WpfKey = Key.RightCtrl, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "Ctrl", KeyCode = VirtualKeyCode.RCONTROL, WpfKey = Key.RightCtrl, WidthFactor = 1.25 }
                }
            };
        }

        private static KeyboardLayout GetUSLayout()
        {
            return new KeyboardLayout
            {
                // Fila 1 - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "`", ShiftDisplayText = "~", KeyCode = VirtualKeyCode.OEM_3, WpfKey = Key.Oem3 },
                    new KeyModel { DisplayText = "1", ShiftDisplayText = "!", KeyCode = VirtualKeyCode.VK_1, WpfKey = Key.D1 },
                    new KeyModel { DisplayText = "2", ShiftDisplayText = "@", KeyCode = VirtualKeyCode.VK_2, WpfKey = Key.D2 },
                    new KeyModel { DisplayText = "3", ShiftDisplayText = "#", KeyCode = VirtualKeyCode.VK_3, WpfKey = Key.D3 },
                    new KeyModel { DisplayText = "4", ShiftDisplayText = "$", KeyCode = VirtualKeyCode.VK_4, WpfKey = Key.D4 },
                    new KeyModel { DisplayText = "5", ShiftDisplayText = "%", KeyCode = VirtualKeyCode.VK_5, WpfKey = Key.D5 },
                    new KeyModel { DisplayText = "6", ShiftDisplayText = "^", KeyCode = VirtualKeyCode.VK_6, WpfKey = Key.D6 },
                    new KeyModel { DisplayText = "7", ShiftDisplayText = "&", KeyCode = VirtualKeyCode.VK_7, WpfKey = Key.D7 },
                    new KeyModel { DisplayText = "8", ShiftDisplayText = "*", KeyCode = VirtualKeyCode.VK_8, WpfKey = Key.D8 },
                    new KeyModel { DisplayText = "9", ShiftDisplayText = "(", KeyCode = VirtualKeyCode.VK_9, WpfKey = Key.D9 },
                    new KeyModel { DisplayText = "0", ShiftDisplayText = ")", KeyCode = VirtualKeyCode.VK_0, WpfKey = Key.D0 },
                    new KeyModel { DisplayText = "-", ShiftDisplayText = "_", KeyCode = VirtualKeyCode.OEM_MINUS, WpfKey = Key.OemMinus },
                    new KeyModel { DisplayText = "=", ShiftDisplayText = "+", KeyCode = VirtualKeyCode.OEM_PLUS, WpfKey = Key.OemPlus },
                    new KeyModel { DisplayText = "\uE756", KeyCode = VirtualKeyCode.BACK, WpfKey = Key.Back, WidthFactor = 2.0 }
                },
                // Fila 2 - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "Tab", KeyCode = VirtualKeyCode.TAB, WpfKey = Key.Tab, WidthFactor = 1.5 },
                    new KeyModel { DisplayText = "q", KeyCode = VirtualKeyCode.VK_Q, WpfKey = Key.Q, IsLetter = true },
                    new KeyModel { DisplayText = "w", KeyCode = VirtualKeyCode.VK_W, WpfKey = Key.W, IsLetter = true },
                    new KeyModel { DisplayText = "e", KeyCode = VirtualKeyCode.VK_E, WpfKey = Key.E, IsLetter = true },
                    new KeyModel { DisplayText = "r", KeyCode = VirtualKeyCode.VK_R, WpfKey = Key.R, IsLetter = true },
                    new KeyModel { DisplayText = "t", KeyCode = VirtualKeyCode.VK_T, WpfKey = Key.T, IsLetter = true },
                    new KeyModel { DisplayText = "y", KeyCode = VirtualKeyCode.VK_Y, WpfKey = Key.Y, IsLetter = true },
                    new KeyModel { DisplayText = "u", KeyCode = VirtualKeyCode.VK_U, WpfKey = Key.U, IsLetter = true },
                    new KeyModel { DisplayText = "i", KeyCode = VirtualKeyCode.VK_I, WpfKey = Key.I, IsLetter = true },
                    new KeyModel { DisplayText = "o", KeyCode = VirtualKeyCode.VK_O, WpfKey = Key.O, IsLetter = true },
                    new KeyModel { DisplayText = "p", KeyCode = VirtualKeyCode.VK_P, WpfKey = Key.P, IsLetter = true },
                    new KeyModel { DisplayText = "[", ShiftDisplayText = "{", KeyCode = VirtualKeyCode.OEM_4, WpfKey = Key.OemOpenBrackets },
                    new KeyModel { DisplayText = "]", ShiftDisplayText = "}", KeyCode = VirtualKeyCode.OEM_6, WpfKey = Key.OemCloseBrackets },
                    new KeyModel { DisplayText = "\\", ShiftDisplayText = "|", KeyCode = VirtualKeyCode.OEM_5, WpfKey = Key.Oem5, WidthFactor = 1.5 }
                },
                // Fila 3 - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "Caps\nLock", KeyCode = VirtualKeyCode.CAPITAL, WpfKey = Key.Capital, WidthFactor = 1.75 },
                    new KeyModel { DisplayText = "a", KeyCode = VirtualKeyCode.VK_A, WpfKey = Key.A, IsLetter = true },
                    new KeyModel { DisplayText = "s", KeyCode = VirtualKeyCode.VK_S, WpfKey = Key.S, IsLetter = true },
                    new KeyModel { DisplayText = "d", KeyCode = VirtualKeyCode.VK_D, WpfKey = Key.D, IsLetter = true },
                    new KeyModel { DisplayText = "f", KeyCode = VirtualKeyCode.VK_F, WpfKey = Key.F, IsLetter = true },
                    new KeyModel { DisplayText = "g", KeyCode = VirtualKeyCode.VK_G, WpfKey = Key.G, IsLetter = true },
                    new KeyModel { DisplayText = "h", KeyCode = VirtualKeyCode.VK_H, WpfKey = Key.H, IsLetter = true },
                    new KeyModel { DisplayText = "j", KeyCode = VirtualKeyCode.VK_J, WpfKey = Key.J, IsLetter = true },
                    new KeyModel { DisplayText = "k", KeyCode = VirtualKeyCode.VK_K, WpfKey = Key.K, IsLetter = true },
                    new KeyModel { DisplayText = "l", KeyCode = VirtualKeyCode.VK_L, WpfKey = Key.L, IsLetter = true },
                    new KeyModel { DisplayText = ";", ShiftDisplayText = ":", KeyCode = VirtualKeyCode.OEM_1, WpfKey = Key.Oem1 },
                    new KeyModel { DisplayText = "'", ShiftDisplayText = "\"", KeyCode = VirtualKeyCode.OEM_7, WpfKey = Key.Oem7 },
                    new KeyModel { DisplayText = "Enter", KeyCode = VirtualKeyCode.RETURN, WpfKey = Key.Return, WidthFactor = 2.25 }
                },
                // Fila 4 - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "Shift", KeyCode = VirtualKeyCode.LSHIFT, WpfKey = Key.LeftShift, WidthFactor = 2.25 },
                    new KeyModel { DisplayText = "z", KeyCode = VirtualKeyCode.VK_Z, WpfKey = Key.Z, IsLetter = true },
                    new KeyModel { DisplayText = "x", KeyCode = VirtualKeyCode.VK_X, WpfKey = Key.X, IsLetter = true },
                    new KeyModel { DisplayText = "c", KeyCode = VirtualKeyCode.VK_C, WpfKey = Key.C, IsLetter = true },
                    new KeyModel { DisplayText = "v", KeyCode = VirtualKeyCode.VK_V, WpfKey = Key.V, IsLetter = true },
                    new KeyModel { DisplayText = "b", KeyCode = VirtualKeyCode.VK_B, WpfKey = Key.B, IsLetter = true },
                    new KeyModel { DisplayText = "n", KeyCode = VirtualKeyCode.VK_N, WpfKey = Key.N, IsLetter = true },
                    new KeyModel { DisplayText = "m", KeyCode = VirtualKeyCode.VK_M, WpfKey = Key.M, IsLetter = true },
                    new KeyModel { DisplayText = ",", ShiftDisplayText = "<", KeyCode = VirtualKeyCode.OEM_COMMA, WpfKey = Key.OemComma },
                    new KeyModel { DisplayText = ".", ShiftDisplayText = ">", KeyCode = VirtualKeyCode.OEM_PERIOD, WpfKey = Key.OemPeriod },
                    new KeyModel { DisplayText = "/", ShiftDisplayText = "?", KeyCode = VirtualKeyCode.OEM_2, WpfKey = Key.OemQuestion },
                    new KeyModel { DisplayText = "Shift", KeyCode = VirtualKeyCode.RSHIFT, WpfKey = Key.RightShift, WidthFactor = 2.75 }
                },
                // Fila 5 - Total: 15 unidades
                new KeyRow
                {
                    new KeyModel { DisplayText = "Ctrl", KeyCode = VirtualKeyCode.LCONTROL, WpfKey = Key.LeftCtrl, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "\uE770", KeyCode = VirtualKeyCode.LWIN, WpfKey = Key.LWin, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "Alt", KeyCode = VirtualKeyCode.LMENU, WpfKey = Key.LeftAlt, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "Space", KeyCode = VirtualKeyCode.SPACE, WpfKey = Key.Space, WidthFactor = 6.25 },
                    new KeyModel { DisplayText = "Alt", KeyCode = VirtualKeyCode.RMENU, WpfKey = Key.RightAlt, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "\uE770", KeyCode = VirtualKeyCode.RWIN, WpfKey = Key.RWin, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "Ctrl", KeyCode = VirtualKeyCode.RCONTROL, WpfKey = Key.RightCtrl, WidthFactor = 1.25 },
                    new KeyModel { DisplayText = "Ctrl", KeyCode = VirtualKeyCode.RCONTROL, WpfKey = Key.RightCtrl, WidthFactor = 1.25 }
                }
            };
        }
    }
}