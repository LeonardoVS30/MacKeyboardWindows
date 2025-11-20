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
        private readonly SoundService _soundService;
        private readonly DispatcherTimer _keyboardStateTimer;

        // Mapas y listas para la UI dinámica
        private readonly Dictionary<Key, Border> _wpfKeyToBorderMap = new Dictionary<Key, Border>();
        private readonly List<Tuple<TextBlock, KeyModel>> _letterKeys = new List<Tuple<TextBlock, KeyModel>>();
        private readonly List<Tuple<TextBlock, KeyModel>> _symbolKeys = new List<Tuple<TextBlock, KeyModel>>();

        private Border _capsLockBorder, _leftShiftBorder, _rightShiftBorder;
        private bool _isSimulating = false;
        private string _currentLayout = "ES";
        #endregion

        public MainWindow()
        {
            // Importante: Aplicar tema antes de inicializar componentes
            ApplyTheme("System");

            InitializeComponent();

            _keyboardService = new KeyboardService();
            _keyboardHookService = new KeyboardHookService();
            _soundService = new SoundService();

            _keyboardStateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _keyboardStateTimer.Tick += (s, e) => UpdateKeyboardState();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        #region Window Lifetime & Layout Loading
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartKeyboardHook();
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            _keyboardStateTimer.Start();
            LoadLayout(_currentLayout);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            StopKeyboardHook();
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            _keyboardStateTimer.Stop();
        }

        private void Layout_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string layoutName)
            {
                LoadLayout(layoutName);
            }
        }

        private void LoadLayout(string name)
        {
            _currentLayout = name;
            var layout = LayoutFactory.GetLayout(name);
            BuildKeyboardUI(layout);
            UpdateKeyboardState();
        }
        #endregion

        #region Dynamic UI Builder
        private void BuildKeyboardUI(KeyboardLayout layout)
        {
            KeyboardContainer.Children.Clear();
            KeyboardContainer.RowDefinitions.Clear();
            _wpfKeyToBorderMap.Clear();
            _letterKeys.Clear();
            _symbolKeys.Clear();
            _capsLockBorder = null; _leftShiftBorder = null; _rightShiftBorder = null;

            for (int i = 0; i < layout.Count; i++)
                KeyboardContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int rowIndex = 0; rowIndex < layout.Count; rowIndex++)
            {
                var keyRowModel = layout[rowIndex];
                var rowGrid = new Grid();
                foreach (var keyModel in keyRowModel)
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(keyModel.WidthFactor, GridUnitType.Star) });

                for (int colIndex = 0; colIndex < keyRowModel.Count; colIndex++)
                {
                    var keyModel = keyRowModel[colIndex];
                    UIElement contentElement;

                    // --- LÓGICA DE ICONOS ---
                    if (keyModel.DisplayText.StartsWith("ICON_"))
                    {
                        // Es un icono: Crear un Path vectorial
                        var path = new System.Windows.Shapes.Path
                        {
                            Style = (Style)FindResource("IconPathStyle"),
                            Stretch = Stretch.Uniform,
                            Height = 14, // Ajusta el tamaño del icono aquí
                            Width = 14
                        };

                        // Elegir el dibujo según el nombre
                        string resourceKey = keyModel.DisplayText switch
                        {
                            "ICON_DELETE" => "IconDelete",
                            "ICON_MENU" => "IconMenu", // Icono de hamburguesa
                            "ICON_WINDOWS" => "IconWindows",
                            _ => ""
                        };

                        if (!string.IsNullOrEmpty(resourceKey))
                        {
                            path.Data = (Geometry)FindResource(resourceKey);
                        }
                        contentElement = path;
                    }
                    else
                    {
                        // Es texto normal
                        var textBlock = new TextBlock { Style = (Style)FindResource(keyModel.DisplayText.Length > 2 ? "SmallKeyTextStyle" : "KeyTextStyle") };
                        if (keyModel.DisplayText.Contains("\n"))
                        {
                            var parts = keyModel.DisplayText.Split('\n');
                            textBlock.Inlines.Add(parts[0]);
                            textBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                            textBlock.Inlines.Add(parts[1]);
                            textBlock.TextAlignment = TextAlignment.Center;
                        }
                        else
                        {
                            textBlock.Text = keyModel.DisplayText;
                        }

                        // Guardar referencias para actualizar texto (Caps/Shift)
                        if (keyModel.IsLetter) _letterKeys.Add(Tuple.Create(textBlock, keyModel));
                        else if (!string.IsNullOrEmpty(keyModel.ShiftDisplayText)) _symbolKeys.Add(Tuple.Create(textBlock, keyModel));

                        contentElement = textBlock;
                    }
                    // -------------------------

                    var border = new Border { Style = (Style)FindResource("KeyStyle"), Child = contentElement, Tag = keyModel };
                    border.MouseLeftButtonDown += Key_MouseLeftButtonDown;
                    Grid.SetColumn(border, colIndex);
                    rowGrid.Children.Add(border);

                    if (!_wpfKeyToBorderMap.ContainsKey(keyModel.WpfKey)) _wpfKeyToBorderMap.Add(keyModel.WpfKey, border);

                    if (keyModel.WpfKey == Key.Capital) _capsLockBorder = border;
                    if (keyModel.WpfKey == Key.LeftShift) _leftShiftBorder = border;
                    if (keyModel.WpfKey == Key.RightShift) _rightShiftBorder = border;
                }
                Grid.SetRow(rowGrid, rowIndex);
                KeyboardContainer.Children.Add(rowGrid);
            }
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

            // Actualizar textos
            foreach (var (textBlock, keyModel) in _letterKeys)
                textBlock.Text = isUppercase ? keyModel.DisplayText.ToUpper() : keyModel.DisplayText.ToLower();
            foreach (var (textBlock, keyModel) in _symbolKeys)
                textBlock.Text = shiftDown ? keyModel.ShiftDisplayText : keyModel.DisplayText;

            // Actualizar teclas de estado (Bloq Mayus / Shift)
            if (_capsLockBorder != null) _capsLockBorder.Background = capsLockOn ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
            if (_leftShiftBorder != null) _leftShiftBorder.Background = shiftDown ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
            if (_rightShiftBorder != null) _rightShiftBorder.Background = shiftDown ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
        }
        #endregion

        #region Input Handlers (Mouse & Keyboard)

        // Clic del ratón en una tecla virtual
        private async void Key_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is KeyModel keyModel)
            {
                e.Handled = true;

                // 1. Sonido
                PlaySoundForKey(keyModel.WpfKey);

                // 2. Feedback Visual Inmediato
                border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");

                // 3. Simular pulsación
                _isSimulating = true;
                _keyboardService.SimulateKeyPress(keyModel.KeyCode);
                _isSimulating = false;

                // 4. Esperar y actualizar estado
                await Task.Delay(50);
                UpdateKeyboardState();

                // 5. Quitar feedback visual si no es tecla de estado
                if (keyModel.WpfKey != Key.Capital && keyModel.WpfKey != Key.LeftShift && keyModel.WpfKey != Key.RightShift)
                {
                    await Task.Delay(50);
                    border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
                }
            }
        }

        // Hooks del teclado físico
        private void StartKeyboardHook() { _keyboardHookService.KeyDown += KeyboardHookService_KeyDown; _keyboardHookService.KeyUp += KeyboardHookService_KeyUp; _keyboardHookService.Start(); }
        private void StopKeyboardHook() { _keyboardHookService.KeyDown -= KeyboardHookService_KeyDown; _keyboardHookService.KeyUp -= KeyboardHookService_KeyUp; _keyboardHookService.Stop(); }

        private void KeyboardHookService_KeyDown(object sender, Key e)
        {
            if (_isSimulating) return;
            Dispatcher.Invoke(() =>
            {
                PlaySoundForKey(e);
                HighlightKey(e, true);
            });
        }

        private void KeyboardHookService_KeyUp(object sender, Key e)
        {
            if (_isSimulating) return;
            Dispatcher.Invoke(() => HighlightKey(e, false));
        }

        private void HighlightKey(Key key, bool isPressed)
        {
            // Ignorar teclas de estado aquí (las maneja UpdateKeyboardState)
            if (key == Key.Capital || key == Key.LeftShift || key == Key.RightShift) return;

            if (_wpfKeyToBorderMap.TryGetValue(key, out Border border))
            {
                if (isPressed) border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
                else border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
            }
        }

        private void PlaySoundForKey(Key key)
        {
            if (key == Key.Space || key == Key.Return || key == Key.Back ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.Capital || key == Key.Tab ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt)
            {
                _soundService.PlayModifier();
            }
            else
            {
                _soundService.PlayClick();
            }
        }
        #endregion

        #region UI Logic (Themes, Menu, Chrome)

        // --- Lógica del Menú Animado (Hamburguesa) ---
        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggleButton)
            {
                if (toggleButton.IsChecked == true)
                {
                    if (toggleButton.ContextMenu != null)
                    {
                        toggleButton.ContextMenu.PlacementTarget = toggleButton;
                        toggleButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                        toggleButton.ContextMenu.IsOpen = true;
                    }
                }
                else
                {
                    if (toggleButton.ContextMenu != null) toggleButton.ContextMenu.IsOpen = false;
                }
            }
        }

        private void MainMenu_Closed(object sender, RoutedEventArgs e)
        {
            // Resetear el botón de hamburguesa a su estado original (3 líneas)
            if (sender is ContextMenu menu && menu.PlacementTarget is System.Windows.Controls.Primitives.ToggleButton toggleButton)
            {
                toggleButton.IsChecked = false;
            }
        }

        // --- Opciones Generales ---
        private void Opacity_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double o)) MainBorder.Opacity = o; }
        private void Zoom_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double f)) { WindowScaleTransform.ScaleX = f; WindowScaleTransform.ScaleY = f; } }
        private void Theme_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string th) ApplyTheme(th); }
        private void SoundToggle_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi) _soundService.IsEnabled = mi.IsChecked; }
        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) => Close();

        // --- Temas y Blur ---
        private void ApplyTheme(string themeName)
        {
            try
            {
                string fileName;
                WindowBlur.EnableBlur(this, false); // Reset Blur

                if (themeName == "System")
                {
                    var v = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                    fileName = (v != null && (int)v == 1) ? "LightTheme.xaml" : "DarkTheme.xaml";
                }
                else if (themeName == "LiquidGlass")
                {
                    fileName = "LiquidGlassTheme.xaml";
                    WindowBlur.EnableBlur(this, true); // Activar Blur
                }
                else if (themeName == "Light") fileName = "LightTheme.xaml";
                else fileName = "DarkTheme.xaml";

                var u = new Uri($"Themes/{fileName}", UriKind.Relative);
                var s = new ResourceDictionary { Source = new Uri("Styles.xaml", UriKind.Relative) };
                var t = new ResourceDictionary { Source = u };
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(s);
                Application.Current.Resources.MergedDictionaries.Add(t);
            }
            catch (Exception ex) { MessageBox.Show($"Error applying theme: {ex.Message}"); }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) { if (e.Category == UserPreferenceCategory.General) Dispatcher.Invoke(() => ApplyTheme("System")); }

        // --- Botones de la ventana ---
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed && !e.Handled) DragMove(); }
        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, MouseButtonEventArgs e) => Close();

        // --- Lógica para hacer la ventana "No Activable" (No roba foco) ---
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_NOACTIVATE);
        }

        #region Window Blur Class
        internal static class WindowBlur
        {
            [DllImport("user32.dll")] internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
            [StructLayout(LayoutKind.Sequential)] internal struct WindowCompositionAttributeData { public WindowCompositionAttribute Attribute; public IntPtr Data; public int SizeOfData; }
            internal enum WindowCompositionAttribute { WCA_ACCENT_POLICY = 19 }
            internal enum AccentState { ACCENT_DISABLED = 0, ACCENT_ENABLE_BLURBEHIND = 3, ACCENT_ENABLE_ACRYLICBLURBEHIND = 4 }
            [StructLayout(LayoutKind.Sequential)] internal struct AccentPolicy { public AccentState AccentState; public int AccentFlags; public int GradientColor; public int AnimationId; }
            public static void EnableBlur(Window window, bool enable)
            {
                var windowHelper = new WindowInteropHelper(window);
                var accent = new AccentPolicy { AccentState = enable ? AccentState.ACCENT_ENABLE_BLURBEHIND : AccentState.ACCENT_DISABLED, GradientColor = 0 };
                var accentStructSize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData { Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY, SizeOfData = accentStructSize, Data = accentPtr };
                SetWindowCompositionAttribute(windowHelper.Handle, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
        }
        #endregion
        #endregion
    }
}