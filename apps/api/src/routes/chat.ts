import { Router } from 'express';
import { claudeService } from '../services/claude.js';

export const chatRouter = Router();

chatRouter.post('/', async (req, res) => {
  const { message, context } = req.body;

  if (!message) {
    return res.status(400).json({ error: 'Message is required' });
  }

  try {
    const response = await claudeService.chat(message, context || {});
    res.json({
      role: 'assistant',
      content: response,
      timestamp: new Date().toISOString(),
    });
  } catch (err) {
    console.error('Claude API error:', err);
    res.status(500).json({ error: 'Failed to get response from AI' });
  }
});
