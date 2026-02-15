'use client';

import AgentChat from '@/components/agent-chat';

export default function ChatPage() {
  return (
    <div className="h-[calc(100vh-0px)] flex flex-col">
      <div className="px-8 py-5 border-b border-surface-3">
        <h1 className="text-2xl font-bold text-gray-100">Agent Chat</h1>
        <p className="text-sm text-gray-500 mt-1">
          Context-aware AI assistant powered by Claude — ask about clashes, options, and engineering constraints
        </p>
      </div>
      <div className="flex-1 overflow-hidden">
        <AgentChat />
      </div>
    </div>
  );
}
