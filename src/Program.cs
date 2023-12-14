using Features.Matches;
using Features.Tournaments;
using OpenTournament.Common.Exceptions;
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
JoinTournament.MapEndpoint(app);
StartTournament.MapEndpoint(app);

GetMatch.MapEndpoint(app);
UpdateMatch.MapEndpoint(app);

app.Run();
