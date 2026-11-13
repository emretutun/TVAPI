using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;
using TVAPI.Application.Competitions;
using TVAPI.Application.Matches;
using TVAPI.Application.News;
using TVAPI.Application.Scenes;
using TVAPI.Application.Teams;
using TVAPI.Application.Weather;
using TVAPI.Infrastructure.Caching;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure;

public static class DependencyInjection
{
    // Yayın uygulamasında veritabanını uzun süre beklemek kabul edilemez. MySQL'in varsayılanları
    // (bağlantı 15 sn, sorgu 30 sn) yerine kısa süreler kullanılır; ayarlardan değiştirilebilir.
    private const uint DefaultConnectTimeoutSeconds = 3;
    private const uint DefaultCommandTimeoutSeconds = 5;

    /// <summary>
    /// Infrastructure katmanını kaydeder: Application'daki arayüzleri MySQL ile çalışan sınıflara bağlar.
    /// Bağlantı bilgileri ayarlardan okunur.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabases(services, configuration);
        AddCaching(services, configuration);
        AddBroadage(services);
        AddChannel(services);
        AddNews(services);

        return services;
    }

    private static void AddDatabases(IServiceCollection services, IConfiguration configuration)
    {
        // Broadage zorunludur: ayarı eksikse uygulama açılırken net bir hatayla durur.
        var broadage = BuildConnectionString(configuration, BroadageDatabase.ConnectionName)
            ?? throw new InvalidOperationException(
                $"'ConnectionStrings:{BroadageDatabase.ConnectionName}' ayarı bulunamadı. " +
                "Yerel geliştirmede 'dotnet user-secrets' ile tanımlanmalıdır.");

        services.AddSingleton(new BroadageDatabase(broadage));

        // Kanal veritabanı zorunlu değildir: ayarı yoksa uygulama açılır, yalnızca o veriyi kullanan uçlar 503 döner.
        services.AddSingleton(new ChannelDatabase(BuildConnectionString(configuration, ChannelDatabase.ConnectionName)));

        // Haber veritabanı da zorunlu değildir.
        services.AddSingleton(new NewsDatabase(BuildConnectionString(configuration, NewsDatabase.ConnectionName)));
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(BroadageCacheOptions.From(configuration));
        services.AddSingleton(ChannelCacheOptions.From(configuration));
        services.AddSingleton(NewsCacheOptions.From(configuration));
        services.AddScoped<ResilientCache>();
    }

    // Her veri alanında aynı düzen: veritabanına giden asıl repository kendi tipiyle kaydedilir,
    // arayüzü isteyen servisler ise onu saran önbellekli halini alır.

    private static void AddBroadage(IServiceCollection services)
    {
        services.AddScoped<CompetitionRepository>();
        services.AddScoped<StandingsRepository>();
        services.AddScoped<MatchRepository>();
        services.AddScoped<TeamRepository>();
        services.AddScoped<MatchDetailRepository>();

        services.AddScoped<ICompetitionRepository, CachedCompetitionRepository>();
        services.AddScoped<IStandingsRepository, CachedStandingsRepository>();
        services.AddScoped<IMatchRepository, CachedMatchRepository>();
        services.AddScoped<ITeamRepository, CachedTeamRepository>();
        services.AddScoped<IMatchDetailRepository, CachedMatchDetailRepository>();
    }

    private static void AddChannel(IServiceCollection services)
    {
        services.AddScoped<SceneTemplateRepository>();
        services.AddScoped<PlaylistRepository>();
        services.AddScoped<WeatherRepository>();

        services.AddScoped<ISceneTemplateRepository, CachedSceneTemplateRepository>();
        services.AddScoped<IPlaylistRepository, CachedPlaylistRepository>();
        services.AddScoped<IWeatherRepository, CachedWeatherRepository>();
    }

    private static void AddNews(IServiceCollection services)
    {
        services.AddScoped<NewsRepository>();

        services.AddScoped<INewsRepository, CachedNewsRepository>();
    }

    /// <summary>Bağlantı metnini okur ve zaman aşımlarını uygular; ayar tanımlı değilse null döner.</summary>
    private static string? BuildConnectionString(IConfiguration configuration, string connectionName)
    {
        var connectionString = configuration.GetConnectionString(connectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        // Zaman aşımları bağlantı metnine bırakılmaz, her ortamda buradan zorunlu olarak uygulanır.
        // Her veritabanının süresi kendi bölümünden okunur (örnek: "Broadage:ConnectTimeoutSeconds").
        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = ReadSeconds(configuration, $"{connectionName}:ConnectTimeoutSeconds", DefaultConnectTimeoutSeconds),
            DefaultCommandTimeout = ReadSeconds(configuration, $"{connectionName}:CommandTimeoutSeconds", DefaultCommandTimeoutSeconds),
        };

        return builder.ConnectionString;
    }

    private static uint ReadSeconds(IConfiguration configuration, string key, uint defaultValue) =>
        uint.TryParse(configuration[key], out var seconds) && seconds > 0 ? seconds : defaultValue;
}
