namespace OpenTournament.Features.Tournaments.Create;

using OpenTournament.Data.Models;

internal sealed class Handler : IRequestHandler<CreateTournamentCommand, OneOf<TournamentId, ValidationFailure, ProblemDetails>>
{
	private readonly AppDbContext _dbContext;

	public Handler(AppDbContext dbContext)
	{
		_dbContext = dbContext;
		//_httpContext = httpContext;
	}
	
	public async ValueTask<OneOf<TournamentId, ValidationFailure, ProblemDetails>> Handle(CreateTournamentCommand request, 
		CancellationToken cancellationToken)
	{
		/*
		Validator validator = new();
		ValidationResult result = validator.Validate(request);
		if (!result.IsValid)
		{
			//var traceId = Tracer.CurrentSpan.Context.TraceId;
			
			//result.Errors;
			return new ValidationFailure();
		}*/
		
		//var creatorId = _httpContext.User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
		//var creatorId = _httpContext.GetUserId();
		var tourny = new Tournament
		{
			Id = TournamentId.NewTournamentId(),
			Name = request.Name
		};

		_dbContext.Add(tourny);
		var results = await _dbContext.SaveChangesAsync(cancellationToken);
		if (results < 1)
		{
			return new ProblemDetails
			{
				Title = "Internal Server Error",
				Status = 500
			};
		}

		return tourny.Id;
	}
}