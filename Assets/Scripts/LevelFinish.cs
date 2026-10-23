using UnityEngine;

public class LevelFinish : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (LevelEndManager.Instance != null)
        {
            LevelEndManager.Instance.GameWin();
        }
    }
}