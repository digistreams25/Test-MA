'use client';

import { useMemo } from 'react';
import type { ClashData, ResolutionOption } from '@/lib/types';

interface CrossSectionVizProps {
  clash: ClashData;
  selectedOption?: ResolutionOption | null;
}

export default function CrossSectionViz({
  clash,
  selectedOption,
}: CrossSectionVizProps) {
  const svg = useMemo(() => {
    const W = 520;
    const H = 320;
    const cx = W / 2;

    // Ground surface
    const groundY = 80;

    // Scale: 1m = 80px vertical
    const scale = 80;

    const e1 = clash.element1;
    const e2 = clash.element2;

    const r1 = ((e1.pipeDiameter ?? 300) / 1000 / 2) * scale;
    const r2 = ((e2.pipeDiameter ?? 200) / 1000 / 2) * scale;

    // Pipe center Y positions (from ground)
    const pipe1CoverPx = (e1.cover ?? 1.5) * scale;
    const pipe2CoverPx = (e2.cover ?? 1.2) * scale;

    let pipe1Y = groundY + pipe1CoverPx + r1;
    let pipe2Y = groundY + pipe2CoverPx + r2;

    // Horizontal positions
    let pipe1X = cx - 30;
    let pipe2X = cx + 30;

    // Apply resolution offset preview
    let offsetY = 0;
    let offsetX = 0;
    if (selectedOption) {
      const offsetPx = selectedOption.offset * scale;
      if (selectedOption.direction === 'lower') offsetY = offsetPx;
      else if (selectedOption.direction === 'raise') offsetY = -offsetPx;
      else if (selectedOption.direction === 'horizontal') offsetX = offsetPx;

      // Apply to target element
      const isTarget =
        selectedOption.targetElement.elementId === e2.elementId;
      if (isTarget) {
        pipe2Y += offsetY;
        pipe2X += offsetX;
      } else {
        pipe1Y += offsetY;
        pipe1X += offsetX;
      }
    }

    // Clash indicator
    const clashDist = Math.abs(clash.distance * 1000);
    const hasClash = !selectedOption;

    return { W, H, groundY, pipe1X, pipe1Y, pipe2X, pipe2Y, r1, r2, e1, e2, hasClash, clashDist, scale };
  }, [clash, selectedOption]);

  return (
    <div className="card p-4">
      <div className="text-xs text-gray-500 uppercase tracking-wider mb-3 font-medium">
        Cross Section
      </div>
      <svg
        viewBox={`0 0 ${svg.W} ${svg.H}`}
        className="w-full h-auto"
        style={{ maxHeight: '280px' }}
      >
        {/* Background */}
        <defs>
          <pattern id="soil" patternUnits="userSpaceOnUse" width="8" height="8">
            <circle cx="2" cy="2" r="0.5" fill="#4a3f2f" opacity="0.4" />
            <circle cx="6" cy="6" r="0.5" fill="#4a3f2f" opacity="0.3" />
          </pattern>
          <linearGradient id="ground" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#3d3424" />
            <stop offset="100%" stopColor="#2a231a" />
          </linearGradient>
        </defs>

        {/* Sky */}
        <rect x="0" y="0" width={svg.W} height={svg.groundY} fill="#0f1520" />

        {/* Ground fill */}
        <rect
          x="0"
          y={svg.groundY}
          width={svg.W}
          height={svg.H - svg.groundY}
          fill="url(#ground)"
        />
        <rect
          x="0"
          y={svg.groundY}
          width={svg.W}
          height={svg.H - svg.groundY}
          fill="url(#soil)"
        />

        {/* Ground surface line */}
        <line
          x1="0"
          y1={svg.groundY}
          x2={svg.W}
          y2={svg.groundY}
          stroke="#5a8a3a"
          strokeWidth="3"
        />

        {/* Ground label */}
        <text x="10" y={svg.groundY - 8} fill="#5a8a3a" fontSize="10" fontFamily="monospace">
          GROUND SURFACE
        </text>

        {/* Cover dimension lines */}
        {/* Pipe 1 cover */}
        <line
          x1={svg.pipe1X - svg.r1 - 25}
          y1={svg.groundY}
          x2={svg.pipe1X - svg.r1 - 25}
          y2={svg.pipe1Y - svg.r1}
          stroke="#666"
          strokeWidth="1"
          strokeDasharray="3,3"
        />
        <text
          x={svg.pipe1X - svg.r1 - 28}
          y={(svg.groundY + svg.pipe1Y - svg.r1) / 2}
          fill="#999"
          fontSize="9"
          fontFamily="monospace"
          textAnchor="end"
        >
          {((svg.pipe1Y - svg.r1 - svg.groundY) / svg.scale).toFixed(2)}m
        </text>

        {/* Pipe 1 */}
        <circle
          cx={svg.pipe1X}
          cy={svg.pipe1Y}
          r={svg.r1}
          fill="none"
          stroke="#3b82f6"
          strokeWidth="2.5"
          className="transition-all duration-500"
        />
        <circle
          cx={svg.pipe1X}
          cy={svg.pipe1Y}
          r={svg.r1}
          fill="#3b82f6"
          opacity="0.1"
          className="transition-all duration-500"
        />

        {/* Pipe 2 */}
        <circle
          cx={svg.pipe2X}
          cy={svg.pipe2Y}
          r={svg.r2}
          fill="none"
          stroke={selectedOption ? '#22c55e' : '#ef4444'}
          strokeWidth="2.5"
          className="transition-all duration-500"
        />
        <circle
          cx={svg.pipe2X}
          cy={svg.pipe2Y}
          r={svg.r2}
          fill={selectedOption ? '#22c55e' : '#ef4444'}
          opacity="0.1"
          className="transition-all duration-500"
        />

        {/* Ghost position (original) when option selected */}
        {selectedOption && (
          <circle
            cx={svg.pipe2X - (selectedOption.direction === 'horizontal' ? selectedOption.offset * svg.scale : 0)}
            cy={
              svg.pipe2Y -
              (selectedOption.direction === 'lower'
                ? selectedOption.offset * svg.scale
                : selectedOption.direction === 'raise'
                ? -selectedOption.offset * svg.scale
                : 0)
            }
            r={svg.r2}
            fill="none"
            stroke="#ef4444"
            strokeWidth="1"
            strokeDasharray="4,4"
            opacity="0.4"
          />
        )}

        {/* Clash indicator */}
        {svg.hasClash && (
          <>
            <line
              x1={svg.pipe1X}
              y1={svg.pipe1Y}
              x2={svg.pipe2X}
              y2={svg.pipe2Y}
              stroke="#ef4444"
              strokeWidth="1.5"
              strokeDasharray="4,4"
            />
            <text
              x={(svg.pipe1X + svg.pipe2X) / 2 + 15}
              y={(svg.pipe1Y + svg.pipe2Y) / 2 - 5}
              fill="#ef4444"
              fontSize="10"
              fontFamily="monospace"
              fontWeight="bold"
            >
              {svg.clashDist.toFixed(0)}mm
            </text>
          </>
        )}

        {/* Resolution arrow */}
        {selectedOption && (
          <line
            x1={svg.pipe2X}
            y1={
              svg.pipe2Y -
              (selectedOption.direction === 'lower'
                ? selectedOption.offset * svg.scale
                : selectedOption.direction === 'raise'
                ? -selectedOption.offset * svg.scale
                : 0)
            }
            x2={svg.pipe2X}
            y2={svg.pipe2Y}
            stroke="#22c55e"
            strokeWidth="2"
            markerEnd="url(#arrowhead)"
            className="transition-all duration-500"
          />
        )}
        <defs>
          <marker
            id="arrowhead"
            markerWidth="8"
            markerHeight="6"
            refX="8"
            refY="3"
            orient="auto"
          >
            <polygon points="0 0, 8 3, 0 6" fill="#22c55e" />
          </marker>
        </defs>

        {/* Labels */}
        <text
          x={svg.pipe1X}
          y={svg.pipe1Y + svg.r1 + 16}
          fill="#3b82f6"
          fontSize="9"
          fontFamily="monospace"
          textAnchor="middle"
        >
          {svg.e1.pipeName}
        </text>
        <text
          x={svg.pipe1X}
          y={svg.pipe1Y + svg.r1 + 28}
          fill="#666"
          fontSize="8"
          fontFamily="monospace"
          textAnchor="middle"
        >
          {svg.e1.pipeDiameter}mm {svg.e1.networkType}
        </text>

        <text
          x={svg.pipe2X}
          y={svg.pipe2Y + svg.r2 + 16}
          fill={selectedOption ? '#22c55e' : '#ef4444'}
          fontSize="9"
          fontFamily="monospace"
          textAnchor="middle"
          className="transition-all duration-500"
        >
          {svg.e2.pipeName}
        </text>
        <text
          x={svg.pipe2X}
          y={svg.pipe2Y + svg.r2 + 28}
          fill="#666"
          fontSize="8"
          fontFamily="monospace"
          textAnchor="middle"
        >
          {svg.e2.pipeDiameter}mm {svg.e2.networkType}
        </text>
      </svg>
    </div>
  );
}
