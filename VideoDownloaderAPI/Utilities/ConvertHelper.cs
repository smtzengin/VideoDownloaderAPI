namespace VideoDownloaderAPI.Utilities
{
    public static class ConvertHelper
    {
        public static double CalculateFileSizeInMB(double bitrateKbps, double durationInSeconds)
        {
            // Bitrate'ten byte'a dönüşüm
            double fileSizeInBytes = (bitrateKbps * 1024 / 8) * durationInSeconds;

            // Byte'tan MB'ye dönüşüm
            double fileSizeInMB = fileSizeInBytes / (1024 * 1024);

            // Sonucu iki ondalığa yuvarlayarak döndürme
            return Math.Round(fileSizeInMB, 2);
        }

        public static double ConvertDurationToSeconds(string durationString)
        {
            // Süreyi TimeSpan'e çevirme
            TimeSpan duration;

            if (TimeSpan.TryParse(durationString, out duration))
            {
                // Saniye cinsinden döndürme
                return duration.TotalSeconds;
            }
            else
            {
                throw new FormatException("Süre formatı geçersiz.");
            }
        }
    }
}