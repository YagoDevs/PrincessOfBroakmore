using UnityEngine;

/// <summary>
/// Helper script to automatically setup AudioManager in the scene.
/// Attach this to any GameObject or create a dedicated AudioManager GameObject.
/// </summary>
public class AudioSetupHelper : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool createAudioManagerOnStart = true;
    
    [Header("Default Audio Clips (Optional)")]
    [SerializeField] private AudioClip[] defaultBoxPushSounds;
    [SerializeField] private AudioClip[] defaultBoxSlideSounds;

    private void Start()
    {
        if (createAudioManagerOnStart && AudioManager.Instance == null)
        {
            SetupAudioManager();
        }
    }

    /// <summary>
    /// Creates and configures AudioManager in the scene
    /// </summary>
    [ContextMenu("Setup AudioManager")]
    public void SetupAudioManager()
    {
        // Check if AudioManager already exists
        if (AudioManager.Instance != null)
        {
            Debug.Log("AudioManager already exists in the scene!");
            return;
        }

        // Create AudioManager GameObject
        GameObject audioManagerGO = new GameObject("AudioManager");
        AudioManager audioManager = audioManagerGO.AddComponent<AudioManager>();
        
        // Configure with default clips if available
        if (defaultBoxPushSounds != null && defaultBoxPushSounds.Length > 0)
        {
            // Use reflection to set private serialized fields (for demo purposes)
            var fieldInfo = typeof(AudioManager).GetField("boxPushSounds", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo?.SetValue(audioManager, defaultBoxPushSounds);
        }
        
        if (defaultBoxSlideSounds != null && defaultBoxSlideSounds.Length > 0)
        {
            var fieldInfo = typeof(AudioManager).GetField("boxSlideSounds", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo?.SetValue(audioManager, defaultBoxSlideSounds);
        }

        Debug.Log("AudioManager created and configured successfully!");
    }

    /// <summary>
    /// Test audio system by playing a sample sound
    /// </summary>
    [ContextMenu("Test Audio System")]
    public void TestAudioSystem()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager not found! Create one first.");
            return;
        }

        // Test push sound at this object's position
        AudioManager.Instance.PlayBoxPushSound(transform.position, 1f);
        Debug.Log("Test sound played!");
    }
}
