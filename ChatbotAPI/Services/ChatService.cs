using ChatbotAPI.Clients;
using ChatbotAPI.Contracts.Requests;
using ChatbotAPI.Data;
using ChatbotAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatbotAPI.Services
{
    /// <summary>
    /// Resultado interno usado quando a API gera uma resposta de chat.
    /// </summary>
    public class ChatResponse
    {
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Resultado interno usado no fluxo de upload e processamento de documentos.
    /// </summary>
    public class DocumentUploadResponse
    {
        public bool Success { get; set; }
        public string? DocumentName { get; set; }
        public int ExtractedCharacters { get; set; }
        public List<string> SuggestedQuestions { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Resultado interno usado quando a aplicacao gera o PDF final do resumo.
    /// </summary>
    public class SummaryExportResponse
    {
        public bool Success { get; set; }
        public byte[]? FileContent { get; set; }
        public string? FileName { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Resultado interno do fluxo de transcricao de audio.
    /// </summary>
    public class AudioTranscriptionResponse
    {
        public bool Success { get; set; }
        public string? Transcript { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Service principal da aplicacao.
    /// Ele coordena banco, documentos, Gemini e geracao de resumo.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IDocumentService _documentService;
        private readonly IGeminiClient _geminiClient;
        private const long MaxAudioSizeBytes = 15L * 1024 * 1024;
        private static readonly HashSet<string> AllowedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".wav", ".mp3", ".m4a", ".ogg", ".webm"
        };

        /// <summary>
        /// Recebe as dependencias centrais usadas no fluxo do chat.
        /// </summary>
        public ChatService(
            AppDbContext context,
            IDocumentService documentService,
            IGeminiClient geminiClient)
        {
            _context = context;
            _documentService = documentService;
            _geminiClient = geminiClient;
        }

        /// <summary>
        /// Cria uma nova sessao e salva o prompt base que guiara o assistente.
        /// </summary>
        public async Task<int> CreateSessionAsync(string systemPrompt)
        {
            var sessao = new ChatSession { SystemPrompt = systemPrompt };
            _context.ChatSessions.Add(sessao);
            await _context.SaveChangesAsync();
            return sessao.Id;
        }

        /// <summary>
        /// Processa os documentos enviados e salva o contexto extraido na sessao.
        /// </summary>
        public async Task<DocumentUploadResponse> UploadDocumentsAsync(int sessionId, IEnumerable<IFormFile> documents)
        {
            var sessao = await _context.ChatSessions.FindAsync(sessionId);
            if (sessao is null)
            {
                return new DocumentUploadResponse
                {
                    Success = false,
                    ErrorMessage = "Sessao nao encontrada."
                };
            }

            var documentProcessing = await _documentService.ProcessDocumentsAsync(documents);
            if (!documentProcessing.Success)
            {
                return new DocumentUploadResponse
                {
                    Success = false,
                    ErrorMessage = documentProcessing.ErrorMessage
                };
            }

            sessao.DocumentName = documentProcessing.DocumentName;
            sessao.DocumentContext = documentProcessing.DocumentContext;

            var suggestedQuestions = await GenerateSuggestedQuestionsAsync(sessao);

            await _context.SaveChangesAsync();

            return new DocumentUploadResponse
            {
                Success = true,
                DocumentName = sessao.DocumentName,
                ExtractedCharacters = documentProcessing.ExtractedCharacters,
                SuggestedQuestions = suggestedQuestions
            };
        }

        /// <summary>
        /// Remove o documento vinculado a uma sessao existente.
        /// </summary>
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

        /// <summary>
        /// Valida e transcreve um audio enviado pelo usuario.
        /// </summary>
        public async Task<AudioTranscriptionResponse> TranscribeAudioAsync(int sessionId, IFormFile audio)
        {
            if (audio is null || audio.Length <= 0)
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Nenhum audio foi enviado."
                };
            }

            if (audio.Length > MaxAudioSizeBytes)
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "O audio excede o limite de 15 MB por envio."
                };
            }

            var sessaoExists = await _context.ChatSessions.AnyAsync(s => s.Id == sessionId);
            if (!sessaoExists)
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Sessao nao encontrada."
                };
            }

            var extension = Path.GetExtension(audio.FileName).ToLowerInvariant();
            if (!AllowedAudioExtensions.Contains(extension))
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "Formato de audio nao suportado. Use WAV, MP3, M4A, OGG ou WEBM."
                };
            }

            if (!_geminiClient.IsConfigured)
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = "A chave do Gemini nao foi configurada."
                };
            }

            await using var audioStream = audio.OpenReadStream();
            await using var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream);
            var base64Audio = Convert.ToBase64String(memoryStream.ToArray());

            var mimeType = ResolveAudioMimeType(audio.FileName, audio.ContentType);
            var transcriptionResult = await _geminiClient.TranscribeAudioAsync(base64Audio, mimeType);

            if (!transcriptionResult.Success || string.IsNullOrWhiteSpace(transcriptionResult.Text))
            {
                return new AudioTranscriptionResponse
                {
                    Success = false,
                    ErrorMessage = string.IsNullOrWhiteSpace(transcriptionResult.ErrorMessage)
                        ? "Nao foi possivel entender o audio enviado."
                        : transcriptionResult.ErrorMessage
                };
            }

            return new AudioTranscriptionResponse
            {
                Success = true,
                Transcript = transcriptionResult.Text
            };
        }

        /// <summary>
        /// Registra a mensagem do usuario, consulta o Gemini e grava a resposta no historico.
        /// </summary>
        public async Task<ChatResponse> SendMessageAsync(SendMessageRequest request)
        {
            var mensagemUsuario = new ChatMessage
            {
                SessionId = request.SessionId,
                Role = "User",
                Content = request.Texto
            };
            _context.ChatMessages.Add(mensagemUsuario);
            await _context.SaveChangesAsync();

            var sessao = await _context.ChatSessions.FindAsync(request.SessionId);
            if (sessao is null)
            {
                return new ChatResponse
                {
                    Success = false,
                    ErrorMessage = "Sessao nao encontrada."
                };
            }

            var historico = await _context.ChatMessages
                .Where(m => m.SessionId == request.SessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var systemPrompt = BuildSystemPrompt(sessao);

            if (!_geminiClient.IsConfigured)
            {
                return new ChatResponse
                {
                    Success = false,
                    ErrorMessage = "A chave do Gemini nao foi configurada. Defina Gemini__ApiKey em variaveis de ambiente ou user-secrets."
                };
            }

            var mensagensGemini = historico
                .Select(msg => new GeminiChatMessage
                {
                    Role = msg.Role == "User" ? "user" : "model",
                    Content = msg.Content
                })
                .ToList();

            var chatResult = await _geminiClient.GenerateChatReplyAsync(systemPrompt, mensagensGemini);
            if (!chatResult.Success || string.IsNullOrWhiteSpace(chatResult.Text))
            {
                return new ChatResponse
                {
                    Success = false,
                    ErrorMessage = chatResult.ErrorMessage
                };
            }

            var mensagemIA = new ChatMessage
            {
                SessionId = request.SessionId,
                Role = "Assistant",
                Content = chatResult.Text
            };
            _context.ChatMessages.Add(mensagemIA);
            await _context.SaveChangesAsync();

            return new ChatResponse { Success = true, Response = chatResult.Text };
        }

        /// <summary>
        /// Gera um PDF com o resumo final da sessao e da conversa.
        /// </summary>
        public async Task<SummaryExportResponse> ExportSummaryPdfAsync(int sessionId)
        {
            var sessao = await _context.ChatSessions.FindAsync(sessionId);
            if (sessao is null)
            {
                return new SummaryExportResponse
                {
                    Success = false,
                    ErrorMessage = "Sessao nao encontrada."
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
                    ErrorMessage = "Nao ha conteudo suficiente para gerar o resumo."
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

        /// <summary>
        /// Monta o prompt final combinando prompt base e contexto do documento.
        /// </summary>
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
                    "Use o conteudo do documento abaixo como contexto principal para responder ao usuario. " +
                    "Se a resposta nao estiver no documento, diga isso com clareza e nao invente informacoes.");

                if (!string.IsNullOrWhiteSpace(sessao.DocumentName))
                {
                    partes.Add($"Documento: {sessao.DocumentName}");
                }

                partes.Add(sessao.DocumentContext.Trim());
            }

            return string.Join("\n\n", partes);
        }

        /// <summary>
        /// Normaliza o mime type do audio para o formato esperado pela API.
        /// </summary>
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

        /// <summary>
        /// Tenta gerar sugestoes de perguntas e cai para um plano B se a IA nao responder.
        /// </summary>
        private async Task<List<string>> GenerateSuggestedQuestionsAsync(ChatSession sessao)
        {
            if (!_geminiClient.IsConfigured || string.IsNullOrWhiteSpace(sessao.DocumentContext))
            {
                return GetFallbackQuestions(sessao);
            }

            var result = await _geminiClient.GenerateSuggestedQuestionsAsync(sessao.DocumentContext);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Text))
            {
                return GetFallbackQuestions(sessao);
            }

            var questions = result.Text
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

        /// <summary>
        /// Limpa marcadores e numeracoes para deixar a pergunta final mais natural.
        /// </summary>
        private static string NormalizeSuggestedQuestion(string line)
        {
            var cleaned = line.Trim();
            cleaned = Regex.Replace(cleaned, @"^[-*\s]+", string.Empty);
            cleaned = Regex.Replace(cleaned, @"^\d+[\).\-:\s]+", string.Empty);
            return cleaned.Trim();
        }

        /// <summary>
        /// Gera perguntas padrao para o caso de a IA nao devolver sugestoes.
        /// </summary>
        private static List<string> GetFallbackQuestions(ChatSession sessao)
        {
            var subject = string.IsNullOrWhiteSpace(sessao.DocumentName)
                ? "este documento"
                : sessao.DocumentName.Split(',')[0].Trim();

            return new List<string>
            {
                $"Qual e o objetivo principal de {subject}?",
                $"Quais pontos mais importantes eu devo entender em {subject}?",
                $"Que informacoes de {subject} merecem atencao especial?"
            };
        }

        /// <summary>
        /// Tenta gerar o markdown do resumo via Gemini e usa um resumo local se necessario.
        /// </summary>
        private async Task<string> GenerateSummaryMarkdownAsync(ChatSession sessao, IReadOnlyList<ChatMessage> historico)
        {
            var transcript = BuildConversationTranscript(historico);
            if (!_geminiClient.IsConfigured || string.IsNullOrWhiteSpace(transcript))
            {
                return BuildFallbackSummaryMarkdown(sessao, historico);
            }

            var documentName = sessao.DocumentName ?? "Documento sem nome";
            var result = await _geminiClient.GenerateSummaryAsync(documentName, transcript);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Text))
            {
                return BuildFallbackSummaryMarkdown(sessao, historico);
            }

            var normalized = NormalizeSummaryMarkdown(result.Text);
            return string.IsNullOrWhiteSpace(normalized)
                ? BuildFallbackSummaryMarkdown(sessao, historico)
                : normalized;
        }

        /// <summary>
        /// Converte o historico de mensagens em um texto linear para resumir depois.
        /// </summary>
        private static string BuildConversationTranscript(IReadOnlyList<ChatMessage> historico)
        {
            if (historico.Count == 0)
            {
                return string.Empty;
            }

            var transcript = new StringBuilder();
            foreach (var message in historico)
            {
                var label = message.Role == "User" ? "Usuario" : message.Role == "Assistant" ? "Assistente" : message.Role;
                transcript.AppendLine($"{label}: {message.Content}");
                transcript.AppendLine();
            }

            var text = transcript.ToString().Trim();
            const int maxTranscriptChars = 12000;
            return text.Length <= maxTranscriptChars ? text : text[..maxTranscriptChars];
        }

        /// <summary>
        /// Monta a linha de apoio exibida abaixo do titulo no PDF.
        /// </summary>
        private static string BuildSummarySubtitle(ChatSession sessao, int messageCount)
        {
            var documentLabel = string.IsNullOrWhiteSpace(sessao.DocumentName)
                ? "Sem documento anexado"
                : sessao.DocumentName;

            return $"Documento: {documentLabel} | Mensagens na conversa: {messageCount}";
        }

        /// <summary>
        /// Gera um nome de arquivo seguro para o PDF exportado.
        /// </summary>
        private static string BuildSummaryFileName(ChatSession sessao)
        {
            var baseName = string.IsNullOrWhiteSpace(sessao.DocumentName)
                ? $"resumo-sessao-{sessao.Id}"
                : sessao.DocumentName.Split(',')[0].Trim();

            var safeName = new string(baseName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch).ToArray());
            safeName = string.IsNullOrWhiteSpace(safeName) ? $"resumo-sessao-{sessao.Id}" : safeName;
            return $"{safeName}-resumo.pdf";
        }

        /// <summary>
        /// Garante que o markdown final tenha pelo menos a estrutura basica esperada.
        /// </summary>
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

        /// <summary>
        /// Cria um resumo simples local quando a IA nao estiver disponivel.
        /// </summary>
        private static string BuildFallbackSummaryMarkdown(ChatSession sessao, IReadOnlyList<ChatMessage> historico)
        {
            var documentLabel = string.IsNullOrWhiteSpace(sessao.DocumentName)
                ? "Documento nao informado"
                : sessao.DocumentName;

            var recentMessages = historico
                .TakeLast(8)
                .Select(message => $"- {(message.Role == "User" ? "Usuario" : "Assistente")}: {message.Content}")
                .ToList();

            return string.Join("\n", new[]
            {
                "# Resumo Executivo",
                "## Contexto",
                $"Conversa baseada em {documentLabel}.",
                "## Pontos principais",
                recentMessages.Count > 0 ? string.Join("\n", recentMessages) : "- Nao ha mensagens suficientes para extrair pontos principais.",
                "## Conclusao",
                "O assistente analisou o conteudo da conversa e consolidou os pontos discutidos.",
                "## Proximos passos",
                "- Revisar os pontos principais no documento.",
                "- Validar eventuais duvidas que ainda ficaram em aberto.",
            });
        }
    }
}
