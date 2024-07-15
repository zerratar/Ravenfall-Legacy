using System;
using UnityEngine;
using UnityEngine.Events;

public class IslandInformationButton : MonoBehaviour
{
    [SerializeField] private GameObject button_up;
    [SerializeField] private GameObject button_down;

    private RaycastHit[] raycastHits = new RaycastHit[24];

    public UnityEvent OnClick;

    private bool isButtonDown;

    private void Start()
    {
        button_down.SetActive(false);
        button_up.SetActive(true);
    }

    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        RayCastMouse(OnMouseDown);
    //    }
    //    else if (Input.GetMouseButtonUp(0))
    //    {
    //        RayCastMouse(OnMouseUp);
    //    }
    //}

    //private void RayCastMouse(Action onHit)
    //{
    //    var activeCamera = Camera.main;
    //    if (!activeCamera || activeCamera == null)
    //    {
    //        return;
    //    }
    //    var ray = activeCamera.ScreenPointToRay(Input.mousePosition);
    //    var hitCount = Physics.RaycastNonAlloc(ray, raycastHits, 1000);
    //    for (var i = 0; i < hitCount; ++i)
    //    {
    //        var hit = raycastHits[i];
    //        if (hit.collider.CompareTag("Button"))
    //        {
    //            if (hit.collider.GetInstanceID() != this.GetInstanceID())
    //                return;
    //            if (onHit != null) onHit();
    //            return;
    //        }
    //    }
    //    if (isButtonDown)
    //    {
    //        OnMouseExit();
    //    }
    //}

    public void OnMouseExit()
    {
        isButtonDown = false;
        button_down.SetActive(false);
        button_up.SetActive(true);
    }

    public void OnMouseDown()
    {
        isButtonDown = true;

        button_up.SetActive(false);
        button_down.SetActive(true);
    }

    public void OnMouseUp()
    {
        var wasDown = isButtonDown;

        button_up.SetActive(true);
        button_down.SetActive(false);
        isButtonDown = false;

        if (wasDown && OnClick != null)
        {
            OnClick.Invoke();
        }
    }
}
