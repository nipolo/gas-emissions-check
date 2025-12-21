using System.Text.Json.Serialization;

namespace GEC.InspectionService.Infrastructure.PlateRecognizer.Dtos;

internal sealed class PlateResult
{
    [JsonPropertyName("plate")]
    public string Plate { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public decimal Score { get; set; }
}
