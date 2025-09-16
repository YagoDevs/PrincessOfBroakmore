using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized audio system manager.
/// Follows the same singleton pattern as DimensionManager for consistency.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float sfxVolume = 1f;
    
    [Header("Box Push Sounds")]
    [SerializeField] private AudioClip[] boxPushSounds;
    [SerializeField] private AudioClip[] boxSlideSounds;
    
    [Header("Dimension Switch Sounds")]
    [SerializeField] private AudioClip[] dimensionSwitchSounds;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume específico para sons de troca de dimensão (0.0 a 1.0)")]
    private float dimensionSwitchVolume = 0.8f;
    [SerializeField] [Range(0f, 2f)] [Tooltip("Delay antes de tocar o som de troca de dimensão (em segundos)")]
    private float dimensionSwitchDelay = 0.0f;
    
    [Header("Audio Sources")]
    [SerializeField] private int maxAudioSources = 10;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Singleton pattern for global access
    public static AudioManager Instance { get; private set; }
    
    // Pool of audio sources for efficiency
    private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
    private List<AudioSource> activeAudioSources = new List<AudioSource>();
    
    // Volume properties
    public float MasterVolume 
    { 
        get => masterVolume; 
        set => masterVolume = Mathf.Clamp01(value); 
    }
    
    public float SfxVolume 
    { 
        get => sfxVolume; 
        set => sfxVolume = Mathf.Clamp01(value); 
    }
    
    // Dimension switch properties
    public float DimensionSwitchVolume 
    { 
        get => dimensionSwitchVolume; 
        set => dimensionSwitchVolume = Mathf.Clamp01(value); 
    }
    
    public float DimensionSwitchDelay 
    { 
        get => dimensionSwitchDelay; 
        set => dimensionSwitchDelay = Mathf.Max(0f, value); 
    }

    private void Awake()
    {
        // Singleton implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log("AudioManager initialized successfully");
        }
        
        // Removed auto-test - only test manually
    }
    
    /// <summary>
    /// Test function to verify audio system is working
    /// </summary>
    [ContextMenu("Test Audio")]
    public void TestAudio()
    {
        Debug.Log("Testing audio system...");
        
        if (boxPushSounds == null || boxPushSounds.Length == 0)
        {
            Debug.LogError("No box push sounds configured!");
            return;
        }
        
        Debug.Log($"Found {boxPushSounds.Length} box push sounds");
        PlayBoxPushSound(transform.position, 1f);
        Debug.Log("Test sound played!");
    }
    
    /// <summary>
    /// Test 2D audio (should be audible regardless of position)
    /// </summary>
    public void Test2DAudio()
    {
        if (boxPushSounds == null || boxPushSounds.Length == 0) return;
        
        Debug.Log("Testing 2D Audio...");
        
        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource != null)
        {
            audioSource.clip = boxPushSounds[0];
            audioSource.volume = 0.5f;
            audioSource.pitch = 1f;
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.Play();
            activeAudioSources.Add(audioSource);
            
            Debug.Log($"2D Test: Playing {audioSource.clip.name} - Volume: {audioSource.volume}");
        }
    }

    /// <summary>
    /// Test dimension switch sound with current settings
    /// </summary>
    [ContextMenu("Test Dimension Switch Sound")]
    public void TestDimensionSwitchSound()
    {
        if (dimensionSwitchSounds == null || dimensionSwitchSounds.Length == 0)
        {
            Debug.LogWarning("No dimension switch sounds configured for testing!");
            return;
        }
        
        Debug.Log($"Testing dimension switch sound with Volume={dimensionSwitchVolume}, Delay={dimensionSwitchDelay}s");
        PlayDimensionSwitchSound(DimensionType.DimensionA, DimensionType.DimensionB);
    }

    /// <summary>
    /// Initialize pool of audio sources
    /// </summary>
    private void InitializeAudioSources()
    {
        for (int i = 0; i < maxAudioSources; i++)
        {
            GameObject audioSourceGO = new GameObject($"AudioSource_{i}");
            audioSourceGO.transform.SetParent(transform);
            AudioSource audioSource = audioSourceGO.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSourcePool.Enqueue(audioSource);
        }
    }

    /// <summary>
    /// Play box push sound at specific position
    /// </summary>
    /// <param name="position">World position where sound should play</param>
    /// <param name="force">Force intensity (affects volume and pitch)</param>
    public void PlayBoxPushSound(Vector3 position, float force = 1f)
    {
        if (boxPushSounds == null || boxPushSounds.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("No box push sounds configured!");
            return;
        }

        // Select random push sound
        AudioClip clipToPlay = boxPushSounds[Random.Range(0, boxPushSounds.Length)];
        
        if (showDebugInfo)
        {
            Debug.Log($"Selected clip: {(clipToPlay != null ? clipToPlay.name : "NULL")}");
        }
        
        // Calculate volume and pitch based on force
        float volume = Mathf.Clamp01(force * 0.3f + 0.4f) * sfxVolume * masterVolume;
        float pitch = Mathf.Clamp(force * 0.2f + 0.8f, 0.8f, 1.2f);
        
        PlaySoundAtPosition(clipToPlay, position, volume, pitch);
        
        if (showDebugInfo)
        {
            Debug.Log($"Playing box push sound at {position} with force {force} (volume: {volume}, pitch: {pitch})");
        }
    }

    /// <summary>
    /// Play box slide sound (for continuous movement)
    /// </summary>
    /// <param name="position">World position where sound should play</param>
    /// <param name="velocity">Movement velocity (affects volume)</param>
    public void PlayBoxSlideSound(Vector3 position, float velocity = 1f)
    {
        if (boxSlideSounds == null || boxSlideSounds.Length == 0)
            return;

        AudioClip clipToPlay = boxSlideSounds[Random.Range(0, boxSlideSounds.Length)];
        float volume = Mathf.Clamp01(velocity * 0.2f + 0.2f) * sfxVolume * masterVolume;
        
        PlaySoundAtPosition(clipToPlay, position, volume, 1f);
    }

    /// <summary>
    /// Play dimension switch sound (2D sound for UI-like effect)
    /// </summary>
    /// <param name="fromDimension">Dimension being switched from</param>
    /// <param name="toDimension">Dimension being switched to</param>
    public void PlayDimensionSwitchSound(DimensionType fromDimension, DimensionType toDimension)
    {
        if (dimensionSwitchSounds == null || dimensionSwitchSounds.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("No dimension switch sounds configured!");
            return;
        }

        if (dimensionSwitchDelay > 0f)
        {
            // Play with delay
            StartCoroutine(PlayDimensionSwitchSoundDelayed(fromDimension, toDimension, dimensionSwitchDelay));
        }
        else
        {
            // Play immediately
            PlayDimensionSwitchSoundInternal(fromDimension, toDimension);
        }
    }

    /// <summary>
    /// Internal coroutine to play dimension switch sound with delay
    /// </summary>
    private System.Collections.IEnumerator PlayDimensionSwitchSoundDelayed(DimensionType fromDimension, DimensionType toDimension, float delay)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Dimension switch sound delayed by {delay}s: {fromDimension} → {toDimension}");
        }
        
        yield return new WaitForSeconds(delay);
        PlayDimensionSwitchSoundInternal(fromDimension, toDimension);
    }

    /// <summary>
    /// Internal method to actually play the dimension switch sound
    /// </summary>
    private void PlayDimensionSwitchSoundInternal(DimensionType fromDimension, DimensionType toDimension)
    {
        // Select random switch sound
        AudioClip clipToPlay = dimensionSwitchSounds[Random.Range(0, dimensionSwitchSounds.Length)];
        
        if (showDebugInfo)
        {
            Debug.Log($"Playing dimension switch sound: {fromDimension} → {toDimension}");
            Debug.Log($"Selected clip: {(clipToPlay != null ? clipToPlay.name : "NULL")}");
        }
        
        // Play as 2D sound with configurable volume
        float volume = dimensionSwitchVolume * sfxVolume * masterVolume;
        float pitch = 1f;
        
        // Get available audio source
        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource == null)
        {
            Debug.LogWarning("No available audio sources for dimension switch!");
            return;
        }

        // Configure as 2D sound (UI-like)
        audioSource.transform.position = Vector3.zero;
        audioSource.clip = clipToPlay;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        
        // Play immediately
        audioSource.Play();
        activeAudioSources.Add(audioSource);
        
        if (showDebugInfo)
        {
            Debug.Log($"✓ Dimension switch sound playing: Volume={volume}, Playing={audioSource.isPlaying}");
        }
        
        // Return to pool when finished
        StartCoroutine(ReturnAudioSourceToPool(audioSource, clipToPlay.length / pitch));
    }

    /// <summary>
    /// Play any sound at specific position with custom parameters
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="position">World position</param>
    /// <param name="volume">Volume (0-1)</param>
    /// <param name="pitch">Pitch (0.1-3)</param>
    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) 
        {
            Debug.LogError("AudioClip is null!");
            return;
        }

        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource == null)
        {
            Debug.LogWarning("No available audio sources in pool!");
            return;
        }

        // Configure audio source
        audioSource.transform.position = position;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = 0f; // 2D sound for testing
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 20f;
        audioSource.minDistance = 1f;
        
        // Play and manage lifecycle
        audioSource.Play();
        activeAudioSources.Add(audioSource);
        
        if (showDebugInfo)
        {
            Debug.Log($"✓ AudioSource configured: Clip={clip.name}, Volume={volume}, Playing={audioSource.isPlaying}");
            Debug.Log($"AudioSource details: SpatialBlend={audioSource.spatialBlend}, MaxDistance={audioSource.maxDistance}");
        }
        
        // Return to pool when finished
        StartCoroutine(ReturnAudioSourceToPool(audioSource, clip.length / pitch));
    }

    /// <summary>
    /// Get available audio source from pool
    /// </summary>
    private AudioSource GetAvailableAudioSource()
    {
        if (audioSourcePool.Count > 0)
        {
            return audioSourcePool.Dequeue();
        }
        
        // If pool is empty, try to find a finished audio source
        for (int i = activeAudioSources.Count - 1; i >= 0; i--)
        {
            if (!activeAudioSources[i].isPlaying)
            {
                AudioSource audioSource = activeAudioSources[i];
                activeAudioSources.RemoveAt(i);
                return audioSource;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Return audio source to pool after delay
    /// </summary>
    private System.Collections.IEnumerator ReturnAudioSourceToPool(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (activeAudioSources.Contains(audioSource))
        {
            activeAudioSources.Remove(audioSource);
            audioSource.Stop();
            audioSource.clip = null;
            audioSourcePool.Enqueue(audioSource);
        }
    }

    /// <summary>
    /// Stop all currently playing sounds
    /// </summary>
    public void StopAllSounds()
    {
        foreach (AudioSource audioSource in activeAudioSources)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
        
        // Return all to pool
        while (activeAudioSources.Count > 0)
        {
            AudioSource audioSource = activeAudioSources[0];
            activeAudioSources.RemoveAt(0);
            audioSource.clip = null;
            audioSourcePool.Enqueue(audioSource);
        }
    }

    private void OnDestroy()
    {
        StopAllSounds();
    }
}
