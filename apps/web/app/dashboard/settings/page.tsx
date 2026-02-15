'use client';

import { useState } from 'react';
import { mockProject, mockConnection } from '@/lib/mock-data';

export default function SettingsPage() {
  const [source, setSource] = useState<'navisworks' | 'acc'>(
    mockProject.clashSource
  );
  const [constraints, setConstraints] = useState(mockProject.constraints);
  const [accConnected, setAccConnected] = useState(mockConnection.acc);
  const [accProjectName, setAccProjectName] = useState('');

  const updateConstraint = (key: keyof typeof constraints, value: string) => {
    const num = parseFloat(value);
    if (!isNaN(num)) {
      setConstraints((prev) => ({ ...prev, [key]: num }));
    }
  };

  return (
    <div className="p-8 max-w-3xl">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-100">Project Settings</h1>
        <p className="text-sm text-gray-500 mt-1">
          Configure clash source, thresholds, and integrations
        </p>
      </div>

      {/* Project Info */}
      <div className="card p-6 mb-6">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider mb-4">
          Project Information
        </h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs text-gray-500 mb-1">
              Project Name
            </label>
            <input
              type="text"
              defaultValue={mockProject.name}
              className="input w-full text-sm"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Location</label>
            <input
              type="text"
              defaultValue={mockProject.location}
              className="input w-full text-sm"
            />
          </div>
        </div>
      </div>

      {/* Clash Source Selection */}
      <div className="card p-6 mb-6">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider mb-4">
          Clash Source
        </h2>
        <div className="grid grid-cols-2 gap-4">
          <button
            onClick={() => setSource('navisworks')}
            className={`p-4 rounded-lg border text-left transition-all ${
              source === 'navisworks'
                ? 'border-brand-gold/40 bg-brand-gold/10'
                : 'border-surface-3 bg-surface-2 hover:border-surface-4'
            }`}
          >
            <div className="text-sm font-medium text-gray-200 mb-1">
              Navisworks
            </div>
            <div className="text-xs text-gray-500">
              Local Clash Detective via COM automation. Requires local agent on same machine.
            </div>
            <div className="mt-2 flex items-center gap-2 text-xs">
              <span
                className={`w-2 h-2 rounded-full ${
                  mockConnection.navisworks
                    ? 'bg-clash-resolved'
                    : 'bg-clash-critical'
                }`}
              />
              <span className="text-gray-400">
                {mockConnection.navisworks ? 'Connected' : 'Disconnected'}
              </span>
            </div>
          </button>

          <button
            onClick={() => setSource('acc')}
            className={`p-4 rounded-lg border text-left transition-all ${
              source === 'acc'
                ? 'border-brand-gold/40 bg-brand-gold/10'
                : 'border-surface-3 bg-surface-2 hover:border-surface-4'
            }`}
          >
            <div className="text-sm font-medium text-gray-200 mb-1">
              ACC Model Coordination
            </div>
            <div className="text-xs text-gray-500">
              Autodesk Construction Cloud via APS REST API. Cloud-to-cloud clash reading.
            </div>
            <div className="mt-2 flex items-center gap-2 text-xs">
              <span
                className={`w-2 h-2 rounded-full ${
                  accConnected ? 'bg-clash-resolved' : 'bg-clash-critical'
                }`}
              />
              <span className="text-gray-400">
                {accConnected ? 'Connected' : 'Not connected'}
              </span>
            </div>
          </button>
        </div>

        {/* ACC Connection */}
        {source === 'acc' && !accConnected && (
          <div className="mt-4 p-4 rounded-lg bg-surface-2 border border-surface-3">
            <h3 className="text-sm font-medium text-gray-300 mb-2">
              Connect to Autodesk Construction Cloud
            </h3>
            <p className="text-xs text-gray-500 mb-3">
              Authenticate with your Autodesk account to access Model Coordination
              clash data. Uses 3-legged OAuth 2.0 with scopes: data:read, data:write,
              account:read.
            </p>
            <button
              onClick={() => {
                // In production, this would redirect to APS OAuth
                setAccConnected(true);
                setAccProjectName('Highway 401 Widening');
              }}
              className="btn-primary text-sm"
            >
              Connect Autodesk Account
            </button>
          </div>
        )}

        {source === 'acc' && accConnected && (
          <div className="mt-4 p-4 rounded-lg bg-surface-2 border border-clash-resolved/20">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-sm text-gray-300 flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-clash-resolved" />
                  Connected to ACC
                </div>
                <div className="text-xs text-gray-500 mt-1">
                  Project: {accProjectName || 'Highway 401 Widening'}
                </div>
                <div className="text-xs text-gray-600 mt-0.5">
                  Model Coordination space linked
                </div>
              </div>
              <button
                onClick={() => setAccConnected(false)}
                className="btn-secondary text-xs"
              >
                Disconnect
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Constraint Thresholds */}
      <div className="card p-6 mb-6">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider mb-1">
          Engineering Constraint Thresholds
        </h2>
        <p className="text-xs text-gray-500 mb-4">
          Configurable per project — different jurisdictions have different standards
        </p>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs text-gray-500 mb-1">
                Min Cover — Gravity (m)
              </label>
              <input
                type="number"
                step="0.1"
                value={constraints.minCoverGravity}
                onChange={(e) =>
                  updateConstraint('minCoverGravity', e.target.value)
                }
                className="input w-full text-sm font-mono"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-500 mb-1">
                Min Cover — Pressure (m)
              </label>
              <input
                type="number"
                step="0.1"
                value={constraints.minCoverPressure}
                onChange={(e) =>
                  updateConstraint('minCoverPressure', e.target.value)
                }
                className="input w-full text-sm font-mono"
              />
            </div>
          </div>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-xs text-gray-500 mb-1">
                Crossing Clearance (m)
              </label>
              <input
                type="number"
                step="0.01"
                value={constraints.crossingClearance}
                onChange={(e) =>
                  updateConstraint('crossingClearance', e.target.value)
                }
                className="input w-full text-sm font-mono"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-500 mb-1">
                Min Slope — Gravity (fraction)
              </label>
              <input
                type="number"
                step="0.001"
                value={constraints.minSlopeGravity}
                onChange={(e) =>
                  updateConstraint('minSlopeGravity', e.target.value)
                }
                className="input w-full text-sm font-mono"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-500 mb-1">
                Max Depth (m)
              </label>
              <input
                type="number"
                step="0.5"
                value={constraints.maxDepth}
                onChange={(e) => updateConstraint('maxDepth', e.target.value)}
                className="input w-full text-sm font-mono"
              />
            </div>
          </div>
        </div>
      </div>

      {/* Save */}
      <div className="flex justify-end gap-3">
        <button className="btn-secondary">Reset to Defaults</button>
        <button className="btn-primary">Save Settings</button>
      </div>
    </div>
  );
}
