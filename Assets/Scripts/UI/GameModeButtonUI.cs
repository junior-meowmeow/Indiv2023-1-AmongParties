using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameModeButtonUI : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private TMP_Text text;
    [SerializeField] private GameMode gameMode;
    [SerializeField] private string gameModeName;

    public void Init()
    {
        if(toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
        if(gameModeName == null)
        {
            gameModeName = gameMode.ToString();
        }
        if(text == null)
        {
            text = GetComponentInChildren<TMP_Text>();
        }
        toggle.onValueChanged.AddListener(delegate {
            if(toggle.isOn)
            {
                MainUIManager.updateGameModeUI(gameMode);
            }
        });
        text.text = gameModeName;
    }

    public void UpdateButton(GameMode gameMode, bool isServer)
    {
        if(this.gameMode == gameMode)
        {
            toggle.isOn = true;
            toggle.interactable = false;
        }
        else
        {
            toggle.isOn = false;
            toggle.interactable = isServer;
        }
    }

}
