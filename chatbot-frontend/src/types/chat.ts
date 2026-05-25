export interface ChatSession {
    id: number;
    localSessionId: string;
    systemPrompt: string;
    createdAt: string;
    documentName?: string | null;
    suggestedQuestions?: string[];
}

export interface ChatMessage {
    id: number;
    sessionId: number;
    role: "User" | "Assistant" | "System";
    content: string;
    createdAt: string;
}

export interface ChatLocalStorage {
    sessions: ChatSession[];
    sessionMessages: Record<string, ChatMessage[]>;
    activeSessionId: string | null;
}

export interface MessageRequest {
    sessionId: number;
    texto: string;
}

export interface ApiResponse<T> {
    sessionId?: number;
    resposta?: T;
    erro?: string;
}
