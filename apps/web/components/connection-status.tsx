'use client';

import { useState } from 'react';
import { mockConnection } from '@/lib/mock-data';

function Dot({ active }: { active: boolean }) {
  return (
    <span
      className={`inline-block w-2 h-2 rounded-full ${
        active ? 'bg-clash-resolved animate-pulse' : 'bg-clash-critical'
      }`}
    />
  );
}

export default function ConnectionStatus() {
  const [conn] = useState(mockConnection);

  const items = [
    { label: 'Agent', active: conn.agent },
    { label: 'Navisworks', active: conn.navisworks },
    { label: 'Civil 3D', active: conn.civil3d },
    { label: 'ACC', active: conn.acc },
  ];

  return (
    <div className="space-y-2">
      <div className="text-[11px] text-gray-500 uppercase tracking-wider font-medium">
        Connections
      </div>
      {items.map((item) => (
        <div key={item.label} className="flex items-center gap-2 text-xs">
          <Dot active={item.active} />
          <span className={item.active ? 'text-gray-300' : 'text-gray-500'}>
            {item.label}
          </span>
        </div>
      ))}
    </div>
  );
}
