interface Props {
  connected: boolean;
  host: string;
  port: number;
}

export function ConnectionStatus({ connected, host, port }: Props) {
  return (
    <div className="flex items-center gap-2 px-3 py-1.5 bg-slate-800 rounded-lg border border-slate-700">
      <span className={`w-2.5 h-2.5 rounded-full ${connected ? 'bg-green-500' : 'bg-red-500 animate-pulse'}`} />
      <span className="text-sm text-slate-300">
        {connected ? 'Verbunden' : 'Getrennt'}
      </span>
      <span className="text-xs text-slate-500 hidden sm:block">
        {host}:{port}
      </span>
    </div>
  );
}
