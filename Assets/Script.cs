using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WinGameManager : MonoBehaviour
{
    public Text uiText; // Legacy UI Text

    void Start()
    {
        StartCoroutine(WaitForWinnerKey());
    }

    private IEnumerator WaitForWinnerKey()
    {
        // ✅ Keep checking until Winner is set in PlayerPrefs
        while (string.IsNullOrEmpty(PlayerPrefs.GetString("Winner")))
        {
            yield return null; // wait 1 frame
        }

        // ✅ Now read the winner
        string winner = PlayerPrefs.GetString("Winner");

        string displayMessage = (winner == "Wolf")
            ? "Wolf Wins! All Bonks are lost!"
            : "Bonks Win! The Wolf has been defeated!";

        if (uiText != null)
            uiText.text = displayMessage;
        else
            Debug.LogWarning("⚠️ No UI Text assigned in WinGameManager!");
    }
}
