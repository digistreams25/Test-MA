'use client';

import { useState } from 'react';
import AuditTable from '@/components/audit-table';
import { mockAuditEntries } from '@/lib/mock-data';

export default function AuditPage() {
  const [search, setSearch] = useState('');

  const filtered = mockAuditEntries.filter(
    (e) =>
      !search ||
      e.clashId.toLowerCase().includes(search.toLowerCase()) ||
      e.proposalId.toLowerCase().includes(search.toLowerCase()) ||
      e.approvedBy.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="p-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-100">Audit Log</h1>
          <p className="text-sm text-gray-500 mt-1">
            All model modifications — ISO 19650 compliant audit trail
          </p>
        </div>
        <div className="flex gap-2">
          <button className="btn-secondary text-sm">Export CSV</button>
          <button className="btn-secondary text-sm">Export PDF</button>
        </div>
      </div>

      <div className="mb-4">
        <input
          type="text"
          placeholder="Search by clash ID, proposal, or user..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="input w-80 text-sm"
        />
      </div>

      <AuditTable entries={filtered} />
    </div>
  );
}
