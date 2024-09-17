using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoDownloaderAPI.Models;
using VideoDownloaderAPI.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VideoDownloaderAPI.Services
{
    public class VideoService : IVideoService
    {
        private readonly string ytDlpPath;
        private readonly ProcessRunner processRunner;
        private readonly ILogger<VideoService> logger;

        public VideoService(ProcessRunner processRunner, ILogger<VideoService> logger)
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools");
            ytDlpPath = Path.Combine(toolsPath, "yt-dlp.exe");
            this.processRunner = processRunner;
            this.logger = logger;
        }

        public async Task<VideoInfo> GetVideoDetailsAsync(string videoUrl)
        {
            logger.LogInformation($"Fetching video details for URL: {videoUrl}");
            try
            {
                var (output, error, exitCode) = await processRunner.RunProcessAsync(ytDlpPath, $"-j \"{videoUrl}\"");

                if (exitCode != 0)
                {
                    logger.LogError($"yt-dlp hatası: {error}");
                    throw new Exception($"yt-dlp hatası: {error}");
                }

                logger.LogDebug($"yt-dlp JSON Output: {output}");

                var json = JObject.Parse(output);
                var info = new VideoInfo
                {
                    Title = json["title"]?.ToString(),
                    Url = json["webpage_url"]?.ToString(),
                    Duration = json["duration"]?.ToObject<double?>(),
                    ViewCount = json["view_count"]?.ToObject<int?>(),
                    DownloadOptions = new List<DownloadOption>()
                };

                // Format bilgilerini JSON'dan alıyoruz
                var formats = json["formats"] as JArray;
                if (formats != null)
                {
                    foreach (var format in formats)
                    {
                        var formatId = format["format_id"]?.ToString();
                        var ext = format["ext"]?.ToString();
                        var resolution = format["resolution"]?.ToString() ?? format["height"]?.ToString() ?? "unknown";
                        var filesize = format["filesize"]?.Value<long>() ?? 0;

                        // Bitrate ve Framerate eklemek için
                        var bitrate = format["tbr"]?.ToObject<int?>();
                        var framerate = format["fps"]?.ToObject<int?>();

                        // Örnek olarak, video ve audio formatlarını dahil ediyoruz
                        if (format["vcodec"]?.ToString() != "none" || format["acodec"]?.ToString() != "none")
                        {
                            info.DownloadOptions.Add(new DownloadOption
                            {
                                Format = formatId,
                                Resolution = resolution,
                                Url = videoUrl, // Daha detaylı linkler oluşturmak için ek bilgiler ekleyebilirsiniz
                                Extension = ext,
                                FileSize = filesize,
                                Bitrate = bitrate,
                                Framerate = framerate
                            });
                        }
                    }
                }
                else
                {
                    logger.LogWarning("Formats bilgisi JSON içinde bulunamadı.");
                }

                // İndirme seçenekleri listesi boşsa uyarı ver ve hata fırlat
                if (info.DownloadOptions.Count == 0)
                {
                    logger.LogWarning("İndirme seçenekleri bulunamadı. Belki de bu platform desteklenmiyor.");
                    throw new Exception("Bu platformdan video bilgisi alınamadı veya desteklenmiyor.");
                }

                return info;
            }
            catch (JsonReaderException jex)
            {
                logger.LogError(jex, "JSON parse edilirken hata oluştu.");
                throw new Exception("Video bilgilerini işlerken JSON hatası oluştu.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Video detayları alınırken genel bir hata oluştu.");
                throw;
            }
        }

        public async Task<string> DownloadVideoAsync(DownloadRequest request)
        {
            var downloadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            if (!Directory.Exists(downloadFolder))
                Directory.CreateDirectory(downloadFolder);

            // Güvenli dosya ismi oluşturma
            var safeTitle = string.Join("_", request.VideoUrl.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{Guid.NewGuid()}_{safeTitle}.{request.SelectedFormat}";
            var filePath = Path.Combine(downloadFolder, fileName);

            var arguments = $"-f {request.SelectedFormat} -o \"{filePath}\" \"{request.VideoUrl}\"";

            var (output, error, exitCode) = await processRunner.RunProcessAsync(ytDlpPath, arguments);

            if (exitCode != 0)
                throw new Exception($"İndirme hatası: {error}");

            return filePath;
        }
    }
}
