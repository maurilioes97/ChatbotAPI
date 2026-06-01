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

    public class SummaryExportResponse
    {
        public bool Success { get; set; }
        public byte[]? FileContent { get; set; }
        public string? FileName { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AudioTranscriptionResponse
    {
        public bool Success { get; set; }
        public string? Transcript { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class GeminiContentPart
    {
        [System.Text.Json.Serialization.JsonPropertyName("text")]
        public string? Text { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("inline_data")]
        public GeminiInlineData? InlineData { get; set; }
    }

    public class GeminiInlineData
    {
        [System.Text.Json.Serialization.JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    public class GeminiContent
    {
        [System.Text.Json.Serialization.JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [System.Text.Json.Serialization.JsonPropertyName("parts")]
        public List<GeminiContentPart> Parts { get; set; } = new();
    }

    public class GeminiGenerateContentRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("generationConfig")]
        public object? GenerationConfig { get; set; }
    }

    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly GeminiOptions _geminiOptions;
        private const int MaxDocumentContextChars = 50000;
        private const long MaxFileSizeBytes = 10L * 1024 * 1024;
        private const long MaxTotalUploadBytes = 20L * 1024 * 1024;
        private const long MaxAudioSizeBytes = 15L * 1024 * 1024;
        private static readonly HashSet<string> AllowedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".wav", ".mp3", ".m4a", ".ogg", ".webm"
        };

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

            var oversizedDocument = validDocuments.FirstOrDefault(document => document.Length > MaxFileSizeBytes);
            if (oversizedDocument is not null)
            {
                return new DocumentUploadResponse
                {
                    Success = false,
                    ErrorMessage = $"O arquivo '{oversizedDocument.FileName}' excede o limite de 10 MB por arquivo."
                };
            }

            var totalBytes = validDocuments.Sum(document => document.Length);
            if (totalBytes > MaxTotalUploadBytes)
            {
                return new DocumentUploadResponse
                {
                    Success = false,
                    ErrorMessage = "O total dos arquivos excede o limite de 20 MB por envio."
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

        public async Task<AudioTranscriptionResponse> TranscribeAudioAsync(int sessionId, IFormFile audio)
        {
            if (audio is null || audio.Length <= 0)
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Nenhum áudio foi enviado."
                };
            }

            if (audio.Length > MaxAudioSizeBytes)
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "O áudio excede o limite de 15 MB por envio."
                };
            }

            var sessaoExists = await _context.ChatSessions.AnyAsync(s => s.Id == sessionId);
            if (!sessaoExists)
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Sessão não encontrada."
                };
            }

            var extension = Path.GetExtension(audio.FileName).ToLowerInvariant();
            if (!AllowedAudioExtensions.Contains(extension))
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Formato de áudio não suportado. Use WAV, MP3, M4A, OGG ou WEBM."
                };
            }

            if (string.IsNullOrWhiteSpace(_geminiOptions.ApiKey))
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "A chave do Gemini não foi configurada."
                };
            }

            await using var audioStream = audio.OpenReadStream();
            await using var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream);
            var base64Audio = Convert.ToBase64String(memoryStream.ToArray());

            var mimeType = ResolveAudioMimeType(audio.FileName, audio.ContentType);

            var payload = new GeminiGenerateContentRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Role = "user",
                        Parts = new List<GeminiContentPart>
                        {
                            new GeminiContentPart
                            {
                                Text = "Transcreva o áudio para texto em português do Brasil. Responda apenas com a transcrição, sem comentários."
                            },
                            new GeminiContentPart
                            {
                                InlineData = new GeminiInlineData
                                {
                                    MimeType = mimeType,
                                    Data = base64Audio
                                }
                            }
                        }
                    }
                },
                GenerationConfig = new { temperature = 0.0 }
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
                    return new AudioTranscriptionResponse
                    {
                        Success = false,
                        ErrorMessage = string.IsNullOrWhiteSpace(jsonRetornado)
                            ? $"Não foi possível transcrever o áudio no momento. ({(int)response.StatusCode} {response.ReasonPhrase})"
                            : $"Não foi possível transcrever o áudio no momento. {jsonRetornado}"
                    };
                }

                using var doc = JsonDocument.Parse(jsonRetornado);
                var transcript = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    return new AudioTranscriptionResponse
                    {
                        Success = false,
                        ErrorMessage = "Não foi possível entender o áudio enviado."
                    };
                }

                return new AudioTranscriptionResponse
                {
                    Success = true,
                    Transcript = transcript.Trim()
                };
            }
            catch
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Erro ao processar o áudio. Tente novamente."
                };
            }
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

        public async Task<SummaryExportResponse> ExportSummaryPdfAsync(int sessionId)
        {
            var sessao = await _context.ChatSessions.FindAsync(sessionId);
            if (sessao is null)
            {
                return new SummaryExportResponse
                {
                    Success = false,
                    ErrorMessage = "Sessão não encontrada."
                };
            }

            var historico = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            if (historico.Count == 0 && string.IsNullOrWhiteSpace(sessao.DocumentContext))
            {
                return new SummaryExportResponse
                {
                    Success = false,
                    ErrorMessage = "Não há conteúdo suficiente para gerar o resumo."
                };
            }

            var summaryMarkdown = await GenerateSummaryMarkdownAsync(sessao, historico);
            var pdfBytes = SimplePdfWriter.BuildSummaryPdf(
                title: "Encerrar e Exportar Resumo",
                subtitle: BuildSummarySubtitle(sessao, historico.Count),
                markdownContent: summaryMarkdown,
                footerText: $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}"
            );

            var fileName = BuildSummaryFileName(sessao);

            return new SummaryExportResponse
            {
                Success = true,
                FileContent = pdfBytes,
                FileName = fileName
            };
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
                ".pdf" => await ReadPdfTextAsync(document),
                _ => throw new InvalidOperationException("Formato não suportado. Use apenas arquivos TXT ou PDF.")
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

        private static string ResolveAudioMimeType(string fileName, string? contentType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
                ? string.Empty
                : contentType.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

            return extension switch
            {
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".ogg" => "audio/ogg",
                ".webm" => "audio/webm",
                _ => string.IsNullOrWhiteSpace(normalizedContentType) ? "audio/webm" : normalizedContentType,
            };
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

        private async Task<string> GenerateSummaryMarkdownAsync(ChatSession sessao, IReadOnlyList<ChatMessage> historico)
        {
            var transcript = BuildConversationTranscript(historico);
            if (string.IsNullOrWhiteSpace(_geminiOptions.ApiKey) || string.IsNullOrWhiteSpace(transcript))
            {
                return BuildFallbackSummaryMarkdown(sessao, historico);
            }

            var prompt =
                "Você vai resumir uma conversa sobre um documento. Gere um resumo executivo em português, usando markdown simples e SOMENTE esta estrutura: \n" +
                "# Resumo Executivo\n" +
                "## Contexto\n" +
                "## Pontos principais\n" +
                "- ...\n" +
                "## Conclusão\n" +
                "## Próximos passos\n" +
                "- ...\n\n" +
                "Regras: seja objetivo, não invente informações, use frases curtas e claras.\n\n" +
                $"Documento: {sessao.DocumentName ?? "Documento sem nome"}\n\n" +
                $"Conversa:\n{transcript}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new { temperature = 0.25 }
            };

            try
            {
                string url = $"{_geminiOptions.BaseUrl}/{_geminiOptions.Model}:generateContent?key={_geminiOptions.ApiKey}";

                using var httpClient = new HttpClient();
                var jsonEnviado = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonEnviado, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);
                var jsonRetornado = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return BuildFallbackSummaryMarkdown(sessao, historico);
                }

                using var doc = JsonDocument.Parse(jsonRetornado);
                var rawText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                var normalized = NormalizeSummaryMarkdown(rawText ?? string.Empty);
                return string.IsNullOrWhiteSpace(normalized)
                    ? BuildFallbackSummaryMarkdown(sessao, historico)
                    : normalized;
            }
            catch
            {
                return BuildFallbackSummaryMarkdown(sessao, historico);
            }
        }

        private static string BuildConversationTranscript(IReadOnlyList<ChatMessage> historico)
        {
            if (historico.Count == 0)
            {
                return string.Empty;
            }

            var transcript = new StringBuilder();
            foreach (var message in historico)
            {
                var label = message.Role == "User" ? "Usuário" : message.Role == "Assistant" ? "Assistente" : message.Role;
                transcript.AppendLine($"{label}: {message.Content}");
                transcript.AppendLine();
            }

            var text = transcript.ToString().Trim();
            const int maxTranscriptChars = 12000;
            return text.Length <= maxTranscriptChars ? text : text[..maxTranscriptChars];
        }

        private static string BuildSummarySubtitle(ChatSession sessao, int messageCount)
        {
            var documentLabel = string.IsNullOrWhiteSpace(sessao.DocumentName)
                ? "Sem documento anexado"
                : sessao.DocumentName;

            return $"Documento: {documentLabel} | Mensagens na conversa: {messageCount}";
        }

        private static string BuildSummaryFileName(ChatSession sessao)
        {
            var baseName = string.IsNullOrWhiteSpace(sessao.DocumentName)
                ? $"resumo-sessao-{sessao.Id}"
                : sessao.DocumentName.Split(',')[0].Trim();

            var safeName = new string(baseName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch).ToArray());
            safeName = string.IsNullOrWhiteSpace(safeName) ? $"resumo-sessao-{sessao.Id}" : safeName;
            return $"{safeName}-resumo.pdf";
        }

        private static string NormalizeSummaryMarkdown(string rawText)
        {
            var text = rawText.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            if (!text.StartsWith("#"))
            {
                text = "# Resumo Executivo\n\n" + text;
            }

            return text;
        }

        private static string BuildFallbackSummaryMarkdown(ChatSession sessao, IReadOnlyList<ChatMessage> historico)
        {
            var documentLabel = string.IsNullOrWhiteSpace(sessao.DocumentName)
                ? "Documento não informado"
                : sessao.DocumentName;

            var recentMessages = historico
                .TakeLast(8)
                .Select(message => $"- {(message.Role == "User" ? "Usuário" : "Assistente")}: {message.Content}")
                .ToList();

            return string.Join("\n", new[]
            {
                "# Resumo Executivo",
                "## Contexto",
                $"Conversa baseada em {documentLabel}.",
                "## Pontos principais",
                recentMessages.Count > 0 ? string.Join("\n", recentMessages) : "- Não há mensagens suficientes para extrair pontos principais.",
                "## Conclusão",
                "O assistente analisou o conteúdo da conversa e consolidou os pontos discutidos.",
                "## Próximos passos",
                "- Revisar os pontos principais no documento.",
                "- Validar eventuais dúvidas que ainda ficaram em aberto.",
            });
        }
    }
}
