using System;
using System.IO;
using System.Text.Json;

namespace HassNotifyReceiver
{
    public class AppSettings
    {
        public string MqttBrokerAddress { get; set; } = "192.168.1.100";
        public int MqttPort { get; set; } = 1883;
        public string MqttUsername { get; set; } = "";
        public string MqttPassword { get; set; } = "";
        public string MqttTopic { get; set; } = "desktop/notifications";
    }

    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HassNotifyReceiver", "settings.json");

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            // Override with Environment Variables if they exist
            var envMqttIp = Environment.GetEnvironmentVariable("MQTT_IP");
            if (!string.IsNullOrWhiteSpace(envMqttIp)) settings.MqttBrokerAddress = envMqttIp;

            var envMqttPortString = Environment.GetEnvironmentVariable("MQTT_PORT");
            if (!string.IsNullOrWhiteSpace(envMqttPortString) && int.TryParse(envMqttPortString, out int envMqttPort))
            {
                settings.MqttPort = envMqttPort;
            }

            var envMqttUser = Environment.GetEnvironmentVariable("MQTT_USER");
            if (!string.IsNullOrWhiteSpace(envMqttUser)) settings.MqttUsername = envMqttUser;

            var envMqttPw = Environment.GetEnvironmentVariable("MQTT_PW");
            if (!string.IsNullOrWhiteSpace(envMqttPw)) settings.MqttPassword = envMqttPw;

            var envHassIp = Environment.GetEnvironmentVariable("HASS_IP");
            var envHassPort = Environment.GetEnvironmentVariable("HASS_PORT");
            var envHassToken = Environment.GetEnvironmentVariable("HASS_TOKEN");
            
            // Note: HASS_IP, HASS_PORT, and HASS_TOKEN are parsed here for future WebSocket/REST use
            // but the current implementation primarily uses MQTT as requested.
            // We can store them in the settings model if we want, but for now they are processed here.

            return settings;
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
