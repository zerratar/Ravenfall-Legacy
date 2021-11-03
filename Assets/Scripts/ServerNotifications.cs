using UnityEngine;

public class ServerNotifications : MonoBehaviour
{
    [SerializeField] private Transform notificationPanel;
    [SerializeField] private TMPro.TextMeshProUGUI lblText;

    [SerializeField] private RectTransform progress;

    private ServerMessage activeMessage;
    private float messageLife = 0;

    public bool HasActiveMessage => activeMessage != null && messageLife < activeMessage.Time;

    public void Update()
    {
        if (activeMessage == null)
        {
            RemoveNotification();
            return;
        }

        if (messageLife >= GetMessageTime())
        {
            RemoveNotification();
            return;
        }

        UpdateNotification();
    }

    private void UpdateNotification()
    {
        if (notificationPanel == null)
            return;

        notificationPanel.gameObject.SetActive(true);
        messageLife += Time.deltaTime;
        var prog = messageLife / GetMessageTime();
        progress.sizeDelta = new Vector2(2560f * prog, progress.sizeDelta.y);
    }

    private void RemoveNotification()
    {
        if (notificationPanel != null)
            notificationPanel.gameObject.SetActive(false);
        this.activeMessage = null;
    }

    public void ShowMessage(ServerMessage message)
    {
        this.activeMessage = message;
        this.messageLife = 0;
        progress.sizeDelta = new Vector2(0, progress.sizeDelta.y);
        lblText.text = message?.Message;
    }
    private float GetMessageTime()
    {
        if (activeMessage == null) return 0;
        return (float)activeMessage.Time / 1000f;
    }
}
