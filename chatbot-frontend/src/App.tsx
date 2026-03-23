import {
  Bars3Icon,
  PlusIcon,
  XMarkIcon,
} from '@heroicons/react/24/outline';
import { useEffect, useState } from 'react';
import { ChatWindow } from './components/ChatWindow';
import { NewSessionModal } from './components/NewSessionModal';
import { SessionList } from './components/SessionList';
import { useChatAPI } from './hooks/useChatAPI';
import { ChatLocalStorage, ChatMessage, ChatSession } from './types/chat';
import {
  createLocalSessionId,
  loadPersistedChatState,
  savePersistedChatState,
} from './utils/chatCache';

export function App() {
  const [initialState] = useState<ChatLocalStorage>(() => loadPersistedChatState());

  const sessionSuggestions = [
    {
      title: 'Programação C#/.NET',
      description: 'Tire dúvidas técnicas com foco em backend, API e boas práticas.',
      prompt: 'Você é um assistente para dúvidas de programação em C# e .NET.',
    },
    {
      title: 'Tutor da Faculdade',
      description: 'Receba explicações passo a passo para exercícios e trabalhos.',
      prompt:
        'Você é um tutor para exercícios da faculdade, explicando passo a passo.',
    },
    {
      title: 'Revisor Acadêmico',
      description: 'Revise textos em português formal com foco em clareza e coesão.',
      prompt: 'Você é um revisor de textos acadêmicos em português formal.',
    },
  ];

  const [sessions, setSessions] = useState<ChatSession[]>(initialState.sessions);
  const [sessionMessages, setSessionMessages] = useState<
    Record<string, ChatMessage[]>
  >(initialState.sessionMessages);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(
    initialState.activeSessionId
  );
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isNewSessionModalOpen, setIsNewSessionModalOpen] = useState(false);

  const { criarSessao, loading } = useChatAPI();

  useEffect(() => {
    const persistedState: ChatLocalStorage = {
      sessions,
      sessionMessages,
      activeSessionId,
    };

    savePersistedChatState(persistedState);
  }, [sessions, sessionMessages, activeSessionId]);

  const handleCreateSession = async (systemPrompt: string) => {
    try {
      const sessionId = await criarSessao(systemPrompt);
      const localSessionId = createLocalSessionId();

      const newSession: ChatSession = {
        id: sessionId,
        localSessionId,
        systemPrompt,
        createdAt: new Date().toISOString(),
      };

      setSessions([...sessions, newSession]);
      setSessionMessages({ ...sessionMessages, [localSessionId]: [] });
      setActiveSessionId(localSessionId);
      setIsNewSessionModalOpen(false);
    } catch (error) {
      console.error('Erro ao criar sessão:', error);
    }
  };

  const handleSelectSession = (id: string) => {
    setActiveSessionId(id);
    setIsSidebarOpen(false);
  };

  const handleDeleteSession = (id: string) => {
    const remainingSessions = sessions.filter(
      (session) => session.localSessionId !== id
    );
    setSessions(remainingSessions);
    const newMessages = { ...sessionMessages };
    delete newMessages[id];
    setSessionMessages(newMessages);

    if (activeSessionId === id) {
      setActiveSessionId(
        remainingSessions.length > 0 ? remainingSessions[0].localSessionId : null
      );
    }
  };

  const handleAddMessage = (message: ChatMessage) => {
    const targetSession = sessions.find((session) => session.id === message.sessionId);

    if (!targetSession) {
      return;
    }

    setSessionMessages((prev) => ({
      ...prev,
      [targetSession.localSessionId]: [
        ...(prev[targetSession.localSessionId] || []),
        message,
      ],
    }));
  };

  const currentSession = activeSessionId
    ? sessions.find((session) => session.localSessionId === activeSessionId) ?? null
    : null;

  const currentMessages = currentSession
    ? sessionMessages[currentSession.localSessionId] || []
    : [];

  return (
    <div className="flex h-screen bg-slate-100 overflow-hidden">
      <div className="hidden md:block md:shrink-0">
        <SessionList
          sessions={sessions}
          activeSessionId={activeSessionId}
          onSelectSession={handleSelectSession}
          onDeleteSession={handleDeleteSession}
          onNewSession={() => setIsNewSessionModalOpen(true)}
          className="w-80 h-screen border-r border-slate-200"
        />
      </div>

      <main className="flex-1 min-w-0 min-h-0 flex flex-col">
        <header className="md:hidden h-14 px-3 border-b border-slate-200 bg-white flex items-center justify-between shrink-0">
          <button
            onClick={() => setIsSidebarOpen(true)}
            className="h-9 w-9 rounded-lg border border-slate-200 text-slate-600 flex items-center justify-center"
            aria-label="Abrir sessões"
          >
            <Bars3Icon className="h-5 w-5" />
          </button>

          <p className="font-semibold text-slate-800 text-sm">Chatbot AI</p>

          <button
            onClick={() => setIsNewSessionModalOpen(true)}
            className="h-9 w-9 rounded-lg bg-blue-500 text-white flex items-center justify-center"
            aria-label="Nova sessão"
          >
            <PlusIcon className="h-5 w-5" />
          </button>
        </header>

        {/* Main Chat Area */}
        <div className="flex-1 min-h-0">
        {currentSession ? (
          <ChatWindow
            sessionId={currentSession.id}
            messages={currentMessages}
            onAddMessage={handleAddMessage}
          />
        ) : (
          <div className="h-full bg-slate-50 overflow-y-auto text-slate-500">
            <div className="min-h-full w-full max-w-6xl mx-auto px-3 md:px-6 py-4 md:py-8 text-center">
              <p className="text-2xl md:text-3xl font-semibold text-slate-700 mb-2">
                Nenhuma sessão ativa
              </p>
              <p className="text-sm md:text-base text-slate-500 mb-5 md:mb-7">
                Escolha uma sugestão para começar rápido ou crie uma sessão
                personalizada.
              </p>

              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-5 md:mb-7 text-left">
                {sessionSuggestions.map((suggestion) => (
                  <button
                    key={suggestion.title}
                    onClick={() => handleCreateSession(suggestion.prompt)}
                    disabled={loading}
                    className="w-full h-full p-4 md:p-5 rounded-2xl border border-slate-200 bg-white hover:bg-slate-100 hover:border-slate-300 text-slate-700 transition-colors shadow-sm disabled:opacity-60 disabled:cursor-not-allowed"
                  >
                    <p className="font-semibold text-slate-800 text-lg md:text-xl mb-1.5">
                      {suggestion.title}
                    </p>
                    <p className="text-sm md:text-base text-slate-500 mb-3 line-clamp-2 lg:line-clamp-none">
                      {suggestion.description}
                    </p>
                    <span className="inline-flex items-center text-blue-600 font-medium text-sm md:text-base">
                      Iniciar sessão
                    </span>
                  </button>
                ))}
              </div>

              <button
                onClick={() => setIsNewSessionModalOpen(true)}
                className="px-5 py-2.5 bg-blue-500 text-white rounded-xl hover:bg-blue-600 disabled:opacity-60"
                disabled={loading}
              >
                + Criar Nova Sessão
              </button>
            </div>
          </div>
        )}
        </div>
      </main>

      {isSidebarOpen && (
        <div className="fixed inset-0 z-40 md:hidden">
          <button
            type="button"
            className="absolute inset-0 bg-slate-900/40"
            onClick={() => setIsSidebarOpen(false)}
            aria-label="Fechar sessões"
          />

          <div className="absolute inset-y-0 left-0 w-[86%] max-w-sm bg-slate-50 shadow-xl">
            <button
              onClick={() => setIsSidebarOpen(false)}
              className="absolute top-3 right-3 h-9 w-9 rounded-lg border border-slate-200 bg-white text-slate-600 flex items-center justify-center z-10"
              aria-label="Fechar menu"
            >
              <XMarkIcon className="h-5 w-5" />
            </button>

            <SessionList
              sessions={sessions}
              activeSessionId={activeSessionId}
              onSelectSession={handleSelectSession}
              onDeleteSession={handleDeleteSession}
              onNewSession={() => {
                setIsSidebarOpen(false);
                setIsNewSessionModalOpen(true);
              }}
              className="w-full h-full"
            />
          </div>
        </div>
      )}

      {/* Modal */}
      <NewSessionModal
        isOpen={isNewSessionModalOpen}
        onClose={() => setIsNewSessionModalOpen(false)}
        onCreateSession={handleCreateSession}
        isLoading={loading}
      />
    </div>
  );
}
