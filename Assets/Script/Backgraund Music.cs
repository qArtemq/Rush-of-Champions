using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgraundMusic : MonoBehaviour
{
    [SerializeField] AudioClip[] musicBackGround;
    AudioSource musicSource;
    private List<int> trackHistory = new List<int>();
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
    public void NextTrack()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        PlayRandomTrack();
    }
    public void PreviousTrack()
    {
        if (trackHistory.Count > 1 && currentTrackIndex > 0)
        {
            currentTrackIndex--;
            int previousIndex = trackHistory[currentTrackIndex];

            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            musicSource.clip = musicBackGround[previousIndex];
            musicSource.Play();
        }
    }
    void PlayRandomTrack()
    {
        if (musicBackGround.Length == 0)
            return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, musicBackGround.Length);
        } while (trackHistory.Count > 0 && newIndex == trackHistory[trackHistory.Count - 1] && musicBackGround.Length > 1);

        trackHistory.Add(newIndex);
        currentTrackIndex = trackHistory.Count - 1;

        musicSource.clip = musicBackGround[newIndex];
        musicSource.Play();
    }
}
