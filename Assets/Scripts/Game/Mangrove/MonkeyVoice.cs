using UnityEngine;

/// <summary>
/// Answers the gorilla clips' AnimalSound events (the Low Poly Animated Animals pack's own PlaySound is not used): the
/// banana island's monkeys, their chief and the Monkey Pet are quiet for now. Without it every idle loop logs an error.
/// </summary>
public sealed class MonkeyVoice : MonoBehaviour
{
    public void AnimalSound() { }
}
