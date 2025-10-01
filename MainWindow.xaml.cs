using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MacKeyboardWindows.Models;
using MacKeyboardWindows.Services;
using Microsoft.Win32;

namespace MacKeyboardWindows
{
    public partial class MainWindow : Window
    {
        #region P/Invoke and Constants
        [DllImport("user32.dll")] public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] public static extern short GetKeyState(int nVirtKey);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int VK_CAPITAL = 0x14;
        private const int VK_SHIFT = 0x10;
        #endregion

        #region Fields
        private readonly KeyboardService _keyboardService;
        private readonly KeyboardHookService _keyboardHookService;
        private readonly Dictionary<string, Border> _keyControls = new();
        private readonly Dictionary<string, VirtualKeyCode> _textToVirtualKey = new();
        private readonly DispatcherTimer _keyboardStateTimer;
        private bool _isSimulating = false;
        #endregion

        public MainWindow()
        {
            ApplySystemTheme();
            InitializeComponent();
            _keyboardService = new KeyboardService();
            _keyboardHookService = new KeyboardHookService();
            InitializeTextToKeyMapping();

            _keyboardStateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _keyboardStateTimer.Tick += (s, e) => UpdateKeyboardState();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        #region Window Lifetime Events
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_NOACTIVATE);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeKeyMapping();
            StartKeyboardHook();
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            _keyboardStateTimer.Start();
            UpdateKeyboardState();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            StopKeyboardHook();
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            _keyboardStateTimer.Stop();
        }
        #endregion

        #region Keyboard State Management
        private bool IsCapsLocked() => (GetKeyState(VK_CAPITAL) & 1) != 0;
        private bool IsShiftPressed() => (GetKeyState(VK_SHIFT) & 0x8000) != 0;

        private void UpdateKeyboardState()
        {
            bool capsLockOn = IsCapsLocked();
            bool shiftDown = IsShiftPressed();
            bool isUppercase = capsLockOn ^ shiftDown;

            KeyQ.Text = isUppercase ? "Q" : "q"; KeyW.Text = isUppercase ? "W" : "w"; KeyE.Text = isUppercase ? "E" : "e"; KeyR.Text = isUppercase ? "R" : "r"; KeyT.Text = isUppercase ? "T" : "t"; KeyY.Text = isUppercase ? "Y" : "y"; KeyU.Text = isUppercase ? "U" : "u"; KeyI.Text = isUppercase ? "I" : "i"; KeyO.Text = isUppercase ? "O" : "o"; KeyP.Text = isUppercase ? "P" : "p";
            KeyA.Text = isUppercase ? "A" : "a"; KeyS.Text = isUppercase ? "S" : "s"; KeyD.Text = isUppercase ? "D" : "d"; KeyF.Text = isUppercase ? "F" : "f"; KeyG.Text = isUppercase ? "G" : "g"; KeyH.Text = isUppercase ? "H" : "h"; KeyJ.Text = isUppercase ? "J" : "j"; KeyK.Text = isUppercase ? "K" : "k"; KeyL.Text = isUppercase ? "L" : "l";
            KeyZ.Text = isUppercase ? "Z" : "z"; KeyX.Text = isUppercase ? "X" : "x"; KeyC.Text = isUppercase ? "C" : "c"; KeyV.Text = isUppercase ? "V" : "v"; KeyB.Text = isUppercase ? "B" : "b"; KeyN.Text = isUppercase ? "N" : "n"; KeyM.Text = isUppercase ? "M" : "m";

            if (capsLockOn) CapsLockBorder.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
            else CapsLockBorder.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");

            if (shiftDown)
            {
                LeftShiftBorder.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
                RightShiftBorder.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
            }
            else
            {
                LeftShiftBorder.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
                RightShiftBorder.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
            }
        }
        #endregion

        #region On-Screen Keyboard Click Logic (CORREGIDO)
        private async void Key_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Child is TextBlock textBlock)
            {
                e.Handled = true;
                string keyText = textBlock.Text.Replace("\r\n", "");

                // 1. Flash instantáneo para TODAS las teclas
                border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");

                // 2. Simular la pulsación
                if (_textToVirtualKey.TryGetValue(keyText, out var keyCode) || _textToVirtualKey.TryGetValue(keyText.ToLower(), out keyCode))
                {
                    _isSimulating = true;
                    _keyboardService.SimulateKeyPress(keyCode);
                    _isSimulating = false;
                }

                // 3. Pausa crítica para que Windows procese el cambio de estado
                await Task.Delay(50);

                // 4. Actualizar el estado persistente de TODO el teclado
                UpdateKeyboardState();

                // 5. Apagar el flash SOLO para teclas que no son de estado
                if (keyText != "BloqMayus" && keyText != "Shift")
                {
                    // La pausa total del flash será de unos 100ms
                    await Task.Delay(50);
                    border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
                }
            }
        }
        #endregion

        #region Physical Keyboard Hook Logic
        private void StartKeyboardHook()
        {
            _keyboardHookService.KeyDown += KeyboardHookService_KeyDown;
            _keyboardHookService.KeyUp += KeyboardHookService_KeyUp;
            _keyboardHookService.Start();
        }

        private void StopKeyboardHook()
        {
            _keyboardHookService.KeyDown -= KeyboardHookService_KeyDown;
            _keyboardHookService.KeyUp -= KeyboardHookService_KeyUp;
            _keyboardHookService.Stop();
        }

        private void KeyboardHookService_KeyDown(object sender, Key e)
        {
            if (_isSimulating) return;
            Dispatcher.Invoke(() => HighlightKey(e, true));
        }

        private void KeyboardHookService_KeyUp(object sender, Key e)
        {
            if (_isSimulating) return;
            Dispatcher.Invoke(() => HighlightKey(e, false));
        }

        private void HighlightKey(Key key, bool isPressed)
        {
            if (key == Key.Capital || key == Key.LeftShift || key == Key.RightShift) return;
            string displayText = GetKeyDisplayText(key);
            if (_keyControls.TryGetValue(displayText, out Border border))
            {
                if (isPressed) border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
                else border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
            }
        }
        #endregion

        #region Mappings, Menus, UI Handlers (Sin Cambios)
        private void InitializeTextToKeyMapping() { _textToVirtualKey["Esc"] = VirtualKeyCode.ESCAPE; _textToVirtualKey["F1"] = VirtualKeyCode.F1; _textToVirtualKey["F2"] = VirtualKeyCode.F2; _textToVirtualKey["F3"] = VirtualKeyCode.F3; _textToVirtualKey["F4"] = VirtualKeyCode.F4; _textToVirtualKey["F5"] = VirtualKeyCode.F5; _textToVirtualKey["F6"] = VirtualKeyCode.F6; _textToVirtualKey["F7"] = VirtualKeyCode.F7; _textToVirtualKey["F8"] = VirtualKeyCode.F8; _textToVirtualKey["F9"] = VirtualKeyCode.F9; _textToVirtualKey["F10"] = VirtualKeyCode.F10; _textToVirtualKey["F11"] = VirtualKeyCode.F11; _textToVirtualKey["F12"] = VirtualKeyCode.F12; _textToVirtualKey["Print"] = VirtualKeyCode.PRINT_SCREEN; _textToVirtualKey["Scroll"] = VirtualKeyCode.SCROLL; _textToVirtualKey["Pause"] = VirtualKeyCode.PAUSE; _textToVirtualKey["`"] = VirtualKeyCode.OEM_3; _textToVirtualKey["1"] = VirtualKeyCode.VK_1; _textToVirtualKey["2"] = VirtualKeyCode.VK_2; _textToVirtualKey["3"] = VirtualKeyCode.VK_3; _textToVirtualKey["4"] = VirtualKeyCode.VK_4; _textToVirtualKey["5"] = VirtualKeyCode.VK_5; _textToVirtualKey["6"] = VirtualKeyCode.VK_6; _textToVirtualKey["7"] = VirtualKeyCode.VK_7; _textToVirtualKey["8"] = VirtualKeyCode.VK_8; _textToVirtualKey["9"] = VirtualKeyCode.VK_9; _textToVirtualKey["0"] = VirtualKeyCode.VK_0; _textToVirtualKey["-"] = VirtualKeyCode.OEM_MINUS; _textToVirtualKey["="] = VirtualKeyCode.OEM_PLUS; _textToVirtualKey["\uE756"] = VirtualKeyCode.BACK; _textToVirtualKey["Tab"] = VirtualKeyCode.TAB; _textToVirtualKey["q"] = VirtualKeyCode.VK_Q; _textToVirtualKey["w"] = VirtualKeyCode.VK_W; _textToVirtualKey["e"] = VirtualKeyCode.VK_E; _textToVirtualKey["r"] = VirtualKeyCode.VK_R; _textToVirtualKey["t"] = VirtualKeyCode.VK_T; _textToVirtualKey["y"] = VirtualKeyCode.VK_Y; _textToVirtualKey["u"] = VirtualKeyCode.VK_U; _textToVirtualKey["i"] = VirtualKeyCode.VK_I; _textToVirtualKey["o"] = VirtualKeyCode.VK_O; _textToVirtualKey["p"] = VirtualKeyCode.VK_P; _textToVirtualKey["["] = VirtualKeyCode.OEM_4; _textToVirtualKey["]"] = VirtualKeyCode.OEM_6; _textToVirtualKey["\\"] = VirtualKeyCode.OEM_5; _textToVirtualKey["BloqMayus"] = VirtualKeyCode.CAPITAL; _textToVirtualKey["a"] = VirtualKeyCode.VK_A; _textToVirtualKey["s"] = VirtualKeyCode.VK_S; _textToVirtualKey["d"] = VirtualKeyCode.VK_D; _textToVirtualKey["f"] = VirtualKeyCode.VK_F; _textToVirtualKey["g"] = VirtualKeyCode.VK_G; _textToVirtualKey["h"] = VirtualKeyCode.VK_H; _textToVirtualKey["j"] = VirtualKeyCode.VK_J; _textToVirtualKey["k"] = VirtualKeyCode.VK_K; _textToVirtualKey["l"] = VirtualKeyCode.VK_L; _textToVirtualKey[";"] = VirtualKeyCode.OEM_1; _textToVirtualKey["'"] = VirtualKeyCode.OEM_7; _textToVirtualKey["Enter"] = VirtualKeyCode.RETURN; _textToVirtualKey["Shift"] = VirtualKeyCode.LSHIFT; _textToVirtualKey["z"] = VirtualKeyCode.VK_Z; _textToVirtualKey["x"] = VirtualKeyCode.VK_X; _textToVirtualKey["c"] = VirtualKeyCode.VK_C; _textToVirtualKey["v"] = VirtualKeyCode.VK_V; _textToVirtualKey["b"] = VirtualKeyCode.VK_B; _textToVirtualKey["n"] = VirtualKeyCode.VK_N; _textToVirtualKey["m"] = VirtualKeyCode.VK_M; _textToVirtualKey[","] = VirtualKeyCode.OEM_COMMA; _textToVirtualKey["."] = VirtualKeyCode.OEM_PERIOD; _textToVirtualKey["/"] = VirtualKeyCode.OEM_2; _textToVirtualKey["Ctrl"] = VirtualKeyCode.LCONTROL; _textToVirtualKey["\uE770"] = VirtualKeyCode.LWIN; _textToVirtualKey["Alt"] = VirtualKeyCode.LMENU; _textToVirtualKey["Space"] = VirtualKeyCode.SPACE; }
        private void InitializeKeyMapping() { FindKeyControls(this); }
        private void FindKeyControls(DependencyObject parent) { for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is Border b && b.Child is TextBlock t) { string txt = t.Text.Replace("\r\n", ""); if (!string.IsNullOrEmpty(txt) && !_keyControls.ContainsKey(txt)) _keyControls[txt] = b; } FindKeyControls(child); } }
        private static string GetKeyDisplayText(Key key) { if (key == Key.Escape) return "Esc"; if (key == Key.LWin || key == Key.RWin) return "\uE770"; if (key == Key.Back) return "\uE756"; if (key == Key.Capital) return "BloqMayus"; if (key == Key.LeftShift || key == Key.RightShift) return "Shift"; return KeyMapping.VirtualKeyToDisplayText.TryGetValue(key, out string d) ? d : key.ToString(); }
        private void OptionsButton_Click(object sender, MouseButtonEventArgs e) { if (sender is FrameworkElement el && el.ContextMenu != null) { e.Handled = true; el.ContextMenu.PlacementTarget = el; el.ContextMenu.IsOpen = true; } }
        private void Opacity_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double o)) MainBorder.Opacity = o; }
        private void Zoom_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double f)) { WindowScaleTransform.ScaleX = f; WindowScaleTransform.ScaleY = f; } }
        private void Theme_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string th) { if (th == "Light") ApplySystemTheme(true); else if (th == "Dark") ApplySystemTheme(false); else ApplySystemTheme(); } }
        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) => Close();
        private void ApplySystemTheme(bool? forceLight = null) { try { string n; if (forceLight.HasValue) n = forceLight.Value ? "LightTheme" : "DarkTheme"; else { var v = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1); n = (v != null && (int)v == 1) ? "LightTheme" : "DarkTheme"; } var u = new Uri($"Themes/{n}.xaml", UriKind.Relative); var s = new ResourceDictionary { Source = new Uri("Styles.xaml", UriKind.Relative) }; var t = new ResourceDictionary { Source = u }; Application.Current.Resources.MergedDictionaries.Clear(); Application.Current.Resources.MergedDictionaries.Add(s); Application.Current.Resources.MergedDictionaries.Add(t); } catch (Exception ex) { MessageBox.Show($"Could not apply the theme: {ex.Message}"); } }
        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) { if (e.Category == UserPreferenceCategory.General) Dispatcher.Invoke(ApplySystemTheme); }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed && !e.Handled) DragMove(); }
        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, MouseButtonEventArgs e) => Close();
        #endregion
    }
}