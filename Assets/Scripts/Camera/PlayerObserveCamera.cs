using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class PlayerObserveCamera : MonoBehaviour
{
    public bool CanControlPlayer;

    [SerializeField] private float positionOffsetY = 5f;
    [SerializeField] private float viewOffsetY = 5f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private RawImage image;

    private PlayerController player;
    private int playerObserveLayer;


    public bool HasTarget => player != null;

    // Update is called once per frame
    void Update()
    {
        if (!player)
        {
            if (image) image.gameObject.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        if (CanControlPlayer && Input.GetKeyUp(KeyCode.F3))
        {
            player.ToggleControl();
            if (player.Controlled)
            {
                Shinobytes.Debug.Log(player.Name + " is now being controlled.");
            }
            else
            {
                Shinobytes.Debug.Log(player.Name + " is no longer being controlled.");
            }
        }

    }

    public void LateUpdate()
    {
        if (!player)
        {
            return;
        }

        if (player.Controlled)
        {
            return;
        }

        var t = player.transform;
        var playerScale = t.localScale.y;
        //var position = player.PositionInternal + t.forward * (playerScale * distance);
        var position = t.position + t.forward * (playerScale * distance);

        transform.position = position + positionOffsetY * Vector3.up;
        transform.LookAt(t.position + new Vector3(0, viewOffsetY, 0));
    }

    public void ObservePlayer(PlayerController player)
    {
        if (this.player)
        {
            this.player.RevokeControl();
            ResetPlayerLayer();
        }

        this.player = player;
        if (!this.player)
        {
            return;
        }

        if (image) image.gameObject.SetActive(true);
        gameObject.SetActive(true);
        UpdatePlayerLayer();
    }
    public void UpdatePlayerLayer()
    {
        if (!player) return;
        if (playerObserveLayer <= 0)
            playerObserveLayer = LayerMask.NameToLayer("PlayerObserve");
        SetLayerRecursive(player.gameObject, playerObserveLayer);
    }

    private void ResetPlayerLayer()
    {
        if (!player) return;
        SetLayerRecursive(player.gameObject, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        for (var i = 0; i < go.transform.childCount; ++i)
        {
            SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
