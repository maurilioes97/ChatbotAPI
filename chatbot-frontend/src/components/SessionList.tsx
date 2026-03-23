import { ChatBubbleLeftEllipsisIcon, ExclamationTriangleIcon, PlusIcon } from '@heroicons/react/24/outline';
import { useState } from 'react';
import { ChatSession } from '../types/chat';
import { Modal } from './Modal';
import { SessionItem } from './SessionItem';

interface SessionListProps {
  sessions: ChatSession[];
  activeSessionId: string | null;
  onSelectSession: (id: string) => void;
  onDeleteSession: (id: string) => void;
  onNewSession: () => void;
  className?: string;
}

export const SessionList = ({
  sessions,
  activeSessionId,
  onSelectSession,
  onDeleteSession,
  onNewSession,
  className,
}: SessionListProps) => {
  // Estado para controlar qual sessão o usuário clicou para deletar
  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null);

  const handleConfirmDelete = () => {
    if (sessionToDelete !== null) {
      onDeleteSession(sessionToDelete);
      setSessionToDelete(null); // Fecha a modal após deletar
    }
  };

  return (
    <aside
      className={`bg-slate-50 flex flex-col min-h-0 overflow-hidden ${className ?? ''}`}
    >
      {/* Header */}
      <div className="p-4 md:p-6">
        <div className="flex items-center gap-2 mb-6">
          <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center shadow-blue-200 shadow-lg">
            <span className="text-white font-bold text-sm">AI</span>
          </div>
          <h1 className="text-xl font-bold text-slate-800 tracking-tight">
            Chatbot AI
          </h1>
        </div>
        
        <button
          onClick={onNewSession}
          className="w-full py-3 bg-white border border-slate-200 text-slate-700 rounded-xl hover:bg-slate-50 hover:border-blue-300 hover:text-blue-600 transition-all font-semibold text-sm shadow-sm flex items-center justify-center gap-2 active:scale-[0.98]"
        >
          <PlusIcon className="w-4 h-4" />
          Nova Sessão
        </button>
      </div>

      {/* Lista de Sessões */}
      <div className="flex-1 overflow-y-auto px-3 pb-4 space-y-1 min-h-0">
        <p className="px-3 text-[11px] font-bold text-slate-400 uppercase tracking-widest mb-2">
          Suas Conversas
        </p>
        
        {sessions.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 px-4 text-center">
            <div className="w-12 h-12 bg-slate-100 rounded-full flex items-center justify-center mb-3">
              <ChatBubbleLeftEllipsisIcon className="w-6 h-6 text-slate-300" />
            </div>
            <p className="text-slate-400 text-xs">Nenhuma sessão iniciada ainda.</p>
          </div>
        ) : (
          sessions.map((session) => (
            <SessionItem
              key={session.localSessionId}
              session={session}
              isActive={activeSessionId === session.localSessionId}
              onClick={onSelectSession}
              onDelete={setSessionToDelete}
            />
          ))
        )}
      </div>

      {/* --- Modal de Confirmação --- */}
      <Modal 
        isOpen={sessionToDelete !== null} 
        onClose={() => setSessionToDelete(null)}
        title="Excluir Conversa"
      >
        <div className="flex flex-col items-center text-center">
          <div className="w-14 h-14 bg-red-50 rounded-full flex items-center justify-center mb-4">
            <ExclamationTriangleIcon className="w-7 h-7 text-red-500" />
          </div>
          
          <h2 className="text-lg font-bold text-slate-800 mb-1">Tem certeza?</h2>
          <p className="text-slate-500 text-sm">
            Você está prestes a apagar todo o histórico desta conversa. Esta ação não pode ser desfeita.
          </p>

          <div className="flex w-full gap-3 mt-8">
            <button 
              onClick={() => setSessionToDelete(null)}
              className="flex-1 px-4 py-2.5 text-sm font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-xl transition-colors"
            >
              Cancelar
            </button>
            <button 
              onClick={handleConfirmDelete}
              className="flex-1 px-4 py-2.5 text-sm font-semibold text-white bg-red-600 hover:bg-red-700 rounded-xl shadow-md shadow-red-100 transition-all active:scale-95"
            >
              Sim, excluir
            </button>
          </div>
        </div>
      </Modal>
    </aside>
  );
};