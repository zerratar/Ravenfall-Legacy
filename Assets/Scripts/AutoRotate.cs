using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [SerializeField] public Vector3 rotationSpeed;

    private Vector3 rotation;
    private Vector3 startRotation;

    // Start is called before the first frame update
    void Start()
    {
        startRotation = transform.rotation.eulerAngles;
        rotation = startRotation;
    }

    // Update is called once per frame
    void Update()
    {
        rotation += rotationSpeed * GameTime.deltaTime;
        transform.rotation = Quaternion.Euler(rotation);
    }
}
