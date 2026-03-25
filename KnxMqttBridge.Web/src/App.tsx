import { useEffect, useState, useCallback, useMemo } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { NavBar } from './components/NavBar';
import { Dashboard } from './pages/Dashboard';
import { Configure } from './pages/Configure';
import { Settings } from './pages/Settings';
import { useMqtt } from './hooks/useMqtt';
import type { FrontendSettings, KnxAddress, UiConfig, DashboardItem } from './types';

interface AppConfig {
  settings: FrontendSettings;
  uiConfig: UiConfig;
}

const DEFAULT_CONFIG: AppConfig = {
  settings: {
    mqttBrokerHost: 'localhost',
    mqttWebSocketPort: 9001,
    mqttUsername: '',
    mqttPassword: '',
    topicPrefix: 'knx',
    addressStyle: 'ThreeLevel',
  },
  uiConfig: { groups: [], items: [] },
};

export default function App() {
  const [config, setConfig] = useState<AppConfig>(DEFAULT_CONFIG);
  const [addresses, setAddresses] = useState<KnxAddress[]>([]);
  const [loading, setLoading] = useState(true);

  // Stabilize reference so useMqtt only reconnects when MQTT settings actually change
  const mqttSettings = useMemo(() => config.settings, [
    config.settings.mqttBrokerHost,
    config.settings.mqttWebSocketPort,
    config.settings.mqttUsername,
    config.settings.mqttPassword,
    config.settings.topicPrefix,
  ]);

  const { connected, values, publish } = useMqtt(mqttSettings);

  useEffect(() => {
    Promise.all([
      fetch('/api/config').then((r) => r.json()) as Promise<AppConfig>,
      fetch('/api/addresses').then((r) => r.json()) as Promise<KnxAddress[]>,
    ])
      .then(([cfg, addrs]) => {
        setConfig({
          settings: { ...DEFAULT_CONFIG.settings, ...cfg.settings },
          uiConfig: { ...DEFAULT_CONFIG.uiConfig, ...cfg.uiConfig },
        });
        setAddresses(addrs);
      })
      .catch(() => {
        // Keep defaults, still show the UI
      })
      .finally(() => setLoading(false));
  }, []);

  const saveConfig = useCallback(async (next: Partial<AppConfig>) => {
    const merged = { ...config, ...next };
    setConfig(merged);
    await fetch('/api/config', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(merged),
    });
  }, [config]);

  const dashboardItems: DashboardItem[] = config.uiConfig.items
    .map((item) => {
      const meta = addresses.find((a) => a.address === item.address);
      if (!meta) return null;
      return {
        address: item.address,
        label: item.label || meta.name,
        dpt: meta.dpt,
        group: item.group || 'Sonstige',
        order: item.order,
        category: meta.category,
        subcategory: meta.subcategory,
      };
    })
    .filter((item): item is DashboardItem => item !== null)
    .sort((a, b) => a.order - b.order);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen bg-slate-950 text-slate-400">
        Lade...
      </div>
    );
  }

  return (
    <BrowserRouter>
      <div className="flex flex-col h-screen bg-slate-950 text-white overflow-hidden">
        <NavBar />
        <main className="flex-1 overflow-hidden">
          <Routes>
            <Route
              path="/"
              element={
                <Dashboard
                  items={dashboardItems}
                  values={values}
                  connected={connected}
                  settings={config.settings}
                  onPublish={publish}
                  onReorder={(uiConfig) => saveConfig({ uiConfig })}
                  uiConfig={config.uiConfig}
                />
              }
            />
            <Route
              path="/configure"
              element={
                <Configure
                  addresses={addresses}
                  uiConfig={config.uiConfig}
                  onSave={(uiConfig) => saveConfig({ uiConfig })}
                />
              }
            />
            <Route
              path="/settings"
              element={
                <Settings
                  settings={config.settings}
                  onSave={(settings) => saveConfig({ settings })}
                />
              }
            />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}
