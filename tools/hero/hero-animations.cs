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
    // a straight shot: the Aiming-Fire clips draw side on, the bow 85 degrees left of where the character faces
    ("Hero-Shoot", "2Hand-Bow/RPG-Character@2Hand-Bow-Attack4.FBX", "2Hand-Bow-Attack4", 0.9f, false, null),
    ("Hero-Volley", "2Hand-Bow/RPG-Character@2Hand-Bow-Boost1.FBX", "2Hand-Bow-Boost1", 0.85f, false, null),
    ("Hero-Cast", "Staff/RPG-Character@Staff-Cast-Attack1.fbx", "Staff-Cast-Attack1_start", 1f, false, "Hero-CastEnd"),
    ("Hero-CastEnd", "Staff/RPG-Character@Staff-Cast-Attack1.fbx", "Staff-Cast-Attack1_end", 0.9f, false, null),
    ("Hero-CastArea", "Staff/RPG-Character@Staff-Cast-L-AOE1.fbx", "Staff-Cast-L-AOE1_start", 1f, false, "Hero-CastAreaEnd"),
    ("Hero-CastAreaEnd", "Staff/RPG-Character@Staff-Cast-L-AOE1.fbx", "Staff-Cast-L-AOE1_end", 0.9f, false, null),
    ("Hero-Buff", "Staff/RPG-Character@Staff-Cast-L-Buff1.fbx", "Staff-Cast-L-Buff1_start", 1f, false, "Hero-BuffEnd"),
    ("Hero-BuffEnd", "Staff/RPG-Character@Staff-Cast-L-Buff1.fbx", "Staff-Cast-L-Buff1_end", 0.9f, false, null),
    // the click attacks: three swings in a row per kind of weapon (HeroController's combo)
    ("Hero-2HSword-1", "2Hand-Sword/RPG-Character@2Hand-Sword-Attack1.FBX", "2Hand-Sword-Attack1", 0.85f, false, null),
    ("Hero-2HSword-2", "2Hand-Sword/RPG-Character@2Hand-Sword-Attack2.FBX", "2Hand-Sword-Attack2", 0.85f, false, null),
    ("Hero-2HSword-3", "2Hand-Sword/RPG-Character@2Hand-Sword-Attack3.FBX", "2Hand-Sword-Attack3", 0.85f, false, null),
    ("Hero-2HAxe-1", "2Hand-Axe/RPG-Character@2Hand-Axe-Attack1.FBX", "2Hand-Axe-Attack1", 0.85f, false, null),
    ("Hero-2HAxe-2", "2Hand-Axe/RPG-Character@2Hand-Axe-Attack2.FBX", "2Hand-Axe-Attack2", 0.85f, false, null),
    ("Hero-2HAxe-3", "2Hand-Axe/RPG-Character@2Hand-Axe-Attack3.FBX", "2Hand-Axe-Attack3", 0.85f, false, null),
    ("Hero-Spear-1", "2Hand-Spear/RPG-Character@2Hand-Spear-Attack1.FBX", "2Hand-Spear-Attack1", 0.85f, false, null),
    ("Hero-Spear-2", "2Hand-Spear/RPG-Character@2Hand-Spear-Attack2.FBX", "2Hand-Spear-Attack2", 0.85f, false, null),
    ("Hero-Spear-3", "2Hand-Spear/RPG-Character@2Hand-Spear-Attack3.FBX", "2Hand-Spear-Attack3", 0.85f, false, null),
    ("Hero-Sword-1", "Sword/RPG-Character@Sword-Attack-R1.FBX", "Sword-Attack-R1", 0.85f, false, null),
    ("Hero-Sword-2", "Sword/RPG-Character@Sword-Attack-L1.FBX", "Sword-Attack-L1", 0.85f, false, null),
    ("Hero-Sword-3", "Sword/RPG-Character@Sword-Attack-R2.FBX", "Sword-Attack-R2", 0.85f, false, null),
    ("Hero-Axe-1", "Mace/RPG-Character@Mace-Attack-R1.FBX", "Mace-Attack-R1", 0.85f, false, null),
    ("Hero-Axe-2", "Mace/RPG-Character@Mace-Attack-L1.FBX", "Mace-Attack-L1", 0.85f, false, null),
    ("Hero-Axe-3", "Mace/RPG-Character@Mace-Attack-R2.FBX", "Mace-Attack-R2", 0.85f, false, null),
};
var machine = controller.layers[0].stateMachine;
var sb = new System.Text.StringBuilder();

// where a move goes back to: the fighting idle of the weapon the hero holds (the Weapon parameter, Shield for a sword
// and board), so a swing never drops to the plain idle and draws the weapon again
UnityEditor.Animations.AnimatorState Find(UnityEditor.Animations.AnimatorStateMachine m, string path)
{
    var parts = path.Split('/');
    for (var i = 0; i < parts.Length - 1; i++)
    {
        UnityEditor.Animations.AnimatorStateMachine sub = null;
        foreach (var child in m.stateMachines) if (child.stateMachine.name == parts[i]) sub = child.stateMachine;
        if (sub == null) return null;
        m = sub;
    }
    foreach (var child in m.states) if (child.state.name == parts[parts.Length - 1]) return child.state;
    return null;
}
var stances = new (int Weapon, int Shield, string Path)[]
{
    (0, -1, "Combat/Unarmed/Unarmed-Idle"),
    (1, 0, "Combat/One Handed Sword/Armed-Idle"),
    (1, 1, "Combat/One Handed Sword Shield/Armed-Idle"),
    (2, -1, "Combat/Two Handed Sword/2Hand-Sword-Idle"),
    (3, -1, "Combat/Ranged/2Hand-Bow-Idle"),
    (4, -1, "Combat/Magic/Staff-Idle"),
    (5, -1, "Combat/Katana/Idle"),
    (6, -1, "Combat/Healing/Staff-Idle"),
    (7, -1, "Combat/Spear/Idle"),
    (8, -1, "Combat/Two Handed Axe/Idle"),
};
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
        // back to the weapon's fighting idle while fighting; else to the animator's usual flow
        foreach (var stance in stances)
        {
            var to = Find(machine, stance.Path);
            if (to == null) { sb.Append("no " + stance.Path + "; "); continue; }
            var t = state.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = move.ExitTime;
            t.duration = 0.15f;
            t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Attacking");
            t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, stance.Weapon, "Weapon");
            if (stance.Shield >= 0) t.AddCondition(stance.Shield == 1 ? UnityEditor.Animations.AnimatorConditionMode.If : UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Shield");
        }
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
// a fix to the animator's own flow: putting the axe away went straight back to chopping whenever Action was 1, even
// with woodcutting over, so the axe chopped and sheathed in a loop (hero mode holds Action at 1)
var sheath = Find(machine, "Woodcutting/2Hand-Axe-Sheath-Back-Relax");
if (sheath != null)
{
    foreach (var t in sheath.transitions)
    {
        if (t.destinationState == null || t.destinationState.name != "2Hand-Axe-Attack4") continue;
        var has = false;
        foreach (var c in t.conditions) if (c.parameter == "Woodcutting") has = true;
        if (!has) { t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Woodcutting"); sb.Append("woodcutting sheath fixed; "); }
    }
}
else sb.Append("no woodcutting sheath; ");

// and the bow never left its stance when the fight was over (the sword and the staff do): a bow user could not go on
// to chop, mine or fish. Its idle and run leave the fight like theirs.
var exitCombat = Find(machine, "Combat/Exit Combat");
foreach (var bowPath in new[] { "Combat/Ranged/2Hand-Bow-Idle", "Combat/Ranged/2Hand-Bow-Run-Forward" })
{
    var state = Find(machine, bowPath);
    if (state == null || exitCombat == null) { sb.Append("no " + bowPath + "; "); continue; }
    var has = false;
    foreach (var t in state.transitions) if (t.destinationState == exitCombat) has = true;
    if (has) continue;
    var leave = state.AddTransition(exitCombat);
    leave.hasExitTime = false;
    leave.duration = 0.15f;
    leave.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Attacking");
    sb.Append(bowPath + " leaves the fight; ");
}

UnityEditor.EditorUtility.SetDirty(controller);
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
