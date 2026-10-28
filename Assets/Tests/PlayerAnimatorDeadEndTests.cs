using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Assets.Scripts.Tests
{
    /// <summary>
    /// The player's animator must never leave a character frozen. From every state it can get into, it has to find its
    /// way back to idle once the game switches everything off (PlayerAnimationController.ResetAnimationStates, with the
    /// player standing still), and into whatever the game starts next. A state that only leaves on a flag the game may
    /// never set again (Attacking, Moving) holds its last frame forever: a raid ending mid-cast left mages frozen at the
    /// rocks with a pickaxe in hand that way (2026-10-27).
    /// <para>
    /// The animator is stepped by hand on a bare GameObject, so this checks the state machine (transitions, exit times,
    /// sub-state machines), not how the clips look.
    /// </para>
    /// </summary>
    internal class PlayerAnimatorDeadEndTests
    {
        private const string ControllerPath = "Assets/Animations/PlayerAnimationController.controller";
        private const float Step = 0.05f;
        private const float SettleSeconds = 8f;
        private const float WatchSeconds = 3f;

        /// <summary>What the game turns on after ResetAnimationStates, as PlayerAnimationController sets it.</summary>
        private class Activity
        {
            public string Name;
            public string Bool;
            public bool StartTrigger = true;
            public int? Weapon;
            public int? Action;
            public override string ToString() => Name;
        }

        private static readonly Activity[] Activities = BuildActivities();

        /// <summary>
        /// States the game's code holds while combat is on and ends itself, so they stay put while Attacking is: they
        /// must still leave once everything is off. Name the code that ends each one.
        /// </summary>
        private static readonly Dictionary<string, string> HeldInCombat = new Dictionary<string, string>
        {
            // HeroController.StartSpin plays it and StopSpin plays Hero-SpinEnd when the spin's time is up, when hero
            // mode ends (HeroController.End) and after a revive
            ["Base Layer.Hero-Spin"] = "HeroController.StopSpin",
        };

        private static Activity[] BuildActivities()
        {
            var list = new List<Activity>
            {
                new Activity { Name = "Mining", Bool = "Mining" },
                new Activity { Name = "Woodcutting", Bool = "Woodcutting" },
                new Activity { Name = "Fishing", Bool = "Fishing" },
                new Activity { Name = "Crafting", Bool = "Crafting" },
                new Activity { Name = "Cooking", Bool = "Cooking" },
                new Activity { Name = "Brewing", Bool = "Brewing" },
                new Activity { Name = "Farming", Bool = "Farming" },
                new Activity { Name = "Gathering kneeling", Bool = "Gathering", Action = 0 },
                new Activity { Name = "Gathering standing", Bool = "Gathering", Action = 1 },
                new Activity { Name = "Onsen sitting", Bool = "Sitting", StartTrigger = false },
                new Activity { Name = "Onsen swimming", Bool = "Swimming", StartTrigger = false },
                new Activity { Name = "Onsen meditating", Bool = "Meditating", StartTrigger = false },
                new Activity { Name = "Onsen sleeping", Bool = "Sleeping", StartTrigger = false },
                new Activity { Name = "Ferry captain", Bool = "Captain", StartTrigger = false },
            };
            // PlayerCombat's weapon animations: 0 unarmed, 1 sword, 2 two handed sword, 3 bow, 4 staff (magic),
            // 5 katana, 6 healing, 7 spear, 8 two handed axe
            for (var w = 0; w <= 8; ++w)
            {
                list.Add(new Activity { Name = "Combat weapon " + w, Bool = "Attacking", Weapon = w });
            }
            return list.ToArray();
        }

        internal class StateRef
        {
            public int Layer;
            public string Path;
            public int Hash;
            public override string ToString() => Path;
        }

        private static AnimatorController Controller => AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        /// <summary>Every state something leads to (a transition, an entry, a default state); the rest can never play.</summary>
        private static IEnumerable<StateRef> ReachableStates()
        {
            var controller = Controller;
            var result = new List<StateRef>();
            for (var layer = 0; layer < controller.layers.Length; ++layer)
            {
                var root = controller.layers[layer].stateMachine;
                var reached = new HashSet<AnimatorState>();
                Collect(root, reached);
                Walk(root, controller.layers[layer].name, layer, reached, result);
            }
            return result;
        }

        private static void Collect(AnimatorStateMachine sm, HashSet<AnimatorState> reached)
        {
            if (sm.defaultState) reached.Add(sm.defaultState);
            foreach (var t in sm.anyStateTransitions) if (t.destinationState) reached.Add(t.destinationState);
            foreach (var t in sm.entryTransitions) if (t.destinationState) reached.Add(t.destinationState);
            foreach (var s in sm.states)
                foreach (var t in s.state.transitions)
                    if (t.destinationState) reached.Add(t.destinationState);
            foreach (var c in sm.stateMachines)
            {
                foreach (var t in sm.GetStateMachineTransitions(c.stateMachine))
                    if (t.destinationState) reached.Add(t.destinationState);
                Collect(c.stateMachine, reached);
            }
        }

        private static void Walk(AnimatorStateMachine sm, string path, int layer, HashSet<AnimatorState> reached, List<StateRef> result)
        {
            foreach (var s in sm.states)
            {
                if (!reached.Contains(s.state)) continue;
                var full = path + "." + s.state.name;
                result.Add(new StateRef { Layer = layer, Path = full, Hash = Animator.StringToHash(full) });
            }
            foreach (var c in sm.stateMachines)
            {
                Walk(c.stateMachine, path + "." + c.stateMachine.name, layer, reached, result);
            }
        }

        private GameObject host;
        private Animator animator;
        private Dictionary<string, HashSet<int>> expected;

        [OneTimeSetUp]
        public void SetUp()
        {
            host = new GameObject("PlayerAnimatorDeadEndTests") { hideFlags = HideFlags.HideAndDontSave };
            animator = host.AddComponent<Animator>();
            animator.runtimeAnimatorController = Controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // the clips' events (the captain's spin, footsteps) have no receivers on a bare GameObject
            animator.fireEvents = false;

            // where a fresh character settles with everything off, and with each activity on: the places it must reach
            expected = new Dictionary<string, HashSet<int>>();
            for (var layer = 0; layer < animator.layerCount; ++layer)
            {
                animator.Rebind();
                Rest();
                expected["rest:" + layer] = Settle(layer);
                foreach (var a in Activities)
                {
                    animator.Rebind();
                    Rest();
                    Begin(a);
                    expected[a.Name + ":" + layer] = Settle(layer);
                }
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (host) Object.DestroyImmediate(host);
        }

        /// <summary>Everything off and no trigger waiting, as after ResetAnimationStates with the player standing still.</summary>
        private void Rest()
        {
            foreach (var p in animator.parameters)
            {
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Bool: animator.SetBool(p.nameHash, false); break;
                    case AnimatorControllerParameterType.Int: animator.SetInteger(p.nameHash, 0); break;
                    case AnimatorControllerParameterType.Float: animator.SetFloat(p.nameHash, p.defaultFloat); break;
                    case AnimatorControllerParameterType.Trigger: animator.ResetTrigger(p.nameHash); break;
                }
            }
        }

        private void Begin(Activity a)
        {
            if (a.Weapon.HasValue) animator.SetInteger("Weapon", a.Weapon.Value);
            if (a.Action.HasValue) animator.SetInteger("Action", a.Action.Value);
            if (a.StartTrigger) animator.SetTrigger("Start");
            animator.SetBool(a.Bool, true);
        }

        /// <summary>Steps the animator and returns the states it was in over the last few seconds.</summary>
        private HashSet<int> Settle(int layer)
        {
            var seen = new HashSet<int>();
            for (var t = 0f; t < SettleSeconds; t += Step)
            {
                animator.Update(Step);
                if (t >= SettleSeconds - WatchSeconds) seen.Add(animator.GetCurrentAnimatorStateInfo(layer).fullPathHash);
            }
            return seen;
        }

        private bool Enter(StateRef state)
        {
            animator.Rebind();
            Rest();
            animator.Play(state.Hash, state.Layer, 0f);
            animator.Update(0f);
            return animator.GetCurrentAnimatorStateInfo(state.Layer).fullPathHash == state.Hash;
        }

        private string Names(HashSet<int> hashes)
        {
            var names = ReachableStates().Where(s => hashes.Contains(s.Hash)).Select(s => s.Path).ToList();
            return names.Count > 0 ? string.Join(", ", names) : "?";
        }

        private static IEnumerable<StateRef> States() => ReachableStates();

        [TestCaseSource(nameof(States))]
        public void ReturnsToIdleWhenEverythingStops(StateRef state)
        {
            Assert.IsTrue(Enter(state), "could not enter " + state.Path);
            var home = expected["rest:" + state.Layer];
            var seen = Settle(state.Layer);
            Assert.IsTrue(seen.Overlaps(home),
                state.Path + " never gets back to idle once everything is off; it stays in " + Names(seen));
        }

        [TestCaseSource(nameof(States))]
        public void StartsEveryActivity(StateRef state)
        {
            var failed = new List<string>();
            foreach (var a in Activities)
            {
                if (a.Bool == "Attacking" && HeldInCombat.ContainsKey(state.Path)) continue;
                Assert.IsTrue(Enter(state), "could not enter " + state.Path);
                Begin(a);
                var seen = Settle(state.Layer);
                if (!seen.Overlaps(expected[a.Name + ":" + state.Layer]))
                {
                    failed.Add(a.Name + " (stays in " + Names(seen) + ")");
                }
            }
            Assert.IsEmpty(failed, state.Path + " cannot start: " + string.Join("; ", failed));
        }
    }
}
