using System.Collections.Generic;
using System.Windows.Input;
using MacKeyboardWindows.Services;

namespace MacKeyboardWindows.Models
{
    public class PianoKeyModel
    {
        public string NoteName { get; set; }
        public int Octave { get; set; }
        public bool IsBlackKey { get; set; }
        public VirtualKeyCode KeyCode { get; set; }
        public Key WpfKey { get; set; }
    }

    public static class PianoLayout
    {
        /// <summary>
        /// Returns the full piano key layout matching FL Studio's "Play with keyboard" mapping.
        /// Lower octave: Z S X D C V G B H N J M  → C3 through B3
        /// Upper octave: Q 2 W 3 E R 5 T 6 Y 7 U  → C4 through B4
        /// Highest:      I 9 O 0 P                  → C5 through E5
        /// </summary>
        public static List<PianoKeyModel> GetKeys()
        {
            return new List<PianoKeyModel>
            {
                // === Octave 3 (lower) — mapped to Z row + S/D/G/H/J ===
                new PianoKeyModel { NoteName = "C",  Octave = 3, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_Z, WpfKey = Key.Z },
                new PianoKeyModel { NoteName = "C#", Octave = 3, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_S, WpfKey = Key.S },
                new PianoKeyModel { NoteName = "D",  Octave = 3, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_X, WpfKey = Key.X },
                new PianoKeyModel { NoteName = "D#", Octave = 3, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_D, WpfKey = Key.D },
                new PianoKeyModel { NoteName = "E",  Octave = 3, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_C, WpfKey = Key.C },
                new PianoKeyModel { NoteName = "F",  Octave = 3, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_V, WpfKey = Key.V },
                new PianoKeyModel { NoteName = "F#", Octave = 3, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_G, WpfKey = Key.G },
                new PianoKeyModel { NoteName = "G",  Octave = 3, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_B, WpfKey = Key.B },
                new PianoKeyModel { NoteName = "G#", Octave = 3, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_H, WpfKey = Key.H },
                new PianoKeyModel { NoteName = "A",  Octave = 3, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_N, WpfKey = Key.N },
                new PianoKeyModel { NoteName = "A#", Octave = 3, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_J, WpfKey = Key.J },
                new PianoKeyModel { NoteName = "B",  Octave = 3, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_M, WpfKey = Key.M },

                // === Octave 4 (upper) — mapped to Q row + 2/3/5/6/7 ===
                new PianoKeyModel { NoteName = "C",  Octave = 4, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_Q, WpfKey = Key.Q },
                new PianoKeyModel { NoteName = "C#", Octave = 4, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_2, WpfKey = Key.D2 },
                new PianoKeyModel { NoteName = "D",  Octave = 4, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_W, WpfKey = Key.W },
                new PianoKeyModel { NoteName = "D#", Octave = 4, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_3, WpfKey = Key.D3 },
                new PianoKeyModel { NoteName = "E",  Octave = 4, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_E, WpfKey = Key.E },
                new PianoKeyModel { NoteName = "F",  Octave = 4, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_R, WpfKey = Key.R },
                new PianoKeyModel { NoteName = "F#", Octave = 4, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_5, WpfKey = Key.D5 },
                new PianoKeyModel { NoteName = "G",  Octave = 4, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_T, WpfKey = Key.T },
                new PianoKeyModel { NoteName = "G#", Octave = 4, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_6, WpfKey = Key.D6 },
                new PianoKeyModel { NoteName = "A",  Octave = 4, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_Y, WpfKey = Key.Y },
                new PianoKeyModel { NoteName = "A#", Octave = 4, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_7, WpfKey = Key.D7 },
                new PianoKeyModel { NoteName = "B",  Octave = 4, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_U, WpfKey = Key.U },

                // === Octave 5 (highest) — mapped to I/O/P + 9/0 ===
                new PianoKeyModel { NoteName = "C",  Octave = 5, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_I, WpfKey = Key.I },
                new PianoKeyModel { NoteName = "C#", Octave = 5, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_9, WpfKey = Key.D9 },
                new PianoKeyModel { NoteName = "D",  Octave = 5, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_O, WpfKey = Key.O },
                new PianoKeyModel { NoteName = "D#", Octave = 5, IsBlackKey = true,  KeyCode = VirtualKeyCode.VK_0, WpfKey = Key.D0 },
                new PianoKeyModel { NoteName = "E",  Octave = 5, IsBlackKey = false, KeyCode = VirtualKeyCode.VK_P, WpfKey = Key.P },
            };
        }
    }
}
