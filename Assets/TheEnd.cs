using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TheEnd : MonoBehaviour
{
    [SerializeField] private GameObject winUI;
    [SerializeField] private TextMeshProUGUI levelInfo;
    [SerializeField] private string levelInfoText;

    public void Win()
    {
        winUI.SetActive(true);
        var level = PlayerPrefs.GetInt("HoneyLevel");
        PlayerPrefs.SetInt("HoneyLevel", ++level);
        levelInfo.text = string.Format(levelInfoText, level);

        Debug.Log("WIN!");
    }

    public void Restart()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }
}