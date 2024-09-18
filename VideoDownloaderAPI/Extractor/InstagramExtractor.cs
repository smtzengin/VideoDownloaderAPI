using Newtonsoft.Json.Linq;
using VideoDownloaderAPI.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoDownloaderAPI.Extractor
{
    public class InstagramExtractor : IVideoExtractor
    {
        private readonly string ytDlpPath;
        private readonly ProcessRunner processRunner;
        private readonly ILogger<InstagramExtractor> logger;

        public InstagramExtractor(ProcessRunner processRunner, ILogger<InstagramExtractor> logger)
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools");
            ytDlpPath = Path.Combine(toolsPath, "yt-dlp.exe");
            this.processRunner = processRunner;
            this.logger = logger;
        }

        public async Task<VideoInfo> GetVideoDetailsAsync(string videoUrl)
        {
            return await ExtractVideoInfoAsync(videoUrl);
        }

        public async Task<string> DownloadVideoAsync(string videoUrl, string formatId, string filePath)
        {
            var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                logger.LogError($"ffmpeg yolunda ffmpeg bulunamadı: {ffmpegPath}");
                throw new FileNotFoundException($"ffmpeg bulunamadı: {ffmpegPath}");
            }

            var arguments = $"-f {formatId} --merge-output-format mp4 --ffmpeg-location \"{ffmpegPath}\" " +
                 //$"--username \"<instagram_username>\" --password \"<instagram_password>\" " + // Kullanıcı adı ve şifre
                 $"--audio-format aac --audio-quality 192K " +
                 $"--postprocessor-args \"-c:a aac\" " +
                 $"-o \"{filePath}\" \"{videoUrl}\" " +
                 "--no-check-certificate --no-playlist --verbose";

            logger.LogInformation($"Instagram videosu indirilmeye başlıyor: {videoUrl}, Format ID: {formatId}.");

            var startTime = DateTime.Now;

            var (output, error, exitCode) = await processRunner.RunProcessAsync(ytDlpPath, arguments);

            var endTime = DateTime.Now;
            logger.LogInformation($"İndirme süresi: {(endTime - startTime).TotalSeconds} saniye.");

            if (exitCode != 0)
            {
                logger.LogError($"Instagram indirme hatası: {error}");
                throw new Exception($"Instagram indirme hatası: {error}");
            }

            logger.LogInformation($"İndirme tamamlandı: {videoUrl}. Dosya konumu: {filePath}.");
            return filePath;
        }

        private async Task<VideoInfo> ExtractVideoInfoAsync(string videoUrl)
        {
            var (output, error, exitCode) = await processRunner.RunProcessAsync(ytDlpPath, $"-j \"{videoUrl}\"");

            if (exitCode != 0)
            {
                logger.LogError($"yt-dlp hatası (Instagram): {error}");
                throw new Exception($"yt-dlp hatası: {error}");
            }

            var json = JObject.Parse(output);
            var info = new VideoInfo
            {
                Title = json["title"]?.ToString() ?? "video",
                Url = json["webpage_url"]?.ToString() ?? videoUrl,
                Thumbnail = json["thumbnail"]?.ToString(),
                Duration = json["duration"]?.ToObject<double?>(),
                ViewCount = json["view_count"]?.ToObject<int?>(),
                DownloadOptions = new List<DownloadOption>()
            };

            var formats = json["formats"] as JArray;
            if (formats != null)
            {
                // İzin verilen çözünürlükler: 568p, 1280p, 1920p
                var allowedHeights = new[] { 568, 1024,1280, 1920 };

                // Hem video hem de ses içeren formatları filtreleme
                var videoFormats = formats
                    .Where(f => f["vcodec"]?.ToString() != "none")
                    .Where(f =>
                    {
                        var height = f["height"]?.ToObject<int?>() ?? 0;
                        return allowedHeights.Contains(height);
                    })
                    .ToList();

                var audioFormats = formats
                    .Where(f => f["acodec"]?.ToString() != "none")
                    .OrderByDescending(f => f["abr"]?.ToObject<int?>() ?? 0)
                    .ToList();

                var bestAudio = audioFormats.FirstOrDefault();

                // Eğer izin verilen çözünürlüklerde format yoksa hata verelim
                if (videoFormats.Count == 0 || bestAudio == null)
                {
                    logger.LogWarning("Instagram için uygun format bulunamadı.");
                    throw new Exception("Bu platformdan video bilgisi alınamadı veya desteklenmiyor.");
                }

                // Her yükseklik için en iyi formatı seçmek
                var bestVideoFormats = videoFormats
                    .Where(f =>
                    {
                        var height = f["height"]?.ToObject<int?>() ?? 0;
                        return allowedHeights.Contains(height);
                    })
                    .GroupBy(f => f["height"]?.ToObject<int?>())
                    .Select(g => g.OrderByDescending(f => f["tbr"]?.ToObject<double?>() ?? 0).FirstOrDefault())
                    .Where(f => f != null) // null kontrolleri
                    .OrderByDescending(f => f["height"]?.ToObject<int?>() ?? 0)
                    .ToList();

                // Seçilen formatları DownloadOptions listesine ekleyelim
                foreach (var format in bestVideoFormats)
                {
                    var formatId = format["format_id"]?.ToString() ?? "unknown";
                    var ext = format["ext"]?.ToString() ?? "mp4";
                    var height = format["height"]?.ToObject<int?>() ?? 0;
                    var resolution = $"{height}p";
                    var fps = format["fps"]?.ToObject<int?>() ?? 30;

                    info.DownloadOptions.Add(new DownloadOption
                    {
                        Format = $"{format["format_id"]}+{bestAudio["format_id"]}",
                        Resolution = resolution,
                        Url = videoUrl,
                        Extension = ext,
                        FrameRate = fps
                    });

                    // Loglama
                    logger.LogDebug($"Instagram Format - ID: {formatId}, Resolution: {resolution}, FPS: {fps}, Extension: {ext}, TBR: {format["tbr"]}");
                }
            }
            else
            {
                logger.LogWarning("Instagram için format bilgisi bulunamadı.");
            }

            if (info.DownloadOptions.Count == 0)
            {
                logger.LogWarning("Instagram için indirilebilir formatlar bulunamadı.");
                throw new Exception("Bu platformdan video bilgisi alınamadı veya desteklenmiyor.");
            }

            return info;
        }
    }
}
