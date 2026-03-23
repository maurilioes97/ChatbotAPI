import { useState } from 'react';
import { API_BASE_URL } from '../config/api';

export const useChatAPI = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const criarSessao = async (systemPrompt: string): Promise<number> => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_BASE_URL}/chat/nova-sessao`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(systemPrompt),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.erro || 'Erro ao criar sessão');
      }

      const data = await response.json();
      return data.sessionId;
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMsg);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const enviarMensagem = async (
    sessionId: number,
    texto: string
  ): Promise<string> => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_BASE_URL}/chat/enviar-mensagem`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sessionId, texto }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.erro || 'Erro ao enviar mensagem');
      }

      const data = await response.json();
      return data.resposta;
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMsg);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  return { criarSessao, enviarMensagem, loading, error };
};
