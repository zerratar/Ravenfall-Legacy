using System;
using UnityEngine;

public class MenuView : MonoBehaviour
{
    private Action onHide;

    public bool Visible => gameObject.activeSelf;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            //this.Hide();
            HideAndApplyChanges();
        }
    }

    public void Show(Action onHide = null)
    {
        this.onHide = onHide;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (onHide != null) onHide();
    }

    public void HideAndApplyChanges()
    {
        OnChangesApplied();
        Hide();
    }

    protected virtual void OnChangesApplied()
    {
    }
}