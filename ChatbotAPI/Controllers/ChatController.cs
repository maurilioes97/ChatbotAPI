using Microsoft.AspNetCore.Mvc;
using ChatbotAPI.Data;
using ChatbotAPI.Models;
using System.Text.Json;
using System.Text;

// Rota da API
[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly string _apiKey;

    public ChatController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _apiKey = config["GeminiApiKey"];
    }

    // Rota Criar nova sessão
    [HttpPost("nova-sessao")]
    public IActionResult CriarSessao([FromBody] string prompt)
    {
        var sessao = new ChatSession { SystemPrompt = prompt };
        
        _context.ChatSessions.Add(sessao);
        _context.SaveChanges();

        // Retorna o ID para o React guardar
        return Ok(new { sessionId = sessao.Id });
    }

    // Rota Enviar mensagens
    [HttpPost("enviar-mensagem")]
    public async Task<IActionResult> EnviarMensagem([FromBody] MensagemRequest request)
    {
        // Salva msg do usuario no banco
        var msgUsuario = new ChatMessage { 
            SessionId = request.SessionId, 
            Role = "user", 
            Content = request.Texto 
        };
        _context.ChatMessages.Add(msgUsuario);
        _context.SaveChanges();

        // Busca prompt e histórico
        var sessao = _context.ChatSessions.Find(request.SessionId);
        var historico = _context.ChatMessages
            .Where(m => m.SessionId == request.SessionId)
            .OrderBy(m => m.CreatedAt).ToList();

        // Prepara o pacote JSON p/ Gemini
        var payload = new {
            systemInstruction = new { parts = new[] { new { text = sessao.SystemPrompt } } },

            contents = historico.Select(m => new {
            // Se no banco for "Assistant", enviamos "model" para o Google, caso contrario, user.
            role = (m.Role == "Assistant") ? "model" : m.Role, 
            parts = new[] { new { text = m.Content } }
            })
        };

        // Faz a chamada para a API do Gemini
        var client = new HttpClient();

        // Transformar o objeto em json
        var jsonParaIA = JsonSerializer.Serialize(payload);

        // Cria o conteúdo: string, encoding, media type (encapsulamento de texto)
        var conteudo = new StringContent(jsonParaIA, Encoding.UTF8, "application/json");
    
        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + _apiKey;
        
        // Enviar pacote p/ google
        var respostaIA = await client.PostAsync(url, conteudo);

        // Ler a resposta do google
        var resultadoBruto = await respostaIA.Content.ReadAsStringAsync();

        // Extrai o texto da resposta usando JsonDocument (Parsing)
        using var doc = JsonDocument.Parse(resultadoBruto);
        string textoResposta = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text").GetString();

        // 6. Salva a resposta da IA no banco e envia para o React
        var msgIA = new ChatMessage { 
            SessionId = request.SessionId, 
            Role = "Assistant", 
            Content = textoResposta 
        };
        _context.ChatMessages.Add(msgIA);
        _context.SaveChanges();

        return Ok(new { resposta = textoResposta });
    }
}
