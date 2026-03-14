using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Keys = System.Windows.Forms.Keys;

namespace fovia
{
    public partial class MainWindow : Window
    {
        private bool _isLoaded = false;
        private int _assignMode = 0;

        public MainWindow()
        {
            InitializeComponent();

            TxtZoomIn.Text = App.Settings.ZoomInText;
            TxtZoomOut.Text = App.Settings.ZoomOutText;

            CmbTrackingMode.SelectedIndex = App.Settings.TrackingMode;
            SldMaxZoom.Value = App.Settings.MaxZoom;
            SldZoomStep.Value = App.Settings.ZoomStep;
            SldZoomSmoothness.Value = App.Settings.ZoomSmoothness;
            SldPanSmoothness.Value = App.Settings.PanSmoothness;
            SldPushMargin.Value = App.Settings.PushMargin;

            _isLoaded = true;
            UpdatePushMarginState();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle);
        }

        private void UpdatePushMarginState()
        {
            if (CmbTrackingMode.SelectedIndex == 0)
            {
                SldPushMargin.IsEnabled = true;
                SldPushMargin.Opacity = 1.0;
            }
            else
            {
                SldPushMargin.IsEnabled = false;
                SldPushMargin.Opacity = 0.4;
            }
        }

        private void OnSettingChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || _assignMode != 0) return;

            App.Settings.TrackingMode = CmbTrackingMode.SelectedIndex;
            App.Settings.MaxZoom = (float)SldMaxZoom.Value;
            App.Settings.ZoomStep = (float)SldZoomStep.Value;
            App.Settings.ZoomSmoothness = (float)SldZoomSmoothness.Value;
            App.Settings.PanSmoothness = (float)SldPanSmoothness.Value;
            App.Settings.PushMargin = (float)SldPushMargin.Value;

            UpdatePushMarginState();
            App.SaveSettings();
        }

        private SolidColorBrush GetBrush(string hexColor)
        {
            return (SolidColorBrush)(new BrushConverter().ConvertFromString(hexColor)!);
        }

        private void BorderZoomIn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartAssigning(1);
        }

        private void BorderZoomOut_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartAssigning(2);
        }

        private void StartAssigning(int mode)
        {
            _assignMode = mode;

            BorderZoomIn.BorderBrush = GetBrush(mode == 1 ? "#4FA8FF" : "#3A3A3E");
            BorderZoomOut.BorderBrush = GetBrush(mode == 2 ? "#4FA8FF" : "#3A3A3E");

            TxtHotkeyStatus.Text = "Press a key combination or scroll...";
            TxtHotkeyStatus.Foreground = GetBrush("#FFB900");

            if (mode == 1) TxtZoomIn.Text = "Listening...";
            if (mode == 2) TxtZoomOut.Text = "Listening...";

            if (mode == 1) BorderZoomIn.Focus();
            else BorderZoomOut.Focus();
        }

        private void CancelAssigning()
        {
            _assignMode = 0;
            BorderZoomIn.BorderBrush = GetBrush("#3A3A3E");
            BorderZoomOut.BorderBrush = GetBrush("#3A3A3E");

            TxtZoomIn.Text = App.Settings.ZoomInText;
            TxtZoomOut.Text = App.Settings.ZoomOutText;

            TxtHotkeyStatus.Text = "Assignment cancelled.";
            TxtHotkeyStatus.Foreground = GetBrush("#888888");
        }

        private void Hotkey_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_assignMode == 0) return;
            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                CancelAssigning();
                return;
            }

            int mods = (int)GetWinFormsModifiers();

            if (IsModifierKey(e.Key))
            {
                UpdateTempText(mods);
                return;
            }

            int key = KeyInterop.VirtualKeyFromKey(e.Key);
            SaveAssignment(mods, key, 0);
        }

        private void Hotkey_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_assignMode == 0) return;
            e.Handled = true;

            int mods = (int)GetWinFormsModifiers();
            int scroll = e.Delta > 0 ? 1 : -1;

            SaveAssignment(mods, 0, scroll);
        }

        private void SaveAssignment(int mods, int key, int scroll)
        {
            string text = GenerateHotkeyText(mods, key, scroll);

            if (_assignMode == 1)
            {
                App.Settings.ZoomInModifiers = mods;
                App.Settings.ZoomInKey = key;
                App.Settings.ZoomInScroll = scroll;
                App.Settings.ZoomInText = text;
                TxtZoomIn.Text = text;
            }
            else if (_assignMode == 2)
            {
                App.Settings.ZoomOutModifiers = mods;
                App.Settings.ZoomOutKey = key;
                App.Settings.ZoomOutScroll = scroll;
                App.Settings.ZoomOutText = text;
                TxtZoomOut.Text = text;
            }

            App.SaveSettings();

            _assignMode = 0;
            BorderZoomIn.BorderBrush = GetBrush("#3A3A3E");
            BorderZoomOut.BorderBrush = GetBrush("#3A3A3E");

            TxtHotkeyStatus.Text = "Hotkey saved successfully.";
            TxtHotkeyStatus.Foreground = GetBrush("#4CAF50");
        }

        private void UpdateTempText(int mods)
        {
            var parts = new List<string>();
            if ((mods & (int)Keys.Control) != 0) parts.Add("Ctrl");
            if ((mods & (int)Keys.Alt) != 0) parts.Add("Alt");
            if ((mods & (int)Keys.Shift) != 0) parts.Add("Shift");
            parts.Add("...");

            string text = string.Join(" + ", parts);
            if (_assignMode == 1) TxtZoomIn.Text = text;
            if (_assignMode == 2) TxtZoomOut.Text = text;
        }

        private string GenerateHotkeyText(int mods, int key, int scroll)
        {
            var parts = new List<string>();
            if ((mods & (int)Keys.Control) != 0) parts.Add("Ctrl");
            if ((mods & (int)Keys.Alt) != 0) parts.Add("Alt");
            if ((mods & (int)Keys.Shift) != 0) parts.Add("Shift");

            if (scroll == 1) parts.Add("Scroll Up");
            else if (scroll == -1) parts.Add("Scroll Down");
            else if (key != 0) parts.Add(((Keys)key).ToString());

            return parts.Count > 0 ? string.Join(" + ", parts) : "None";
        }

        private Keys GetWinFormsModifiers()
        {
            Keys modifiers = Keys.None;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= Keys.Control;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= Keys.Alt;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= Keys.Shift;
            return modifiers;
        }

        private bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift;
        }
    }
}