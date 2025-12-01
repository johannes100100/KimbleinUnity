using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    public Dice dice;

    // Called by the Roll Dice button
    public void OnRollDice()
    {
        GameManager.Instance.PlayerRoll();
    }
}
