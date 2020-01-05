using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class PlayerObserveCamera : MonoBehaviour
{
    [SerializeField] private float positionOffsetY = 5f;
    [SerializeField] private float viewOffsetY = 5f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private RawImage image;

    private PlayerController player;
    private int playerObserveLayer;

    // Update is called once per frame
    void Update()
    {
        if (!player)
        {
            if (image) image.gameObject.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        var position = player.transform.position + player.transform.forward * distance;
        transform.position = position + positionOffsetY * Vector3.up;
        transform.LookAt(player.transform.position + new Vector3(0, viewOffsetY, 0));
    }

    public void ObservePlayer(PlayerController player)
    {
        if (this.player) ResetPlayerLayer();
        if (image) image.gameObject.SetActive(true);
        gameObject.SetActive(true);
        this.player = player;        
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
