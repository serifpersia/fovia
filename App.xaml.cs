using Gma.System.MouseKeyHook;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace fovia
{
    public partial class App : System.Windows.Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static float MIN_ZOOM = 1.0f;
        public static float MAX_ZOOM = 8.0f;
        public static float ZOOM_STEP = 0.25f;
        public static float ZOOM_SMOOTHNESS = 15.0f;
        public static float PAN_SMOOTHNESS = 15.0f;
        public static float PUSH_MARGIN = 0.10f;

        private IKeyboardMouseEvents? _hook;
        private Stopwatch _stopwatch = new Stopwatch();

        private float _currentZoom = 1.0f;
        private float _targetZoom = 1.0f;

        private float _viewX = 0f;
        private float _viewY = 0f;

        private void PreJitMethods()
        {
            var method = typeof(App).GetMethod("UpdateLoop", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null) RuntimeHelpers.PrepareMethod(method.MethodHandle);
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 60 });
            ComponentDispatcher.ThreadIdle += (s, args) => { };

            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            PreJitMethods();

            var window = new MainWindow();
            window.Show();

            var handle = new WindowInteropHelper(window).Handle;
            SetForegroundWindow(handle);

            await System.Threading.Tasks.Task.Delay(500);

            if (MagApi.MagInitialize())
            {
                _hook = Hook.GlobalEvents();
                _hook.MouseWheelExt += OnMouseWheelExt;
                _stopwatch.Start();
                CompositionTarget.Rendering += UpdateLoop;
            }
        }

        private void OnMouseWheelExt(object? sender, MouseEventExtArgs e)
        {
            if (Control.ModifierKeys.HasFlag(Keys.Control) && Control.ModifierKeys.HasFlag(Keys.Alt))
            {
                float step = e.Delta > 0 ? ZOOM_STEP : -ZOOM_STEP;
                _targetZoom = Math.Clamp(_targetZoom + step, MIN_ZOOM, MAX_ZOOM);
                e.Handled = true;
            }
        }

        private void UpdateLoop(object? sender, EventArgs e)
        {
            float deltaTime = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            float previousZoom = _currentZoom;
            float zoomT = 1.0f - (float)Math.Exp(-ZOOM_SMOOTHNESS * deltaTime);
            float panT = 1.0f - (float)Math.Exp(-PAN_SMOOTHNESS * deltaTime);
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
                float marginX = currentViewW * PUSH_MARGIN;
                float marginY = currentViewH * PUSH_MARGIN;
                float targetX = _viewX;
                float targetY = _viewY;

                if (mouse.X < targetX + marginX) targetX = mouse.X - marginX;
                if (mouse.X > targetX + currentViewW - marginX) targetX = mouse.X - currentViewW + marginX;
                if (mouse.Y < targetY + marginY) targetY = mouse.Y - marginY;
                if (mouse.Y > targetY + currentViewH - marginY) targetY = mouse.Y - currentViewH + marginY;

                _viewX += (targetX - _viewX) * panT;
                _viewY += (targetY - _viewY) * panT;
                _viewX = Math.Max(0, Math.Min(_viewX, screenW - currentViewW));
                _viewY = Math.Max(0, Math.Min(_viewY, screenH - currentViewH));

                try { MagApi.MagSetFullscreenTransform(_currentZoom, (int)_viewX, (int)_viewY); } catch { }
            }
            else
            {
                _currentZoom = 1.0f;
                _viewX = 0; _viewY = 0;
                MagApi.MagSetFullscreenTransform(1.0f, 0, 0);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hook?.Dispose();
            CompositionTarget.Rendering -= UpdateLoop;
            MagApi.MagSetFullscreenTransform(1.0f, 0, 0);
            MagApi.MagUninitialize();
            base.OnExit(e);
        }
    }
}