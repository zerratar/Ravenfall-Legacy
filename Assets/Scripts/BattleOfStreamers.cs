using System.Collections.Generic;
using UnityEngine;
using Shinobytes.Linq;
public class BattleOfStreamers : MonoBehaviour
{
    private AICombatantController[] combatants;
    private AICombatantController judge;

    private List<AICombatantController> defenders;
    private List<AICombatantController> challengers;

    // Start is called before the first frame update
    void Start()
    {
        SetupCombatants();
    }

    private void SetupCombatants()
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
            }

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

    // Update is called once per frame
    void Update()
    {

    }
}
