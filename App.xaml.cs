using Gma.System.MouseKeyHook;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace fovia
{
    public class AppSettings
    {
        public float MaxZoom { get; set; } = 8.0f;
        public float ZoomStep { get; set; } = 0.25f;
        public float ZoomSmoothness { get; set; } = 15.0f;
        public float PanSmoothness { get; set; } = 15.0f;
        public float PushMargin { get; set; } = 0.10f;
        public int TrackingMode { get; set; } = 0;
        public int ZoomInModifiers { get; set; } = (int)(Keys.Control | Keys.Alt);
        public int ZoomInKey { get; set; } = 0;
        public int ZoomInScroll { get; set; } = 1;
        public string ZoomInText { get; set; } = "Ctrl + Alt + Scroll Up";

        public int ZoomOutModifiers { get; set; } = (int)(Keys.Control | Keys.Alt);
        public int ZoomOutKey { get; set; } = 0;
        public int ZoomOutScroll { get; set; } = -1;
        public string ZoomOutText { get; set; } = "Ctrl + Alt + Scroll Down";
    }

    public partial class App : System.Windows.Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static AppSettings Settings = new AppSettings();
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private IKeyboardMouseEvents? _hook;
        private Stopwatch _stopwatch = new Stopwatch();
        private float _currentZoom = 1.0f;
        private float _targetZoom = 1.0f;
        private float _viewX = 0f;
        private float _viewY = 0f;

        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
        }

        public static void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        private void PreJitMethods()
        {
            var method = typeof(App).GetMethod("UpdateLoop", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null) RuntimeHelpers.PrepareMethod(method.MethodHandle);
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            LoadSettings();

            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 60 });
            ComponentDispatcher.ThreadIdle += (s, args) => { };

            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            PreJitMethods();

            if (MagApi.MagInitialize())
            {
                _hook = Hook.GlobalEvents();
                _hook.MouseWheelExt += OnMouseWheelExt;
                _hook.KeyDown += OnKeyDown;
                _stopwatch.Start();
                CompositionTarget.Rendering += UpdateLoop;
            }

            var window = new MainWindow();

            // -- automate minimize task to fix laggy performance on startup --
            window.Opacity = 0;
            window.Show();
            await System.Threading.Tasks.Task.Delay(50);
            window.WindowState = WindowState.Minimized;
            await System.Threading.Tasks.Task.Delay(50);
            window.WindowState = WindowState.Normal;
            window.Opacity = 1;

            var handle = new WindowInteropHelper(window).Handle;
            SetForegroundWindow(handle);
        }

        private void OnKeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (Settings.ZoomInKey != 0 && Control.ModifierKeys == (Keys)Settings.ZoomInModifiers && e.KeyCode == (Keys)Settings.ZoomInKey)
            {
                _targetZoom = Math.Clamp(_targetZoom + Settings.ZoomStep, 1.0f, Settings.MaxZoom);
                e.Handled = true;
            }
            else if (Settings.ZoomOutKey != 0 && Control.ModifierKeys == (Keys)Settings.ZoomOutModifiers && e.KeyCode == (Keys)Settings.ZoomOutKey)
            {
                _targetZoom = Math.Clamp(_targetZoom - Settings.ZoomStep, 1.0f, Settings.MaxZoom);
                e.Handled = true;
            }
        }

        private void OnMouseWheelExt(object? sender, MouseEventExtArgs e)
        {
            int scrollDir = e.Delta > 0 ? 1 : -1;

            if (Settings.ZoomInScroll == scrollDir && Control.ModifierKeys == (Keys)Settings.ZoomInModifiers)
            {
                _targetZoom = Math.Clamp(_targetZoom + Settings.ZoomStep, 1.0f, Settings.MaxZoom);
                e.Handled = true;
            }
            else if (Settings.ZoomOutScroll == scrollDir && Control.ModifierKeys == (Keys)Settings.ZoomOutModifiers)
            {
                _targetZoom = Math.Clamp(_targetZoom - Settings.ZoomStep, 1.0f, Settings.MaxZoom);
                e.Handled = true;
            }
        }

        private void UpdateLoop(object? sender, EventArgs e)
        {
            float deltaTime = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            float previousZoom = _currentZoom;
            float zoomT = 1.0f - (float)Math.Exp(-Settings.ZoomSmoothness * deltaTime);
            float panT = 1.0f - (float)Math.Exp(-Settings.PanSmoothness * deltaTime);
            _currentZoom += (_targetZoom - _currentZoom) * zoomT;

            if (_currentZoom > 1.001f)
            {
                var mouse = System.Windows.Forms.Cursor.Position;
                var screen = Screen.PrimaryScreen;
                if (screen == null) return;

                float screenW = screen.Bounds.Width;
                float screenH = screen.Bounds.Height;

                if (Math.Abs(_currentZoom - previousZoom) > 0.0001f)
                {
                    float oldViewW = screenW / previousZoom;
                    float oldViewH = screenH / previousZoom;
                    float newViewW = screenW / _currentZoom;
                    float newViewH = screenH / _currentZoom;
                    float relX = (mouse.X - _viewX) / oldViewW;
                    float relY = (mouse.Y - _viewY) / oldViewH;
                    _viewX = mouse.X - (relX * newViewW);
                    _viewY = mouse.Y - (relY * newViewH);
                }

                float currentViewW = screenW / _currentZoom;
                float currentViewH = screenH / _currentZoom;
                float targetX = _viewX;
                float targetY = _viewY;

                if (Settings.TrackingMode == 0)
                {
                    float marginX = currentViewW * Settings.PushMargin;
                    float marginY = currentViewH * Settings.PushMargin;
                    if (mouse.X < targetX + marginX) targetX = mouse.X - marginX;
                    if (mouse.X > targetX + currentViewW - marginX) targetX = mouse.X - currentViewW + marginX;
                    if (mouse.Y < targetY + marginY) targetY = mouse.Y - marginY;
                    if (mouse.Y > targetY + currentViewH - marginY) targetY = mouse.Y - currentViewH + marginY;
                }
                else if (Settings.TrackingMode == 1)
                {
                    targetX = mouse.X - (currentViewW / 2.0f);
                    targetY = mouse.Y - (currentViewH / 2.0f);
                }
                else if (Settings.TrackingMode == 2)
                {
                    float ratioX = mouse.X / screenW;
                    float ratioY = mouse.Y / screenH;
                    targetX = ratioX * (screenW - currentViewW);
                    targetY = ratioY * (screenH - currentViewH);
                }

                _viewX += (targetX - _viewX) * panT;
                _viewY += (targetY - _viewY) * panT;
                _viewX = Math.Max(0, Math.Min(_viewX, screenW - currentViewW));
                _viewY = Math.Max(0, Math.Min(_viewY, screenH - currentViewH));

                try
                {
                    MagApi.MagSetFullscreenTransform(_currentZoom, (int)_viewX, (int)_viewY);

                    var sourceRect = new MagApi.RECT
                    {
                        left = (int)_viewX,
                        top = (int)_viewY,
                        right = (int)(_viewX + currentViewW),
                        bottom = (int)(_viewY + currentViewH)
                    };
                    var destRect = new MagApi.RECT
                    {
                        left = 0,
                        top = 0,
                        right = (int)screenW,
                        bottom = (int)screenH
                    };
                    MagApi.MagSetInputTransform(true, ref sourceRect, ref destRect);
                }
                catch { }
            }
            else
            {
                _currentZoom = 1.0f;
                _viewX = 0; _viewY = 0;
                MagApi.MagSetFullscreenTransform(1.0f, 0, 0);

                var emptyRect = new MagApi.RECT();
                MagApi.MagSetInputTransform(false, ref emptyRect, ref emptyRect);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_hook != null)
            {
                _hook.KeyDown -= OnKeyDown;
                _hook.MouseWheelExt -= OnMouseWheelExt;
                _hook.Dispose();
            }

            CompositionTarget.Rendering -= UpdateLoop;
            MagApi.MagSetFullscreenTransform(1.0f, 0, 0);

            var emptyRect = new MagApi.RECT();
            MagApi.MagSetInputTransform(false, ref emptyRect, ref emptyRect);

            MagApi.MagUninitialize();
            base.OnExit(e);
        }
    }
}