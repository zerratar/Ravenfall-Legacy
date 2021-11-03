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
    private float fadeoutTimer = 2f;
    private Camera mainCamera;
    public float OffsetY = 2.75f;

    //public string Color;

    public float TargetFadeoutOffsetY = 3f;
    public float FadeoutDuration = 2f;
    public Transform Target;
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
        fadeoutTimer = FadeoutDuration;
        this.mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        if (this.transform.position.y - 0.0001 <= OutOfScreen.y)
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
            transform.LookAt(mainCamera.transform);
        }
        FadeOut();
    }

    private void ReturnToPool()
    {
        this.transform.position = OutOfScreen;
        Manager.Return(this);
    }

    public void Activate(Transform target, int damage, bool isHeal)
    {
        Target = target;

        mainCamera = Camera.main;
        transform.position = Target.position + (Vector3.up * (OffsetY + FadeoutOffsetY));
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
        fadeoutTimer -= Time.deltaTime;

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
            transform.position = Target.position + (Vector3.up * (OffsetY + FadeoutOffsetY));
        }

        //if (background)
        //{
        //    var color = NameTag.GetColorFromHex(Color);
        //    background.color = color;
        //}
    }
}
