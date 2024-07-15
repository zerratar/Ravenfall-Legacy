using UnityEngine;

public class TimeoutDestroy : MonoBehaviour
{
    public float TimeoutSeconds = -2f;
    private bool destroyed;

    void Update()
    {
        if (destroyed || TimeoutSeconds <= -2f)
        {
            return;
        }

        TimeoutSeconds -= Time.deltaTime;
        if (TimeoutSeconds <= 0f)
        {
            destroyed = true;
            DestroyImmediate(gameObject);
        }
    }
}
