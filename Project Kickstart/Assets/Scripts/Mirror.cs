using UnityEngine;
using System.Collections;

public class Mirror : MonoBehaviour
{
    [Header("Mirror Settings")]
    [SerializeField] private float triggerRadius = 3f;
    [SerializeField] private bool enableDebug = true;
    
    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = false;
    [SerializeField] private string dimensionSwitchAnimationTrigger = "DimensionSwitch";
    [SerializeField] private float animationDuration = 1f;
    
    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 2f;
    
    private Movement2 playerMovement;
    private GameObject player;
    private Animator playerAnimator;
    private DimensionManager dimensionManager;
    
    // Mirror state
    private bool playerInRange = false;
    private bool canSwitchDimension = true;
    private bool hasPlayerExitedAfterSwitch = true;
    
    private void Start()
    {
        player = FindFirstObjectByType<Movement2>()?.gameObject;
        if (player != null)
        {
            playerMovement = player.GetComponent<Movement2>();
            playerAnimator = player.GetComponent<Animator>();
        }
        
        dimensionManager = FindFirstObjectByType<DimensionManager>();
        
        if (enableDebug)
        {
            Debug.Log($"Mirror initialized. Player found: {player != null}");
        }
    }
    
    private void Update()
    {
        if (player == null) return;
        
        CheckPlayerProximity();
    }
    
    private void CheckPlayerProximity()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distanceToPlayer <= triggerRadius;
        
        if (enableDebug && wasInRange != playerInRange)
        {
            Debug.Log($"Distance to mirror: {distanceToPlayer:F2} (trigger: {triggerRadius})");
        }
        
        // Player entered mirror area
        if (playerInRange && !wasInRange)
        {
            OnPlayerEnterMirrorArea();
        }
        // Player exited mirror area
        else if (!playerInRange && wasInRange)
        {
            OnPlayerExitMirrorArea();
        }
    }
    
    private void OnPlayerEnterMirrorArea()
    {
        if (enableDebug)
        {
            Debug.Log($"Player entered mirror area. CanSwitch: {canSwitchDimension}, HasExited: {hasPlayerExitedAfterSwitch}");
        }
        
        // Only allow switch if not on cooldown and player has exited after last switch
        if (canSwitchDimension && hasPlayerExitedAfterSwitch)
        {
            StartCoroutine(SwitchDimensionWithAnimation());
        }
        else if (enableDebug)
        {
            Debug.Log("Switch blocked - waiting for cooldown or player needs to exit area first");
        }
    }
    
    private void OnPlayerExitMirrorArea()
    {
        if (enableDebug)
        {
            Debug.Log("Player exited mirror area");
        }
        
        // Mark that player has exited the area, allowing new switch on next entry
        hasPlayerExitedAfterSwitch = true;
    }
    
    private IEnumerator SwitchDimensionWithAnimation()
    {
        // Prevent new switches until cooldown
        canSwitchDimension = false;
        hasPlayerExitedAfterSwitch = false;
        
        if (enableDebug)
        {
            Debug.Log("Starting dimension switch");
        }
        
        // Trigger animation if enabled and available
        if (useAnimation && playerAnimator != null && !string.IsNullOrEmpty(dimensionSwitchAnimationTrigger))
        {
            playerAnimator.SetTrigger(dimensionSwitchAnimationTrigger);
            // Wait for animation duration
            yield return new WaitForSeconds(animationDuration);
        }
        
        // Execute dimension switch
        if (dimensionManager != null)
        {
            dimensionManager.SwitchDimension();
            if (enableDebug)
            {
                Debug.Log($"Dimension switched to: {dimensionManager.CurrentDimension}");
            }
        }
        else
        {
            // Fallback to old system
            GameStats.isManic = !GameStats.isManic;
            if (enableDebug)
            {
                Debug.Log($"Fallback - GameStats.isManic is now: {GameStats.isManic}");
            }
        }
        
        // Start cooldown
        StartCoroutine(CooldownTimer());
    }
    
    private IEnumerator CooldownTimer()
    {
        yield return new WaitForSeconds(cooldownTime);
        canSwitchDimension = true;
        
        if (enableDebug)
        {
            Debug.Log("Mirror cooldown finished");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw mirror trigger area
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        if (playerInRange)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, triggerRadius * 0.1f);
        }
    }
}
