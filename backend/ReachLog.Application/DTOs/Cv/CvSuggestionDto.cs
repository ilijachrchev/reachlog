namespace ReachLog.Application.DTOs.Cv;

public record CvSuggestionDto(
    string Id,
    string Section,
    string Type,
    string OriginalText,
    string SuggestedText,
    string Reason
);
