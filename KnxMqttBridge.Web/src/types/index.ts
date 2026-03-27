export interface KnxAddress {
  address: string;
  name: string;
  dpt: string | null;
  category: string;
  subcategory: string;
}

export interface UiConfigItem {
  address: string;
  label?: string;
  group?: string;
  order: number;
  responseAddress?: string;
}

export interface UiConfig {
  groups: string[];
  items: UiConfigItem[];
}

export interface FrontendSettings {
  mqttBrokerHost: string;
  mqttWebSocketPort: number;
  mqttUsername: string;
  mqttPassword: string;
  topicPrefix: string;
  addressStyle: string;
}

export type DimmerDirection = 'up' | 'down';

export interface DimmerValue {
  direction: DimmerDirection;
  steps: number;
}

// Resolved item combining config + address metadata
export interface DashboardItem {
  address: string;
  label: string;
  dpt: string | null;
  group: string;
  order: number;
  category: string;
  subcategory: string;
  responseAddress?: string;
}
