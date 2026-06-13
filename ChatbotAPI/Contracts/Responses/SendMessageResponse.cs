namespace ChatbotAPI.Contracts.Responses
{
    /// <summary>
    /// DTO com a resposta textual que volta para o front depois da conversa com o Gemini.
    /// </summary>
    public class SendMessageResponse
    {
        public string? Resposta { get; set; }
    }
}
