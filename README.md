# HassNotifyReceiver

HassNotifyReceiver is a lightweight, background Windows application designed to receive notifications from Home Assistant over **MQTT** and instantly display them as native **Windows Toasts**. 

It runs quietly in your System Tray and automatically reconnects to your broker if the connection drops.

## Features

- **Native UI Integration**: Leverages `Microsoft.Toolkit.Uwp.Notifications` to push native Windows Action Center toasts.
- **Support for Images**: Include an `image` URL in your payload, and it will be rendered within the Windows Toast.
- **Environment Variables Support**: Easy automated deployments with no GUI needed for configuration.
- **Home Assistant Persistent Notifications**: Includes a quick setup guide to seamlessly push all persistent HA notifications directly to your desktop.

## Installation

1. Go to the [Releases Page](https://github.com/Bluscream/HassNotifyReceiver/releases) and download the latest standalone executable (`HassNotifyReceiver.exe`).
2. Run it! The app will minimize to your System Tray (near the clock).
3. Right-click the tray icon and click `Settings` to configure your broker.

### Optional: Configuration via Environment Variables
If you prefer not to use the UI or are auto-launching the app via a script, you can provide the configuration using environment variables. These variables override any values saved in the UI:
- `MQTT_IP`
- `MQTT_PORT`
- `MQTT_USER`
- `MQTT_PW`

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
description: "Listens for new persistent notifications and sends them to HassNotifyReceiver via MQTT."
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
