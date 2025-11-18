using System.Collections.Generic;
using System.Windows.Input;

namespace MacKeyboardWindows.Models
{
    public static class KeyMapping
    {
        public static readonly Dictionary<Key, string> VirtualKeyToDisplayText = new Dictionary<Key, string>
        {
            // Fila 1
            { Key.Oem3, "º" }, // La tecla a la izquierda del 1
            { Key.D1, "1" }, { Key.D2, "2" }, { Key.D3, "3" }, { Key.D4, "4" },
            { Key.D5, "5" }, { Key.D6, "6" }, { Key.D7, "7" }, { Key.D8, "8" },
            { Key.D9, "9" }, { Key.D0, "0" },
            { Key.OemQuotes, "'" }, // La tecla a la derecha del 0 (apostrofe/interrogacion)
            { Key.Oem6, "¡" },      // La tecla a la derecha de ' (exclamacion invertida)
            { Key.Back, "\uE756" },

            // Fila 2
            { Key.Tab, "Tab" },
            { Key.Q, "q" }, { Key.W, "w" }, { Key.E, "e" }, { Key.R, "r" }, { Key.T, "t" },
            { Key.Y, "y" }, { Key.U, "u" }, { Key.I, "i" }, { Key.O, "o" }, { Key.P, "p" },
            { Key.Oem4, "`" },      // Tilde grave / acento circunflejo (derecha de P)
            { Key.OemPlus, "+" },   // Más / Asterisco (derecha de `)
            { Key.Return, "Enter" },

            // Fila 3
            { Key.Capital, "Bloq Mayus" },
            { Key.A, "a" }, { Key.S, "s" }, { Key.D, "d" }, { Key.F, "f" }, { Key.G, "g" },
            { Key.H, "h" }, { Key.J, "j" }, { Key.K, "k" }, { Key.L, "l" },
            { Key.Oem1, "ñ" },      // Ñ (derecha de L)
            { Key.Oem7, "´" },      // Tilde aguda / Dieresis (derecha de Ñ)
            { Key.Oem5, "ç" },      // C cedilla (derecha de ´)

            // Fila 4
            { Key.LeftShift, "Shift" },
            { Key.Oem102, "<" },    // Menor que / Mayor que (Izquierda de Z)
            { Key.Z, "z" }, { Key.X, "x" }, { Key.C, "c" }, { Key.V, "v" }, { Key.B, "b" },
            { Key.N, "n" }, { Key.M, "m" },
            { Key.OemComma, "," },
            { Key.OemPeriod, "." },
            { Key.OemMinus, "-" },
            { Key.RightShift, "Shift" },

            // Fila 5
            { Key.LeftCtrl, "Ctrl" },
            { Key.LWin, "\uE770" },
            { Key.LeftAlt, "Alt" },
            { Key.Space, "Space" },
            { Key.RightAlt, "Alt Gr" },
            { Key.RWin, "\uE770" },
            { Key.Apps, "Menu" },
            { Key.RightCtrl, "Ctrl" },

            // Teclas de Función
            { Key.Escape, "Esc" },
            { Key.F1, "F1" }, { Key.F2, "F2" }, { Key.F3, "F3" }, { Key.F4, "F4" },
            { Key.F5, "F5" }, { Key.F6, "F6" }, { Key.F7, "F7" }, { Key.F8, "F8" },
            { Key.F9, "F9" }, { Key.F10, "F10" }, { Key.F11, "F11" }, { Key.F12, "F12" },
            { Key.PrintScreen, "Print" }, { Key.Scroll, "Scroll" }, { Key.Pause, "Pause" }
        };
    }
}