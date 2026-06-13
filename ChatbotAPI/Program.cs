using ChatbotAPI.Contracts.Responses;
using ChatbotAPI.Data;
using ChatbotAPI.Middleware;
using ChatbotAPI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Registra os controllers da API.
builder.Services.AddControllers();

// Padroniza a resposta quando a validacao automatica do model falhar.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var firstError = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

        return new BadRequestObjectResult(new ErrorResponse
        {
            Erro = firstError ?? "Os dados enviados sao invalidos."
        });
    };
});

// Habilita o endpoint OpenAPI no ambiente de desenvolvimento.
builder.Services.AddOpenApi();

// Libera chamadas do front local para a API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException("A connection string 'DefaultConnection' nao foi configurada. Defina ConnectionStrings__DefaultConnection ou use appsettings.Development.json.");
}

// Configura o Entity Framework para usar SQL Server.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(defaultConnection, sqlOptions =>
        sqlOptions.EnableRetryOnFailure()));

// Carrega as configuracoes da integracao com o Gemini.
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));

// Registra os servicos principais da aplicacao.
builder.Services.AddHttpClient<ChatbotAPI.Clients.IGeminiClient, ChatbotAPI.Clients.GeminiClient>();
builder.Services.AddScoped<ChatbotAPI.Services.IDocumentService, ChatbotAPI.Services.DocumentService>();
builder.Services.AddScoped<ChatbotAPI.Services.IChatService, ChatbotAPI.Services.ChatService>();

var app = builder.Build();

// Captura erros nao tratados antes de devolver a resposta.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowReactApp");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
