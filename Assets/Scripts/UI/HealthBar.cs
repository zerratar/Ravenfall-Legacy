using Assets.Scripts;
using System;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform progress;

    [SerializeField] private CanvasGroup canvasGroup;

    public IAttackable Target;
    public float YOffset = 0.2f;
    public HealthBarManager Manager;
    private CapsuleCollider capsuleCollider;
    private DungeonBossController dungeonBoss;
    private PlayerController player;
    private bool isPlayer;
    private bool isDungeonBoss;
    private Transform targetTransform;
    private Transform targetCamera;
    private Vector3 targetOffset;
    private bool hasTarget;
    private bool isActive;

    // Start is called before the first frame update
    void Start()
    {
        if (!canvasGroup)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (!progress)
        {
            progress = gameObject.transform.GetChild(0).GetComponent<RectTransform>();
        }

        Dress();
    }

    /// <summary>
    /// The look, built here so it lives in the repo (the prefab does not): a dark, gold-rimmed track like the HUD's frames,
    /// a shaded fill (red for enemies, the party panel's green for players), and a pale trail behind it that shows each
    /// hit's bite. Sprites in Assets/World/Markers/Resources/HealthBar.
    /// </summary>
    private void Dress()
    {
        if (dressed || !progress) return;
        var track = Resources.Load<Sprite>("HealthBar/track");
        var fill = Resources.Load<Sprite>("HealthBar/fill");
        if (!track || !fill) return;
        dressed = true;

        var root = (RectTransform)transform;
        root.sizeDelta = new Vector2(100f, 15f);
        root.localScale = Vector3.one * Width / 100f;
        var back = GetComponent<UnityEngine.UI.Image>();
        if (back)
        {
            back.sprite = track;
            back.type = UnityEngine.UI.Image.Type.Sliced;
            back.pixelsPerUnitMultiplier = 2f;
            back.color = Color.white;
        }

        var inner = new GameObject("Inner", typeof(RectTransform)).GetComponent<RectTransform>();
        inner.SetParent(root, false);
        inner.anchorMin = inner.anchorMax = inner.pivot = new Vector2(0.5f, 0.5f);
        inner.sizeDelta = new Vector2(100f, 9.6f);
        inner.localScale = new Vector3(0.94f, 1f, 1f);

        trail = Bar("Trail", inner, fill, new Color(1f, 0.9f, 0.72f, 0.95f));
        progress.SetParent(inner, false);
        progress.anchorMin = progress.anchorMax = progress.pivot = new Vector2(0f, 0.5f);
        progress.anchoredPosition = Vector2.zero;
        progress.sizeDelta = new Vector2(100f, 9.6f);
        var image = progress.GetComponent<UnityEngine.UI.Image>();
        if (image)
        {
            image.sprite = fill;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2.4f;
        }
        Colour();
    }

    private static RectTransform Bar(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var bar = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
        bar.SetParent(parent, false);
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0f, 0.5f);
        bar.sizeDelta = new Vector2(100f, 9.6f);
        var image = bar.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2.4f;
        image.color = color;
        image.raycastTarget = false;
        return bar;
    }

    private void Colour()
    {
        var image = progress ? progress.GetComponent<UnityEngine.UI.Image>() : null;
        if (image && dressed) image.color = isPlayer ? new Color(0.29f, 0.73f, 0.33f) : new Color(0.8f, 0.17f, 0.13f);
    }

    // Update is called once per frame
    void Update()
    {
        if (!targetCamera && Camera.main)
        {
            this.targetCamera = Camera.main.transform;
        }

        if (GameCache.IsAwaitingGameRestore) return;
        if (Target == null || !((MonoBehaviour)Target))
        {
            if (Manager)
            {
                Manager.Remove(this);
            }
            Destroy(gameObject);
            return;
        }

        //transform.position = Target.Transform.position + (Vector3.up * 2f);

        UpdateHealth();
    }

    // This is being called whenever health is actually changed.
    public void UpdateHealth()
    {
        if (Target == null)
        {
            return;
        }

        var stats = Target.GetStats();
        if (stats.IsDead || !Target.InCombat)
        {
            this.SetActive(false);

            if (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha = 0f;

                return;
            }
        }
        else
        {
            this.SetActive(true);
            if (canvasGroup.alpha <= 0f)
            {
                canvasGroup.alpha = 1f;
            }
        }


        if (!targetCamera)
        {
            return;
        }

        if (hasTarget)
        {
            float proc = stats.Health.CurrentValue > 0 ? 100f * ((float)stats.Health.CurrentValue / stats.Health.Level) : 0f;
            this.transform.position = targetTransform.position + targetOffset;
            this.transform.rotation = targetCamera.rotation;
            progress.sizeDelta = new Vector2(proc, progress.sizeDelta.y);

            // the pale trail waits a moment after a hit, then slides down to the health left, so each hit shows its bite
            if (trail)
            {
                var shown = trail.sizeDelta.x;
                if (proc >= shown) { shown = proc; trailWait = 0f; }
                else if ((trailWait += Time.deltaTime) > TrailDelay) shown = Mathf.MoveTowards(shown, proc, TrailSpeed * Time.deltaTime);
                trail.sizeDelta = new Vector2(shown, trail.sizeDelta.y);
            }
        }
    }

    /// <summary>How wide the bar is in the world, metres.</summary>
    private const float Width = 1.25f;
    private const float TrailDelay = 0.35f;
    private bool dressed;
    private const float TrailSpeed = 60f;
    private RectTransform trail;
    private float trailWait;
    private void SetActive(bool value)
    {
        if (this.isActive != value)
        {
            this.isActive = value;
            this.gameObject.SetActive(value);
        }
    }


    internal void SetTarget(IAttackable target)
    {
        Target = target;

        SetActive(true);

        var behaviour = (MonoBehaviour)target;
        this.capsuleCollider = behaviour.GetComponent<CapsuleCollider>();
        this.dungeonBoss = behaviour.GetComponent<DungeonBossController>();
        this.player = behaviour.GetComponent<PlayerController>();
        this.isPlayer = !!player;
        this.isDungeonBoss = !!dungeonBoss;
        Colour();

        this.targetTransform = behaviour.transform;
        this.hasTarget = false;
        if (this.targetTransform)
        {
            Vector3 pos;
            this.hasTarget = true;
            if (isPlayer && capsuleCollider)
            {
                pos = (Vector3.up * (capsuleCollider.height * targetTransform.localScale.y))
                    + (Vector3.up * YOffset);
            }
            else
            {
                pos = (Vector3.up * 2f) + (Target.HealthBarOffset * Vector3.up);
                if (isDungeonBoss)
                {
                    pos += Vector3.up * dungeonBoss.transform.localScale.x * 1.125f;
                }
            }

            this.targetOffset = pos;
        }


        if (canvasGroup && canvasGroup.alpha != 1f)
            canvasGroup.alpha = 1f;
    }
}
