using ChatbotAPI.Data;
using Microsoft.EntityFrameworkCore;

// contrutor do app (configurações e ferramentas)
var builder = WebApplication.CreateBuilder(args);

// serviços (gerenciar rotas, permissão e conecção sql)
builder.Services.AddControllers();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowReactApp", p => p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseCors("AllowReactApp");
app.MapControllers();
app.Run();