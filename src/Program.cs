using Features.Matches;
using Features.Tournaments;
using Microsoft.AspNetCore.HttpLogging;
using OpenTournament.Common.Exceptions;
using OpenTournament.Common;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddMediator();
builder.Services.AddHealthChecks();
builder.Services.AddHttpLogging((options) =>
{
    options.CombineLogs = true;
    options.LoggingFields = HttpLoggingFields.All;
});
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddSingleton<AppDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseExceptionHandler(options => {});
app.MapHealthChecks("/health");

CreateTournament.MapEndpoint(app);
GetTournament.MapEndpoint(app);
//UpdateTournament.MapEndpoint(app);
DeleteTournament.MapEndpoint(app);
StartTournament.MapEndpoint(app);

LeaveRegistration.MapEndpoint(app);
JoinRegistration.MapEndpoint(app);

GetMatch.MapEndpoint(app);
UpdateMatch.MapEndpoint(app);

app.Run();
