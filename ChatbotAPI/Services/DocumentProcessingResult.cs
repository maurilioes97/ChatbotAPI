namespace ChatbotAPI.Services
{
    /// <summary>
    /// Resultado do processamento dos documentos enviados pelo usuario.
    /// </summary>
    public class DocumentProcessingResult
    {
        public bool Success { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentContext { get; set; }
        public int ExtractedCharacters { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
