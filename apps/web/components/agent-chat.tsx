'use client';

import { useState, useRef, useEffect } from 'react';
import type { ChatMessage } from '@/lib/types';

interface AgentChatProps {
  clashContext?: string;
}

const initialMessages: ChatMessage[] = [
  {
    id: '1',
    role: 'assistant',
    content:
      "Hello! I'm the TwinFlux resolution assistant. I can help you understand clash details, explain resolution options, and prioritize fixes. What would you like to know?",
    timestamp: new Date().toISOString(),
  },
];

const mockResponses: Record<string, string> = {
  default:
    "I can help you analyze clashes, explain risk levels, prioritize fixes, and understand the engineering constraints. Try asking about a specific clash or resolution option.",
  priority:
    "Based on the current clash data, I'd recommend prioritizing **CLH-001** first. It's a hard clash with 45mm penetration between a 600mm storm pipe (gravity) and a 200mm water main (pressure). Since the water main is a pressure pipe, it can be moved more flexibly. The recommended OPT-1 (lower the water main by 245mm) has LOW risk with all constraints passing.",
  'opt-2':
    "**OPT-2 (Raise pipe)** carries HIGH risk for CLH-001 because raising the Water Main W-205 by 245mm would reduce its cover from 1.100m to approximately 0.855m, which is **below the minimum 0.900m** required for pressure pipes. This violates the minimum cover constraint and would require a variance approval from the jurisdiction.",
  downstream:
    "For **OPT-1 on CLH-001**, there are **2 downstream elements** that may need adjustment. When you lower the water main, the connected pipe segments downstream must maintain minimum slope. Since this is a pressure pipe, there's no slope constraint — but the physical connections at fittings need to align. The downstream impact is minor and typically resolved by adjusting the adjacent pipe segment endpoints.",
  storm:
    "Looking at storm network clashes:\n\n- **CLH-001** (G-14): Storm Pipe S-101 (600mm RCP) — hard clash with water main, 45mm penetration\n- **CLH-003** (G-16): Storm Pipe S-103 (600mm RCP) — resolved, was a hard clash with water main\n\nBoth are on the STM-Main-01 network along the Storm-Main-AL alignment. CLH-001 still needs resolution; CLH-003 was fixed using OPT-1 (lowered the water main).",
};

export default function AgentChat({ clashContext }: AgentChatProps) {
  const [messages, setMessages] = useState<ChatMessage[]>(initialMessages);
  const [input, setInput] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages]);

  const handleSend = async () => {
    if (!input.trim()) return;

    const userMsg: ChatMessage = {
      id: Date.now().toString(),
      role: 'user',
      content: input,
      timestamp: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMsg]);
    setInput('');
    setIsTyping(true);

    // Simulate AI response with context-aware mock
    setTimeout(() => {
      const lower = userMsg.content.toLowerCase();
      let response = mockResponses.default;

      if (lower.includes('priorit') || lower.includes('first') || lower.includes('recommend'))
        response = mockResponses.priority;
      else if (lower.includes('opt-2') || lower.includes('raise') || lower.includes('high risk'))
        response = mockResponses['opt-2'];
      else if (lower.includes('downstream') || lower.includes('impact') || lower.includes('propagat'))
        response = mockResponses.downstream;
      else if (lower.includes('storm') || lower.includes('stm') || lower.includes('all clashes'))
        response = mockResponses.storm;

      const assistantMsg: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: response,
        timestamp: new Date().toISOString(),
      };

      setMessages((prev) => [...prev, assistantMsg]);
      setIsTyping(false);
    }, 1200);
  };

  return (
    <div className="flex flex-col h-full">
      {/* Messages */}
      <div ref={scrollRef} className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`flex ${
              msg.role === 'user' ? 'justify-end' : 'justify-start'
            }`}
          >
            <div
              className={`max-w-[85%] rounded-lg px-4 py-3 text-sm ${
                msg.role === 'user'
                  ? 'bg-brand-gold/20 text-gray-200 border border-brand-gold/30'
                  : 'bg-surface-2 text-gray-300 border border-surface-3'
              }`}
            >
              {msg.role === 'assistant' && (
                <div className="text-[10px] text-brand-gold font-medium mb-1.5 uppercase tracking-wider">
                  TwinFlux Agent
                </div>
              )}
              <div className="whitespace-pre-wrap leading-relaxed">
                {msg.content.split(/(\*\*[^*]+\*\*)/).map((part, i) => {
                  if (part.startsWith('**') && part.endsWith('**')) {
                    return (
                      <span key={i} className="font-semibold text-gray-100">
                        {part.slice(2, -2)}
                      </span>
                    );
                  }
                  return <span key={i}>{part}</span>;
                })}
              </div>
              <div className="text-[10px] text-gray-600 mt-1.5">
                {new Date(msg.timestamp).toLocaleTimeString()}
              </div>
            </div>
          </div>
        ))}
        {isTyping && (
          <div className="flex justify-start">
            <div className="bg-surface-2 border border-surface-3 rounded-lg px-4 py-3">
              <div className="flex gap-1">
                <span className="w-2 h-2 bg-gray-500 rounded-full animate-bounce" />
                <span
                  className="w-2 h-2 bg-gray-500 rounded-full animate-bounce"
                  style={{ animationDelay: '0.15s' }}
                />
                <span
                  className="w-2 h-2 bg-gray-500 rounded-full animate-bounce"
                  style={{ animationDelay: '0.3s' }}
                />
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Suggestions */}
      <div className="px-4 py-2 flex gap-2 flex-wrap">
        {[
          'Which clash should I fix first?',
          'Why is OPT-2 high risk?',
          "What's the downstream impact?",
          'Show me all storm network clashes',
        ].map((suggestion) => (
          <button
            key={suggestion}
            onClick={() => {
              setInput(suggestion);
            }}
            className="text-[11px] px-2.5 py-1 rounded-full bg-surface-2 border border-surface-4 text-gray-400 hover:text-brand-gold hover:border-brand-gold/30 transition-colors"
          >
            {suggestion}
          </button>
        ))}
      </div>

      {/* Input */}
      <div className="p-4 border-t border-surface-3">
        <div className="flex gap-2">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSend()}
            placeholder="Ask about clashes, options, or engineering constraints..."
            className="input flex-1 text-sm"
          />
          <button
            onClick={handleSend}
            disabled={!input.trim() || isTyping}
            className="btn-primary px-5"
          >
            Send
          </button>
        </div>
      </div>
    </div>
  );
}
