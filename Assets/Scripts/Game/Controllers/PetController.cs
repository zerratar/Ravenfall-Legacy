using Assets.Scripts;
using Shinobytes.Linq;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class PetController : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 1.0f;
    [SerializeField] private Vector3 offsetPosition;
    [SerializeField] private float sleepTimer = 3.0f;
    [SerializeField] private GameManager gameManager;

    [SerializeField] private int IdlePoses = 1;
    [SerializeField] private float idlePoseTimer = 3.0f;

    [Header("Equipment")] public bool HasSeasonalEquipment;
    [ShowIf("HasSeasonalEquipment")] public GameObject[] HalloweenEquipment;
    [ShowIf("HasSeasonalEquipment")] public GameObject[] ChristmasEquipment;
    //public bool HasRandomEquipment;


    public bool CanSleep = true;
    public bool HasEmotions;

    [Header("Emotion Settings")]
    [ShowIf("HasEmotions")] public bool ReactToChatMessage = true;
    [ShowIf("HasEmotions")] public float Happiness = 0.5f; // 0.5f normal, 1f very happy, 0 very angry or sad
    [ShowIf("HasEmotions")] public float Hunger = 0f; // 0: not hungry, 1: very hungry
    [ShowIf("HasEmotions")] public float Energy = 1f; // 1: full of energy, 0: no energy
    [ShowIf("HasEmotions")] public float EnergyRecoveryPerSecond = 0.1f;

    /*[ShowIf("HasEmotions"), SerializeField] */private float emotionTimer = 60.0f;
    [ShowIf("HasEmotions"), SerializeField] private SkinnedMeshRenderer emotionRenderer;
    [ShowIf("HasEmotions"), SerializeField] private int emotionRendererMaterialIndex = 1;
    [ShowIf("HasEmotions")] public Material[] EmotionMaterials;
    //[ShowIf("HasEmotions")] public EmotionMaterial[] Emotions;

    //[ShowIf("HasEmotions")] public Material mat_standard;
    //[ShowIf("HasEmotions")] public Material mat_happy;
    //[ShowIf("HasEmotions")] public Material mat_drool;
    //[ShowIf("HasEmotions")] public Material mat_angry;
    //[ShowIf("HasEmotions")] public Material mat_sad;
    //[ShowIf("HasEmotions")] public Material mat_confused;
    //[ShowIf("HasEmotions")] public Material mat_love;

    private float timeForNextEmotion = 0f;
    private float timeForNextIdlePose = 0f;
    private bool isSleeping;
    private float lastPlayerMove;

    private Animator animator;
    private PlayerController player;
    private Light lightSource;
    private bool playerWasMoving;
    private Transform _transform;
    private List<Material> sharedMaterials;

    private int currentIdlePoseIndex = 0;
    private int currentMonth;

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

        //rootStateMachine.AddAnyStateTransition

        // Idle to Run
        var idleTransitionToRun = stateIdle.AddTransition(stateRun);
        idleTransitionToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0, "Run");
        idleTransitionToRun.duration = 0;

        if (CanSleep)
        {
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
        }

        if (HasEmotions)
        {
            // Idle should be able to go to any emotion animation, these can only be active while standing still.
        }

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
        this._transform = this.transform;
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
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
        {
            return;
        }

        // Check if we should enable halloween or christmas equipment
        if (HasSeasonalEquipment)
        {
            var month = GameTime.now.Month;
            if (month != currentMonth)
            {
                DisableAllSeasonalEquipment();
                if (month == 10)
                {
                    EnableHalloweenEquipment();
                }
                else if (month == 12)
                {
                    EnableChristmasEquipment();
                }
                currentMonth = month;
            }
        }

        if (lightSource && Overlay.IsGame)
        {
            lightSource.enabled = !gameManager.PotatoMode;
        }

        if (HasEmotions && GameTime.time >= timeForNextEmotion)
        {
            SetNextEmotionMaterial();
        }

        if (IdlePoses > 1)
        {
            if (GameTime.time >= timeForNextIdlePose)
            {
                currentIdlePoseIndex = UnityEngine.Random.Range(0, IdlePoses);
                timeForNextIdlePose = GameTime.time + idlePoseTimer;
            }
        }

        var playerIsMoving = player.Movement.IsMoving;
        if (CanSleep)
        {
            if (playerIsMoving)
            {
                lastPlayerMove = GameTime.time;
                if (isSleeping)
                {
                    isSleeping = false;
                    animator.SetBool("Sleeping", false);
                }
            }

            if (GameTime.time - lastPlayerMove > sleepTimer && !isSleeping)
            {
                isSleeping = true;
                animator.SetBool("Sleeping", true);
            }

            // if (!isSleeping && Energy)
            // should we make the pet stay and not move until it got energy? nah, to troublesome :)

            if (isSleeping && Energy < 1f)
            {
                Energy = Mathf.Clamp01(Energy + GameTime.deltaTime * EnergyRecoveryPerSecond);
            }
        }

        if (_transform.localPosition.x != offsetPosition.x)
            _transform.localPosition = offsetPosition;

        UpdateAnimator(playerIsMoving);
    }


    private void DisableChristmasEquipment()
    {
        if (ChristmasEquipment != null && ChristmasEquipment.Length > 0)
        {
            foreach (var eq in ChristmasEquipment)
            {
                eq.SetActive(false);
            }
        }
    }

    private void DisableHalloweenEquipment()
    {
        if (HalloweenEquipment != null && HalloweenEquipment.Length > 0)
        {
            foreach (var eq in HalloweenEquipment)
            {
                eq.SetActive(false);
            }
        }
    }

    private void EnableChristmasEquipment()
    {
        if (ChristmasEquipment != null && ChristmasEquipment.Length > 0)
        {
            ChristmasEquipment.Random().SetActive(true);
        }
    }

    private void EnableHalloweenEquipment()
    {
        if (HalloweenEquipment != null && HalloweenEquipment.Length > 0)
        {
            HalloweenEquipment.Random().SetActive(true);
        }
    }

    private void DisableAllSeasonalEquipment()
    {
        DisableHalloweenEquipment();
        DisableChristmasEquipment();
    }

    private void SetNextEmotionMaterial()
    {
        if (sharedMaterials == null)
        {
            var mats = emotionRenderer.sharedMaterials.Length > emotionRenderer.materials.Length ? emotionRenderer.sharedMaterials : emotionRenderer.materials;
            sharedMaterials = new List<Material>(mats.Length);
            for (int i = 0; i < mats.Length; ++i)
            {
                sharedMaterials.Add(mats[i]);
            }
        }

        timeForNextEmotion = GameTime.time + emotionTimer;
        sharedMaterials[emotionRendererMaterialIndex] = EmotionMaterials.Random();
        emotionRenderer.SetSharedMaterials(sharedMaterials);
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

    internal void SetEmotion(string emotion)
    {
        UnityEngine.Debug.Log("Setting Emotion to: " + emotion);
    }
}

//[Serializable]
//public struct EmotionMaterial
//{
//    public string Emotion;
//    public Material Material;
//}