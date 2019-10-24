using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteeringWheelController : MonoBehaviour
{
    [SerializeField] private float animationLength = 1f;
    [SerializeField] private Vector3 initialRotation = Vector3.zero;
    [SerializeField] private Vector3 targetRotation = Vector3.zero;
    [SerializeField] private AnimationCurve easing;

    private float animationTime = 0f;

    // Update is called once per frame
    void Update()
    {
        if (animationTime > 0f)
        {
            animationTime -= Time.deltaTime;

            var progress = (animationLength - animationTime) / animationTime;

            transform.localRotation = Quaternion.Euler(targetRotation * easing.Evaluate(progress));

            if (animationTime <= 0f)
            {
                transform.localRotation = Quaternion.Euler(initialRotation);
            }
        }
    }

    public void PlaySpinAnimation()
    {
        animationTime = animationLength;
    }
}
