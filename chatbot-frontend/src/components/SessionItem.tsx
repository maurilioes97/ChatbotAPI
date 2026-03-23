import { ChatSession } from '../types/chat';

interface SessionItemProps {
  session: ChatSession;
  isActive: boolean;
  onClick: (localSessionId: string) => void;
  onDelete: (localSessionId: string) => void;
}

export const SessionItem = ({
  session,
  isActive,
  onClick,
  onDelete,
}: SessionItemProps) => {
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: 'short',
    });
  };

  const truncatePrompt = (prompt: string, maxLength: number = 30) => {
    return prompt.length > maxLength
      ? prompt.substring(0, maxLength) + '...'
      : prompt;
  };

  return (
    <div
      onClick={() => onClick(session.localSessionId)}
      className={`p-3 cursor-pointer rounded-xl border transition-colors ${
        isActive
          ? 'bg-blue-100 border-blue-200 text-slate-800'
          : 'bg-white border-slate-200 hover:bg-slate-50 text-slate-800'
      }`}
    >
      <div className="flex justify-between items-start gap-2">
        <div className="flex-1 min-w-0">
          <p className="font-semibold text-base truncate">
            {truncatePrompt(session.systemPrompt)}
          </p>
          <p
            className={`text-xs mt-1 ${
              isActive ? 'text-slate-600' : 'text-slate-500'
            }`}
          >
            {formatDate(session.createdAt)}
          </p>
        </div>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete(session.localSessionId);
          }}
          className="h-8 w-8 flex items-center justify-center rounded-lg text-xl leading-none bg-pink-100 hover:bg-pink-200 text-pink-500"
        >
          ✕
        </button>
      </div>
    </div>
  );
};
