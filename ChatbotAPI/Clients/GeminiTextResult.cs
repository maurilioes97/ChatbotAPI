namespace ChatbotAPI.Clients
{
    /// <summary>
    /// Resultado padrao das operacoes textuais feitas com o Gemini.
    /// </summary>
    public class GeminiTextResult
    {
        public bool Success { get; set; }
        public string? Text { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
