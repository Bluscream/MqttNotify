using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;

namespace HassNotifyReceiver;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = SettingsManager.Load();
        TxtBroker.Text = settings.MqttBrokerAddress;
        TxtPort.Text = settings.MqttPort.ToString();
        TxtUser.Text = settings.MqttUsername;
        TxtPass.Password = settings.MqttPassword;
        TxtTopic.Text = settings.MqttTopic;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtPort.Text, out int port))
        {
            MessageBox.Show("Port must be a valid number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var settings = new AppSettings
        {
            MqttBrokerAddress = TxtBroker.Text,
            MqttPort = port,
            MqttUsername = TxtUser.Text,
            MqttPassword = TxtPass.Password,
            MqttTopic = TxtTopic.Text
        };
        SettingsManager.Save(settings);
        
        // Hide window instead of closing
        this.Hide();

        // Trigger connection update in App
        App.CurrentApp?.ReloadMqttConnection();
    }

    private void BtnTest_Click(object sender, RoutedEventArgs e)
    {
        new ToastContentBuilder()
            .AddText("Test Notification")
            .AddText("This is a test notification from HassNotifyReceiver!")
            .Show();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Cancel the close, hide instead
        e.Cancel = true;
        this.Hide();
    }
}