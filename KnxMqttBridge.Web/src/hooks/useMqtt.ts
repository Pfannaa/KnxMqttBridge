import { useEffect, useRef, useState, useCallback } from 'react';
import mqtt from 'mqtt';
import type { FrontendSettings } from '../types';

export function useMqtt(settings: FrontendSettings | null) {
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [values, setValues] = useState<Record<string, unknown>>({});
  const clientRef = useRef<ReturnType<typeof mqtt.connect> | null>(null);
  const topicPrefixRef = useRef('knx');

  useEffect(() => {
    if (!settings) return;

    topicPrefixRef.current = settings.topicPrefix;

    const protocol = window.location.protocol === 'https:' ? 'wss' : 'ws';
    const url = `${protocol}://${settings.mqttBrokerHost}:${settings.mqttWebSocketPort}/mqtt`;

    let client: ReturnType<typeof mqtt.connect>;
    try {
      client = mqtt.connect(url, {
        username: settings.mqttUsername || undefined,
        password: settings.mqttPassword || undefined,
        clientId: `knx-ui-${Math.random().toString(16).slice(2, 10)}`,
        reconnectPeriod: 3000,
        connectTimeout: 5000,
      });
    } catch {
      return;
    }

    client.on('connect', () => {
      setConnected(true);
      setError(null);
      client.subscribe(`${settings.topicPrefix}/GroupAddresses/#`);
    });

    client.on('disconnect', () => setConnected(false));
    client.on('error', (err) => {
      setConnected(false);
      setError(err.message);
    });
    client.on('offline', () => setConnected(false));

    client.on('message', (topic: string, payload: Buffer) => {
      if (!topic.endsWith('/notification')) return;

      const prefix = `${settings.topicPrefix}/GroupAddresses/`;
      if (!topic.startsWith(prefix)) return;

      const address = topic.slice(prefix.length, -'/notification'.length);

      try {
        const data = JSON.parse(payload.toString()) as { Value: unknown };
        setValues((prev) => ({ ...prev, [address]: data.Value }));
      } catch {
        // ignore malformed payloads
      }
    });

    clientRef.current = client;

    return () => {
      client.end(true);
      setConnected(false);
    };
  }, [settings]);

  const publish = useCallback((address: string, value: unknown) => {
    if (!clientRef.current) return;
    const topic = `${topicPrefixRef.current}/GroupAddresses/${address}/command`;
    clientRef.current.publish(topic, JSON.stringify({ Value: value }));
    // Optimistic update — reflect the sent value immediately,
    // real notifications from the bridge will override this
    setValues((prev) => ({ ...prev, [address]: value }));
  }, []);

  return { connected, error, values, publish };
}
