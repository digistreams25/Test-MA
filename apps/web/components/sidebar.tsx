'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { clsx } from 'clsx';
import ConnectionStatus from './connection-status';

const navItems = [
  { href: '/dashboard', label: 'Overview', icon: '◫' },
  { href: '/dashboard/clashes', label: 'Clashes', icon: '⚡' },
  { href: '/dashboard/chat', label: 'Agent Chat', icon: '◈' },
  { href: '/dashboard/audit', label: 'Audit Log', icon: '◩' },
  { href: '/dashboard/settings', label: 'Settings', icon: '⚙' },
];

export default function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="w-60 bg-surface-1 border-r border-surface-3 flex flex-col h-screen sticky top-0">
      <div className="px-5 py-5 border-b border-surface-3">
        <h1 className="text-xl font-bold text-brand-gold tracking-tight">
          TwinFlux
        </h1>
        <p className="text-[11px] text-gray-500 mt-0.5">Clash Resolver</p>
      </div>

      <nav className="flex-1 px-3 py-4 space-y-1">
        {navItems.map((item) => {
          const isActive =
            pathname === item.href ||
            (item.href !== '/dashboard' && pathname.startsWith(item.href));
          return (
            <Link
              key={item.href}
              href={item.href}
              className={clsx(
                'flex items-center gap-3 px-3 py-2.5 rounded-md text-sm transition-colors',
                isActive
                  ? 'bg-brand-gold/10 text-brand-gold border border-brand-gold/20'
                  : 'text-gray-400 hover:text-gray-200 hover:bg-surface-2'
              )}
            >
              <span className="text-base">{item.icon}</span>
              {item.label}
            </Link>
          );
        })}
      </nav>

      <div className="px-4 py-4 border-t border-surface-3">
        <ConnectionStatus />
      </div>

      <div className="px-4 py-3 border-t border-surface-3">
        <div className="text-xs text-gray-500">
          Highway 401 Widening — Phase 2
        </div>
        <div className="text-[11px] text-gray-600 mt-0.5">Toronto, ON</div>
      </div>
    </aside>
  );
}
