# KNX-MQTT Bridge

A bidirectional bridge between KNX (via Gira X1 Gateway) and MQTT, enabling integration with home automation systems like Home Assistant, Node-RED, and more.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![KNX](https://img.shields.io/badge/KNX-IP%20Tunneling-00A9CE)](https://www.knx.org/)
[![MQTT](https://img.shields.io/badge/MQTT-3.1.1-660066)](https://mqtt.org/)

## Features

- ✅ **Bidirectional Communication** - Read KNX events → Publish to MQTT, Send MQTT commands → Write to KNX bus
- ✅ **Comprehensive DPT Support** - Boolean, Brightness, Dimming Control (4-bit), Temperature, Scene Control
- ✅ **ETS Integration** - Import group addresses directly from ETS XML export with automatic DPT detection
- ✅ **Flexible Configuration** - Configure via appsettings.json or environment variables
- ✅ **Docker Ready** - Easy deployment with volume mounts and env var overrides

---

## Quick Start

### 1. Export Group Addresses from ETS

1. Open your KNX project in **ETS 5 or ETS 6**
2. Right-click **"Group Addresses"** → **"Export Group Addresses..."**
3. Select **XML format** and enable **"Export with Data Point Types (DPT)"** ✅ **REQUIRED!**
4. Save as `GroupAddresses.xml`

### 2. Configure the Bridge

Edit `appsettings.json`:

```json
{
  "KnxConfig": {
    "XmlPath": "GroupAddresses.xml",
    "GatewayIp": "192.168.1.100",
    "GatewayPort": 3671,
    "UseAutoDiscovery": false
  },
  "Mqtt": {
    "BrokerHost": "192.168.1.10",
    "BrokerPort": 1883,
    "ClientId": "knx-mqtt-bridge",
    "Username": "",
    "Password": "",
    "TopicPrefix": "knx"
  }
}
```

### 3. Run the Bridge

**Option A: .NET**
```bash
dotnet run --project KnxMqttBridge
```

**Option B: Docker**
```bash
docker run -d \
  --name knx-mqtt-bridge \
  --restart unless-stopped \
  --network host \
  -v ./appsettings.json:/app/appsettings.json:ro \
  -v ./GroupAddresses.xml:/app/GroupAddresses.xml:ro \
  knx-mqtt-bridge:latest
```

---

## Configuration Reference

### KNX Settings

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `XmlPath` | Path to ETS group address export | `"GroupAddresses.xml"` | Yes |
| `GatewayIp` | IP address of KNX gateway | `null` | If `UseAutoDiscovery=false` |
| `GatewayPort` | KNX/IP port | `3671` | No |
| `UseAutoDiscovery` | Auto-discover gateway on network | `true` | No |

### MQTT Settings

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `BrokerHost` | MQTT broker hostname/IP | `"localhost"` | Yes |
| `BrokerPort` | MQTT broker port | `1883` | No |
| `ClientId` | MQTT client identifier | `"knx-mqtt-bridge"` | No |
| `Username` | MQTT authentication username | `""` | If broker requires auth |
| `Password` | MQTT authentication password | `""` | If broker requires auth |
| `TopicPrefix` | Prefix for all MQTT topics | `"knx"` | No |
| `CleanSession` | Start with clean MQTT session | `true` | No |
| `KeepAlivePeriod` | Keep-alive interval (seconds) | `60` | No |

### Environment Variables

Override any setting using the format `Section__Property` (double underscore):

```bash
# KNX Configuration
KnxConfig__GatewayIp=192.168.1.100
KnxConfig__UseAutoDiscovery=false

# MQTT Configuration
Mqtt__BrokerHost=192.168.1.10
Mqtt__Username=homeassistant
Mqtt__Password=secretpassword
```

---

## Docker Deployment

### Docker Compose (Recommended)

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  knx-mqtt-bridge:
    image: knx-mqtt-bridge:latest
    container_name: knx-mqtt-bridge
    restart: unless-stopped
    network_mode: host
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
      - ./GroupAddresses.xml:/app/GroupAddresses.xml:ro
    environment:
      - KnxConfig__GatewayIp=192.168.1.100
      - KnxConfig__UseAutoDiscovery=false
      - Mqtt__BrokerHost=192.168.1.10
      - Mqtt__Username=mqtt_user
      - Mqtt__Password=mqtt_password
```

Then run:
```bash
docker-compose up -d
```

**Notes:**
- `--network host` is required for KNX/IP multicast discovery
- Use `:ro` flag for read-only mounts (recommended for config files)
- Environment variables override values in `appsettings.json`

---

## Usage

### Receiving KNX Events (KNX → MQTT)

Events are automatically published to:
```
knx/{subcategory}/{category}/{name}
```

**Example:**
```json
{
  "Name": "Office Light Switch",
  "Address": "2/1/71",
  "DataPointType": "DPST-1-1",
  "Value": 1,
  "LastUpdated": "2025-10-14T10:30:45Z"
}
```

### Sending Commands (MQTT → KNX)

Send commands to: `knx/command/{address-with-dashes}`

**Important:** Use dashes instead of slashes in addresses to avoid MQTT topic conflicts.

| Data Point Type | Use Case | Payload Example |
|-----------------|----------|-----------------|
| **DPST-1-1** (Boolean) | Light switches | `1` (on) or `0` (off) |
| **DPST-5-1** (Brightness) | Dimmer value | `128` (0-255) |
| **DPST-3-7** (Dimming) | Relative dim | `{"Direction":"up","Steps":1}` |
| **DPST-9-1** (Temperature) | Setpoint | `21.5` |
| **DPST-18-1** (Scene) | Scene recall | `5` (0-63) |

**Examples:**

```bash
# Turn light ON (address 2/1/71)
mosquitto_pub -h localhost -t "knx/command/2-1-71" -m "1"

# Set brightness to 50% (address 2/1/4)
mosquitto_pub -h localhost -t "knx/command/2-1-4" -m "128"

# Dim up (address 2/1/12)
mosquitto_pub -h localhost -t "knx/command/2-1-12" -m '{"Direction":"up","Steps":1}'

# Set temperature to 21.5°C (address 3/2/15)
mosquitto_pub -h localhost -t "knx/command/3-2-15" -m "21.5"
```

---

## Integration Examples

### Home Assistant

```yaml
light:
  - platform: mqtt
    name: "Office Light"
    state_topic: "knx/Office/Lighting/Office Light Switch"
    state_value_template: "{{ value_json.Value }}"
    command_topic: "knx/command/2-1-71"
    payload_on: "1"
    payload_off: "0"
    brightness_state_topic: "knx/Office/Lighting/Office Light Brightness"
    brightness_value_template: "{{ value_json.Value }}"
    brightness_command_topic: "knx/command/2-1-4"
    brightness_scale: 255

climate:
  - platform: mqtt
    name: "Living Room"
    current_temperature_topic: "knx/HVAC/Living Room/Temperature"
    current_temperature_template: "{{ value_json.Value }}"
    temperature_command_topic: "knx/command/3-2-15"
    min_temp: 16
    max_temp: 26
```

### Node-RED

```javascript
// Toggle light
msg.topic = "knx/command/2-1-71";
msg.payload = msg.payload === "ON" ? "1" : "0";
return msg;

// Set brightness (0-100% input)
msg.topic = "knx/command/2-1-4";
msg.payload = Math.round((msg.payload / 100) * 255).toString();
return msg;
```

---

## Troubleshooting

### Command Not Working

1. ✅ Check logs for errors
2. ✅ Verify address exists in `GroupAddresses.xml`
3. ✅ Ensure correct DPT payload format
4. ✅ Use control address, not status address
5. ✅ Test command from ETS first

### No Feedback from Bus

This is **normal** for your own commands - Gira X1 filters echo to prevent loops. You'll see feedback from physical switches and other KNX devices.

### Connection Issues

**Can't connect to KNX gateway:**
- Verify IP address in configuration
- Check network connectivity: `ping 192.168.1.100`
- Ensure gateway has available tunneling slots (typically 4-5 max)
- Check firewall settings (port 3671)

**Can't connect to MQTT broker:**
- Verify broker address and port
- Check username/password
- Test: `mosquitto_sub -h localhost -t "#" -v`

---

## Supported Data Point Types

### Boolean/Switch (DPST-1-x)
- **Use:** Light switches, on/off controls
- **Payload:** `0` or `1`

### Brightness (DPST-5-x)
- **Use:** Dimmer values, brightness percentage
- **Payload:** `0` to `255` (0% to 100%)

### Dimming Control (DPST-3-7)
- **Use:** Relative dimming (increase/decrease)
- **Payload:** JSON `{"Direction":"up/down","Steps":0-7}`
- **Note:** Uses 4-bit encoding in APCI field. Steps=0 stops dimming.

### Temperature (DPST-9-x)
- **Use:** Temperature setpoints, sensor values
- **Payload:** Decimal number (e.g., `21.5`)
- **Note:** Encoded as KNX 2-byte float

### Scene Control (DPST-18-1)
- **Use:** Scene recall
- **Payload:** Scene number `0` to `63`

---

## Architecture

```
KnxMqttBridge/
├── Models/              # Data models (DimCommand)
├── Services/
│   ├── Abstractions/    # Service interfaces
│   ├── KnxService.cs    # KNX communication
│   ├── MqttService.cs   # MQTT communication
│   └── KnxValueEncoder.cs  # DPT encoding logic
├── Infrastructure/      # Configuration & XML models
├── Worker.cs           # Main orchestration
└── Program.cs          # Application entry point
```

**Technology Stack:**
- .NET 8.0
- Knx.Falcon 6.3.7959 (KNX/IP protocol)
- MQTTnet 4.x (MQTT client)

---

## FAQ

**Q: Can I use other KNX/IP interfaces besides Gira X1?**
A: Yes! Works with any KNX/IP interface supporting tunneling (e.g., KNX IP Router, ABB IPR/S, MDT Gateway).

**Q: Why don't I see my own commands echoed back?**
A: This is intentional - the gateway filters telegrams from the same connection to prevent loops.

**Q: How do I add support for additional DPT types?**
A: Add encoding logic to `KnxValueEncoder.cs`:

```csharp
return dataPointType switch
{
    "DPST-X-Y" => EncodeYourCustomType(value),
    // ... existing types
    _ => null
};
```

**Q: Can I run multiple bridges?**
A: Yes, but each uses one KNX/IP tunneling connection. Gira X1 typically supports 4-5 concurrent connections. Use different MQTT topic prefixes to avoid conflicts.

---

## Contributing

Contributions welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request

---

## License

[MIT / Apache 2.0 - Add your license here]

---

## Support

- 🐛 [Report issues](https://github.com/yourusername/KnxMqttBridge/issues)
- 💬 [Discussions](https://github.com/yourusername/KnxMqttBridge/discussions)

---

**Made with ❤️ for the KNX and Home Automation community**
