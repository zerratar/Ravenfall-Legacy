public class DownloadProgress
{
    public DownloadProgress(long fileSize, long bytesDownloaded, double bytesPerSecond)
    {
        FileSize = fileSize;
        BytesDownloaded = bytesDownloaded;
        BytesPerSecond = bytesPerSecond;
    }
    public long FileSize { get; }
    public long BytesDownloaded { get; }
    public double BytesPerSecond { get; }
}