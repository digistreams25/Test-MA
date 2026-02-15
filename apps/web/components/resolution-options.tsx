'use client';

import { clsx } from 'clsx';
import type { ResolutionOption, RiskLevel } from '@/lib/types';

interface ResolutionOptionsProps {
  options: ResolutionOption[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}

function RiskBadge({ risk }: { risk: RiskLevel }) {
  return (
    <span
      className={clsx(
        'badge',
        risk === 'low' && 'bg-clash-resolved/20 text-clash-resolved border border-clash-resolved/30',
        risk === 'medium' && 'bg-clash-warning/20 text-clash-warning border border-clash-warning/30',
        risk === 'high' && 'bg-clash-critical/20 text-clash-critical border border-clash-critical/30'
      )}
    >
      {risk.toUpperCase()}
    </span>
  );
}

function DirectionIcon({ direction }: { direction: string }) {
  switch (direction) {
    case 'lower':
      return <span className="text-lg">↓</span>;
    case 'raise':
      return <span className="text-lg">↑</span>;
    case 'horizontal':
      return <span className="text-lg">→</span>;
    default:
      return null;
  }
}

export default function ResolutionOptions({
  options,
  selectedId,
  onSelect,
}: ResolutionOptionsProps) {
  return (
    <div className="space-y-3">
      <div className="text-xs text-gray-500 uppercase tracking-wider font-medium">
        Resolution Options
      </div>
      {options.map((opt) => {
        const isSelected = selectedId === opt.id;
        return (
          <button
            key={opt.id}
            onClick={() => onSelect(opt.id)}
            className={clsx(
              'w-full text-left p-4 rounded-lg border transition-all',
              isSelected
                ? 'bg-brand-gold/10 border-brand-gold/40 ring-1 ring-brand-gold/20'
                : 'bg-surface-2 border-surface-3 hover:border-surface-4'
            )}
          >
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <DirectionIcon direction={opt.direction} />
                <span className="text-sm font-medium text-gray-200">
                  {opt.label}
                </span>
              </div>
              <RiskBadge risk={opt.risk} />
            </div>

            <p className="text-xs text-gray-400 mb-3">{opt.description}</p>

            <div className="grid grid-cols-3 gap-2 text-xs">
              <div>
                <div className="text-gray-500">Cover</div>
                <div className="text-gray-300 font-mono">
                  {opt.resultingCover.toFixed(3)}m
                </div>
              </div>
              <div>
                <div className="text-gray-500">Clearance</div>
                <div className="text-gray-300 font-mono">
                  {opt.resultingClearance.toFixed(3)}m
                </div>
              </div>
              <div>
                <div className="text-gray-500">Offset</div>
                <div className="text-gray-300 font-mono">
                  {(opt.offset * 1000).toFixed(0)}mm
                </div>
              </div>
            </div>

            {/* Constraint checks */}
            <div className="mt-3 space-y-1">
              {opt.constraints.map((c, i) => (
                <div
                  key={i}
                  className="flex items-center justify-between text-[11px]"
                >
                  <span className="text-gray-500">{c.name}</span>
                  <div className="flex items-center gap-2">
                    <span className="text-gray-400 font-mono">
                      {c.actual} / {c.threshold}
                    </span>
                    <span
                      className={
                        c.passed ? 'text-clash-resolved' : 'text-clash-critical'
                      }
                    >
                      {c.passed ? '✓' : '✗'}
                    </span>
                  </div>
                </div>
              ))}
            </div>

            {/* Warnings */}
            {opt.warnings.length > 0 && (
              <div className="mt-2 space-y-1">
                {opt.warnings.map((w, i) => (
                  <div
                    key={i}
                    className="text-[11px] text-clash-warning flex items-start gap-1"
                  >
                    <span>⚠</span>
                    <span>{w}</span>
                  </div>
                ))}
              </div>
            )}

            {opt.downstreamImpactCount > 0 && (
              <div className="mt-2 text-[11px] text-gray-500">
                Downstream impact: {opt.downstreamImpactCount} elements
              </div>
            )}
          </button>
        );
      })}
    </div>
  );
}
