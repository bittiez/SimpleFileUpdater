using Avalonia.Media;

namespace FileUpdaterClient;

public static class Settings
{
    public const string Title = "Client Updater";
    public const string TitleColor = "#F2F2F2";
    
    public const string Subtitle = "Our shard's file downloader";
    public const string SubtitleColor = "#F2F2F2";
    
    public static SolidColorBrush DefaultTextColor = SolidColorBrush.Parse("#F2F2F2");
    public static SolidColorBrush ProgressBarBackground = SolidColorBrush.Parse("#212121");
    public static SolidColorBrush ProgressBarForeground = SolidColorBrush.Parse("#40D659");
    
    public const string UpdateUrl = "http://74.91.125.193:8080/";

    public const string Finished = "Done, you're all up to date!";
    public const string ReqFileList = "Requesting file list from server..";
    public const string ComparingFiles = "Comparing your files to the server.. ({0}/{1})"; //{0} = current file, {1} = total files
    public const string DownloadingFiles = "Downloading files from the server.. ({0}/{1}) - ({2})"; //{0} = current file, {1} = total files, {2} dl speed
    
    public const string ConError = "Unable to connect to server."; //Failed connection to server
    public const string BadData = "Got bad data from server, please try again later."; //Malformed JSON response
    public const string UnknownError = "An unknown error occured, please try again later.";
    public const string FileFailedError = "Failed to download [{0}] after several attempts, skipping.."; //{0} = file name
}