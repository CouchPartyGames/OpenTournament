using Features.Matches;
using Features.Tournaments;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using OpenTournament.Authentication;
using OpenTournament.Common.Exceptions;
using OpenTournament.Common.Data;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=tourny.db");
}, ServiceLifetime.Singleton);
builder.Services.AddSingleton<AppDbContext>();
builder.Services.AddMediator();
builder.Services.AddHealthChecks();
builder.Services.AddHttpLogging((options) =>
{
    options.CombineLogs = true;
    options.LoggingFields = HttpLoggingFields.All;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
/*
    Firebase Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(JwtBearerDefaults.AuthenticationScheme);
builder.Services.AddSingleton(FirebaseApp.Create());
    OR
builder.Services.AddSingleton(FirebaseApp.Create(new AppOptions
{
    Credential = null,
    ProjectId = null,
    ServiceAccountId = null,
    HttpClientFactory = null
}));
*/

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
