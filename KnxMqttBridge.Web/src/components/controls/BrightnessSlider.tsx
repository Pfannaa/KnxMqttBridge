import { useState, useEffect } from 'react';

interface Props {
  value: unknown;
  onSet: (value: number) => void;
}

export function BrightnessSlider({ value, onSet }: Props) {
  const numericValue = typeof value === 'number' ? Math.round(value) : 0;
  const [localValue, setLocalValue] = useState(numericValue);
  const [isDragging, setIsDragging] = useState(false);

  useEffect(() => {
    if (!isDragging) setLocalValue(numericValue);
  }, [numericValue, isDragging]);

  return (
    <div className="flex flex-col gap-3 w-full px-2">
      <div className="flex justify-between items-center">
        <span className="text-zinc-400 text-sm">0%</span>
        <span className="text-white font-bold text-xl">{localValue}%</span>
        <span className="text-zinc-400 text-sm">100%</span>
      </div>
      <input
        type="range"
        min={0}
        max={100}
        value={localValue}
        className="w-full"
        onChange={(e) => {
          setLocalValue(Number(e.target.value));
          setIsDragging(true);
        }}
        onMouseUp={() => {
          setIsDragging(false);
          onSet(localValue);
        }}
        onTouchEnd={() => {
          setIsDragging(false);
          onSet(localValue);
        }}
      />
    </div>
  );
}
