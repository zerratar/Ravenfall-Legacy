using System;
using System.IO;

public class ExternalResource<T>
{
    private readonly DateTime originalWriteTime;
    private readonly DateTime originalCreationTime;
    private readonly long originalSize;

    private readonly FileInfo file;

    public ExternalResource(string fullPath, T resource)
    {
        FullPath = fullPath;
        Resource = resource;

        this.file = new FileInfo(FullPath);
        this.originalSize = FileSize;
        this.originalWriteTime = LastWriteTime;
        this.originalCreationTime = CreationTime;
    }

    public T Resource { get; set; }

    public string FullPath { get; set; }

    public long FileSize => file.Length;
    public DateTime CreationTime => file.CreationTime;
    public DateTime LastWriteTime => file.LastWriteTime;

    public bool HasBeenModified
    {
        get
        {
            file.Refresh();

            if (!file.Exists) return true;

            return originalSize != file.Length;
        }
    }
}
