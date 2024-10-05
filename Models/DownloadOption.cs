namespace VideoDownloaderAPI.Models
{
    public class DownloadOption
    {
        public string Format { get; set; }
        public string Resolution { get; set; }
        public string Url { get; set; }
        public string Extension { get; set; }
        public int? FrameRate { get; set; }
        public double? DownloadSize { get; set; }
    }

}
