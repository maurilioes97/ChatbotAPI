using ChatbotAPI.Contracts.Responses;
using System.Text.Json;

namespace ChatbotAPI.Middleware
{
    /// <summary>
    /// Middleware global para capturar excecoes nao tratadas e devolver erro padronizado.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// Recebe o proximo middleware da pipeline e o logger da aplicacao.
        /// </summary>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Executa o proximo passo da requisicao e trata qualquer excecao inesperada.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro nao tratado durante o processamento da requisicao.");

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse
                {
                    Erro = "Ocorreu um erro interno ao processar a requisicao."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
