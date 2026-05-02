using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    public string finishMessage = "EchoPulse prototype complete.";
    public bool freezePlayerOnFinish;
    public LevelGoal levelGoal;

    private bool completed;

    public bool IsComplete
    {
        get { return completed; }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryComplete(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryComplete(other);
    }

    private void TryComplete(Collider other)
    {
        if (completed)
        {
            return;
        }

        FirstPersonWalkController player = other.GetComponentInParent<FirstPersonWalkController>();
        if (player == null)
        {
            return;
        }

        completed = true;
        Debug.Log(finishMessage);

        if (levelGoal != null)
        {
            levelGoal.CompleteGoal();
        }

        if (freezePlayerOnFinish)
        {
            player.enabled = false;
        }
    }
}
