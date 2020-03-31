using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ClickEvent : UnityEvent { }
public class UIButton : MonoBehaviour
{
    public ClickEvent Click;
}
