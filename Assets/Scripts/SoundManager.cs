using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static void PlaySound(string soundName)
    {
        // Load the audio clip from Resources/Sounds
        AudioClip clip = Resources.Load<AudioClip>("Sounds/" + soundName);
        
        if (clip == null)
        {
            Debug.LogWarning($"Sound '{soundName}' not found in Resources/Sounds");
            return;
        }
        
        // Default values
        float volume = 1f;
        float pitch = 1f;
        bool loop = false;
        
        // Check for special behavior for specific sounds
        switch (soundName)
        {
            case "Pair_Fail":
                volume = 0.7f;
                break;
        }
        
        // Create a temporary GameObject with an AudioSource
        GameObject soundObject = new GameObject("Sound_" + soundName);
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();
        
        // Configure the AudioSource
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.loop = loop;
        
        // Play the sound
        audioSource.Play();
        
        // Clean up: destroy the GameObject after the clip finishes (unless looping)
        if (!loop)
        {
            Object.Destroy(soundObject, clip.length / pitch);
        }
    }
}
