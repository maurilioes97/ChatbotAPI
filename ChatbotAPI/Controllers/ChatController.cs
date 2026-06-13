using ChatbotAPI.Contracts.Requests;
using ChatbotAPI.Contracts.Responses;
using ChatbotAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotAPI.Controllers
{
    /// <summary>
    /// Controller principal do chat.
    /// Recebe as requisicoes HTTP e delega a regra de negocio para o service.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        /// <summary>
        /// Injeta o service responsavel pelo fluxo do chat.
        /// </summary>
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Cria uma nova sessao de conversa usando o prompt base enviado pelo front.
        /// </summary>
        [HttpPost("nova-sessao")]
        public async Task<ActionResult<CreateSessionResponse>> CriarSessao([FromBody] string systemPrompt)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                return BadRequest(new ErrorResponse
                {
                    Erro = "SystemPrompt e obrigatorio."
                });
            }

            var sessionId = await _chatService.CreateSessionAsync(systemPrompt);

            return Ok(new CreateSessionResponse
            {
                SessionId = sessionId
            });
        }

        /// <summary>
        /// Faz upload de um ou mais documentos e salva o contexto textual na sessao.
        /// </summary>
        [HttpPost("sessao/{sessionId:int}/documento")]
        [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
        public async Task<ActionResult<UploadDocumentResponse>> AnexarDocumento([FromRoute] int sessionId, [FromForm] List<IFormFile> documentos)
        {
            var result = await _chatService.UploadDocumentsAsync(sessionId, documentos);

            if (!result.Success)
            {
                return BadRequest(new ErrorResponse
                {
                    Erro = result.ErrorMessage ?? "Erro ao anexar documento."
                });
            }

            return Ok(new UploadDocumentResponse
            {
                DocumentName = result.DocumentName,
                ExtractedCharacters = result.ExtractedCharacters,
                SuggestedQuestions = result.SuggestedQuestions
            });
        }

        /// <summary>
        /// Remove o documento atualmente vinculado a uma sessao.
        /// </summary>
        [HttpDelete("sessao/{sessionId:int}/documento")]
        public async Task<ActionResult<RemoveDocumentResponse>> RemoverDocumento([FromRoute] int sessionId)
        {
            var removed = await _chatService.ClearDocumentsAsync(sessionId);

            if (!removed)
            {
                return NotFound(new ErrorResponse
                {
                    Erro = "Sessao nao encontrada."
                });
            }

            return Ok(new RemoveDocumentResponse
            {
                Removido = true
            });
        }

        /// <summary>
        /// Recebe um audio, envia para transcricao e devolve o texto reconhecido.
        /// </summary>
        [HttpPost("sessao/{sessionId:int}/audio")]
        [RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)]
        public async Task<ActionResult<TranscribeAudioResponse>> TranscreverAudio([FromRoute] int sessionId, [FromForm] IFormFile audio)
        {
            var result = await _chatService.TranscribeAudioAsync(sessionId, audio);

            if (!result.Success)
            {
                return BadRequest(new ErrorResponse
                {
                    Erro = result.ErrorMessage ?? "Erro ao transcrever audio."
                });
            }

            return Ok(new TranscribeAudioResponse
            {
                Transcript = result.Transcript
            });
        }

        /// <summary>
        /// Recebe a mensagem do usuario, envia para o Gemini e retorna a resposta do assistente.
        /// </summary>
        [HttpPost("enviar-mensagem")]
        public async Task<ActionResult<SendMessageResponse>> EnviarMensagem([FromBody] SendMessageRequest request)
        {
            var result = await _chatService.SendMessageAsync(request);
            if (!result.Success)
            {
                return BadRequest(new ErrorResponse
                {
                    Erro = result.ErrorMessage ?? "A API do Google recusou o pedido."
                });
            }

            return Ok(new SendMessageResponse
            {
                Resposta = result.Response
            });
        }

        /// <summary>
        /// Fecha a conversa da sessao e gera um PDF com o resumo final.
        /// </summary>
        [HttpPost("sessao/{sessionId:int}/encerrar-e-exportar-resumo")]
        public async Task<IActionResult> EncerrarEExportarResumo([FromRoute] int sessionId)
        {
            var result = await _chatService.ExportSummaryPdfAsync(sessionId);

            if (!result.Success || result.FileContent is null || string.IsNullOrWhiteSpace(result.FileName))
            {
                return BadRequest(new ErrorResponse
                {
                    Erro = result.ErrorMessage ?? "Nao foi possivel gerar o resumo."
                });
            }

            return File(result.FileContent, "application/pdf", result.FileName);
        }
    }
}
