using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GEC.InspectionService.Infrastructure.PlateRecognizer.Dtos;

internal class PlateRecognizerResponseDto
{
    [JsonPropertyName("results")]
    public List<PlateResult> Results { get; set; } = [];
}
