using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex; // ������ ���� ����������� ����� � ������� checkpoints RaceManager

    RaceManager raceManager;
    void Start()
    {
        raceManager = FindObjectOfType<RaceManager>();
    }
    void OnTriggerEnter(Collider other)
    {
        if (raceManager != null && raceManager.raceStarted)
        {
            raceManager.OnCheckpointReached(other.gameObject, checkpointIndex);
        }
    }
}
