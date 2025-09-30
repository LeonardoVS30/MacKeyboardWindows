using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Win32;
using MacKeyboardWindows.Services;
using MacKeyboardWindows.Models;

namespace MacKeyboardWindows
{
    public partial class MainWindow : Window
    {
        private readonly KeyboardService _keyboardService;
        private readonly KeyboardHookService _keyboardHookService;
        private readonly Dictionary<string, Border> _keyControls = new();
        private readonly Dictionary<Key, string> _pressedKeys = new();

        public MainWindow()
        {
            InitializeComponent();
            _keyboardService = new KeyboardService();
            _keyboardHookService = new KeyboardHookService();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeKeyMapping();
            ApplySystemTheme();
            StartKeyboardHook();

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            StopKeyboardHook();
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        private void InitializeKeyMapping()
        {
            FindKeyControls(MainBorder);
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

        private void KeyboardHookService_KeyDown(object sender, Key key)
        {
            Dispatcher.Invoke(() =>
            {
                if (!_pressedKeys.ContainsKey(key))
                {
                    _pressedKeys[key] = GetKeyDisplayText(key);
                    HighlightKey(key, true);
                }
            });
        }

        private void KeyboardHookService_KeyUp(object sender, Key key)
        {
            Dispatcher.Invoke(() =>
            {
                if (_pressedKeys.Remove(key))
                {
                    HighlightKey(key, false);
                }
            });
        }

        private static string GetKeyDisplayText(Key key)
        {
            return KeyMapping.VirtualKeyToDisplayText.TryGetValue(key, out string? displayText)
                ? displayText
                : key.ToString();
        }

        private void HighlightKey(Key key, bool isPressed)
        {
            string displayText = GetKeyDisplayText(key);

            if (_keyControls.TryGetValue(displayText, out Border? border))
            {
                bool isDarkTheme = IsSystemUsingDarkTheme();
                var pressedColor = isDarkTheme ?
                    (Color)FindResource("DarkKeyPressedColor") :
                    (Color)FindResource("LightKeyPressedColor");
                var normalColor = isDarkTheme ?
                    (Color)FindResource("DarkKeyColor") :
                    (Color)FindResource("LightKeyColor");

                border.Background = new SolidColorBrush(isPressed ? pressedColor : normalColor);
            }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                Dispatcher.Invoke(ApplySystemTheme);
            }
        }

        private void ApplySystemTheme()
        {
            bool isDarkTheme = IsSystemUsingDarkTheme();

            if (isDarkTheme)
            {
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
            }
        }

        private static bool IsSystemUsingDarkTheme()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyLightTheme()
        {
            var backgroundColor = (Color)FindResource("LightBackgroundColor");
            var borderColor = (Color)FindResource("LightKeyBorderColor");

            MainBorder.Background = new SolidColorBrush(backgroundColor);
            MainBorder.BorderBrush = new SolidColorBrush(borderColor);

            ApplyThemeToAllKeys(false);
        }

        private void ApplyDarkTheme()
        {
            var backgroundColor = (Color)FindResource("DarkBackgroundColor");
            var borderColor = (Color)FindResource("DarkKeyBorderColor");

            MainBorder.Background = new SolidColorBrush(backgroundColor);
            MainBorder.BorderBrush = new SolidColorBrush(borderColor);

            ApplyThemeToAllKeys(true);
        }

        private void ApplyThemeToAllKeys(bool isDarkTheme)
        {
            var keyColor = isDarkTheme ?
                (Color)FindResource("DarkKeyColor") :
                (Color)FindResource("LightKeyColor");

            var specialKeyColor = isDarkTheme ?
                (Color)FindResource("DarkSpecialKeyColor") :
                (Color)FindResource("LightSpecialKeyColor");

            var textColor = isDarkTheme ?
                (Color)FindResource("DarkTextColor") :
                (Color)FindResource("LightTextColor");

            var borderColor = isDarkTheme ?
                (Color)FindResource("DarkKeyBorderColor") :
                (Color)FindResource("LightKeyBorderColor");

            ApplyThemeToChildren(MainBorder, keyColor, specialKeyColor, textColor, borderColor);
        }

        private void ApplyThemeToChildren(DependencyObject parent, Color keyColor, Color specialKeyColor, Color textColor, Color borderColor)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Border border)
                {
                    border.BorderBrush = new SolidColorBrush(borderColor);

                    // Solo cambiar el color si la tecla no está presionada
                    if (border.Child is TextBlock textBlock && !_pressedKeys.ContainsValue(textBlock.Text))
                    {
                        if (border.Style == FindResource("MacSpecialKeyStyle") ||
                            border.Style == FindResource("MacWideKeyStyle") ||
                            border.Style == FindResource("MacExtraWideKeyStyle") ||
                            border.Style == FindResource("MacSpaceKeyStyle"))
                        {
                            border.Background = new SolidColorBrush(specialKeyColor);
                        }
                        else
                        {
                            border.Background = new SolidColorBrush(keyColor);
                        }
                    }
                }
                else if (child is TextBlock textBlock)
                {
                    textBlock.Foreground = new SolidColorBrush(textColor);
                }

                ApplyThemeToChildren(child, keyColor, specialKeyColor, textColor, borderColor);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Key_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Child is TextBlock textBlock)
            {
                string keyText = textBlock.Text;
                SimulateKeyPress(keyText);
                e.Handled = true;
            }
        }

        private void Key_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void SimulateKeyPress(string keyText)
        {
            try
            {
                if (string.IsNullOrEmpty(keyText)) return;

                if (keyText.Length > 1 || keyText == " " || keyText == "⌫" ||
                    keyText == "`" || keyText == "[" || keyText == "]" ||
                    keyText == "\\" || keyText == ";" || keyText == "'" ||
                    keyText == "," || keyText == "." || keyText == "/" ||
                    keyText == "-" || keyText == "=")
                {
                    _keyboardService.PressSpecialKey(keyText);
                }
                else
                {
                    // Teclas de caracteres simples
                    char character = keyText[0];
                    _keyboardService.PressKey(character);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al simular tecla {keyText}: {ex.Message}");
            }
        }
    }
}