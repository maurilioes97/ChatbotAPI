using Microsoft.AspNetCore.Http;
using ChatbotAPI.Models;

namespace ChatbotAPI.Services
{
    public interface IChatService
    {
        Task<int> CreateSessionAsync(string systemPrompt);
        Task<DocumentUploadResponse> UploadDocumentsAsync(int sessionId, IEnumerable<IFormFile> documents);
        Task<bool> ClearDocumentsAsync(int sessionId);
        Task<ChatResponse> SendMessageAsync(MensagemRequest request);
    }
}
