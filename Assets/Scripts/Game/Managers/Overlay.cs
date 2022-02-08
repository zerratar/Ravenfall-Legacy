using UnityEngine;
using Assets.Scripts.Overlay;
using System.Collections.Generic;
using RavenNest.Models;
using System;
using UnityEngine.SceneManagement;

public class Overlay : MonoBehaviour
{
    #region Fields and Properties

    private static bool isGame;

    public static float CharacterRotationSpeed = 12;

    [SerializeField] private Camera renderCamera;
    [SerializeField] private OverlayUI ui;
    [SerializeField] private GameObject playerControllerPrefab;
    [SerializeField] private float rotationSpeedDelta = 5f;
    [SerializeField] private float defaultCharacterRotationSpeed = 12;

    [SerializeField] private Material[] skyboxBackgrounds;

    private OverlayServer server;
    private OverlayClient client;
    private Vector3 renderCameraDefaultPosition;
    private OverlayPacketManager packetManager;
    private GameManager gameManager;
    private PlayerController displayedPlayer;

    [HideInInspector] public OverlayPlayerManager Players;
    public ItemManager ItemManager;

    //private bool skyboxVisible = true;

    private int screenSizeIndex = 0;
    private int[] screenSizes = new int[] { 720, 1080 };
    private int skyboxIndex;

    public static bool IsOverlay => !IsGame;
    public static bool IsGame
    {
        get => isGame || SceneManager.GetActiveScene().buildIndex == 1;
        set
        {
            isGame = value;
        }
    }

    #endregion

    #region Game / Server

    public void SendRedeemables(IReadOnlyList<RavenNest.Models.RedeemableItem> redeemables)
    {
        if (!IsGame)
        {
            Shinobytes.Debug.LogError("Redeemables data can only be sent from the Game.");
            return;
        }

        // Potential bug: We are replacing the whole write queue 
        // when we call this function, this could lead to missing writes
        // if it was being enqueued at the time we sent this.
        // 
        // Note: I do deem it to be very minimal and not a huge problem,
        //  it will bee seen as a hiccup at most.
        server.Send(new OverlayPacket("redeemables", redeemables), true);
    }

    public void SendItems(IReadOnlyList<RavenNest.Models.Item> items)
    {
        if (!IsGame)
        {
            Shinobytes.Debug.LogError("Items data can only be sent from the Game.");
            return;
        }

        server.Send(new OverlayPacket("items", items));
    }

    public void SendObservePlayer(PlayerController player)
    {
        if (!IsGame)
        {
            Shinobytes.Debug.LogError("Observe player data can only be sent from the Game.");
            return;
        }

        this.displayedPlayer = player;

        // this packet is not important if we don't have any clients connected.
        // Ignore it.
        if (!server.HasConnections)
        {
            return;
        }

        if (!player || player == null)
        {
            server.Send(new OverlayPacket("clear", null));
            return;
        }

        var data = new OverlayPlayer(player);
        server.Send(new OverlayPacket(data));
    }
    #endregion

    #region Overlay / Client

    internal void OnItemsLoaded(List<Item> data)
    {
        Shinobytes.Debug.Log(data.Count + " items loaded from the game.");
    }

    internal void OnRedeemablesLoaded(List<RavenNest.Models.RedeemableItem> data)
    {
        Shinobytes.Debug.Log(data.Count + " redeemables loaded from the game.");
    }

    internal void OnClearDisplayedPlayer()
    {
        if (!this.displayedPlayer)
        {
            // just in case.
            this.displayedPlayer = FindObjectOfType<PlayerController>();
        }

        if (!this.displayedPlayer)
        {
            return;
        }

        Destroy(displayedPlayer.gameObject);
        displayedPlayer = null;
    }
    public void OnShowPlayer(OverlayPlayer playerInfo, PlayerController player)
    {
        if (!this.displayedPlayer)
        {
            // just in case.
            this.displayedPlayer = FindObjectOfType<PlayerController>();
        }

        if (displayedPlayer && displayedPlayer.Id != player.Id)
        {
            Destroy(displayedPlayer.gameObject);
        }

        this.displayedPlayer = player;
        // 1. Spawn player object
        // 2. Set player display and invalidate UI
        Shinobytes.Debug.Log("Show Player: " + player.Name);

        this.ui.UpdateObservedPlayer(playerInfo, player);
    }

    internal void OnPlayerUpdated(OverlayPlayer playerInfo, PlayerController player)
    {
        this.displayedPlayer = player;
        // Invalidate UI
        Shinobytes.Debug.Log("Update Player: " + player.Name);

        this.ui.UpdateObservedPlayer(playerInfo, player);
    }

    private void RegisterPacketHandlers()
    {
        this.packetManager.Register<UpdatePlayer>(nameof(OverlayPlayer));
        this.packetManager.Register<ReceivedItems>("items");
        this.packetManager.Register<ReceivedRedeemables>("redeemables");
        this.packetManager.Register<ClearPlayer>("clear");
        this.packetManager.PacketHandled += (sender, packet) =>
        {
            ui.UpdateDebugUI(
                client.IsConnected ? "Connected to the game" : "Disconnected",
                "Last packet received at " + DateTime.Now + "\r\n" + Newtonsoft.Json.JsonConvert.SerializeObject(packet));
        };
    }
    #endregion

    #region Base Logic

    public void Awake()
    {
        this.gameManager = FindObjectOfType<GameManager>();
        this.Players = new OverlayPlayerManager(playerControllerPrefab);
        this.packetManager = new OverlayPacketManager(this);

        // although two way communication is supported, its not really used.
        this.RegisterPacketHandlers();

        if (IsGame)
        {
            AwakeGame();
            return;
        }

        AwakeOverlay();
    }

    private void OnDestroy()
    {
        if (IsGame)
        {
            DestroyGame();
            return;
        }

        DestroyOverlay();
    }

    private void OnApplicationQuit()
    {
        if (IsGame)
        {
            DestroyGame();
            return;
        }

        DestroyOverlay();
    }

    private void Update()
    {
        if (IsOverlay)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleUI();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleSkybox();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                ToggleResolution();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ui.ToggleDebugUI();
            }

            if (client != null)
            {
                if (client.IsDisposed)
                {
                    client = new OverlayClient(packetManager);
                    return;
                }

                client.UpdateAsync();

                var speed = CharacterRotationSpeed;
                var shiftDown = Input.GetKey(KeyCode.LeftShift);
                var valuesUpdated = false;
                if (Input.GetKeyDown(KeyCode.Delete))
                {
                    renderCamera.transform.position = renderCameraDefaultPosition;
                    CharacterRotationSpeed = defaultCharacterRotationSpeed;
                    valuesUpdated = true;
                }
                if (Input.GetKey(KeyCode.W))
                {
                    renderCamera.transform.position -= renderCamera.transform.forward * Time.deltaTime;
                    valuesUpdated = true;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    renderCamera.transform.position += renderCamera.transform.forward * Time.deltaTime;
                    valuesUpdated = true;
                }
                if (Input.GetKey(KeyCode.A))
                {
                    renderCamera.transform.position += new Vector3(Time.deltaTime, 0, 0);
                    valuesUpdated = true;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    renderCamera.transform.position -= new Vector3(Time.deltaTime, 0, 0);
                    valuesUpdated = true;
                }

                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q))
                {
                    CharacterRotationSpeed += Time.deltaTime * rotationSpeedDelta * (shiftDown ? 3 : 1);
                    valuesUpdated = true;
                }
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E))
                {
                    CharacterRotationSpeed -= Time.deltaTime * rotationSpeedDelta * (shiftDown ? 3 : 1);
                    valuesUpdated = true;
                }

                if (displayedPlayer)
                {
                    if (speed != CharacterRotationSpeed)
                    {
                        displayedPlayer.GetComponent<AutoRotate>().rotationSpeed = new Vector3(0, CharacterRotationSpeed, 0);
                    }

                    if (valuesUpdated)
                    {
                        SaveValues();
                    }
                }
            }
        }

        if (server != null)
        {
            server.Update();
        }
    }

    private void SaveValues()
    {
        var renderCameraPos = renderCamera.transform.position;
        PlayerPrefs.SetFloat("_overlay_renderCameraPosition_x", renderCameraPos.x);
        PlayerPrefs.SetFloat("_overlay_renderCameraPosition_y", renderCameraPos.y);
        PlayerPrefs.SetFloat("_overlay_renderCameraPosition_z", renderCameraPos.z);
        PlayerPrefs.SetFloat("_overlay_characterRotationSpeed", CharacterRotationSpeed);
    }

    private void LoadValues()
    {
        var renderCameraPos = renderCamera.transform.position;
        var x = PlayerPrefs.GetFloat("_overlay_renderCameraPosition_x", renderCameraPos.x);
        var y = PlayerPrefs.GetFloat("_overlay_renderCameraPosition_y", renderCameraPos.y);
        var z = PlayerPrefs.GetFloat("_overlay_renderCameraPosition_z", renderCameraPos.z);
        var i = PlayerPrefs.GetInt("_overlay_skybox_index", skyboxIndex);
        var ui = PlayerPrefs.GetInt("_overlay_uiEnabled", 1) == 1;
        CharacterRotationSpeed = PlayerPrefs.GetFloat("_overlay_characterRotationSpeed", defaultCharacterRotationSpeed);
        renderCamera.transform.position = new Vector3(x, y, z);
        if (i > 0) SetSkybox(i);
        if (!ui) SetUI(false);
    }

    private void ToggleResolution()
    {
        screenSizeIndex = (screenSizeIndex + 1) % screenSizes.Length;
        Screen.SetResolution(screenSizes[screenSizeIndex], screenSizes[screenSizeIndex], false);
    }

    private void ToggleSkybox()
    {
        skyboxIndex = (skyboxIndex + 1) % skyboxBackgrounds.Length;
        PlayerPrefs.SetInt("_overlay_skybox_index", skyboxIndex);
        SetSkybox(skyboxIndex);
    }

    private void ToggleUI()
    {
        var newValue = !this.ui.gameObject.activeInHierarchy;
        SetUI(newValue);
        PlayerPrefs.SetInt("_overlay_uiEnabled", newValue ? 1 : 0);
    }

    private void SetUI(bool enabled)
    {
        this.ui.gameObject.SetActive(enabled);
    }

    private void SetSkybox(int index)
    {
        skyboxIndex = index;
        RenderSettings.skybox = skyboxBackgrounds[skyboxIndex];
    }

    private void AwakeOverlay()
    {
        if (!Application.isEditor)
        {
            var tmpPlayer = FindObjectOfType<PlayerController>();
            if (tmpPlayer)
            {
                Destroy(tmpPlayer.gameObject);
            }
        }

        LoadValues();

        Application.targetFrameRate = 60;
        //QualitySettings.vSyncCount = 2;
        client = new OverlayClient(packetManager);

        this.renderCameraDefaultPosition = renderCamera.transform.position;
    }

    private void DestroyOverlay()
    {
        try
        {
            client.Dispose();
        }
        catch { }
    }

    private void AwakeGame()
    {
        server = new OverlayServer(this.gameManager, packetManager);
        server.ClientConnected += OverlayClientConnected;
    }

    private void OverlayClientConnected(object sender, OverlayClient e)
    {
        if (gameManager.Items.Loaded)
        {
            SendItems(gameManager.Items.GetItems());
            SendRedeemables(gameManager.Items.GetRedeemableItems());

            if (this.displayedPlayer)
            {
                SendObservePlayer(this.displayedPlayer);
            }
        }
    }

    private void DestroyGame()
    {
        try
        {
            server.ClientConnected -= OverlayClientConnected;
            server.Dispose();
        }
        catch { }
    }
    #endregion
}
