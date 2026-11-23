using UnityEngine;

/// <summary>
/// Holds an animal of a silly world scene (<see cref="WorldGag"/>) in one of its animator's states (a seagull sitting
/// instead of flying, a cat sitting), set each time it shows, since an animator forgets its state when switched off.
/// </summary>
public sealed class SillyAnimalPose : MonoBehaviour
{
    [Tooltip("The state to play (\"Sitting\"); empty keeps the animator's own.")]
    public string State;
    public string[] TrueBools;

    private void OnEnable()
    {
        var animator = GetComponent<Animator>();
        if (!animator) return;
        if (TrueBools != null) foreach (var name in TrueBools) animator.SetBool(name, true);
        if (!string.IsNullOrEmpty(State)) animator.Play(State, 0, Random.value);
    }
}
