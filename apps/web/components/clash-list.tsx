'use client';

import { useState, useMemo } from 'react';
import Link from 'next/link';
import { clsx } from 'clsx';
import type { ClashData } from '@/lib/types';

interface ClashListProps {
  clashes: ClashData[];
}

type SortKey = 'severity' | 'station' | 'date' | 'id';
type FilterStatus = 'all' | 'new' | 'active' | 'reviewed' | 'resolved';
type FilterSeverity = 'all' | 'critical' | 'warning' | 'info';
type FilterSource = 'all' | 'navisworks' | 'acc';

const severityOrder = { critical: 0, warning: 1, info: 2 };

export default function ClashList({ clashes }: ClashListProps) {
  const [sortBy, setSortBy] = useState<SortKey>('severity');
  const [filterStatus, setFilterStatus] = useState<FilterStatus>('all');
  const [filterSeverity, setFilterSeverity] = useState<FilterSeverity>('all');
  const [filterSource, setFilterSource] = useState<FilterSource>('all');
  const [search, setSearch] = useState('');

  const filtered = useMemo(() => {
    let result = clashes;

    if (filterStatus !== 'all')
      result = result.filter((c) => c.status === filterStatus);
    if (filterSeverity !== 'all')
      result = result.filter((c) => c.severity === filterSeverity);
    if (filterSource !== 'all')
      result = result.filter((c) => c.source === filterSource);
    if (search) {
      const q = search.toLowerCase();
      result = result.filter(
        (c) =>
          c.id.toLowerCase().includes(q) ||
          c.element1.pipeName?.toLowerCase().includes(q) ||
          c.element2.pipeName?.toLowerCase().includes(q) ||
          c.gridLocation?.toLowerCase().includes(q)
      );
    }

    result = [...result].sort((a, b) => {
      switch (sortBy) {
        case 'severity':
          return severityOrder[a.severity] - severityOrder[b.severity];
        case 'station':
          return (a.element1.stationStart ?? 0) - (b.element1.stationStart ?? 0);
        case 'date':
          return (
            new Date(b.detectedAt).getTime() - new Date(a.detectedAt).getTime()
          );
        case 'id':
          return a.id.localeCompare(b.id);
        default:
          return 0;
      }
    });

    return result;
  }, [clashes, filterStatus, filterSeverity, filterSource, search, sortBy]);

  return (
    <div>
      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <input
          type="text"
          placeholder="Search clashes..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="input w-64 text-sm"
        />

        <select
          value={filterStatus}
          onChange={(e) => setFilterStatus(e.target.value as FilterStatus)}
          className="input text-sm"
        >
          <option value="all">All Status</option>
          <option value="new">New</option>
          <option value="active">Active</option>
          <option value="reviewed">Reviewed</option>
          <option value="resolved">Resolved</option>
        </select>

        <select
          value={filterSeverity}
          onChange={(e) => setFilterSeverity(e.target.value as FilterSeverity)}
          className="input text-sm"
        >
          <option value="all">All Severity</option>
          <option value="critical">Critical</option>
          <option value="warning">Warning</option>
          <option value="info">Info</option>
        </select>

        <select
          value={filterSource}
          onChange={(e) => setFilterSource(e.target.value as FilterSource)}
          className="input text-sm"
        >
          <option value="all">All Sources</option>
          <option value="navisworks">Navisworks</option>
          <option value="acc">ACC</option>
        </select>

        <select
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value as SortKey)}
          className="input text-sm"
        >
          <option value="severity">Sort: Severity</option>
          <option value="station">Sort: Station</option>
          <option value="date">Sort: Date</option>
          <option value="id">Sort: ID</option>
        </select>

        <div className="ml-auto text-xs text-gray-500">
          {filtered.length} of {clashes.length} clashes
        </div>
      </div>

      {/* Table */}
      <div className="card overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-surface-2 text-gray-400 text-xs uppercase tracking-wider">
              <th className="px-4 py-3 text-left">ID</th>
              <th className="px-4 py-3 text-left">Type</th>
              <th className="px-4 py-3 text-left">Severity</th>
              <th className="px-4 py-3 text-left">Status</th>
              <th className="px-4 py-3 text-left">Source</th>
              <th className="px-4 py-3 text-left">Grid</th>
              <th className="px-4 py-3 text-left">Element 1</th>
              <th className="px-4 py-3 text-left">Element 2</th>
              <th className="px-4 py-3 text-right">Distance</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-surface-3">
            {filtered.map((clash) => (
              <Link
                key={clash.id}
                href={`/dashboard/clashes/${clash.id}`}
                className="contents"
              >
                <tr className="hover:bg-surface-2 transition-colors cursor-pointer">
                  <td className="px-4 py-3 text-brand-gold font-medium">
                    {clash.id}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={
                        clash.type === 'hard' ? 'badge-critical' : 'badge-warning'
                      }
                    >
                      {clash.type}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={clsx(
                        'badge',
                        clash.severity === 'critical' && 'badge-critical',
                        clash.severity === 'warning' && 'badge-warning',
                        clash.severity === 'info' && 'badge-info'
                      )}
                    >
                      {clash.severity}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={clsx(
                        'badge',
                        clash.status === 'resolved' && 'badge-resolved',
                        clash.status === 'active' && 'badge-warning',
                        clash.status === 'new' && 'badge-info',
                        clash.status === 'reviewed' && 'badge-info'
                      )}
                    >
                      {clash.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-400 text-xs">
                    {clash.source === 'acc' ? 'ACC' : 'Navisworks'}
                  </td>
                  <td className="px-4 py-3 text-gray-300">
                    {clash.gridLocation}
                  </td>
                  <td className="px-4 py-3">
                    <div className="text-gray-300 text-xs">
                      {clash.element1.pipeName}
                    </div>
                    <div className="text-gray-500 text-[11px]">
                      {clash.element1.discipline}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="text-gray-300 text-xs">
                      {clash.element2.pipeName}
                    </div>
                    <div className="text-gray-500 text-[11px]">
                      {clash.element2.discipline}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <span
                      className={clsx(
                        'font-mono text-xs',
                        clash.distance < 0
                          ? 'text-clash-critical'
                          : 'text-clash-warning'
                      )}
                    >
                      {clash.distance < 0
                        ? `-${Math.abs(clash.distance * 1000).toFixed(0)}mm`
                        : `+${(clash.distance * 1000).toFixed(0)}mm`}
                    </span>
                  </td>
                </tr>
              </Link>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
