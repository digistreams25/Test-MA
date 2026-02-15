const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:4000';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) throw new Error(`API error: ${res.status} ${res.statusText}`);
  return res.json();
}

export const api = {
  // Auth
  login: (email: string, password: string) =>
    request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  // Projects
  getProjects: () => request('/projects'),
  createProject: (data: Record<string, unknown>) =>
    request('/projects', { method: 'POST', body: JSON.stringify(data) }),

  // ACC
  connectACC: (projectId: string) =>
    request(`/projects/${projectId}/connect-acc`, { method: 'POST' }),
  getACCClashes: (projectId: string) =>
    request(`/projects/${projectId}/acc-clashes`),

  // Chat
  sendMessage: (message: string, context: Record<string, unknown>) =>
    request('/chat', {
      method: 'POST',
      body: JSON.stringify({ message, context }),
    }),

  // Audit
  getAuditLog: (projectId: string) => request(`/audit/${projectId}`),
  exportAudit: (projectId: string, format: 'csv' | 'pdf') =>
    request(`/audit/${projectId}/export?format=${format}`),
};
