using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class NameTagManager : MonoBehaviour
{
    private readonly object mutex = new object();
    private readonly List<NameTag> nameTags = new List<NameTag>();

    [SerializeField] private GameObject nameTagPrefab;
    [SerializeField] private PlayerLogoManager logoManager;

    public PlayerLogoManager LogoManager => logoManager;

    private void Awake()
    {
        if (!logoManager) logoManager = FindObjectOfType<PlayerLogoManager>();
    }

    public void Remove(PlayerController player)
    {
        lock (mutex)
        {
            var nameTag = nameTags.FirstOrDefault(x => x.Target == player);
            if (nameTag)
            {
                Destroy(nameTag.gameObject);
                nameTags.Remove(nameTag);
            }
        }
    }

    public void Add(PlayerController player)
    {
        if (!player) return;
        if (!nameTagPrefab) return;

        lock (mutex)
        {
            if (nameTags.Any(x => x.Target == player))
            {
                Debug.LogWarning($"{player.PlayerName} already have an assigned name tag.");
                return;
            }
        }

        var obj = Instantiate(nameTagPrefab, transform);
        if (!obj)
        {
            Debug.LogError($"Failed to add a nametag for {player.PlayerName}");
            return;
        }

        var nameTag = obj.GetComponent<NameTag>();
        nameTag.Target = player;
        nameTag.Manager = this;

        //if (player.Raider != null)
        //{
        //    nameTag.RaiderLogo = logoManager.GetLogo(player.Raider.RaiderUserId);
        //}

        lock (mutex)
        {
            nameTags.Add(nameTag);
        }
    }

}
