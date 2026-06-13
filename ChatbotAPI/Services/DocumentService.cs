using Microsoft.AspNetCore.Http;
using System.Text;
using UglyToad.PdfPig;

namespace ChatbotAPI.Services
{
    /// <summary>
    /// Service focado em validar, ler e condensar os documentos enviados pelo usuario.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private const int MaxDocumentContextChars = 50000;
        private const long MaxFileSizeBytes = 10L * 1024 * 1024;
        private const long MaxTotalUploadBytes = 20L * 1024 * 1024;

        /// <summary>
        /// Recebe os arquivos, aplica validacoes e monta o texto consolidado que sera usado como contexto.
        /// </summary>
        public async Task<DocumentProcessingResult> ProcessDocumentsAsync(IEnumerable<IFormFile> documents)
        {
            var validDocuments = documents?
                .Where(document => document is not null && document.Length > 0)
                .ToList() ?? new List<IFormFile>();

            if (validDocuments.Count == 0)
            {
                return new DocumentProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Nenhum arquivo foi enviado."
                };
            }

            var oversizedDocument = validDocuments.FirstOrDefault(document => document.Length > MaxFileSizeBytes);
            if (oversizedDocument is not null)
            {
                return new DocumentProcessingResult
                {
                    Success = false,
                    ErrorMessage = $"O arquivo '{oversizedDocument.FileName}' excede o limite de 10 MB por arquivo."
                };
            }

            var totalBytes = validDocuments.Sum(document => document.Length);
            if (totalBytes > MaxTotalUploadBytes)
            {
                return new DocumentProcessingResult
                {
                    Success = false,
                    ErrorMessage = "O total dos arquivos excede o limite de 20 MB por envio."
                };
            }

            var documentParts = new List<string>();
            var documentNames = new List<string>();
            var totalCharacters = 0;

            foreach (var document in validDocuments)
            {
                string extractedText;
                try
                {
                    extractedText = await ExtractDocumentTextAsync(document);
                }
                catch (Exception ex)
                {
                    return new DocumentProcessingResult
                    {
                        Success = false,
                        ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                            ? $"Falha ao ler o arquivo '{document.FileName}'."
                            : $"{document.FileName}: {ex.Message}"
                    };
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new DocumentProcessingResult
                    {
                        Success = false,
                        ErrorMessage = $"Nao foi possivel extrair texto do documento '{document.FileName}'."
                    };
                }

                var trimmedText = TrimDocumentContext(extractedText);
                documentParts.Add($"### {document.FileName}\n{trimmedText}");
                documentNames.Add(document.FileName);
                totalCharacters += trimmedText.Length;
            }

            return new DocumentProcessingResult
            {
                Success = true,
                DocumentName = string.Join(", ", documentNames),
                DocumentContext = TrimDocumentContext(string.Join("\n\n", documentParts)),
                ExtractedCharacters = totalCharacters
            };
        }

        /// <summary>
        /// Decide qual leitor usar com base na extensao do arquivo.
        /// </summary>
        private async Task<string> ExtractDocumentTextAsync(IFormFile document)
        {
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();

            return extension switch
            {
                ".txt" => await ReadPlainTextAsync(document),
                ".pdf" => await ReadPdfTextAsync(document),
                _ => throw new InvalidOperationException("Formato nao suportado. Use apenas arquivos TXT ou PDF.")
            };
        }

        /// <summary>
        /// Le um arquivo TXT simples preservando a codificacao mais comum.
        /// </summary>
        private static async Task<string> ReadPlainTextAsync(IFormFile document)
        {
            using var stream = document.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Extrai o texto bruto de cada pagina de um PDF.
        /// </summary>
        private static async Task<string> ReadPdfTextAsync(IFormFile document)
        {
            await using var sourceStream = document.OpenReadStream();
            await using var memoryStream = new MemoryStream();

            await sourceStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var pdf = PdfDocument.Open(memoryStream);
            var builder = new StringBuilder();

            foreach (var page in pdf.GetPages())
            {
                var pageText = page.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    builder.AppendLine(pageText);
                    builder.AppendLine();
                }
            }

            return builder.ToString().Trim();
        }

        /// <summary>
        /// Corta textos muito grandes para manter o contexto dentro de um limite seguro.
        /// </summary>
        private static string TrimDocumentContext(string text)
        {
            if (text.Length <= MaxDocumentContextChars)
            {
                return text;
            }

            return text[..MaxDocumentContextChars];
        }
    }
}
