namespace OpenTournament.Api.Configuration.Infrastructure;

public static class SignalRService
{
   public static IServiceCollection AddSignalR(this IServiceCollection services)
   {
      services.AddSignalR(options =>
      {
         options.EnableDetailedErrors = true;
      });
      
      return services;
   } 
}