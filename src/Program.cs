using OpenTournament.Common.Exceptions;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
