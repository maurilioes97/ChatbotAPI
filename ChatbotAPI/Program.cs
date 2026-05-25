using ChatbotAPI.Data;
using ChatbotAPI.Options;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // A porta do React
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException("A connection string 'DefaultConnection' não foi configurada. Defina ConnectionStrings__DefaultConnection ou use appsettings.Development.json.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(defaultConnection, sqlOptions =>
        sqlOptions.EnableRetryOnFailure()));

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));

// Serviços de aplicação
builder.Services.AddScoped<ChatbotAPI.Services.IChatService, ChatbotAPI.Services.ChatService>();

var app = builder.Build();

app.UseCors("AllowReactApp");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
