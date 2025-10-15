using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAudio : MonoBehaviour
    {
        public enum EngineAudioOptions
        {
            Simple,
            FourChannel
        }

        public EngineAudioOptions engineSoundStyle = EngineAudioOptions.FourChannel;
        public AudioClip lowAccelClip; 
        public AudioClip lowDecelClip;   
        public AudioClip highAccelClip; 
        public AudioClip highDecelClip;   
        public float pitchMultiplier = 1f;
        public float lowPitchMin = 1f; 
        public float lowPitchMax = 6f;
        public float highPitchMultiplier = 0.25f;
        public float maxRolloffDistance = 100;
        public float dopplerLevel = 1;
        public bool useDoppler = true;

        private AudioSource m_LowAccel;
        private AudioSource m_LowDecel;
        private AudioSource m_HighAccel;
        private AudioSource m_HighDecel;
        private bool m_StartedSound;
        private CarController m_CarController;
        public AudioMixerGroup audioMixerGroupGeneral;

        RaceManager m_RaceManager;
        CarCam m_CarCam;

        private void Awake()
        {
            m_RaceManager = FindObjectOfType<RaceManager>();
            m_CarController = GetComponent<CarController>();
            m_CarCam = FindObjectOfType<CarCam>();
        }
        private void StartSound()
        {
            m_HighAccel = SetUpEngineAudioSource(highAccelClip);

            if (engineSoundStyle == EngineAudioOptions.FourChannel)
            {
                m_LowAccel = SetUpEngineAudioSource(lowAccelClip);
                m_LowDecel = SetUpEngineAudioSource(lowDecelClip);
                m_HighDecel = SetUpEngineAudioSource(highDecelClip);
            }

            m_StartedSound = true;
        }


        private void StopSound()
        {
            foreach (var source in GetComponents<AudioSource>())
            {
                Destroy(source);
            }

            m_StartedSound = false;
        }


        private void Update()
        {
            if (m_RaceManager.playerFinished)
            {
                maxRolloffDistance = 50;
            }

            if (m_LowAccel != null) m_LowAccel.maxDistance = maxRolloffDistance;
            if (m_LowDecel != null) m_LowDecel.maxDistance = maxRolloffDistance;
            if (m_HighAccel != null) m_HighAccel.maxDistance = maxRolloffDistance;
            if (m_HighDecel != null) m_HighDecel.maxDistance = maxRolloffDistance;

            if (m_CarController.isPlayerCar)
            {
                float camDist = (m_CarCam.Endcamera.transform.position - transform.position).sqrMagnitude;
                if (m_StartedSound && camDist > maxRolloffDistance * maxRolloffDistance)
                {
                    StopSound();
                }
            }
            
            
            if (!m_StartedSound)
            {
                StartSound();
            }

            if (m_StartedSound)
            {
                float pitch = ULerp(lowPitchMin, lowPitchMax, m_CarController.Revs);

                pitch = Mathf.Min(lowPitchMax, pitch);

                if (engineSoundStyle == EngineAudioOptions.Simple)
                {
                    m_HighAccel.pitch = pitch * pitchMultiplier * highPitchMultiplier;
                    m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_HighAccel.volume = 1;
                }
                else
                {

                    m_LowAccel.pitch = pitch * pitchMultiplier;
                    m_LowDecel.pitch = pitch * pitchMultiplier;
                    m_HighAccel.pitch = pitch * highPitchMultiplier * pitchMultiplier;
                    m_HighDecel.pitch = pitch * highPitchMultiplier * pitchMultiplier;

                    float accFade = Mathf.Abs(m_CarController.AccelInput);
                    float decFade = 1 - accFade;

                    float highFade = Mathf.InverseLerp(0.2f, 0.8f, m_CarController.Revs);
                    float lowFade = 1 - highFade;

                    highFade = 1 - ((1 - highFade) * (1 - highFade));
                    lowFade = 1 - ((1 - lowFade) * (1 - lowFade));
                    accFade = 1 - ((1 - accFade) * (1 - accFade));
                    decFade = 1 - ((1 - decFade) * (1 - decFade));

                    m_LowAccel.volume = lowFade * accFade;
                    m_LowDecel.volume = lowFade * decFade;
                    m_HighAccel.volume = highFade * accFade;
                    m_HighDecel.volume = highFade * decFade;

                    m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_LowAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_HighDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_LowDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                }
            }
        }

        private AudioSource SetUpEngineAudioSource(AudioClip clip)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = 0;
            source.loop = true;

            source.time = Random.Range(0f, clip.length);
            source.Play();
            source.minDistance = 5;
            source.maxDistance = maxRolloffDistance;
            source.spatialBlend = 1;
            source.dopplerLevel = 0;

            source.outputAudioMixerGroup = audioMixerGroupGeneral;
            return source;
        }


        private static float ULerp(float from, float to, float value)
        {
            return (1.0f - value) * from + value * to;
        }
    }
}
