using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;

namespace HassNotifyReceiver;

class Program
{
    private static MqttService? _mqttService;

    static async Task Main(string[] args)
    {
        string? mqttIp = null;
        int mqttPort = 1883;
        string? mqttUser = null;
        string? mqttPw = null;
        string mqttTopic = "desktop/notifications";
        bool showHelp = false;

        // 1. Env Vars
        mqttIp = Environment.GetEnvironmentVariable("MQTT_IP") ?? mqttIp;
        if (int.TryParse(Environment.GetEnvironmentVariable("MQTT_PORT"), out int parsedPort)) mqttPort = parsedPort;
        mqttUser = Environment.GetEnvironmentVariable("MQTT_USER") ?? mqttUser;
        mqttPw = Environment.GetEnvironmentVariable("MQTT_PW") ?? mqttPw;
        mqttTopic = Environment.GetEnvironmentVariable("MQTT_TOPIC") ?? mqttTopic;

        // 2. CLI Args
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--mqtt-ip":
                    mqttIp = (i + 1 < args.Length) ? args[++i] : mqttIp;
                    break;
                case "--mqtt-port":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int p)) mqttPort = p;
                    break;
                case "--mqtt-user":
                    mqttUser = (i + 1 < args.Length) ? args[++i] : mqttUser;
                    break;
                case "--mqtt-pw":
                    mqttPw = (i + 1 < args.Length) ? args[++i] : mqttPw;
                    break;
                case "--mqtt-topic":
                    mqttTopic = (i + 1 < args.Length) ? args[++i] : mqttTopic;
                    break;
                case "--help":
                case "-h":
                case "/?":
                    showHelp = true;
                    break;
            }
        }

        if (showHelp || string.IsNullOrWhiteSpace(mqttIp))
        {
            Console.WriteLine("HassNotifyReceiver - Headless MQTT Windows Toast Receiver");
            Console.WriteLine("Usage: HassNotifyReceiver.exe [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --mqtt-ip <ip>      [Required] MQTT Broker IP or Hostname");
            Console.WriteLine("  --mqtt-port <port>  [Optional] MQTT Broker Port (default: 1883)");
            Console.WriteLine("  --mqtt-user <user>  [Optional] MQTT Username");
            Console.WriteLine("  --mqtt-pw <pw>      [Optional] MQTT Password");
            Console.WriteLine("  --mqtt-topic <topic>[Optional] MQTT Topic (default: desktop/notifications)");
            Console.WriteLine("");
            Console.WriteLine("These settings can also be provided via Environment Variables:");
            Console.WriteLine("  MQTT_IP, MQTT_PORT, MQTT_USER, MQTT_PW, MQTT_TOPIC");
            return;
        }

        Console.WriteLine("Starting HassNotifyReceiver...");

        // Setup notification backgrounds action processor
        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            var tArgs = ToastArguments.Parse(toastArgs.Argument);

            if (tArgs.TryGetValue("action", out string actionStr))
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
                    if (_mqttService != null)
                    {
                        _ = _mqttService.PublishActionAsync(actionStr, toastArgs.UserInput, mqttTopic);
                    }
                }
            }
            else if (tArgs.TryGetValue("uri", out string urlStr))
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

        _mqttService = new MqttService();
        await _mqttService.StartAsync(mqttIp, mqttPort, mqttUser, mqttPw, mqttTopic);

        Console.WriteLine($"Connected to {mqttIp}:{mqttPort} listening on '{mqttTopic}'...");
        Console.WriteLine("Press Ctrl+C to exit.");

        // Keep running until closed
        await Task.Delay(-1);
    }
}
