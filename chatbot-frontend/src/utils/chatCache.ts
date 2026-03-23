import { ChatLocalStorage, ChatMessage, ChatSession } from "../types/chat";

const CHAT_STORAGE_KEY = "chatbot-ui-state";
const CHAT_CACHE_TTL_MS = 4 * 24 * 60 * 60 * 1000;

type PersistedChatState = ChatLocalStorage;

interface PersistedChatCache {
    data: PersistedChatState;
    updatedAt: string;
}

const createEmptyState = (): PersistedChatState => ({
    sessions: [],
    sessionMessages: {},
    activeSessionId: null,
});

export const createLocalSessionId = () => {
    if (
        typeof crypto !== "undefined" &&
        typeof crypto.randomUUID === "function"
    ) {
        return crypto.randomUUID();
    }

    return `local-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
};

const isMessageRole = (value: unknown): value is ChatMessage["role"] => {
    return value === "User" || value === "Assistant" || value === "System";
};

const sanitizeMessages = (messages: unknown): ChatMessage[] => {
    if (!Array.isArray(messages)) {
        return [];
    }

    return messages.filter(
        (message): message is ChatMessage =>
            typeof message === "object" &&
            message !== null &&
            typeof (message as ChatMessage).id === "number" &&
            typeof (message as ChatMessage).sessionId === "number" &&
            isMessageRole((message as ChatMessage).role) &&
            typeof (message as ChatMessage).content === "string" &&
            typeof (message as ChatMessage).createdAt === "string",
    );
};

export const loadPersistedChatState = (): PersistedChatState => {
    if (typeof window === "undefined") {
        return createEmptyState();
    }

    try {
        const rawState = window.localStorage.getItem(CHAT_STORAGE_KEY);

        if (!rawState) {
            return createEmptyState();
        }

        const parsedCache = JSON.parse(rawState) as {
            data?: unknown;
            updatedAt?: unknown;
            sessions?: unknown;
            sessionMessages?: unknown;
            activeSessionId?: unknown;
        };

        const hasCacheEnvelope =
            typeof parsedCache === "object" &&
            parsedCache !== null &&
            "data" in parsedCache &&
            "updatedAt" in parsedCache;

        if (hasCacheEnvelope) {
            const updatedAtMs =
                typeof parsedCache.updatedAt === "string"
                    ? new Date(parsedCache.updatedAt).getTime()
                    : NaN;

            if (
                !Number.isFinite(updatedAtMs) ||
                Date.now() - updatedAtMs > CHAT_CACHE_TTL_MS
            ) {
                window.localStorage.removeItem(CHAT_STORAGE_KEY);
                return createEmptyState();
            }
        }

        const parsedState = (
            hasCacheEnvelope ? parsedCache.data : parsedCache
        ) as {
            sessions?: unknown;
            sessionMessages?: unknown;
            activeSessionId?: unknown;
        };

        const sessions = Array.isArray(parsedState.sessions)
            ? parsedState.sessions
                  .filter(
                      (session): session is ChatSession =>
                          typeof session?.id === "number" &&
                          typeof session?.systemPrompt === "string" &&
                          typeof session?.createdAt === "string",
                  )
                  .map((session) => ({
                      ...session,
                      localSessionId:
                          typeof session.localSessionId === "string"
                              ? session.localSessionId
                              : createLocalSessionId(),
                  }))
            : [];

        const rawSessionMessages =
            parsedState.sessionMessages &&
            typeof parsedState.sessionMessages === "object"
                ? parsedState.sessionMessages
                : {};

        const sessionMessages: Record<string, ChatMessage[]> = {};

        Object.entries(rawSessionMessages).forEach(([key, messages]) => {
            const targetSession =
                sessions.find((session) => session.localSessionId === key) ||
                sessions.find((session) => String(session.id) === key);

            const storageKey = targetSession?.localSessionId ?? key;
            sessionMessages[storageKey] = sanitizeMessages(messages);
        });

        let activeSessionId: string | null = null;
        const activeSessionIdRaw = parsedState.activeSessionId;

        if (
            typeof activeSessionIdRaw === "string" &&
            sessions.some(
                (session) => session.localSessionId === activeSessionIdRaw,
            )
        ) {
            activeSessionId = activeSessionIdRaw;
        }

        if (typeof activeSessionIdRaw === "number") {
            const activeSession = sessions.find(
                (session) => session.id === activeSessionIdRaw,
            );
            activeSessionId = activeSession?.localSessionId ?? null;
        }

        return {
            sessions,
            sessionMessages,
            activeSessionId,
        };
    } catch {
        return createEmptyState();
    }
};

export const savePersistedChatState = (state: PersistedChatState) => {
    if (typeof window === "undefined") {
        return;
    }

    const persistedCache: PersistedChatCache = {
        data: state,
        updatedAt: new Date().toISOString(),
    };

    window.localStorage.setItem(
        CHAT_STORAGE_KEY,
        JSON.stringify(persistedCache),
    );
};
