using System.ComponentModel;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

//Must end in a slash
const string UPDATE_URL = "http://yoururl.com:8080/";
const int WORKER_COUNT = 8;

HttpClient client = new HttpClient();
BackgroundWorker[] workers = new BackgroundWorker[WORKER_COUNT];

Console.WriteLine("Requesting file information from server...");

HttpResponseMessage response = await client.GetAsync(UPDATE_URL);

Queue<FileEntry> downloadQueue = new Queue<FileEntry>();
Queue<FileEntry> compareFileQueue = new Queue<FileEntry>();

if (response.StatusCode == HttpStatusCode.NotFound)
{
    Console.WriteLine("Error 404 received. Cannot continue.");
    return;
}

try
{
    string json = await response.Content.ReadAsStringAsync();

    FileEntry[] fileList = JsonSerializer.Deserialize<FileEntry[]>(json);
    Console.WriteLine($"Received information for {fileList.Length} files from the server, comparing to local files..");

    foreach (var item in fileList)
    {
        compareFileQueue.Enqueue(item);
    }

    WorkerSetupComparing();
    WaitForWorkers();

    Console.WriteLine($"{downloadQueue.Count} files queued for download.");

    if (downloadQueue.Count > 0)
    {
        Console.WriteLine($"Starting downloads, up to [{WORKER_COUNT}] at a time.");
        Console.WriteLine("This may take some time, please be patient..");
        Console.WriteLine();
        WorkerSetupDownloading();
        WaitForWorkers();
        Console.WriteLine("All done!");
    }
    else
    {
        Console.WriteLine("Your all up to date!");
    }
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}




void WorkerSetupDownloading()
{
    for (int i = 0; i < workers.Length; i++)
    {
        workers[i] = new BackgroundWorker();
        workers[i].DoWork += BackgroundWorker_DoWork;
        workers[i].RunWorkerAsync();
    }
}

void WorkerSetupComparing()
{
    for (int i = 0; i < workers.Length; i++)
    {
        workers[i] = new BackgroundWorker();
        workers[i].DoWork += BackgroundWorker_CompareFile; ;
        workers[i].RunWorkerAsync();
    }
}

void BackgroundWorker_CompareFile(object? sender, DoWorkEventArgs e)
{
    while (compareFileQueue.TryDequeue(out FileEntry file))
    {
        if (File.Exists(file.name))
        {
            if (!file.md5.Equals(GetMD5HashFromFile(file.name)))
            {
                downloadQueue.Enqueue(file);
                Console.WriteLine($"[{file.name}] does not match the version from the server, queued for download..");
            }
        }
        else
        {
            downloadQueue.Enqueue(file);
            Console.WriteLine($"[{file.name}] does not exist, queued for download..");
        }
    }
}

void WaitForWorkers()
{
    foreach (var worker in workers)
    {
        while (worker.IsBusy)
        {
            Thread.Sleep(100);
        }
    }
}

void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
{
    while (downloadQueue.TryDequeue(out FileEntry file))
    {
        if (file == null)
        {
            continue;
        }

        try
        {
            Console.WriteLine($"Downloading [{file.name}]...");
            EnsureDirectory(file.name);

            var task = client.GetByteArrayAsync(UPDATE_URL + "/file/" + file.name);
            task.Wait();
            byte[] dl = task.Result;
            var fileStream = File.Create(file.name);
            fileStream.Write(dl, 0, dl.Length);
            fileStream.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}

static void EnsureDirectory(string filePath)
{
    string dirPath = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
    {
        Directory.CreateDirectory(dirPath);
    }
}

static string GetMD5HashFromFile(string fileName)
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

public class FileEntry
{
    public string name { get; set; }
    public string md5 { get; set; }
}