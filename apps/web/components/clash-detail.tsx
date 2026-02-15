'use client';

import type { ClashElement } from '@/lib/types';
import { clsx } from 'clsx';

interface ClashDetailProps {
  element: ClashElement;
  label: string;
  color: string;
}

function PropRow({ name, value, unit }: { name: string; value?: string | number | null; unit?: string }) {
  if (value === undefined || value === null) return null;
  return (
    <div className="flex items-center justify-between py-1.5 border-b border-surface-3 last:border-0">
      <span className="text-xs text-gray-500">{name}</span>
      <span className="text-xs text-gray-300 font-mono">
        {typeof value === 'number' ? value.toFixed(3) : value}
        {unit && <span className="text-gray-500 ml-1">{unit}</span>}
      </span>
    </div>
  );
}

export default function ClashDetailPanel({ element, label, color }: ClashDetailProps) {
  return (
    <div className="card p-4">
      <div className="flex items-center gap-2 mb-3">
        <span className={clsx('w-3 h-3 rounded-full', color)} />
        <span className="text-xs text-gray-400 uppercase tracking-wider font-medium">
          {label}
        </span>
      </div>

      <div className="text-sm font-medium text-gray-200 mb-1">
        {element.pipeName || element.elementId}
      </div>
      <div className="text-xs text-gray-500 mb-3">
        {element.discipline} &middot; {element.category}
      </div>

      <div className="space-y-0">
        <PropRow name="Element ID" value={element.elementId} />
        <PropRow name="Model File" value={element.modelFile} />
        <PropRow name="Network" value={element.pipeNetworkName} />
        <PropRow name="Network Type" value={element.networkType} />
        <PropRow name="Diameter" value={element.pipeDiameter} unit="mm" />
        <PropRow name="Material" value={element.material} />
        <PropRow name="Alignment" value={element.alignmentName} />
        <PropRow name="Station Start" value={element.stationStart} unit="m" />
        <PropRow name="Station End" value={element.stationEnd} unit="m" />
        <PropRow name="Invert Elev." value={element.invertElevation} unit="m" />
        <PropRow name="Crown Elev." value={element.crownElevation} unit="m" />
        <PropRow
          name="Slope"
          value={element.slope != null ? `${(element.slope * 100).toFixed(2)}%` : undefined}
        />
        <PropRow name="Cover" value={element.cover} unit="m" />
      </div>
    </div>
  );
}
