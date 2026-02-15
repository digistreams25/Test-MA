'use client';

import Link from 'next/link';
import { mockClashes, mockAuditEntries, mockConnection } from '@/lib/mock-data';

function StatCard({
  label,
  value,
  color,
}: {
  label: string;
  value: number;
  color: string;
}) {
  return (
    <div className="card p-5">
      <div className="text-sm text-gray-400 mb-1">{label}</div>
      <div className={`text-3xl font-bold ${color}`}>{value}</div>
    </div>
  );
}

export default function DashboardPage() {
  const total = mockClashes.length;
  const critical = mockClashes.filter((c) => c.severity === 'critical').length;
  const warnings = mockClashes.filter((c) => c.severity === 'warning').length;
  const resolved = mockClashes.filter((c) => c.status === 'resolved').length;
  const active = mockClashes.filter(
    (c) => c.status === 'active' || c.status === 'new'
  ).length;

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-100">Project Dashboard</h1>
        <p className="text-sm text-gray-500 mt-1">
          Highway 401 Widening — Phase 2 &middot; Toronto, ON
        </p>
      </div>

      <div className="grid grid-cols-4 gap-4 mb-8">
        <StatCard label="Total Clashes" value={total} color="text-gray-100" />
        <StatCard
          label="Critical"
          value={critical}
          color="text-clash-critical"
        />
        <StatCard label="Warnings" value={warnings} color="text-clash-warning" />
        <StatCard
          label="Resolved"
          value={resolved}
          color="text-clash-resolved"
        />
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Active clashes summary */}
        <div className="card p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider">
              Active Clashes
            </h2>
            <Link
              href="/dashboard/clashes"
              className="text-xs text-brand-gold hover:text-brand-gold-light"
            >
              View All
            </Link>
          </div>
          <div className="space-y-3">
            {mockClashes
              .filter((c) => c.status !== 'resolved')
              .map((clash) => (
                <Link
                  key={clash.id}
                  href={`/dashboard/clashes/${clash.id}`}
                  className="flex items-center justify-between p-3 rounded-md bg-surface-2 hover:bg-surface-3 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <span
                      className={
                        clash.severity === 'critical'
                          ? 'badge-critical'
                          : 'badge-warning'
                      }
                    >
                      {clash.type}
                    </span>
                    <div>
                      <div className="text-sm text-gray-200">{clash.id}</div>
                      <div className="text-xs text-gray-500">
                        {clash.element1.pipeName} vs {clash.element2.pipeName}
                      </div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-xs text-gray-400">
                      {clash.gridLocation}
                    </div>
                    <div className="text-xs text-gray-500">
                      {clash.distance < 0
                        ? `${Math.abs(clash.distance * 1000).toFixed(0)}mm penetration`
                        : `${(clash.distance * 1000).toFixed(0)}mm gap`}
                    </div>
                  </div>
                </Link>
              ))}
          </div>
        </div>

        {/* Recent activity */}
        <div className="card p-6">
          <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider mb-4">
            Recent Activity
          </h2>
          <div className="space-y-3">
            {mockAuditEntries.map((entry) => (
              <div
                key={entry.id}
                className="p-3 rounded-md bg-surface-2 border-l-2 border-clash-resolved"
              >
                <div className="flex items-center justify-between">
                  <div className="text-sm text-gray-200">
                    {entry.optionApplied}
                  </div>
                  <div className="text-xs text-gray-500">
                    {new Date(entry.timestamp).toLocaleDateString()}
                  </div>
                </div>
                <div className="text-xs text-gray-400 mt-1">
                  Clash {entry.clashId} &middot; {entry.elementsModified.length}{' '}
                  elements modified
                </div>
                <div className="text-xs text-gray-500 mt-0.5">
                  Approved by {entry.approvedBy}
                </div>
              </div>
            ))}

            <div className="p-3 rounded-md bg-surface-2 border-l-2 border-clash-warning">
              <div className="flex items-center justify-between">
                <div className="text-sm text-gray-200">
                  New clash detected
                </div>
                <div className="text-xs text-gray-500">Dec 3, 2025</div>
              </div>
              <div className="text-xs text-gray-400 mt-1">
                CLH-004 — Clearance issue at H-17
              </div>
              <div className="text-xs text-gray-500 mt-0.5">
                Source: ACC Model Coordination
              </div>
            </div>

            <div className="p-3 rounded-md bg-surface-2 border-l-2 border-clash-critical">
              <div className="flex items-center justify-between">
                <div className="text-sm text-gray-200">
                  Clash requires attention
                </div>
                <div className="text-xs text-gray-500">Dec 1, 2025</div>
              </div>
              <div className="text-xs text-gray-400 mt-1">
                CLH-001 — Hard clash at G-14, 45mm penetration
              </div>
              <div className="text-xs text-gray-500 mt-0.5">
                Source: Navisworks Clash Detective
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Source breakdown */}
      <div className="card p-6 mt-6">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider mb-4">
          Clashes by Source
        </h2>
        <div className="grid grid-cols-2 gap-4">
          <div className="p-4 rounded-md bg-surface-2">
            <div className="flex items-center gap-2 mb-2">
              <span className="w-3 h-3 rounded bg-clash-info" />
              <span className="text-sm text-gray-300">Navisworks</span>
            </div>
            <div className="text-2xl font-bold text-gray-100">
              {mockClashes.filter((c) => c.source === 'navisworks').length}
            </div>
            <div className="text-xs text-gray-500 mt-1">
              Local Clash Detective
            </div>
          </div>
          <div className="p-4 rounded-md bg-surface-2">
            <div className="flex items-center gap-2 mb-2">
              <span className="w-3 h-3 rounded bg-brand-gold" />
              <span className="text-sm text-gray-300">ACC</span>
            </div>
            <div className="text-2xl font-bold text-gray-100">
              {mockClashes.filter((c) => c.source === 'acc').length}
            </div>
            <div className="text-xs text-gray-500 mt-1">
              Model Coordination
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
