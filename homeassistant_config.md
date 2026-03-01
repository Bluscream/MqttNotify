# Home Assistant Configuration Guide

This guide explains how to configure Home Assistant to send standard and persistent notifications to the **HassNotifyReceiver**.

## 1. Configure the standard Notifier
You need to create a new notification platform in Home Assistant that uses MQTT to send messages to the topic configured in HassNotifyReceiver (e.g., `desktop/notifications`).

Add this to your `configuration.yaml` in Home Assistant:

```yaml
notify:
  - name: windows_pc
    platform: mqtt
    command_topic: "desktop/notifications"
    # Ensure the command_topic exactly matches what is configured in the Windows App Settings
```

**Restart Home Assistant** after making this change.

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
