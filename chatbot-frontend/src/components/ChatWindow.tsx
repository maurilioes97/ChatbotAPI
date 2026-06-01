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
const EMPTY_SUGGESTIONS: string[] = [];
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
const MAX_TOTAL_UPLOAD_BYTES = 20 * 1024 * 1024;
const ALLOWED_EXTENSIONS = new Set(['.pdf', '.txt']);

export const ChatWindow = ({
  sessionId,
  messages,
  documentName,
  suggestedQuestions = [],
  onAddMessage,
  onDocumentUploaded,
}: ChatWindowProps) => {
  const { enviarMensagem, anexarDocumento, removerDocumento, exportarResumo, loading, error } = useChatAPI();
  const [localError, setLocalError] = useState<string | null>(error);
  const [typingMessage, setTypingMessage] = useState('');
  const [isAnimatingResponse, setIsAnimatingResponse] = useState(false);
  const [isExportingSummary, setIsExportingSummary] = useState(false);
  const [activeDocumentName, setActiveDocumentName] = useState<string | null>(
    documentName ?? null,
  );

  useEffect(() => {
    setLocalError(error);
  }, [error]);

  useEffect(() => {
    setActiveDocumentName(documentName ?? null);
  }, [documentName]);

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

      const invalidFile = files.find((file) => {
        const extension = file.name.includes('.')
          ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase()
          : '';
        return !ALLOWED_EXTENSIONS.has(extension);
      });

      if (invalidFile) {
        setLocalError(`Formato não suportado em '${invalidFile.name}'. Use apenas arquivos PDF ou TXT.`);
        return;
      }

      const oversizedFile = files.find((file) => file.size > MAX_FILE_SIZE_BYTES);
      if (oversizedFile) {
        setLocalError(`O arquivo '${oversizedFile.name}' excede o limite de 10 MB por arquivo.`);
        return;
      }

      const totalBytes = files.reduce((acc, file) => acc + file.size, 0);
      if (totalBytes > MAX_TOTAL_UPLOAD_BYTES) {
        setLocalError('O total dos arquivos excede o limite de 20 MB por envio.');
        return;
      }

      const result = await anexarDocumento(sessionId, files);
      const resolvedDocumentName = result.documentName || files.map((file) => file.name).join(', ');
      setActiveDocumentName(resolvedDocumentName);
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

  const handleExportSummary = async () => {
    try {
      setLocalError(null);
      setIsExportingSummary(true);
      await exportarResumo(sessionId);
    } catch (err) {
      setLocalError(
        err instanceof Error ? err.message : 'Erro ao exportar resumo',
      );
    } finally {
      setIsExportingSummary(false);
    }
  };

  const activeSuggestions = suggestedQuestions ?? EMPTY_SUGGESTIONS;

  return (
    <div className="flex flex-col h-full min-h-0 w-full bg-slate-50">
      <div className="px-3 md:px-4 pt-3 pb-3 bg-slate-50 border-b border-slate-200">
        <div className="mx-auto w-full max-w-6xl flex items-center justify-end">
          <button
            type="button"
            onClick={handleExportSummary}
            disabled={loading || isAnimatingResponse || isExportingSummary}
            aria-busy={isExportingSummary}
            className="group inline-flex items-center gap-1.5 rounded-xl border border-slate-800/10 bg-gradient-to-r from-slate-900 via-slate-800 to-slate-700 px-3 py-2 text-xs font-semibold text-white shadow-md shadow-slate-900/15 transition-all duration-200 hover:-translate-y-0.5 hover:from-slate-800 hover:via-slate-800 hover:to-slate-700 hover:shadow-lg hover:shadow-slate-900/20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-400 focus-visible:ring-offset-2 focus-visible:ring-offset-slate-50 active:translate-y-0 disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
          >
            {isExportingSummary ? (
              <span className="h-3.5 w-3.5 border-2 border-white border-t-transparent rounded-full animate-spin" />
            ) : (
              <svg
                className="h-3.5 w-3.5 transition-transform duration-200 group-hover:-translate-y-0.5"
                viewBox="0 0 24 24"
                fill="none"
                aria-hidden="true"
              >
                <path
                  d="M12 3v10m0 0 4-4m-4 4-4-4M5 14.5V17a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-2.5"
                  stroke="currentColor"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            )}
            <span className="whitespace-nowrap">Exportar resumo</span>
          </button>
        </div>
      </div>

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
