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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

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
            InitializeComponent();

            _keyboardService = new KeyboardService();
            _keyboardHookService = new KeyboardHookService();
            _soundService = new SoundService();

            _keyboardStateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _keyboardStateTimer.Tick += (s, e) => UpdateKeyboardState();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                // 1. Eliminar animaciones de Opacidad
                MainBorder.BeginAnimation(Border.OpacityProperty, null);
                MainBorder.Opacity = 1.0;

                // 2. Eliminar animaciones de Transformación
                if (MainBorder.RenderTransform is TransformGroup tg)
                {
                    if (tg.Children[0] is ScaleTransform st)
                    {
                        st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                        st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                        st.ScaleX = 1.0;
                        st.ScaleY = 1.0;
                    }
                    if (tg.Children[1] is TranslateTransform tt)
                    {
                        tt.BeginAnimation(TranslateTransform.YProperty, null);
                        tt.Y = 0;
                    }
                }
            }
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

        private void LoadLayout(string name)
        {
            _currentLayout = name;
            var layout = LayoutFactory.GetLayout(name);

            // Animación de cambio de layout (Fade Out -> Rebuild -> Fade In)
            var duration = TimeSpan.FromMilliseconds(150);
            var fadeOut = new DoubleAnimation(0, duration);
            var fadeIn = new DoubleAnimation(1, duration);

            fadeOut.Completed += (s, e) =>
            {
                BuildKeyboardUI(layout);
                UpdateKeyboardState();
                KeyboardContainer.BeginAnimation(OpacityProperty, fadeIn);
            };

            KeyboardContainer.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void Layout_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string layoutCode)
            {
                LoadLayout(layoutCode);
            }
        }

        private void BuildKeyboardUI(List<KeyRow> layout)
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
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double f))
            {
                // Animación suave de escala (Apple-style: CubicEaseInOut)
                var duration = TimeSpan.FromMilliseconds(300);
                var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

                var animX = new DoubleAnimation(f, duration) { EasingFunction = ease };
                var animY = new DoubleAnimation(f, duration) { EasingFunction = ease };

                WindowScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
                WindowScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
            }
        }
        private void Theme_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string th) ApplyTheme(th); }
        private void SoundToggle_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi) _soundService.IsEnabled = mi.IsChecked; }
        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) => Close();

        // --- Temas y Blur ---
        private void LoadThemeResources(string themeName)
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
            var stylesDict = new ResourceDictionary { Source = new Uri("Styles.xaml", UriKind.Relative) };
            var themeDict = new ResourceDictionary { Source = u };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(stylesDict);
            Application.Current.Resources.MergedDictionaries.Add(themeDict);
        }

        private void ApplyTheme(string themeName)
        {
            try
            {
                // 1. Capture Snapshot of the current state (Old Theme)
                if (MainBorder.ActualWidth > 0 && MainBorder.ActualHeight > 0)
                {
                    // Render the MainBorder to a bitmap
                    var rtb = new RenderTargetBitmap((int)MainBorder.ActualWidth, (int)MainBorder.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(MainBorder);
                    
                    // Set as source for the transition image
                    TransitionImage.Source = rtb;
                    TransitionOverlay.Visibility = Visibility.Visible;
                }

                // 2. Change Theme Resources (Underneath the overlay)
                LoadThemeResources(themeName);

                // 3. Setup Transition Animation
                // We want to wipe the Old Theme (TransitionImage) away from Left to Right.
                // Mask: Gradient [Transparent ... Black]. Move Transparent region from Left to Right.
                
                var maskBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                
                // Initial state: All Black (Visible).
                // We start with the transparent part off-screen to the left.
                var stopTransparent = new GradientStop(Colors.Transparent, -0.2);
                var stopBlack = new GradientStop(Colors.Black, -0.1); 
                
                maskBrush.GradientStops.Add(stopTransparent);
                maskBrush.GradientStops.Add(stopBlack);
                
                TransitionImage.OpacityMask = maskBrush;

                // Animation Parameters
                var duration = TimeSpan.FromMilliseconds(800); // Slower, liquid feel
                var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

                // Animate Offsets to move the "Transparent" window across the screen
                var animTransparent = new DoubleAnimation(1.0, duration) { EasingFunction = ease };
                var animBlack = new DoubleAnimation(1.1, duration) { EasingFunction = ease };
                
                stopTransparent.BeginAnimation(GradientStop.OffsetProperty, animTransparent);
                stopBlack.BeginAnimation(GradientStop.OffsetProperty, animBlack);

                // 4. Animate Glass Wave (The "Liquid" edge)
                // It should follow the edge of the mask.
                TransitionGlassWave.Opacity = 0.8;
                var waveWidth = TransitionGlassWave.Width;
                var startX = -waveWidth;
                var endX = MainBorder.ActualWidth;
                
                var waveAnim = new DoubleAnimation(startX, endX, duration) { EasingFunction = ease };
                
                GlassWaveTranslate.BeginAnimation(TranslateTransform.XProperty, waveAnim);
                
                // Cleanup when done
                waveAnim.Completed += (s, args) =>
                {
                    TransitionOverlay.Visibility = Visibility.Collapsed;
                    TransitionImage.Source = null;
                    TransitionImage.OpacityMask = null;
                    // Reset wave
                    GlassWaveTranslate.BeginAnimation(TranslateTransform.XProperty, null);
                    GlassWaveTranslate.X = -100;
                };
            }
            catch (Exception ex) { MessageBox.Show($"Error applying theme: {ex.Message}"); }
        }


        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) { if (e.Category == UserPreferenceCategory.General) Dispatcher.Invoke(() => ApplyTheme("System")); }

        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e)
        {
            // Animación de minimizar estilo macOS (Genie-like approximation: Scale Down + Translate Down + Fade Out)
            var duration = TimeSpan.FromMilliseconds(350);
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

            var scaleX = new DoubleAnimation(0.1, duration) { EasingFunction = ease };
            var scaleY = new DoubleAnimation(0.1, duration) { EasingFunction = ease };
            var translateY = new DoubleAnimation(200, duration) { EasingFunction = ease }; // Mover hacia abajo
            var fade = new DoubleAnimation(0, duration) { EasingFunction = ease };

            var sb = new Storyboard();

            Storyboard.SetTargetName(scaleX, "MainBorderScale");
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            Storyboard.SetTargetName(scaleY, "MainBorderScale");
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));

            Storyboard.SetTargetName(translateY, "MainBorderTranslate");
            Storyboard.SetTargetProperty(translateY, new PropertyPath(TranslateTransform.YProperty));

            Storyboard.SetTarget(fade, MainBorder);
            Storyboard.SetTargetProperty(fade, new PropertyPath(Border.OpacityProperty));

            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);
            sb.Children.Add(translateY);
            sb.Children.Add(fade);

            sb.Completed += (s, args) =>
            {
                WindowState = WindowState.Minimized;
                
                // Detener el storyboard para liberar las propiedades
                sb.Stop(this);

                // Resetear estado inmediatamente (aunque no se vea)
                if (MainBorder.RenderTransform is TransformGroup tg)
                {
                    if (tg.Children[0] is ScaleTransform st) { st.ScaleX = 1.0; st.ScaleY = 1.0; }
                    if (tg.Children[1] is TranslateTransform tt) { tt.Y = 0; }
                }
                MainBorder.Opacity = 1.0;
            };

            sb.Begin(this);
        }

        private void CloseButton_Click(object sender, MouseButtonEventArgs e) => Close();

        // --- Lógica para hacer la ventana "No Activable" (No roba foco) ---
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_NOACTIVATE);
            
            // Aplicar tema inicial (ahora que la ventana está lista y MainBorder existe)
            ApplyTheme("System");
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
                if (windowHelper.Handle == IntPtr.Zero) return;

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