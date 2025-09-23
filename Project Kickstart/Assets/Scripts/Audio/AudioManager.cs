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
    
    [Header("Platform Movement Sounds")]
    [SerializeField] private AudioClip[] platformMovementSounds;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume dos sons de movimento das plataformas (0.0 a 1.0)")]
    private float platformMovementVolume = 0.6f;
    
    [Header("Flower Light Sounds")]
    [SerializeField] private AudioClip[] flowerEmissionToObjectSounds;
    [SerializeField] private AudioClip[] flowerEmissionToFlowerSounds;
    [SerializeField] private AudioClip[] flowerLightReceptionSounds;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume dos sons de emissão para objetos (0.0 a 1.0)")]
    private float flowerEmissionToObjectVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume dos sons de emissão para flores - caminho correto! (0.0 a 1.0)")]
    private float flowerEmissionToFlowerVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume dos sons de recepção de luz das flores (0.0 a 1.0)")]
    private float flowerReceptionVolume = 0.7f;
    
    [Header("Dimension Switch Sounds")]
    [SerializeField] private AudioClip[] dimensionSwitchSounds;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume específico para sons de troca de dimensão (0.0 a 1.0)")]
    private float dimensionSwitchVolume = 0.8f;
    [SerializeField] [Range(0f, 2f)] [Tooltip("Delay antes de tocar o som de troca de dimensão (em segundos)")]
    private float dimensionSwitchDelay = 0.0f;
    
    [Header("Guard Sounds")]
    [SerializeField] private AudioClip[] guardProximitySounds;
    [SerializeField] private AudioClip[] princessHitSounds;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume dos sons de proximidade do guarda (0.0 a 1.0)")]
    private float guardProximityVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume dos sons de hit na princesa (0.0 a 1.0)")]
    private float princessHitVolume = 0.8f;
    
    [Header("Background Music")]
    [SerializeField] private AudioClip dimensionABackgroundMusic;
    [SerializeField] private AudioClip dimensionBBackgroundMusic;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Volume da música de fundo (0.0 a 1.0)")]
    private float backgroundMusicVolume = 0.3f;
    [SerializeField] [Range(0f, 3f)] [Tooltip("Tempo de fade ao trocar música (em segundos)")]
    private float musicFadeTime = 1.5f;
    [SerializeField] private bool enableBackgroundMusic = true;
    
    [Header("Audio Sources")]
    [SerializeField] private int maxAudioSources = 10;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Singleton pattern for global access
    public static AudioManager Instance { get; private set; }
    
    // Pool of audio sources for efficiency
    private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
    private List<AudioSource> activeAudioSources = new List<AudioSource>();
    
    // Background music control
    private AudioSource backgroundMusicSource;
    private DimensionType currentMusicDimension = DimensionType.DimensionA;
    private bool isMusicFading = false;
    
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
    
    public float BackgroundMusicVolume 
    { 
        get => backgroundMusicVolume; 
        set => backgroundMusicVolume = Mathf.Clamp01(value); 
    }
    
    public bool EnableBackgroundMusic 
    { 
        get => enableBackgroundMusic; 
        set => enableBackgroundMusic = value; 
    }
    
    public float PlatformMovementVolume 
    { 
        get => platformMovementVolume; 
        set => platformMovementVolume = Mathf.Clamp01(value); 
    }
    
    public float FlowerEmissionToObjectVolume 
    { 
        get => flowerEmissionToObjectVolume; 
        set => flowerEmissionToObjectVolume = Mathf.Clamp01(value); 
    }
    
    public float FlowerEmissionToFlowerVolume 
    { 
        get => flowerEmissionToFlowerVolume; 
        set => flowerEmissionToFlowerVolume = Mathf.Clamp01(value); 
    }
    
    public float FlowerReceptionVolume 
    { 
        get => flowerReceptionVolume; 
        set => flowerReceptionVolume = Mathf.Clamp01(value); 
    }
    
    public float GuardProximityVolume 
    { 
        get => guardProximityVolume; 
        set => guardProximityVolume = Mathf.Clamp01(value); 
    }
    
    public float PrincessHitVolume 
    { 
        get => princessHitVolume; 
        set => princessHitVolume = Mathf.Clamp01(value); 
    }

    private void Awake()
    {
        // Singleton implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            InitializeBackgroundMusic();
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
        
        // Subscribe to dimension changes to handle background music
        DimensionManager.OnDimensionChanged += OnDimensionChanged;
        
        // Start background music for initial dimension
        if (enableBackgroundMusic)
        {
            StartBackgroundMusic(DimensionType.DimensionA);
        }
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
    /// Play platform movement sound (for up/down movement)
    /// </summary>
    /// <param name="position">World position where sound should play</param>
    /// <param name="isMovingDown">True if platform is moving down, false if moving up</param>
    public void PlayPlatformMovementSound(Vector3 position, bool isMovingDown = true)
    {
        if (platformMovementSounds == null || platformMovementSounds.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("No platform movement sounds configured!");
            return;
        }

        // Select random platform movement sound
        AudioClip clipToPlay = platformMovementSounds[Random.Range(0, platformMovementSounds.Length)];
        
        // Calculate volume and pitch
        float volume = platformMovementVolume * sfxVolume * masterVolume;
        float pitch = isMovingDown ? 0.9f : 1.1f; // Slightly lower pitch for down, higher for up
        
        PlaySoundAtPosition(clipToPlay, position, volume, pitch);
        
        if (showDebugInfo)
        {
            Debug.Log($"Playing platform movement sound at {position} (moving {(isMovingDown ? "down" : "up")}) with volume {volume} and pitch {pitch}");
        }
    }

    /// <summary>
    /// Play flower light emission sound to object (when flower emits light to non-flower objects)
    /// </summary>
    /// <param name="position">World position where sound should play</param>
    /// <param name="targetName">Name of target for debug purposes</param>
    public void PlayFlowerEmissionToObjectSound(Vector3 position, string targetName = "unknown")
    {
        if (flowerEmissionToObjectSounds == null || flowerEmissionToObjectSounds.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("No flower emission to object sounds configured!");
            return;
        }

        // Select random flower emission to object sound
        AudioClip clipToPlay = flowerEmissionToObjectSounds[Random.Range(0, flowerEmissionToObjectSounds.Length)];
        
        // Calculate volume and pitch
        float volume = flowerEmissionToObjectVolume * sfxVolume * masterVolume;
        float pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation for variety
        
        PlaySoundAtPosition(clipToPlay, position, volume, pitch);
        
        if (showDebugInfo)
        {
            Debug.Log($"Playing flower emission to OBJECT sound at {position} targeting {targetName} with volume {volume}");
        }
    }

    /// <summary>
    /// Play flower light emission sound to another flower (indicates correct puzzle path!)
    /// </summary>
    /// <param name="position">World position where sound should play</param>
    /// <param name="targetFlowerName">Name of target flower for debug purposes</param>
    public void PlayFlowerEmissionToFlowerSound(Vector3 position, string targetFlowerName = "unknown")
    {
        if (flowerEmissionToFlowerSounds == null || flowerEmissionToFlowerSounds.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("No flower emission to flower sounds configured!");
            return;
        }

        // Select random flower emission to flower sound
        AudioClip clipToPlay = flowerEmissionToFlowerSounds[Random.Range(0, flowerEmissionToFlowerSounds.Length)];
        
        // Calculate volume and pitch - higher volume and pitch for positive feedback
        float volume = flowerEmissionToFlowerVolume * sfxVolume * masterVolume;
        float pitch = Random.Range(1.1f, 1.3f); // Higher pitch for correct path feedback
        
        PlaySoundAtPosition(clipToPlay, position, volume, pitch);
        
        if (showDebugInfo)
        {
            Debug.Log($"Playing flower emission to FLOWER sound at {position} targeting {targetFlowerName} with volume {volume} - CORRECT PUZZLE PATH!");
        }
    }

    /// <summary>
    /// Play flower light reception sound (when flower receives light - indicates correct path)
    /// </summary>
    /// <param name="position">World position where sound should play</param>
    /// <param name="flowerName">Name of flower for debug purposes</param>
    public void PlayFlowerLightReceptionSound(Vector3 position, string flowerName = "unknown")
    {
        if (flowerLightReceptionSounds == null || flowerLightReceptionSounds.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("No flower light reception sounds configured!");
            return;
        }

        // Select random flower reception sound
        AudioClip clipToPlay = flowerLightReceptionSounds[Random.Range(0, flowerLightReceptionSounds.Length)];
        
        // Calculate volume and pitch 
        float volume = flowerReceptionVolume * sfxVolume * masterVolume;
        float pitch = Random.Range(1.0f, 1.2f); // Slightly higher pitch for positive feedback
        
        PlaySoundAtPosition(clipToPlay, position, volume, pitch);
        
        if (showDebugInfo)
        {
            Debug.Log($"Playing flower reception sound for {flowerName} at {position} with volume {volume} (correct path!)");
        }
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

    /// <summary>
    /// Initialize background music audio source
    /// </summary>
    private void InitializeBackgroundMusic()
    {
        GameObject musicSourceGO = new GameObject("BackgroundMusicSource");
        musicSourceGO.transform.SetParent(transform);
        backgroundMusicSource = musicSourceGO.AddComponent<AudioSource>();
        backgroundMusicSource.playOnAwake = false;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.spatialBlend = 0f; // 2D sound
        backgroundMusicSource.volume = 0f; // Start silent, will fade in
    }

    /// <summary>
    /// Handle dimension change events to switch background music
    /// </summary>
    private void OnDimensionChanged(DimensionType newDimension)
    {
        if (!enableBackgroundMusic) return;
        
        if (showDebugInfo)
        {
            Debug.Log($"AudioManager: Dimension changed to {newDimension}, switching background music");
        }
        
        ChangeBackgroundMusic(newDimension);
    }

    /// <summary>
    /// Start background music for a specific dimension
    /// </summary>
    public void StartBackgroundMusic(DimensionType dimension)
    {
        if (!enableBackgroundMusic || backgroundMusicSource == null) return;
        
        AudioClip musicClip = GetBackgroundMusicForDimension(dimension);
        if (musicClip == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"No background music configured for {dimension}");
            return;
        }
        
        backgroundMusicSource.clip = musicClip;
        backgroundMusicSource.volume = backgroundMusicVolume * masterVolume;
        backgroundMusicSource.Play();
        currentMusicDimension = dimension;
        
        if (showDebugInfo)
        {
            Debug.Log($"Started background music for {dimension}: {musicClip.name}");
        }
    }

    /// <summary>
    /// Change background music with fade transition
    /// </summary>
    public void ChangeBackgroundMusic(DimensionType newDimension)
    {
        if (!enableBackgroundMusic || backgroundMusicSource == null) return;
        if (newDimension == currentMusicDimension) return; // Already playing this dimension's music
        
        AudioClip newMusicClip = GetBackgroundMusicForDimension(newDimension);
        if (newMusicClip == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"No background music configured for {newDimension}");
            return;
        }
        
        if (isMusicFading) return; // Prevent multiple fade operations
        
        StartCoroutine(FadeBackgroundMusic(newMusicClip, newDimension));
    }

    /// <summary>
    /// Get background music clip for a specific dimension
    /// </summary>
    private AudioClip GetBackgroundMusicForDimension(DimensionType dimension)
    {
        return dimension == DimensionType.DimensionA ? dimensionABackgroundMusic : dimensionBBackgroundMusic;
    }

    /// <summary>
    /// Coroutine to fade between background music tracks
    /// </summary>
    private System.Collections.IEnumerator FadeBackgroundMusic(AudioClip newClip, DimensionType newDimension)
    {
        isMusicFading = true;
        float targetVolume = backgroundMusicVolume * masterVolume;
        
        if (showDebugInfo)
        {
            Debug.Log($"Fading background music from {currentMusicDimension} to {newDimension}");
        }
        
        // Fade out current music
        if (backgroundMusicSource.isPlaying)
        {
            float startVolume = backgroundMusicSource.volume;
            float fadeOutTime = musicFadeTime * 0.5f; // Half time for fade out
            
            for (float t = 0; t < fadeOutTime; t += Time.deltaTime)
            {
                backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutTime);
                yield return null;
            }
            
            backgroundMusicSource.volume = 0f;
            backgroundMusicSource.Stop();
        }
        
        // Switch to new music
        backgroundMusicSource.clip = newClip;
        backgroundMusicSource.Play();
        currentMusicDimension = newDimension;
        
        // Fade in new music
        float fadeInTime = musicFadeTime * 0.5f; // Half time for fade in
        
        for (float t = 0; t < fadeInTime; t += Time.deltaTime)
        {
            backgroundMusicSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeInTime);
            yield return null;
        }
        
        backgroundMusicSource.volume = targetVolume;
        isMusicFading = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"Background music fade complete: Now playing {newClip.name} for {newDimension}");
        }
    }

    /// <summary>
    /// Stop background music
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Stop();
            if (showDebugInfo)
            {
                Debug.Log("Background music stopped");
            }
        }
    }

    /// <summary>
    /// Test background music system
    /// </summary>
    [ContextMenu("Test Background Music A")]
    public void TestBackgroundMusicA()
    {
        ChangeBackgroundMusic(DimensionType.DimensionA);
    }

    /// <summary>
    /// Test background music system
    /// </summary>
    [ContextMenu("Test Background Music B")]
    public void TestBackgroundMusicB()
    {
        ChangeBackgroundMusic(DimensionType.DimensionB);
    }

    /// <summary>
    /// Test platform movement sound (down)
    /// </summary>
    [ContextMenu("Test Platform Sound Down")]
    public void TestPlatformSoundDown()
    {
        PlayPlatformMovementSound(transform.position, true);
    }

    /// <summary>
    /// Test platform movement sound (up)
    /// </summary>
    [ContextMenu("Test Platform Sound Up")]
    public void TestPlatformSoundUp()
    {
        PlayPlatformMovementSound(transform.position, false);
    }

    /// <summary>
    /// Test flower emission to object sound
    /// </summary>
    [ContextMenu("Test Flower Emission to Object")]
    public void TestFlowerEmissionToObjectSound()
    {
        PlayFlowerEmissionToObjectSound(transform.position, "TestObject");
    }

    /// <summary>
    /// Test flower emission to flower sound (correct path)
    /// </summary>
    [ContextMenu("Test Flower Emission to Flower")]
    public void TestFlowerEmissionToFlowerSound()
    {
        PlayFlowerEmissionToFlowerSound(transform.position, "TestFlower");
    }

    /// <summary>
    /// Test flower light reception sound
    /// </summary>
    [ContextMenu("Test Flower Reception Sound")]
    public void TestFlowerReceptionSound()
    {
        PlayFlowerLightReceptionSound(transform.position, "TestFlower");
    }

    /// <summary>
    /// Play guard proximity sound (when guard gets close to princess during charge)
    /// </summary>
    /// <param name="position">World position where sound should play</param>
    public void PlayGuardProximitySound(Vector3 position)
    {
        if (guardProximitySounds == null || guardProximitySounds.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("No guard proximity sounds configured!");
            return;
        }

        // Select random guard proximity sound
        AudioClip clipToPlay = guardProximitySounds[Random.Range(0, guardProximitySounds.Length)];
        
        // Calculate volume and pitch
        float volume = guardProximityVolume * sfxVolume * masterVolume;
        float pitch = Random.Range(0.8f, 1.2f); // Slight pitch variation for menacing effect
        
        PlaySoundAtPosition(clipToPlay, position, volume, pitch);
        
        if (showDebugInfo)
        {
            Debug.Log($"Playing guard proximity sound at {position} with volume {volume}");
        }
    }

    /// <summary>
    /// Play princess hit sound (when guard catches the princess)
    /// </summary>
    /// <param name="position">World position where sound should play</param>
    public void PlayPrincessHitSound(Vector3 position)
    {
        if (princessHitSounds == null || princessHitSounds.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("No princess hit sounds configured!");
            return;
        }

        // Select random princess hit sound
        AudioClip clipToPlay = princessHitSounds[Random.Range(0, princessHitSounds.Length)];
        
        // Calculate volume and pitch
        float volume = princessHitVolume * sfxVolume * masterVolume;
        float pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation
        
        PlaySoundAtPosition(clipToPlay, position, volume, pitch);
        
        if (showDebugInfo)
        {
            Debug.Log($"Playing princess hit sound at {position} with volume {volume}");
        }
    }

    /// <summary>
    /// Test guard proximity sound
    /// </summary>
    [ContextMenu("Test Guard Proximity Sound")]
    public void TestGuardProximitySound()
    {
        PlayGuardProximitySound(transform.position);
    }

    /// <summary>
    /// Test princess hit sound
    /// </summary>
    [ContextMenu("Test Princess Hit Sound")]
    public void TestPrincessHitSound()
    {
        PlayPrincessHitSound(transform.position);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        DimensionManager.OnDimensionChanged -= OnDimensionChanged;
        
        StopAllSounds();
        StopBackgroundMusic();
    }
}
