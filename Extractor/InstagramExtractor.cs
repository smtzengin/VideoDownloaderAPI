using Newtonsoft.Json.Linq;
using System.Text;
using VideoDownloaderAPI.Models;
using VideoDownloaderAPI.Utilities;

namespace VideoDownloaderAPI.Extractor
{
    /// <summary>
    /// Instagram'dan video detaylarını almak ve video indirme işlemlerini gerçekleştiren sınıf.
    /// </summary>
    public class InstagramExtractor : BaseExtractor
    {
        #region Constructor

        /// <summary>
        /// InstagramExtractor sınıfı yapıcı metodu.
        /// </summary>
        /// <param name="processRunner">Dış işlem çalıştırıcısı.</param>
        /// <param name="logger">Loglama servisi.</param>
        public InstagramExtractor(ProcessRunner processRunner, ILogger<YouTubeExtractor> logger)
            : base(processRunner, logger) { }

        #endregion

        #region GetVideoDetailsAsync

        /// <summary>
        /// Verilen video URL'sine göre video bilgilerini alır.
        /// </summary>
        /// <param name="videoUrl">Video URL'si.</param>
        /// <returns>VideoInfo nesnesi.</returns>
        public override async Task<VideoInfo> GetVideoDetailsAsync(string videoUrl)
        {
            return await ExtractVideoInfoAsync(videoUrl);
        }

        #endregion

        #region DownloadVideoAsync

        public override async Task<byte[]> DownloadVideoAsync(string videoUrl, string formatId)
        {
            var ffmpegPath = "/app/Tools/ffmpeg"; // Docker Linux ortamında ffmpeg yolu

            if (!File.Exists(ffmpegPath))
            {
                logger.LogError($"ffmpeg bulunamadı: {ffmpegPath}");
                throw new FileNotFoundException($"ffmpeg bulunamadı: {ffmpegPath}");
            }

            // Geçici dosya yolu oluştur
            string tempFilePath = Path.Combine("/tmp", $"{Guid.NewGuid()}.mp4");

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



        #endregion

        #region ExtractVideoInfoAsync

        private async Task<VideoInfo> ExtractVideoInfoAsync(string videoUrl)
        {
            var (output, error, exitCode) = await processRunner.RunProcessForOutputAsync(ytDlpPath, $"-j \"{videoUrl}\"");

            if (exitCode != 0)
            {
                logger.LogError($"yt-dlp hatası (Instagram): {error}");
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
                AddBestVideoAndAudioFormats(formats, info, videoUrl);
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

        #endregion

        #region AddBestVideoAndAudioFormats

        /// <summary>
        /// Video ve ses formatlarını indirilebilir seçenekler listesine ekler.
        /// </summary>
        /// <param name="formats">Mevcut formatlar.</param>
        /// <param name="info">Video bilgileri.</param>
        /// <param name="videoUrl">Video URL'si.</param>
        private void AddBestVideoAndAudioFormats(JArray formats, VideoInfo info, string videoUrl)
        {
            var allowedHeights = new[] { 568, 1024, 1280, 1920 };

            var videoFormats = formats
                .Where(f => f["vcodec"]?.ToString() != "none")
                .Where(f => allowedHeights.Contains(f["height"]?.ToObject<int?>() ?? 0))
                .ToList();

            var bestAudio = GetBestAudioFormat(formats);

            if (videoFormats.Count == 0 || bestAudio == null)
            {
                logger.LogWarning("Instagram için uygun format bulunamadı.");
                throw new Exception("Bu platformdan video bilgisi alınamadı veya desteklenmiyor.");
            }

            var bestVideoFormats = videoFormats
                .GroupBy(f => f["height"]?.ToObject<int?>())
                .Select(g => g.OrderByDescending(f => f["tbr"]?.ToObject<double?>() ?? 0).FirstOrDefault())
                .Where(f => f != null)
                .OrderByDescending(f => f["height"]?.ToObject<int?>() ?? 0)
                .ToList();

            foreach (var format in bestVideoFormats)
            {
                if (format != null)
                {
                    var formatId = format["format_id"]?.ToString() ?? "unknown";
                    var ext = format["ext"]?.ToString() ?? "mp4";
                    var height = format["height"]?.ToObject<int?>() ?? 0;
                    var resolution = $"{height}p";
                    var fps = format["fps"]?.ToObject<int?>() ?? 30;

                    // Video bitrate (vbr) ve audio bitrate (abr) ayrı ayrı alınıyor
                    var videoBitrate = format["vbr"]?.ToObject<double?>() ?? 0;
                    var audioBitrate = bestAudio["abr"]?.ToObject<double?>() ?? 0; // bestAudio üzerinden abr alınıyor

                    logger.LogWarning($"VBR: {videoBitrate}, ABR: {audioBitrate}, Total: {videoBitrate + audioBitrate}");

                    // Süreyi saniye cinsinden hesaplama
                    double duration = ConvertHelper.ConvertDurationToSeconds(info.DurationString);

                    // Toplam bitrate ile dosya boyutunu MB olarak hesaplama
                    var totalMB = ConvertHelper.CalculateFileSizeInMB(videoBitrate + audioBitrate, duration);

                    info.DownloadOptions.Add(new DownloadOption
                    {
                        Format = $"{format["format_id"]}+{bestAudio["format_id"]}",
                        Resolution = resolution,
                        Url = videoUrl,
                        Extension = ext,
                        FrameRate = fps,
                        DownloadSize = totalMB
                    });
                }
            }
        }


        #endregion


    }
}
