using System.Diagnostics;
using System.Text;

public class ProcessRunner
{
    public async Task<(string Output, string Error, int ExitCode)> RunProcessAsync(string fileName, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                output.AppendLine(args.Data);
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                error.AppendLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return (output.ToString(), error.ToString(), process.ExitCode);
    }
}
