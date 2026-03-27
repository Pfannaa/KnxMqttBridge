import { NavLink } from 'react-router-dom';

interface Props {
  connected: boolean;
  error: string | null;
  host: string;
  port: number;
}

const navItems = [
  { to: '/', label: 'Dashboard', icon: '⚡' },
  { to: '/configure', label: 'Konfigurieren', icon: '⚙' },
  { to: '/settings', label: 'Einstellungen', icon: '🔧' },
];

export function NavBar({ connected, error, host, port }: Props) {
  return (
    <nav className="bg-slate-900 border-b border-slate-700 px-4">
      <div className="flex items-center h-16 gap-1">
        <div className="flex items-center gap-3 mr-6">
          <div className="w-8 h-8 bg-brand-600 rounded-lg flex items-center justify-center">
            <span className="text-white font-bold text-sm">FF</span>
          </div>
          <span className="text-white font-bold text-lg hidden sm:block">KNX Steuerung</span>
        </div>

        <div className="flex gap-1">
          {navItems.map(({ to, label, icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-2 px-4 h-10 rounded-lg text-sm font-medium transition-colors
                ${isActive
                  ? 'bg-brand-600 text-white'
                  : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                }`
              }
            >
              <span>{icon}</span>
              <span className="hidden sm:block">{label}</span>
            </NavLink>
          ))}
        </div>

        <div className="ml-auto flex items-center gap-2 px-3 py-1.5 bg-slate-800 rounded-lg border border-slate-700"
             title={!connected && error ? error : `${host}:${port}`}>
          <span className={`w-2 h-2 rounded-full shrink-0 ${connected ? 'bg-green-500' : 'bg-red-500 animate-pulse'}`} />
          <span className="text-sm text-slate-300 hidden sm:block">
            {connected ? 'Verbunden' : 'Getrennt'}
          </span>
          <span className="text-xs text-slate-500 hidden md:block">{host}:{port}</span>
        </div>
      </div>
    </nav>
  );
}
