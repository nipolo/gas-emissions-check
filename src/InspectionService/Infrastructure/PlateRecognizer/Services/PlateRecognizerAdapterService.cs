using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using GEC.Common.Consts;

using GEC.InspectionService.Infrastructure.PlateRecognizer.Dtos;
using GEC.InspectionService.Infrastructure.PlateRecognizer.Module;
using GEC.InspectionService.Infrastructure.PlateRecognizer.Services.Abstractions;

using Microsoft.Extensions.Options;

namespace GEC.InspectionService.Infrastructure.PlateRecognizer.Services;

public class PlateRecognizerAdapterService : IPlateRecognizerAdapterService
{
    private const string SampleImageFileName = "image.png";
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PlateRecognizerOptions _options;

    public PlateRecognizerAdapterService(
        IHttpClientFactory httpClientFactory,
        IOptions<PlateRecognizerOptions> options)
    {
        _httpClientFactory = httpClientFactory;

        _options = options.Value;
    }

    public async Task<string> TryGetPlateNumberAsync(
        byte[] pngImageBytes,
        CancellationToken cancellationToken = default)
    {
        if (pngImageBytes is null || pngImageBytes.Length == 0)
        {
            throw new ArgumentException("Image bytes are null or empty.", nameof(pngImageBytes));
        }

        var httpClient = _httpClientFactory.CreateClient(HttpClientConsts.PlateRecognizer);

        using var form = new MultipartFormDataContent();

        var imageContent = new ByteArrayContent(pngImageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

        form.Add(imageContent, "upload", SampleImageFileName);

        using var response = await httpClient.PostAsync(
            _options.PlateReaderPath,
            form,
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"PlateRecognizer request failed. Status: {(int)response.StatusCode}. Body: {json}");
        }

        var parsed = JsonSerializer.Deserialize<PlateRecognizerResponseDto>(json, _jsonOptions);

        if (parsed.Results is null || parsed.Results.Count == 0)
        {
            return null;
        }

        var bestResult = parsed.Results.OrderByDescending(r => r.Score).FirstOrDefault();

        if (bestResult is null || string.IsNullOrWhiteSpace(bestResult.Plate))
        {
            return null;
        }

        if (_options.MinimumScore.HasValue && bestResult.Score < _options.MinimumScore.Value)
        {
            return null;
        }

        return bestResult.Plate.ToUpperInvariant();
    }
}
