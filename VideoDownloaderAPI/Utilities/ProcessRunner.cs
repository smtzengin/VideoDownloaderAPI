using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VideoDownloaderAPI.Utilities
{
    public class ProcessRunner
    {
        private readonly ILogger<ProcessRunner> logger;

        public ProcessRunner(ILogger<ProcessRunner> logger)
        {
            this.logger = logger;
        }

        public async Task<(string output, string error, int exitCode)> RunProcessAsync(string fileName, string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                logger.LogInformation($"Process '{fileName} {arguments}' exited with code {process.ExitCode}.");

                if (!string.IsNullOrEmpty(output))
                    logger.LogDebug($"Process Output: {output}");
                if (!string.IsNullOrEmpty(error))
                    logger.LogDebug($"Process Error: {error}");

                return (output, error, process.ExitCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Process çalıştırılırken hata oluştu.");
                throw;
            }
        }
    }
}
