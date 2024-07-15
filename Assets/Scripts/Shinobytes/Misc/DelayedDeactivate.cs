using UnityEngine;

public class DelayedDeactivate : MonoBehaviour
{
    public float TimeoutSeconds = 0;

    void Update()
    {
        if (TimeoutSeconds <= 0)
        {
            return;
        }

        TimeoutSeconds -= Time.deltaTime;
        if (TimeoutSeconds <= 0f)
        {
            this.gameObject.SetActive(false);
        }
    }
}
