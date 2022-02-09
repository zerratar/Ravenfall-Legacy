using System.Runtime.CompilerServices;
using UnityEngine;

public class OverlayUI : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;

    [Header("UI Setup")]
    [SerializeField] private TMPro.TextMeshProUGUI lblCharacterName;

    [SerializeField] private GameObject trainingContainer;
    [SerializeField] private TMPro.TextMeshProUGUI lblCombatLevel;
    [SerializeField] private TMPro.TextMeshProUGUI lblTraining;
    [SerializeField] private TMPro.TextMeshProUGUI lblSkillLevel;
    [SerializeField] private TMPro.TextMeshProUGUI lblSkillProgress;
    [SerializeField] private TMPro.TextMeshProUGUI lblSkillNextLevel;


    [Header("Debug UI Setup")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TMPro.TextMeshProUGUI lblDebugConnected;
    [SerializeField] private TMPro.TextMeshProUGUI lblDebugLastPacket;



    private OverlayPlayer currentPlayerInfo;
    private PlayerController currentPlayerInstance;
    private Skills currentPlayerStats;
    private bool debugPanelActive;

    // Start is called before the first frame update
    void Start()
    {
        UpdateDebugUI("Disconnected", "No packets received yet.");

        //if (!Application.isEditor)
        //{
        Clear();
        debugPanel.SetActive(false);
        //}

        this.debugPanelActive = debugPanel.activeInHierarchy;
    }

    public void ToggleDebugUI()
    {
        if (debugPanel.activeInHierarchy)
        {
            debugPanel.SetActive(false);
            debugPanelActive = false;
        }
        else
        {
            gameObject.SetActive(true);
            debugPanel.SetActive(true);
            debugPanelActive = true;
        }
    }

    public void UpdateObservedPlayer(OverlayPlayer playerInfo, PlayerController playerInstance)
    {
        if (playerInfo == null)
        {
            Clear();
            return;
        }

        this.currentPlayerInfo = playerInfo;
        this.currentPlayerInstance = playerInstance;
        this.currentPlayerStats = new Skills(playerInfo.Character.Skills);
        this.UpdateLabels();

        this.uiCanvas.enabled = true;
    }

    public void UpdateDebugUI(string connectionStatus, string lastPacketStatus)
    {
        this.lblDebugConnected.text = connectionStatus;
        this.lblDebugLastPacket.text = lastPacketStatus;
    }

    private void UpdateLabels()
    {
        if (this.currentPlayerInfo == null || this.currentPlayerInfo.Character == null)
        {
            ClearLabels();
            return;
        }

        var character = this.currentPlayerInfo.Character;
        var player = this.currentPlayerInstance;
        var stats = this.currentPlayerStats;

        this.lblCharacterName.text = character.Name;
        this.lblCombatLevel.text = stats.CombatLevel.ToString();

        var trainingSkill = player.GetActiveSkillStat();
        if (!string.IsNullOrEmpty(character.State.Task) && trainingSkill != null)
        {
            this.trainingContainer.SetActive(true);
            this.lblTraining.text = trainingSkill.Name;
            this.lblSkillLevel.text = trainingSkill.Level.ToString();
            this.lblSkillProgress.text = ((int)(GetPercentForNextLevel(trainingSkill.Level, trainingSkill.Experience) * 100)) + "%";
        }
        else
        {
            this.trainingContainer.SetActive(false);
        }
    }

    private void ClearLabels()
    {
        this.lblCharacterName.text = "No Player";
        this.lblCombatLevel.text = "-";
        this.lblTraining.text = "";
        this.lblSkillLevel.text = "";
        this.lblSkillProgress.text = "";
        this.trainingContainer.SetActive(false);
    }

    private void Clear()
    {
        ClearLabels();
        //uiCanvas.enabled = false;
        gameObject.SetActive(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetPercentForNextLevel(int level, double exp)
    {
        var nextLevel = GameMath.ExperienceForLevel(level + 1);
        var thisLevel = exp;

        return (float)(thisLevel / nextLevel);
    }
}
