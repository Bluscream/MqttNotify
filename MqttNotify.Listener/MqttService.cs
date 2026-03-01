using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Windows.UI.Notifications;

namespace MqttNotify.Listener;

public class NotificationAction
{
    public string Action { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
}

public class NotificationInput
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class NotificationData
{
    public const string NoAction = "noAction";
    public const string ImportanceHigh = "high";

    public int Duration { get; set; } = 0;
    public string Image { get; set; } = string.Empty;
    public string ClickAction { get; set; } = NoAction;
    public string Tag { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; } = string.Empty;
    public bool Sticky { get; set; }
    public string Importance { get; set; } = string.Empty;

    public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();
    public List<NotificationInput> Inputs { get; set; } = new List<NotificationInput>();
}

public class ToastPayload
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public NotificationData? Data { get; set; }
}

public class MqttService
{
    private IManagedMqttClient? _mqttClient;

    public async Task StartAsync(string mqttIp, int mqttPort, string? mqttUser, string? mqttPw, string mqttTopic)
    {
        if (string.IsNullOrWhiteSpace(mqttIp)) return;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId(Guid.NewGuid().ToString())
            .WithTcpServer(mqttIp, mqttPort);

        if (!string.IsNullOrEmpty(mqttUser))
        {
            clientOptionsBuilder.WithCredentials(mqttUser, mqttPw);
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
                    payload = JsonSerializer.Deserialize<ToastPayload>(payloadString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    // If it's not JSON, just treat the whole payload as the message
                    payload = new ToastPayload { Message = payloadString, Title = "Notification", Data = new NotificationData() };
                }

                if (payload != null)
                {
                    if (payload.Data == null) payload.Data = new NotificationData();

                    // Check for "clear_notification"
                    if (payload.Message == "clear_notification")
                    {
                        if (!string.IsNullOrWhiteSpace(payload.Data.Tag) && !string.IsNullOrWhiteSpace(payload.Data.Group))
                            ToastNotificationManagerCompat.History.Remove(payload.Data.Tag, payload.Data.Group);
                        else if (!string.IsNullOrWhiteSpace(payload.Data.Tag))
                            ToastNotificationManagerCompat.History.Remove(payload.Data.Tag);
                        else
                            ToastNotificationManagerCompat.History.Clear();
                        
                        return Task.CompletedTask;
                    }

                    var builder = new ToastContentBuilder()
                        .AddText(payload.Title ?? "Home Assistant")
                        .AddText(payload.Message ?? "");

                    // Click Action
                    if (payload.Data.ClickAction != NotificationData.NoAction && !string.IsNullOrWhiteSpace(payload.Data.ClickAction))
                    {
                        builder.AddArgument("action", payload.Data.ClickAction);
                    }

                    // Hero Image
                    if (!string.IsNullOrWhiteSpace(payload.Data.Image))
                    {
                        if (Uri.TryCreate(payload.Data.Image, UriKind.Absolute, out Uri? imageUrl))
                        {
                            builder.AddHeroImage(imageUrl);
                        }
                    }

                    // App Logo Override
                    if (!string.IsNullOrWhiteSpace(payload.Data.IconUrl))
                    {
                        if (Uri.TryCreate(payload.Data.IconUrl, UriKind.Absolute, out Uri? iconUrl))
                        {
                            builder.AddAppLogoOverride(iconUrl, ToastGenericAppLogoCrop.Default);
                        }
                    }

                    // Buttons/Actions
                    if (payload.Data.Actions != null && payload.Data.Actions.Count > 0)
                    {
                        foreach (var action in payload.Data.Actions)
                        {
                            if (string.IsNullOrEmpty(action.Action)) continue;
                            
                            var button = new ToastButton()
                                .SetContent(action.Title)
                                .AddArgument("action", action.Action);

                            if (!string.IsNullOrWhiteSpace(action.Uri))
                            {
                                // UWP Notifications handle URLs as Protocol activations natively if you use SetProtocolActivation
                                // But if we want it to report back to our app to do logic, we route it as a background argument
                                button.AddArgument("uri", action.Uri);
                            }
                            
                            builder.AddButton(button);
                        }
                    }

                    // Text Inputs
                    if (payload.Data.Inputs != null && payload.Data.Inputs.Count > 0)
                    {
                        foreach (var input in payload.Data.Inputs)
                        {
                            if (string.IsNullOrEmpty(input.Id)) continue;
                            builder.AddInputTextBox(input.Id, input.Text, input.Title);
                        }
                    }

                    // Tag and Group
                    if (!string.IsNullOrWhiteSpace(payload.Data.Tag))
                    {
                        // The builder doesn't have a direct method for Tag/Group before showing
                        // so we pass them via the Show() extension if needed, but ToastContentBuilder handles it strangely.
                        // Actually, builder.Show() accepts a ToastNotification which can have a Tag and Group set.
                        // We will build the notification manually below if we need to set Tag/Group.
                    }

                    // Scenarios (Urgent / Sticky)
                    if (payload.Data.Sticky)
                    {
                        builder.SetToastScenario(ToastScenario.Reminder);
                    }
                    else if (payload.Data.Importance == NotificationData.ImportanceHigh)
                    {
                        builder.SetToastScenario(ToastScenario.Alarm);
                    }

                    var toast = builder.GetToastContent();
                    var notification = new ToastNotification(toast.GetXml());

                    if (!string.IsNullOrWhiteSpace(payload.Data.Tag)) notification.Tag = payload.Data.Tag;
                    if (!string.IsNullOrWhiteSpace(payload.Data.Group)) notification.Group = payload.Data.Group;

                    if (payload.Data.Duration > 0)
                    {
                         notification.ExpirationTime = DateTimeOffset.Now.AddSeconds(payload.Data.Duration);
                    }

                    ToastNotificationManagerCompat.CreateToastNotifier().Show(notification);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling MQTT message: {ex.Message}");
            }
            
            return Task.CompletedTask;
        };

        await _mqttClient.StartAsync(options);

        if (!string.IsNullOrWhiteSpace(mqttTopic))
        {
            var filter = new MqttTopicFilterBuilder()
                .WithTopic(mqttTopic)
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

    public async Task PublishActionAsync(string actionId, Windows.Foundation.Collections.ValueSet userInput, string baseTopic)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected) return;

        var inputDict = new Dictionary<string, string>();
        foreach (var kvp in userInput)
        {
            if (kvp.Value is string s)
            {
                inputDict[kvp.Key] = s;
            }
        }

        var payload = new 
        {
            action = actionId,
            input = inputDict
        };

        string json = System.Text.Json.JsonSerializer.Serialize(payload);
        
        if (string.IsNullOrWhiteSpace(baseTopic)) return;

        string replyTopic = baseTopic + "/actions";

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(replyTopic)
            .WithPayload(json)
            .WithRetainFlag(false)
            .Build();

        await _mqttClient.InternalClient.PublishAsync(message);
    }
}
