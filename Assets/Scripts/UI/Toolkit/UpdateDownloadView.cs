using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// A new version, in the bottom left corner's stack (the connection warnings' card, amber: news, not trouble): the
    /// background download's progress (BackgroundUpdate), then Update now, which restarts straight into the patcher
    /// (the user, 2026-11-04: less time on the update screen for the viewers). With the setting off, or a download that
    /// failed, Update now restarts and downloads on the update screen as before. Replaces the line at the top of the
    /// screen. The x tucks it away until the download is done.
    /// </summary>
    public class UpdateDownloadView : MonoBehaviour
    {
        private VisualElement card, bar, fill;
        private Label icon, title, note;
        private Button update, retry, notNow;
        private bool bound;
        private float nextRefresh;
        private bool hidden;
        private BackgroundUpdate.Phase hiddenAt;

        private void OnEnable()
        {
            bound = false;
            Languages.Changed += Rebind;
        }

        private void OnDisable()
        {
            Languages.Changed -= Rebind;
            card?.RemoveFromHierarchy();
            bound = false;
        }

        private void Rebind() => bound = false;

        private void Update()
        {
            if (bound && card.panel == null) bound = false;
            if (!bound) TryBind();
            if (!bound || Time.unscaledTime < nextRefresh) return;
            BackgroundUpdate.Tick();
            nextRefresh = Time.unscaledTime + 0.25f;
            Refresh();
        }

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var stack = doc != null ? doc.rootVisualElement?.Q<VisualElement>(className: "rf-conn-stack") : null;
            if (stack == null) return;

            card?.RemoveFromHierarchy();
            card = new VisualElement { name = "update-card" };
            card.AddToClassList("rf-conn-server");
            card.AddToClassList("rf-conn-server--outdated");
            card.AddToClassList("rf-update");

            icon = new Label("") { pickingMode = PickingMode.Ignore }; // a download arrow
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-conn-server__icon");
            card.Add(icon);

            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("rf-conn-server__body");
            body.AddToClassList("rf-update__body");
            card.Add(body);

            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList("rf-update__head");
            title = new Label { pickingMode = PickingMode.Ignore };
            title.AddToClassList("rf-conn-server__title");
            title.AddToClassList("rf-update__title");
            head.Add(title);
            var close = new Button(() => { hidden = true; hiddenAt = BackgroundUpdate.State; Refresh(); }) { text = "", tooltip = Localization.UI_UPDATE_CARD_HIDE };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-update__close");
            head.Add(close);
            body.Add(head);

            note = new Label { pickingMode = PickingMode.Ignore };
            note.AddToClassList("rf-conn-server__note");
            body.Add(note);

            bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("rf-update__bar");
            fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("rf-update__fill");
            bar.Add(fill);
            body.Add(bar);

            var actions = new VisualElement { pickingMode = PickingMode.Ignore };
            actions.AddToClassList("rf-update__actions");
            update = new Button(BackgroundUpdate.UpdateNow) { text = Localization.UI_UPDATE_CARD_BUTTON };
            update.AddToClassList("rf-button");
            update.AddToClassList("rf-button--primary");
            update.AddToClassList("rf-update__button");
            retry = new Button(BackgroundUpdate.Retry) { text = Localization.UI_UPDATE_CARD_RETRY };
            retry.AddToClassList("rf-button");
            retry.AddToClassList("rf-update__button");
            notNow = new Button(BackgroundUpdate.CancelAuto) { text = Localization.UI_UPDATE_CARD_NOT_NOW };
            notNow.AddToClassList("rf-button");
            notNow.AddToClassList("rf-update__button");
            actions.Add(update);
            actions.Add(retry);
            actions.Add(notNow);
            body.Add(actions);

            // at the top of the stack: the warnings below it stay where they were
            stack.Insert(0, card);
            bound = true;
            Refresh();
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            var version = BackgroundUpdate.Version;
            var state = BackgroundUpdate.State;
            // tucked away until the download is done; never while the automatic restart counts down
            if (hidden && state != hiddenAt && state == BackgroundUpdate.Phase.Ready) hidden = false;
            var counting = BackgroundUpdate.AutoUpdateAt >= 0f;
            if (counting) hidden = false;
            var show = gm && gm.NewUpdateAvailable && !string.IsNullOrEmpty(version) && !hidden;
            card.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;

            var progress = BackgroundUpdate.Progress;
            var downloading = state == BackgroundUpdate.Phase.Downloading;
            var ready = state == BackgroundUpdate.Phase.Ready;
            card.EnableInClassList("rf-update--ready", ready);

            title.text = string.Format(CultureInfo.InvariantCulture, ready ? Localization.UI_UPDATE_CARD_READY_TITLE : Localization.UI_UPDATE_CARD_TITLE, version);
            icon.text = ready ? "" : "";

            if (downloading)
            {
                note.text = DownloadLine(progress);
                var share = progress != null && progress.FileSize > 0 ? Mathf.Clamp01((float)progress.BytesDownloaded / progress.FileSize) : 0f;
                fill.style.width = Length.Percent(share * 100f);
            }
            else if (ready && counting)
                note.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_UPDATE_CARD_AUTO, Mathf.CeilToInt(BackgroundUpdate.AutoUpdateAt - Time.unscaledTime));
            else if (ready && BackgroundUpdate.AutoUpdateWaiting) note.text = Localization.UI_UPDATE_CARD_AUTO_WAIT;
            else if (ready) note.text = Localization.UI_UPDATE_CARD_READY;
            else if (state == BackgroundUpdate.Phase.Failed) note.text = Localization.UI_UPDATE_CARD_FAILED;
            else note.text = Localization.UI_UPDATE_CARD_RESTART;

            bar.style.display = downloading ? DisplayStyle.Flex : DisplayStyle.None;
            // Update now is there throughout: with the download unfinished it restarts and finishes it on the update screen
            update.style.display = DisplayStyle.Flex;
            retry.style.display = state == BackgroundUpdate.Phase.Failed ? DisplayStyle.Flex : DisplayStyle.None;
            notNow.style.display = ready && (counting || BackgroundUpdate.AutoUpdateWaiting) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string DownloadLine(DownloadProgress p)
        {
            if (p == null || p.FileSize <= 0) return Localization.UI_UPDATE_CARD_STARTING;
            var mb = 1024d * 1024d;
            var line = string.Format(CultureInfo.InvariantCulture, Localization.UI_UPDATE_CARD_DOWNLOADING,
                Mathf.FloorToInt(100f * p.BytesDownloaded / p.FileSize), (p.BytesDownloaded / mb).ToString("0", CultureInfo.InvariantCulture),
                (p.FileSize / mb).ToString("0", CultureInfo.InvariantCulture));
            if (p.BytesPerSecond > 1)
            {
                var seconds = (p.FileSize - p.BytesDownloaded) / p.BytesPerSecond;
                var left = seconds >= 90 ? string.Format(CultureInfo.InvariantCulture, Localization.UI_UPDATE_CARD_MINUTES, Mathf.CeilToInt((float)(seconds / 60)))
                    : string.Format(CultureInfo.InvariantCulture, Localization.UI_UPDATE_CARD_SECONDS, Mathf.CeilToInt((float)seconds));
                line += " - " + left;
            }
            return line;
        }
    }
}
