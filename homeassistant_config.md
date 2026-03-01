# Home Assistant Configuration Guide

This guide explains how to configure Home Assistant to send standard and persistent notifications to the **MqttNotify** listener app.

## 1. Configure the standard Notifier

### Option A: Automatic Discovery (Recommended)
Starting with version 1.5.0, the **MqttNotify.Listener** app will automatically Register itself with Home Assistant using **MQTT Discovery**. 

Once the app is running and connected to your broker, a new entity named `notify.windows_notifications_<your_pc_name>` should appear automatically in your Home Assistant Devices/Settings.

### Option B: Manual YAML (Fallback)
If Discovery is disabled in your MQTT integration, you can manually add this to your `configuration.yaml`:

```yaml
mqtt:
  - notify:
      name: "windows_pc"
      command_topic: "desktop/notifications"
```

**Restart Home Assistant** after making manual changes.

### How to use it in Automations:
You can now use the `notify.windows_pc` service in HA like any other notifier.
```yaml
service: notify.windows_pc
data:
  title: "Front Door Opened"
  message: "Someone opened the front door."
  data:
    # Optional image
    image: "http://<YOUR_HA_IP>:8123/local/images/front_door.jpg"
```

## 2. Forwarding Persistent Notifications
By default, Home Assistant doesn't broadcast its internal persistent notifications over MQTT. To send them to your Windows PC, you can add a simple automation that listens for the `persistent_notification.create` event and publishes it to the MQTT topic.

Add this automation to your Home Assistant:

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
      topic: "desktop/notifications" # Make sure this matches!
      payload_template: >
        {
          "title": "{{ trigger.event.data.service_data.title | default('Home Assistant', true) | tojson }}",
          "message": "{{ trigger.event.data.service_data.message | tojson }}"
        }
mode: queued
```

### Optional: Include System Notifications
There are also internal system notifications. You may want to subscribe to `persistent_notifications_updated` events if you have advanced needs, but the above automation covers 99% of user-created and integration-created persistent notifications!
