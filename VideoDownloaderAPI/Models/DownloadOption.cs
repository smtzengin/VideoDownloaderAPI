namespace VideoDownloaderAPI.Models
{
    public class DownloadOption
    {
        public string Format { get; set; }
        public string Resolution { get; set; }
        public string Url { get; set; }
        public string Extension { get; set; }
        public long FileSize { get; set; } // Non-nullable, çünkü null değer atamıyoruz
        public int? Bitrate { get; set; }   // Nullable int
        public int? Framerate { get; set; } // Nullable int
    }
}
