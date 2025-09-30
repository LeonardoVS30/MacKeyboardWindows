using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;

namespace MacKeyboardWindows.Services
{
    public class KeyboardService
    {
        private readonly InputSimulator _inputSimulator;

        public KeyboardService()
        {
            _inputSimulator = new InputSimulator();
        }

        public void PressKey(Key key)
        {
            var virtualKey = ConvertToVirtualKey(key);
            _inputSimulator.Keyboard.KeyPress(virtualKey);
        }

        public void PressKey(char character)
        {
            _inputSimulator.Keyboard.TextEntry(character);
        }

        public void PressSpecialKey(string keyName)
        {
            var virtualKey = keyName.ToLower() switch
            {
                "esc" or "escape" => VirtualKeyCode.ESCAPE,
                "enter" => VirtualKeyCode.RETURN,
                "space" => VirtualKeyCode.SPACE,
                "shift" => VirtualKeyCode.SHIFT,
                "ctrl" or "control" => VirtualKeyCode.CONTROL,
                "alt" => VirtualKeyCode.MENU,
                "win" or "windows" => VirtualKeyCode.LWIN,
                "back" or "⌫" or "backspace" => VirtualKeyCode.BACK,
                "tab" => VirtualKeyCode.TAB,
                "caps" or "capslock" => VirtualKeyCode.CAPITAL,
                "`" => VirtualKeyCode.OEM_3,
                "[" => VirtualKeyCode.OEM_4,
                "]" => VirtualKeyCode.OEM_6,
                "\\" => VirtualKeyCode.OEM_5,
                ";" => VirtualKeyCode.OEM_1,
                "'" => VirtualKeyCode.OEM_7,
                "," => VirtualKeyCode.OEM_COMMA,
                "." => VirtualKeyCode.OEM_PERIOD,
                "/" => VirtualKeyCode.OEM_2,
                "-" => VirtualKeyCode.OEM_MINUS,
                "=" => VirtualKeyCode.OEM_PLUS,
                _ => VirtualKeyCode.SPACE
            };

            _inputSimulator.Keyboard.KeyPress(virtualKey);
        }

        private static VirtualKeyCode ConvertToVirtualKey(Key key)
        {
            return key switch
            {
                Key.Escape => VirtualKeyCode.ESCAPE,
                Key.Enter => VirtualKeyCode.RETURN,
                Key.Space => VirtualKeyCode.SPACE,
                Key.LeftShift => VirtualKeyCode.SHIFT,
                Key.RightShift => VirtualKeyCode.RSHIFT,
                Key.LeftCtrl => VirtualKeyCode.CONTROL,
                Key.RightCtrl => VirtualKeyCode.RCONTROL,
                Key.LWin => VirtualKeyCode.LWIN,
                Key.Back => VirtualKeyCode.BACK,
                Key.Tab => VirtualKeyCode.TAB,
                Key.CapsLock => VirtualKeyCode.CAPITAL,
                _ => (VirtualKeyCode)KeyInterop.VirtualKeyFromKey(key)
            };
        }
    }
}