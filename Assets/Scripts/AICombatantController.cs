using Shinobytes.Linq;
using UnityEngine;

public class AICombatantController : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshPro lblName;
    [SerializeField] private AICombatantRole role;
    [SerializeField] private Animator animator;
    [SerializeField] private RectTransform nameTagTransform;

    [SerializeField] private float getHitDelay = 0.3f;
    [SerializeField] private float stunTimeAfterHit = 1f;
    [SerializeField] private float movementSpeed = 2.5f;
    [SerializeField] private float minTimeBetweenAttack = 1f;
    [SerializeField] private float maxTimeBetweenAttack = 3f;

    private float stunTimer = 0f;

    private float movement;
    private float attackInterval;
    private float nextAttack;
    private string nameFormat;
    private float nameTagBaseY;
    private SkinnedMeshRenderer activeMeshRenderer;

    private AICombatantController attackTarget;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool initialized;

    public AICombatantRole Role => role;

    public void Awake()
    {
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        initialized = true;
    }

    public void SetRole(string name, AICombatantRole role)
    {
        this.role = role;
        this.name = name;

        if (string.IsNullOrEmpty(nameFormat))
        {
            nameFormat = lblName.text;
        }

        lblName.text = string.Format(nameFormat, role, name);

        AssignRandomMesh();
    }

    private void AssignRandomMesh()
    {
        var meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (activeMeshRenderer == null)
        {
            activeMeshRenderer = meshRenderers.FirstOrDefault(x => x.gameObject.activeInHierarchy);
        }

        activeMeshRenderer.gameObject.SetActive(false);
        activeMeshRenderer = meshRenderers.Random();
        activeMeshRenderer.gameObject.SetActive(true);
    }

    internal void Attack(AICombatantController closest)
    {
        if (closest == null)
        {
            return;
        }

        if (nextAttack == 0)
        {
            nextAttack = GameTime.time + Random.Range(0, minTimeBetweenAttack);
            return;
        }

        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            return;
        }

        if (GameTime.time < nextAttack)
        {
            return;
        }

        // rotate towards the enemy
        var direction = (closest.transform.position - transform.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        nextAttack = GameTime.time + Random.Range(minTimeBetweenAttack, maxTimeBetweenAttack);

        animator.SetInteger("AttackType", Random.Range(0, 6));
        animator.SetTrigger("Attack");

        this.attackTarget = closest;

        // get hit should be slightly delayed

        Invoke(nameof(HitOpponent), getHitDelay);
    }


    // called from certain attack animations via AnimationEvent
    public void Hit()
    {
        // leave empty, not going to be used.
    }

    private void HitOpponent()
    {
        //lastAttackedTarget.GetHit();
        if (stunTimer > 0)
        {
            return;
        }

        attackTarget.GetHit();
    }

    private void GetHit()
    {
        animator.SetTrigger("Hit");
        stunTimer = stunTimeAfterHit;
    }

    internal void MoveTowards(Vector3 position)
    {
        if (movement < 1f)
        {
            movement = 1f;
            animator.SetFloat("Movement", movement);
        }

        transform.position = Vector3.MoveTowards(transform.position, position, Time.deltaTime * movementSpeed);
    }

    internal void StopMoving()
    {
        if (movement > 0)
        {
            movement = 0;
            animator.SetFloat("Movement", movement);
        }
    }

    internal void SetNameTagYOffset(float offset)
    {
        var pos = nameTagTransform.localPosition;
        if (nameTagBaseY == 0f)
        {
            nameTagBaseY = pos.y;
        }

        pos.y = nameTagBaseY + offset;
        nameTagTransform.localPosition = pos;
    }

    internal void Reset()
    {
        transform.SetPositionAndRotation(startingPosition, startingRotation);

        if (role == AICombatantRole.Judge || !initialized)
        {
            return;
        }

        animator.SetBool("Win", false);
        animator.SetBool("Lose", false);

        StopMoving();
    }

    internal void Lose()
    {
        animator.SetBool("Win", false);
        animator.SetBool("Lose", true);
    }

    internal void Win()
    {
        animator.SetBool("Lose", false);
        animator.SetBool("Win", true);
    }
}

public enum AICombatantRole
{
    Judge,
    Defender,
    Challenger
}
