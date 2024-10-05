using System.Threading.Tasks;
using VideoDownloaderAPI.Models;

namespace VideoDownloaderAPI.Services
{
    public interface IVideoService
    {
        Task<VideoInfo> GetVideoDetailsAsync(string videoUrl);
        Task<byte[]> DownloadVideoAsync(DownloadRequest request);
    }

}
