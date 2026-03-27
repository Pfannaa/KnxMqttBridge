import { Power } from 'lucide-react';

interface Props {
  value: unknown;
  onToggle: (value: number) => void;
}

export function ToggleSwitch({ value, onToggle }: Props) {
  const isOn = value === 1 || value === true;

  return (
    <button
      className={`w-full h-full rounded-lg flex flex-col items-center justify-center gap-2 transition-all duration-200 focus:outline-none
        ${isOn
          ? 'bg-brand-500/20 text-brand-300'
          : 'bg-zinc-700 text-zinc-200 hover:bg-zinc-600'
        }`}
      onClick={() => onToggle(isOn ? 0 : 1)}
      aria-label={isOn ? 'Turn off' : 'Turn on'}
    >
      <Power className="w-7 h-7" strokeWidth={1.75} />
      <span className="text-xs font-semibold tracking-widest">{isOn ? 'ON' : 'OFF'}</span>
    </button>
  );
}
