namespace ChatbotAPI.Contracts.Responses
{
    /// <summary>
    /// DTO devolvido apos o processamento de documentos.
    /// </summary>
    public class UploadDocumentResponse
    {
        public string? DocumentName { get; set; }
        public int ExtractedCharacters { get; set; }
        public List<string> SuggestedQuestions { get; set; } = new();
    }
}
