using Newtonsoft.Json.Linq;
using VideoDownloaderAPI.Models;

namespace VideoDownloaderAPI.Extractor
{
    /// <summary>
    /// Temel video çekme işlemlerini gerçekleştiren soyut sınıf.
    /// Tüm video platformlarına özel işlemler bu sınıfı genişleterek yapılır.
    /// </summary>
    public abstract class BaseExtractor : IVideoExtractor
    {
        protected readonly string ytDlpPath;
        protected readonly ProcessRunner processRunner;
        protected readonly ILogger logger;

        /// <summary>
        /// BaseExtractor yapıcı metodu, gerekli yolları ve loglama işlemlerini ayarlar.
        /// </summary>
        /// <param name="processRunner">Dış işlem çalıştırıcısı.</param>
        /// <param name="logger">Loglama servisi.</param>
        protected BaseExtractor(ProcessRunner processRunner, ILogger logger)
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools");
            ytDlpPath = Path.Combine(toolsPath, "yt-dlp.exe");
            this.processRunner = processRunner;
            this.logger = logger;
        }

        /// <summary>
        /// Belirtilen video URL'sine göre video bilgilerini döner.
        /// Bu metot her bir platforma özgü detayların elde edilmesi için genişletilmelidir.
        /// </summary>
        /// <param name="videoUrl">Video URL'si.</param>
        /// <returns>Video hakkında detaylı bilgi döner.</returns>
        public abstract Task<VideoInfo> GetVideoDetailsAsync(string videoUrl);

        /// <summary>
        /// Seçilen format ve çözünürlüğe göre videoyu indirir.
        /// Bu metot her bir platforma özgü indirme işlemleri için genişletilmelidir.
        /// </summary>
        /// <param name="videoUrl">İndirilecek video URL'si.</param>
        /// <param name="formatId">İndirilecek video formatı ID'si.</param>
        /// <param name="filePath">Videonun kaydedileceği dosya yolu.</param>
        /// <returns>İndirilen video dosyasının yolu.</returns>
        public abstract Task<string> DownloadVideoAsync(string videoUrl, string formatId, string filePath);

        /// <summary>
        /// Verilen saniyeyi bir TimeSpan nesnesine dönüştürür.
        /// </summary>
        /// <param name="seconds">Saniye cinsinden video süresi.</param>
        /// <returns>Video süresi olarak TimeSpan nesnesi döner.</returns>
        protected TimeSpan GetDurationFromSeconds(double? seconds)
        {
            return TimeSpan.FromSeconds(seconds ?? 0);
        }

        /// <summary>
        /// Mevcut formatlar arasından en iyi ses formatını seçer.
        /// </summary>
        /// <param name="formats">Video ve ses formatları listesi.</param>
        /// <returns>En iyi ses formatını döner, bulunamazsa null döner.</returns>
        protected JToken? GetBestAudioFormat(JArray formats) => formats
            .Where(f => f["acodec"]?.ToString() != "none")
            .OrderByDescending(f => f["abr"]?.ToObject<int?>() ?? 0)
            .FirstOrDefault();
    }
}
