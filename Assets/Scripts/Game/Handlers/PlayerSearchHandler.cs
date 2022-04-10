using System.Collections;
using Shinobytes.Linq;
using TMPro;
using UnityEngine;

public class PlayerSearchHandler : MonoBehaviour
{
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlayerList playerList;
    [SerializeField] private GameCamera gameCamera;
    [SerializeField] private TMP_InputField inputPlayerSearch;

    public bool Visible { get; private set; }

    public void InputPlayerSearchChanged(string value)
    {
        playerList.ClearFocus();
        if (string.IsNullOrEmpty(value)) return;
        UpdatePlayerFocus();
    }

    private void UpdatePlayerFocus()
    {
        var players = playerManager.FindPlayers(inputPlayerSearch.text);
        if (players.Count > 0)
            playerList.FocusOnPlayers(players);
    }

    private void Start()
    {
        var onValueChanged = new TMP_InputField.OnChangeEvent();
        onValueChanged.AddListener(InputPlayerSearchChanged);
        inputPlayerSearch.onValueChanged = onValueChanged;
    }

    private void Update()
    {
        if (!inputPlayerSearch)
            return;

        if (inputPlayerSearch.isFocused)
            UpdatePlayerFocus();

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Hide();
            return;
        }

        if (Input.GetKeyUp(KeyCode.KeypadEnter) || Input.GetKeyUp(KeyCode.Return))
        {
            if (string.IsNullOrEmpty(inputPlayerSearch.text))
            {
                Hide();
                return;
            }

            SearchForPlayers();
        }
    }

    private void SearchForPlayers()
    {
        var possiblePlayers = playerManager.FindPlayers(inputPlayerSearch.text);
        if (possiblePlayers == null || possiblePlayers.Count == 0)
        {
            StartCoroutine(NoPlayersFound());
            return;
        }
        gameCamera.ObservePlayer(possiblePlayers.FirstOrDefault());
        Hide();
    }

    private IEnumerator NoPlayersFound()
    {
        inputPlayerSearch.richText = true;
        var text = inputPlayerSearch.text;
        var isRed = true;
        for (var i = 0; i < 4; ++i)
        {
            var color = isRed ? "<color=#FF0000>" : "";
            inputPlayerSearch.text = $"{color}{inputPlayerSearch.text}";
            yield return new WaitForSeconds(0.125f);
            isRed = !isRed;
        }

        inputPlayerSearch.text = text;
    }

    private void Hide()
    {
        inputPlayerSearch.text = "";
        playerList.ClearFocus();
        gameObject.SetActive(false);
        Visible = false;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        inputPlayerSearch.ActivateInputField();
        inputPlayerSearch.Select();
        Visible = true;
    }
}