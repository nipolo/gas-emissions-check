namespace GEC.InspectionService.Infrastructure.IPCamera.Module;

public class TPLinkTapoSnapshotOptions
{
    public const string Key = nameof(TPLinkTapoSnapshotOptions);

    public string CameraIp { get; init; }

    public int RtspPort { get; init; }

    public string CameraUsername { get; init; }

    public string CameraPassword { get; init; }

    public string StreamPath { get; init; }

    public string FfmpegPath { get; init; }

    public int TimeoutSeconds { get; init; }
}
