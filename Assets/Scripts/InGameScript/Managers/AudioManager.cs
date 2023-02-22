using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : SingletonBehavior<AudioManager>
{
    [SerializeField] private AudioClip[] hitSounds;

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
            Debug.Log("in");
            GetComponent<AudioSource>().PlayOneShot(clip);
        }
    }
}
