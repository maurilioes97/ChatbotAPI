using ChatbotAPI.Data;
using ChatbotAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ChatbotAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Injetamos o IConfiguration para conseguirmos ler a chave no appsettings.json
        public ChatController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("nova-sessao")]
        public async Task<IActionResult> CriarSessao([FromBody] string systemPrompt)
        {
            var sessao = new ChatSession { SystemPrompt = systemPrompt };
            _context.ChatSessions.Add(sessao);
            await _context.SaveChangesAsync();
            return Ok(new { SessionId = sessao.Id });
        }

        [HttpPost("enviar-mensagem")]
        public async Task<IActionResult> EnviarMensagem([FromBody] MensagemRequest request)
        {
            // 1. Salva a mensagem do usuário no banco
            var mensagemUsuario = new ChatMessage
            {
                SessionId = request.SessionId,
                Role = "User",
                Content = request.Texto
            };
            _context.ChatMessages.Add(mensagemUsuario);
            await _context.SaveChangesAsync();

            // 2. Busca a Sessão (para pegar o System Prompt) e o Histórico da conversa
            var sessao = await _context.ChatSessions.FindAsync(request.SessionId);
            var historico = await _context.ChatMessages
                .Where(m => m.SessionId == request.SessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // 3. Monta o corpo da requisição no formato que o Gemini exige
            var conteudos = new List<object>();
            foreach (var msg in historico)
            {
                // O Gemini usa "user" e "model" em vez de "User" e "Assistant"
                string roleGemini = msg.Role == "User" ? "user" : "model";
                conteudos.Add(new { role = roleGemini, parts = new[] { new { text = msg.Content } } });
            }

            var payload = new
            {
                // Trocamos o underline por systemInstruction (com I maiúsculo)
                // E adicionamos o new[] para garantir que é uma lista, como o Google exige!
                systemInstruction = new { parts = new[] { new { text = sessao!.SystemPrompt } } },
                contents = conteudos
            };

            // 4. Faz a chamada HTTP para a API do Gemini
            string apiKey = _configuration["GeminiApiKey"]!;
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            using var httpClient = new HttpClient();
            var jsonEnviado = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonEnviado, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            var jsonRetornado = await response.Content.ReadAsStringAsync();

            // --- TRAVA DE SEGURANÇA ---
            // Se o status da resposta não for de Sucesso (200 OK), a gente para tudo 
            // e devolve o erro real do Google para a tela, sem quebrar o C#!
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new
                {
                    Erro = "A API do Google recusou o pedido.",
                    MotivoReal = jsonRetornado
                });
            }
            // --------------------------

            // Se passou do "if" acima, é porque deu tudo certo! Aí sim lemos o texto:
            using var doc = JsonDocument.Parse(jsonRetornado);
            string respostaDaIA = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()!;

            // 6. Salva a resposta da IA no nosso banco de dados
            var mensagemIA = new ChatMessage
            {
                SessionId = request.SessionId,
                Role = "Assistant",
                Content = respostaDaIA
            };
            _context.ChatMessages.Add(mensagemIA);
            await _context.SaveChangesAsync();

            // 7. Devolve para o React (ou para o nosso teste)
            return Ok(new { Resposta = respostaDaIA });
        }
    }

    
}
