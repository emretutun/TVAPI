namespace TVAPI.Domain.Teams;

/// <summary>Bir takımın künyesi. Kaynakta boş bırakılan bilgiler null olur.</summary>
public sealed record Team(
    int Id,
    string Name,
    string ShortName,
    string MediumName,
    string? CoachName,
    Country? Country,
    Stadium? Stadium);

public sealed record Country(int Id, string Name, string ShortName);

public sealed record Stadium(int Id, string Name, string ShortName, string? CityName);
