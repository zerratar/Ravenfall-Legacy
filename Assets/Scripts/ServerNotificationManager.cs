using System.Collections.Concurrent;
using UnityEngine;

public class ServerNotificationManager : MonoBehaviour
{
    [SerializeField] private ServerNotifications notifications;

    private ConcurrentQueue<ServerMessage> messages
        = new ConcurrentQueue<ServerMessage>();

    public void Update()
    {
        if (!notifications.HasActiveMessage && TryGetNextNotification(out var notification))
        {
            notifications.ShowMessage(notification);
        }
    }

    internal bool TryGetNextNotification(out ServerMessage notification)
    {
        return messages.TryDequeue(out notification);
    }

    public void EnqueueServerMessage(ServerMessage serverMessage)
    {
        messages.Enqueue(serverMessage);
    }
}
