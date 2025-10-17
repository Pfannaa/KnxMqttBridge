# KNX-MQTT Bridge

A bidirectional bridge between KNX (via Gira X1 Gateway) and MQTT, enabling integration with home automation systems like Home Assistant, Node-RED, and more.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![KNX](https://img.shields.io/badge/KNX-IP%20Tunneling-00A9CE)](https://www.knx.org/)
[![MQTT](https://img.shields.io/badge/MQTT-3.1.1-660066)](https://mqtt.org/)

## Features

- ✅ **Bidirectional Communication** - Read KNX events → Publish to MQTT, Send MQTT commands → Write to KNX bus
- ✅ **Comprehensive DPT Support** - Boolean, Brightness, Dimming Control (4-bit), Temperature, Scene Control, and more
- ✅ **ETS Integration** - Import group addresses directly from ETS XML export with automatic DPT detection
- ✅ **Docker Ready** - Easy deployment with docker-compose

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
    "XmlPath": "GroupAddresses.xml"
  },
  "Mqtt": {
    "BrokerHost": "192.168.1.10",
    "BrokerPort": 1883,
    "ClientId": "knx-mqtt-bridge",
    "Username": "",
    "Password": "",
    "TopicPrefix": "knx",
    "CleanSession": true,
    "KeepAlivePeriod": 60
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

## Configuration

### KNX Settings

| Parameter | Description | Default |
|-----------|-------------|---------|
| `XmlPath` | Path to ETS group address export | `"GroupAddresses.xml"` |
| `GatewayIp` | IP address of KNX gateway (required if UseAutoDiscovery=false) | `null` |
| `GatewayPort` | KNX/IP port | `3671` |
| `UseAutoDiscovery` | Auto-discover gateway on network | `true` |
| `AddressStyle` | KNX address format: `ThreeLevel` (1/2/3) or `TwoLevel` (1/2) | `ThreeLevel` |

**Notes:**
- If `UseAutoDiscovery` is `true` (default), the gateway is automatically discovered using multicast
- If `UseAutoDiscovery` is `false`, you **must** provide `GatewayIp`
- `AddressStyle` determines the MQTT topic structure and must match your KNX installation:
  - `ThreeLevel`: Topics like `knx/GroupAddresses/2/1/71/command` (standard 3-level addressing)
  - `TwoLevel`: Topics like `knx/GroupAddresses/2/71/command` (2-level addressing)

**⚠️ Container Networking Limitations:**
- **Windows containers (Docker/Podman)**: Auto-discovery does **NOT** work due to multicast limitations. You **must** use manual configuration with `UseAutoDiscovery=false` and specify `GatewayIp`.
- **Linux containers**: Auto-discovery works if your KNX gateway supports multicast discovery and the container has proper network access.
- **Native Windows/.NET**: Auto-discovery works normally when running outside containers.

### MQTT Settings

| Parameter | Description | Default |
|-----------|-------------|---------|
| `BrokerHost` | MQTT broker hostname/IP | `"localhost"` |
| `BrokerPort` | MQTT broker port | `1883` |
| `ClientId` | MQTT client identifier | `"knx-mqtt-bridge"` |
| `Username` | MQTT authentication username | `""` |
| `Password` | MQTT authentication password | `""` |
| `TopicPrefix` | Prefix for all MQTT topics | `"knx"` |
| `CleanSession` | Start with clean MQTT session | `true` |
| `KeepAlivePeriod` | Keep-alive interval (seconds) | `60` |

### Environment Variables

Override any setting using the format `Section__Property` (double underscore):

```bash
# KNX Configuration
KnxConfig__GatewayIp=192.168.1.169
KnxConfig__GatewayPort=3671
KnxConfig__UseAutoDiscovery=false
KnxConfig__AddressStyle=ThreeLevel

# MQTT Configuration
Mqtt__BrokerHost=192.168.1.10
Mqtt__BrokerPort=1883
Mqtt__Username=homeassistant
Mqtt__Password=secretpassword
Mqtt__TopicPrefix=knx
```

**Using with Docker Run:**
```bash
docker run -d \
  --name knx-mqtt-bridge \
  --restart unless-stopped \
  --network host \
  -e KnxConfig__UseAutoDiscovery=false \
  -e KnxConfig__GatewayIp=192.168.1.169 \
  -e Mqtt__BrokerHost=192.168.1.10 \
  -e Mqtt__Username=homeassistant \
  -e Mqtt__Password=secretpassword \
  -v ./GroupAddresses.xml:/app/GroupAddresses.xml:ro \
  knx-mqtt-bridge:latest
```

**Using with Docker Compose:**
```yaml
version: '3.8'

services:
  knx-mqtt-bridge:
    image: knx-mqtt-bridge:latest
    container_name: knx-mqtt-bridge
    restart: unless-stopped
    network_mode: host
    environment:
      # KNX Configuration
      - KnxConfig__UseAutoDiscovery=false
      - KnxConfig__GatewayIp=192.168.1.169
      - KnxConfig__GatewayPort=3671
      - KnxConfig__AddressStyle=ThreeLevel  # or TwoLevel
      # MQTT Configuration
      - Mqtt__BrokerHost=192.168.1.10
      - Mqtt__BrokerPort=1883
      - Mqtt__Username=homeassistant
      - Mqtt__Password=secretpassword
      - Mqtt__TopicPrefix=knx
    volumes:
      - ./GroupAddresses.xml:/app/GroupAddresses.xml:ro
```

---

## Usage

### Address Styles

The bridge supports both KNX addressing styles:

**Three-Level (default):** `main/middle/sub` (e.g., `2/1/71`)
- Most common format
- Topics: `knx/GroupAddresses/2/1/71/notification` and `knx/GroupAddresses/2/1/71/command`

**Two-Level:** `main/sub` (e.g., `2/71`)
- Legacy format used in some installations
- Topics: `knx/GroupAddresses/2/71/notification` and `knx/GroupAddresses/2/71/command`
- Set `"AddressStyle": TwoLevel` in configuration

### Receiving KNX Events (KNX → MQTT)

Events are automatically published under the `GroupAddresses` topic namespace:

**Three-Level format:**
```
knx/GroupAddresses/{main}/{middle}/{sub}/notification
```

Examples:
- `knx/GroupAddresses/2/1/71/notification` - Office Light Switch
- `knx/GroupAddresses/2/1/4/notification` - Office Light Brightness
- `knx/GroupAddresses/3/2/15/notification` - Living Room Temperature

**Two-Level format:**
```
knx/GroupAddresses/{main}/{sub}/notification
```

Examples:
- `knx/GroupAddresses/2/71/notification` - Office Light Switch
- `knx/GroupAddresses/2/4/notification` - Office Light Brightness
- `knx/GroupAddresses/3/15/notification` - Living Room Temperature

**Example payload (with ETS configuration):**
```json
{
  "Address": "2/1/71",
  "RawValue": "AQ==",
  "Value": 1,
  "Timestamp": "2025-10-14T10:30:45Z",
  "Metadata": {
    "Name": "Office Light Switch",
    "Category": "Lighting",
    "Subcategory": "Office",
    "FullPath": "Lighting/Office",
    "DataPointType": "DPST-1-1",
    "DataPointDescription": "Switch",
    "ClassicDataType": "Boolean",
    "Security": null
  }
}
```

**Example payload (without ETS configuration):**
```json
{
  "Address": "2/1/71",
  "RawValue": "AQ==",
  "Value": 1,
  "Timestamp": "2025-10-14T10:30:45Z"
}
```

**Value Types in Notifications:**
- **Boolean (DPST-1-x):** Integer `0` or `1` (not boolean for InfluxDB compatibility)
- **Percentage (DPST-5-1):** Float `0.0` to `100.0` (percentage)
- **Temperature (DPST-9-x):** Float with decimal precision (e.g., `21.5`)
- **Scene (DPST-18-1):** Integer `0` to `63`

**Benefits of this structure:**
- **Clear namespace separation** - Group addresses under `GroupAddresses`, leaves room for system topics (e.g., `knx/errors`, `knx/status`)
- **Natural hierarchy** - Matches KNX 3-level group addresses
- **Easy filtering** - Subscribe to `knx/GroupAddresses/2/#` for all main group 2 devices
- **Intuitive discovery** - Address visible directly in topic path
- **Works with or without ETS** - Bridge functions with heuristic decoding if no configuration provided

### Sending Commands (MQTT → KNX)

Send commands using the same hierarchical structure under `GroupAddresses`:

**Three-Level format:**
```
knx/GroupAddresses/{main}/{middle}/{sub}/command
```

Examples:
- `knx/GroupAddresses/2/1/71/command` - Toggle Office Light
- `knx/GroupAddresses/2/1/4/command` - Set Office Light Brightness
- `knx/GroupAddresses/3/2/15/command` - Set Living Room Temperature

**Two-Level format:**
```
knx/GroupAddresses/{main}/{sub}/command
```

Examples:
- `knx/GroupAddresses/2/71/command` - Toggle Office Light
- `knx/GroupAddresses/2/4/command` - Set Office Light Brightness
- `knx/GroupAddresses/3/15/command` - Set Living Room Temperature

### Command Payload Format

All commands must be sent as **JSON objects** with the following structure:

```json
{
  "Value": <value>,
  "DataPointType": "<optional-dpt>"
}
```

- **`Value`** (required): The value to send - can be a number, boolean, string, or object
- **`DataPointType`** (optional): Only needed if the address is not in your ETS configuration

| Data Point Type | Use Case | Example Payload |
|-----------------|----------|-----------------|
| **DPST-1-1** (Boolean) | Light switches | `{"Value": 1}` or `{"Value": 0}` |
| **DPST-5-1** (Percentage) | Dimmer brightness | `{"Value": 50}` (0-100%) |
| **DPST-3-7** (Dimming) | Relative dim | `{"Value": {"Direction":"up","Steps":1}}` |
| **DPST-9-1** (Temperature) | Setpoint | `{"Value": 21.5}` |
| **DPST-18-1** (Scene) | Scene recall | `{"Value": 5}` (0-63) |

**Command Examples (Three-Level):**

```bash
# Turn light ON (address 2/1/71)
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/1/71/command" -m '{"Value":1}'

# Turn light OFF
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/1/71/command" -m '{"Value":0}'

# Set brightness to 50% (address 2/1/4)
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/1/4/command" -m '{"Value":50}'

# Dim up (address 2/1/12)
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/1/12/command" -m '{"Value":{"Direction":"up","Steps":1}}'

# Dim down
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/1/12/command" -m '{"Value":{"Direction":"down","Steps":3}}'

# Set temperature to 21.5°C (address 3/2/15)
mosquitto_pub -h localhost -t "knx/GroupAddresses/3/2/15/command" -m '{"Value":21.5}'
```

**Command Examples (Two-Level):**

```bash
# Turn light ON (address 2/71)
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/71/command" -m '{"Value":1}'

# Set brightness to 50% (address 2/4)
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/4/command" -m '{"Value":50}'

# Dim up (address 2/12)
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/12/command" -m '{"Value":{"Direction":"up","Steps":1}}'
```

**Commands for Unconfigured Addresses:**

If you're running without ETS configuration, include the `DataPointType` in the payload:

```bash
# Boolean switch without config
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/1/71/command" -m '{"Value":1,"DataPointType":"DPST-1-1"}'

# Temperature setpoint without config
mosquitto_pub -h localhost -t "knx/GroupAddresses/3/2/15/command" -m '{"Value":21.5,"DataPointType":"DPST-9-1"}'

# Brightness without config (75%)
mosquitto_pub -h localhost -t "knx/GroupAddresses/2/1/4/command" -m '{"Value":75,"DataPointType":"DPST-5-1"}'
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
```

Then run:
```bash
docker-compose up -d
```

### Docker Run

**Linux (with auto-discovery):**
```bash
docker run -d \
  --name knx-mqtt-bridge \
  --restart unless-stopped \
  --network host \
  -v ./appsettings.json:/app/appsettings.json:ro \
  -v ./GroupAddresses.xml:/app/GroupAddresses.xml:ro \
  knx-mqtt-bridge:latest
```

**Windows/Podman (manual configuration required):**
```bash
docker run -d \
  --name knx-mqtt-bridge \
  --restart unless-stopped \
  --network host \
  -e KnxConfig__UseAutoDiscovery=false \
  -e KnxConfig__GatewayIp=192.168.1.169 \
  -e KnxConfig__GatewayPort=3671 \
  -e Mqtt__BrokerHost=192.168.1.10 \
  -v ./GroupAddresses.xml:/app/GroupAddresses.xml:ro \
  knx-mqtt-bridge:latest
```

### Volume Mounting

You can mount your own configuration files to override the defaults:

**Using absolute paths:**
```bash
docker run -d \
  --name knx-mqtt-bridge \
  --restart unless-stopped \
  --network host \
  -v /home/user/knx/appsettings.json:/app/appsettings.json:ro \
  -v /home/user/knx/GroupAddresses.xml:/app/GroupAddresses.xml:ro \
  knx-mqtt-bridge:latest
```

**Using relative paths (from current directory):**
```bash
docker run -d \
  --name knx-mqtt-bridge \
  --restart unless-stopped \
  --network host \
  -v $(pwd)/config/appsettings.json:/app/appsettings.json:ro \
  -v $(pwd)/config/GroupAddresses.xml:/app/GroupAddresses.xml:ro \
  knx-mqtt-bridge:latest
```

**Docker Compose with custom paths:**
```yaml
version: '3.8'

services:
  knx-mqtt-bridge:
    image: knx-mqtt-bridge:latest
    container_name: knx-mqtt-bridge
    restart: unless-stopped
    network_mode: host
    volumes:
      # Mount from custom directory
      - ./config/appsettings.json:/app/appsettings.json:ro
      - ./config/GroupAddresses.xml:/app/GroupAddresses.xml:ro
      # Or use absolute paths
      # - /home/user/knx/appsettings.json:/app/appsettings.json:ro
      # - /home/user/knx/GroupAddresses.xml:/app/GroupAddresses.xml:ro
```

**Recommended directory structure:**
```
/home/user/knx-bridge/
├── docker-compose.yml
├── config/
│   ├── appsettings.json          ← Your custom settings
│   └── GroupAddresses.xml        ← Your ETS export
└── logs/                         ← Optional
```

### Updating Configuration

To update configuration files:

1. **Edit your local files** (e.g., `./config/appsettings.json`)
2. **Restart the container:**
   ```bash
   docker restart knx-mqtt-bridge
   ```
   Or with docker-compose:
   ```bash
   docker-compose restart
   ```

The container will reload configuration from your mounted files on restart.

**Notes:**
- `--network host` is required for KNX auto-discovery to work
- Use `:ro` flag for read-only mounts (recommended for config files)
- Always use absolute paths or paths relative to docker-compose.yml location

---

## Integration Examples

### Home Assistant

```yaml
light:
  - platform: mqtt
    name: "Office Light"
    state_topic: "knx/GroupAddresses/2/1/71/notification"
    state_value_template: "{{ value_json.Value }}"
    command_topic: "knx/GroupAddresses/2/1/71/command"
    payload_on: '{"Value":1}'
    payload_off: '{"Value":0}'
    brightness_state_topic: "knx/GroupAddresses/2/1/4/notification"
    brightness_value_template: "{{ value_json.Value }}"
    brightness_command_topic: "knx/GroupAddresses/2/1/4/command"
    brightness_command_template: '{"Value":{{ value }}}'
    brightness_scale: 100

climate:
  - platform: mqtt
    name: "Living Room"
    current_temperature_topic: "knx/GroupAddresses/3/2/15/notification"
    current_temperature_template: "{{ value_json.Value }}"
    temperature_command_topic: "knx/GroupAddresses/3/2/15/command"
    temperature_command_template: '{"Value":{{ value }}}'
    min_temp: 16
    max_temp: 26
```

### Node-RED

```javascript
// Toggle light
msg.topic = "knx/GroupAddresses/2/1/71/command";
msg.payload = JSON.stringify({
    Value: msg.payload === "ON" ? 1 : 0
});
return msg;

// Set brightness (0-100% input)
msg.topic = "knx/GroupAddresses/2/1/4/command";
msg.payload = JSON.stringify({
    Value: msg.payload  // Pass through directly as percentage
});
return msg;

// Dim up
msg.topic = "knx/GroupAddresses/2/1/12/command";
msg.payload = JSON.stringify({
    Value: {
        Direction: "up",
        Steps: 1
    }
});
return msg;

// Subscribe to all devices in main group 2 (lighting)
msg.topic = "knx/GroupAddresses/2/#";
return msg;
```

---

## Troubleshooting

### Command Not Working

1. ✅ Check logs for errors
2. ✅ Verify address exists in `GroupAddresses.xml`
3. ✅ Ensure correct DPT payload format
4. ✅ Use control address, not status address

### No Feedback from Bus

This is **normal** for your own commands - the gateway filters echo to prevent loops. You'll see feedback from physical switches and other devices.

### Connection Issues

**Can't connect to KNX gateway:**
- Verify gateway is on network and reachable
- Check `--network host` is used in Docker
- Ensure gateway has available tunneling slots (typically 4-5 max)

**Can't connect to MQTT broker:**
- Verify broker address and port
- Check username/password
- Test: `mosquitto_sub -h localhost -t "#" -v`

---

## Supported Data Point Types

### Boolean/Switch (DPST-1-x)
- **Use:** Light switches, on/off controls
- **Payload:** `0` or `1`
- **Published Value:** Integer `0` or `1` (not boolean for InfluxDB compatibility)

### Percentage/Brightness (DPST-5-1)
- **Use:** Dimmer brightness, percentage values
- **Command Payload:** `0` to `100` (percentage)
- **Published Value:** `0.0` to `100.0` (float percentage)
- **Note:** Commands accept intuitive 0-100% values, automatically converted to KNX 0-255 range

### Dimming Control (DPST-3-7)
- **Use:** Relative dimming (increase/decrease)
- **Payload:** JSON `{"Direction":"up/down","Steps":0-7}`
- **Note:** Uses 4-bit encoding in APCI field. Steps=0 stops dimming.

### Temperature (DPST-9-x)
- **Use:** Temperature setpoints, sensor values
- **Payload:** Decimal number (e.g., `21.5`)

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
│   ├── KnxService.cs    # KNX communication (auto-discovery)
│   ├── MqttService.cs   # MQTT communication
│   └── KnxValueEncoder.cs  # DPT encoding logic
├── Infrastructure/      # Configuration & XML models
├── Worker.cs           # Main orchestration
└── Program.cs          # Application entry point
```

**Technology Stack:**
- .NET
- Knx.Falcon (KNX/IP protocol)
- MQTTnet (MQTT client)

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
A: Yes, but each uses one KNX/IP tunneling connection. Most gateways support 4-5 concurrent connections.

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

- 🐛 [Report issues](https://github.com/Pfannaa/KnxMqttBridge/issues)
- 💬 [Discussions](https://github.com/Pfannaa/KnxMqttBridge/discussions)

---

**Made with ❤️ for the KNX and Home Automation community**
