using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEventHandler : MonoBehaviour
{
    public bool PauseState = false;

    [SerializeField]
    private List<GameObject> PlayObjects;
    [SerializeField]
    private List<GameObject> PauseObjects;
    public void OnPauseApplication()
    {
        foreach(GameObject item in PlayObjects)
        {
            item.SetActive(PauseState);
        }
        foreach(GameObject item in PauseObjects)
        {
            item.SetActive(!PauseState);
        }
        Time.timeScale = PauseState?1:0;
        PauseState = !PauseState;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPauseApplication();
        }
    }
}
