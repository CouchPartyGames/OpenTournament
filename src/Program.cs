using Features.Tournaments;
using OpenTournament.Common.Exceptions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenTournament.Common;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddMediator();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<AppDbContext>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/", () => "Hello World!");
CreateTournament.MapEndpoint(app);

app.Run();
