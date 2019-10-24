using UnityEngine;

internal class UnityLogger : ILogger
{
    public void WriteMessage(string message)
    {
        Debug.Log(message);
    }

    public void WriteError(string error)
    {
        Debug.LogError(error);
    }

    public void WriteDebug(string message)
    {
        Debug.Log($"[DBG]: {message}");
    }

    public void WriteWarning(string message)
    {
        Debug.LogWarning(message);
    }
}