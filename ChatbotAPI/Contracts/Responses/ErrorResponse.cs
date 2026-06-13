namespace ChatbotAPI.Contracts.Responses
{
    /// <summary>
    /// DTO padrao de erro para manter as respostas da API consistentes.
    /// </summary>
    public class ErrorResponse
    {
        public string Erro { get; set; } = string.Empty;
    }
}
