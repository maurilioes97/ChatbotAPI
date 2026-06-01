import { ChangeEvent, FormEvent, useEffect, useState } from 'react';

interface InputAreaProps {
  onSendMessage: (message: string) => void;
  onAttachDocument: (files: File[]) => Promise<void>;
  onRemoveDocument: () => Promise<void>;
  isLoading: boolean;
  documentName?: string | null;
}

export const InputArea = ({
  onSendMessage,
  onAttachDocument,
  onRemoveDocument,
  isLoading,
  documentName,
}: InputAreaProps) => {
  const [message, setMessage] = useState('');
  const [fileName, setFileName] = useState<string | null>(documentName ?? null);

  useEffect(() => {
    setFileName(documentName ?? null);
  }, [documentName]);

  const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);

    if (files.length > 0) {
      setFileName(files.map((file) => file.name).join(', '));
      await onAttachDocument(files);
    }

    e.target.value = '';
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (message.trim()) {
      onSendMessage(message);
      setMessage('');
    }
  };

  const displayDocumentName = documentName ?? fileName;

  return (
    <form
      onSubmit={handleSubmit}
      className="p-2 md:p-4 border-t border-slate-200 bg-slate-100"
    >
      <div className="mx-auto w-full max-w-6xl space-y-2">
        {displayDocumentName && (
          <div className="inline-flex items-center gap-2 rounded-full border border-blue-200 bg-blue-50 px-3 py-1 text-xs md:text-sm text-blue-700">
            <span className="h-2 w-2 rounded-full bg-blue-500" />
            Documento ativo: {displayDocumentName}
            <button
              type="button"
              onClick={onRemoveDocument}
              disabled={isLoading}
              className="ml-1 inline-flex h-5 w-5 items-center justify-center rounded-full text-blue-700 hover:bg-blue-100 disabled:cursor-not-allowed disabled:opacity-50"
              aria-label="Remover documento"
              title="Remover documento"
            >
              ×
            </button>
          </div>
        )}

        <div className="flex items-center gap-1.5 md:gap-2 rounded-2xl bg-white border border-slate-300 px-2 md:px-3 py-2">
          <input
            id="document-upload-input"
            type="file"
            accept=".txt,.pdf"
            multiple
            onChange={handleFileChange}
            disabled={isLoading}
            className="hidden"
          />

          <label
            htmlFor="document-upload-input"
            className="h-10 w-10 md:h-11 md:w-11 shrink-0 rounded-xl border border-slate-200 text-slate-500 hover:bg-slate-100 disabled:bg-slate-300 disabled:cursor-not-allowed transition-colors flex items-center justify-center cursor-pointer"
          >
            <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="1.8">
              <path d="M21.44 11.05 12.25 20.24a6 6 0 1 1-8.49-8.49l9.19-9.19a4 4 0 1 1 5.66 5.66l-9.2 9.2a2 2 0 0 1-2.83-2.83l8.49-8.48" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </label>

          <input
            type="text"
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="Digite sua mensagem ou faça uma pergunta..."
            disabled={isLoading}
            className="flex-1 min-w-0 px-2 md:px-3 py-2 text-slate-700 placeholder:text-slate-400 bg-transparent focus:outline-none disabled:text-slate-400"
          />

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
      </div>
    </form>
  );
};
