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

            var selectedOption = (await extractor.GetVideoDetailsAsync(request.VideoUrl)).DownloadOptions
                .FirstOrDefault(o => o.Format == request.SelectedFormat);

            if (selectedOption == null)
            {
                throw new Exception("Seçilen format bulunamadı.");
            }

            // Videoyu indir ve bellekte tut
            var videoBytes = await extractor.DownloadVideoAsync(request.VideoUrl, request.SelectedFormat);

            return videoBytes; // Byte array olarak döndür
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
