using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public class TabInputField : MonoBehaviour
{
    public Selectable nextSelectable;

    public bool OnTab = true;
    public bool OnEnter = true;

    private bool listening;
    private void Awake()
    {
        var thisSelectable = GetComponent<TMP_InputField>();
        thisSelectable.onSelect.AddListener(x => { listening = true; });
        thisSelectable.onDeselect.AddListener(x => { listening = false; });
    }

    private void Update()
    {
        if (!listening)
        {
            return;
        }

        ListenForInputManager();
    }

    private void ListenForInputManager()
    {
        var keyDown = (OnTab && Input.GetKeyDown(KeyCode.Tab)) || (OnEnter && Input.GetKeyDown(KeyCode.Return));
        if (keyDown)
        {
            nextSelectable.Select();
        }
    }
}
