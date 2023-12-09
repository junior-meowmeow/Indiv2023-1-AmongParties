using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ObjectiveUI : MonoBehaviour
{
    public bool isEnded = false;
    public bool isDone = false;
    public TMP_Text objectiveTitle;
    [SerializeField] private RectTransform rect;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private GameObject objectiveDoneText;
    [SerializeField] private GameObject objectiveFailText;
    
    public void UpdateObjective(Objective objective)
    {
        objectiveText.text = objective.GetObjectiveDescription() + "\nProgress: " + objective.score.ToString() + "/" + objective.targetScore.ToString();

        objectiveDoneText.SetActive(objective.isComplete);
    }

    public void EndObjective(Objective objective)
    {
        isEnded = true;
        isDone = objective.isComplete;
        SetEndedUI(objective.isComplete);
    }

    public void SetEndedUI(bool isDone)
    {
        objectiveText.text = "";
        objectiveDoneText.SetActive(isDone);
        objectiveFailText.SetActive(!isDone);
        rect.sizeDelta = new Vector2(400, 80);
    }
}
