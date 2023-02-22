using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : SingletonBehavior<AudioManager>
{
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioClip wallDestroySound;
    [SerializeField] private float volume = 1.0f;


    public void HitSound(AkgRigidbody akg)
    {
        AudioClip clip = null;
        if (akg.layerMask.HasFlag(AkgLayerMask.STONE))
        {
            int index = UnityEngine.Random.Range(0, hitSounds.Length);
            clip = hitSounds[index];
        }
        else
        {
            clip = wallDestroySound;
        }

        if (clip != null)
        {
            GetComponent<AudioSource>().PlayOneShot(clip, volume);
        }
    }
    public void DestroySound()
    {
        GetComponent<AudioSource>().PlayOneShot(destroySound, volume*0.3f);
    }

    public void SetAudioVolume(float volume)
    {
        this.volume = volume;
        GetComponent<AudioSource>().volume = volume;
    }
}
