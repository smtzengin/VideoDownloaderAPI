using VideoDownloaderAPI.Models;

namespace VideoDownloaderAPI.Extractor
{
    public interface IVideoExtractor
    {
        Task<VideoInfo> GetVideoDetailsAsync(string videoUrl);
        Task<string> DownloadVideoAsync(string videoUrl, string formatId, string filePath);
    }
}
