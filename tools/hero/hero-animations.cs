// Run with: unity command eval_file --file tools/hero/hero-animations.cs
//
// Adds hero mode's moves (docs/hero-mode-design.md, 7a) to the player's animator (Assets/Animations/
// PlayerAnimationController.controller), from the RPG Character Animation Pack the animator is already built on:
// states the game plays by name (Animator.CrossFadeInFixedTime) that go back to the animator's usual flow when they
// end. Run it again to bring them up to date; nothing else in the animator changes.
var path = "Assets/Animations/PlayerAnimationController.controller";
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
if (!controller) return "no animator at " + path;
var pack = "Assets/RPG Character Animation Pack/Animations/";
// a looping move repeats until the game plays another state (the whirlwind ends with Hero-SpinEnd)
var moves = new (string State, string File, string Clip, float ExitTime, bool Loop)[]
{
    ("Hero-Roll", "Armed/RPG-Character@Armed-Roll-Forward.FBX", "Armed-Roll-Forward", 0.85f, false),
    ("Hero-Cleave", "2Hand-Sword/RPG-Character@2Hand-Sword-Attack1.FBX", "2Hand-Sword-Attack1", 0.8f, false),
    ("Hero-Leap", "Armed/RPG-Character@Armed-Jump.FBX", "Armed-Jump", 0.9f, false),
    ("Hero-Land", "Armed/RPG-Character@Armed-Jump.FBX", "Armed-Land", 0.85f, false),
    ("Hero-Spin", "2Hand-Sword/RPG-Character@2Hand-Sword-Special1.FBX", "2Hand-Sword-Special1_loop", 1f, true),
    ("Hero-SpinEnd", "2Hand-Sword/RPG-Character@2Hand-Sword-Special1.FBX", "2Hand-Sword-Special1_end", 0.85f, false),
    ("Hero-WarCry", "Armed/RPG-Character@Armed-Boost1.FBX", "Armed-Boost1", 0.85f, false),
    ("Hero-Drink", "Unarmed/RPG-Character@Unarmed-Boost1.FBX", "Unarmed-Boost1", 0.8f, false),
};
var machine = controller.layers[0].stateMachine;
var sb = new System.Text.StringBuilder();
foreach (var move in moves)
{
    UnityEngine.AnimationClip clip = null;
    foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(pack + move.File))
    {
        if (asset is UnityEngine.AnimationClip c && c.name == move.Clip) { clip = c; break; }
    }
    if (!clip) { sb.Append("no clip in " + move.File + "; "); continue; }

    UnityEditor.Animations.AnimatorState state = null;
    foreach (var child in machine.states) if (child.state.name == move.State) state = child.state;
    if (state == null) state = machine.AddState(move.State, new UnityEngine.Vector3(900f, 60f * System.Array.IndexOf(moves, move), 0f));
    state.motion = clip;
    state.speed = 1f;
    foreach (var t in state.transitions) state.RemoveTransition(t);
    if (move.Loop)
    {
        var again = state.AddTransition(state);
        again.hasExitTime = true;
        again.exitTime = 1f;
        again.duration = 0f;
    }
    else
    {
        // back to the animator's usual flow (idle, running, fighting) when the move is over
        var back = state.AddExitTransition();
        back.hasExitTime = true;
        back.exitTime = move.ExitTime;
        back.duration = 0.15f;
    }
    sb.Append(move.State + " (" + clip.name + ", " + clip.length.ToString("0.00") + " s); ");
}
UnityEditor.EditorUtility.SetDirty(controller);
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
