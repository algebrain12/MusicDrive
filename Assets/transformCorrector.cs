using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class transformCorrector : MonoBehaviour
{
    public Button button;

    private Rigidbody rb;

    private void Awake()
    {
        // Optional: if the player has a Rigidbody, we correct through it instead
        // of the raw transform so physics stays in sync and any spin is cleared.
        rb = GetComponent<Rigidbody>();
    }

    // Call this right after instantiating the player and pass it the button
    // that already exists in the game scene.
    public void Init(Button resetButton)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ResetRotation);
        }

        button = resetButton;

        if (button != null)
        {
            button.onClick.AddListener(ResetRotation);
        }
    }

    private void OnDestroy()
    {
        // Unhook so the scene's button doesn't keep a dangling reference to a
        // destroyed player after respawns/scene changes.
        if (button != null)
        {
            button.onClick.RemoveListener(ResetRotation);
        }
    }

    public void ResetRotation()
    {
        float yaw = transform.eulerAngles.y;
        Quaternion upright = Quaternion.Euler(0f, yaw, 0f);
        transform.Translate(new Vector3(0,10f,0));

        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.MoveRotation(upright);
        }
        else
        {
            transform.rotation = upright;
        }
        button.interactable = false;
        StartCoroutine(Hello());
    }

    private IEnumerator Hello()
    {
        yield return new WaitForSecondsRealtime(5f);
        button.interactable = true;
    }
}