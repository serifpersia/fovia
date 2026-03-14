using System;
using System.Windows;
using System.Windows.Interop;

namespace fovia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SldMaxZoom.Value = App.MAX_ZOOM;
            SldZoomStep.Value = App.ZOOM_STEP;
            SldZoomSmoothness.Value = App.ZOOM_SMOOTHNESS;
            SldPanSmoothness.Value = App.PAN_SMOOTHNESS;
            SldPushMargin.Value = App.PUSH_MARGIN;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle);
        }

        private void OnSettingChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            App.MAX_ZOOM = (float)SldMaxZoom.Value;
            App.ZOOM_STEP = (float)SldZoomStep.Value;
            App.ZOOM_SMOOTHNESS = (float)SldZoomSmoothness.Value;
            App.PAN_SMOOTHNESS = (float)SldPanSmoothness.Value;
            App.PUSH_MARGIN = (float)SldPushMargin.Value;
        }
    }
}