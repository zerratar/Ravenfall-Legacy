using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The game's rebindable keyboard shortcuts. Every hotkey check goes through here rather than
/// testing a KeyCode itself, so a key can be changed in the settings and the shortcut menu shows
/// what it is now. Only bindings that differ from the defaults are saved, in
/// <see cref="PlayerSettings.KeyBindings"/> as "Action=Ctrl+F".
/// </summary>
/// <remarks>
/// Modifiers match exactly: Ctrl+F does not fire for Ctrl+Shift+F, and F2 does not fire while Ctrl
/// is held. The old checks treated Shift as Ctrl, so Shift+F also opened the search. Nothing fires
/// while a text field has the keyboard (typing a Q in the player search used to hide the logos), or
/// while the settings window is waiting for a key to bind. Esc is not rebindable; it always backs
/// out of whatever is open.
/// </remarks>
public static class Keybinds
{
    public enum Action
    {
        ToggleHud, PlayerDetails, NextPlayer, FreeCamera, NextIsland, ControlPlayer,
        Search, ExpRate, Logos, UiBigger, UiSmaller, Graphics, Physics, SaveState, Reload,
        StartRaid, StartDungeon,
    }

    [Flags]
    public enum Mods { None = 0, Ctrl = 1, Shift = 2, Alt = 4 }

    /// <summary>When the action fires: as the key goes down, as it comes up, or for as long as it is held.</summary>
    public enum Trigger { Down, Up, Held }

    public struct Binding : IEquatable<Binding>
    {
        public KeyCode Key;
        public Mods Mods;

        public Binding(KeyCode key, Mods mods = Mods.None) { Key = key; Mods = mods; }

        public bool IsNone => Key == KeyCode.None;
        public static Binding None => new Binding(KeyCode.None);

        public bool Equals(Binding other) => Key == other.Key && Mods == other.Mods;
        public override bool Equals(object obj) => obj is Binding b && Equals(b);
        public override int GetHashCode() => ((int)Key * 8) ^ (int)Mods;
        public static bool operator ==(Binding a, Binding b) => a.Equals(b);
        public static bool operator !=(Binding a, Binding b) => !a.Equals(b);

        /// <summary>As stored, and as shown: "Ctrl+F", "Shift+Tab", "F9".</summary>
        public override string ToString()
        {
            if (IsNone) return "-";
            var parts = new List<string>(4);
            if ((Mods & Mods.Ctrl) != 0) parts.Add("Ctrl");
            if ((Mods & Mods.Shift) != 0) parts.Add("Shift");
            if ((Mods & Mods.Alt) != 0) parts.Add("Alt");
            parts.Add(KeyName(Key));
            return string.Join("+", parts);
        }

        public static bool TryParse(string text, out Binding binding)
        {
            binding = None;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (text.Trim() == "-") return true;
            var parts = text.Split('+');
            var mods = Mods.None;
            for (var i = 0; i < parts.Length - 1; ++i)
            {
                switch (parts[i].Trim().ToLowerInvariant())
                {
                    case "ctrl": mods |= Mods.Ctrl; break;
                    case "shift": mods |= Mods.Shift; break;
                    case "alt": mods |= Mods.Alt; break;
                    default: return false;
                }
            }

            // "Ctrl++" splits into an empty last part: that is the plus key
            var last = parts[parts.Length - 1].Trim();
            if (last.Length == 0 && text.EndsWith("+")) last = "+";
            if (!TryKey(last, out var key)) return false;
            binding = new Binding(key, mods);
            return true;
        }
    }

    private sealed class Def
    {
        public Action Action;
        public Binding Default;
        public Trigger Trigger;
        public string Name;
        public bool AdminOnly;
    }

    private static readonly List<Def> defs = new List<Def>
    {
        new Def { Action = Action.ToggleHud, Default = new Binding(KeyCode.F1), Trigger = Trigger.Down, Name = Localization.UI_KEY_HUD },
        new Def { Action = Action.PlayerDetails, Default = new Binding(KeyCode.F2), Trigger = Trigger.Up, Name = Localization.UI_KEY_DETAILS },
        new Def { Action = Action.NextPlayer, Default = new Binding(KeyCode.Tab), Trigger = Trigger.Down, Name = Localization.UI_KEY_NEXT_PLAYER },
        new Def { Action = Action.FreeCamera, Default = new Binding(KeyCode.Tab, Mods.Shift), Trigger = Trigger.Held, Name = Localization.UI_KEY_FREE_CAMERA },
        new Def { Action = Action.NextIsland, Default = new Binding(KeyCode.I, Mods.Ctrl), Trigger = Trigger.Down, Name = Localization.UI_KEY_NEXT_ISLAND },
        new Def { Action = Action.ControlPlayer, Default = new Binding(KeyCode.F3), Trigger = Trigger.Up, Name = Localization.UI_KEY_CONTROL_PLAYER },
        new Def { Action = Action.Search, Default = new Binding(KeyCode.F, Mods.Ctrl), Trigger = Trigger.Up, Name = Localization.UI_KEY_SEARCH },
        new Def { Action = Action.ExpRate, Default = new Binding(KeyCode.I), Trigger = Trigger.Up, Name = Localization.UI_KEY_EXP },
        new Def { Action = Action.Logos, Default = new Binding(KeyCode.Q), Trigger = Trigger.Up, Name = Localization.UI_KEY_LOGOS },
        new Def { Action = Action.UiBigger, Default = new Binding(KeyCode.Equals, Mods.Ctrl), Trigger = Trigger.Down, Name = Localization.UI_KEY_UI_BIGGER },
        new Def { Action = Action.UiSmaller, Default = new Binding(KeyCode.Minus, Mods.Ctrl), Trigger = Trigger.Down, Name = Localization.UI_KEY_UI_SMALLER },
        new Def { Action = Action.Graphics, Default = new Binding(KeyCode.F9), Trigger = Trigger.Down, Name = Localization.UI_KEY_GRAPHICS },
        new Def { Action = Action.Physics, Default = new Binding(KeyCode.F4), Trigger = Trigger.Up, Name = Localization.UI_KEY_PHYSICS },
        new Def { Action = Action.SaveState, Default = new Binding(KeyCode.S, Mods.Ctrl), Trigger = Trigger.Up, Name = Localization.UI_KEY_SAVE },
        new Def { Action = Action.Reload, Default = new Binding(KeyCode.F5), Trigger = Trigger.Down, Name = Localization.UI_KEY_RELOAD },
        new Def { Action = Action.StartRaid, Default = new Binding(KeyCode.P, Mods.Ctrl), Trigger = Trigger.Up, Name = Localization.UI_KEY_RAID, AdminOnly = true },
        new Def { Action = Action.StartDungeon, Default = new Binding(KeyCode.O, Mods.Ctrl), Trigger = Trigger.Up, Name = Localization.UI_KEY_DUNGEON, AdminOnly = true },
    };

    private static readonly Dictionary<Action, Binding> current = new Dictionary<Action, Binding>();
    private static bool loaded;

    /// <summary>Raised when a binding changes, for the shortcut menu's badges.</summary>
    public static event System.Action Changed;

    /// <summary>Set while the settings window waits for a key to bind, so that key does nothing else.</summary>
    public static bool Suspended { get; set; }

    public static IEnumerable<Action> All => defs.Select(d => d.Action);

    public static string Name(Action action) => Find(action).Name;
    public static bool AdminOnly(Action action) => Find(action).AdminOnly;
    public static Binding Default(Action action) => Find(action).Default;

    public static Binding Get(Action action)
    {
        EnsureLoaded();
        return current.TryGetValue(action, out var b) ? b : Find(action).Default;
    }

    public static string Label(Action action) => Get(action).ToString();

    public static bool IsDefault(Action action) => Get(action) == Default(action);

    /// <summary>
    /// Binds <paramref name="binding"/> to <paramref name="action"/>. Any other action already on
    /// that key gets this action's old key, so nothing ends up on two actions or on none.
    /// </summary>
    /// <returns>The action that was swapped, if any.</returns>
    public static Action? Set(Action action, Binding binding)
    {
        EnsureLoaded();
        var old = Get(action);
        Action? swapped = null;
        if (!binding.IsNone)
        {
            foreach (var d in defs)
            {
                if (d.Action != action && Get(d.Action) == binding)
                {
                    current[d.Action] = old;
                    swapped = d.Action;
                }
            }
        }

        current[action] = binding;
        Store();
        return swapped;
    }

    public static void Reset(Action action) => Set(action, Default(action));

    public static void ResetAll()
    {
        EnsureLoaded();
        current.Clear();
        foreach (var d in defs) current[d.Action] = d.Default;
        Store();
    }

    /// <summary>The action fired this frame, by its trigger: down, up, or (for held actions) down.</summary>
    public static bool Pressed(Action action)
    {
        if (!Ready()) return false;
        var d = Find(action);
        var b = Get(action);
        if (b.IsNone || !ModsMatch(b.Mods)) return false;
        return d.Trigger == Trigger.Up ? Input.GetKeyUp(b.Key) : Input.GetKeyDown(b.Key);
    }

    /// <summary>The action's key and modifiers are held down.</summary>
    public static bool Held(Action action)
    {
        if (!Ready()) return false;
        var b = Get(action);
        return !b.IsNone && ModsMatch(b.Mods) && Input.GetKey(b.Key);
    }

    /// <summary>The modifiers held right now.</summary>
    public static Mods HeldMods()
    {
        var mods = Mods.None;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) mods |= Mods.Ctrl;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) mods |= Mods.Shift;
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) mods |= Mods.Alt;
        return mods;
    }

    /// <summary>Keys that can be bound: not modifiers, not the mouse, not Esc.</summary>
    public static bool IsBindable(KeyCode key)
    {
        if (key == KeyCode.None || key == KeyCode.Escape) return false;
        if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) return false;
        if (key >= KeyCode.JoystickButton0) return false;
        switch (key)
        {
            case KeyCode.LeftControl: case KeyCode.RightControl:
            case KeyCode.LeftShift: case KeyCode.RightShift:
            case KeyCode.LeftAlt: case KeyCode.RightAlt:
            case KeyCode.LeftCommand: case KeyCode.RightCommand:
            case KeyCode.LeftWindows: case KeyCode.RightWindows:
            case KeyCode.AltGr:
                return false;
        }

        return true;
    }

    private static bool ModsMatch(Mods required) => HeldMods() == required;

    private static bool Ready() => !Suspended && !IsTyping;

    private static Def Find(Action action)
    {
        for (var i = 0; i < defs.Count; ++i) if (defs[i].Action == action) return defs[i];
        throw new ArgumentOutOfRangeException(nameof(action));
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        current.Clear();
        foreach (var d in defs) current[d.Action] = d.Default;

        var saved = PlayerSettings.Instance.KeyBindings;
        if (saved == null) return;
        foreach (var entry in saved)
        {
            if (string.IsNullOrEmpty(entry)) continue;
            var eq = entry.IndexOf('=');
            if (eq <= 0) continue;
            if (!Enum.TryParse<Action>(entry.Substring(0, eq).Trim(), out var action)) continue;
            if (Binding.TryParse(entry.Substring(eq + 1), out var binding)) current[action] = binding;
        }
    }

    private static void Store()
    {
        PlayerSettings.Instance.KeyBindings = defs
            .Where(d => current[d.Action] != d.Default)
            .Select(d => d.Action + "=" + current[d.Action])
            .ToArray();
        Changed?.Invoke();
    }

    // ---- Text fields ----------------------------------------------------------------------------

    private static int typingFrame = -1;
    private static bool typing;
    private static readonly List<UIDocument> documents = new List<UIDocument>();
    private static float nextDocumentScan;

    /// <summary>
    /// True while a text field has the keyboard, in uGUI (the player search) or UI Toolkit (the
    /// build mode search, the settings window's fields). Worked out once a frame.
    /// </summary>
    public static bool IsTyping
    {
        get
        {
            if (typingFrame == Time.frameCount) return typing;
            typingFrame = Time.frameCount;
            typing = CheckTyping();
            return typing;
        }
    }

    private static bool CheckTyping()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        var selected = es ? es.currentSelectedGameObject : null;
        if (selected)
        {
            var tmp = selected.GetComponent<TMPro.TMP_InputField>();
            if (tmp && tmp.isFocused) return true;
            var legacy = selected.GetComponent<UnityEngine.UI.InputField>();
            if (legacy && legacy.isFocused) return true;
        }

        if (Time.unscaledTime >= nextDocumentScan)
        {
            nextDocumentScan = Time.unscaledTime + 2f;
            documents.Clear();
            documents.AddRange(UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None));
        }

        for (var i = 0; i < documents.Count; ++i)
        {
            var doc = documents[i];
            if (!doc || doc.rootVisualElement == null || doc.rootVisualElement.panel == null) continue;
            for (var e = doc.rootVisualElement.panel.focusController?.focusedElement as VisualElement; e != null; e = e.parent)
            {
                if (e is TextField) return true;
            }
        }

        return false;
    }

    // ---- Key names ------------------------------------------------------------------------------

    private static string KeyName(KeyCode key)
    {
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) return ((int)(key - KeyCode.Alpha0)).ToString();
        if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) return "Num" + (int)(key - KeyCode.Keypad0);
        switch (key)
        {
            case KeyCode.Equals: return "+";
            case KeyCode.Plus: return "+";
            case KeyCode.Minus: return "-";
            case KeyCode.KeypadPlus: return "Num+";
            case KeyCode.KeypadMinus: return "Num-";
            case KeyCode.KeypadMultiply: return "Num*";
            case KeyCode.KeypadDivide: return "Num/";
            case KeyCode.KeypadEnter: return "NumEnter";
            case KeyCode.Return: return "Enter";
            case KeyCode.Escape: return "Esc";
            case KeyCode.Space: return "Space";
            case KeyCode.Backspace: return "Backspace";
            case KeyCode.Delete: return "Del";
            case KeyCode.Insert: return "Ins";
            case KeyCode.PageUp: return "PgUp";
            case KeyCode.PageDown: return "PgDn";
            case KeyCode.UpArrow: return "Up";
            case KeyCode.DownArrow: return "Down";
            case KeyCode.LeftArrow: return "Left";
            case KeyCode.RightArrow: return "Right";
            case KeyCode.Comma: return ",";
            case KeyCode.Period: return ".";
            case KeyCode.Slash: return "/";
            case KeyCode.Backslash: return "\\";
            case KeyCode.Semicolon: return ";";
            case KeyCode.Quote: return "'";
            case KeyCode.LeftBracket: return "[";
            case KeyCode.RightBracket: return "]";
            case KeyCode.BackQuote: return "`";
            default: return key.ToString();
        }
    }

    private static bool TryKey(string name, out KeyCode key)
    {
        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
        {
            if (KeyName(k).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                // "+" is both Equals (the main keyboard key) and Plus; the key that can be pressed wins
                key = name == "+" ? KeyCode.Equals : k;
                return true;
            }
        }

        return Enum.TryParse(name, true, out key);
    }
}
