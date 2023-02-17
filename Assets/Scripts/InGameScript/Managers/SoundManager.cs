using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : SingletonBehavior<SoundManager>
{
    [SerializeField] private AudioSource audioSource;
    public AudioSource AudioSource => audioSource;

    public void SetAudioVolume(float volume)
    {
        AudioSource.volume = volume;
    }
}
