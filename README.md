# MqttNotify

**MqttNotify** (formerly HassNotifyReceiver) is a lightweight, headless Windows console application designed to receive notifications over **MQTT** and instantly display them as native **Windows Toasts**. It provides full feature parity with the popular `HASS.Agent` notification schema.

It runs cleanly from the Command Line (or a background script) and automatically manages reconnections to your broker.

## Features

- **Headless Console App**: No GUI or System Tray icons. Configure it entirely via CLI Arguments or Environment Variables.
- **Native UI Integration**: Leverages `Microsoft.Toolkit.Uwp.Notifications` to push native Windows Action Center toasts.
- **HASS.Agent Parity**: Achieve 100% feature parity with HASS.Agent notifications. You can:
  - Add interactive `actions` (Buttons) which report back via MQTT.
  - Add text `inputs` (Quick Replies) which report back via MQTT.
  - Embed `image` hero images or custom `icon_url` overrides.
  - Automatically handle `clickAction` URL launches.
  - Force `sticky: true` (Reminder Notifications) or `importance: high` (Alarm Notifications).
  - Clear specific notifications instantly using a `clear_notification` payload with `tag` and `group` matching.
- **Test Application Included**: Easily test your notifications by using the bundled `MqttNotify.Test` project.

## Installation & Usage

1. Go to the [Releases Page](https://github.com/Bluscream/MqttNotify/releases) and download the compiled binaries.
2. Open a Terminal or Command Prompt.
3. Run the Listener with your required parameters:

```bash
.\MqttNotify.Listener.exe --mqtt-ip 192.168.1.100 --mqtt-port 1883 --mqtt-user <user> --mqtt-pw <pass>
```

### Configuration Precedence:
The app reads settings in the following order:
1. Command Line Arguments (`--mqtt-ip`, `--mqtt-port`, `--mqtt-user`, `--mqtt-pw`, `--mqtt-topic`)
2. Environment Variables (`MQTT_IP`, `MQTT_PORT`, `MQTT_USER`, `MQTT_PW`, `MQTT_TOPIC`)

### Testing your Setup:
You can quickly ensure Notifications are showing correctly by running the included `MqttNotify.Test.exe` application:
```bash
.\MqttNotify.Test.exe --mqtt-ip 192.168.1.100 --title "Incoming Alert!" --message "Testing the Windows Toast!"
```
This application will send a mock payload (containing dummy buttons, inputs, and URLs) to verify your Listener handles rich notifications flawlessly.

## Home Assistant Setup

### Option 1: Add a standard MQTT Notifier
To use this app as a standard notification service in Home Assistant automations (`notify.windows_pc`), add this to your `configuration.yaml` and restart HA:

```yaml
notify:
  - name: windows_pc
    platform: mqtt
    command_topic: "desktop/notifications" # Make sure this matches the App Settings
```

**Example Usage in Automations:**
```yaml
service: notify.windows_pc
data:
  title: "Server Room"
  message: "Motion Detected!"
  data:
    image: "http://<YOUR_HA_IP>:8123/local/images/server_room.jpg"
```

### Option 2: Forward all HA Persistent Notifications
Home Assistant doesn't expose its internal system/persistent notifications to MQTT by default. You can easily forward them by adding this automation to Home Assistant:

```yaml
alias: "Forward Persistent Notifications to Windows PC"
description: "Listens for new persistent notifications and sends them to MqttNotify via MQTT."
trigger:
  - platform: event
    event_type: call_service
    event_data:
      domain: persistent_notification
      service: create
action:
  - service: mqtt.publish
    data:
      topic: "desktop/notifications" # Make sure this matches the App Settings
      payload_template: >
        {
          "title": "{{ trigger.event.data.service_data.title | default('Home Assistant', true) | tojson }}",
          "message": "{{ trigger.event.data.service_data.message | tojson }}"
        }
mode: queued
```

## Contributing

Created with ❤️ and AI. Feel free to open issues or PRs.
