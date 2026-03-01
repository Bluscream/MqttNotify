using Microsoft.Toolkit.Uwp.Notifications;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HassNotifyReceiver;

public class ToastPayload
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? ImageUrl { get; set; }
}

public class MqttService
{
    private IManagedMqttClient? _mqttClient;

    public async Task StartAsync()
    {
        var settings = SettingsManager.Load();
        if (string.IsNullOrWhiteSpace(settings.MqttBrokerAddress)) return;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId(Guid.NewGuid().ToString())
            .WithTcpServer(settings.MqttBrokerAddress, settings.MqttPort);

        if (!string.IsNullOrEmpty(settings.MqttUsername))
        {
            clientOptionsBuilder.WithCredentials(settings.MqttUsername, settings.MqttPassword);
        }

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(clientOptionsBuilder.Build())
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                var payloadString = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                System.Diagnostics.Debug.WriteLine($"Received topic: {e.ApplicationMessage.Topic}, Payload: {payloadString}");

                ToastPayload? payload = null;
                try
                {
                    payload = JsonSerializer.Deserialize<ToastPayload>(payloadString);
                }
                catch
                {
                    // If it's not JSON, just treat the whole payload as the message
                    payload = new ToastPayload { Message = payloadString, Title = "Notification" };
                }

                if (payload != null)
                {
                    var builder = new ToastContentBuilder()
                        .AddText(payload.Title ?? "Home Assistant")
                        .AddText(payload.Message ?? "");

                    if (!string.IsNullOrWhiteSpace(payload.ImageUrl))
                    {
                        if (Uri.TryCreate(payload.ImageUrl, UriKind.Absolute, out Uri? imageUrl))
                        {
                            builder.AddHeroImage(imageUrl);
                        }
                    }

                    builder.Show();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling MQTT message: {ex.Message}");
            }
            
            return Task.CompletedTask;
        };

        await _mqttClient.StartAsync(options);

        if (!string.IsNullOrWhiteSpace(settings.MqttTopic))
        {
            var filter = new MqttTopicFilterBuilder()
                .WithTopic(settings.MqttTopic)
                .Build();
            await _mqttClient.SubscribeAsync(new[] { filter });
        }
    }

    public async Task StopAsync()
    {
        if (_mqttClient != null)
        {
            await _mqttClient.StopAsync();
            _mqttClient.Dispose();
            _mqttClient = null;
        }
    }
}
