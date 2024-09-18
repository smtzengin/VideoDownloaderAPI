public static class PlatformDetector
{
    public static PlatformType GetPlatform(string videoUrl)
    {
        if (videoUrl.Contains("youtube.com") || videoUrl.Contains("youtu.be"))
            return PlatformType.YouTube;
        else if (videoUrl.Contains("instagram.com"))
            return PlatformType.Instagram;
        else if (videoUrl.Contains("tiktok.com"))
            return PlatformType.TikTok;
        else
            return PlatformType.Unknown;
    }
}

public enum PlatformType
{
    YouTube,
    Instagram,
    TikTok,
    Unknown
}
