using Shinobytes.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerList : MonoBehaviour
{
    [SerializeField] private float rowHeight = 70f;
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private GameObject playerListItem; // prefabs
    [SerializeField] private GameObject listRoot;

    private readonly List<PlayerController> trackedPlayers = new List<PlayerController>();
    private readonly List<PlayerListItem> instantiatedPlayerListItems = new List<PlayerListItem>();

    private ExpProgressHelpStates expHelpState;

    private GameCamera gameCamera;
    private float startPos;

    private RectTransform rectTransform;
    private RectTransform viewportTransform;
    private RectTransform listRootRectTransform;
    private float switchTimer = 1f;
    private List<PlayerController> filteredPlayers;

    public float Scale
    {
        get
        {
            return listRoot.transform.localScale.x;
        }
        set
        {
            listRoot.transform.localScale = new Vector3(value, value, 1);
        }
    }

    public float Bottom
    {
        get
        {
            if (!EnsureRectTransform()) return 0;
            return listRootRectTransform.rect.yMin;
        }
        set
        {
            if (!EnsureRectTransform()) return;
            listRootRectTransform.SetBottom(value);
        }
    }

    public float Height
    {
        get
        {
            if (!EnsureRectTransform()) return 0;
            return rectTransform.rect.height;
        }
    }

    public float ContainerHeight
    {
        get
        {
            if (!EnsureRectTransform()) return 0;
            return viewportTransform.rect.height;
        }
    }

    public int MaxVisibleCount
    {
        get
        {
            return Mathf.FloorToInt(ContainerHeight / rowHeight) + 1;
        }
    }

    private List<PlayerController> DataSource => filteredPlayers != null ? filteredPlayers : trackedPlayers;

    // Start is called before the first frame update
    void Start()
    {
        EnsureRectTransform();
        gameCamera = GameObject.FindObjectOfType<GameCamera>();
        startPos = rectTransform.anchoredPosition.y;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateScroll();
    }

    public void UpdateScroll()
    {
        EnsureRectTransform();

        var items = DataSource;
        if (items.Count == 0)
        {
            return;
        }

        var maxVisibleCount = MaxVisibleCount;
        if (instantiatedPlayerListItems.Count < maxVisibleCount)
        {
            return;
        }

        var playerCount = items.Count / 10f;
        var speed = playerCount * scrollSpeed * Time.deltaTime;

        // update Content (this) position
        float newPos = rectTransform.anchoredPosition.y + speed;
        if (newPos >= startPos + rowHeight)
        {
            // First item is no longer visible
            var first = items[0];
            items.RemoveAt(0);
            items.Add(first);
            RefreshVisible();

            // Reset Content position
            newPos = startPos;
        }

        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newPos);
    }


    public void AddPlayer(PlayerController player)
    {
        if (!player)
        {
            Shinobytes.Debug.LogWarning("Player already exists in the list?");
            return;
        }

        if (trackedPlayers.Contains(player))
        {
            return;
        }

        if (playerListItem == null)
        {
            Shinobytes.Debug.LogError("No PlayerList Item Prefab set.");
            return;
        }

        trackedPlayers.Add(player);

        var maxVisibleCount = MaxVisibleCount;
        if (instantiatedPlayerListItems.Count < maxVisibleCount)
        {
            var item = Instantiate(playerListItem, transform);
            var listItem = item.GetComponent<PlayerListItem>();

            listItem.List = this;
            listItem.ExpProgressHelpState = expHelpState;
            listItem.UpdatePlayerInfo(player, gameCamera, trackedPlayers.Count - 1);
            listItem.gameObject.SetActive(filteredPlayers == null);
            instantiatedPlayerListItems.Add(listItem);
        }
    }

    public void RemovePlayer(PlayerController player)
    {
        if (!player)
        {
            return;
        }

        trackedPlayers.Remove(player);

        var maxVisibleCount = MaxVisibleCount;
        if (trackedPlayers.Count < maxVisibleCount)
        {
            var li = instantiatedPlayerListItems.FirstOrDefault(x => x.TargetPlayer && x.TargetPlayer.PlayerName == player.PlayerName);
            if (li)
            {
                instantiatedPlayerListItems.Remove(li);
                Destroy(li.gameObject);
            }
        }

        RebuildIndices();
    }

    private void RefreshVisible()
    {
        var items = DataSource;
        for (var i = 0; i < instantiatedPlayerListItems.Count; ++i)
        {
            var item = instantiatedPlayerListItems[i];
            if (i < items.Count)
            {
                item.ExpPerHourUpdate = 0f;
                item.ExpProgressHelpState = this.expHelpState;
                item.UpdatePlayerInfo(items[i], gameCamera, i);
                item.gameObject.SetActive(true);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private void RebuildIndices()
    {
        var items = DataSource;
        if (items.Count == 0)
        {
            return;
        }

        for (var i = 0; i < instantiatedPlayerListItems.Count; i++)
        {
            var item = instantiatedPlayerListItems[i];
            item.ItemIndex = trackedPlayers.FindIndex(x => x.Id == item.TargetPlayer.Id);
        }
    }

    public void ClearFocus()
    {
        this.filteredPlayers = null;
        RefreshVisible();
        RebuildIndices();
    }

    public void FocusOnPlayers(IReadOnlyList<PlayerController> players)
    {
        this.filteredPlayers = players.AsList();
        RefreshVisible();
        RebuildIndices();
    }

    public void ToggleExpRate()
    {
        expHelpState = (ExpProgressHelpStates)((((int)expHelpState) + 1) % 4);
        foreach (var item in instantiatedPlayerListItems)
        {
            item.ExpProgressHelpState = expHelpState;
            item.ExpPerHourUpdate = 0; // this will trigger the UI to update right away.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EnsureRectTransform()
    {
        if (!rectTransform) rectTransform = transform as RectTransform;
        if (!viewportTransform) viewportTransform = transform.parent as RectTransform;
        if (!listRootRectTransform) listRootRectTransform = listRoot.transform as RectTransform;
        return listRootRectTransform;
    }
}
