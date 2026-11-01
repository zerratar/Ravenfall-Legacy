using UnityEngine;

/// <summary>
/// A parrot perched on the banana island (a still Pirate Realm prop): now and then it turns its head to look about, or
/// hops round on its perch. Only near the camera.
/// </summary>
public sealed class ParrotIdle : MonoBehaviour
{
    private const float SeenWithin = 120f;
    private Quaternion rest;
    private Quaternion target;
    private Vector3 restPosition;
    private float nextMove;
    private float hop;

    private void Start()
    {
        rest = target = transform.localRotation;
        restPosition = transform.localPosition;
        nextMove = Time.time + Random.Range(1f, 6f);
    }

    private void Update()
    {
        var cam = Camera.main;
        if (cam && (cam.transform.position - transform.position).sqrMagnitude > SeenWithin * SeenWithin) return;

        if (Time.time >= nextMove)
        {
            nextMove = Time.time + Random.Range(1.5f, 6f);
            target = rest * Quaternion.Euler(0f, Random.Range(-70f, 70f), Random.Range(-8f, 8f));
            if (Random.value < 0.25f) hop = 1f;
        }
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, 1f - Mathf.Exp(-Time.deltaTime * 10f));
        hop = Mathf.MoveTowards(hop, 0f, Time.deltaTime * 4f);
        transform.localPosition = restPosition + Vector3.up * (Mathf.Sin(hop * Mathf.PI) * 0.06f);
    }
}
