using ChatbotAPI.Contracts.Requests;
using Microsoft.AspNetCore.Http;

namespace ChatbotAPI.Services
{
    /// <summary>
    /// Contrato do service principal do chat.
    /// Aqui ficam as operacoes de alto nivel usadas pelo controller.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Cria e persiste uma nova sessao de conversa.
        /// </summary>
        Task<int> CreateSessionAsync(string systemPrompt);

        /// <summary>
        /// Processa os arquivos enviados e vincula o contexto extraido a sessao.
        /// </summary>
        Task<DocumentUploadResponse> UploadDocumentsAsync(int sessionId, IEnumerable<IFormFile> documents);

        /// <summary>
        /// Transcreve um audio enviado pelo usuario.
        /// </summary>
        Task<AudioTranscriptionResponse> TranscribeAudioAsync(int sessionId, IFormFile audio);

        /// <summary>
        /// Remove o documento associado a uma sessao.
        /// </summary>
        Task<bool> ClearDocumentsAsync(int sessionId);

        /// <summary>
        /// Envia uma mensagem para o fluxo de conversa e devolve a resposta gerada.
        /// </summary>
        Task<ChatResponse> SendMessageAsync(SendMessageRequest request);

        /// <summary>
        /// Gera um PDF com o resumo final da sessao.
        /// </summary>
        Task<SummaryExportResponse> ExportSummaryPdfAsync(int sessionId);
    }
}
