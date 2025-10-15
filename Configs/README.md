# KNX Monitoring Stack

Complete monitoring stack for KNX smart home: **KNX → MQTT → Telegraf → InfluxDB → Grafana**

## 📊 Architecture

```
KNX Bus → KNX-MQTT Bridge → MQTT Broker (Mosquitto)
                                  ↓
                            Telegraf (metrics collector)
                                  ↓
                            InfluxDB (time-series DB)
                                  ↓
                            Grafana (visualization)
```

## 🚀 Quick Start

### Prerequisites

- Docker and Docker Compose
- Your ETS `GroupAddresses.xml` export file
- KNX/IP gateway on your network

### Setup

1. **Place your ETS export:**
   ```bash
   cp /path/to/your/GroupAddresses.xml ./GroupAddresses.xml
   ```

2. **Start the stack:**
   ```bash
   docker-compose up -d
   ```

3. **Access services:**
   - Grafana: http://localhost:3000 (admin/admin)
   - InfluxDB UI: http://localhost:8086 (admin/adminpassword)
   - MQTT: localhost:1883

### First Time Setup

The stack will automatically:
- ✅ Initialize InfluxDB with org `knx` and bucket `knx-data`
- ✅ Configure Grafana datasource
- ✅ Load example dashboard
- ✅ Start collecting KNX metrics

## 📁 Files

### `docker-compose.yml`
Complete stack definition with all services:
- **mosquitto**: MQTT broker
- **influxdb**: Time-series database
- **telegraf**: Metrics collector
- **grafana**: Visualization dashboard
- **knxmqttbridge**: KNX to MQTT bridge

### `mosquitto.conf`
MQTT broker configuration:
- Port 1883 (MQTT)
- Port 9001 (WebSockets)
- Anonymous access enabled (change for production!)

### `telegraf.conf`
Telegraf configuration for KNX metrics:
- Subscribes to `knx/GroupAddresses/+/+/+/notification`
- Extracts metadata as tags (Name, Category, DataPointType)
- Writes to InfluxDB with 5s interval

### `ExampleDashboard.json`
Grafana dashboard with:
- 💡 Light status indicators
- 🌡️ Room temperature gauges
- 📊 Temperature history graphs
- 📋 Status table for all devices

### `grafana-provisioning/`
Auto-configuration for Grafana:
- `datasources/influxdb.yml` - InfluxDB connection
- `dashboards/default.yml` - Dashboard provider
- `dashboards/ExampleDashboard.json` - Pre-loaded dashboard

## ⚙️ Configuration

### Change InfluxDB Credentials

Edit `docker-compose.yml`:
```yaml
influxdb:
  environment:
    - DOCKER_INFLUXDB_INIT_USERNAME=your-username
    - DOCKER_INFLUXDB_INIT_PASSWORD=your-password
    - DOCKER_INFLUXDB_INIT_ADMIN_TOKEN=your-secret-token
```

Also update the token in:
- `telegraf.conf` (line 61)
- `grafana-provisioning/datasources/influxdb.yml` (line 14)

### Change Grafana Password

Edit `docker-compose.yml`:
```yaml
grafana:
  environment:
    - GF_SECURITY_ADMIN_PASSWORD=your-password
```

### Configure KNX Bridge

Edit `docker-compose.yml` under `knxmqttbridge`:
```yaml
environment:
  # Use manual gateway configuration
  - KnxConfig__UseAutoDiscovery=false
  - KnxConfig__GatewayIp=192.168.1.169
  - KnxConfig__AddressStyle=ThreeLevel  # or TwoLevel
```

### Enable MQTT Authentication

1. Create password file in mosquitto container:
   ```bash
   docker exec -it mosquitto mosquitto_passwd -c /mosquitto/config/passwd username
   ```

2. Update `mosquitto.conf`:
   ```conf
   password_file /mosquitto/config/passwd
   allow_anonymous false
   ```

3. Update bridge configuration with MQTT credentials

## 📈 Dashboard Queries

The example dashboard uses Flux queries. Here are some examples:

**Get current light status:**
```flux
from(bucket: "knx-data")
  |> range(start: -24h)
  |> filter(fn: (r) => r["_measurement"] == "knx")
  |> filter(fn: (r) => r["Metadata_DataPointType"] == "DPST-1-1")
  |> filter(fn: (r) => r["_field"] == "Value")
  |> last()
  |> group(columns: ["Metadata_Name"])
```

**Temperature history:**
```flux
from(bucket: "knx-data")
  |> range(start: -24h)
  |> filter(fn: (r) => r["_measurement"] == "knx")
  |> filter(fn: (r) => r["Metadata_DataPointType"] == "DPST-9-1")
  |> filter(fn: (r) => r["_field"] == "Value")
  |> aggregateWindow(every: 5m, fn: mean, createEmpty: false)
```

## 🔧 Troubleshooting

### Check service logs
```bash
docker-compose logs -f [service-name]
```

### Verify MQTT messages
```bash
docker exec -it mosquitto mosquitto_sub -t "knx/#" -v
```

### Check Telegraf is receiving data
```bash
docker-compose logs telegraf | grep knx
```

### Verify InfluxDB has data
```bash
# Access InfluxDB CLI
docker exec -it influxdb influx

# Query data
> use knx-data
> SELECT * FROM knx LIMIT 10
```

### Reset everything
```bash
docker-compose down -v  # Warning: deletes all data!
docker-compose up -d
```

## 📊 Data Retention

Default retention: **30 days**

To change, edit `docker-compose.yml`:
```yaml
influxdb:
  environment:
    - DOCKER_INFLUXDB_INIT_RETENTION=90d  # 90 days
```

## 🔐 Security Recommendations

For production use:
1. ✅ Change all default passwords
2. ✅ Enable MQTT authentication
3. ✅ Use strong InfluxDB token
4. ✅ Enable TLS/SSL for exposed services
5. ✅ Use Docker secrets for credentials
6. ✅ Restrict network access with firewall rules

## 📝 Notes

- KNX Bridge uses `network_mode: host` for auto-discovery
- Other services use bridge network for isolation
- Grafana dashboard auto-loads on first start
- InfluxDB data persists in Docker volumes

## 🆘 Support

For issues or questions:
- Check logs: `docker-compose logs -f`
- Verify your `GroupAddresses.xml` is valid
- Ensure KNX gateway is reachable
- Check MQTT broker connectivity
