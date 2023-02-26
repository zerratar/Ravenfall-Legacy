using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UIElements.Image;

public class DuelCameraScript : MonoBehaviour
{
    private readonly List<DuelHandler> targets = new List<DuelHandler>();

    [SerializeField] private FocusTargetCamera focusCam;
    [SerializeField] private RawImage image;
    [SerializeField] private float targetTime = 3f;

    private Camera renderCamera;
    private DuelHandler target;
    private float targetObserverTimer = 3f;
    private int targetIndex;

    // Start is called before the first frame update
    void Start()
    {
        renderCamera = GetComponent<Camera>();
        if (!focusCam)
        {
            focusCam = GetComponent<FocusTargetCamera>();
        }

        if (!image)
        {
            var duelCamera = GameObject.Find("DuelCameraImage");
            if (duelCamera)
                image = duelCamera.GetComponent<RawImage>();
        }

        if (focusCam)
        {
            focusCam.UsePosition = true;
        }
    }

    void Update()
    {
        if (targets.Count == 0)
        {
            focusCam.enabled = false;
            renderCamera.enabled = false;
            if (image)
            {
                image.enabled = false;
            }
            return;
        }

        if (targetObserverTimer > 0f)
        {
            targetObserverTimer -= GameTime.deltaTime;
            if (targetObserverTimer <= 0f)
            {
                targetObserverTimer = targetTime;
                targetIndex = (targetIndex + 1) % targets.Count;
                target = targets[targetIndex];
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


        focusCam.TargetPosition = 0.5f * Vector3.Normalize(target.Opponent.Position - target.transform.position) + target.transform.position;
    }

    public bool AddTarget(DuelHandler duelHandler)
    {
        focusCam.enabled = true;
        renderCamera.enabled = true;

        targetObserverTimer = targetTime;
        targets.Add(duelHandler);
        return true;
    }

    public void RemoveTarget(DuelHandler duelHandler)
    {
        targets.Remove(duelHandler);
        if (targets.Count == 0)
        {
            if (image)
            {
                image.enabled = false;
            }

            focusCam.enabled = false;
            renderCamera.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
