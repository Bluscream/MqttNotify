using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace MqttNotify.Test;

class Program
{
    static async Task Main(string[] args)
    {
        string? mqttIp = null;
        int mqttPort = 1883;
        string? mqttUser = null;
        string? mqttPw = null;
        string mqttTopic = "desktop/notifications";

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
            }
        }

        if (string.IsNullOrWhiteSpace(mqttIp))
        {
            Console.WriteLine("Please provide --mqtt-ip or set the MQTT_IP environment variable.");
            return;
        }

        Console.WriteLine($"Connecting to {mqttIp}:{mqttPort}...");

        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId(Guid.NewGuid().ToString())
            .WithTcpServer(mqttIp, mqttPort);

        if (!string.IsNullOrEmpty(mqttUser))
        {
            clientOptionsBuilder.WithCredentials(mqttUser, mqttPw);
        }

        await mqttClient.ConnectAsync(clientOptionsBuilder.Build(), CancellationToken.None);
        Console.WriteLine("Connected! Sending test notification...");

        var payload = new
        {
            title = "Test Notification",
            message = "This is a test from MqttNotify.Test!",
            data = new
            {
                clickAction = "https://github.com",
                duration = 5,
                actions = new[]
                {
                    new { action = "test_action_1", title = "Test Button", uri = "" },
                    new { action = "", title = "Open Google", uri = "https://google.com" }
                },
                inputs = new[]
                {
                    new { id = "reply", title = "Quick Reply", text = "I am a default reply" }
                }
            }
        };

        string json = JsonSerializer.Serialize(payload);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(mqttTopic)
            .WithPayload(json)
            .WithRetainFlag(false)
            .Build();

        await mqttClient.PublishAsync(message, CancellationToken.None);

        Console.WriteLine($"Published to '{mqttTopic}'. Disconnecting...");
        await mqttClient.DisconnectAsync();
    }
}
