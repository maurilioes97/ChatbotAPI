using ChatbotAPI.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ChatbotAPI.Clients
{
    /// <summary>
    /// Cliente HTTP que encapsula as chamadas para a API do Gemini.
    /// </summary>
    public class GeminiClient : IGeminiClient
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        /// <summary>
        /// Recebe o HttpClient gerenciado pelo ASP.NET e as configuracoes do Gemini.
        /// </summary>
        public GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        /// <summary>
        /// Indica se existe uma API key pronta para uso.
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

        /// <summary>
        /// Envia o historico da conversa para gerar a proxima resposta do assistente.
        /// </summary>
        public Task<GeminiTextResult> GenerateChatReplyAsync(string systemPrompt, IReadOnlyList<GeminiChatMessage> history)
        {
            var contents = history.Select(message => new
            {
                role = message.Role,
                parts = new[] { new { text = message.Content } }
            });

            var payload = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents
            };

            return SendForTextAsync(payload);
        }

        /// <summary>
        /// Pede ao Gemini sugestoes de perguntas com base no documento anexado.
        /// </summary>
        public Task<GeminiTextResult> GenerateSuggestedQuestionsAsync(string documentContext)
        {
            var prompt =
                "Leia o documento abaixo e sugira exatamente 3 perguntas inteligentes que o usuario pode fazer sobre ele. " +
                "As perguntas devem ser curtas, objetivas e diretamente relacionadas ao conteudo. " +
                "Responda somente com as 3 perguntas, uma por linha, sem numeracao, sem marcadores e sem explicacoes.\n\n" +
                documentContext;

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

            return SendForTextAsync(payload);
        }

        /// <summary>
        /// Gera um resumo executivo da conversa em markdown simples.
        /// </summary>
        public Task<GeminiTextResult> GenerateSummaryAsync(string documentName, string transcript)
        {
            var prompt =
                "Voce vai resumir uma conversa sobre um documento. Gere um resumo executivo em portugues, usando markdown simples e SOMENTE esta estrutura: \n" +
                "# Resumo Executivo\n" +
                "## Contexto\n" +
                "## Pontos principais\n" +
                "- ...\n" +
                "## Conclusao\n" +
                "## Proximos passos\n" +
                "- ...\n\n" +
                "Regras: seja objetivo, nao invente informacoes, use frases curtas e claras.\n\n" +
                $"Documento: {documentName}\n\n" +
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

            return SendForTextAsync(payload);
        }

        /// <summary>
        /// Envia um audio em base64 para o Gemini e retorna a transcricao.
        /// </summary>
        public Task<GeminiTextResult> TranscribeAudioAsync(string base64Audio, string mimeType)
        {
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
                                Text = "Transcreva o audio para texto em portugues do Brasil. Responda apenas com a transcricao, sem comentarios."
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

            return SendForTextAsync(payload, includeStatusCodeInError: true);
        }

        /// <summary>
        /// Metodo central que monta a chamada HTTP e extrai o texto retornado pela API.
        /// </summary>
        private async Task<GeminiTextResult> SendForTextAsync(object payload, bool includeStatusCodeInError = false)
        {
            if (!IsConfigured)
            {
                return new GeminiTextResult
                {
                    Success = false,
                    ErrorMessage = "A chave do Gemini nao foi configurada."
                };
            }

            var url = $"{_options.BaseUrl}/{_options.Model}:generateContent?key={_options.ApiKey}";
            var requestBody = JsonSerializer.Serialize(payload);
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = includeStatusCodeInError && string.IsNullOrWhiteSpace(responseBody)
                        ? $"Nao foi possivel concluir a chamada ao Gemini. ({(int)response.StatusCode} {response.ReasonPhrase})"
                        : responseBody;

                    return new GeminiTextResult
                    {
                        Success = false,
                        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                            ? "Nao foi possivel concluir a chamada ao Gemini."
                            : errorMessage
                    };
                }

                using var document = JsonDocument.Parse(responseBody);
                var text = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return new GeminiTextResult
                {
                    Success = !string.IsNullOrWhiteSpace(text),
                    Text = text?.Trim(),
                    ErrorMessage = string.IsNullOrWhiteSpace(text)
                        ? "O Gemini retornou uma resposta vazia."
                        : null
                };
            }
            catch (Exception ex)
            {
                return new GeminiTextResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private class GeminiContentPart
        {
            [System.Text.Json.Serialization.JsonPropertyName("text")]
            public string? Text { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("inline_data")]
            public GeminiInlineData? InlineData { get; set; }
        }

        private class GeminiInlineData
        {
            [System.Text.Json.Serialization.JsonPropertyName("mime_type")]
            public string MimeType { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("data")]
            public string Data { get; set; } = string.Empty;
        }

        private class GeminiContent
        {
            [System.Text.Json.Serialization.JsonPropertyName("role")]
            public string Role { get; set; } = "user";

            [System.Text.Json.Serialization.JsonPropertyName("parts")]
            public List<GeminiContentPart> Parts { get; set; } = new();
        }

        private class GeminiGenerateContentRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("contents")]
            public List<GeminiContent> Contents { get; set; } = new();

            [System.Text.Json.Serialization.JsonPropertyName("generationConfig")]
            public object? GenerationConfig { get; set; }
        }
    }
}
