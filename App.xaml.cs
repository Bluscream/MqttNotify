using System.Windows;
using H.NotifyIcon;

namespace HassNotifyReceiver;

public partial class App : Application
{
    private TaskbarIcon? _notifyIcon;
    private MainWindow? _settingsWindow;

    public static App? CurrentApp => Current as App;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize the notify icon (created in XAML)
        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        
        // Use a generic system icon for now
        _notifyIcon.Icon = System.Drawing.SystemIcons.Information;

        // Initialize the settings window but keep it hidden
        _settingsWindow = new MainWindow();

        // Start MQTT Client
        ReloadMqttConnection();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }

    private MqttService _mqttService = new();

    public async void ReloadMqttConnection()
    {
        await _mqttService.StopAsync();
        await _mqttService.StartAsync();
    }

    private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new MainWindow();
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void MenuItemExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
