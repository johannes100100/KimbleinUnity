using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player Info")]
    public string playerName;
    public Piece[] pieces;

    [Header("Home positions")]
    public Transform[] homePositions;

    [Header("Board Orientation")]
    public float boardAngle;   // Angle where the board rotates for this player

    private void Start()
    {
        // Safety checks for missing references
        if (pieces == null)
        {
            Debug.LogWarning(playerName + ": pieces-array is missing!");
            return;
        }

        if (homePositions == null || homePositions.Length == 0)
        {
            Debug.LogWarning(playerName + ": homePositions not assigned!");
        }

        Debug.Log(playerName + " ready with " + pieces.Length + " pieces");

        // Place pieces into the correct home slots at game start
        InitializePiecesToHome();
    }

    // --------------------------------------------------
    // INITIAL SETUP
    // --------------------------------------------------

    private void InitializePiecesToHome()
    {
        if (homePositions == null || homePositions.Length == 0)
            return;

        // Assign each piece to its corresponding home slot
        for (int i = 0; i < pieces.Length && i < homePositions.Length; i++)
        {
            if (pieces[i] == null)
                continue;

            pieces[i].homePosition = homePositions[i];
            pieces[i].MoveToHome();
        }
    }

    // --------------------------------------------------
    // DEBUG PRINT
    // --------------------------------------------------

    public void SetRollResult(int result)
    {
        // Simple debug output for dice rolls
        Debug.Log(playerName + " heitti " + result);
    }

    // --------------------------------------------------
    // FIND THE FIRST FREE HOME SPOT
    // --------------------------------------------------

    public Transform GetFreeHomeSpot()
    {
        if (homePositions == null || homePositions.Length == 0)
            return null;

        // Loop through home positions and find one not occupied by a piece
        foreach (Transform spot in homePositions)
        {
            bool taken = false;

            foreach (Piece p in pieces)
            {
                if (p == null)
                    continue;

                // A piece with routeIndex -1 is in home
                if (p.routeIndex == -1 &&
                    Vector3.Distance(p.transform.position, spot.position) < 0.01f)
                {
                    taken = true;
                    break;
                }
            }

            if (!taken)
                return spot;
        }

        // Fallback if something goes wrong
        return homePositions[0];
    }
}
