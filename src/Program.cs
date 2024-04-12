using Features.Matches;
using Features.Tournaments;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTournament.Authentication;
using OpenTournament.Common;
using OpenTournament.Common.Exceptions;
using OpenTournament.Features.Authentication;
using OpenTournament.Features.Matches;
using OpenTournament.Features.Tournaments;
using OpenTournament.Features.Templates;
using OpenTournament.Services;
using OpenTournament.Identity;
using OpenTournament.Identity.Authorization;
using OpenTournament.Options;
using Quartz;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(opts =>
{
    opts.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(GlobalConstants.AppName));
    opts.IncludeScopes = true;
    opts.IncludeFormattedMessage = true;
    opts.AddOtlpExporter(export =>
    {
        export.Endpoint = new Uri("http://localhost:4317");
    });
});
builder.Services.Configure<FirebaseAuthenticationOptions>(
    builder.Configuration.GetSection(FirebaseAuthenticationOptions.SectionName));
    //.ValidateDataAnnotations().ValidateOnStart();

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));

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
builder.Services.AddQuartz(opts =>
{
    var jobKey = JobKey.Create(nameof(OutboxBackgroundJob));
    opts.AddJob<OutboxBackgroundJob>(jobKey)
        .AddTrigger(trigger =>
        {
            trigger.ForJob(jobKey).WithSimpleSchedule(schedule =>
            {
                schedule.WithIntervalInSeconds(1).RepeatForever();
            });
        });
});
builder.Services.AddQuartzHostedService();
builder.Services.AddOpenTelemetry()
    .WithMetrics(o =>
    {
        o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(GlobalConstants.AppName));
        o.AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        o.AddOtlpExporter(export =>
        {
            var addr = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";
            export.Endpoint = new Uri(addr);
        });
    })
    .WithTracing(opts =>
    {
        opts.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(GlobalConstants.AppName));
        opts.SetSampler(new AlwaysOnSampler());

        opts.AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
        
        opts.AddOtlpExporter(export =>
        {
            export.Endpoint = new Uri("http://localhost:4317");
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
