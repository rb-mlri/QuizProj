using UnityEngine;
using System.Collections;

public class Scarecrow : MonoBehaviour
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isWiggling = false;

    [Header("Wiggle Settings")]
    public float wiggleDelay = 0.1f;   // time before wiggle starts
    public float duration = 0.25f;     // how long wiggle lasts
    public float rotationMagnitude = 5f; // how much rotation
    public float shakeAmount = 0.05f;    // how much it moves side-to-side

    void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    public void Wiggle()
    {
        if (!isWiggling)
            StartCoroutine(WiggleRoutine());
    }

    private IEnumerator WiggleRoutine()
    {
        isWiggling = true;

        // ⏳ delay before wiggle starts
        yield return new WaitForSeconds(wiggleDelay);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float angle = Mathf.Sin(elapsed * 40f) * rotationMagnitude;
            float offsetX = Mathf.Sin(elapsed * 50f) * shakeAmount;

            transform.localRotation = Quaternion.Euler(0, 0, angle);
            transform.localPosition = originalPosition + new Vector3(offsetX, 0, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // reset back
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        isWiggling = false;
    }
}
