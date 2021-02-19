using System.Collections;
using System.Collections.Generic;
using System.Media;
using UnityEngine;

public class ScreamingLady : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SphereCollider sphereCollider;
    [SerializeField] private GameCamera camera;
    [SerializeField] private Animator animator;

    private bool inTrigger;

    // Start is called before the first frame update
    void Start()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!camera) camera = FindObjectOfType<GameCamera>();
        if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        var rotBefore = this.transform.rotation.eulerAngles;
        this.transform.LookAt(camera.transform);
        var rot = this.transform.rotation.eulerAngles;
        this.transform.eulerAngles = new Vector3(rotBefore.x, rot.y, rot.z);

        if (audioSource.isPlaying)
            return;

        var cp = camera.transform.position;
        var rc = transform.position + sphereCollider.center;
        if (Vector3.Distance(cp, rc) < sphereCollider.radius)
        {
            if (!inTrigger && !audioSource.isPlaying)
            {
                inTrigger = true;
                audioSource.loop = false;
                animator.SetTrigger("Scream");
                audioSource.Play();
            }
        }
        else
        {
            inTrigger = false;
        }
    }
}
