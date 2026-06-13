using System.ComponentModel.DataAnnotations;

namespace ChatbotAPI.Contracts.Requests
{
    /// <summary>
    /// DTO usado quando o front envia uma nova mensagem para o chat.
    /// </summary>
    public class SendMessageRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "SessionId deve ser maior que zero.")]
        public int SessionId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Texto e obrigatorio.")]
        [RegularExpression(@".*\S.*", ErrorMessage = "Texto e obrigatorio.")]
        public string Texto { get; set; } = string.Empty;
    }
}
