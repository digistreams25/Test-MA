'use client';

import { clsx } from 'clsx';
import type { ConsentState } from '@/lib/types';

interface ConsentBarProps {
  state: ConsentState;
  selectedOptionLabel: string | null;
  onApprove: () => void;
  onExecute: () => void;
  onRevoke: () => void;
  onValidate: () => void;
}

const steps: { state: ConsentState; label: string }[] = [
  { state: 'options_ready', label: 'Options Ready' },
  { state: 'option_selected', label: 'Selected' },
  { state: 'approved', label: 'Approved' },
  { state: 'executing', label: 'Executing' },
  { state: 'executed', label: 'Executed' },
  { state: 'validating', label: 'Validating' },
  { state: 'resolved', label: 'Resolved' },
];

const stateOrder: Record<string, number> = {};
steps.forEach((s, i) => {
  stateOrder[s.state] = i;
});

export default function ConsentBar({
  state,
  selectedOptionLabel,
  onApprove,
  onExecute,
  onRevoke,
  onValidate,
}: ConsentBarProps) {
  const currentStep = stateOrder[state] ?? 0;

  return (
    <div className="card p-4">
      {/* Progress steps */}
      <div className="flex items-center gap-1 mb-4">
        {steps.map((step, i) => {
          const isComplete = currentStep > i;
          const isCurrent = currentStep === i;
          return (
            <div key={step.state} className="flex items-center gap-1 flex-1">
              <div
                className={clsx(
                  'w-full h-1.5 rounded-full transition-colors',
                  isComplete && 'bg-clash-resolved',
                  isCurrent && 'bg-brand-gold',
                  !isComplete && !isCurrent && 'bg-surface-3'
                )}
              />
            </div>
          );
        })}
      </div>

      <div className="flex items-center justify-between text-xs text-gray-500 mb-4">
        {steps.map((step, i) => {
          const isCurrent = currentStep === i;
          const isComplete = currentStep > i;
          return (
            <span
              key={step.state}
              className={clsx(
                isCurrent && 'text-brand-gold font-medium',
                isComplete && 'text-clash-resolved'
              )}
            >
              {step.label}
            </span>
          );
        })}
      </div>

      {/* Actions */}
      <div className="flex items-center gap-3">
        {state === 'option_selected' && (
          <>
            <button onClick={onApprove} className="btn-primary flex-1">
              Approve: {selectedOptionLabel}
            </button>
            <button onClick={onRevoke} className="btn-secondary">
              Change
            </button>
          </>
        )}

        {state === 'approved' && (
          <>
            <button onClick={onExecute} className="btn-primary flex-1">
              Execute in Civil 3D
            </button>
            <button onClick={onRevoke} className="btn-danger">
              Revoke Approval
            </button>
          </>
        )}

        {state === 'executing' && (
          <div className="flex items-center gap-3 text-brand-gold w-full justify-center py-2">
            <div className="w-4 h-4 border-2 border-brand-gold border-t-transparent rounded-full animate-spin" />
            <span className="text-sm">Executing modification in Civil 3D...</span>
          </div>
        )}

        {state === 'executed' && (
          <button onClick={onValidate} className="btn-primary flex-1">
            Run Validation
          </button>
        )}

        {state === 'validating' && (
          <div className="flex items-center gap-3 text-brand-gold w-full justify-center py-2">
            <div className="w-4 h-4 border-2 border-brand-gold border-t-transparent rounded-full animate-spin" />
            <span className="text-sm">Validating — re-running clash check...</span>
          </div>
        )}

        {state === 'resolved' && (
          <div className="flex items-center gap-2 text-clash-resolved w-full justify-center py-2">
            <span className="text-lg">✓</span>
            <span className="text-sm font-medium">Clash Resolved</span>
          </div>
        )}

        {state === 'partially_resolved' && (
          <div className="flex items-center gap-2 text-clash-warning w-full justify-center py-2">
            <span className="text-lg">⚠</span>
            <span className="text-sm">
              Partially resolved — new clashes detected
            </span>
          </div>
        )}

        {state === 'not_resolved' && (
          <div className="flex items-center gap-2 text-clash-critical w-full justify-center py-2">
            <span className="text-lg">✗</span>
            <span className="text-sm">
              Not resolved — re-analysis needed with greater offset
            </span>
          </div>
        )}

        {state === 'failed' && (
          <div className="flex items-center gap-2 text-clash-critical w-full justify-center py-2">
            <span className="text-lg">✗</span>
            <span className="text-sm">Execution failed — try a different option</span>
          </div>
        )}

        {state === 'options_ready' && (
          <div className="text-sm text-gray-500 w-full text-center py-2">
            Select a resolution option to proceed
          </div>
        )}

        {state === 'analyzing' && (
          <div className="flex items-center gap-3 text-gray-400 w-full justify-center py-2">
            <div className="w-4 h-4 border-2 border-gray-400 border-t-transparent rounded-full animate-spin" />
            <span className="text-sm">Analyzing clash...</span>
          </div>
        )}
      </div>
    </div>
  );
}
