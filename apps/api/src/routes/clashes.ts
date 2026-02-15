import { Router } from 'express';

export const clashesRouter = Router();

// This route proxies to the local agent for Navisworks clashes,
// or fetches from ACC via the acc service. In production the web
// dashboard would talk directly to localhost:3000 for NW clashes.

clashesRouter.get('/', (_req, res) => {
  res.json({
    message: 'Use the local agent API (localhost:3000) for Navisworks clashes, or /projects/:id/acc-clashes for ACC clashes',
  });
});
