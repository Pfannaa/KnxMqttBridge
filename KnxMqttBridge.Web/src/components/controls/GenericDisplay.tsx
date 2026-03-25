interface Props {
  value: unknown;
}

export function GenericDisplay({ value }: Props) {
  const display =
    value === null || value === undefined
      ? '–'
      : typeof value === 'object'
      ? JSON.stringify(value)
      : String(value);

  return (
    <div className="flex items-center justify-center">
      <span className="text-2xl font-mono text-white">{display}</span>
    </div>
  );
}
