using System.Linq;
using UnityEngine;

public class ClanSkillObserver : MonoBehaviour
{
    [SerializeField] private PlayerController observedPlayer;
    [SerializeField] private StatObserver enchanting;
    [SerializeField] private GameManager gameManager;

    public void Start()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
    }

    public void Observe(PlayerController player)
    {
        observedPlayer = player;
        enchanting.gameObject.SetActive(false);

        // Hide the skill if its not avilable.
        if (!player || !player.Clan.InClan)
            return;

        if (!gameManager)
            return;

        var skills = gameManager.Clans.GetClanSkills(player.Clan.ClanInfo.Id);
        if (skills != null && skills.Count > 0)
        {
            // TODO: fix so we can just loop all skills and add them dynamically.
            var enchantingSkill = skills.FirstOrDefault(x => x.Name.IndexOf("enchant", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (enchantingSkill != null)
            {
                enchanting.gameObject.SetActive(true);
                enchanting.Observe(enchantingSkill);
            }
        }
    }
}
