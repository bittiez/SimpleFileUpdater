using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia.Threading;

namespace FileUpdaterClient;

public static class UpdateHandler
{
    private const int WORKER_COUNT = 2;
    private static HttpClient client = new();
    private static ConcurrentQueue<FileEntry> downloadQueue = new();
    private static ConcurrentQueue<FileEntry> remoteFileListQueue = new();
    private static MainViewModel data;
    private static double currentMaxProgress;
    private static Dictionary<string, int> retryMap = new();
    private static long totalBytesDownloaded = 0;
    private static TimeSpan totalDownloadTime = TimeSpan.Zero;
    private static readonly object downloadStatsLock = new();
    private static DateTime lastUiUpdateTime = DateTime.MinValue;
    private static readonly CancellationTokenSource cancellationSource = new();
    private static readonly CancellationToken cancellationToken = cancellationSource.Token;

    public static async Task HandleUpdates(MainViewModel dataModel)
    {
        client.Timeout = TimeSpan.FromSeconds(5); //Initial connection
        data = dataModel;
        if (!await GetFileList()) return;

        await StartComparingFiles();

        client = new HttpClient(); //Must have new client for new timeout
        client.Timeout = TimeSpan.FromMinutes(15); //Download timeout
        await StartDownloading();

        Dispatcher.UIThread.Post(() => //Ensure the final finished text is queued in case other text updates are already queued, making sure this is the last one ran.
        {
            data.Progress = 100;
            data.ProgressText = Settings.Finished;
        });
    }

    public static void Cancel()
    {
        cancellationSource.Cancel();
    }

    private static async Task<bool> GetFileList()
    {
        data.ProgressText = Settings.ReqFileList;
        var response = await client.GetAsync(new Uri(Settings.UpdateUrl));

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            data.ErrorMessage = Settings.ConError;
            return false;
        }

        try
        {
            string json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(json))
            {
                data.ErrorMessage = Settings.BadData;
                return false;
            }

            FileEntry[] fileList = JsonSerializer.Deserialize<FileEntry[]>(json);
            Console.WriteLine(
                $"Received information for {fileList.Length} files from the server, comparing to local files..");

            foreach (var item in fileList)
            {
                remoteFileListQueue.Enqueue(item);
            }

            data.Progress = 0;
            return true;
        }
        catch (Exception e)
        {
            data.ErrorMessage = Settings.UnknownError;
            Console.WriteLine(e.Message);
            return false;
        }
    }

    private static async Task StartComparingFiles()
    {
        if (remoteFileListQueue.IsEmpty) return;

        currentMaxProgress = remoteFileListQueue.Count;
        Dispatcher.UIThread.Post(() =>
        {
            data.Progress = 0;
            data.ProgressText = string.Format(Settings.ComparingFiles, "0", currentMaxProgress);
        });

        var tasks = new List<Task>();
        for (int i = 0; i < WORKER_COUNT; i++)
        {
            tasks.Add(Task.Run(BackgroundWorker_CompareFile));
        }

        await Task.WhenAll(tasks);
    }

    private static async Task StartDownloading()
    {
        if (downloadQueue.IsEmpty) return;


        currentMaxProgress = downloadQueue.Count;
        Dispatcher.UIThread.Post(() =>
        {
            data.Progress = 0;
            data.ProgressText = string.Format(Settings.DownloadingFiles, "0", currentMaxProgress, "0");
        });

        var tasks = new List<Task>();
        for (int i = 0; i < WORKER_COUNT; i++)
        {
            tasks.Add(Task.Run(() => BackgroundWorker_DoWork()));
        }

        await Task.WhenAll(tasks);
    }

    private static void BackgroundWorker_CompareFile()
    {
        while (!cancellationToken.IsCancellationRequested && remoteFileListQueue.TryDequeue(out FileEntry file))
        {
            if (File.Exists(file.name))
            {
                if (!file.md5.Equals(GetMD5HashFromFile(file.name)))
                {
                    downloadQueue.Enqueue(file);
                    Console.WriteLine(
                        $"[{file.name}] does not match the version from the server, queued for download..");
                }
            }
            else
            {
                downloadQueue.Enqueue(file);
                Console.WriteLine($"[{file.name}] does not exist, queued for download..");
            }

            Dispatcher.UIThread.Post(() =>
            {
                data.Progress = ((currentMaxProgress - remoteFileListQueue.Count) / currentMaxProgress) * 100;
                data.ProgressText = string.Format(Settings.ComparingFiles,
                    currentMaxProgress - remoteFileListQueue.Count, currentMaxProgress);
            });
        }
    }

    private static async void BackgroundWorker_DoWork()
    {
        while (!cancellationToken.IsCancellationRequested && downloadQueue.TryDequeue(out FileEntry file))
        {
            if (file == null)
                continue;

            try
            {
                Console.WriteLine($"Downloading [{file.name}]...");
                EnsureDirectory(file.name);

                Uri updateUrl = new Uri(Settings.UpdateUrl + "/file/" + file.name);

                using var responseStream = await client.GetStreamAsync(updateUrl);
                using var fileStream = File.Create(file.name);

                byte[] buffer = new byte[81920];
                int bytesRead;
                long fileBytesDownloaded = 0;
                var sw = Stopwatch.StartNew();

                while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    fileBytesDownloaded += bytesRead;

                    lock (downloadStatsLock)
                    {
                        totalBytesDownloaded += bytesRead;
                        totalDownloadTime += sw.Elapsed;
                    }

                    // Limit UI updates to every 0.5 seconds
                    if ((DateTime.UtcNow - lastUiUpdateTime).TotalSeconds >= 0.5)
                    {
                        lastUiUpdateTime = DateTime.UtcNow;

                        double avgSpeed;
                        lock (downloadStatsLock)
                        {
                            avgSpeed = totalDownloadTime.TotalSeconds > 0
                                ? totalBytesDownloaded / totalDownloadTime.TotalSeconds
                                : 0;
                        }

                        string speedStr = $"{(avgSpeed / 1024):F2} KB/s";
                        Dispatcher.UIThread.Post(() =>
                        {
                            double progress =
                                ((currentMaxProgress - downloadQueue.Count) / (double)currentMaxProgress) * 100;
                            data.Progress = progress;
                            data.ProgressText = string.Format(Settings.DownloadingFiles,
                                currentMaxProgress - downloadQueue.Count, currentMaxProgress, speedStr);
                        });

                        sw.Restart(); // reset stopwatch for next chunk interval
                    }
                }
            }
            catch (Exception ex)
            {
                if (!retryMap.TryGetValue(file.name, out int count))
                    count = 0;

                if (count > 5)
                {
                    var fname = file.name;
                    Console.WriteLine($"Failed to download [{file.name}] after 5 attempts, skipping..");
                    Dispatcher.UIThread.Post(() => data.ErrorMessage = string.Format(Settings.FileFailedError, fname));
                    continue;
                }

                retryMap[file.name] = count + 1;
                downloadQueue.Enqueue(file);
                Console.WriteLine(ex.ToString());
            }

            // Final UI update after file is done
            double finalAvgSpeed;
            lock (downloadStatsLock)
            {
                finalAvgSpeed = totalDownloadTime.TotalSeconds > 0
                    ? totalBytesDownloaded / totalDownloadTime.TotalSeconds
                    : 0;
            }

            string finalSpeedStr = $"{(finalAvgSpeed / 1024):F2} KB/s";
            Dispatcher.UIThread.Post(() =>
            {
                double progress = ((currentMaxProgress - downloadQueue.Count) / (double)currentMaxProgress) * 100;
                data.Progress = progress;

                if(progress >= 100 && downloadQueue.Count == 0)
                    data.ProgressText = Settings.Finished;
                else
                    data.ProgressText = string.Format(Settings.DownloadingFiles, currentMaxProgress - downloadQueue.Count, currentMaxProgress, finalSpeedStr);
            });
        }
    }

    private static string GetMD5HashFromFile(string fileName)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(fileName))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    private static void EnsureDirectory(string filePath)
    {
        string dirPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
    }
}

public class FileEntry
{
    public string name { get; set; }
    public string md5 { get; set; }
}