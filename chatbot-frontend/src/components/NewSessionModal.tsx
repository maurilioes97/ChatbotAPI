import { useState } from 'react';

interface NewSessionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreateSession: (systemPrompt: string) => void;
  isLoading: boolean;
}

export const NewSessionModal = ({
  isOpen,
  onClose,
  onCreateSession,
  isLoading,
}: NewSessionModalProps) => {
  const [prompt, setPrompt] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (prompt.trim()) {
      onCreateSession(prompt);
      setPrompt('');
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-lg max-w-md w-full mx-4">
        <div className="p-6">
          <h2 className="text-lg font-bold mb-4 text-gray-800">
            Nova Sessão de Chat
          </h2>
          <form onSubmit={handleSubmit}>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              System Prompt
            </label>
            <textarea
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              placeholder="Ex: Você é um assistente especializado em Python..."
              className="w-full h-32 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              disabled={isLoading}
            />
            <p className="text-xs text-gray-500 mt-2">
              Insira um prompt que guiará o comportamento da IA nesta sessão.
            </p>

            {/* Buttons */}
            <div className="flex gap-3 mt-6">
              <button
                type="button"
                onClick={onClose}
                disabled={isLoading}
                className="flex-1 px-4 py-2 bg-gray-300 text-gray-800 rounded-lg hover:bg-gray-400 disabled:bg-gray-200 transition-colors font-semibold"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={isLoading || !prompt.trim()}
                className="flex-1 px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors font-semibold"
              >
                {isLoading ? 'Criando...' : 'Criar'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
