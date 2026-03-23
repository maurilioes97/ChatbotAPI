import { CheckIcon, ClipboardIcon } from '@heroicons/react/24/outline';
import { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import { ChatMessage } from '../types/chat';

interface MessageItemProps {
  message: ChatMessage;
}

export const MessageItem = ({ message }: MessageItemProps) => {
  const isUser = message.role === 'User';
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(message.content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000); // Volta ao ícone original após 2s
  };

  return (
    <div className={`flex items-start gap-3 mb-6 group ${isUser ? 'flex-row-reverse' : 'flex-row'}`}>
      {/* Avatar */}
      <div className={`h-10 w-10 shrink-0 rounded-full flex items-center justify-center text-lg shadow-sm border ${
        isUser ? 'bg-blue-600 border-blue-700 text-white' : 'bg-cyan-100 border-cyan-200'
      }`}>
        {isUser ? 'U' : '🤖'}
      </div>

      {/* Balão de Mensagem */}
      <div
        className={`relative max-w-[85%] lg:max-w-[75ch] px-5 py-4 rounded-2xl shadow-sm transition-all ${
          isUser
            ? 'bg-blue-600 text-white rounded-tr-none' 
            : 'bg-white border border-slate-200 text-slate-800 rounded-tl-none'
        }`}
      >
        {/* Botão Copiar (Apenas para o Bot) */}
        {!isUser && (
          <button
            onClick={handleCopy}
            className="absolute -right-12 top-0 p-2 rounded-lg bg-white border border-slate-200 shadow-sm opacity-0 group-hover:opacity-100 transition-opacity hover:bg-slate-50"
            title="Copiar mensagem"
          >
            {copied ? (
              <CheckIcon className="w-4 h-4 text-green-500" />
            ) : (
              <ClipboardIcon className="w-4 h-4 text-slate-400" />
            )}
          </button>
        )}

        {/* Conteúdo Renderizado */}
        <div className={`prose prose-sm md:prose-base max-w-none break-words ${
          isUser 
            ? 'prose-invert prose-p:text-blue-50' 
            : 'prose-slate prose-headings:text-slate-900 prose-strong:text-blue-700'
        }`}>
          {isUser ? (
             <p className="whitespace-pre-wrap">{message.content}</p>
          ) : (
            <ReactMarkdown>{message.content}</ReactMarkdown>
          )}
        </div>

        {/* Timestamp */}
        <p className={`text-sm mt-3 font-medium uppercase tracking-wider ${
          isUser ? 'text-blue-200 text-right' : 'text-slate-400'
        }`}>
          {new Date(message.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </p>
      </div>
    </div>
  );
};