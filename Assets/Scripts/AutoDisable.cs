using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDisable : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        this.gameObject.SetActive(false);
    }
}
