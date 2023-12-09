using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ObjectiveUI : MonoBehaviour
{
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
        objectiveText.text = "";
        objectiveDoneText.SetActive(objective.isComplete);
        objectiveFailText.SetActive(!objective.isComplete);
        rect.sizeDelta = new Vector2(400, 80);
    }
}
