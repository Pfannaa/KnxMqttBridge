interface Props {
  value: unknown;
}

export function TemperatureDisplay({ value }: Props) {
  const temp = typeof value === 'number' ? value.toFixed(1) : '–';

  return (
    <div className="flex flex-col items-center gap-1">
      <span className="text-4xl font-bold text-white tabular-nums">{temp}</span>
      <span className="text-slate-400 text-base">°C</span>
    </div>
  );
}
