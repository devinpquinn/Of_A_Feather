using UnityEngine;

public class SoundTrigger : MonoBehaviour
{
    public void PlaySound(string soundName)
    {
        SoundManager.PlaySound(soundName);
    }
}
