namespace ChatbotAPI.Models
{
    public class MensagemRequest
        {
            public int SessionId { get; set; }
            public string Texto { get; set; } = string.Empty;
        }
}