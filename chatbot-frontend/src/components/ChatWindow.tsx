import { useEffect, useState } from 'react';
import { useChatAPI } from '../hooks/useChatAPI';
import { ChatMessage } from '../types/chat';
import { InputArea } from './InputArea.tsx';
import { MessageList } from './MessageList';

interface ChatWindowProps {
  sessionId: number;
  messages: ChatMessage[];
  documentName?: string | null;
  suggestedQuestions?: string[];
  onAddMessage: (message: ChatMessage) => void;
  onDocumentUploaded: (sessionId: number, documentName: string | null, suggestedQuestions: string[]) => Promise<void>;
}

const createMessageId = () => Date.now() * 1000 + Math.floor(Math.random() * 1000);

export const ChatWindow = ({
  sessionId,
  messages,
  documentName,
  suggestedQuestions = [],
  onAddMessage,
  onDocumentUploaded,
}: ChatWindowProps) => {
  const { enviarMensagem, anexarDocumento, removerDocumento, loading, error } = useChatAPI();
  const [localError, setLocalError] = useState<string | null>(error);
  const [typingMessage, setTypingMessage] = useState('');
  const [isAnimatingResponse, setIsAnimatingResponse] = useState(false);
  const [activeDocumentName, setActiveDocumentName] = useState<string | null>(
    documentName ?? null,
  );
  const [activeSuggestions, setActiveSuggestions] = useState<string[]>(suggestedQuestions);

  useEffect(() => {
    setLocalError(error);
  }, [error]);

  useEffect(() => {
    setActiveDocumentName(documentName ?? null);
  }, [documentName]);

  useEffect(() => {
    setActiveSuggestions(suggestedQuestions);
  }, [suggestedQuestions]);

  const animateAssistantMessage = async (fullText: string) => {
    if (!fullText) {
      return;
    }

    const charsPerTick = Math.max(1, Math.ceil(fullText.length / 120));
    const tickMs = 22;

    for (let index = charsPerTick; index <= fullText.length; index += charsPerTick) {
      setTypingMessage(fullText.slice(0, index));
      await new Promise((resolve) => setTimeout(resolve, tickMs));
    }

    setTypingMessage(fullText);
  };

  const handleSendMessage = async (text: string) => {
    try {
      setLocalError(null);

      // Adicionar mensagem do usuário imediatamente
      const userMessage: ChatMessage = {
        id: createMessageId(),
        sessionId,
        role: 'User',
        content: text,
        createdAt: new Date().toISOString(),
      };
      onAddMessage(userMessage);

      // Enviar para a API
      const response = await enviarMensagem(sessionId, text);

      setTypingMessage('');
      setIsAnimatingResponse(true);
      await animateAssistantMessage(response);

      // Adicionar resposta da IA
      const assistantMessage: ChatMessage = {
        id: createMessageId(),
        sessionId,
        role: 'Assistant',
        content: response,
        createdAt: new Date().toISOString(),
      };
      onAddMessage(assistantMessage);
      setTypingMessage('');
      setIsAnimatingResponse(false);
    } catch (err) {
      setTypingMessage('');
      setIsAnimatingResponse(false);
      setLocalError(
        err instanceof Error ? err.message : 'Erro ao enviar mensagem'
      );
    }
  };

  const handleAttachDocument = async (files: File[]) => {
    try {
      setLocalError(null);
      const result = await anexarDocumento(sessionId, files);
      const resolvedDocumentName = result.documentName || files.map((file) => file.name).join(', ');
      setActiveDocumentName(resolvedDocumentName);
      setActiveSuggestions(result.suggestedQuestions);
      await onDocumentUploaded(sessionId, resolvedDocumentName, result.suggestedQuestions);
    } catch (err) {
      setLocalError(
        err instanceof Error ? err.message : 'Erro ao anexar documento',
      );
    }
  };

  const handleRemoveDocument = async () => {
    try {
      setLocalError(null);
      await removerDocumento(sessionId);
      setActiveDocumentName(null);
      setActiveSuggestions([]);
      await onDocumentUploaded(sessionId, null, []);
    } catch (err) {
      setLocalError(
        err instanceof Error ? err.message : 'Erro ao remover documento',
      );
    }
  };

  const handleSuggestionClick = (question: string) => {
    void handleSendMessage(question);
  };

  return (
    <div className="flex flex-col h-full min-h-0 w-full bg-slate-50">
      <MessageList
        messages={messages}
        isTyping={loading && !typingMessage}
        typingMessage={typingMessage}
        documentName={activeDocumentName}
      />
      {localError && (
        <div className="p-4 bg-red-100 text-red-700 border-t border-red-200">
          Erro: {localError}
        </div>
      )}
      {activeSuggestions.length > 0 && (
        <div className="px-3 md:px-4 pt-3 bg-slate-50 border-t border-slate-200">
          <div className="mx-auto w-full max-w-6xl">
            <p className="mb-2 text-xs md:text-sm font-semibold uppercase tracking-wide text-slate-500">
              Perguntas sugeridas
            </p>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              {activeSuggestions.map((question) => (
                <button
                  key={question}
                  type="button"
                  onClick={() => handleSuggestionClick(question)}
                  disabled={loading || isAnimatingResponse}
                  className="rounded-2xl border border-blue-200 bg-white px-4 py-3 text-left text-sm md:text-base text-slate-700 shadow-sm hover:border-blue-300 hover:bg-blue-50 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
                >
                  <span className="block text-xs font-semibold uppercase tracking-wide text-blue-600 mb-1">
                    Sugestão
                  </span>
                  <span>{question}</span>
                </button>
              ))}
            </div>
          </div>
        </div>
      )}
      <InputArea
        onSendMessage={handleSendMessage}
        onAttachDocument={handleAttachDocument}
        onRemoveDocument={handleRemoveDocument}
        isLoading={loading || isAnimatingResponse}
        documentName={activeDocumentName}
      />
    </div>
  );
};
