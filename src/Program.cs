using Features.Matches;
using Features.Tournaments;
using OpenTournament.Common.Exceptions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenTournament.Common;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddMediator();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<AppDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseExceptionHandler();
app.MapHealthChecks("/health");
CreateTournament.MapEndpoint(app);
GetTournament.MapEndpoint(app);
UpdateTournament.MapEndpoint(app);
GetMatch.MapEndpoint(app);

app.Run();
