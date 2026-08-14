using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// UI Toolkit implementation of the code of conduct screen.
    ///
    /// <para>
    /// Second screen migrated off uGUI, after the updater. See docs/ui-toolkit-migration.md.
    /// </para>
    ///
    /// <para>
    /// Values set on this view are held until the visual tree exists and then applied. Unity runs
    /// every Awake before any OnEnable, and UIDocument builds its tree in OnEnable, so a controller
    /// writing from Awake would otherwise be writing into nothing and the screen would come up
    /// blank with no error.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CodeOfConductScreenView : MonoBehaviour, ICodeOfConductScreen
    {
        private Label headerLabel;
        private Label messageLabel;
        private Label versionLabel;
        private Label modifiedLabel;
        private Button acceptButton;
        private Button declineButton;

        private Action onAccept;
        private Action onDecline;

        // Held until the tree is available.
        private string pendingHeader;
        private string pendingMessage;
        private string pendingVersion;
        private string pendingModified;

        private bool bound;

        private void OnEnable()
        {
            TryBind();
        }

        private void Update()
        {
            // Cheap no-op once bound. Covers the case where the document is not laid out on the
            // first OnEnable, which happens depending on component order in the scene.
            if (!bound)
            {
                TryBind();
            }
        }

        private void TryBind()
        {
            if (bound)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null)
            {
                return;
            }

            headerLabel = root.Q<Label>("header-label");
            messageLabel = root.Q<Label>("message-label");
            versionLabel = root.Q<Label>("version-label");
            modifiedLabel = root.Q<Label>("modified-label");
            acceptButton = root.Q<Button>("accept-button");
            declineButton = root.Q<Button>("decline-button");

            if (acceptButton != null)
            {
                acceptButton.clicked -= HandleAccept;
                acceptButton.clicked += HandleAccept;
            }
            if (declineButton != null)
            {
                declineButton.clicked -= HandleDecline;
                declineButton.clicked += HandleDecline;
            }

            bound = true;

            // Flush anything set before the tree existed.
            if (pendingHeader != null) headerLabel.text = pendingHeader;
            if (pendingMessage != null && messageLabel != null) messageLabel.text = pendingMessage;
            if (pendingVersion != null && versionLabel != null) versionLabel.text = pendingVersion;
            if (pendingModified != null && modifiedLabel != null) modifiedLabel.text = pendingModified;
        }

        private void OnDisable()
        {
            if (acceptButton != null) acceptButton.clicked -= HandleAccept;
            if (declineButton != null) declineButton.clicked -= HandleDecline;
            bound = false;
        }

        private void HandleAccept() => onAccept?.Invoke();
        private void HandleDecline() => onDecline?.Invoke();

        public void BindActions(Action accept, Action decline)
        {
            onAccept = accept;
            onDecline = decline;
            TryBind();
        }

        public string Header
        {
            set { pendingHeader = value; TryBind(); if (bound && headerLabel != null) headerLabel.text = value; }
        }

        public string Message
        {
            set { pendingMessage = value; TryBind(); if (bound && messageLabel != null) messageLabel.text = value; }
        }

        public string Version
        {
            set { pendingVersion = value; TryBind(); if (bound && versionLabel != null) versionLabel.text = value; }
        }

        public string LastModified
        {
            set { pendingModified = value; TryBind(); if (bound && modifiedLabel != null) modifiedLabel.text = value; }
        }
    }
}
