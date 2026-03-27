import { GripVertical } from 'lucide-react';
import type { DashboardItem } from '../types';
import { ToggleSwitch } from './controls/ToggleSwitch';
import { BrightnessSlider } from './controls/BrightnessSlider';
import { DimmerControl } from './controls/DimmerControl';
import { TemperatureDisplay } from './controls/TemperatureDisplay';
import { SceneButton } from './controls/SceneButton';
import { GenericDisplay } from './controls/GenericDisplay';

interface Props {
  item: DashboardItem;
  value: unknown;
  onPublish: (address: string, value: unknown) => void;
  dragHandleProps?: React.HTMLAttributes<HTMLDivElement>;
}

function getDptFamily(dpt: string | null): string {
  if (!dpt) return 'unknown';
  const match = dpt.match(/^DPST?-(\d+)/i);
  return match ? match[1] : 'unknown';
}

function isActiveValue(dpt: string | null, value: unknown): boolean {
  const family = getDptFamily(dpt);
  if (family === '1') return value === 1 || value === true;
  if (family === '5') return typeof value === 'number' && value > 0;
  return false;
}

function renderControl(item: DashboardItem, value: unknown, onPublish: (address: string, v: unknown) => void) {
  const family = getDptFamily(item.dpt);

  switch (family) {
    case '1':
      return <ToggleSwitch value={value} onToggle={(v) => onPublish(item.address, v)} />;
    case '3':
      return <DimmerControl onDim={(direction) => onPublish(item.address, { Direction: direction, Steps: 1 })} onStop={() => onPublish(item.address, { Direction: 'up', Steps: 0 })} />;
    case '5':
      return <BrightnessSlider value={value} onSet={(v) => onPublish(item.address, v)} />;
    case '9':
      return <TemperatureDisplay value={value} />;
    case '18':
      return <SceneButton onActivate={() => onPublish(item.address, 0)} />;
    default:
      return <GenericDisplay value={value} />;
  }
}

export function AddressCard({ item, value, onPublish, dragHandleProps }: Props) {
  const active = isActiveValue(item.dpt, value);
  return (
    <div className={`bg-zinc-800/60 rounded-xl p-4 flex flex-col gap-3 transition-colors duration-300 border-l-2 ${
      active ? 'border-brand-500/50' : 'border-transparent'
    }`}>
      <div className="flex flex-col gap-0.5">
        <div className="flex items-center gap-1.5">
          {dragHandleProps && (
            <div
              {...dragHandleProps}
              className="shrink-0 text-zinc-600 hover:text-zinc-400 cursor-grab active:cursor-grabbing touch-none select-none"
              aria-label="Drag to reorder"
            >
              <GripVertical className="w-4 h-4" strokeWidth={1.75} />
            </div>
          )}
          <h2 className="flex-1 text-white font-medium text-sm leading-tight truncate">{item.label}</h2>
          {item.dpt && (
            <span className="text-xs font-mono bg-zinc-700/60 text-zinc-400 px-1.5 py-0.5 rounded shrink-0">
              {item.dpt}
            </span>
          )}
        </div>
        <span className={`text-zinc-600 text-xs font-mono ${dragHandleProps ? 'pl-[22px]' : ''}`}>
          {item.address}
        </span>
      </div>

      <div className="flex items-center justify-center h-24 w-full">
        {renderControl(item, value, onPublish)}
      </div>
    </div>
  );
}
