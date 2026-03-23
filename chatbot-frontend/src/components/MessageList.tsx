import { useEffect, useRef } from 'react';
import { ChatMessage } from '../types/chat';
import { MessageItem } from './MessageItem';

interface MessageListProps {
  messages: ChatMessage[];
  isTyping?: boolean;
  typingMessage?: string;
}

export const MessageList = ({
  messages,
  isTyping = false,
  typingMessage = '',
}: MessageListProps) => {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Auto-scroll para a última mensagem
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isTyping, typingMessage]);

  if (messages.length === 0 && !typingMessage) {
    return (
      <div className="flex-1 flex items-center justify-center text-slate-500 px-6">
        <p className="text-center text-lg md:text-base max-w-sm">
          Nenhuma mensagem ainda. Comece conversando!
        </p>
      </div>
    );
  }

  return (
    <div className="flex-1 min-h-0 overflow-y-auto px-4 md:px-8 py-6">
      <div className="mx-auto w-full max-w-6xl space-y-6">
        {messages.map((message) => (
          <MessageItem key={message.id} message={message} />
        ))}

        {typingMessage && (
          <MessageItem
            message={{
              id: -1,
              sessionId: -1,
              role: 'Assistant',
              content: typingMessage,
              createdAt: new Date().toISOString(),
            }}
          />
        )}

        {isTyping && (
          <div className="flex items-end gap-3">
            <div className="h-10 w-10 rounded-full bg-cyan-100 border border-cyan-200 flex items-center justify-center text-lg">
              🤖
            </div>
            <div>
              <p className="text-slate-500 font-medium">Bot está digitando...</p>
              <div className="mt-1 flex gap-2">
                <span className="h-2.5 w-2.5 rounded-full bg-slate-400 animate-pulse" />
                <span className="h-2.5 w-2.5 rounded-full bg-slate-300 animate-pulse [animation-delay:120ms]" />
                <span className="h-2.5 w-2.5 rounded-full bg-slate-300 animate-pulse [animation-delay:220ms]" />
              </div>
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>
    </div>
  );
};
