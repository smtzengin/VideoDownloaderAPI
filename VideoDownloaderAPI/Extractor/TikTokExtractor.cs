using Newtonsoft.Json.Linq;
using VideoDownloaderAPI.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace VideoDownloaderAPI.Extractor
{
    public class TikTokExtractor : BaseExtractor
    {
        public TikTokExtractor(ProcessRunner processRunner, ILogger<YouTubeExtractor> logger)
            : base(processRunner, logger) { }
        public override async Task<VideoInfo> GetVideoDetailsAsync(string videoUrl)
        {
            return await ExtractVideoInfoAsync(videoUrl);
        }

        public override async Task<byte[]> DownloadVideoAsync(string videoUrl, string formatId)
        {
            var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                logger.LogError($"ffmpeg bulunamadı: {ffmpegPath}");
                throw new FileNotFoundException($"ffmpeg bulunamadı: {ffmpegPath}");
            }

            // Geçici dosya yolu oluştur
            string tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder); // Downloads klasörü yoksa oluştur
            }

            string tempFilePath = Path.Combine(tempFolder, $"{Guid.NewGuid()}.mp4");

            // Ses codec'ini AAC'ye dönüştürmek için gerekli argümanlar
            var arguments = $"-f {formatId} --merge-output-format mp4 --ffmpeg-location \"{ffmpegPath}\" " +
                            $"--postprocessor-args \"ffmpeg:-c:a aac\" " +
                            $"--no-check-certificate --no-playlist --verbose \"{videoUrl}\" -o \"{tempFilePath}\"";

            logger.LogInformation($"TikTok videosu indirilmeye başlıyor: {videoUrl}, Geçici Dosya Yolu: {tempFilePath}");

            // Komutu çalıştır ve dosyayı indir
            await processRunner.RunProcessAsync(ytDlpPath, arguments);

            // İndirilen dosyanın varlığını kontrol edin
            if (!File.Exists(tempFilePath))
            {
                throw new FileNotFoundException($"Dosya bulunamadı: {tempFilePath}");
            }

            // Geçici dosyayı `byte[]` olarak belleğe oku
            byte[] videoBytes = await File.ReadAllBytesAsync(tempFilePath);

            // Geçici dosyayı sil
            File.Delete(tempFilePath);

            return videoBytes;
        }



        private async Task<VideoInfo> ExtractVideoInfoAsync(string videoUrl)
        {
            var (output, error, exitCode) = await processRunner.RunProcessForOutputAsync(ytDlpPath, $"-j \"{videoUrl}\"");

            if (exitCode != 0)
            {
                logger.LogError($"yt-dlp hatası (TikTok): {error}");
                throw new Exception($"yt-dlp hatası: {error}");
            }

            var json = JObject.Parse(output);
            var durationInSeconds = json["duration"]?.ToObject<double?>() ?? 0;
            var durationTimeSpan = TimeSpan.FromSeconds(durationInSeconds);

            var info = new VideoInfo
            {
                ChannelName = json["channel"]?.ToString() ?? "unknown",
                ChannelFollowerCount = json["channel_follower_count"]?.ToObject<uint?>(),
                Title = json["title"]?.ToString() ?? "video",
                Url = json["webpage_url"]?.ToString() ?? videoUrl,
                Thumbnail = json["thumbnail"]?.ToString(),
                DurationString = durationTimeSpan.ToString(@"hh\:mm\:ss"),
                LikeCount = json["like_count"]?.ToObject<uint?>(),
                ViewCount = json["view_count"]?.ToObject<int?>(),
                DownloadOptions = new List<DownloadOption>()
            };

            var formats = json["formats"] as JArray;
            if (formats != null)
            {
                // İzin verilen maksimum yükseklikler
                var allowedHeights = new[] { 1920, 1280, 1024 };

                // Hem video hem de ses içeren formatları filtreleme
                var videoFormats = formats
                    .Where(f => f["vcodec"]?.ToString() != "none")
                    .Where(f =>
                    {
                        var height = f["height"]?.ToObject<int>() ?? 0;
                        return allowedHeights.Contains(height);
                    })
                    .ToList();

                if (videoFormats.Count == 0)
                {
                    logger.LogWarning("TikTok için uygun format bulunamadı.");
                    throw new Exception("Bu platformdan video bilgisi alınamadı veya desteklenmiyor.");
                }

                // Her yükseklik için en iyi formatı seçmek
                var bestVideoFormats = videoFormats
                    .GroupBy(f => f["height"]?.ToObject<int>() ?? 0)
                    .Select(g => g.OrderByDescending(f => f["tbr"]?.ToObject<double>() ?? 0).First())
                    .OrderByDescending(f => f["height"]?.ToObject<int>() ?? 0)
                    .ToList();

                foreach (var format in bestVideoFormats)
                {
                    var formatId = format["format_id"]?.ToString() ?? "unknown";
                    var ext = format["ext"]?.ToString() ?? "mp4";
                    var height = format["height"]?.ToObject<int>() ?? 0;
                    var resolution = $"{height}p";
                    var fps = format["fps"]?.ToObject<int>() ?? 30;
                    var fSize = format["filesize"]?.ToObject<double?>() ?? 0;

                    double fSizeMB = Math.Round(fSize / (1024 * 1024), 2);

                    info.DownloadOptions.Add(new DownloadOption
                    {
                        Format = formatId,
                        Resolution = resolution,
                        Url = videoUrl,
                        Extension = ext,
                        FrameRate = fps,
                        DownloadSize = fSizeMB
                    });

                    // Loglama
                    logger.LogDebug($"TikTok Format - ID: {formatId}, Resolution: {resolution}, FPS: {fps}, Extension: {ext}, TBR: {format["tbr"]}");
                }
            }
            else
            {
                logger.LogWarning("TikTok için format bilgisi bulunamadı.");
            }

            if (info.DownloadOptions.Count == 0)
            {
                logger.LogWarning("TikTok için indirilebilir formatlar bulunamadı.");
                throw new Exception("Bu platformdan video bilgisi alınamadı veya desteklenmiyor.");
            }

            return info;
        }
    }
}
