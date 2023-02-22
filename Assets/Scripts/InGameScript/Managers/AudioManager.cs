using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : SingletonBehavior<AudioManager>
{
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private float volume = 1.0f;


    public void HitSound(AkgRigidbody akg)
    {
        AudioClip clip = null;
        if (akg.layerMask.HasFlag(AkgLayerMask.STONE))
        {
            int index = UnityEngine.Random.Range(0, hitSounds.Length);
            clip = hitSounds[index];
        }

        if (clip != null)
        {
            GetComponent<AudioSource>().PlayOneShot(clip, volume);
        }
    }

    public void SetAudioVolume(float volume)
    {
        this.volume = volume;
        GetComponent<AudioSource>().volume = volume;
    }
}
