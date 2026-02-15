import { Router } from 'express';
import { accService } from '../services/acc.js';

export const projectsRouter = Router();

// Mock project data
const projects = [
  {
    id: 'proj-001',
    name: 'Highway 401 Widening — Phase 2',
    location: 'Toronto, ON',
    clashSource: 'navisworks' as const,
    constraints: {
      minCoverGravity: 1.2,
      minCoverPressure: 0.9,
      crossingClearance: 0.15,
      minSlopeGravity: 0.005,
      maxDepth: 6.0,
    },
    createdAt: '2025-11-15T08:00:00Z',
  },
];

projectsRouter.get('/', (_req, res) => {
  res.json(projects);
});

projectsRouter.post('/', (req, res) => {
  const project = {
    id: `proj-${Date.now()}`,
    ...req.body,
    createdAt: new Date().toISOString(),
  };
  projects.push(project);
  res.status(201).json(project);
});

projectsRouter.get('/:id', (req, res) => {
  const project = projects.find((p) => p.id === req.params.id);
  if (!project) return res.status(404).json({ error: 'Project not found' });
  res.json(project);
});

// ACC Integration endpoints
projectsRouter.post('/:id/connect-acc', (req, res) => {
  // In production: initiate APS 3-legged OAuth 2.0 flow
  // Redirect user to Autodesk login, handle callback, store tokens
  const authUrl = accService.getAuthorizationUrl(req.params.id);
  res.json({
    message: 'Redirect user to Autodesk login',
    authUrl,
    scopes: ['data:read', 'data:write', 'account:read'],
  });
});

// OAuth callback from Autodesk
projectsRouter.get('/:id/acc-callback', async (req, res) => {
  const { code } = req.query;
  if (!code) return res.status(400).json({ error: 'Missing authorization code' });

  try {
    const tokens = await accService.exchangeCodeForTokens(code as string);
    // In production: store tokens encrypted in database
    res.json({ message: 'ACC connected successfully', projectId: req.params.id });
  } catch (err) {
    res.status(500).json({ error: 'Failed to connect to ACC' });
  }
});

// Fetch clashes from ACC Model Coordination
projectsRouter.get('/:id/acc-clashes', async (req, res) => {
  try {
    const clashes = await accService.getClashes(req.params.id);
    res.json(clashes);
  } catch (err) {
    res.status(500).json({ error: 'Failed to fetch ACC clashes' });
  }
});
