using Features.Matches;
using Features.Tournaments;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTournament.Authentication;
using OpenTournament.Common;
using OpenTournament.Common.Exceptions;
using OpenTournament.Features.Authentication;
using OpenTournament.Features.Templates;
using OpenTournament.Services;
using OpenTournament.Identity;
using OpenTournament.Identity.Authorization;
using OpenTournament.Options;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
//builder.Logging.AddJsonConsole();
builder.Logging.AddConsole();
builder.Services.Configure<FirebaseAuthenticationOptions>(
    builder.Configuration.GetSection(FirebaseAuthenticationOptions.SectionName));
    //.ValidateDataAnnotations().ValidateOnStart();

//builder.Services.AddOptions<DatabaseOptions>(
//    builder.Configuration.GetSection(DatabaseOptions.SectionName));

builder.Services.AddHttpLogging((options) =>
{
    options.CombineLogs = true;
    options.LoggingFields = HttpLoggingFields.All;
});
builder.Services.AddDbContext<AppDbContext>((Action<DbContextOptionsBuilder>?)null, ServiceLifetime.Singleton);
builder.Services.AddMediator();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddTournamentLayouts();
builder.Services.AddOpenTelemetry()
    .WithMetrics(o =>
    {
        o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(GlobalConstants.AppName));
        o.AddMeter("Microsoft.AspNetCore.Hosting",
            "System.Net.Http");

        o.AddOtlpExporter(export =>
        {
            var addr = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost";
            export.Endpoint = new Uri(addr);
        });
    });

builder.Services.AddSingleton<IAuthorizationHandler, MatchEditHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var firebaseAuth = builder.Configuration
        .GetSection(FirebaseAuthenticationOptions.SectionName)
        .Get<FirebaseAuthenticationOptions>();

    options.Authority = firebaseAuth.Authority;
    options.TokenValidationParameters = new()
    {
        ValidIssuer = firebaseAuth.Issuer,
        ValidAudience = firebaseAuth.Audience,
        
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(IdentityData.ParticipantPolicyName, policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser();
    });
    
    options.AddPolicy(IdentityData.ServerPolicyName, policyBuilder =>
    {
        policyBuilder.RequireClaim(IdentityData.ServerClaimName, "server");
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpLogging();
app.UseExceptionHandler(options => {});
app.MapHealthChecks("/health");

CreateTournament.MapEndpoint(app);
GetTournament.MapEndpoint(app);
UpdateTournament.MapEndpoint(app);
DeleteTournament.MapEndpoint(app);
StartTournament.MapEndpoint(app);

LeaveRegistration.MapEndpoint(app);
JoinRegistration.MapEndpoint(app);
ListRegistration.MapEndpoint(app);

GetMatch.MapEndpoint(app);
UpdateMatch.MapEndpoint(app);
CompleteMatch.MapEndpoint(app);

//CreateTemplate.MapEndpoint(app);
//DeleteTemplate.MapEndpoint(app);
//ListTemplate.MapEndpoint(app);
//UpdateTemplate.MapEndpoint(app);

Login.MapEndpoint(app);

app.Run();
