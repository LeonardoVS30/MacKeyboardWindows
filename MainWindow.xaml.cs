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
using System.Windows.Media.Animation;
using System.Windows.Shapes; // Necesario para Rectangle
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
        private readonly Dictionary<Key, Border> _wpfKeyToBorderMap = new Dictionary<Key, Border>();
        private readonly List<Tuple<TextBlock, KeyModel>> _letterKeys = new List<Tuple<TextBlock, KeyModel>>();
        private readonly List<Tuple<TextBlock, KeyModel>> _symbolKeys = new List<Tuple<TextBlock, KeyModel>>();
        private Border _capsLockBorder, _leftShiftBorder, _rightShiftBorder;
        private bool _isSimulating = false;
        private string _currentLayout = "ES";
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

        #region Window Lifetime & Events
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
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                MainBorder.BeginAnimation(Border.OpacityProperty, null);
                MainBorder.Opacity = 1.0;
                WindowScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                WindowScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                WindowScaleTransform.ScaleX = 1.0; WindowScaleTransform.ScaleY = 1.0;
            }
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_NOACTIVATE);
        }
        #endregion

        #region Layout & UI Construction
        private void Layout_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string layoutName) LoadLayout(layoutName); }
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
            _wpfKeyToBorderMap.Clear();
            _letterKeys.Clear();
            _symbolKeys.Clear();
            _capsLockBorder = null; _leftShiftBorder = null; _rightShiftBorder = null;

            for (int i = 0; i < layout.Count; i++) KeyboardContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int rowIndex = 0; rowIndex < layout.Count; rowIndex++)
            {
                var keyRowModel = layout[rowIndex];
                var rowGrid = new Grid();
                foreach (var keyModel in keyRowModel) rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(keyModel.WidthFactor, GridUnitType.Star) });

                for (int colIndex = 0; colIndex < keyRowModel.Count; colIndex++)
                {
                    var keyModel = keyRowModel[colIndex];
                    UIElement contentElement;
                    if (keyModel.DisplayText.StartsWith("ICON_"))
                    {
                        var path = new System.Windows.Shapes.Path { Style = (Style)FindResource("IconPathStyle"), Stretch = Stretch.Uniform, Height = 14, Width = 14 };
                        string resourceKey = keyModel.DisplayText switch { "ICON_DELETE" => "IconDelete", "ICON_MENU" => "IconMenu", "ICON_WINDOWS" => "IconWindows", _ => "" };
                        if (!string.IsNullOrEmpty(resourceKey)) path.Data = (Geometry)FindResource(resourceKey);
                        contentElement = path;
                    }
                    else
                    {
                        var textBlock = new TextBlock { Style = (Style)FindResource(keyModel.DisplayText.Length > 2 ? "SmallKeyTextStyle" : "KeyTextStyle") };
                        if (keyModel.DisplayText.Contains("\n")) { var parts = keyModel.DisplayText.Split('\n'); textBlock.Inlines.Add(parts[0]); textBlock.Inlines.Add(new System.Windows.Documents.LineBreak()); textBlock.Inlines.Add(parts[1]); textBlock.TextAlignment = TextAlignment.Center; }
                        else textBlock.Text = keyModel.DisplayText;
                        if (keyModel.IsLetter) _letterKeys.Add(Tuple.Create(textBlock, keyModel));
                        else if (!string.IsNullOrEmpty(keyModel.ShiftDisplayText)) _symbolKeys.Add(Tuple.Create(textBlock, keyModel));
                        contentElement = textBlock;
                    }
                    var border = new Border { Style = (Style)FindResource("KeyStyle"), Child = contentElement, Tag = keyModel };
                    border.RenderTransformOrigin = new Point(0.5, 0.5);
                    border.RenderTransform = new ScaleTransform(1.0, 1.0);
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

        #region Keyboard State & Input
        private bool IsCapsLocked() => (GetKeyState(VK_CAPITAL) & 1) != 0;
        private bool IsShiftPressed() => (GetKeyState(VK_SHIFT) & 0x8000) != 0;
        private void UpdateKeyboardState()
        {
            bool capsLockOn = IsCapsLocked(), shiftDown = IsShiftPressed(), isUppercase = capsLockOn ^ shiftDown;
            foreach (var (textBlock, keyModel) in _letterKeys) textBlock.Text = isUppercase ? keyModel.DisplayText.ToUpper() : keyModel.DisplayText.ToLower();
            foreach (var (textBlock, keyModel) in _symbolKeys) textBlock.Text = shiftDown ? keyModel.ShiftDisplayText : keyModel.DisplayText;
            if (_capsLockBorder != null) _capsLockBorder.Background = capsLockOn ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
            if (_leftShiftBorder != null) _leftShiftBorder.Background = shiftDown ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
            if (_rightShiftBorder != null) _rightShiftBorder.Background = shiftDown ? (Brush)FindResource("KeyBackgroundPressedColor") : (Brush)FindResource("KeyBackgroundColor");
        }
        private async void Key_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is KeyModel keyModel)
            {
                e.Handled = true;
                PlaySoundForKey(keyModel.WpfKey);
                border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor");
                AnimateKeyPress(border);
                _isSimulating = true;
                _keyboardService.SimulateKeyPress(keyModel.KeyCode);
                _isSimulating = false;
                await Task.Delay(50);
                UpdateKeyboardState();
                if (keyModel.WpfKey != Key.Capital && keyModel.WpfKey != Key.LeftShift && keyModel.WpfKey != Key.RightShift)
                {
                    await Task.Delay(50);
                    border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor");
                    AnimateKeyRelease(border);
                }
            }
        }
        private void AnimateKeyPress(Border border) { if (border.RenderTransform is ScaleTransform st) { if (st.IsFrozen) { st = st.Clone(); border.RenderTransform = st; } var anim = new DoubleAnimation(0.92, TimeSpan.FromMilliseconds(50)); st.BeginAnimation(ScaleTransform.ScaleXProperty, anim); st.BeginAnimation(ScaleTransform.ScaleYProperty, anim); } }
        private void AnimateKeyRelease(Border border) { if (border.RenderTransform is ScaleTransform st) { if (st.IsFrozen) { st = st.Clone(); border.RenderTransform = st; } var anim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut } }; st.BeginAnimation(ScaleTransform.ScaleXProperty, anim); st.BeginAnimation(ScaleTransform.ScaleYProperty, anim); } }
        private void PlaySoundForKey(Key key) { if (key == Key.Space || key == Key.Return || key == Key.Back || key == Key.LeftShift || key == Key.RightShift || key == Key.Capital || key == Key.Tab || key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt) _soundService.PlayModifier(); else _soundService.PlayClick(); }
        private void StartKeyboardHook() { _keyboardHookService.KeyDown += KeyboardHookService_KeyDown; _keyboardHookService.KeyUp += KeyboardHookService_KeyUp; _keyboardHookService.Start(); }
        private void StopKeyboardHook() { _keyboardHookService.KeyDown -= KeyboardHookService_KeyDown; _keyboardHookService.KeyUp -= KeyboardHookService_KeyUp; _keyboardHookService.Stop(); }
        private void KeyboardHookService_KeyDown(object sender, Key e) { if (!_isSimulating) Dispatcher.Invoke(() => { PlaySoundForKey(e); HighlightKey(e, true); }); }
        private void KeyboardHookService_KeyUp(object sender, Key e) { if (!_isSimulating) Dispatcher.Invoke(() => HighlightKey(e, false)); }
        private void HighlightKey(Key key, bool isPressed)
        {
            if (key == Key.Capital || key == Key.LeftShift || key == Key.RightShift) return;
            if (_wpfKeyToBorderMap.TryGetValue(key, out Border border))
            {
                if (isPressed) { border.Background = (SolidColorBrush)FindResource("KeyBackgroundPressedColor"); AnimateKeyPress(border); }
                else { border.SetResourceReference(Border.BackgroundProperty, "KeyBackgroundColor"); AnimateKeyRelease(border); }
            }
        }
        #endregion

        #region UI Logic
        private void MenuToggleButton_Click(object sender, RoutedEventArgs e) { if (sender is System.Windows.Controls.Primitives.ToggleButton tb) { if (tb.IsChecked == true && tb.ContextMenu != null) { tb.ContextMenu.PlacementTarget = tb; tb.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom; tb.ContextMenu.IsOpen = true; } else if (tb.ContextMenu != null) tb.ContextMenu.IsOpen = false; } }
        private void MainMenu_Closed(object sender, RoutedEventArgs e) { if (sender is ContextMenu menu && menu.PlacementTarget is System.Windows.Controls.Primitives.ToggleButton tb) tb.IsChecked = false; }
        private void Opacity_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double o)) MainBorder.Opacity = o; }
        private void Zoom_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string t && double.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double f)) { var d = TimeSpan.FromMilliseconds(400); var ease = new QuinticEase { EasingMode = EasingMode.EaseInOut }; WindowScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(f, d) { EasingFunction = ease }); WindowScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(f, d) { EasingFunction = ease }); } }
        private void Theme_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi && mi.Tag is string th) ApplyTheme(th); }
        private void SoundToggle_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem mi) _soundService.IsEnabled = mi.IsChecked; }
        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) => Close();

        private void LoadThemeResources(string themeName)
        {
            string fileName;
            WindowBlur.EnableBlur(this, false);
            if (themeName == "System") { var v = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1); fileName = (v != null && (int)v == 1) ? "LightTheme.xaml" : "DarkTheme.xaml"; }
            else if (themeName == "LiquidGlass") { fileName = "LiquidGlassTheme.xaml"; WindowBlur.EnableBlur(this, true); }
            else if (themeName == "Light") fileName = "LightTheme.xaml"; else fileName = "DarkTheme.xaml";
            var u = new Uri($"Themes/{fileName}", UriKind.Relative);
            var s = new ResourceDictionary { Source = new Uri("Styles.xaml", UriKind.Relative) };
            var t = new ResourceDictionary { Source = u };
            Application.Current.Resources.MergedDictionaries.Clear(); Application.Current.Resources.MergedDictionaries.Add(s); Application.Current.Resources.MergedDictionaries.Add(t);
        }

        private void ApplyTheme(string themeName)
        {
            try
            {
                // 1. Captura "Visual" (Vectorial) del estado actual
                // Usamos VisualBrush para evitar problemas de pixeles y alineación al escalar
                var visualBrush = new VisualBrush(MainBorder)
                {
                    Stretch = Stretch.None,
                    TileMode = TileMode.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };

                TransitionVisual.Fill = visualBrush;
                TransitionOverlay.Visibility = Visibility.Visible;

                // 2. Cambiar recursos
                LoadThemeResources(themeName);

                // 3. Animación de barrido
                var maskBrush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
                var stopTransparent = new GradientStop(Colors.Transparent, -0.2);
                var stopBlack = new GradientStop(Colors.Black, -0.1);
                maskBrush.GradientStops.Add(stopTransparent); maskBrush.GradientStops.Add(stopBlack);
                TransitionVisual.OpacityMask = maskBrush;

                var duration = TimeSpan.FromMilliseconds(800);
                var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
                stopTransparent.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(1.0, duration) { EasingFunction = ease });
                stopBlack.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(1.1, duration) { EasingFunction = ease });

                TransitionGlassWave.Opacity = 0.8;
                var waveAnim = new DoubleAnimation(-TransitionGlassWave.Width, 1100, duration) { EasingFunction = ease };
                GlassWaveTranslate.BeginAnimation(TranslateTransform.XProperty, waveAnim);

                waveAnim.Completed += (s, args) => { TransitionOverlay.Visibility = Visibility.Collapsed; TransitionVisual.Fill = null; TransitionVisual.OpacityMask = null; GlassWaveTranslate.BeginAnimation(TranslateTransform.XProperty, null); GlassWaveTranslate.X = -100; };
            }
            catch (Exception ex) { MessageBox.Show($"Error applying theme: {ex.Message}"); }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) { if (e.Category == UserPreferenceCategory.General) Dispatcher.Invoke(() => ApplyTheme("System")); }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed && !e.Handled) DragMove(); }
        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e)
        {
            var duration = TimeSpan.FromMilliseconds(500); var ease = new QuinticEase { EasingMode = EasingMode.EaseInOut };
            var scaleX = new DoubleAnimation(0.1, duration) { EasingFunction = ease }; var scaleY = new DoubleAnimation(0.1, duration) { EasingFunction = ease }; var fade = new DoubleAnimation(0, duration) { EasingFunction = ease };
            var sb = new Storyboard();
            Storyboard.SetTargetName(scaleX, "WindowScaleTransform"); Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            Storyboard.SetTargetName(scaleY, "WindowScaleTransform"); Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            Storyboard.SetTarget(fade, MainBorder); Storyboard.SetTargetProperty(fade, new PropertyPath(Border.OpacityProperty));
            sb.Children.Add(scaleX); sb.Children.Add(scaleY); sb.Children.Add(fade);
            sb.Completed += (s, args) => { WindowState = WindowState.Minimized; sb.Stop(this); WindowScaleTransform.ScaleX = 1.0; WindowScaleTransform.ScaleY = 1.0; MainBorder.Opacity = 1.0; };
            sb.Begin(this);
        }
        private void CloseButton_Click(object sender, MouseButtonEventArgs e) => Close();
        #endregion

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
    }
}