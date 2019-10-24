using TMPro;
using UnityEngine;

public class AutoHideUI : MonoBehaviour
{
    public float Timeout = 3f;

    private float timer = 0f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void Reset()
    {
        timer = Timeout;
        gameObject.SetActive(true);
    }
}
