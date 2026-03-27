import { NavLink } from 'react-router-dom';
import { Flame, LayoutDashboard, SlidersHorizontal, Settings, Wifi, WifiOff } from 'lucide-react';

interface Props {
  connected: boolean;
  error: string | null;
  host: string;
  port: number;
}

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/configure', label: 'Configure', icon: SlidersHorizontal },
  { to: '/settings', label: 'Settings', icon: Settings },
];

export function NavBar({ connected, error, host, port }: Props) {
  return (
    <nav className="bg-zinc-900 border-b border-zinc-800 px-4">
      <div className="flex items-center h-14 gap-1">
        <div className="flex items-center gap-2 mr-6">
          <Flame className="w-6 h-6 text-brand-500" strokeWidth={2} />
          <span className="text-white font-semibold text-base hidden sm:block tracking-tight">KNX Control</span>
        </div>

        <div className="flex gap-0.5">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-2 px-3 h-9 rounded-md text-sm font-medium transition-colors
                ${isActive
                  ? 'bg-zinc-800 text-white'
                  : 'text-zinc-400 hover:bg-zinc-800/60 hover:text-zinc-100'
                }`
              }
            >
              <Icon className="w-4 h-4 shrink-0" strokeWidth={1.75} />
              <span className="hidden sm:block">{label}</span>
            </NavLink>
          ))}
        </div>

        <div
          className="ml-auto flex items-center gap-2 px-2.5 py-1.5 rounded-md"
          title={!connected && error ? error : `${host}:${port}`}
        >
          {connected
            ? <Wifi className="w-4 h-4 text-green-500 shrink-0" strokeWidth={1.75} />
            : <WifiOff className="w-4 h-4 text-red-400 animate-pulse shrink-0" strokeWidth={1.75} />
          }
          <span className={`text-sm hidden sm:block ${connected ? 'text-zinc-300' : 'text-red-400'}`}>
            {connected ? 'Connected' : 'Disconnected'}
          </span>
          <span className="text-sm text-zinc-500 hidden md:block">{host}:{port}</span>
        </div>
      </div>
    </nav>
  );
}
