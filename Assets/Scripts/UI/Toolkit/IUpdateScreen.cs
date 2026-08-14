namespace Shinobytes.UI
{
    /// <summary>
    /// What the updater needs from whatever is drawing the launcher screen.
    ///
    /// <para>
    /// Exists so GameUpdater does not care whether the screen is the original uGUI hierarchy or the
    /// UI Toolkit replacement. During the migration both are present: the legacy path stays wired
    /// in the scene and keeps working, and the new one is opted into by assigning it. That way a
    /// screen can be swapped, play tested, and reverted by unassigning a field rather than by
    /// rebuilding a scene.
    /// </para>
    /// </summary>
    public interface IUpdateScreen
    {
        /// <summary>Status line, from "Checking for updates." through to error text.</summary>
        string Status { set; }

        /// <summary>Version string shown under the title.</summary>
        string Version { set; }

        /// <summary>Whether the progress bar is shown at all.</summary>
        bool ProgressVisible { set; }

        /// <summary>Progress in the range 0 to 1.</summary>
        float Progress { set; }
    }
}
