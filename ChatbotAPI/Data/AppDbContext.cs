using ChatbotAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatbotAPI.Data
{
    /// <summary>
    /// Contexto principal do Entity Framework.
    /// Aqui ficam os mapeamentos das tabelas usadas pela aplicacao.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Recebe as configuracoes do banco vindas do Program.
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        /// <summary>
        /// Tabela de sessoes de conversa.
        /// </summary>
        public DbSet<ChatSession> ChatSessions { get; set; }

        /// <summary>
        /// Tabela de mensagens trocadas em cada sessao.
        /// </summary>
        public DbSet<ChatMessage> ChatMessages { get; set; }
    }
}
