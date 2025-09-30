using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MacKeyboardWindows.Models;
using MacKeyboardWindows.Services;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MacKeyboardWindows
{
    public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private readonly KeyboardService _keyboardService;
        private readonly KeyboardHookService _keyboardHookService;
        private readonly Dictionary<string, Border> _keyControls = new();
        private readonly Dictionary<Key, string> _pressedKeys = new();

        public MainWindow()
        {
            ApplySystemTheme();
            InitializeComponent();
            _keyboardService = new KeyboardService();
            _keyboardHookService = new KeyboardHookService();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

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

        // --- Lógica para el menú de opciones ---

        private void OptionsButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.ContextMenu != null)
            {
                // Previene que el DragMove se active al hacer clic en el botón
                e.Handled = true;
                element.ContextMenu.PlacementTarget = element;
                element.ContextMenu.IsOpen = true;
            }
        }

        private void Opacity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string tag)
            {
                if (double.TryParse(tag, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double opacity))
                {
                    MainBorder.Opacity = opacity;
                }
            }
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string theme)
            {
                if (theme == "Light") ApplySystemTheme(true);
                else if (theme == "Dark") ApplySystemTheme(false);
                else ApplySystemTheme(); // System theme
            }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // --- Lógica de cambio de tema MODIFICADA para forzar un tema ---
        private void ApplySystemTheme(bool? forceLight = null)
        {
            try
            {
                string themeName;
                if (forceLight.HasValue)
                {
                    themeName = forceLight.Value ? "LightTheme" : "DarkTheme";
                }
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

        private void InitializeKeyMapping()
        {
            FindKeyControls(this);
        }

        private void FindKeyControls(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Border border && border.Child is TextBlock textBlock)
                {
                    string keyText = textBlock.Text;
                    if (!string.IsNullOrEmpty(keyText) && !_keyControls.ContainsKey(keyText))
                    {
                        _keyControls[keyText] = border;
                    }
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
            Dispatcher.Invoke(() => HighlightKey(e, true));
        }

        private void KeyboardHookService_KeyUp(object sender, Key e)
        {
            Dispatcher.Invoke(() => HighlightKey(e, false));
        }

        private static string GetKeyDisplayText(Key key)
        {
            if (key == Key.LWin || key == Key.RWin) return "\uE770";
            if (key == Key.Back) return "\uE756";

            return KeyMapping.VirtualKeyToDisplayText.TryGetValue(key, out string displayText)
                ? displayText
                : key.ToString();
        }

        private void HighlightKey(Key key, bool isPressed)
        {
            string displayText = GetKeyDisplayText(key);

            if (_keyControls.TryGetValue(displayText, out Border border))
            {
                if (isPressed)
                {
                    border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
                }
                else
                {
                    border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
                }
            }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                Dispatcher.Invoke(() => ApplySystemTheme());
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.Handled == false)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}