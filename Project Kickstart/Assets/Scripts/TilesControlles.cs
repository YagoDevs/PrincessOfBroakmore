using UnityEngine;
using System.Collections;

public class TilesController : MonoBehaviour
{
    public float timeBeforeDisappear = 2f;
    public float timeToReappear = 3f;
    public float shrinkSpeed = 2f;
    public float growSpeed = 2f;
    public float blinkDuration = 1f;
    public float blinkInterval = 0.2f;

    private bool playerOnTop = false;
    private bool disappearing = false;
    private Collider col;
    private Renderer rend;
    private Vector3 originalScale;

    void Start()
    {
        col = GetComponent<Collider>();
        rend = GetComponent<Renderer>();
        originalScale = transform.localScale;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !disappearing)
        {
            playerOnTop = true;
            Invoke("Disappear", timeBeforeDisappear);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnTop = false;
        }
    }

    void Disappear()
    {
        if (playerOnTop)
        {
            StartCoroutine(BlinkAndShrink());
        }
    }

    IEnumerator BlinkAndShrink()
    {
        disappearing = true;
        float elapsed = 0f;
        while (elapsed < blinkDuration)
        {
            rend.enabled = !rend.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }
        rend.enabled = true;
        yield return StartCoroutine(ShrinkAndDisable());
    }

    IEnumerator ShrinkAndDisable()
    {
        while (transform.localScale.magnitude > 0.05f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * shrinkSpeed);
            yield return null;
        }
        col.enabled = false;
        rend.enabled = false;
        transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(timeToReappear);
        StartCoroutine(GrowAndEnable());
    }

    IEnumerator GrowAndEnable()
    {
        col.enabled = true;
        rend.enabled = true;
        while (Vector3.Distance(transform.localScale, originalScale) > 0.05f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * growSpeed);
            yield return null;
        }
        transform.localScale = originalScale;
        disappearing = false;
    }
}