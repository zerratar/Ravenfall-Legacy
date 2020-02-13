using TMPro;
using UnityEngine;

public class DamageCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelBack;
    [SerializeField] private TextMeshProUGUI labelFront;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float FadeoutOffsetY = 0f;
    [SerializeField] private UnityEngine.UI.Image background;

    private int damage = 0;
    private float fadeoutTimer = 2f;

    public float OffsetY = 2.75f;
    public string Color;
    public float TargetFadeoutOffsetY = 3f;
    public float FadeoutDuration = 2f;
    public Transform Target;
    public DamageCounterManager Manager { get; internal set; }

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
    }

    // Update is called once per frame
    void Update()
    {
        if (!Target)
        {
            return;
        }

        if (fadeoutTimer <= 0f)
        {
            return;
        }

        transform.LookAt(Camera.main.transform);

        fadeoutTimer -= Time.deltaTime;

        if (fadeoutTimer <= 0f)
        {
            fadeoutTimer = FadeoutDuration;
            FadeoutOffsetY = 0;
            canvasGroup.alpha = 0;
            Manager.Return(this);
            return;
        }

        FadeOut();
    }

    public void Activate(Transform target, int damage)
    {
        Target = target;

        transform.position = Target.position + (Vector3.up * (OffsetY + FadeoutOffsetY));
        fadeoutTimer = FadeoutDuration;

        Damage = damage;

        canvasGroup.alpha = 1;

        gameObject.SetActive(true);
    }

    private void UpdateDamageText()
    {
        labelBack.text = Damage.ToString();
        labelFront.text = Damage.ToString();
    }

    private void FadeOut()
    {
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

        if (background)
        {
            var color = NameTag.GetColorFromHex(Color);
            background.color = color;
        }
    }
}
