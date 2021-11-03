using UnityEngine;

public class PetRacingContender : MonoBehaviour
{
    [SerializeField] private NameTagManager nameTagManager;
    private NameTag nameTag;

    public void Start()
    {
        if (!nameTagManager) nameTagManager = FindObjectOfType<NameTagManager>();
        this.nameTag = nameTagManager.Add(this.transform);
        this.nameTag.Scale = 0.1f;
        this.transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    public void OnDestroy()
    {
        if (!nameTagManager) return;
        nameTagManager.Remove(this.transform);
    }
}
