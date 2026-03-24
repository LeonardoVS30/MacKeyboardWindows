using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Linq;
using System.Windows.Media.Imaging;
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
        [DllImport("user32.dll")] public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int VK_CAPITAL = 0x14;
        private const int VK_SHIFT = 0x10;

        // Mensajes de Windows para redimensionado
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_SIZING = 0x0214;

        // Códigos de retorno para áreas de la ventana
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]

        public struct WindowCompositionAttributeData { public int Attribute; public IntPtr Data; public int SizeOfData; }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy { public int AccentState; public int AccentFlags; public int GradientColor; public int AnimationId; }
        #endregion

        #region Fields
        private readonly KeyboardService _keyboardService;
        private readonly KeyboardHookService _keyboardHookService;
        private readonly SoundService _soundService;
        private readonly DispatcherTimer _keyboardStateTimer;

        private readonly Dictionary<Key, Border> _wpfKeyToBorderMap = new Dictionary<Key, Border>();
        private readonly List<Tuple<TextBlock, KeyModel>> _letterKeys = new List<Tuple<TextBlock, KeyModel>>();
        private readonly List<Tuple<TextBlock, KeyModel>> _symbolKeys = new List<Tuple<TextBlock, KeyModel>>();

        private Border _capsLockBorder, _leftShiftBorder, _rightShiftBorder;
        private bool _isSimulating = false;
        private string _currentLayout = "ES";
        private string _currentMode = "Keyboard";
        private string _currentScale = "C_Major";

        // Mapeo de teclas WPF a Borders del piano para resaltado por hook
        private readonly Dictionary<Key, Border> _pianoKeyToBorderMap = new Dictionary<Key, Border>();

        // Parámetros de diseño y redimensionado
        private const double DesignWidth = 1100.0;
        private const double DesignHeight = 350.0;
        private const double ASPECT_RATIO = 1100.0 / 350.0;
        private const int RESIZE_MARGIN = 15; // Sensibilidad para agarrar los bordes
        private bool _isInternalScaling = false;
        #endregion

        public MainWindow()
        {
            LoadThemeResources("System");
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

        #region Window Lifetime & Resizing Logic
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartKeyboardHook();
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            _keyboardStateTimer.Start();

            var settings = Properties.Settings.Default;
            if (settings.Opacity > 0.1) MainBorder.Opacity = settings.Opacity;

            // Restaurar zoom y tamaño de ventana
            double savedZoom = settings.Zoom > 0.1 ? settings.Zoom : 1.0;
            ApplyZoom(savedZoom, false);

            _soundService.IsEnabled = settings.SoundEnabled;
            if (SoundMenuItem != null) SoundMenuItem.IsChecked = settings.SoundEnabled;

            if (!string.IsNullOrEmpty(settings.Layout)) _currentLayout = settings.Layout;
            LoadLayout(_currentLayout);
            if (!string.IsNullOrEmpty(settings.Theme)) ApplyTheme(settings.Theme);
            if (StartupMenuItem != null) StartupMenuItem.IsChecked = IsStartupEnabled();

            // Restaurar escala guardada
            if (!string.IsNullOrEmpty(settings.Scale)) _currentScale = settings.Scale;

            // Restaurar modo guardado
            string savedMode = settings.Mode;
            if (savedMode == "Piano")
            {
                _currentMode = "Piano";
                PianoModeToggle.IsChecked = true;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;

            // No activar al hacer clic (para que no robe el foco de otras apps)
            SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);

            // Registrar el Hook para el redimensionado manual
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);
        }

        // Constantes de WM_SIZING wParam (borde/esquina que se arrastra)
        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SIZING)
            {
                RECT rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                int edge = wParam.ToInt32();

                int width = rect.Right - rect.Left;
                int newHeight = (int)(width / ASPECT_RATIO);

                // Ajustar el borde correcto según desde dónde se arrastra
                if (edge == WMSZ_TOP || edge == WMSZ_TOPLEFT || edge == WMSZ_TOPRIGHT)
                {
                    // Arrastrar desde arriba: Bottom se queda fijo, Top se ajusta
                    rect.Top = rect.Bottom - newHeight;
                }
                else
                {
                    // Arrastrar desde abajo o los lados: Top se queda fijo, Bottom se ajusta
                    rect.Bottom = rect.Top + newHeight;
                }

                Marshal.StructureToPtr(rect, lParam, true);

                double factor = width / 1100.0;
                Properties.Settings.Default.Zoom = factor;
                Properties.Settings.Default.Save();
            }
            return IntPtr.Zero;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                MainBorder.Opacity = Properties.Settings.Default.Opacity > 0.1 ? Properties.Settings.Default.Opacity : 1.0;
                ApplyZoom(Properties.Settings.Default.Zoom, false);
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            StopKeyboardHook();
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            _keyboardStateTimer.Stop();
        }
        #endregion

        #region Zoom & Appearance Logic
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double f))
                ApplyZoom(f, true);
        }

        private void ApplyZoom(double factor, bool animate)
        {
            _isInternalScaling = true;
            double targetWidth = DesignWidth * factor;
            double targetHeight = DesignHeight * factor;

            if (animate)
            {
                var duration = TimeSpan.FromMilliseconds(400);
                var ease = new QuinticEase { EasingMode = EasingMode.EaseInOut };

                // Solo animamos el tamaño de ventana; el Viewbox se encarga de escalar el contenido
                this.BeginAnimation(Window.WidthProperty, new DoubleAnimation(targetWidth, duration) { EasingFunction = ease });
                var hAnim = new DoubleAnimation(targetHeight, duration) { EasingFunction = ease };
                hAnim.Completed += (s, e) => _isInternalScaling = false;
                this.BeginAnimation(Window.HeightProperty, hAnim);
            }
            else
            {
                this.Width = targetWidth;
                this.Height = targetHeight;
                _isInternalScaling = false;
            }

            Properties.Settings.Default.Zoom = factor;
            Properties.Settings.Default.Save();
        }

        private void Opacity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double o))
            {
                MainBorder.Opacity = o;
                Properties.Settings.Default.Opacity = o;
                Properties.Settings.Default.Save();
            }
        }
        #endregion

        #region Layout Construction
        private void LoadLayout(string name)
        {
            _currentLayout = name;
            var layout = LayoutFactory.GetLayout(name);
            BuildKeyboardUI(layout);
            UpdateKeyboardState();
        }

        private void BuildKeyboardUI(KeyboardLayout layout)
        {
            KeyboardContainer.Children.Clear();
            KeyboardContainer.RowDefinitions.Clear();
            _wpfKeyToBorderMap.Clear(); _letterKeys.Clear(); _symbolKeys.Clear();

            for (int i = 0; i < layout.Count; i++)
                KeyboardContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int rowIndex = 0; rowIndex < layout.Count; rowIndex++)
            {
                var keyRowModel = layout[rowIndex];
                var rowGrid = new Grid();
                foreach (var km in keyRowModel) rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(km.WidthFactor, GridUnitType.Star) });

                for (int colIndex = 0; colIndex < keyRowModel.Count; colIndex++)
                {
                    var keyModel = keyRowModel[colIndex];
                    UIElement contentElement;

                    if (keyModel.DisplayText.StartsWith("ICON_"))
                    {
                        var path = new System.Windows.Shapes.Path { Style = (Style)FindResource("IconPathStyle"), Stretch = Stretch.Uniform, Height = 14, Width = 14 };
                        string rk = keyModel.DisplayText switch { "ICON_DELETE" => "IconDelete", "ICON_MENU" => "IconMenu", "ICON_WINDOWS" => "IconWindows", _ => "" };
                        if (!string.IsNullOrEmpty(rk)) path.Data = (Geometry)FindResource(resourceKey: rk);
                        contentElement = path;
                    }
                    else
                    {
                        var tb = new TextBlock { Style = (Style)FindResource(keyModel.DisplayText.Length > 2 ? "SmallKeyTextStyle" : "KeyTextStyle") };
                        if (keyModel.DisplayText.Contains("\n")) { var p = keyModel.DisplayText.Split('\n'); tb.Inlines.Add(p[0]); tb.Inlines.Add(new System.Windows.Documents.LineBreak()); tb.Inlines.Add(p[1]); tb.TextAlignment = TextAlignment.Center; }
                        else tb.Text = keyModel.DisplayText;
                        if (keyModel.IsLetter) _letterKeys.Add(Tuple.Create(tb, keyModel));
                        else if (!string.IsNullOrEmpty(keyModel.ShiftDisplayText)) _symbolKeys.Add(Tuple.Create(tb, keyModel));
                        contentElement = tb;
                    }

                    var border = new Border { Style = (Style)FindResource("KeyStyle"), Child = contentElement, Tag = keyModel, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new ScaleTransform(1.0, 1.0) };
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

        #region Keyboard Interaction & Animation
        private async void Key_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is KeyModel km)
            {
                e.Handled = true; PlaySoundForKey(km.WpfKey);
                border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
                AnimateKeyPress(border);
                _isSimulating = true; _keyboardService.SimulateKeyPress(km.KeyCode); _isSimulating = false;
                await Task.Delay(50); UpdateKeyboardState();
                if (km.WpfKey != Key.Capital && km.WpfKey != Key.LeftShift && km.WpfKey != Key.RightShift) { await Task.Delay(50); border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor"); AnimateKeyRelease(border); }
            }
        }

        private void AnimateKeyPress(Border b) { if (b.RenderTransform is ScaleTransform st) { if (st.IsFrozen) b.RenderTransform = st.Clone(); st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.92, TimeSpan.FromMilliseconds(50))); st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.92, TimeSpan.FromMilliseconds(50))); } }
        private void AnimateKeyRelease(Border b) { if (b.RenderTransform is ScaleTransform st) { if (st.IsFrozen) b.RenderTransform = st.Clone(); var a = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut } }; st.BeginAnimation(ScaleTransform.ScaleXProperty, a); st.BeginAnimation(ScaleTransform.ScaleYProperty, a); } }
        private void PlaySoundForKey(Key k) { if (k == Key.Space || k == Key.Return || k == Key.Back || k == Key.LeftShift || k == Key.RightShift || k == Key.Capital || k == Key.Tab || k == Key.LeftCtrl || k == Key.RightCtrl || k == Key.LeftAlt || k == Key.RightAlt) _soundService.PlayModifier(); else _soundService.PlayClick(); }

        private void StartKeyboardHook() { _keyboardHookService.KeyDown += KeyboardHookService_KeyDown; _keyboardHookService.KeyUp += KeyboardHookService_KeyUp; _keyboardHookService.Start(); }
        private void StopKeyboardHook() { _keyboardHookService.KeyDown -= KeyboardHookService_KeyDown; _keyboardHookService.KeyUp -= KeyboardHookService_KeyUp; _keyboardHookService.Stop(); }
        private void KeyboardHookService_KeyDown(object sender, Key e) { if (_isSimulating) return; Dispatcher.Invoke(() => { PlaySoundForKey(e); HighlightKey(e, true); HighlightPianoKey(e, true); }); }
        private void KeyboardHookService_KeyUp(object sender, Key e) { if (_isSimulating) return; Dispatcher.Invoke(() => { HighlightKey(e, false); HighlightPianoKey(e, false); }); }
        private void HighlightKey(Key k, bool p) { if (k == Key.Capital || k == Key.LeftShift || k == Key.RightShift) return; if (_wpfKeyToBorderMap.TryGetValue(k, out Border b)) { if (p) { b.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor"); AnimateKeyPress(b); } else { b.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor"); AnimateKeyRelease(b); } } }
        #endregion

        #region Theme & Tray
        private void ApplyTheme(string themeName)
        {
            try
            {
                if (MainBorder.ActualWidth > 0)
                {
                    // Capturar el estado actual antes de cambiar el tema
                    TransitionVisual.Fill = new VisualBrush(MainBorder) { Stretch = Stretch.None };
                    TransitionOverlay.Visibility = Visibility.Visible;
                }

                LoadThemeResources(themeName);

                if (TransitionOverlay.Visibility == Visibility.Visible)
                {
                    var mask = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
                    var s1 = new GradientStop(Colors.Transparent, -0.2);
                    var s2 = new GradientStop(Colors.Black, -0.1);
                    mask.GradientStops.Add(s1);
                    mask.GradientStops.Add(s2);
                    TransitionVisual.OpacityMask = mask;

                    var dur = TimeSpan.FromMilliseconds(800);
                    var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

                    s1.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(1.0, dur) { EasingFunction = ease });
                    s2.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(1.1, dur) { EasingFunction = ease });

                    TransitionGlassWave.Opacity = 0.8;
                    // Animamos la onda desde bien afuera (-150) hasta bien afuera (1200)
                    var wAnim = new DoubleAnimation(-150, 1200, dur) { EasingFunction = ease };

                    wAnim.Completed += (s, a) =>
                    {
                        // LIMPIEZA TOTAL:
                        TransitionOverlay.Visibility = Visibility.Collapsed;
                        TransitionVisual.Fill = null;
                        TransitionVisual.OpacityMask = null;
                        // Detener animaciones para que no consuman recursos ni se vean
                        GlassWaveTranslate.BeginAnimation(TranslateTransform.XProperty, null);
                        TransitionGlassWave.BeginAnimation(Border.OpacityProperty, null);
                    };

                    GlassWaveTranslate.BeginAnimation(TranslateTransform.XProperty, wAnim);
                }
            }
            catch { }
        }

        private void LoadThemeResources(string themeName)
        {
            string fileName; EnableBlur(themeName == "LiquidGlass");
            if (themeName == "System") { var v = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1); fileName = (v != null && (int)v == 1) ? "LightTheme.xaml" : "DarkTheme.xaml"; }
            else if (themeName == "LiquidGlass") fileName = "LiquidGlassTheme.xaml";
            else if (themeName == "Light") fileName = "LightTheme.xaml"; else fileName = "DarkTheme.xaml";
            var s = new ResourceDictionary { Source = new Uri("Styles.xaml", UriKind.Relative) };
            var t = new ResourceDictionary { Source = new Uri($"Themes/{fileName}", UriKind.Relative) };
            Application.Current.Resources.MergedDictionaries.Clear(); Application.Current.Resources.MergedDictionaries.Add(s); Application.Current.Resources.MergedDictionaries.Add(t);
        }

        private void EnableBlur(bool enable)
        {
            var helper = new WindowInteropHelper(this);
            var accent = new AccentPolicy { AccentState = enable ? 3 : 0 };
            var accentSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData { Attribute = 19, SizeOfData = accentSize, Data = accentPtr };
            SetWindowCompositionAttribute(helper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) { if (e.Category == UserPreferenceCategory.General) Dispatcher.Invoke(() => ApplyTheme("System")); }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(this);
            // Solo DragMove si no estamos en el margen de redimensionado
            if (p.X > 10 && p.X < ActualWidth - 10 && p.Y > 10 && p.Y < ActualHeight - 10)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { this.DragMove(); } catch { }
                }
            }
        }

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e) { if (sender is System.Windows.Controls.Primitives.ToggleButton tb && tb.IsChecked == true && tb.ContextMenu != null) { tb.ContextMenu.PlacementTarget = tb; tb.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom; tb.ContextMenu.IsOpen = true; } }
        private void MainMenu_Closed(object sender, RoutedEventArgs e) { if (sender is ContextMenu m && m.PlacementTarget is System.Windows.Controls.Primitives.ToggleButton tb) tb.IsChecked = false; }
        private void Theme_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string th) { ApplyTheme(th); Properties.Settings.Default.Theme = th; Properties.Settings.Default.Save(); } }
        private void SoundToggle_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi) { _soundService.IsEnabled = mi.IsChecked; Properties.Settings.Default.SoundEnabled = mi.IsChecked; Properties.Settings.Default.Save(); } }
        private void Layout_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string l) { LoadLayout(l); Properties.Settings.Default.Layout = l; Properties.Settings.Default.Save(); } }
        private void Scale_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string s)
            {
                _currentScale = s;
                BuildPianoUI();
                Properties.Settings.Default.Scale = s;
                Properties.Settings.Default.Save();
            }
        }
        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) => this.Hide();
        private void NotifyIcon_DoubleClick(object sender, RoutedEventArgs e) => ShowAndRestoreWindow();
        private void NotifyIcon_Open_Click(object sender, RoutedEventArgs e) => ShowAndRestoreWindow();
        private void NotifyIcon_Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void ShowAndRestoreWindow() { this.Show(); this.WindowState = WindowState.Normal; this.Activate(); ApplyZoom(Properties.Settings.Default.Zoom, false); }
        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e) { WindowState = WindowState.Minimized; }
        private void CloseButton_Click(object sender, MouseButtonEventArgs e) => this.Hide();
        private void StartupToggle_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi) SetStartup(mi.IsChecked); }
        private void SetStartup(bool enable) { try { using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)) { if (enable) rk.SetValue("MacKeyboardWindows", Process.GetCurrentProcess().MainModule.FileName); else rk.DeleteValue("MacKeyboardWindows", false); } } catch { } }
        private bool IsStartupEnabled() { using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)) return rk.GetValue("MacKeyboardWindows") != null; }
        #endregion

        #region Mode Toggle
        private void KeyboardModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentMode == "Keyboard") return;
            _currentMode = "Keyboard";
            SwitchMode("Keyboard");
        }

        private void PianoModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentMode == "Piano") return;
            _currentMode = "Piano";
            BuildPianoUI();
            SwitchMode("Piano");
        }

        private void SwitchMode(string mode)
        {
            var showContainer = mode == "Piano" ? PianoContainer : KeyboardContainer;
            var hideContainer = mode == "Piano" ? KeyboardContainer : PianoContainer;

            // Animated cross-fade transition
            var duration = TimeSpan.FromMilliseconds(250);
            var ease = new QuinticEase { EasingMode = EasingMode.EaseInOut };

            var fadeOut = new DoubleAnimation(1, 0, duration) { EasingFunction = ease };
            fadeOut.Completed += (s, a) =>
            {
                hideContainer.Visibility = Visibility.Collapsed;
                showContainer.Opacity = 0;
                showContainer.Visibility = Visibility.Visible;
                showContainer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
            };
            hideContainer.BeginAnimation(OpacityProperty, fadeOut);

            Properties.Settings.Default.Mode = mode;
            Properties.Settings.Default.Save();
        }
        #endregion

        #region Piano UI Construction
        private void BuildPianoUI()
        {
            PianoContainer.Children.Clear();
            _pianoKeyToBorderMap.Clear();

            // Obtener notas de la escala con sus atajos de teclado
            var scaleKeys = PianoLayout.GetKeys(_currentScale);
            var scaleLookup = new Dictionary<string, PianoKeyModel>();
            foreach (var sk in scaleKeys)
                scaleLookup[$"{sk.NoteName}{sk.Octave}"] = sk;

            // Generar TODAS las notas cromáticas de C3 a G7
            var allNotes = new List<(string name, int octave, bool isBlack)>();
            for (int oct = 3; oct <= 6; oct++)
                foreach (var n in NoteNames.All)
                    allNotes.Add((n, oct, NoteNames.IsBlack(n)));
            // Octava 7 parcial: C C# D D# E F F# G
            foreach (var n in new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G" })
                allNotes.Add((n, 7, NoteNames.IsBlack(n)));

            var whiteNotes = allNotes.Where(n => !n.isBlack).ToList();
            int whiteKeyCount = whiteNotes.Count;

            // Canvas principal del piano
            var pianoGrid = new Grid();

            // === CAPA 1: Teclas blancas ===
            var whiteKeysGrid = new Grid();
            for (int i = 0; i < whiteKeyCount; i++)
                whiteKeysGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < whiteKeyCount; i++)
            {
                var note = whiteNotes[i];
                string noteId = $"{note.name}{note.octave}";
                bool inScale = scaleLookup.TryGetValue(noteId, out PianoKeyModel pk);

                var border = new Border
                {
                    Style = (Style)FindResource("WhitePianoKeyStyle"),
                    Tag = pk,
                    RenderTransformOrigin = new Point(0.5, 1),
                    RenderTransform = new ScaleTransform(1, 1),
                    Opacity = inScale ? 1.0 : 0.35,
                    IsHitTestVisible = inScale
                };

                // Contenido: nota + atajo de teclado
                var content = new Grid();

                if (inScale)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = noteId,
                        FontFamily = new FontFamily("./Fonts/#Inter"),
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (Brush)FindResource("PianoBlackKeyColor"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 12)
                    });
                    content.Children.Add(new TextBlock
                    {
                        Text = pk.KeyLabel,
                        FontFamily = new FontFamily("./Fonts/#Inter"),
                        FontSize = 9,
                        Foreground = (Brush)FindResource("PianoLabelColor"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 6)
                    });
                }
                else if (note.name == "C")
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = $"C{note.octave}",
                        Style = (Style)FindResource("PianoOctaveLabelStyle")
                    });
                }

                border.Child = content;
                border.MouseLeftButtonDown += PianoKey_MouseLeftButtonDown;
                border.MouseLeftButtonUp += PianoKey_MouseLeftButtonUp;
                Grid.SetColumn(border, i);
                whiteKeysGrid.Children.Add(border);

                if (pk != null)
                    _pianoKeyToBorderMap[pk.WpfKey] = border;
            }

            pianoGrid.Children.Add(whiteKeysGrid);

            // === CAPA 2: Teclas negras (Canvas superpuesto) ===
            var blackKeysCanvas = new Canvas { IsHitTestVisible = true };
            pianoGrid.Children.Add(blackKeysCanvas);

            // Posicionar las teclas negras cuando se conozca el tamaño
            pianoGrid.SizeChanged += (s, e2) =>
            {
                blackKeysCanvas.Children.Clear();
                if (whiteKeyCount == 0) return;

                double totalWidth = pianoGrid.ActualWidth;
                double totalHeight = pianoGrid.ActualHeight;
                double whiteKeyWidth = totalWidth / whiteKeyCount;
                double blackKeyWidth = whiteKeyWidth * 0.58;
                double blackKeyHeight = totalHeight * 0.62;

                int whiteIndex = 0;
                for (int i = 0; i < allNotes.Count; i++)
                {
                    var note = allNotes[i];
                    if (!note.isBlack)
                    {
                        whiteIndex++;
                        continue;
                    }

                    string noteId = $"{note.name}{note.octave}";
                    bool inScale = scaleLookup.TryGetValue(noteId, out PianoKeyModel pk);

                    double xPos = (whiteIndex * whiteKeyWidth) - (blackKeyWidth / 2);

                    var border = new Border
                    {
                        Style = (Style)FindResource("BlackPianoKeyStyle"),
                        Width = blackKeyWidth,
                        Height = blackKeyHeight,
                        Tag = pk,
                        RenderTransformOrigin = new Point(0.5, 0),
                        RenderTransform = new ScaleTransform(1, 1),
                        Opacity = inScale ? 1.0 : 0.25,
                        IsHitTestVisible = inScale
                    };

                    if (inScale)
                    {
                        var content = new Grid();
                        content.Children.Add(new TextBlock
                        {
                            Text = noteId,
                            FontFamily = new FontFamily("./Fonts/#Inter"),
                            FontSize = 9,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 10)
                        });
                        content.Children.Add(new TextBlock
                        {
                            Text = pk.KeyLabel,
                            FontFamily = new FontFamily("./Fonts/#Inter"),
                            FontSize = 8,
                            Foreground = Brushes.White,
                            Opacity = 0.6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Margin = new Thickness(0, 0, 0, 4)
                        });
                        border.Child = content;
                    }

                    border.MouseLeftButtonDown += PianoKey_MouseLeftButtonDown;
                    border.MouseLeftButtonUp += PianoKey_MouseLeftButtonUp;

                    Canvas.SetLeft(border, xPos);
                    Canvas.SetTop(border, 0);
                    blackKeysCanvas.Children.Add(border);

                    if (pk != null)
                        _pianoKeyToBorderMap[pk.WpfKey] = border;
                }
            };

            PianoContainer.Children.Add(pianoGrid);
        }
        #endregion

        #region Piano Key Interaction
        private void PianoKey_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is PianoKeyModel pk)
            {
                e.Handled = true;
                _soundService.PlayClick();

                // Visual feedback
                string pressedResource = pk.IsBlackKey ? "PianoBlackKeyPressedColor" : "PianoWhiteKeyPressedColor";
                border.Background = (Brush)FindResource(pressedResource);
                AnimateKeyPress(border);

                // Simulate keypress for DAW
                _isSimulating = true;
                _keyboardService.SimulateKeyPress(pk.KeyCode);
                _isSimulating = false;
            }
        }

        private void PianoKey_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is PianoKeyModel pk)
            {
                string normalResource = pk.IsBlackKey ? "PianoBlackKeyColor" : "PianoWhiteKeyColor";
                border.SetResourceReference(Border.BackgroundProperty, normalResource);
                AnimateKeyRelease(border);
            }
        }

        private void HighlightPianoKey(Key k, bool pressed)
        {
            if (_currentMode != "Piano") return;
            if (_pianoKeyToBorderMap.TryGetValue(k, out Border b))
            {
                var pk = b.Tag as PianoKeyModel;
                if (pk == null) return;
                if (pressed)
                {
                    string pressedResource = pk.IsBlackKey ? "PianoBlackKeyPressedColor" : "PianoWhiteKeyPressedColor";
                    b.Background = (Brush)FindResource(pressedResource);
                    AnimateKeyPress(b);
                }
                else
                {
                    string normalResource = pk.IsBlackKey ? "PianoBlackKeyColor" : "PianoWhiteKeyColor";
                    b.SetResourceReference(Border.BackgroundProperty, normalResource);
                    AnimateKeyRelease(b);
                }
            }
        }
        #endregion

        #region Keyboard State
        private bool IsCapsLocked() => (GetKeyState(VK_CAPITAL) & 1) != 0;
        private bool IsShiftPressed() => (GetKeyState(VK_SHIFT) & 0x8000) != 0;
        private void UpdateKeyboardState()
        {
            bool caps = IsCapsLocked(); bool shift = IsShiftPressed(); bool upper = caps ^ shift;
            foreach (var (tb, km) in _letterKeys) tb.Text = upper ? km.DisplayText.ToUpper() : km.DisplayText.ToLower();
            foreach (var (tb, km) in _symbolKeys) tb.Text = shift ? km.ShiftDisplayText : km.DisplayText;
            if (_capsLockBorder != null) _capsLockBorder.Background = caps ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
            if (_leftShiftBorder != null) _leftShiftBorder.Background = shift ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
            if (_rightShiftBorder != null) _rightShiftBorder.Background = shift ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
        }
        #endregion
    }
}