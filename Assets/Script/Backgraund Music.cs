using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgraundMusic : MonoBehaviour
{
    [SerializeField] AudioClip[] musicBackGround;
    AudioSource musicSource;
    private List<int> trackHistory = new List<int>(); // История проигранных треков
    private int currentTrackIndex = -1;

    void Start()
    {
        musicSource = GetComponent<AudioSource>();
        musicSource.playOnAwake = true;
        musicSource.loop = false;

        PlayRandomTrack();
    }

    void Update()
    {
        // Проверяем, закончился ли текущий трек
        if (!musicSource.isPlaying)
        {
            PlayRandomTrack();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            PreviousTrack();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            NextTrack();
        }
    }
    // Метод для переключения на следующий трек
    public void NextTrack()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        PlayRandomTrack();
    }
    // Метод для переключения на предыдущий трек
    public void PreviousTrack()
    {
        if (trackHistory.Count > 1 && currentTrackIndex > 0)
        {
            currentTrackIndex--; // Переходим к предыдущему треку
            int previousIndex = trackHistory[currentTrackIndex];

            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            // Воспроизводим предыдущий трек
            musicSource.clip = musicBackGround[previousIndex];
            musicSource.Play();
        }
    }
    // Метод для воспроизведения случайного трека
    void PlayRandomTrack()
    {
        if (musicBackGround.Length == 0)
            return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, musicBackGround.Length);
        } while (trackHistory.Count > 0 && newIndex == trackHistory[trackHistory.Count - 1] && musicBackGround.Length > 1);

        // Сохраняем трек в истории
        trackHistory.Add(newIndex);
        currentTrackIndex = trackHistory.Count - 1;

        // Воспроизводим трек
        musicSource.clip = musicBackGround[newIndex];
        musicSource.Play();
    }
}
