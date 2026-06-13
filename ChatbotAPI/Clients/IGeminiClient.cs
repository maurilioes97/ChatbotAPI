namespace ChatbotAPI.Clients
{
    /// <summary>
    /// Contrato do cliente que conversa diretamente com a API do Gemini.
    /// </summary>
    public interface IGeminiClient
    {
        /// <summary>
        /// Indica se a aplicacao possui chave configurada para chamar o Gemini.
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Gera a resposta principal do chat com base no prompt e no historico.
        /// </summary>
        Task<GeminiTextResult> GenerateChatReplyAsync(string systemPrompt, IReadOnlyList<GeminiChatMessage> history);

        /// <summary>
        /// Sugere perguntas que o usuario pode fazer sobre o documento anexado.
        /// </summary>
        Task<GeminiTextResult> GenerateSuggestedQuestionsAsync(string documentContext);

        /// <summary>
        /// Gera um resumo estruturado da conversa para exportacao.
        /// </summary>
        Task<GeminiTextResult> GenerateSummaryAsync(string documentName, string transcript);

        /// <summary>
        /// Envia um audio para transcricao em texto.
        /// </summary>
        Task<GeminiTextResult> TranscribeAudioAsync(string base64Audio, string mimeType);
    }
}
