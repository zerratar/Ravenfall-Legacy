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
        private bool queried;

        private void OnEnable()
        {
            Query();
        }

        private void Query()
        {
            if (queried)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null)
            {
                // Not laid out yet. Whatever the controller sets next will try again.
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
                acceptButton.clicked += HandleAccept;
            }
            if (declineButton != null)
            {
                declineButton.clicked += HandleDecline;
            }

            queried = true;
        }

        private void OnDisable()
        {
            if (acceptButton != null)
            {
                acceptButton.clicked -= HandleAccept;
            }
            if (declineButton != null)
            {
                declineButton.clicked -= HandleDecline;
            }
            queried = false;
        }

        private void HandleAccept()
        {
            onAccept?.Invoke();
        }

        private void HandleDecline()
        {
            onDecline?.Invoke();
        }

        public void BindActions(Action accept, Action decline)
        {
            onAccept = accept;
            onDecline = decline;
            Query();
        }

        public string Header
        {
            set { Query(); if (headerLabel != null) headerLabel.text = value; }
        }

        public string Message
        {
            set { Query(); if (messageLabel != null) messageLabel.text = value; }
        }

        public string Version
        {
            set { Query(); if (versionLabel != null) versionLabel.text = value; }
        }

        public string LastModified
        {
            set { Query(); if (modifiedLabel != null) modifiedLabel.text = value; }
        }
    }
}
