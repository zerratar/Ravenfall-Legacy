using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UIElements.Image;

public class DuelCameraScript : MonoBehaviour
{
    private readonly List<DuelHandler> targets = new List<DuelHandler>();
    private readonly object mutex = new object();

    [SerializeField] private FocusTargetCamera focusCam;
    [SerializeField] private RawImage image;
    [SerializeField] private float targetTime = 3f;

    private DuelHandler target;
    private float targetObserverTimer = 3f;
    private int targetIndex;

    // Start is called before the first frame update
    void Start()
    {
        if (!focusCam)
        {
            focusCam = GetComponent<FocusTargetCamera>();
        }

        if (!image)
        {
            image = GameObject.Find("DuelCameraImage").GetComponent<RawImage>();
        }

        if (focusCam)
        {
            focusCam.UsePosition = true;
        }
    }

    void Update()
    {
        lock (mutex)
        {
            if (targets.Count == 0)
            {
                if (image)
                {
                    image.enabled = false;
                }
                return;
            }

            if (targetObserverTimer > 0f)
            {
                targetObserverTimer -= Time.deltaTime;
                if (targetObserverTimer <= 0f)
                {
                    targetObserverTimer = targetTime;
                    targetIndex = (targetIndex + 1) % targets.Count;
                    target = targets[targetIndex];
                }
            }
        }

        if (!target)
        {
            return;
        }

        if (!target.InDuel)
        {
            return;
        }

        if (image)
        {
            image.enabled = true;
        }


        focusCam.TargetPosition = 0.5f * Vector3.Normalize(target.Opponent.transform.position - target.transform.position) + target.transform.position;
    }

    public bool AddTarget(DuelHandler duelHandler)
    {
        lock (mutex)
        {
            targetObserverTimer = targetTime;
            targets.Add(duelHandler);
        }
        return true;
    }

    public void RemoveTarget(DuelHandler duelHandler)
    {
        lock (mutex)
        {
            targets.Remove(duelHandler);
        }
    }
}
