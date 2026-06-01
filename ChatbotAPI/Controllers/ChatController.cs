using ChatbotAPI.Models;
using ChatbotAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("nova-sessao")] // Rota para criar sessão no banco de dados e responder para o react
        public async Task<IActionResult> CriarSessao([FromBody] string systemPrompt)
        {
            var sessionId = await _chatService.CreateSessionAsync(systemPrompt);
            return Ok(new { SessionId = sessionId });
        }

        [HttpPost("sessao/{sessionId:int}/documento")]
        [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
        public async Task<IActionResult> AnexarDocumento([FromRoute] int sessionId, [FromForm] List<IFormFile> documentos)
        {
            var result = await _chatService.UploadDocumentsAsync(sessionId, documentos);

            if (!result.Success)
            {
                return BadRequest(new { Erro = result.ErrorMessage });
            }

            return Ok(new
            {
                documentName = result.DocumentName,
                extractedCharacters = result.ExtractedCharacters,
                suggestedQuestions = result.SuggestedQuestions
            });
        }

        [HttpDelete("sessao/{sessionId:int}/documento")]
        public async Task<IActionResult> RemoverDocumento([FromRoute] int sessionId)
        {
            var removed = await _chatService.ClearDocumentsAsync(sessionId);

            if (!removed)
            {
                return NotFound(new { Erro = "Sessão não encontrada." });
            }

            return Ok(new { Removido = true });
        }

        [HttpPost("sessao/{sessionId:int}/audio")]
        [RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)]
        public async Task<IActionResult> TranscreverAudio([FromRoute] int sessionId, [FromForm] IFormFile audio)
        {
            var result = await _chatService.TranscribeAudioAsync(sessionId, audio);

            if (!result.Success)
            {
                return BadRequest(new { Erro = result.ErrorMessage });
            }

            return Ok(new { transcript = result.Transcript });
        }

        [HttpPost("enviar-mensagem")] // Rota para enviar mensagem e receber respota do gemini
        public async Task<IActionResult> EnviarMensagem([FromBody] MensagemRequest request)
        {
            var result = await _chatService.SendMessageAsync(request);
            if (!result.Success)
                return BadRequest(new { Erro = result.ErrorMessage ?? "A API do Google recusou o pedido." });

            return Ok(new { Resposta = result.Response });
        }

        [HttpPost("sessao/{sessionId:int}/encerrar-e-exportar-resumo")]
        public async Task<IActionResult> EncerrarEExportarResumo([FromRoute] int sessionId)
        {
            var result = await _chatService.ExportSummaryPdfAsync(sessionId);

            if (!result.Success || result.FileContent is null || string.IsNullOrWhiteSpace(result.FileName))
            {
                return BadRequest(new { Erro = result.ErrorMessage ?? "Não foi possível gerar o resumo." });
            }

            return File(result.FileContent, "application/pdf", result.FileName);
        }
    }
}
