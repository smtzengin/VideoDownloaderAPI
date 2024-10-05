using VideoDownloaderAPI.Models;

namespace VideoDownloaderAPI.Extractor
{
    public interface IVideoExtractor
    {
        Task<VideoInfo> GetVideoDetailsAsync(string videoUrl);
        Task<byte[]> DownloadVideoAsync(string videoUrl, string formatId);
    }
}
