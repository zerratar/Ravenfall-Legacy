using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class PlayerDetails : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController observedPlayer;
    [SerializeField] private CombatSkillObserver combatSkillObserver;
    [SerializeField] private SkillStatsObserver skillStatsObserver;
    [SerializeField] private ResourcesObserver resourcesObserver;
    [SerializeField] private TextMeshProUGUI lblObserving;
    [SerializeField] private TextMeshProUGUI lblPlayername;
    [SerializeField] private TextMeshProUGUI lblPlayerlevel;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private GameObject subscriberBadge;
    [SerializeField] private GameObject vipBadge;
    [SerializeField] private GameObject modBadge;
    [SerializeField] private GameObject broadcasterBadge;

    private float observedPlayerTimeout;

    // Start is called before the first frame update
    void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!lblObserving || !lblPlayername || !observedPlayer || !canvasGroup)
        {
            if (canvasGroup.alpha != 0)
            {
                canvasGroup.alpha = 0;
            }

            return;
        }

        if (canvasGroup.alpha != 1)
        {
            canvasGroup.alpha = 1;
        }

        SetActive(subscriberBadge, observedPlayer.IsSubscriber);
        SetActive(vipBadge, observedPlayer.IsVip);
        SetActive(modBadge, observedPlayer.IsModerator);
        SetActive(broadcasterBadge, observedPlayer.IsBroadcaster);

        observedPlayerTimeout -= Time.deltaTime;
        SetText(lblObserving, $"Currently observing ({Mathf.FloorToInt(observedPlayerTimeout) + 1})");
        SetText(lblPlayerlevel, $"LV : <b>{observedPlayer.Stats.CombatLevel}");
        SetText(lblPlayername, $"{observedPlayer.PlayerName}");

        if (observedPlayerTimeout < 0)
        {
            gameManager.Camera.ObserveNextPlayer();
        }
    }

    public void Observe(PlayerController player, float timeout)
    {
        observedPlayerTimeout = timeout;
        observedPlayer = player;
        resourcesObserver.Observe(player);
        combatSkillObserver.Observe(player);
        skillStatsObserver.Observe(player);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetText(TextMeshProUGUI obj, string value)
    {
        if (!obj) return;
        if (obj.text != value)
            obj.text = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetActive(GameObject obj, bool value)
    {
        if (!obj) return;
        if (obj.activeSelf != value)
            obj.SetActive(value);
    }

}
