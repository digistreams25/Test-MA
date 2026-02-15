'use client';

import ClashList from '@/components/clash-list';
import { mockClashes } from '@/lib/mock-data';

export default function ClashesPage() {
  return (
    <div className="p-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-100">Clash Detection</h1>
        <p className="text-sm text-gray-500 mt-1">
          All detected clashes from Navisworks and ACC Model Coordination
        </p>
      </div>
      <ClashList clashes={mockClashes} />
    </div>
  );
}
