using Newtonsoft.Json.Linq;
using VideoDownloaderAPI.Models;
using System.Linq;

namespace VideoDownloaderAPI.Extractor
{
    public class YouTubeExtractor : IVideoExtractor
    {
        private readonly string ytDlpPath;
        private readonly ProcessRunner processRunner;
        private readonly ILogger<YouTubeExtractor> logger;

        public YouTubeExtractor(ProcessRunner processRunner, ILogger<YouTubeExtractor> logger)
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

            // AAC formatına dönüştürmek için --audio-format aac ve ses codec'ini zorlamak için --postprocessor-args '-c:a aac' ekledik
            var arguments = $"-f {formatId} --merge-output-format mp4 --ffmpeg-location \"{ffmpegPath}\" " +
                            $"--audio-format aac --audio-quality 192K " +
                            $"--postprocessor-args \"-c:a aac\" " +
                            $"-o \"{filePath}\" \"{videoUrl}\" " +
                            "--no-check-certificate --no-playlist --verbose";

            logger.LogInformation($"Video indirilmeye başlıyor: {videoUrl}, Format ID: {formatId}.");

            var startTime = DateTime.Now;

            var (output, error, exitCode) = await processRunner.RunProcessAsync(ytDlpPath, arguments);
            
            var endTime = DateTime.Now;
            logger.LogInformation($"İndirme süresi: {(endTime - startTime).TotalSeconds} saniye.");

            if (exitCode != 0)
            {
                logger.LogError($"YouTube indirme hatası: {error}");
                throw new Exception($"YouTube indirme hatası: {error}");
            }

            logger.LogInformation($"İndirme tamamlandı: {videoUrl}. Dosya konumu: {filePath}.");
            return filePath;
        }

        private async Task<VideoInfo> ExtractVideoInfoAsync(string videoUrl)
        {
            var (output, error, exitCode) = await processRunner.RunProcessAsync(ytDlpPath, $"-j \"{videoUrl}\"");

            if (exitCode != 0)
            {
                logger.LogError($"yt-dlp error (YouTube): {error}");
                throw new Exception($"yt-dlp error: {error}");
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
                // Video ve ses formatlarını ayırma
                var videoFormats = formats
                    .Where(f => f["vcodec"]?.ToString() != "none")
                    .ToList();

                var audioFormats = formats
                    .Where(f => f["acodec"]?.ToString() != "none")
                    .OrderByDescending(f => f["abr"]?.ToObject<int?>() ?? 0)
                    .ToList();

                // En iyi ses formatını seçme (en yüksek bitrate)
                var bestAudio = audioFormats.FirstOrDefault();

                // Yatay ve dikey çözünürlükler
                var allowedResolutionsHorizontal = new[] { 144, 360, 480, 720, 1080, 1440, 2160 };
                var allowedResolutionsVertical = new[] { 568, 1024, 1280, 1920 };

                // Yatay ve dikey video çözünürlüklerini ayırt edelim
                var bestVideoFormats = videoFormats
                    .Where(f =>
                    {
                        var height = f["height"]?.ToObject<int?>() ?? 0;
                        var width = f["width"]?.ToObject<int?>() ?? 0;

                        // Shorts videolarının dikey olduğunu varsayıyoruz (yükseklik > genişlik)
                        if (height > width)
                        {
                            return allowedResolutionsVertical.Contains(height);
                        }
                        else
                        {
                            return allowedResolutionsHorizontal.Contains(height);
                        }
                    })
                    .GroupBy(f => f["height"]?.ToObject<int?>())
                    .Select(g => g.OrderByDescending(f => f["tbr"]?.ToObject<double>() ?? 0).First())
                    .OrderByDescending(f => f["height"]?.ToObject<int?>() ?? 0)
                    .ToList();

                foreach (var videoFormat in bestVideoFormats)
                {
                    var height = videoFormat["height"]?.ToObject<int?>() ?? 0;
                    var resolution = $"{height}p";
                    var fps = videoFormat["fps"]?.ToObject<int?>() ?? 0;
                    var ext = videoFormat["ext"]?.ToString() ?? "mp4";

                    if (bestAudio != null)
                    {
                        info.DownloadOptions.Add(new DownloadOption
                        {
                            Format = $"{videoFormat["format_id"]}+{bestAudio["format_id"]}",
                            Resolution = resolution,
                            Extension = ext,
                            Url = videoUrl,
                            FrameRate = fps
                        });

                        // Loglama
                        logger.LogDebug($"YouTube Format - ID: {videoFormat["format_id"]}, Resolution: {resolution}, FPS: {fps}, Extension: {ext}, TBR: {videoFormat["tbr"]}");
                    }
                }
            }
            else
            {
                logger.LogWarning("YouTube için format bilgisi bulunamadı.");
            }

            if (info.DownloadOptions.Count == 0)
            {
                logger.LogWarning("YouTube için indirilebilir formatlar bulunamadı.");
                throw new Exception("Bu platformdan video bilgisi alınamadı veya desteklenmiyor.");
            }

            return info;
        }
    }
}
