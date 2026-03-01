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

        // Listen to notification activation (button clicks, textbox inputs)
        Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            var args = Microsoft.Toolkit.Uwp.Notifications.ToastArguments.Parse(toastArgs.Argument);

            // Handle normal URL actions
            if (args.TryGetValue("action", out string actionStr))
            {
                if (Uri.TryCreate(actionStr, UriKind.Absolute, out Uri? uriResult))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = uriResult.ToString(),
                        UseShellExecute = true
                    });
                }
                else
                {
                    // If it's not a URL, it's a designated HA Action string. Report it via MQTT.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _ = _mqttService.PublishActionAsync(actionStr, toastArgs.UserInput);
                    });
                }
            }
            else if (args.TryGetValue("uri", out string urlStr))
            {
                if (Uri.TryCreate(urlStr, UriKind.Absolute, out Uri? uriResult))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = uriResult.ToString(),
                        UseShellExecute = true
                    });
                }
            }
        };

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
