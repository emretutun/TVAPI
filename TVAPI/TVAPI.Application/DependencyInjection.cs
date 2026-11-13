using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TVAPI.Application.Common;
using TVAPI.Application.Competitions;
using TVAPI.Application.Matches;
using TVAPI.Application.News;
using TVAPI.Application.Scenes;
using TVAPI.Application.Teams;
using TVAPI.Application.Weather;

namespace TVAPI.Application;

public static class DependencyInjection
{
    /// <summary>Application katmanının servislerini kaydeder. Program.cs tek satırla çağırır.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDataFreshness, DataFreshness>();

        // "Bugün" bilgisi doğrudan DateTime.Now ile değil buradan okunur; testte saat elle verilebilir.
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ICompetitionService, CompetitionService>();
        services.AddScoped<IStandingsService, StandingsService>();
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IMatchDetailService, MatchDetailService>();

        services.AddScoped<ISceneTemplateService, SceneTemplateService>();
        services.AddScoped<IPlaylistService, PlaylistService>();

        services.AddScoped<IWeatherService, WeatherService>();

        services.AddScoped<INewsService, NewsService>();

        return services;
    }
}
