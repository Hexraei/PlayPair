using System;
using System.ComponentModel;
using System.Windows;

namespace PlayPair.Client.AppShell.Views;

public partial class StatusOverlayWindow : Window
{
    private bool _allowClose;

    public StatusOverlayWindow()
    {
        InitializeComponent();
    }

    public event EventHandler? RequestCloseToTray;

    public void AllowClose()
    {
        _allowClose = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_allowClose)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        Hide();
        RequestCloseToTray?.Invoke(this, EventArgs.Empty);
    }

    private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close(); // This will trigger OnClosing which hides the window to tray safely
    }
}
