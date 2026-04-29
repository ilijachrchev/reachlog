namespace ReachLog.Infrastructure.Models;

internal sealed record ParsedCv(
    string Name,
    string Contact,
    IReadOnlyList<CvSection> Sections
);

internal sealed record CvSection(
    string Title,
    IReadOnlyList<CvEntry> Entries
);

internal sealed record CvEntry(
    string Organization,
    string Date,
    string Role,
    string Location,
    IReadOnlyList<string> Bullets,
    string? FlowText = null
);
