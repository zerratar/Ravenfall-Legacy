using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameObject all2d;
    [SerializeField] private GameObject playerList;

    private UIState state;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            state = (UIState)(((int)state + 1) % 3);
            if (state == UIState.Everything)
            {
                all2d.SetActive(true);
                playerList.SetActive(true);
            }
            else if (state == UIState.NoPlayerList)
            {
                playerList.SetActive(false);
            }
            else
            {
                all2d.SetActive(false);
                playerList.SetActive(false);
            }
        }
    }
    public enum UIState
    {
        Everything,
        NoPlayerList,
        Nothing
    }
}

