﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class PlayerList : MonoBehaviour
{
    [SerializeField] private GameObject observedPlayerListItem; // prefabs
    [SerializeField] private GameObject playerListItem; // prefabs
    //[SerializeField] private RectTransform contentPanel; // prefabs
    [SerializeField] private GameObject scrollRect; // prefabs
    [SerializeField] private float stickyTime = 2f;

    private readonly List<PlayerListItem> instantiatedPlayerListItems
        = new List<PlayerListItem>();

    //private readonly object mutex = new object();

    private ExpProgressHelpStates expHelpState;

    private float stickyTimer = 0f;

    private ScrollRect scroll;
    private RectTransform scrollRectTransform;
    private GameCamera gameCamera;
    private float scrollPosition = 0f;
    private float scrollSpeed = 0.1f;

    public float Scale
    {
        get
        {
            return scrollRect.transform.localScale.x;
        }
        set
        {
            scrollRect.transform.localScale = new Vector3(value, value, 1);
        }
    }

    public float Bottom
    {
        get
        {
            if (!EnsureRectTransform()) return 0;
            return scrollRectTransform.rect.yMin;
        }
        set
        {
            if (!EnsureRectTransform()) return;
            scrollRectTransform.SetBottom(value);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        EnsureRectTransform();
        gameCamera = GameObject.FindObjectOfType<GameCamera>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateScroll();
    }

    public void UpdateScroll()
    {
        EnsureRectTransform();
        if (scroll != null)
        {
            if (stickyTimer > 0f)
            {
                stickyTimer -= GameTime.deltaTime;
                return;
            }

            var speed = scrollSpeed * GameTime.deltaTime;
            speed /= instantiatedPlayerListItems.Count * 0.25f;
            scrollPosition += speed;
            scrollPosition = Math.Min(1f, scrollPosition);
            scrollPosition = Math.Max(0f, scrollPosition);
            scroll.normalizedPosition = new Vector2(0, scrollPosition);

            if ((scrollPosition >= 1f && scrollSpeed > 0f) || (scrollPosition <= 0f && scrollSpeed < 0))
            {
                stickyTimer = stickyTime;
                scrollSpeed *= -1f;
            }
        }
    }


    public void AddPlayer(PlayerController player)
    {
        if (!player)
        {
            Shinobytes.Debug.LogWarning("Player already exists in the list?");
            return;
        }

        for (var i = 0; i < instantiatedPlayerListItems.Count; ++i)
        {
            var x = instantiatedPlayerListItems[i];
            if (x.TargetPlayer && x.TargetPlayer.PlayerName == player.PlayerName)
            {
                Shinobytes.Debug.LogWarning("Unable to add Player " + player.Name + " to the player list. It is already in there :o");
                return;
            }
        }

        if (playerListItem == null)
        {
            Shinobytes.Debug.LogError("No PlayerList Item Prefab set.");
            return;
        }

        var item = Instantiate(playerListItem, transform);
        var listItem = item.GetComponent<PlayerListItem>();
        listItem.List = this;
        listItem.ExpProgressHelpState = expHelpState;
        listItem.UpdatePlayerInfo(player, gameCamera);
        instantiatedPlayerListItems.Add(listItem);
    }

    public void RemovePlayer(PlayerController player)
    {
        if (!player)
        {
            return;
        }

        //lock (mutex)
        {
            var li = instantiatedPlayerListItems.FirstOrDefault(x =>
            x.TargetPlayer && x.TargetPlayer.PlayerName == player.PlayerName);

            if (li)
            {
                Destroy(li.gameObject);
                instantiatedPlayerListItems.Remove(li);
            }
        }
    }

    public void Remove(PlayerListItem item)
    {
        try
        {
            if (item != null)
            {
                instantiatedPlayerListItems.Remove(item);
                Destroy(item.gameObject);
            }
        }
        catch { }
    }

    public void ClearFocus()
    {
        //lock (mutex)
        {
            foreach (var item in instantiatedPlayerListItems)
            {
                var cg = item.GetComponent<CanvasGroup>();
                if (!cg) cg = item.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
            }
        }
    }

    public void FocusOnPlayers(IReadOnlyList<PlayerController> players)
    {
        //lock (mutex)
        {
            var tintOutPlayers = instantiatedPlayerListItems.Except(
            players.Select(x => instantiatedPlayerListItems.FirstOrDefault(y => y.TargetPlayer == x)));

            foreach (var item in tintOutPlayers)
            {
                var cg = item.GetComponent<CanvasGroup>();
                if (!cg) cg = item.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.35f;
            }
        }
    }

    public void ToggleExpRate()
    {
        //lock (mutex)
        {
            expHelpState = (ExpProgressHelpStates)((((int)expHelpState) + 1) % 4);
            foreach (var item in instantiatedPlayerListItems)
            {
                item.ExpProgressHelpState = expHelpState;
                item.ExpPerHourUpdate = 0; // this will trigger the UI to update right away.
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EnsureRectTransform()
    {
        if (!scrollRect) return false;
        if (!scroll) scroll = scrollRect.GetComponent<ScrollRect>();
        if (!scroll) return false;
        if (!scrollRectTransform) scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        return scrollRectTransform;
    }
}
