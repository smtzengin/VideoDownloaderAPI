namespace VideoDownloaderAPI.Models
{
    public class DownloadRequest
    {
        public string VideoUrl { get; set; }
        public string SelectedFormat { get; set; }
        public string SelectedResolution { get; set; }
    }
}
