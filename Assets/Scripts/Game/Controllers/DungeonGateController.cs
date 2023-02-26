using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGateController : MonoBehaviour
{
    [SerializeField] private Transform gate;
    [SerializeField] private float openAnimationDuration = 1f;

    private Vector3 closeStatePosition;
    private Vector3 openStatePosition;
    private bool isOpen;

    // Start is called before the first frame update
    void Start()
    {
        if (!gate) gate = transform.GetChild(0);
        closeStatePosition = gate.position;
        openStatePosition = closeStatePosition - Vector3.up * 4f;
    }

    public void Open()
    {
        StartCoroutine(OpenGate());
    }

    public void Close()
    {
        if (!gate) gate = transform.GetChild(0);
        isOpen = false;
        if (gate)
        gate.position = closeStatePosition;
    }

    private IEnumerator OpenGate()
    {
        if (isOpen) yield break;

        var timer = 0f;
        while (timer < openAnimationDuration)
        {
            timer += GameTime.deltaTime;
            var progress = timer / openAnimationDuration;
            var newPosition = Vector3.Lerp(closeStatePosition, openStatePosition, progress);
            gate.position = newPosition;
            yield return null;
        }

        isOpen = true;
    }
}
