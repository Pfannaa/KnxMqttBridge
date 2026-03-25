interface Props {
  value: unknown;
  onToggle: (value: number) => void;
}

export function ToggleSwitch({ value, onToggle }: Props) {
  const isOn = value === 1 || value === true;

  return (
    <div className="flex flex-col items-center gap-3">
      <button
        className={`
          relative w-20 h-11 rounded-full transition-colors duration-200 focus:outline-none overflow-hidden
          ${isOn ? 'bg-green-500' : 'bg-slate-600'}
        `}
        onClick={() => onToggle(isOn ? 0 : 1)}
        aria-label={isOn ? 'Ausschalten' : 'Einschalten'}
      >
        <span
          className={`
            absolute top-1.5 left-1.5 w-8 h-8 bg-white rounded-full shadow-lg
            transition-transform duration-200
            ${isOn ? 'translate-x-9' : 'translate-x-0'}
          `}
        />
      </button>
      <span className={`text-sm font-semibold tracking-wide ${isOn ? 'text-green-400' : 'text-slate-400'}`}>
        {isOn ? 'EIN' : 'AUS'}
      </span>
    </div>
  );
}
