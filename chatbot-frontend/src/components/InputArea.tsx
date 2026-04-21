import { useState } from 'react';

interface InputAreaProps {
  onSendMessage: (message: string) => void;
  isLoading: boolean;
}

export const InputArea = ({ onSendMessage, isLoading }: InputAreaProps) => {
  const [message, setMessage] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (message.trim()) {
      onSendMessage(message);
      setMessage('');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="p-2 md:p-4 border-t border-slate-200 bg-slate-100">
      <div className="mx-auto w-full max-w-6xl flex items-center gap-1.5 md:gap-2 rounded-2xl bg-white border border-slate-300 px-2 md:px-3 py-2">
        <input
          type="text"
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          placeholder="Digite sua mensagem ou faça uma pergunta..."
          disabled={isLoading}
          className="flex-1 min-w-0 px-2 md:px-3 py-2 text-slate-700 placeholder:text-slate-400 bg-transparent focus:outline-none disabled:text-slate-400"
        />

        {/*
        <button
          type="button"
          className="h-9 w-9 rounded-full text-slate-500 hover:bg-slate-100 flex items-center justify-center"
          aria-label="Anexar arquivo"
        >
          <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="1.8">
            <path d="M21 12.2 12.8 20.4a6 6 0 1 1-8.5-8.5l8.2-8.2a4 4 0 0 1 5.6 5.6l-8.4 8.4a2 2 0 1 1-2.8-2.8l7.8-7.8" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </button>

        <button
          type="button"
          className="hidden sm:flex h-9 w-9 rounded-full text-slate-500 hover:bg-slate-100 items-center justify-center"
          aria-label="Mensagem por voz"
        >
          <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="1.8">
            <path d="M12 15a3 3 0 0 0 3-3V7a3 3 0 0 0-6 0v5a3 3 0 0 0 3 3Z" strokeLinecap="round" strokeLinejoin="round" />
            <path d="M19 11.5a7 7 0 0 1-14 0M12 18.5V21M8 21h8" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </button>
        */}

        <button
          type="submit"
          disabled={isLoading || !message.trim()}
          className="h-10 w-10 md:h-11 md:w-11 shrink-0 rounded-xl bg-blue-500 text-white hover:bg-blue-600 disabled:bg-slate-300 disabled:cursor-not-allowed transition-colors flex items-center justify-center"
        >
          {isLoading ? (
            <span className="h-5 w-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
          ) : (
            <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M3 12h15" strokeLinecap="round" strokeLinejoin="round" />
              <path d="m12 5 7 7-7 7" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          )}
        </button>
      </div>
    </form>
  );
};
