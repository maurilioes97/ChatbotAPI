using ChatbotAPI.Data;
using ChatbotAPI.Models;
using ChatbotAPI.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace ChatbotAPI.Services
{
    public class ChatResponse
    {
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DocumentUploadResponse
    {
        public bool Success { get; set; }
        public string? DocumentName { get; set; }
        public int ExtractedCharacters { get; set; }
        public List<string> SuggestedQuestions { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly GeminiOptions _geminiOptions;
        private const int MaxDocumentContextChars = 50000;

        public ChatService(AppDbContext context, IOptions<GeminiOptions> geminiOptions)
        {
            _context = context;
            _geminiOptions = geminiOptions.Value;
        }

        public async Task<int> CreateSessionAsync(string systemPrompt)
        {
            var sessao = new ChatSession { SystemPrompt = systemPrompt };
            _context.ChatSessions.Add(sessao);
            await _context.SaveChangesAsync();
            return sessao.Id;
        }

        public async Task<DocumentUploadResponse> UploadDocumentsAsync(int sessionId, IEnumerable<IFormFile> documents)
        {
            var validDocuments = documents?
                .Where(document => document is not null && document.Length > 0)
                .ToList() ?? new List<IFormFile>();

            if (validDocuments.Count == 0)
            {
                return new DocumentUploadResponse
                {
                    Success = false,
                    ErrorMessage = "Nenhum arquivo foi enviado."
                };
            }

            var sessao = await _context.ChatSessions.FindAsync(sessionId);
            if (sessao is null)
            {
                return new DocumentUploadResponse
                {
                    Success = false,
                    ErrorMessage = "Sessão não encontrada."
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
                    return new DocumentUploadResponse
                    {
                        Success = false,
                        ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                            ? $"Falha ao ler o arquivo '{document.FileName}'."
                            : $"{document.FileName}: {ex.Message}"
                    };
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new DocumentUploadResponse
                    {
                        Success = false,
                        ErrorMessage = $"Não foi possível extrair texto do documento '{document.FileName}'."
                    };
                }

                var trimmedText = TrimDocumentContext(extractedText);
                documentParts.Add($"### {document.FileName}\n{trimmedText}");
                documentNames.Add(document.FileName);
                totalCharacters += trimmedText.Length;
            }

            sessao.DocumentName = string.Join(", ", documentNames);
            sessao.DocumentContext = TrimDocumentContext(string.Join("\n\n", documentParts));

            var suggestedQuestions = await GenerateSuggestedQuestionsAsync(sessao);

            await _context.SaveChangesAsync();

            return new DocumentUploadResponse
            {
                Success = true,
                DocumentName = sessao.DocumentName,
                ExtractedCharacters = totalCharacters,
                SuggestedQuestions = suggestedQuestions
            };
        }

        public async Task<bool> ClearDocumentsAsync(int sessionId)
        {
            var sessao = await _context.ChatSessions.FindAsync(sessionId);
            if (sessao is null)
            {
                return false;
            }

            sessao.DocumentName = null;
            sessao.DocumentContext = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ChatResponse> SendMessageAsync(MensagemRequest request)
        {
            // Salva a mensagem do usuário
            var mensagemUsuario = new ChatMessage
            {
                SessionId = request.SessionId,
                Role = "User",
                Content = request.Texto
            };
            _context.ChatMessages.Add(mensagemUsuario);
            await _context.SaveChangesAsync();

            // Busca sessão e histórico
            var sessao = await _context.ChatSessions.FindAsync(request.SessionId);
            if (sessao is null)
            {
                return new ChatResponse
                {
                    Success = false,
                    ErrorMessage = "Sessão não encontrada."
                };
            }

            var historico = await _context.ChatMessages
                .Where(m => m.SessionId == request.SessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var systemPrompt = BuildSystemPrompt(sessao);

            // Monta payload
            var conteudos = new List<object>();
            foreach (var msg in historico)
            {
                string roleGemini = msg.Role == "User" ? "user" : "model";
                conteudos.Add(new { role = roleGemini, parts = new[] { new { text = msg.Content } } });
            }

            var payload = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = conteudos
            };

            if (string.IsNullOrWhiteSpace(_geminiOptions.ApiKey))
            {
                return new ChatResponse
                {
                    Success = false,
                    ErrorMessage = "A chave do Gemini não foi configurada. Defina Gemini__ApiKey em variáveis de ambiente ou user-secrets."
                };
            }

            string url = $"{_geminiOptions.BaseUrl}/{_geminiOptions.Model}:generateContent?key={_geminiOptions.ApiKey}";

            using var httpClient = new HttpClient();
            var jsonEnviado = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonEnviado, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            var jsonRetornado = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ChatResponse
                {
                    Success = false,
                    ErrorMessage = jsonRetornado
                };
            }

            using var doc = JsonDocument.Parse(jsonRetornado);
            string respostaDaIA = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()!;

            var mensagemIA = new ChatMessage
            {
                SessionId = request.SessionId,
                Role = "Assistant",
                Content = respostaDaIA
            };
            _context.ChatMessages.Add(mensagemIA);
            await _context.SaveChangesAsync();

            return new ChatResponse { Success = true, Response = respostaDaIA };
        }

        private static string BuildSystemPrompt(ChatSession sessao)
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(sessao.SystemPrompt))
            {
                partes.Add(sessao.SystemPrompt.Trim());
            }

            if (!string.IsNullOrWhiteSpace(sessao.DocumentContext))
            {
                partes.Add(
                    "Use o conteúdo do documento abaixo como contexto principal para responder ao usuário. " +
                    "Se a resposta não estiver no documento, diga isso com clareza e não invente informações.");

                if (!string.IsNullOrWhiteSpace(sessao.DocumentName))
                {
                    partes.Add($"Documento: {sessao.DocumentName}");
                }

                partes.Add(sessao.DocumentContext.Trim());
            }

            return string.Join("\n\n", partes);
        }

        private async Task<string> ExtractDocumentTextAsync(IFormFile document)
        {
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();

            return extension switch
            {
                ".txt" => await ReadPlainTextAsync(document),
                ".csv" => await ReadPlainTextAsync(document),
                ".pdf" => await ReadPdfTextAsync(document),
                _ => throw new InvalidOperationException("Formato não suportado. Use arquivos TXT, PDF ou CSV.")
            };
        }

        private static async Task<string> ReadPlainTextAsync(IFormFile document)
        {
            using var stream = document.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync();
        }

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

        private static string TrimDocumentContext(string text)
        {
            if (text.Length <= MaxDocumentContextChars)
            {
                return text;
            }

            return text[..MaxDocumentContextChars];
        }

        private async Task<List<string>> GenerateSuggestedQuestionsAsync(ChatSession sessao)
        {
            if (string.IsNullOrWhiteSpace(_geminiOptions.ApiKey) || string.IsNullOrWhiteSpace(sessao.DocumentContext))
            {
                return GetFallbackQuestions(sessao);
            }

            var prompt =
                "Leia o documento abaixo e sugira exatamente 3 perguntas inteligentes que o usuário pode fazer sobre ele. " +
                "As perguntas devem ser curtas, objetivas e diretamente relacionadas ao conteúdo. " +
                "Responda somente com as 3 perguntas, uma por linha, sem numeração, sem marcadores e sem explicações.\n\n" +
                sessao.DocumentContext;

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            string url = $"{_geminiOptions.BaseUrl}/{_geminiOptions.Model}:generateContent?key={_geminiOptions.ApiKey}";

            try
            {
                using var httpClient = new HttpClient();
                var jsonEnviado = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonEnviado, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);
                var jsonRetornado = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return GetFallbackQuestions(sessao);
                }

                using var doc = JsonDocument.Parse(jsonRetornado);
                var rawText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                var questions = (rawText ?? string.Empty)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(NormalizeSuggestedQuestion)
                    .Where(question => !string.IsNullOrWhiteSpace(question))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

                if (questions.Count < 3)
                {
                    questions.AddRange(GetFallbackQuestions(sessao).Where(q => !questions.Contains(q, StringComparer.OrdinalIgnoreCase)));
                }

                return questions.Take(3).ToList();
            }
            catch
            {
                return GetFallbackQuestions(sessao);
            }
        }

        private static string NormalizeSuggestedQuestion(string line)
        {
            var cleaned = line.Trim();
            cleaned = Regex.Replace(cleaned, @"^[-*•\s]+", string.Empty);
            cleaned = Regex.Replace(cleaned, @"^\d+[\).\-:\s]+", string.Empty);
            return cleaned.Trim();
        }

        private static List<string> GetFallbackQuestions(ChatSession sessao)
        {
            var subject = string.IsNullOrWhiteSpace(sessao.DocumentName)
                ? "este documento"
                : sessao.DocumentName.Split(',')[0].Trim();

            return new List<string>
            {
                $"Qual é o objetivo principal de {subject}?",
                $"Quais pontos mais importantes eu devo entender em {subject}?",
                $"Que informações de {subject} merecem atenção especial?"
            };
        }
    }
}
