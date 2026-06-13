namespace ChatbotAPI.Options
{
    /// <summary>
    /// Classe de configuracao para concentrar os dados de acesso ao Gemini.
    /// </summary>
    public class GeminiOptions
    {
        /// <summary>
        /// Chave usada para autenticar nas chamadas da API.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Modelo do Gemini que sera usado pela aplicacao.
        /// </summary>
        public string Model { get; set; } = "gemini-2.5-flash";

        /// <summary>
        /// URL base da API generative language.
        /// </summary>
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    }
}
