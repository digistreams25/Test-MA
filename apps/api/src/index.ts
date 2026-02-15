import express from 'express';
import cors from 'cors';
import { authRouter } from './routes/auth.js';
import { projectsRouter } from './routes/projects.js';
import { clashesRouter } from './routes/clashes.js';
import { chatRouter } from './routes/chat.js';
import { auditRouter } from './routes/audit.js';

const app = express();
const PORT = process.env.PORT || 4000;

app.use(cors({ origin: '*' }));
app.use(express.json());

// Routes
app.use('/auth', authRouter);
app.use('/projects', projectsRouter);
app.use('/clashes', clashesRouter);
app.use('/chat', chatRouter);
app.use('/audit', auditRouter);

// Health check
app.get('/health', (_req, res) => {
  res.json({ status: 'ok', service: 'twinflux-api', timestamp: new Date().toISOString() });
});

app.listen(PORT, () => {
  console.log(`TwinFlux API server running on port ${PORT}`);
});
