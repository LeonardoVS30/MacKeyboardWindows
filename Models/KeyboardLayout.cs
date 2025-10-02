// Models/KeyboardLayout.cs
using System.Collections.Generic;
using System.Windows.Input;
using MacKeyboardWindows.Services;

namespace MacKeyboardWindows.Models
{
    // Representa una única tecla en el teclado virtual
    public class KeyModel
    {
        public string DisplayText { get; set; }
        public string ShiftDisplayText { get; set; }
        public VirtualKeyCode KeyCode { get; set; }
        public Key WpfKey { get; set; }
        public double WidthFactor { get; set; } = 1.0;
        public bool IsLetter { get; set; } = false;
    }

    // Representa una fila de teclas
    public class KeyRow : List<KeyModel> { }

    // Representa la distribución completa del teclado
    public class KeyboardLayout : List<KeyRow> { }
}