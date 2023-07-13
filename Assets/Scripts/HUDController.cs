using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameObject all2d;
    [SerializeField] private GameObject playerList;
    [SerializeField] private Canvas GameUICanvas;
    [SerializeField] private GameManager gameManager;

    private UIState state;

    private void Awake()
    {
        //GameUICanvas.worldCamera = Camera.main;
        //GameUICanvas.renderMode = RenderMode.ScreenSpaceCamera;
        var playerListVisible = PlayerSettings.Instance.PlayerListVisible.GetValueOrDefault();
        if (!playerListVisible)
        {
            SetState(UIState.NoPlayerList);
        }
    }

    private void SetState(UIState newState)
    {
        state = newState;

        var wasPlayerListVisible = PlayerSettings.Instance.PlayerListVisible;
        var playerListVisible = true;
        if (state == UIState.Everything)
        {
            all2d.SetActive(true);
            playerList.SetActive(true);
        }
        else if (state == UIState.NoPlayerList)
        {
            playerList.SetActive(false);
            playerListVisible = false;
        }
        else
        {
            all2d.SetActive(false);
            playerList.SetActive(false);
            playerListVisible = false;
        }

        if (wasPlayerListVisible != playerListVisible)
        {
            PlayerSettings.Instance.PlayerListVisible = playerListVisible;
            PlayerSettings.Save();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetState((UIState)(((int)state + 1) % 3));
        }
    }

    public enum UIState
    {
        Everything,
        NoPlayerList,
        Nothing
    }
}

