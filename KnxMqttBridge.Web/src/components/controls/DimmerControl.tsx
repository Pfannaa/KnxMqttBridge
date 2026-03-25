interface Props {
  onDim: (direction: 'up' | 'down') => void;
  onStop: () => void;
}

export function DimmerControl({ onDim, onStop }: Props) {
  return (
    <div className="flex gap-4 items-center w-full">
      <button
        className="flex-1 h-16 bg-slate-700 hover:bg-slate-600 active:bg-slate-500 rounded-xl
                   text-white font-bold text-3xl transition-colors select-none
                   flex items-center justify-center"
        onPointerDown={() => onDim('down')}
        onPointerUp={onStop}
        onPointerLeave={onStop}
        aria-label="Dunkler"
      >
        −
      </button>

      <button
        className="flex-1 h-16 bg-slate-700 hover:bg-slate-600 active:bg-slate-500 rounded-xl
                   text-white font-bold text-3xl transition-colors select-none
                   flex items-center justify-center"
        onPointerDown={() => onDim('up')}
        onPointerUp={onStop}
        onPointerLeave={onStop}
        aria-label="Heller"
      >
        +
      </button>
    </div>
  );
}
