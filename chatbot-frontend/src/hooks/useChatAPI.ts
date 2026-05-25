import { useState } from 'react';
import { API_BASE_URL } from '../config/api';

export const useChatAPI = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const readErrorMessage = async (response: Response, fallbackMessage: string) => {
    const rawBody = await response.text();

    if (!rawBody.trim()) {
      return fallbackMessage;
    }

    try {
      const parsed = JSON.parse(rawBody) as { erro?: string; error?: string };
      return parsed.erro || parsed.error || fallbackMessage;
    } catch {
      return rawBody;
    }
  };

  const anexarDocumento = async (
    sessionId: number,
    documentos: File[],
  ): Promise<{ documentName: string; extractedCharacters: number; suggestedQuestions: string[] }> => {
    setLoading(true);
    setError(null);

    try {
      const formData = new FormData();
      documentos.forEach((documento) => {
        formData.append('documentos', documento);
      });

      const response = await fetch(
        `${API_BASE_URL}/chat/sessao/${sessionId}/documento`,
        {
          method: 'POST',
          body: formData,
        },
      );

      if (!response.ok) {
        const errorMessage = await readErrorMessage(
          response,
          'Erro ao anexar documento',
        );
        throw new Error(errorMessage);
      }

      const data = await response.json();
      return {
        documentName: data.documentName,
        extractedCharacters: data.extractedCharacters,
        suggestedQuestions: data.suggestedQuestions ?? [],
      };
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMsg);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const removerDocumento = async (sessionId: number): Promise<void> => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(
        `${API_BASE_URL}/chat/sessao/${sessionId}/documento`,
        {
          method: 'DELETE',
        },
      );

      if (!response.ok) {
        const errorMessage = await readErrorMessage(
          response,
          'Erro ao remover documento',
        );
        throw new Error(errorMessage);
      }
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Erro desconhecido';
      setError(errorMsg);
      throw err;
    } finally {
      setLoading(false);
    }
  };

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
        const errorMessage = await readErrorMessage(
          response,
          'Erro ao criar sessão',
        );
        throw new Error(errorMessage);
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
        const errorMessage = await readErrorMessage(
          response,
          'Erro ao enviar mensagem',
        );
        throw new Error(errorMessage);
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

  return { criarSessao, anexarDocumento, removerDocumento, enviarMensagem, loading, error };
};
