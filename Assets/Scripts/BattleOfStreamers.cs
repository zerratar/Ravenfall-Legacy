using Shinobytes.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
public class BattleOfStreamers : MonoBehaviour
{
    [SerializeField] private Transform centerOfRing;
    [SerializeField] private float attackRange = 1.25f;
    [SerializeField] private float nameTagOffsetYDelta = 1f;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameManager gameManager;

    [SerializeField] private TMPro.TextMeshPro fightLabel;
    [SerializeField] private TMPro.TextMeshPro resetLabel;
    [SerializeField] private TMPro.TextMeshPro nextLabel;

    [SerializeField] private GameObject defendersWinUI;
    [SerializeField] private GameObject challengersWinUI;

    private List<string> availableJudges = new List<string>();

    private AICombatantController[] combatants;
    private AICombatantController judge;

    private List<AICombatantController> defenders;
    private List<AICombatantController> challengers;
    private string fightText;
    private bool judgeInitialized;

    private BattleOfStreamersState state;

    // Start is called before the first frame update
    void Start()
    {
        fightLabel.enabled = true;
        resetLabel.enabled = false;
        nextLabel.enabled = false;

        defendersWinUI.SetActive(false);
        challengersWinUI.SetActive(false);

        availableJudges.Add("RavenMMO");
        availableJudges.Add("Zerratar");
        availableJudges.Add("CyBeRDoCToR");
        availableJudges.Add("Kohrean");
        availableJudges.Add("Stimpson");
        availableJudges.Add("Thpathpa");
        availableJudges.Add("Vertex101");
        availableJudges.Add("LosCuatroAmigos");
        availableJudges.Add("Madgarou");
        availableJudges.Add("CartiZ");

        AddCombatants();
    }

    private void AddCombatants()
    {
        this.defenders = new List<AICombatantController>();
        this.challengers = new List<AICombatantController>();
        this.combatants = GetComponentsInChildren<AICombatantController>();
        for (var i = 0; i < this.combatants.Length; i++)
        {
            var combatant = this.combatants[i];
            if (combatant.Role == AICombatantRole.Judge)
            {
                this.judge = combatant;
                this.judge.name = availableJudges.Random();
            }

            combatant.SetRole(combatant.name, combatant.Role);

            if (combatant.Role == AICombatantRole.Defender)
            {
                this.defenders.Add(combatant);
            }

            if (combatant.Role == AICombatantRole.Challenger)
            {
                this.challengers.Add(combatant);
            }
        }
    }

    public void ToggleBattle()
    {
        switch (state)
        {
            case BattleOfStreamersState.NotStarted:
                StartBattle();
                break;
            case BattleOfStreamersState.Started:
                EndBattle();
                break;
            case BattleOfStreamersState.Finished:
                NextBattle();
                break;
        }
    }


    private void SelectNewCombatants()
    {
        // should we assign new combatants based on the players in the game?
        var players = playerManager.GetAllRealPlayers().ToList(); // make a copy
        if (players.Count < 2)
        {
            return;
        }

        foreach (var combatant in this.combatants)
        {
            if (combatant.Role == AICombatantRole.Judge)
            {
                continue;
            }
            
            combatant.Reset();

            var player = players.Random();
            combatant.SetRole(player.Name, combatant.Role);
            players.Remove(player);
        }
    }

    private void NextBattle()
    {
        SelectNewCombatants();
        defendersWinUI.SetActive(false);
        challengersWinUI.SetActive(false);
        fightLabel.enabled = true;
        nextLabel.enabled = false;
        resetLabel.enabled = false;
        state = BattleOfStreamersState.NotStarted;
    }

    private void EndBattle()
    {
        foreach (var combatant in this.combatants)
        {
            combatant.Reset();
        }

        var winner = UnityEngine.Random.Range(0, 2);

        defendersWinUI.SetActive(winner == 0);
        challengersWinUI.SetActive(winner == 1);

        if (winner == 0)
        {
            // defenders won
            defenders.ForEach(x => x.Win());
            challengers.ForEach(x => x.Lose());
        }
        else
        {
            defenders.ForEach(x => x.Lose());
            challengers.ForEach(x => x.Win());
        }

        this.state = BattleOfStreamersState.Finished;

        fightLabel.enabled = false;
        nextLabel.enabled = true;
        resetLabel.enabled = false;
    }
    public void StartBattle()
    {
        foreach (var combatant in this.combatants)
        {
            combatant.Reset();
        }
        fightLabel.enabled = false;
        nextLabel.enabled = false;
        resetLabel.enabled = true;
        state = BattleOfStreamersState.Started;
    }

    // Update is called once per frame
    void Update()
    {
        if (!judgeInitialized)
        {
            if (!gameManager || gameManager.RavenNest == null || !gameManager.RavenNest.SessionStarted)
            {
                return;
            }

            judgeInitialized = true;
            judge.SetRole(gameManager.RavenNest.TwitchUserName, AICombatantRole.Judge);
        }

        if (this.state != BattleOfStreamersState.Started)
        {
            return;
        }

        // simple AI logic
        // if the combatants are not close enough to eachother to fight. Walk towards the center of the ring.
        // but stop if close enough to the other combatant

        for (var i = 0; i < this.combatants.Length; i++)
        {
            var combatant = this.combatants[i];
            if (combatant.Role == AICombatantRole.Judge)
            {
                continue;
            }

            //// since name tags can overlap when players are to close to eachother
            //// then move the name tag up in Y axis to avoid overlap
            //// in the order of distance to the center, keep the closest player's name tag at the bottom
            //combatant.SetNameTagYOffset(i);

            var target = combatant.Role == AICombatantRole.Defender ? this.challengers : this.defenders;
            var closest = target.MinBy(x => Vector3.Distance(x.transform.position, combatant.transform.position));
            var distanceToTarget = Vector3.Distance(closest.transform.position, combatant.transform.position);

            // check if we are close enough to attack
            if (distanceToTarget > attackRange)
            {
                combatant.MoveTowards(centerOfRing.position);
            }
            else
            {
                combatant.SetNameTagYOffset(i * nameTagOffsetYDelta);
                combatant.StopMoving();
                combatant.Attack(closest);
            }
        }
    }
}

public enum BattleOfStreamersState
{
    NotStarted,
    Started,
    Finished
}
