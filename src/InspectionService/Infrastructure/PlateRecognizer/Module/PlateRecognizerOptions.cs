namespace GEC.InspectionService.Infrastructure.PlateRecognizer.Module;

public class PlateRecognizerOptions
{
    public const string Key = nameof(PlateRecognizerOptions);

    public string ApiToken { get; set; }

    public string BaseUrl { get; set; }

    public string PlateReaderPath { get; set; }

    public decimal? MinimumScore { get; set; }

    public int TimeoutSeconds { get; init; }
}
