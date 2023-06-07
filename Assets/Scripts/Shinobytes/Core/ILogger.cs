namespace RavenNest.SDK
{
    public interface ILogger
    {
        void WriteWarning(string message);
        void Write(string message);
        void WriteMessage(string message);
        void WriteDebug(string message);
        void WriteError(string errorMessage);
    }
}