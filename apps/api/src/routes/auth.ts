import { Router } from 'express';
import { v4 as uuid } from 'uuid';

export const authRouter = Router();

// Mock auth for development
authRouter.post('/login', (req, res) => {
  const { email, password } = req.body;
  // In production: validate against database, hash passwords, issue JWT
  res.json({
    token: `mock-jwt-${uuid()}`,
    user: {
      id: 'user-001',
      email: email || 'coordinator@example.com',
      name: 'BIM Coordinator',
      role: 'coordinator',
    },
  });
});

authRouter.post('/register', (req, res) => {
  const { email, name, password } = req.body;
  res.json({
    token: `mock-jwt-${uuid()}`,
    user: {
      id: `user-${uuid().slice(0, 8)}`,
      email,
      name,
      role: 'coordinator',
    },
  });
});
