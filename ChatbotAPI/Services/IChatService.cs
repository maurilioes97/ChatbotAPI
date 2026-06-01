using Microsoft.AspNetCore.Http;
using ChatbotAPI.Models;

namespace ChatbotAPI.Services
{
    public interface IChatService
    {
        Task<int> CreateSessionAsync(string systemPrompt);
        Task<DocumentUploadResponse> UploadDocumentsAsync(int sessionId, IEnumerable<IFormFile> documents);
        Task<AudioTranscriptionResponse> TranscribeAudioAsync(int sessionId, IFormFile audio);
        Task<bool> ClearDocumentsAsync(int sessionId);
        Task<ChatResponse> SendMessageAsync(MensagemRequest request);
        Task<SummaryExportResponse> ExportSummaryPdfAsync(int sessionId);
    }
}
