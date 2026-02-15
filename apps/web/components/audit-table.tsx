'use client';

import type { AuditEntry } from '@/lib/types';

interface AuditTableProps {
  entries: AuditEntry[];
}

export default function AuditTable({ entries }: AuditTableProps) {
  return (
    <div className="card overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="bg-surface-2 text-gray-400 text-xs uppercase tracking-wider">
            <th className="px-4 py-3 text-left">Date</th>
            <th className="px-4 py-3 text-left">Clash ID</th>
            <th className="px-4 py-3 text-left">Proposal</th>
            <th className="px-4 py-3 text-left">Option Applied</th>
            <th className="px-4 py-3 text-left">Approved By</th>
            <th className="px-4 py-3 text-left">Elements Modified</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-surface-3">
          {entries.map((entry) => (
            <tr key={entry.id} className="hover:bg-surface-2 transition-colors">
              <td className="px-4 py-3 text-gray-400 font-mono text-xs">
                {new Date(entry.timestamp).toLocaleString()}
              </td>
              <td className="px-4 py-3 text-brand-gold font-medium">
                {entry.clashId}
              </td>
              <td className="px-4 py-3 text-gray-400 text-xs">
                {entry.proposalId}
              </td>
              <td className="px-4 py-3 text-gray-300">{entry.optionApplied}</td>
              <td className="px-4 py-3 text-gray-400 text-xs">
                {entry.approvedBy}
              </td>
              <td className="px-4 py-3">
                <div className="space-y-1">
                  {entry.elementsModified.map((mod, i) => (
                    <div key={i} className="text-xs">
                      <span className="text-gray-400">{mod.property}: </span>
                      <span className="text-clash-critical font-mono">
                        {mod.oldValue}
                      </span>
                      <span className="text-gray-500"> → </span>
                      <span className="text-clash-resolved font-mono">
                        {mod.newValue}
                      </span>
                      <span className="text-gray-600 ml-1">{mod.unit}</span>
                    </div>
                  ))}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {entries.length === 0 && (
        <div className="p-8 text-center text-gray-500 text-sm">
          No modifications recorded yet
        </div>
      )}
    </div>
  );
}
