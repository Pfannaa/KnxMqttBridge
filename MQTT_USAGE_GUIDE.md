# KNX-MQTT Bridge Usage Guide

## Overview

This bridge provides bidirectional communication between your KNX bus (via Gira X1) and MQTT:
- **KNX → MQTT**: Events from the KNX bus are automatically published to MQTT topics
- **MQTT → KNX**: Commands sent to MQTT topics are written to the KNX bus

---

## Receiving KNX Events (KNX → MQTT)

### Topic Format
Events are published to: `knx/{subcategory}/{category}/{name}`

**Example:**
```
knx/Schalten Dimmen/Schalten Dimmen/Büro Decke Licht sr
```

### Payload Format
All events are published as JSON with complete metadata:

```json
{
  "Name": "Büro Decke Licht sr",
  "Address": "2/1/71",
  "DataPointType": "DPST-1-1",
  "Category": "Schalten Dimmen",
  "Subcategory": "Schalten Dimmen",
  "FullPath": "Schalten Dimmen/Schalten Dimmen",
  "Security": null,
  "Value": 1,
  "RawValue": "AQ==",
  "LastUpdated": "2025-10-14T10:30:45.1234567+00:00"
}
```

**Fields:**
- `Value`: Decoded human-readable value (depends on data point type)
- `RawValue`: Base64-encoded raw KNX telegram bytes
- `LastUpdated`: Timestamp when the value was received

---

## Sending Commands (MQTT → KNX)

### Topic Format
Send commands to: `knx/command/{address-with-dashes}`

**Important:** Use dashes (`-`) instead of slashes (`/`) in the address to avoid MQTT topic conflicts.

**Examples:**
- KNX address `2/1/71` → MQTT topic `knx/command/2-1-71`
- KNX address `3/2/15` → MQTT topic `knx/command/3-2-15`

### Payload Format by Data Point Type

The payload format depends on the **Data Point Type (DPT)** of the group address as defined in your ETS export.

---

## Supported Data Point Types

### 1. Boolean/Switch (DPST-1-1, DPST-1-11, DPST-1-24)

**Used for:** Light switches, on/off controls, binary sensors

**Payload:** Plain number (`0` or `1`)

**Examples:**
```bash
# Turn light ON (address 2/1/71)
mosquitto_pub -h localhost -t "knx/command/2-1-71" -m "1"

# Turn light OFF (address 2/1/71)
mosquitto_pub -h localhost -t "knx/command/2-1-71" -m "0"
```

**Notes:**
- Any value > 0 is treated as `true`/`on`
- 0 is treated as `false`/`off`

---

### 2. Brightness/Percentage (DPST-5-1, DPST-5-10)

**Used for:** Dimmer values, brightness percentage, scaling (0-255)

**Payload:** Plain number (`0` to `255`)

**Examples:**
```bash
# Set brightness to 0% (off) - address 2/1/4
mosquitto_pub -h localhost -t "knx/command/2-1-4" -m "0"

# Set brightness to 50% (~128)
mosquitto_pub -h localhost -t "knx/command/2-1-4" -m "128"

# Set brightness to 100% (255)
mosquitto_pub -h localhost -t "knx/command/2-1-4" -m "255"
```

**Notes:**
- Value range: 0-255
- 0 = 0%, 255 = 100%
- Some actuators require the light to be switched on first before accepting brightness changes
- Make sure you're writing to the control address, not the status address

---

### 3. Dimming Control (DPST-3-7)

**Used for:** Relative dimming (increase/decrease brightness in steps)

**Payload:** JSON with `Direction` and `Steps`

**Format:**
```json
{
  "Direction": "up",    // or "down"
  "Steps": 1            // 0-7
}
```

**Examples:**
```bash
# Dim up by 1 step - address 2/1/12
mosquitto_pub -h localhost -t "knx/command/2-1-12" -m '{"Direction":"up","Steps":1}'

# Dim down by 1 step
mosquitto_pub -h localhost -t "knx/command/2-1-12" -m '{"Direction":"down","Steps":1}'

# Stop dimming (0 steps)
mosquitto_pub -h localhost -t "knx/command/2-1-12" -m '{"Direction":"up","Steps":0}'
```

**How Dimming Works:**

KNX dimming control uses a **start/stop pattern**. To dim a light:

1. **Start dimming:** Send a command with `Steps` > 0
   - Example: `{"Direction":"up","Steps":1}` starts dimming brighter
2. **The light continues dimming** as long as the command is active
3. **Stop dimming:** Send a command with `Steps` = 0
   - Example: `{"Direction":"up","Steps":0}` stops the dimming

**Typical Usage Pattern:**

```bash
# To make the light brighter:
# 1. Start dimming up
mosquitto_pub -h localhost -t "knx/command/2-1-12" -m '{"Direction":"up","Steps":1}'

# 2. Wait for desired brightness (the light keeps getting brighter)
sleep 2

# 3. Stop dimming
mosquitto_pub -h localhost -t "knx/command/2-1-12" -m '{"Direction":"up","Steps":0}'
```

**Alternative: Single Step Commands**

Some actuators respond to single commands (without needing a stop):
```bash
# Send a single "dim up" command
mosquitto_pub -h localhost -t "knx/command/2-1-12" -m '{"Direction":"up","Steps":1}'
```

The actuator will dim by a small amount and stop automatically. You can send multiple commands to continue dimming.

**Notes:**
- `Direction`: Must be `"up"` (brighter) or `"down"` (darker) - lowercase
- `Steps`: Integer from 0 to 7
  - `0` = Stop dimming
  - `1` = Typical value for start/continuous dimming
  - Higher values may dim faster (actuator-dependent)
- **Light must be ON** first for dimming commands to work
- **No echo feedback:** Dimming commands typically don't echo back to the bus
- **KNX Secure:** Ensure KNX Secure is disabled or properly configured on your actuators
- **4-bit encoding:** DPST-3-7 uses special 4-bit encoding in the KNX telegram's APCI field

---

### 4. Temperature (DPST-9-1, DPST-9-4, DPST-9-5)

**Used for:** Temperature setpoints, sensor values (2-byte float)

**Payload:** Plain decimal number

**Examples:**
```bash
# Set temperature to 21.5°C - address 3/2/15
mosquitto_pub -h localhost -t "knx/command/3-2-15" -m "21.5"

# Set temperature to 18°C
mosquitto_pub -h localhost -t "knx/command/3-2-15" -m "18"

# Set temperature to 22.8°C
mosquitto_pub -h localhost -t "knx/command/3-2-15" -m "22.8"
```

**Notes:**
- Encoded as KNX 2-byte float (DPT 9)
- Supports negative values
- Typical range: -273°C to ~670°C (depends on exponent)
- Precision: ~0.01°C

---

### 5. Scene Control (DPST-18-1)

**Used for:** Scene recall (0-63)

**Payload:** Plain number (`0` to `63`)

**Examples:**
```bash
# Activate scene 1 - address 5/1/1
mosquitto_pub -h localhost -t "knx/command/5-1-1" -m "1"

# Activate scene 10
mosquitto_pub -h localhost -t "knx/command/5-1-1" -m "10"

# Activate scene 20
mosquitto_pub -h localhost -t "knx/command/5-1-1" -m "20"
```

**Notes:**
- Valid scene numbers: 0-63 (6-bit value)
- Scene must be programmed in your KNX actuators via ETS

---

## Integration Examples

### Home Assistant

```yaml
# Light with on/off and brightness control
light:
  - platform: mqtt
    name: "Office Ceiling Light"
    # Status feedback
    state_topic: "knx/Schalten Dimmen/Schalten Dimmen/Büro Decke Licht sr"
    state_value_template: "{{ value_json.Value }}"
    # Commands
    command_topic: "knx/command/2-1-71"
    payload_on: "1"
    payload_off: "0"
    # Brightness
    brightness_state_topic: "knx/Schalten Dimmen/Schalten Dimmen/Büro Decke Licht wr"
    brightness_value_template: "{{ value_json.Value }}"
    brightness_command_topic: "knx/command/2-1-4"
    brightness_scale: 255

# Climate control (thermostat)
climate:
  - platform: mqtt
    name: "Living Room Thermostat"
    # Temperature reading
    current_temperature_topic: "knx/Heizung/Heizung/Wohnzimmer Temperatur Ist"
    current_temperature_template: "{{ value_json.Value }}"
    # Temperature setpoint
    temperature_state_topic: "knx/Heizung/Heizung/Wohnzimmer Temperatur Soll"
    temperature_state_template: "{{ value_json.Value }}"
    temperature_command_topic: "knx/command/3-2-15"
    min_temp: 16
    max_temp: 26
    temp_step: 0.5

# Binary sensor (motion, window contact, etc.)
binary_sensor:
  - platform: mqtt
    name: "Office Motion"
    state_topic: "knx/Sensoren/Sensoren/Büro Bewegung"
    value_template: "{{ value_json.Value }}"
    payload_on: "1"
    payload_off: "0"
    device_class: motion
```

---

### Node-RED

**Toggle Light:**
```javascript
// In a function node
msg.topic = "knx/command/2-1-71";
msg.payload = msg.payload === "ON" ? "1" : "0";
return msg;
```

**Set Brightness:**
```javascript
// In a function node
// Input: 0-100 percentage
msg.topic = "knx/command/2-1-4";
msg.payload = Math.round((msg.payload / 100) * 255).toString();
return msg;
```

**Dim Control:**
```javascript
// In a function node
msg.topic = "knx/command/2-1-12";
msg.payload = JSON.stringify({
    Direction: "up",
    Steps: 3
});
return msg;
```

**Set Temperature:**
```javascript
// In a function node
msg.topic = "knx/command/3-2-15";
msg.payload = "21.5";
return msg;
```

---

## Troubleshooting

### Command Not Working

1. **Check the logs** - Look for error messages or encoding failures
2. **Verify the address** - Ensure the address exists in your ETS export
3. **Check DPT** - Make sure you're sending the correct format for the data point type
4. **Control vs Status address** - You might be writing to a status address instead of the control address
5. **Test with ETS** - Verify the command works from ETS first

### Unknown Group Address Error

```
Unknown group address in command: 2/1/71. Check your ETS export.
```

**Solution:**
- The address doesn't exist in your `GroupAddresses.xml` file
- Re-export from ETS and update the XML file
- Check for typos in the address

### Light Doesn't Respond to Brightness

**Possible causes:**
1. Light needs to be switched ON first before accepting brightness
2. Writing to status address instead of control address
3. Actuator not configured for brightness control in ETS
4. Check if the actuator has a separate "switch and dim" address

### Dimming Commands Don't Work

**Possible causes:**
1. **KNX Secure not configured** - This was your issue! Ensure KNX Secure is properly set up
2. Light must be ON first
3. Wrong address (status vs control)
4. Actuator doesn't support relative dimming
5. Try absolute brightness (DPST-5-1) instead

### No Feedback from Bus

**This is normal for:**
- Your own telegrams (Gira X1 filters echo to prevent loops)
- Dimming commands (DPST-3-7) - they're fire-and-forget

**You should see feedback for:**
- Physical switch presses
- Status updates from actuators
- Commands from other KNX/IP connections

---

## Important Notes

### Address Format
- **In topics:** Use dashes (`2-1-71`)
- **In KNX:** Uses slashes (`2/1/71`)
- The bridge automatically converts between formats

### Data Point Types
- All addresses must be defined in your ETS export (`GroupAddresses.xml`)
- The DPT determines how values are encoded
- Sending wrong format for the DPT will fail silently or cause errors

### Retained Messages
- KNX status updates are published with `retain: true`
- This means new MQTT subscribers immediately get the last known state

### KNX Secure
- Ensure KNX Secure is properly configured on your actuators
- Some operations (like dimming) may require proper security setup
- Check your Gira X1 and actuator security settings in ETS

### Connection
- The bridge uses KNX/IP Tunneling protocol
- Gira X1 has limited tunneling connections (typically 4-5)
- Ensure your X1 has available connection slots

---

## Testing Commands

### Using mosquitto_pub (Linux/Mac/WSL)

```bash
# Test switch on
mosquitto_pub -h localhost -t "knx/command/2-1-71" -m "1"

# Test brightness
mosquitto_pub -h localhost -t "knx/command/2-1-4" -m "128"

# Test dimming (note the quotes for JSON)
mosquitto_pub -h localhost -t "knx/command/2-1-12" -m '{"Direction":"up","Steps":3}'

# Test temperature
mosquitto_pub -h localhost -t "knx/command/3-2-15" -m "21.5"

# Test scene
mosquitto_pub -h localhost -t "knx/command/5-1-1" -m "5"
```

### Using MQTT Explorer (GUI)

1. Connect to your MQTT broker
2. Navigate to `knx/command/`
3. Publish to the specific address topic
4. Enter the payload according to the DPT format

---

## Support

For issues or questions:
- Check the application logs for detailed error messages
- Verify your ETS export is up to date
- Ensure KNX Secure is properly configured
- Test commands from ETS first to verify KNX configuration

---

**Generated for KNX-MQTT Bridge v1.0**
**Compatible with Gira X1 KNX/IP Gateway**
