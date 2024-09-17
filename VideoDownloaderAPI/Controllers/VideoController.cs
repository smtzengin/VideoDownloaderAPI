﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using VideoDownloaderAPI.Models;
using VideoDownloaderAPI.Services;
using Microsoft.Extensions.Logging;

namespace VideoDownloaderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IVideoService videoService;
        private readonly ILogger<VideoController> logger;

        public VideoController(IVideoService videoService, ILogger<VideoController> logger)
        {
            this.videoService = videoService;
            this.logger = logger;
        }

        /// <summary>
        /// Video bilgilerini almak için kullanılır.
        /// </summary>
        /// <param name="videoUrl">İndirilecek videonun URL'si</param>
        /// <returns>Video bilgileri</returns>
        [HttpPost("info")]
        public async Task<IActionResult> GetVideoInfo([FromBody] string videoUrl)
        {
            if (string.IsNullOrWhiteSpace(videoUrl) || !IsValidUrl(videoUrl))
                return BadRequest(new { error = "Geçerli bir Video URL gerekli." });

            try
            {
                var info = await videoService.GetVideoDetailsAsync(videoUrl);
                return Ok(info);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Video bilgileri alınırken hata oluştu.");
                return BadRequest(new { error = ex.Message });
            }

        }

        /// <summary>
        /// Seçilen format ve çözünürlüğe göre videoyu indirmek için kullanılır.
        /// </summary>
        /// <param name="request">İndirme isteği bilgileri</param>
        /// <returns>İndirme işlemi hakkında bilgi</returns>
        [HttpPost("download")]
        public async Task<IActionResult> DownloadVideo([FromBody] DownloadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VideoUrl) ||
                string.IsNullOrWhiteSpace(request.SelectedFormat) ||
                string.IsNullOrWhiteSpace(request.SelectedResolution))
            {
                return BadRequest(new { error = "Gerekli tüm alanlar doldurulmalıdır." });
            }

            if (!IsValidUrl(request.VideoUrl))
                return BadRequest(new { error = "Geçerli bir Video URL gerekli." });

            try
            {
                var downloadPath = await videoService.DownloadVideoAsync(request);
                return Ok(new { message = "İndirme başladı.", downloadPath });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "İndirme sırasında hata oluştu.");
                return BadRequest(new { error = ex.Message });
            }
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}