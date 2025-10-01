using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MacKeyboardWindows.Models;
using MacKeyboardWindows.Services;
using Microsoft.Win32;

namespace MacKeyboardWindows
{
    public partial class MainWindow : Window
    {
        // P/Invoke
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        [DllImport("user32.dll")] public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        // Servicios y Campos
        private readonly KeyboardService _keyboardService;
        private readonly KeyboardHookService _keyboardHookService;
        private readonly Dictionary<string, Border> _keyControls = new();
        private readonly Dictionary<string, VirtualKeyCode> _textToVirtualKey = new();

        // --- LA BANDERA PARA EVITAR EL BUCLE DE FEEDBACK ---
        private bool _isSimulating = false;

        public MainWindow()
        {
            ApplySystemTheme();
            InitializeComponent();
            _keyboardService = new KeyboardService();
            _keyboardHookService = new KeyboardHookService();
            InitializeTextToKeyMapping();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        #region Window Initialization
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
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            StopKeyboardHook();
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }
        #endregion

        #region Mapping Initializers
        private void InitializeTextToKeyMapping()
        {
            // Fila 1
            _textToVirtualKey["Esc"] = VirtualKeyCode.ESCAPE; _textToVirtualKey["F1"] = VirtualKeyCode.F1; _textToVirtualKey["F2"] = VirtualKeyCode.F2; _textToVirtualKey["F3"] = VirtualKeyCode.F3; _textToVirtualKey["F4"] = VirtualKeyCode.F4; _textToVirtualKey["F5"] = VirtualKeyCode.F5; _textToVirtualKey["F6"] = VirtualKeyCode.F6; _textToVirtualKey["F7"] = VirtualKeyCode.F7; _textToVirtualKey["F8"] = VirtualKeyCode.F8; _textToVirtualKey["F9"] = VirtualKeyCode.F9; _textToVirtualKey["F10"] = VirtualKeyCode.F10; _textToVirtualKey["F11"] = VirtualKeyCode.F11; _textToVirtualKey["F12"] = VirtualKeyCode.F12;
            _textToVirtualKey["Print"] = VirtualKeyCode.PRINT_SCREEN; _textToVirtualKey["Scroll"] = VirtualKeyCode.SCROLL; _textToVirtualKey["Pause"] = VirtualKeyCode.PAUSE;
            // Fila 2
            _textToVirtualKey["`"] = VirtualKeyCode.OEM_3; _textToVirtualKey["1"] = VirtualKeyCode.VK_1; _textToVirtualKey["2"] = VirtualKeyCode.VK_2; _textToVirtualKey["3"] = VirtualKeyCode.VK_3; _textToVirtualKey["4"] = VirtualKeyCode.VK_4; _textToVirtualKey["5"] = VirtualKeyCode.VK_5; _textToVirtualKey["6"] = VirtualKeyCode.VK_6; _textToVirtualKey["7"] = VirtualKeyCode.VK_7; _textToVirtualKey["8"] = VirtualKeyCode.VK_8; _textToVirtualKey["9"] = VirtualKeyCode.VK_9; _textToVirtualKey["0"] = VirtualKeyCode.VK_0;
            _textToVirtualKey["-"] = VirtualKeyCode.OEM_MINUS; _textToVirtualKey["="] = VirtualKeyCode.OEM_PLUS; _textToVirtualKey["\uE756"] = VirtualKeyCode.BACK;
            // Fila 3
            _textToVirtualKey["Tab"] = VirtualKeyCode.TAB; _textToVirtualKey["q"] = VirtualKeyCode.VK_Q; _textToVirtualKey["w"] = VirtualKeyCode.VK_W; _textToVirtualKey["e"] = VirtualKeyCode.VK_E; _textToVirtualKey["r"] = VirtualKeyCode.VK_R; _textToVirtualKey["t"] = VirtualKeyCode.VK_T; _textToVirtualKey["y"] = VirtualKeyCode.VK_Y; _textToVirtualKey["u"] = VirtualKeyCode.VK_U; _textToVirtualKey["i"] = VirtualKeyCode.VK_I; _textToVirtualKey["o"] = VirtualKeyCode.VK_O; _textToVirtualKey["p"] = VirtualKeyCode.VK_P;
            _textToVirtualKey["["] = VirtualKeyCode.OEM_4; _textToVirtualKey["]"] = VirtualKeyCode.OEM_6; _textToVirtualKey["\\"] = VirtualKeyCode.OEM_5;
            // Fila 4
            _textToVirtualKey["BloqMayus"] = VirtualKeyCode.CAPITAL; _textToVirtualKey["a"] = VirtualKeyCode.VK_A; _textToVirtualKey["s"] = VirtualKeyCode.VK_S; _textToVirtualKey["d"] = VirtualKeyCode.VK_D; _textToVirtualKey["f"] = VirtualKeyCode.VK_F; _textToVirtualKey["g"] = VirtualKeyCode.VK_G; _textToVirtualKey["h"] = VirtualKeyCode.VK_H; _textToVirtualKey["j"] = VirtualKeyCode.VK_J; _textToVirtualKey["k"] = VirtualKeyCode.VK_K; _textToVirtualKey["l"] = VirtualKeyCode.VK_L;
            _textToVirtualKey[";"] = VirtualKeyCode.OEM_1; _textToVirtualKey["'"] = VirtualKeyCode.OEM_7; _textToVirtualKey["Enter"] = VirtualKeyCode.RETURN;
            // Fila 5
            _textToVirtualKey["Shift"] = VirtualKeyCode.LSHIFT; _textToVirtualKey["z"] = VirtualKeyCode.VK_Z; _textToVirtualKey["x"] = VirtualKeyCode.VK_X; _textToVirtualKey["c"] = VirtualKeyCode.VK_C; _textToVirtualKey["v"] = VirtualKeyCode.VK_V; _textToVirtualKey["b"] = VirtualKeyCode.VK_B; _textToVirtualKey["n"] = VirtualKeyCode.VK_N; _textToVirtualKey["m"] = VirtualKeyCode.VK_M;
            _textToVirtualKey[","] = VirtualKeyCode.OEM_COMMA; _textToVirtualKey["."] = VirtualKeyCode.OEM_PERIOD; _textToVirtualKey["/"] = VirtualKeyCode.OEM_2;
            // Fila 6
            _textToVirtualKey["Ctrl"] = VirtualKeyCode.LCONTROL; _textToVirtualKey["\uE770"] = VirtualKeyCode.LWIN; _textToVirtualKey["Alt"] = VirtualKeyCode.LMENU; _textToVirtualKey["Space"] = VirtualKeyCode.SPACE;
        }
        #endregion

        #region On-Screen Keyboard Click Logic
        private async void Key_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Child is TextBlock textBlock)
            {
                e.Handled = true;
                string keyText = textBlock.Text;

                // 1. Mostrar feedback visual
                border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");

                // 2. Simular pulsación, usando la bandera para evitar el feedback loop
                if (_textToVirtualKey.TryGetValue(keyText, out VirtualKeyCode keyCode))
                {
                    _isSimulating = true;
                    try
                    {
                        _keyboardService.SimulateKeyPress(keyCode);
                    }
                    finally
                    {
                        _isSimulating = false;
                    }
                }

                // 3. Quitar resaltado después de una pausa
                await Task.Delay(100);
                border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
            }
        }
        #endregion

        #region Context Menu Logic
        private void OptionsButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.ContextMenu != null)
            {
                e.Handled = true;
                element.ContextMenu.PlacementTarget = element;
                element.ContextMenu.IsOpen = true;
            }
        }
        private void Opacity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string tag && double.TryParse(tag, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double opacity))
                MainBorder.Opacity = opacity;
        }
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string tag && double.TryParse(tag, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double scaleFactor))
            {
                WindowScaleTransform.ScaleX = scaleFactor;
                WindowScaleTransform.ScaleY = scaleFactor;
            }
        }
        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string theme)
            {
                if (theme == "Light") ApplySystemTheme(true);
                else if (theme == "Dark") ApplySystemTheme(false);
                else ApplySystemTheme();
            }
        }
        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) => Close();
        #endregion

        #region Theme Management
        private void ApplySystemTheme(bool? forceLight = null)
        {
            try
            {
                string themeName;
                if (forceLight.HasValue)
                    themeName = forceLight.Value ? "LightTheme" : "DarkTheme";
                else
                {
                    var useLightTheme = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                    themeName = (useLightTheme != null && (int)useLightTheme == 1) ? "LightTheme" : "DarkTheme";
                }
                var themeUri = new Uri($"Themes/{themeName}.xaml", UriKind.Relative);
                var styleDict = new ResourceDictionary { Source = new Uri("Styles.xaml", UriKind.Relative) };
                var themeDict = new ResourceDictionary { Source = themeUri };
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(styleDict);
                Application.Current.Resources.MergedDictionaries.Add(themeDict);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not apply the theme: {ex.Message}");
            }
        }
        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
                Dispatcher.Invoke(ApplySystemTheme);
        }
        #endregion

        #region Physical Keyboard Hook and Highlighting Logic
        private void InitializeKeyMapping() => FindKeyControls(this);
        private void FindKeyControls(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Border border && border.Child is TextBlock textBlock)
                {
                    string keyText = textBlock.Text;
                    if (!string.IsNullOrEmpty(keyText) && !_keyControls.ContainsKey(keyText))
                        _keyControls[keyText] = border;
                }
                FindKeyControls(child);
            }
        }
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
            // --- COMPROBAR LA BANDERA ---
            if (_isSimulating) return;
            Dispatcher.Invoke(() => HighlightKey(e, true));
        }
        private void KeyboardHookService_KeyUp(object sender, Key e)
        {
            // --- COMPROBAR LA BANDERA ---
            if (_isSimulating) return;
            Dispatcher.Invoke(() => HighlightKey(e, false));
        }
        private static string GetKeyDisplayText(Key key)
        {
            if (key == Key.Escape) return "Esc";
            if (key == Key.LWin || key == Key.RWin) return "\uE770";
            if (key == Key.Back) return "\uE756";
            if (key == Key.Capital) return "BloqMayus";
            return KeyMapping.VirtualKeyToDisplayText.TryGetValue(key, out string displayText) ? displayText : key.ToString();
        }
        private void HighlightKey(Key key, bool isPressed)
        {
            string displayText = GetKeyDisplayText(key);
            if (_keyControls.TryGetValue(displayText, out Border border))
            {
                if (isPressed)
                    border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
                else
                    border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
            }
        }
        #endregion

        #region Window Chrome and Dragging
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && !e.Handled)
                DragMove();
        }
        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, MouseButtonEventArgs e) => Close();
        #endregion
    }
}