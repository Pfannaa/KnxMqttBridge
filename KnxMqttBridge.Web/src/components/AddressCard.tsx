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

function renderControl(item: DashboardItem, value: unknown, onPublish: (address: string, v: unknown) => void) {
  const family = getDptFamily(item.dpt);

  switch (family) {
    case '1':
      return (
        <ToggleSwitch
          value={value}
          onToggle={(v) => onPublish(item.address, v)}
        />
      );

    case '3':
      return (
        <DimmerControl
          onDim={(direction) => onPublish(item.address, { Direction: direction, Steps: 1 })}
          onStop={() => onPublish(item.address, { Direction: 'up', Steps: 0 })}
        />
      );

    case '5':
      return (
        <BrightnessSlider
          value={value}
          onSet={(v) => onPublish(item.address, v)}
        />
      );

    case '9':
      return <TemperatureDisplay value={value} />;

    case '18':
      return <SceneButton onActivate={() => onPublish(item.address, 0)} />;

    default:
      return <GenericDisplay value={value} />;
  }
}

export function AddressCard({ item, value, onPublish, dragHandleProps }: Props) {
  return (
    <div className="bg-slate-800 rounded-2xl p-4 flex flex-col gap-4 border border-slate-700">
      <div className="flex flex-col gap-1">
        <div className="flex items-center gap-2">
          {dragHandleProps && (
            <div
              {...dragHandleProps}
              className="flex items-center justify-center w-6 h-6 shrink-0
                         text-slate-500 hover:text-slate-300 cursor-grab active:cursor-grabbing
                         rounded touch-none select-none text-lg leading-none"
              aria-label="Verschieben"
            >
              ⠿
            </div>
          )}
          <h2 className="flex-1 text-white font-semibold text-base leading-tight truncate">{item.label}</h2>
          {item.dpt && (
            <span className="text-xs font-mono bg-slate-700 text-slate-300 px-2 py-0.5 rounded shrink-0">
              {item.dpt}
            </span>
          )}
        </div>
        <span className={`text-slate-500 text-xs font-mono ${dragHandleProps ? 'pl-8' : ''}`}>
          {item.address}
        </span>
      </div>

      <div className="flex items-center justify-center h-24 w-full">
        {renderControl(item, value, onPublish)}
      </div>
    </div>
  );
}
