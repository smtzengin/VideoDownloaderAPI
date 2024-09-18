using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoDownloaderAPI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static PlatformDetector;
using VideoDownloaderAPI.Extractor;

namespace VideoDownloaderAPI.Services
{
    public class VideoService : IVideoService
    {
        private readonly ProcessRunner processRunner;
        private readonly ILogger<VideoService> logger;
        private readonly IServiceProvider serviceProvider;

        public VideoService(ProcessRunner processRunner, ILogger<VideoService> logger, IServiceProvider serviceProvider)
        {
            this.processRunner = processRunner;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task<VideoInfo> GetVideoDetailsAsync(string videoUrl)
        {
            var platform = PlatformDetector.GetPlatform(videoUrl);
            IVideoExtractor extractor = GetExtractor(platform);

            if (extractor == null)
            {
                throw new Exception("Desteklenmeyen platform.");
            }

            return await extractor.GetVideoDetailsAsync(videoUrl);
        }

        public async Task<byte[]> DownloadVideoAsync(DownloadRequest request)
        {
            var platform = PlatformDetector.GetPlatform(request.VideoUrl);
            IVideoExtractor extractor = GetExtractor(platform);

            if (extractor == null)
            {
                throw new Exception("Desteklenmeyen platform.");
            }

            string safeTitle = await GetSafeTitleAsync(request.VideoUrl);
            string downloadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            if (!Directory.Exists(downloadFolder))
                Directory.CreateDirectory(downloadFolder);

            var selectedOption = (await extractor.GetVideoDetailsAsync(request.VideoUrl)).DownloadOptions
                .FirstOrDefault(o => o.Format == request.SelectedFormat);

            if (selectedOption == null)
            {
                throw new Exception("Seçilen format bulunamadı.");
            }

            string fileName = $"{safeTitle}.{selectedOption.Extension}";
            string filePath = Path.Combine(downloadFolder, fileName);

            await extractor.DownloadVideoAsync(request.VideoUrl, request.SelectedFormat, filePath);

            return await File.ReadAllBytesAsync(filePath); // Byte array olarak döndür.
        }


        private IVideoExtractor GetExtractor(PlatformType platform)
        {
            switch (platform)
            {
                case PlatformType.YouTube:
                    return serviceProvider.GetService<YouTubeExtractor>();
                case PlatformType.Instagram:
                    return serviceProvider.GetService<InstagramExtractor>();
                case PlatformType.TikTok:
                    return serviceProvider.GetService<TikTokExtractor>();
                default:
                    return null;
            }
        }

        private async Task<string> GetSafeTitleAsync(string videoUrl)
        {
            var videoInfo = await GetVideoDetailsAsync(videoUrl);
            string title = videoInfo.Title;

            if (!string.IsNullOrEmpty(title) && title.Length <= 150)
            {
                // Geçersiz karakterleri kaldırma
                return string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
            }
            else
            {
                return "video";
            }
        }
    }

}
