'use client';

import { useState, useMemo } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { mockClashes, generateOptions } from '@/lib/mock-data';
import ClashDetailPanel from '@/components/clash-detail';
import ResolutionOptions from '@/components/resolution-options';
import ConsentBar from '@/components/consent-bar';
import CrossSectionViz from '@/components/cross-section-viz';
import ProfileViz from '@/components/profile-viz';
import type { ConsentState, ResolutionOption } from '@/lib/types';

export default function ClashDetailPage() {
  const params = useParams();
  const clashId = params.id as string;

  const clash = mockClashes.find((c) => c.id === clashId);
  const options = useMemo(
    () => (clash ? generateOptions(clash) : []),
    [clash]
  );

  const [selectedOptionId, setSelectedOptionId] = useState<string | null>(null);
  const [consentState, setConsentState] = useState<ConsentState>('options_ready');

  const selectedOption = useMemo(
    () => options.find((o) => o.id === selectedOptionId) ?? null,
    [options, selectedOptionId]
  );

  if (!clash) {
    return (
      <div className="p-8">
        <div className="card p-8 text-center">
          <p className="text-gray-400">Clash not found: {clashId}</p>
          <Link
            href="/dashboard/clashes"
            className="text-brand-gold text-sm mt-2 inline-block"
          >
            Back to clash list
          </Link>
        </div>
      </div>
    );
  }

  const handleSelect = (id: string) => {
    setSelectedOptionId(id);
    setConsentState('option_selected');
  };

  const handleApprove = () => setConsentState('approved');

  const handleExecute = () => {
    setConsentState('executing');
    // Simulate execution
    setTimeout(() => setConsentState('executed'), 2500);
  };

  const handleRevoke = () => {
    setConsentState('options_ready');
    setSelectedOptionId(null);
  };

  const handleValidate = () => {
    setConsentState('validating');
    // Simulate validation
    setTimeout(() => setConsentState('resolved'), 2000);
  };

  return (
    <div className="p-8">
      {/* Header */}
      <div className="flex items-center gap-4 mb-6">
        <Link
          href="/dashboard/clashes"
          className="text-gray-500 hover:text-gray-300 transition-colors"
        >
          ← Back
        </Link>
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-100">{clash.id}</h1>
            <span
              className={
                clash.severity === 'critical' ? 'badge-critical' : 'badge-warning'
              }
            >
              {clash.severity}
            </span>
            <span
              className={clash.type === 'hard' ? 'badge-critical' : 'badge-warning'}
            >
              {clash.type}
            </span>
            <span className="badge bg-surface-3 text-gray-400 border border-surface-4">
              {clash.source === 'acc' ? 'ACC' : 'Navisworks'}
            </span>
          </div>
          <p className="text-sm text-gray-500 mt-1">
            {clash.testName} &middot; Grid {clash.gridLocation} &middot;{' '}
            {clash.distance < 0
              ? `${Math.abs(clash.distance * 1000).toFixed(0)}mm penetration`
              : `${(clash.distance * 1000).toFixed(0)}mm gap`}
          </p>
        </div>
      </div>

      {/* Consent bar */}
      <div className="mb-6">
        <ConsentBar
          state={consentState}
          selectedOptionLabel={selectedOption?.label ?? null}
          onApprove={handleApprove}
          onExecute={handleExecute}
          onRevoke={handleRevoke}
          onValidate={handleValidate}
        />
      </div>

      {/* Main content — two column */}
      <div className="grid grid-cols-2 gap-6">
        {/* Left: Element details + Visualizations */}
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <ClashDetailPanel
              element={clash.element1}
              label="Element 1"
              color="bg-blue-500"
            />
            <ClashDetailPanel
              element={clash.element2}
              label="Element 2"
              color="bg-red-500"
            />
          </div>

          <CrossSectionViz clash={clash} selectedOption={selectedOption} />
          <ProfileViz clash={clash} selectedOption={selectedOption} />
        </div>

        {/* Right: Resolution options */}
        <div>
          <ResolutionOptions
            options={options}
            selectedId={selectedOptionId}
            onSelect={handleSelect}
          />
        </div>
      </div>
    </div>
  );
}
