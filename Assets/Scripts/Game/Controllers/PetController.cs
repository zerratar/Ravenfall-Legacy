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
    private bool playerWasMoving;

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

        var playerIsMoving = player.IsMoving;
        if (playerIsMoving)
        {
            lastPlayerMove = Time.time;
            if (isSleeping)
            {
                isSleeping = false;
                animator.SetBool("Sleeping", false);
            }
        }

        if (Time.time - lastPlayerMove > sleepTimer && !isSleeping)
        {
            isSleeping = true;
            animator.SetBool("Sleeping", true);
        }

        if (transform.localPosition.x != offsetPosition.x)
            transform.localPosition = offsetPosition;

        UpdateAnimator(playerIsMoving);
    }

    private void UpdateAnimator(bool playerIsMoving)
    {
        if (playerWasMoving != playerIsMoving)
        {
            animator.SetFloat("Walk", playerIsMoving ? movementSpeed : 0);
            animator.SetFloat("Run", playerIsMoving ? movementSpeed : 0);
            playerWasMoving = playerIsMoving;
        }
    }
}