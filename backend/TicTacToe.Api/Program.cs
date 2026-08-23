using TicTacToe.Api.Repositories;
using TicTacToe.Api.Repositories.Interfaces;
using TicTacToe.Api.Services;
using TicTacToe.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IGameRepository, InMemoryGameRepository>();
builder.Services.AddSingleton<IScoreboardRepository, InMemoryScoreboardRepository>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IScoreboardService, ScoreboardService>();
builder.Services.AddScoped<IComputerPlayerService, ComputerPlayerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(30)));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularDev");
app.UseAuthorization();
app.MapControllers();
app.Run();