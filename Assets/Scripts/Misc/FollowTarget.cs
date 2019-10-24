using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public GameObject Target;

    void Update()
    {
        if (!Target)
        {
            return;
        }

        transform.position = Target.transform.position;
    }
}
