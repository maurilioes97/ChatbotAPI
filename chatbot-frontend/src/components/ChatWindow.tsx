import { useEffect, useState } from 'react';
import { useChatAPI } from '../hooks/useChatAPI';
import { ChatMessage } from '../types/chat';
import { InputArea } from './InputArea';
import { MessageList } from './MessageList';

interface ChatWindowProps {
  sessionId: number;
  messages: ChatMessage[];
  onAddMessage: (message: ChatMessage) => void;
}

const createMessageId = () => Date.now() * 1000 + Math.floor(Math.random() * 1000);

export const ChatWindow = ({
  sessionId,
  messages,
  onAddMessage,
}: ChatWindowProps) => {
  const { enviarMensagem, loading, error } = useChatAPI();
  const [localError, setLocalError] = useState<string | null>(error);
  const [typingMessage, setTypingMessage] = useState('');
  const [isAnimatingResponse, setIsAnimatingResponse] = useState(false);

  useEffect(() => {
    setLocalError(error);
  }, [error]);

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

  return (
    <div className="flex flex-col h-full min-h-0 w-full bg-slate-50">
      <MessageList
        messages={messages}
        isTyping={loading && !typingMessage}
        typingMessage={typingMessage}
      />
      {localError && (
        <div className="p-4 bg-red-100 text-red-700 border-t border-red-200">
          Erro: {localError}
        </div>
      )}
      <InputArea
        onSendMessage={handleSendMessage}
        isLoading={loading || isAnimatingResponse}
      />
    </div>
  );
};
