namespace ChatbotAPI.Contracts.Responses
{
    /// <summary>
    /// DTO usado para devolver a transcricao final do audio.
    /// </summary>
    public class TranscribeAudioResponse
    {
        public string? Transcript { get; set; }
    }
}
