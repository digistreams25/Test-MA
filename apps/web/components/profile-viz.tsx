'use client';

import { useMemo } from 'react';
import type { ClashData, ResolutionOption } from '@/lib/types';

interface ProfileVizProps {
  clash: ClashData;
  selectedOption?: ResolutionOption | null;
}

export default function ProfileViz({ clash, selectedOption }: ProfileVizProps) {
  const svg = useMemo(() => {
    const W = 520;
    const H = 200;
    const padL = 60;
    const padR = 20;
    const padT = 30;
    const padB = 40;

    const e1 = clash.element1;
    const e2 = clash.element2;

    // Station range (use element 1 as reference)
    const stMin = (e1.stationStart ?? 2440) - 10;
    const stMax = (e1.stationEnd ?? 2460) + 10;
    const stRange = stMax - stMin;

    // Elevation range
    const allElevations = [
      e1.invertElevation ?? 183,
      e1.crownElevation ?? 184,
      e2.invertElevation ?? 183,
      e2.crownElevation ?? 184,
    ];
    const elMin = Math.min(...allElevations) - 0.5;
    const elMax = Math.max(...allElevations) + 1.5;
    const elRange = elMax - elMin;

    const scaleX = (st: number) => padL + ((st - stMin) / stRange) * (W - padL - padR);
    const scaleY = (el: number) => padT + ((elMax - el) / elRange) * (H - padT - padB);

    // Ground surface (approximate)
    const groundEl = (e1.invertElevation ?? 183) + (e1.cover ?? 1.5) + (e1.pipeDiameter ?? 600) / 1000;

    // Pipe 1 profile line (invert)
    const p1x1 = scaleX(e1.stationStart ?? stMin + 5);
    const p1x2 = scaleX(e1.stationEnd ?? stMax - 5);
    const p1y1 = scaleY(e1.invertElevation ?? 183);
    const slope1 = e1.slope ?? 0;
    const p1y2 = scaleY((e1.invertElevation ?? 183) - slope1 * ((e1.stationEnd ?? 0) - (e1.stationStart ?? 0)));

    // Crown
    const p1cy1 = scaleY(e1.crownElevation ?? 184);
    const p1cy2 = scaleY((e1.crownElevation ?? 184) - slope1 * ((e1.stationEnd ?? 0) - (e1.stationStart ?? 0)));

    // Pipe 2 profile (crossing)
    const crossStation = ((e1.stationStart ?? 2448) + (e1.stationEnd ?? 2452)) / 2;
    const p2cx = scaleX(crossStation);
    const p2InvY = scaleY(e2.invertElevation ?? 183.5);
    const p2CrownY = scaleY(e2.crownElevation ?? 184);

    // Apply offset to pipe 2 if option selected
    let p2OffsetPx = 0;
    if (selectedOption) {
      const elOffset = selectedOption.direction === 'lower'
        ? -selectedOption.offset
        : selectedOption.direction === 'raise'
        ? selectedOption.offset
        : 0;
      p2OffsetPx = (elOffset / elRange) * (H - padT - padB);
    }

    return {
      W, H, padL, padR, padT, padB,
      stMin, stMax, elMin, elMax,
      groundEl,
      scaleX, scaleY,
      p1x1, p1x2, p1y1, p1y2, p1cy1, p1cy2,
      p2cx, p2InvY, p2CrownY, p2OffsetPx,
      e1, e2,
      crossStation,
    };
  }, [clash, selectedOption]);

  // Grid lines
  const gridElevations: number[] = [];
  for (let e = Math.ceil(svg.elMin); e <= Math.floor(svg.elMax); e += 0.5) {
    gridElevations.push(e);
  }

  return (
    <div className="card p-4">
      <div className="text-xs text-gray-500 uppercase tracking-wider mb-3 font-medium">
        Profile View (Longitudinal)
      </div>
      <svg
        viewBox={`0 0 ${svg.W} ${svg.H}`}
        className="w-full h-auto"
        style={{ maxHeight: '180px' }}
      >
        {/* Grid */}
        {gridElevations.map((el) => (
          <g key={el}>
            <line
              x1={svg.padL}
              y1={svg.scaleY(el)}
              x2={svg.W - svg.padR}
              y2={svg.scaleY(el)}
              stroke="#1e1e2e"
              strokeWidth="0.5"
            />
            <text
              x={svg.padL - 5}
              y={svg.scaleY(el) + 3}
              fill="#555"
              fontSize="8"
              fontFamily="monospace"
              textAnchor="end"
            >
              {el.toFixed(1)}m
            </text>
          </g>
        ))}

        {/* Ground surface */}
        <line
          x1={svg.padL}
          y1={svg.scaleY(svg.groundEl)}
          x2={svg.W - svg.padR}
          y2={svg.scaleY(svg.groundEl)}
          stroke="#5a8a3a"
          strokeWidth="2"
        />
        <text
          x={svg.W - svg.padR}
          y={svg.scaleY(svg.groundEl) - 5}
          fill="#5a8a3a"
          fontSize="8"
          fontFamily="monospace"
          textAnchor="end"
        >
          Ground
        </text>

        {/* Pipe 1 (invert line) */}
        <line
          x1={svg.p1x1}
          y1={svg.p1y1}
          x2={svg.p1x2}
          y2={svg.p1y2}
          stroke="#3b82f6"
          strokeWidth="2"
        />
        {/* Pipe 1 (crown line) */}
        <line
          x1={svg.p1x1}
          y1={svg.p1cy1}
          x2={svg.p1x2}
          y2={svg.p1cy2}
          stroke="#3b82f6"
          strokeWidth="1"
          strokeDasharray="4,2"
          opacity="0.5"
        />
        {/* Pipe 1 fill */}
        <polygon
          points={`${svg.p1x1},${svg.p1y1} ${svg.p1x2},${svg.p1y2} ${svg.p1x2},${svg.p1cy2} ${svg.p1x1},${svg.p1cy1}`}
          fill="#3b82f6"
          opacity="0.08"
        />

        {/* Pipe 2 (crossing marker) */}
        <rect
          x={svg.p2cx - 12}
          y={svg.p2CrownY + svg.p2OffsetPx}
          width={24}
          height={svg.p2InvY - svg.p2CrownY}
          fill={selectedOption ? '#22c55e' : '#ef4444'}
          opacity="0.2"
          rx="2"
          className="transition-all duration-500"
        />
        <line
          x1={svg.p2cx - 12}
          y1={svg.p2InvY + svg.p2OffsetPx}
          x2={svg.p2cx + 12}
          y2={svg.p2InvY + svg.p2OffsetPx}
          stroke={selectedOption ? '#22c55e' : '#ef4444'}
          strokeWidth="2"
          className="transition-all duration-500"
        />
        <line
          x1={svg.p2cx - 12}
          y1={svg.p2CrownY + svg.p2OffsetPx}
          x2={svg.p2cx + 12}
          y2={svg.p2CrownY + svg.p2OffsetPx}
          stroke={selectedOption ? '#22c55e' : '#ef4444'}
          strokeWidth="1.5"
          className="transition-all duration-500"
        />

        {/* Ghost position when option selected */}
        {selectedOption && svg.p2OffsetPx !== 0 && (
          <rect
            x={svg.p2cx - 12}
            y={svg.p2CrownY}
            width={24}
            height={svg.p2InvY - svg.p2CrownY}
            fill="none"
            stroke="#ef4444"
            strokeWidth="1"
            strokeDasharray="3,3"
            opacity="0.3"
            rx="2"
          />
        )}

        {/* Clash point */}
        {!selectedOption && (
          <circle
            cx={svg.p2cx}
            cy={(svg.p2InvY + svg.p2CrownY) / 2}
            r="5"
            fill="#ef4444"
            opacity="0.8"
          >
            <animate
              attributeName="r"
              values="4;7;4"
              dur="2s"
              repeatCount="indefinite"
            />
            <animate
              attributeName="opacity"
              values="0.8;0.3;0.8"
              dur="2s"
              repeatCount="indefinite"
            />
          </circle>
        )}

        {/* Labels */}
        <text
          x={svg.p1x1}
          y={svg.p1y1 + 14}
          fill="#3b82f6"
          fontSize="8"
          fontFamily="monospace"
        >
          {svg.e1.pipeName}
        </text>
        <text
          x={svg.p2cx}
          y={svg.p2InvY + svg.p2OffsetPx + 14}
          fill={selectedOption ? '#22c55e' : '#ef4444'}
          fontSize="8"
          fontFamily="monospace"
          textAnchor="middle"
          className="transition-all duration-500"
        >
          {svg.e2.pipeName}
        </text>

        {/* Station axis */}
        <line
          x1={svg.padL}
          y1={svg.H - svg.padB + 10}
          x2={svg.W - svg.padR}
          y2={svg.H - svg.padB + 10}
          stroke="#333"
          strokeWidth="1"
        />
        <text
          x={svg.scaleX(svg.e1.stationStart ?? svg.stMin + 5)}
          y={svg.H - svg.padB + 24}
          fill="#555"
          fontSize="8"
          fontFamily="monospace"
          textAnchor="middle"
        >
          STA {(svg.e1.stationStart ?? 0).toFixed(1)}
        </text>
        <text
          x={svg.scaleX(svg.e1.stationEnd ?? svg.stMax - 5)}
          y={svg.H - svg.padB + 24}
          fill="#555"
          fontSize="8"
          fontFamily="monospace"
          textAnchor="middle"
        >
          STA {(svg.e1.stationEnd ?? 0).toFixed(1)}
        </text>

        {/* Y-axis label */}
        <text
          x="10"
          y={svg.padT}
          fill="#555"
          fontSize="8"
          fontFamily="monospace"
          transform={`rotate(-90, 10, ${svg.padT + 30})`}
        >
          Elevation (m)
        </text>
      </svg>
    </div>
  );
}
