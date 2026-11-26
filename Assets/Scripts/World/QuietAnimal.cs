using UnityEngine;

/// <summary>
/// Answers the sound events of the Low Poly Animated Animals pack's clips (AnimalSound, Walking, Eating, ...) on an animal
/// that has no pack PlaySound to play them: the Battle of Streamers' cow combatants, the pen animals of the tower towns
/// (their builds keep only an animal's looks and animator). Quiet, as the banana monkeys' MonkeyVoice is: without a
/// receiver every loop of an idle clip logs "AnimationEvent 'AnimalSound' ... has no receiver!".
/// </summary>
public sealed class QuietAnimal : MonoBehaviour
{
    public void AnimalSound() { }
    public void Walking() { }
    public void Eating() { }
    public void Running() { }
    public void Attacking() { }
    public void Death() { }
    public void Sleeping() { }

    /// <summary>Gives every animator under <paramref name="root"/> whose object is named "... (pen)" a quiet receiver.</summary>
    public static void AddToPens(Transform root)
    {
        if (!root) return;
        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            if (animator.name.EndsWith("(pen)") && !animator.GetComponent<QuietAnimal>() && !animator.GetComponent<LowPolyAnimalPack.PlaySound>())
                animator.gameObject.AddComponent<QuietAnimal>();
    }
}
