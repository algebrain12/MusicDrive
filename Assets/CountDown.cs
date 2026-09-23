using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    public TMP_Text countdownText;
    public BeatClock clock;
    public GameInput GetInput;
    // Start is called before the first frame update
    void Start()
    {
        GetInput.IsPaused = true;
        StartCoroutine(CountdownThenGo(3));
    }

    private IEnumerator CountdownThenGo(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        countdownText.text = "Go!";
        GetInput.IsPaused = false;
        clock.StartSong();
        yield return new WaitForSecondsRealtime(0.5f);
        countdownText.text = "";
        countdownText.gameObject.SetActive(false);
    }
}
