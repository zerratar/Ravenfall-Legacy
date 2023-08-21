using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteeringWheelController : MonoBehaviour
{
    [SerializeField] private float animationLength = 1f;
    [SerializeField] private AnimationCurve easing;

    private float animationTime = 0f;
    private float targetRotation;
    private Vector3 initialRotation;
    private Quaternion init;

    void Awake()
    {
        this.initialRotation = transform.localRotation.eulerAngles;
        this.init = transform.localRotation;
        this.targetRotation = this.initialRotation.z + 360f;
    }

    // Update is called once per frame
    void Update()
    {
        if (animationTime > 0f)
        {
            animationTime -= Time.deltaTime;

            var progress = (animationLength - animationTime) / animationTime;

            var angle = targetRotation * easing.Evaluate(progress);
            transform.localRotation = Quaternion.Euler(initialRotation.x, initialRotation.y, angle);

            if (animationTime <= 0f)
            {
                transform.localRotation = init;
            }
        }
    }

    public void PlaySpinAnimation()
    {
        animationTime = animationLength;
    }
}
