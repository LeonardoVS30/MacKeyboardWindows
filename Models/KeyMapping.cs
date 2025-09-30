using System.Collections.Generic;
using System.Windows.Input;

namespace MacKeyboardWindows.Models
{
    public static class KeyMapping
    {
        public static Dictionary<Key, string> VirtualKeyToDisplayText { get; } = new Dictionary<Key, string>
        {
            // Letras
            { Key.A, "a" }, { Key.B, "b" }, { Key.C, "c" }, { Key.D, "d" }, { Key.E, "e" },
            { Key.F, "f" }, { Key.G, "g" }, { Key.H, "h" }, { Key.I, "i" }, { Key.J, "j" },
            { Key.K, "k" }, { Key.L, "l" }, { Key.M, "m" }, { Key.N, "n" }, { Key.O, "o" },
            { Key.P, "p" }, { Key.Q, "q" }, { Key.R, "r" }, { Key.S, "s" }, { Key.T, "t" },
            { Key.U, "u" }, { Key.V, "v" }, { Key.W, "w" }, { Key.X, "x" }, { Key.Y, "y" },
            { Key.Z, "z" },

            // Números
            { Key.D0, "0" }, { Key.D1, "1" }, { Key.D2, "2" }, { Key.D3, "3" }, { Key.D4, "4" },
            { Key.D5, "5" }, { Key.D6, "6" }, { Key.D7, "7" }, { Key.D8, "8" }, { Key.D9, "9" },

            // Teclas especiales
            { Key.Escape, "esc" },
            { Key.Enter, "Enter" },
            { Key.Space, "Space" },
            { Key.LeftShift, "Shift" }, { Key.RightShift, "Shift" },
            { Key.LeftCtrl, "Ctrl" }, { Key.RightCtrl, "Ctrl" },
            { Key.LWin, "Win" }, { Key.RWin, "Win" },
            { Key.LeftAlt, "Alt" }, { Key.RightAlt, "Alt" },
            { Key.Back, "⌫" },
            { Key.Tab, "Tab" },
            { Key.CapsLock, "Caps" },
            { Key.OemMinus, "-" },
            { Key.OemPlus, "=" },
            { Key.OemOpenBrackets, "[" },
            { Key.OemCloseBrackets, "]" },
            { Key.OemSemicolon, ";" },
            { Key.OemQuotes, "'" },
            { Key.OemComma, "," },
            { Key.OemPeriod, "." },
            { Key.OemQuestion, "/" },
            { Key.OemBackslash, "\\" },
            { Key.OemTilde, "`" },
            { Key.OemPipe, "|" },

            // Agrega estas líneas al diccionario existente:
{ Key.F1, "F1" }, { Key.F2, "F2" }, { Key.F3, "F3" }, { Key.F4, "F4" },
{ Key.F5, "F5" }, { Key.F6, "F6" }, { Key.F7, "F7" }, { Key.F8, "F8" },
{ Key.F9, "F9" }, { Key.F10, "F10" }, { Key.F11, "F11" }, { Key.F12, "F12" },
{ Key.PrintScreen, "Print" }, { Key.Scroll, "Scroll" }, { Key.Pause, "Pause" }
        };
    }
}