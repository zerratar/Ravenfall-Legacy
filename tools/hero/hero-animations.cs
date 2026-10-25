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
// a looping move repeats until the game plays another state (the whirlwind ends with Hero-SpinEnd); a move with a
// next one goes on to it (a cast's start, then its end)
var moves = new (string State, string File, string Clip, float ExitTime, bool Loop, string Next)[]
{
    ("Hero-Roll", "Armed/RPG-Character@Armed-Roll-Forward.FBX", "Armed-Roll-Forward", 0.85f, false, null),
    ("Hero-Cleave", "2Hand-Sword/RPG-Character@2Hand-Sword-Attack1.FBX", "2Hand-Sword-Attack1", 0.8f, false, null),
    ("Hero-Leap", "Armed/RPG-Character@Armed-Jump.FBX", "Armed-Jump", 0.9f, false, null),
    ("Hero-Land", "Armed/RPG-Character@Armed-Jump.FBX", "Armed-Land", 0.85f, false, null),
    ("Hero-Spin", "2Hand-Sword/RPG-Character@2Hand-Sword-Special1.FBX", "2Hand-Sword-Special1_loop", 1f, true, null),
    ("Hero-SpinEnd", "2Hand-Sword/RPG-Character@2Hand-Sword-Special1.FBX", "2Hand-Sword-Special1_end", 0.85f, false, null),
    ("Hero-WarCry", "Armed/RPG-Character@Armed-Boost1.FBX", "Armed-Boost1", 0.85f, false, null),
    ("Hero-Drink", "Unarmed/RPG-Character@Unarmed-Boost1.FBX", "Unarmed-Boost1", 0.8f, false, null),
    // the ranged, magic and healing kits (7c)
    ("Hero-Shoot", "2Hand-Bow/RPG-Character@2Hand-Bow-Aiming-Fire.FBX", "2Hand-Bow-Aiming-Fire-CM", 0.9f, false, null),
    ("Hero-Volley", "2Hand-Bow/RPG-Character@2Hand-Bow-Boost1.FBX", "2Hand-Bow-Boost1", 0.85f, false, null),
    ("Hero-Cast", "Staff/RPG-Character@Staff-Cast-Attack1.fbx", "Staff-Cast-Attack1_start", 1f, false, "Hero-CastEnd"),
    ("Hero-CastEnd", "Staff/RPG-Character@Staff-Cast-Attack1.fbx", "Staff-Cast-Attack1_end", 0.9f, false, null),
    ("Hero-CastArea", "Staff/RPG-Character@Staff-Cast-L-AOE1.fbx", "Staff-Cast-L-AOE1_start", 1f, false, "Hero-CastAreaEnd"),
    ("Hero-CastAreaEnd", "Staff/RPG-Character@Staff-Cast-L-AOE1.fbx", "Staff-Cast-L-AOE1_end", 0.9f, false, null),
    ("Hero-Buff", "Staff/RPG-Character@Staff-Cast-L-Buff1.fbx", "Staff-Cast-L-Buff1_start", 1f, false, "Hero-BuffEnd"),
    ("Hero-BuffEnd", "Staff/RPG-Character@Staff-Cast-L-Buff1.fbx", "Staff-Cast-L-Buff1_end", 0.9f, false, null),
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
    else if (move.Next != null)
    {
        // made in the second pass, once every state exists
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
foreach (var move in moves)
{
    if (move.Next == null) continue;
    UnityEditor.Animations.AnimatorState from = null, to = null;
    foreach (var child in machine.states)
    {
        if (child.state.name == move.State) from = child.state;
        if (child.state.name == move.Next) to = child.state;
    }
    if (from == null || to == null) { sb.Append("no " + move.Next + "; "); continue; }
    var next = from.AddTransition(to);
    next.hasExitTime = true;
    next.exitTime = move.ExitTime;
    next.duration = 0.05f;
}
UnityEditor.EditorUtility.SetDirty(controller);
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
