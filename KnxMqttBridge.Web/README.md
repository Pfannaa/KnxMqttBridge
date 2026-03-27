# KNX MQTT Web Dashboard

A touch-friendly web dashboard for monitoring and controlling KNX devices via MQTT. Built with React + ASP.NET Core.

## Features

- **Dashboard** — view and control KNX group addresses in real time (lights, dimmers, temperatures, scenes)
- **Configure** — pick which addresses to show and arrange them on the dashboard
- **Settings** — set the MQTT broker connection from the UI; changes persist to `config.json`
- Connects to the MQTT broker directly from the browser via **WebSockets**
- Reads KNX address metadata from your ETS `GroupAddresses.xml` export for automatic DPT-based controls

## Architecture

```
Browser → WebSocket → MQTT Broker ← KNX-MQTT Bridge ← KNX Bus
              ↑
    ASP.NET Core (serves the SPA + REST API for config/addresses)
```

The backend serves the React app and two API endpoints:
- `GET/POST /api/config` — load and save `config.json`
- `GET /api/addresses` — parse and return addresses from `GroupAddresses.xml`

All MQTT communication happens client-side over WebSockets. The backend does not touch MQTT.

## Prerequisites

- Docker (for container deployment)
- MQTT broker with **WebSocket listener** enabled (see below)
- Optionally: ETS `GroupAddresses.xml` export for named controls

## Quick Start

### Docker Run

```bash
docker run -d \
  --name knx-web \
  -p 8080:8080 \
  -v /path/to/config.json:/app/config.json \
  -v /path/to/GroupAddresses.xml:/app/GroupAddresses.xml \
  knxmqttbridge-web
```

On first run without a `config.json`, the app starts with defaults and you configure the broker via the Settings page. The file is created automatically on first save.

### Docker Compose

```yaml
services:
  knx-web:
    image: knxmqttbridge-web
    ports:
      - "8080:8080"
    volumes:
      - ./config.json:/app/config.json
      - ./GroupAddresses.xml:/app/GroupAddresses.xml
    restart: unless-stopped
```

### Combined with the KNX-MQTT Bridge

```yaml
services:
  knxmqttbridge:
    image: knxmqttbridge
    network_mode: host
    environment:
      - KnxConfig__GatewayIp=192.168.1.169
      - KnxConfig__UseAutoDiscovery=false
      - KnxConfig__AddressStyle=ThreeLevel
      - MqttConfig__BrokerHost=localhost
      - MqttConfig__BrokerPort=1883
    volumes:
      - ./GroupAddresses.xml:/app/GroupAddresses.xml
    restart: unless-stopped

  knx-web:
    image: knxmqttbridge-web
    ports:
      - "8080:8080"
    volumes:
      - ./config.json:/app/config.json
      - ./GroupAddresses.xml:/app/GroupAddresses.xml
    restart: unless-stopped
```

Access the dashboard at `http://<your-host>:8080`.

## Configuration

### Environment Variables

Override `appsettings.json` values using double-underscore notation:

| Variable | Default | Description |
|---|---|---|
| `Web__XmlPath` | `/app/GroupAddresses.xml` | Path to ETS GroupAddresses.xml inside the container |
| `Web__ConfigPath` | `/app/config.json` | Path to the UI config file inside the container |
| `ASPNETCORE_HTTP_PORTS` | `8080` | Port the app listens on |

### config.json

This file stores the MQTT connection settings and the dashboard layout. It is created automatically when you save for the first time via the Settings page.

```json
{
  "settings": {
    "mqttBrokerHost": "192.168.1.10",
    "mqttWebSocketPort": 9001,
    "mqttUsername": "",
    "mqttPassword": "",
    "topicPrefix": "knx",
    "addressStyle": "ThreeLevel"
  },
  "uiConfig": {
    "groups": [],
    "items": []
  }
}
```

| Field | Description |
|---|---|
| `mqttBrokerHost` | IP or hostname of the MQTT broker, reachable from the **browser** |
| `mqttWebSocketPort` | WebSocket port on the broker (Mosquitto default: 9001) |
| `mqttUsername` / `mqttPassword` | Leave empty if the broker allows anonymous access |
| `topicPrefix` | Must match the `TopicPrefix` configured in the KNX-MQTT Bridge (default: `knx`) |
| `addressStyle` | `ThreeLevel` (1/2/3) or `TwoLevel` (1/2) — must match the bridge setting |

> **Note:** `mqttBrokerHost` is used by the browser, not the server. Use the broker's IP or hostname that is reachable from whatever device is viewing the dashboard, not `localhost`.

### GroupAddresses.xml (Optional)

Export from ETS via **File → Export → Group Addresses → XML**. Mount it into the container at the path configured by `Web__XmlPath`. Without this file the dashboard still works, but controls fall back to generic displays without DPT-based styling or labels.

## MQTT Broker — WebSocket Requirement

The dashboard connects to MQTT directly from the browser, which requires WebSocket support in the broker. For **Mosquitto**, add to `mosquitto.conf`:

```conf
listener 9001
protocol websockets
allow_anonymous true
```

Restart Mosquitto after changing the config. The standard MQTT port (1883) is only used by the bridge, not the web dashboard.

## Building from Source

```bash
# From the KnxMqttBridge.Web directory
docker build -t knxmqttbridge-web .
```

The Dockerfile uses a multi-stage build:
1. **Node 20** — builds the React frontend (`npm ci && npm run build`)
2. **dotnet SDK 10** — publishes the ASP.NET Core backend
3. **dotnet runtime 10** — lean final image, port 8080

## Troubleshooting

**Dashboard shows "Disconnected"**
- Check that the MQTT broker is reachable from the browser (not just from the server)
- Confirm the WebSocket listener is enabled in Mosquitto
- Verify `mqttBrokerHost` and `mqttWebSocketPort` in Settings match your broker

**No controls visible on the dashboard**
- Go to **Configure** and add group addresses to the dashboard

**Addresses show as "unknown" without labels or DPT**
- Ensure `GroupAddresses.xml` is mounted and the path matches `Web__XmlPath`

**Settings not persisting after container restart**
- Mount `config.json` as a volume — without a volume mount, changes are lost when the container restarts

**Check container logs**
```bash
docker logs knx-web
```
