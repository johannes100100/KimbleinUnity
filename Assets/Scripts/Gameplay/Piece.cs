using UnityEngine;
using System.Collections;

public class Piece : MonoBehaviour
{
    public int boardIndex = -1;
    public Transform homePosition;

    public Transform startTile;
    [Tooltip("All tiles this piece moves through in order from start tile to goal")]
    public Transform[] routeTiles;
    public int routeIndex = -1;

    public GameObject glowObject;
    private bool glowing = false;
    private float glowSpeed = 2f;
    private float glowAlpha = 0f;
    private SpriteRenderer glowRenderer;

    private bool canBeClicked = false;

    public Player ownerPlayer;

    public int goalCount = 4; // number of goal tiles at end of route

    private SpriteRenderer spriteRenderer;
    private int originalSortingOrder;

    // ---------------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------------

    void Start()
    {
        if (glowObject != null)
            glowRenderer = glowObject.GetComponent<SpriteRenderer>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSortingOrder = spriteRenderer.sortingOrder;
    }

    // ---------------------------------------------------
    // BASIC POSITION HELPERS
    // ---------------------------------------------------

    public void MoveToTile(Transform tile)
    {
        transform.position = tile.position;
    }

    public void MoveToHome()
    {
        routeIndex = -1;

        if (homePosition != null)
            transform.position = homePosition.position;
        else
            Debug.LogWarning(name + ": homePosition missing!");
    }

    // ---------------------------------------------------
    // CLICK HANDLING
    // ---------------------------------------------------

    void OnMouseDown()
    {
        // Only allow clicking if piece is selectable and game is running
        if (canBeClicked && GameManager.Instance.gameStarted)
        {
            GameManager.Instance.PieceSelected(this);
        }
    }

    public void EnableSelection(bool enabled)
    {
        canBeClicked = enabled;
        SetGlow(enabled);
    }

    // ---------------------------------------------------
    // GLOW EFFECT
    // ---------------------------------------------------

    public void SetGlow(bool state)
    {
        glowing = state;

        if (glowObject != null)
            glowObject.SetActive(state);
    }

    void Update()
    {
        // Animate glow alpha with sine wave
        if (glowing && glowRenderer != null)
        {
            glowAlpha = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f;
            Color c = glowRenderer.color;
            c.a = glowAlpha * 0.8f;
            glowRenderer.color = c;
        }
    }

    // ---------------------------------------------------
    // MOVEMENT RULES
    // ---------------------------------------------------

    public bool IsInHome()
    {
        return routeIndex == -1;
    }

    public bool IsInGoal()
    {
        return routeIndex >= routeTiles.Length - goalCount;
    }

    public bool CanMove(int steps)
    {
        // Leaving home requires exactly a 6
        if (IsInHome())
        {
            if (steps != 6)
                return false;

            // Start tile blocked by own piece
            if (GameManager.Instance.IsTileOccupiedByOwnPiece(this, startTile))
                return false;

            return true;
        }

        int targetIndex = routeIndex + steps;

        // Can't overshoot route
        if (targetIndex >= routeTiles.Length)
            return false;

        Transform targetTile = routeTiles[targetIndex];

        // Can't land on own piece
        if (GameManager.Instance.IsTileOccupiedByOwnPiece(this, targetTile))
            return false;

        return true;
    }

    // ---------------------------------------------------
    // MOVEMENT + ANIMATION
    // ---------------------------------------------------

    public IEnumerator MoveAnimated(int steps, float moveSpeed = 0.12f)
    {
        RaiseSortingOrder();

        // ---------------------------------------------------
        // LEAVING HOME
        // ---------------------------------------------------
        if (IsInHome())
        {
            if (steps == 6)
            {
                // Start tile blocked?
                if (GameManager.Instance.IsTileOccupiedByOwnPiece(this, startTile))
                {
                    ResetSortingOrder();
                    yield break;
                }

                // Check enemy at start tile
                Piece enemy = GameManager.Instance.GetEnemyPieceOnTile(this, startTile);
                if (enemy != null)
                {
                    enemy.routeIndex = -1;
                    enemy.StartCoroutine(enemy.SendToHomeAnimated());
                }

                // Move onto start tile
                routeIndex = 0;
                yield return StartCoroutine(MoveToPosition(startTile.position, moveSpeed));
            }

            ResetSortingOrder();
            yield break;
        }

        // ---------------------------------------------------
        // NORMAL BOARD MOVEMENT
        // ---------------------------------------------------

        int finalIndex = routeIndex + steps;

        if (finalIndex >= routeTiles.Length)
        {
            ResetSortingOrder();
            yield break;
        }

        // Walk tile-by-tile without eating
        for (int i = 0; i < steps; i++)
        {
            int nextIndex = routeIndex + 1;

            Vector3 endPos = routeTiles[nextIndex].position;
            yield return StartCoroutine(MoveToPosition(endPos, moveSpeed));

            routeIndex = nextIndex;
            yield return new WaitForSeconds(0.03f);
        }

        // ---------------------------------------------------
        // FINAL TILE EATING (only after movement finishes)
        // ---------------------------------------------------

        Transform finalTile = routeTiles[finalIndex];

        if (!GameManager.Instance.boardRotating && routeIndex == finalIndex)
        {
            Piece finalEnemy = GameManager.Instance.GetEnemyPieceOnTile(this, finalTile);

            if (finalEnemy != null)
            {
                // Wait for enemy home animation to complete
                yield return finalEnemy.SendToHomeAnimated();
            }
        }

        ResetSortingOrder();
    }

    // ---------------------------------------------------
    // MOVING ANIMATION HELPER
    // ---------------------------------------------------

    private IEnumerator MoveToPosition(Vector3 target, float speed)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / speed;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }

    // ---------------------------------------------------
    // HOME JUMP ANIMATION (being eaten)
    // ---------------------------------------------------

    public IEnumerator SendToHomeAnimated()
    {
        RaiseSortingOrder();

        Transform homeSpot = ownerPlayer.GetFreeHomeSpot();
        Vector3 start = transform.position;
        Vector3 end = homeSpot.position;

        float t = 0f;
        Vector3 mid = (start + end) / 2f + Vector3.up * 0.8f;

        // Parabolic jump to home position
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;

            Vector3 a = Vector3.Lerp(start, mid, t);
            Vector3 b = Vector3.Lerp(mid, end, t);
            transform.position = Vector3.Lerp(a, b, t);

            yield return null;
        }

        // Finalize home placement
        routeIndex = -1;
        transform.position = end;

        ResetSortingOrder();
    }

    // ---------------------------------------------------
    // SPRITE LAYER ORDER
    // ---------------------------------------------------

    public void RaiseSortingOrder()
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = originalSortingOrder + 10;
    }

    public void ResetSortingOrder()
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = originalSortingOrder;
    }
}
