using System.Collections.Generic;
using System.Windows.Input;
using MacKeyboardWindows.Services;

namespace MacKeyboardWindows.Models
{
    public class PianoKeyModel
    {
        public string NoteName { get; set; } = "";
        public int Octave { get; set; }
        public bool IsBlackKey { get; set; }
        public VirtualKeyCode KeyCode { get; set; }
        public Key WpfKey { get; set; }
        public string KeyLabel { get; set; } = ""; // Label del atajo de teclado (ej: "Z", "A", "1")
        public int KeyboardRow { get; set; } // 0=Z row, 1=A row, 2=Q row, 3=number row
    }

    /// <summary>
    /// Definición de una escala musical con nombre y patrón de intervalos (semitonos).
    /// </summary>
    public class ScaleDefinition
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int[] Intervals { get; set; } = System.Array.Empty<int>(); // semitonos desde la raíz
    }

    /// <summary>
    /// Nombres de las 12 notas cromáticas.
    /// </summary>
    public static class NoteNames
    {
        public static readonly string[] All = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public static bool IsBlack(string name) =>
            name == "C#" || name == "D#" || name == "F#" || name == "G#" || name == "A#";
    }

    /// <summary>
    /// Escalas disponibles.
    /// </summary>
    public static class PianoScales
    {
        public static readonly List<ScaleDefinition> All = new List<ScaleDefinition>
        {
            // Escalas mayores
            new ScaleDefinition { Name = "C_Major",  DisplayName = "C Major (Do Mayor)",    Intervals = new[] { 0, 2, 4, 5, 7, 9, 11 } },
            new ScaleDefinition { Name = "D_Major",  DisplayName = "D Major (Re Mayor)",    Intervals = new[] { 2, 4, 6, 7, 9, 11, 13 } },
            new ScaleDefinition { Name = "E_Major",  DisplayName = "E Major (Mi Mayor)",    Intervals = new[] { 4, 6, 8, 9, 11, 13, 15 } },
            new ScaleDefinition { Name = "F_Major",  DisplayName = "F Major (Fa Mayor)",    Intervals = new[] { 5, 7, 9, 10, 12, 14, 16 } },
            new ScaleDefinition { Name = "G_Major",  DisplayName = "G Major (Sol Mayor)",   Intervals = new[] { 7, 9, 11, 12, 14, 16, 18 } },
            new ScaleDefinition { Name = "A_Major",  DisplayName = "A Major (La Mayor)",    Intervals = new[] { 9, 11, 13, 14, 16, 18, 20 } },
            new ScaleDefinition { Name = "B_Major",  DisplayName = "B Major (Si Mayor)",    Intervals = new[] { 11, 13, 15, 16, 18, 20, 22 } },

            // Escalas menores
            new ScaleDefinition { Name = "C_Minor",  DisplayName = "C Minor (Do Menor)",    Intervals = new[] { 0, 2, 3, 5, 7, 8, 10 } },
            new ScaleDefinition { Name = "D_Minor",  DisplayName = "D Minor (Re Menor)",    Intervals = new[] { 2, 4, 5, 7, 9, 10, 12 } },
            new ScaleDefinition { Name = "E_Minor",  DisplayName = "E Minor (Mi Menor)",    Intervals = new[] { 4, 6, 7, 9, 11, 12, 14 } },
            new ScaleDefinition { Name = "F_Minor",  DisplayName = "F Minor (Fa Menor)",    Intervals = new[] { 5, 7, 8, 10, 12, 13, 15 } },
            new ScaleDefinition { Name = "G_Minor",  DisplayName = "G Minor (Sol Menor)",   Intervals = new[] { 7, 9, 10, 12, 14, 15, 17 } },
            new ScaleDefinition { Name = "A_Minor",  DisplayName = "A Minor (La Menor)",    Intervals = new[] { 9, 11, 12, 14, 16, 17, 19 } },
            new ScaleDefinition { Name = "B_Minor",  DisplayName = "B Minor (Si Menor)",    Intervals = new[] { 11, 13, 14, 16, 18, 19, 21 } },
        };

        public static ScaleDefinition GetByName(string name)
        {
            return All.Find(s => s.Name == name) ?? All[0];
        }
    }

    /// <summary>
    /// Genera la distribución de teclas del piano según la escala seleccionada.
    /// 
    /// Filas del teclado:
    ///   Row 0 (Z-M):   7 teclas → notas de la escala en octava 3
    ///   Row 1 (A-J):   7 teclas → notas de la escala en octava 4
    ///   Row 2 (Q-U):   7 teclas → notas de la escala en octava 5
    ///   Row 3 (1-¡):  12 teclas → notas de la escala en octavas 6-7
    /// </summary>
    public static class PianoLayout
    {
        // Definición fija de teclas por fila
        private static readonly (VirtualKeyCode code, Key wpf, string label)[] Row0 =
        {
            (VirtualKeyCode.VK_Z, Key.Z, "Z"),
            (VirtualKeyCode.VK_X, Key.X, "X"),
            (VirtualKeyCode.VK_C, Key.C, "C"),
            (VirtualKeyCode.VK_V, Key.V, "V"),
            (VirtualKeyCode.VK_B, Key.B, "B"),
            (VirtualKeyCode.VK_N, Key.N, "N"),
            (VirtualKeyCode.VK_M, Key.M, "M"),
        };

        private static readonly (VirtualKeyCode code, Key wpf, string label)[] Row1 =
        {
            (VirtualKeyCode.VK_A, Key.A, "A"),
            (VirtualKeyCode.VK_S, Key.S, "S"),
            (VirtualKeyCode.VK_D, Key.D, "D"),
            (VirtualKeyCode.VK_F, Key.F, "F"),
            (VirtualKeyCode.VK_G, Key.G, "G"),
            (VirtualKeyCode.VK_H, Key.H, "H"),
            (VirtualKeyCode.VK_J, Key.J, "J"),
        };

        private static readonly (VirtualKeyCode code, Key wpf, string label)[] Row2 =
        {
            (VirtualKeyCode.VK_Q, Key.Q, "Q"),
            (VirtualKeyCode.VK_W, Key.W, "W"),
            (VirtualKeyCode.VK_E, Key.E, "E"),
            (VirtualKeyCode.VK_R, Key.R, "R"),
            (VirtualKeyCode.VK_T, Key.T, "T"),
            (VirtualKeyCode.VK_Y, Key.Y, "Y"),
            (VirtualKeyCode.VK_U, Key.U, "U"),
        };

        private static readonly (VirtualKeyCode code, Key wpf, string label)[] Row3 =
        {
            (VirtualKeyCode.VK_1, Key.D1, "1"),
            (VirtualKeyCode.VK_2, Key.D2, "2"),
            (VirtualKeyCode.VK_3, Key.D3, "3"),
            (VirtualKeyCode.VK_4, Key.D4, "4"),
            (VirtualKeyCode.VK_5, Key.D5, "5"),
            (VirtualKeyCode.VK_6, Key.D6, "6"),
            (VirtualKeyCode.VK_7, Key.D7, "7"),
            (VirtualKeyCode.VK_8, Key.D8, "8"),
            (VirtualKeyCode.VK_9, Key.D9, "9"),
            (VirtualKeyCode.VK_0, Key.D0, "0"),
            (VirtualKeyCode.OEM_7, Key.OemQuotes, "'"),
            (VirtualKeyCode.OEM_6, Key.Oem6, "¡"),
        };

        /// <summary>
        /// Genera todas las teclas del piano para una escala dada.
        /// </summary>
        public static List<List<PianoKeyModel>> GetKeysByRow(string scaleName)
        {
            var scale = PianoScales.GetByName(scaleName);
            var rows = new List<List<PianoKeyModel>>();

            // Row 0: octava 3 (7 notas)
            rows.Add(GenerateRow(scale, 3, Row0, 0));
            // Row 1: octava 4 (7 notas)
            rows.Add(GenerateRow(scale, 4, Row1, 1));
            // Row 2: octava 5 (7 notas)
            rows.Add(GenerateRow(scale, 5, Row2, 2));
            // Row 3: octavas 6-7 (12 notas — escala cíclica)
            rows.Add(GenerateRow(scale, 6, Row3, 3));

            return rows;
        }

        /// <summary>
        /// Genera una fila de teclas mapeando notas de la escala
        /// a las teclas de teclado de la fila dada.
        /// </summary>
        private static List<PianoKeyModel> GenerateRow(
            ScaleDefinition scale, int startOctave,
            (VirtualKeyCode code, Key wpf, string label)[] keyDefs,
            int rowIndex)
        {
            var result = new List<PianoKeyModel>();
            int notesPerOctave = scale.Intervals.Length; // 7 para escalas diatónicas

            for (int i = 0; i < keyDefs.Length; i++)
            {
                // Calcular qué nota de la escala es (cicla por octavas)
                int scaleIndex = i % notesPerOctave;
                int octaveOffset = i / notesPerOctave;
                int octave = startOctave + octaveOffset;

                // Intervalo en semitonos desde C0
                int semitone = scale.Intervals[scaleIndex] % 12;
                string noteName = NoteNames.All[semitone];

                result.Add(new PianoKeyModel
                {
                    NoteName = noteName,
                    Octave = octave,
                    IsBlackKey = NoteNames.IsBlack(noteName),
                    KeyCode = keyDefs[i].code,
                    WpfKey = keyDefs[i].wpf,
                    KeyLabel = keyDefs[i].label,
                    KeyboardRow = rowIndex
                });
            }

            return result;
        }

        /// <summary>
        /// Devuelve todas las teclas como lista plana (compatibilidad).
        /// </summary>
        public static List<PianoKeyModel> GetKeys(string scaleName = "C_Major")
        {
            var allKeys = new List<PianoKeyModel>();
            foreach (var row in GetKeysByRow(scaleName))
                allKeys.AddRange(row);
            return allKeys;
        }
    }
}
