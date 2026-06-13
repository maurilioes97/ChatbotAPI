using Microsoft.AspNetCore.Http;

namespace ChatbotAPI.Services
{
    /// <summary>
    /// Contrato do service responsavel por ler e preparar documentos.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Valida os arquivos recebidos, extrai o texto e monta um contexto reutilizavel.
        /// </summary>
        Task<DocumentProcessingResult> ProcessDocumentsAsync(IEnumerable<IFormFile> documents);
    }
}
