using Assets.Scripts;
using TMPro;
using UnityEngine;

public class DamageCounter : MonoBehaviour
{
    private static readonly Vector3 OutOfScreen = new Vector3(0, -999999, 0);

    [SerializeField] private TextMeshProUGUI labelBack;
    [SerializeField] private TextMeshProUGUI labelFront;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float FadeoutOffsetY = 0f;
    [SerializeField] private UnityEngine.UI.Image background;

    [SerializeField] private Color damageColor;
    [SerializeField] private Color healColor;
    [SerializeField] private Color noChangeColor;

    private int damage = 0;
    private Transform _transform;
    private float fadeoutTimer = 2f;
    private Camera mainCamera;
    private Vector3 targetPosition;
    private Vector3 currentPosition;
    public float OffsetY = 2.75f;

    //public string Color;

    public float TargetFadeoutOffsetY = 3f;
    public float FadeoutDuration = 2f;
    public Transform Target;
    private bool inUse;

    public DamageCounterManager Manager { get; internal set; }
    public float FadeOutProgress => (FadeoutDuration - fadeoutTimer) / FadeoutDuration;
    public int Damage
    {
        get
        {
            return damage;
        }
        set
        {
            damage = value;
            UpdateDamageText();
        }
    }

    void Start()
    {
        this._transform = transform;

        fadeoutTimer = FadeoutDuration;
        this.mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (!inUse || GameCache.IsAwaitingGameRestore) return;

        if (this.currentPosition.y - 0.0001 <= OutOfScreen.y)
        {
            return;
        }

        if (!Target || fadeoutTimer <= 0f)
        {
            ReturnToPool();
            return;
        }

        if (mainCamera)
        {
            _transform.LookAt(mainCamera.transform);
        }
        FadeOut();
    }

    private void ReturnToPool()
    {
        _transform.position = OutOfScreen;
        currentPosition = OutOfScreen;
        inUse = false;
        Target = null;
        Manager.Return(this);
    }

    public void Activate(Transform target, int damage, bool isHeal)
    {
        inUse = true;
        this._transform = transform;

        Target = target;
        mainCamera = Camera.main;

        targetPosition = Target.position;
        currentPosition = targetPosition + (Vector3.up * (OffsetY + FadeoutOffsetY));
        _transform.position = currentPosition;

        fadeoutTimer = FadeoutDuration;

        canvasGroup.alpha = 1;

        UpdateBackgroundColor(isHeal);

        Damage = System.Math.Abs(damage);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public void UpdateBackgroundColor(bool isHeal)
    {
        if (isHeal)
        {
            background.color = healColor;
        }
        else
        {
            if (damage == 0)
            {
                background.color = noChangeColor;
            }
            else
            {

                background.color = damageColor;
            }
        }
    }

    private void UpdateDamageText()
    {
        mainCamera = Camera.main;
        labelBack.text = Damage.ToString();
        labelFront.text = Damage.ToString();
    }

    private void FadeOut()
    {
        fadeoutTimer -= GameTime.deltaTime;

        if (fadeoutTimer <= 0f)
        {
            fadeoutTimer = FadeoutDuration;
            FadeoutOffsetY = 0;
            if (canvasGroup.alpha > 0)
                canvasGroup.alpha = 0;
            ReturnToPool();
            return;
        }

        var fadeoutProgress = fadeoutTimer / FadeoutDuration;
        var proc = 1f - fadeoutProgress;

        if (fadeoutTimer <= FadeoutDuration / 2f)
        {
            canvasGroup.alpha = fadeoutTimer / (FadeoutDuration / 2f);
        }

        FadeoutOffsetY = Mathf.Lerp(0, TargetFadeoutOffsetY, proc);

        if (Target)
        {
            currentPosition = Target.position + (Vector3.up * (OffsetY + FadeoutOffsetY));
            _transform.position = currentPosition;
        }

        //if (background)
        //{
        //    var color = NameTag.GetColorFromHex(Color);
        //    background.color = color;
        //}
    }
}
