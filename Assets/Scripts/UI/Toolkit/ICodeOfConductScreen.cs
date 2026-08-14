using System;

namespace Shinobytes.UI
{
    /// <summary>
    /// What CodeOfConductController needs from whatever is drawing the code of conduct.
    ///
    /// <para>
    /// Same shape as <see cref="IUpdateScreen"/>: the controller does not care whether the original
    /// uGUI hierarchy or the UI Toolkit replacement is present, so a screen can be swapped, play
    /// tested and reverted by clearing a field.
    /// </para>
    ///
    /// <para>
    /// Unlike the update screen this one has actions, not just output. The old screen wired Accept
    /// and Decline through UnityEvents on the buttons in the scene; the UI Toolkit version has no
    /// scene wiring to hang those on, so the controller hands them over here instead.
    /// </para>
    /// </summary>
    public interface ICodeOfConductScreen
    {
        string Header { set; }
        string Message { set; }
        string Version { set; }
        string LastModified { set; }

        /// <summary>
        /// Supplies what the buttons do. Called once by the controller during setup.
        /// </summary>
        void BindActions(Action onAccept, Action onDecline);
    }
}
