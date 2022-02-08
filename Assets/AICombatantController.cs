using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICombatantController : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshPro lblName;
    [SerializeField] private AICombatantRole role;

    public AICombatantRole Role => role;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetRole(string name, AICombatantRole role)
    {
        this.role = role;
        this.name = name;
        lblName.text = "<size=6>The " + role + "\r\n<size=12.2>" + name;
    }
}

public enum AICombatantRole
{
    Judge,
    Defender,
    Challenger
}
