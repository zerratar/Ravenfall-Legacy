using System;

public class ExternalResource<T>
{
    private readonly DateTime originalWriteTime;
    private readonly DateTime originalCreationTime;
    private readonly long originalSize;
    public ExternalResource(string fullPath, T resource)
    {
        FullPath = fullPath;
        Resource = resource;
        this.originalSize = FileSize;
        this.originalWriteTime = LastWriteTime;
        this.originalCreationTime = CreationTime;
    }
    public T Resource { get; set; }
    public string FullPath { get; set; }
    public long FileSize => new System.IO.FileInfo(FullPath).Length;
    public DateTime CreationTime => new System.IO.FileInfo(FullPath).CreationTime;
    public DateTime LastWriteTime => new System.IO.FileInfo(FullPath).LastWriteTime;
    public bool HasBeenModified
    {
        get
        {
            var fi = new System.IO.FileInfo(FullPath);
            if (!fi.Exists) return true;
            return originalSize != fi.Length || originalWriteTime != fi.LastWriteTime || originalWriteTime != fi.CreationTime;
        }
    }
}
