using Assets.Scripts;
using Shinobytes.Linq;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

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

    //[SerializeField] private bool hasGearStats;
    //[SerializeField] private int gearStat;



#if UNITY_EDITOR
    [Button("Generate Animation Controller")]
    public void GenerateAnimationController()
    {
        var itemName = gameObject.name;

        // try get existing controller so we can grab the correct animations and then replace the animation controller used.

        var animator = gameObject.GetComponentInChildren<Animator>();

        var currentController = animator.runtimeAnimatorController; // this is the one we want to replace later.

        // get all animation clips, we will use these to determine which animations to use later.
        var availableAnimations = currentController.animationClips;


        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Pets/" + itemName.Replace(" ", "") + ".controller");

        // Add parameters
        controller.AddParameter("Run", AnimatorControllerParameterType.Float);
        controller.AddParameter("Walk", AnimatorControllerParameterType.Float);
        controller.AddParameter("Sleeping", AnimatorControllerParameterType.Bool);


        // Add StateMachines
        var rootStateMachine = controller.layers[0].stateMachine;

        var stateIdle = rootStateMachine.AddState("Idle");
        stateIdle.motion = availableAnimations.OrderBy(x => x.name.Length).FirstOrDefault(x => x.name.Contains("idle", System.StringComparison.OrdinalIgnoreCase));

        var stateRun = rootStateMachine.AddState("Run");
        stateRun.motion = availableAnimations.OrderBy(x => x.name.Length).FirstOrDefault(x => x.name.Contains("run", System.StringComparison.OrdinalIgnoreCase));

        var stateSleeping = rootStateMachine.AddState("Sleeping");
        stateSleeping.motion = availableAnimations.OrderBy(x => x.name.Length).FirstOrDefault(x => x.name.Contains("sleep", System.StringComparison.OrdinalIgnoreCase)) ?? stateIdle.motion;

        rootStateMachine.AddEntryTransition(stateIdle);

        // Idle to Run
        var idleTransitionToRun = stateIdle.AddTransition(stateRun);
        idleTransitionToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0, "Run");
        idleTransitionToRun.duration = 0;

        // Idle to Sleeping
        var idleTransitionToSleep = stateIdle.AddTransition(stateSleeping);
        idleTransitionToSleep.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Sleeping");
        idleTransitionToSleep.duration = 0;

        // Sleeping to Run
        var sleepingTransitionToRun = stateSleeping.AddTransition(stateRun);
        sleepingTransitionToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0, "Run");
        sleepingTransitionToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "Sleeping");
        sleepingTransitionToRun.duration = 0;

        // Sleeping to Idle
        var sleepingTransitionToIdle = stateSleeping.AddTransition(stateIdle);
        sleepingTransitionToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "Sleeping");
        sleepingTransitionToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Run");
        sleepingTransitionToIdle.duration = 0;


        // Run to Sleeping
        var runTransitionToSleeping = stateRun.AddTransition(stateSleeping);
        runTransitionToSleeping.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Run");
        runTransitionToSleeping.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Sleeping");
        runTransitionToSleeping.duration = 0;

        // Run to Idle
        var runTransitionToIdle = stateRun.AddTransition(stateIdle);
        runTransitionToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Run");
        runTransitionToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "Sleeping");
        runTransitionToIdle.duration = 0;

        // replace the used runtime controller
        animator.runtimeAnimatorController = controller;
    }

    [Button("Generate Item Row")]
    public void GenerateItemRow()
    {
        //Shinobytes.Debug.Log("Button for " + gameObject.name);
        var itemId = System.Guid.NewGuid();
        var itemName = gameObject.name;

        var itemRow = new RavenNest.Models.Item
        {
            Id = itemId,
            Name = itemName,
            Category = RavenNest.Models.ItemCategory.Pet,
            Type = RavenNest.Models.ItemType.Pet,
            IsGenericModel = true,
            GenericPrefab = "Pets/" + itemName.Replace(" ", ""),
            Level = 1,
            ShopSellPrice = 1000,
        };

        var resx = Resources.Load<GameObject>(itemRow.GenericPrefab);
        if (!resx)
        {
            Shinobytes.Debug.LogWarning(itemRow.GenericPrefab + " does not exist. Saving a copy");
            var instance = Instantiate(this.gameObject);
            instance.transform.position = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Resources/" + itemRow.GenericPrefab + ".prefab");
            DestroyImmediate(instance);
        }
        var data = Newtonsoft.Json.JsonConvert.SerializeObject(itemRow);
        GUIUtility.systemCopyBuffer = data;
        Shinobytes.Debug.Log(data);
    }

#endif
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
        if (GameCache.IsAwaitingGameRestore) return;
        if (!player)
        {
            player = GetComponentInParent<PlayerController>();
            return;
        }

        if (!animator)
            return;

        if (lightSource && Overlay.IsGame)
        {
            lightSource.enabled = !gameManager.PotatoMode;
        }

        var playerIsMoving = player.Movement.IsMoving;
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