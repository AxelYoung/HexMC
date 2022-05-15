using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BadPauseScript : MonoBehaviour {
    public GameObject loadingScreen;
    public PlayerController player;

    public void Awake() {
        loadingScreen.SetActive(true);
        player.enabled = false;
        StartCoroutine(Loading());
    }

    public IEnumerator Loading() {
        yield return new WaitForSeconds(10);
        player.enabled = true;
        yield return new WaitForSeconds(5);
        loadingScreen.SetActive(false);
    }
}
