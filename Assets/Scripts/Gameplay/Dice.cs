using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Dice : MonoBehaviour
{
    [Header("Dice Logic")]
    public int lastResult = 1;  // Last rolled number (1–6)

    [Header("Visuals")]
    public Image diceImage;     // UI image showing the dice face
    public Sprite[] faceSprites; // Sprites for faces 1–6

    [Header("Pop-O-Matic Movement")]
    public float moveRadius = 30f; // How far the dice can shift inside its dome

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip diceSound;

    [Header("Dice Visual Root")]
    public Transform diceVisual;  // Used for bounce animation


    // ---------------------------------------------------------
    // PUBLIC: Rolls the dice and updates the visual result
    // ---------------------------------------------------------
    public int Roll()
    {
        lastResult = Random.Range(1, 7);    // Unity’s Range is min inclusive, max exclusive

        Debug.Log("Dice rolled: " + lastResult);

        UpdateVisual();
        RandomizePositionAndRotation();

        return lastResult;
    }


    // ---------------------------------------------------------
    // Update the dice sprite to match the rolled number
    // ---------------------------------------------------------
    private void UpdateVisual()
    {
        if (diceImage == null)
        {
            Debug.LogWarning("DiceImage missing!");
            return;
        }

        if (faceSprites == null || faceSprites.Length < 6)
        {
            Debug.LogWarning("Not all dice face sprites assigned!");
            return;
        }

        // Safe index selection
        int index = Mathf.Clamp(lastResult - 1, 0, faceSprites.Length - 1);
        diceImage.sprite = faceSprites[index];
    }


    // ---------------------------------------------------------
    // Simulates the dice shifting around inside a pop-o-matic
    // ---------------------------------------------------------
    private void RandomizePositionAndRotation()
    {
        if (diceImage == null)
            return;

        RectTransform rt = diceImage.rectTransform;
        if (rt == null)
            return;

        // Random offset within a circle
        Vector2 offset = Random.insideUnitCircle * moveRadius;
        rt.anchoredPosition = offset;

        // Random rotation for visual effect
        rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
    }


    // ---------------------------------------------------------
    // Plays a small bounce animation when clicking the dice
    // ---------------------------------------------------------
    public IEnumerator PlayDiceAnimation()
    {
        GameManager.Instance.diceAnimating = true;

        // Optional audio feedback
        if (audioSource != null && diceSound != null)
            audioSource.PlayOneShot(diceSound);

        if (diceVisual == null)
        {
            Debug.LogWarning("diceVisual not assigned!");
            GameManager.Instance.diceAnimating = false;
            yield break;
        }

        Vector3 originalScale = diceVisual.localScale;
        Vector3 largerScale = originalScale * 1.25f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;

            // Smooth bounce curve
            float bounce = Mathf.Sin(t * Mathf.PI);

            diceVisual.localScale = Vector3.Lerp(originalScale, largerScale, bounce);

            yield return null;
        }

        // Ensure exact original scale is restored
        diceVisual.localScale = originalScale;

        GameManager.Instance.diceAnimating = false;
    }
}
