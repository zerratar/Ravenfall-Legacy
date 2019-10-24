using UnityEngine;

public class FocusTargetCamera : MonoBehaviour
{
    [SerializeField] private float radius = 18f;
    [SerializeField] private float rotationSpeed = 0.002f;
    [SerializeField] private float angle;

    public bool UsePosition = false;

    public Transform Target;

    public Vector3 TargetPosition;

    // Update is called once per frame
    void Update()
    {
        if (!Target && !UsePosition) return;

        //this.transform.RotateAround(target.position);
        var radiansToDegrees = 180f / Mathf.PI;
        var degrees = radiansToDegrees * (angle += Time.deltaTime * rotationSpeed);

        var x = Mathf.Sin(degrees) * radius;
        var z = Mathf.Cos(degrees) * radius;

        var targetPos = UsePosition ? TargetPosition : Target.position;
        transform.position = targetPos + new Vector3(x, radius, z);
        transform.LookAt(targetPos);
    }
}
