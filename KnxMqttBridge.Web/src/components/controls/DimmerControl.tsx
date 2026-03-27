interface Props {
  onDim: (direction: 'up' | 'down') => void;
  onStop: () => void;
}

export function DimmerControl({ onDim, onStop }: Props) {
  return (
    <div className="flex gap-4 items-center w-full">
      <button
        className="flex-1 h-16 bg-zinc-700 hover:bg-zinc-600 active:bg-zinc-500 active:scale-95 rounded-xl
                   text-white font-bold text-3xl transition-colors select-none
                   flex items-center justify-center"
        onPointerDown={() => onDim('down')}
        onPointerUp={onStop}
        onPointerLeave={onStop}
        aria-label="Dim"
      >
        −
      </button>

      <button
        className="flex-1 h-16 bg-zinc-700 hover:bg-zinc-600 active:bg-zinc-500 active:scale-95 rounded-xl
                   text-white font-bold text-3xl transition-colors select-none
                   flex items-center justify-center"
        onPointerDown={() => onDim('up')}
        onPointerUp={onStop}
        onPointerLeave={onStop}
        aria-label="Brighten"
      >
        +
      </button>
    </div>
  );
}
