using Assets.Scripts;
using UnityEngine;

public class PetController : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 1.0f;
    [SerializeField] private Vector3 offsetPosition;
    [SerializeField] private float sleepTimer = 3.0f;
    [SerializeField] private GameManager gameManager;

    public bool isSleeping;
    public float lastPlayerMove;

    private Animator animator;
    private PlayerController player;
    private Light lightSource;

    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        player = GetComponentInParent<PlayerController>();
        lightSource = GetComponentInChildren<Light>();
    }

    void Update()
    {
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        if (!player)
        {
            player = GetComponentInParent<PlayerController>();
            return;
        }

        if (lightSource)
        {
            lightSource.enabled = !gameManager.PotatoMode;
        }

        if (!animator) return;

        transform.localPosition = offsetPosition;
        var playerIsMoving = player.IsMoving;
        if (playerIsMoving)
        {
            lastPlayerMove = Time.time;
            if (isSleeping)
            {
                isSleeping = false;
            }
        }

        if (Time.time - lastPlayerMove > sleepTimer && !isSleeping)
        {
            isSleeping = true;
        }

        animator.SetFloat("Walk", playerIsMoving ? movementSpeed : 0);
        animator.SetFloat("Run", playerIsMoving ? movementSpeed : 0);
        animator.SetBool("Sleeping", isSleeping);
    }
}