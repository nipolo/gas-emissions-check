using System;
using System.Threading;
using System.Threading.Tasks;

using GEC.Common.Shared.Utils;

using GEC.InspectionService.Infrastructure.IPCamera.Module;
using GEC.InspectionService.Infrastructure.IPCamera.Services.Abstractions;

using Microsoft.Extensions.Options;

namespace GEC.InspectionService.Infrastructure.IPCamera.Services;

public class TapoCameraSnapshotService : ICameraSnapshotService
{
    private readonly TPLinkTapoSnapshotOptions _tpLinkTapoSnapshotOptions;

    public TapoCameraSnapshotService(IOptions<TPLinkTapoSnapshotOptions> tpLinkTapoSnapshotOptions)
    {
        _tpLinkTapoSnapshotOptions = tpLinkTapoSnapshotOptions.Value;
    }

    public async Task<byte[]> CapturePngAsync(CancellationToken ct = default)
    {
        var rtspUrl = BuildRtspUrl();

        var ffmpegArgs =
            "-hide_banner -loglevel error " +
            "-rtsp_transport tcp " +
            $"-i \"{rtspUrl}\" " +
            "-frames:v 1 -an " +
            "-f image2 -vcodec png pipe:1";

        var result = await ProcessRunner.RunToBytesAsync(
                        _tpLinkTapoSnapshotOptions.FfmpegPath,
                        ffmpegArgs,
                        TimeSpan.FromSeconds(_tpLinkTapoSnapshotOptions.TimeoutSeconds),
                        ct);

        if (!result.IsSuccess || result.StdOut.Length == 0)
        {
            throw new InvalidOperationException(
                $"Snapshot failed. ExitCode={result.ExitCode}\n{result.StdErr}");
        }

        return result.StdOut;
    }

    private string BuildRtspUrl()
    {
        return $"rtsp://{Uri.EscapeDataString(_tpLinkTapoSnapshotOptions.CameraUsername)}:{Uri.EscapeDataString(_tpLinkTapoSnapshotOptions.CameraPassword)}@{_tpLinkTapoSnapshotOptions.CameraIp}:{_tpLinkTapoSnapshotOptions.RtspPort}/{_tpLinkTapoSnapshotOptions.StreamPath}";
    }
}
