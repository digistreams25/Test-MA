const AGENT_BASE = 'http://localhost:3000';

async function agentRequest<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${AGENT_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) throw new Error(`Agent error: ${res.status} ${res.statusText}`);
  return res.json();
}

export const agentClient = {
  health: () => agentRequest('/health'),
  getClashes: () => agentRequest('/clashes'),
  getClash: (id: string) => agentRequest(`/clashes/${id}`),
  propose: (clashId: string) =>
    agentRequest(`/clashes/${clashId}/propose`, { method: 'POST' }),
  approve: (proposalId: string) =>
    agentRequest(`/proposals/${proposalId}/approve`, { method: 'POST' }),
  execute: (proposalId: string) =>
    agentRequest(`/proposals/${proposalId}/execute`, { method: 'POST' }),
  validate: (proposalId: string) =>
    agentRequest(`/proposals/${proposalId}/validate`, { method: 'POST' }),
  getAuditLog: () => agentRequest('/audit-log'),
};
