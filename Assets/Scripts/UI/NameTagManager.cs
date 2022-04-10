using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class NameTagManager : MonoBehaviour
{
    //private readonly object mutex = new object();
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
        //lock (mutex)
        {
            var nameTag = nameTags.FirstOrDefault(x => x.TargetPlayer == player);
            if (nameTag)
            {
                Destroy(nameTag.gameObject);
                nameTags.Remove(nameTag);
            }
        }
    }

    public void Remove(NameTag nameTag)
    {
        if (nameTag)
        {
            Destroy(nameTag.gameObject);
            nameTags.Remove(nameTag);
        }
    }

    public void Remove(Transform transform)
    {
        //lock (mutex)
        {
            var nameTag = nameTags.FirstOrDefault(x => x.TargetTransform.GetInstanceID() == transform.GetInstanceID());
            if (nameTag)
            {
                Destroy(nameTag.gameObject);
                nameTags.Remove(nameTag);
            }
        }
    }

    public NameTag Add(Transform transform)
    {
        if (!transform) return null;
        if (!nameTagPrefab) return null;
        var targetName = transform.name;
        //lock (mutex)
        //{
        //    if (nameTags.Any(x => x.TargetTransform.GetInstanceID() == transform.GetInstanceID()))
        //    {
        //        Shinobytes.Debug.LogWarning($"{targetName} already have an assigned name tag.");
        //        return null;
        //    }
        //}
        return AddNameTag(null, targetName, transform);
    }

    public NameTag Add(PlayerController player)
    {
        if (!player) return null;
        if (!nameTagPrefab) return null;

        //lock (mutex)
        {
            if (nameTags.Any(x => x.TargetPlayer == player))
            {
                Shinobytes.Debug.LogWarning($"{player.PlayerName} already have an assigned name tag.");
                return null;
            }
        }

        var targetName = player.PlayerName;
        var targetTransform = player.Transform;

        return AddNameTag(player, targetName, targetTransform);
    }

    private NameTag AddNameTag(PlayerController player, string targetName, Transform targetTransform)
    {
        var obj = Instantiate(nameTagPrefab, transform);
        if (!obj)
        {
            Shinobytes.Debug.LogError($"Failed to add a nametag for {targetName}");
            return null;
        }

        var nameTag = obj.GetComponent<NameTag>();
        nameTag.TargetPlayer = player;
        nameTag.TargetTransform = targetTransform;
        nameTag.Manager = this;
        nameTag.HasTargetPlayer = true;

        //lock (mutex)
        {
            nameTags.Add(nameTag);
        }

        return nameTag;
    }
}
