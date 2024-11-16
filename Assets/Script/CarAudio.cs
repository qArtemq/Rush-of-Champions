using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAudio : MonoBehaviour
    {
        // This script reads some of the car's current properties and plays sounds accordingly.
        // The engine sound can be a simple single clip which is looped and pitched, or it
        // can be a crossfaded blend of four clips which represent the timbre of the engine
        // at different RPM and Throttle state.

        // the engine clips should all be a steady pitch, not rising or falling.

        // when using four channel engine crossfading, the four clips should be:
        // lowAccelClip : The engine at low revs, with throttle open (i.e. begining acceleration at very low speed)
        // highAccelClip : Thenengine at high revs, with throttle open (i.e. accelerating, but almost at max speed)
        // lowDecelClip : The engine at low revs, with throttle at minimum (i.e. idling or engine-braking at very low speed)
        // highDecelClip : Thenengine at high revs, with throttle at minimum (i.e. engine-braking at very high speed)

        // For proper crossfading, the clips pitches should all match, with an octave offset between low and high.


        public enum EngineAudioOptions // Options for the engine audio
        {
            Simple, // Simple style audio
            FourChannel // four Channel audio
        }

        public EngineAudioOptions engineSoundStyle = EngineAudioOptions.FourChannel;// Set the default audio options to be four channel
        public AudioClip lowAccelClip;                                              // Audio clip for low acceleration
        public AudioClip lowDecelClip;                                              // Audio clip for low deceleration
        public AudioClip highAccelClip;                                             // Audio clip for high acceleration
        public AudioClip highDecelClip;                                             // Audio clip for high deceleration
        public float pitchMultiplier = 1f;                                          // Used for altering the pitch of audio clips
        public float lowPitchMin = 1f;                                              // The lowest possible pitch for the low sounds
        public float lowPitchMax = 6f;                                              // The highest possible pitch for the low sounds
        public float highPitchMultiplier = 0.25f;                                   // Used for altering the pitch of high sounds
        public float maxRolloffDistance = 100;                                      // The maximum distance where rollof starts to take place
        public float dopplerLevel = 1;                                              // The mount of doppler effect used in the audio
        public bool useDoppler = true;                                              // Toggle for using doppler

        private AudioSource m_LowAccel; // Source for the low acceleration sounds
        private AudioSource m_LowDecel; // Source for the low deceleration sounds
        private AudioSource m_HighAccel; // Source for the high acceleration sounds
        private AudioSource m_HighDecel; // Source for the high deceleration sounds
        private bool m_StartedSound; // flag for knowing if we have started sounds
        private CarController m_CarController; // Reference to car we are controlling
        public AudioMixerGroup audioMixerGroupGeneral;

        RaceManager m_RaceManager;
        CarCam m_CarCam;

        private void Awake()
        {
            m_RaceManager = FindObjectOfType<RaceManager>();
            // �������� carcontroller (��� �� ����� null, ��� ��� � ��� ���� ��������� require)
            m_CarController = GetComponent<CarController>();
            m_CarCam = FindObjectOfType<CarCam>();
        }
        private void StartSound()
        {
            // ��������� �������� ��������� �����
            m_HighAccel = SetUpEngineAudioSource(highAccelClip);

            // ���� � ��� ���������������� ����, ��������� ������ ��������� �����
            if (engineSoundStyle == EngineAudioOptions.FourChannel)
            {
                m_LowAccel = SetUpEngineAudioSource(lowAccelClip);
                m_LowDecel = SetUpEngineAudioSource(lowDecelClip);
                m_HighDecel = SetUpEngineAudioSource(highDecelClip);
            }

            // ���� ����, ��� �� ������ ��������������� ������
            m_StartedSound = true;
        }


        private void StopSound()
        {
            //���������� ��� ��������� ����� �� ���� �������:
            foreach (var source in GetComponents<AudioSource>())
            {
                Destroy(source);
            }

            m_StartedSound = false;
        }


        // Update is called once per frame
        private void Update()
        {
            if (m_RaceManager.playerFinished)
            {
                maxRolloffDistance = 50;
            }

            // ��������� maxDistance ��� ���� ���������� �����
            if (m_LowAccel != null) m_LowAccel.maxDistance = maxRolloffDistance;
            if (m_LowDecel != null) m_LowDecel.maxDistance = maxRolloffDistance;
            if (m_HighAccel != null) m_HighAccel.maxDistance = maxRolloffDistance;
            if (m_HighDecel != null) m_HighDecel.maxDistance = maxRolloffDistance;

            if (m_CarController.isPlayerCar)
            {
                // �������� ���������� �� �������� ������
                float camDist = (m_CarCam.Endcamera.transform.position - transform.position).sqrMagnitude;
                // ��������� �� ���������� ���������� ��� ������� � ��������� �����
                if (m_StartedSound && camDist > maxRolloffDistance * maxRolloffDistance)
                {
                    StopSound();
                }
            }
            
            
            if (!m_StartedSound)
            {
                StartSound(); // ��� ���������, ������ StartSound() �� ������ �������
            }

            if (m_StartedSound)
            {
                // ��� ��������������� ����� ����������� � ������������ ���������� � ������������ � ��������� ����������.
                float pitch = ULerp(lowPitchMin, lowPitchMax, m_CarController.Revs);

                // ������� ����������� ��� (�������� ��������, �� ��������� ������������ ��� ��� ������� �������� ��� ���������)
                pitch = Mathf.Min(lowPitchMax, pitch);

                if (engineSoundStyle == EngineAudioOptions.Simple)
                {
                    // ��� 1-���������� ����� ���������, ��� ����� ������:
                    m_HighAccel.pitch = pitch * pitchMultiplier * highPitchMultiplier;
                    m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_HighAccel.volume = 1;
                }
                else
                {
                    // ��� 4-���������� ����� ���������, ��� ������� �������:

                    // ������������� ���� ������� �� ������ ����������
                    m_LowAccel.pitch = pitch * pitchMultiplier;
                    m_LowDecel.pitch = pitch * pitchMultiplier;
                    m_HighAccel.pitch = pitch * highPitchMultiplier * pitchMultiplier;
                    m_HighDecel.pitch = pitch * highPitchMultiplier * pitchMultiplier;

                    // �������� �������� ��� ��������� ������ � ����������� �� ���������
                    float accFade = Mathf.Abs(m_CarController.AccelInput);
                    float decFade = 1 - accFade;

                    // �������� �������� high fade �� ������ �������� ����������
                    float highFade = Mathf.InverseLerp(0.2f, 0.8f, m_CarController.Revs);
                    float lowFade = 1 - highFade;

                    // �������������� ��������, ����� ��� ���� ����� �������������
                    highFade = 1 - ((1 - highFade) * (1 - highFade));
                    lowFade = 1 - ((1 - lowFade) * (1 - lowFade));
                    accFade = 1 - ((1 - accFade) * (1 - accFade));
                    decFade = 1 - ((1 - decFade) * (1 - decFade));

                    // ������������� ��������� ��������� �� ������ �������� ���������
                    m_LowAccel.volume = lowFade * accFade;
                    m_LowDecel.volume = lowFade * decFade;
                    m_HighAccel.volume = highFade * accFade;
                    m_HighDecel.volume = highFade * decFade;

                    // ��������� ������� �������
                    m_HighAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_LowAccel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_HighDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                    m_LowDecel.dopplerLevel = useDoppler ? dopplerLevel : 0;
                }
            }
        }


        // ������������� � ��������� ����� �������� ����� � ������ gane
        private AudioSource SetUpEngineAudioSource(AudioClip clip)
        {
            // �������� ����� ��������� ��������� ����� �� ������� ������� � ��������� ��� ��������
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = 0;
            source.loop = true;

            // start the clip from a random point
            source.time = Random.Range(0f, clip.length);
            source.Play();
            source.minDistance = 5;
            source.maxDistance = maxRolloffDistance;
            source.spatialBlend = 1;
            source.dopplerLevel = 0;

            source.outputAudioMixerGroup = audioMixerGroupGeneral;
            return source;
        }


        // ��������� ������ Lerp � Inverse Lerp, ����� ��������� �������� ����� �� ������� ��������� �� ��
        private static float ULerp(float from, float to, float value)
        {
            return (1.0f - value) * from + value * to;
        }
    }
}
