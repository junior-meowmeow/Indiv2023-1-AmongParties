using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ObjectiveUIManager : NetworkBehaviour
{

    private static ObjectiveUIManager instance;
    public static ObjectiveUIManager Instance => instance;

    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject ObjectiveUIPrefab;
    [SerializeField] private List<ObjectiveUI> ObjectiveList;
    [SerializeField] private Transform ObjectiveParent;
    [SerializeField] private COOPGameModeManager gameModeManager;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void SetTimerActive(bool isActive)
    {
        timerText.gameObject.SetActive(isActive);
    }

    public void UpdateTimer(float time, bool isRelax)
    {
        if (isRelax) timerText.color = new Color(1, 0.185f, 0.185f);
        else timerText.color = new Color(1, 1, 1);
        int timeInt = Mathf.RoundToInt(time);
        string secondText = (timeInt % 60).ToString("00");
        timerText.text = (timeInt / 60).ToString() + ":" + secondText;
    }

    public void StartObjective(Objective objective)
    {
        ObjectiveUI obj = Instantiate(ObjectiveUIPrefab, ObjectiveParent).GetComponent<ObjectiveUI>();
        obj.UpdateObjective(objective);
        ObjectiveList.Add(obj);
        obj.objectiveTitle.text = "Objective  " + ObjectiveList.Count.ToString();
    }

    public void EndObjective(Objective objective)
    {
        ObjectiveList.Last().EndObjective(objective);
    }

    public void UpdateObjective(Objective objective)
    {
        if (ObjectiveList.Count == 0) return;
        ObjectiveList.Last().UpdateObjective(objective);
    }

    public void ResetObjectives()
    {
        foreach (ObjectiveUI obj in ObjectiveList)
        {
            Destroy(obj.gameObject);
        }
        ObjectiveList.Clear();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestObjectiveServerRPC(ServerRpcParams serverRpcParams = default)
    {
        if (GameDataManager.Instance.GetGameState() != GameState.INGAME) return;
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            //var client = NetworkManager.ConnectedClients[clientId];
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            for (int i = 0; i < ObjectiveList.Count; i++)
            {
                if (ObjectiveList[i].isEnded)
                {
                    SetEndedObjectiveUIClientRPC(ObjectiveList[i].isDone, clientRpcParams);
                }
                else
                {
                    Objective objective = gameModeManager.GetCurrentObjective();
                    int id = objective.GetID();
                    ushort score = objective.score;
                    ushort targetScore = objective.targetScore;
                    SetOngoingObjectiveUIClientRPC(id, score, targetScore, clientRpcParams);
                }
            }
        }
    }

    [ClientRpc]
    private void SetEndedObjectiveUIClientRPC(bool isDone, ClientRpcParams clientRpcParams = default)
    {
        ObjectiveUI obj = Instantiate(ObjectiveUIPrefab, ObjectiveParent).GetComponent<ObjectiveUI>();
        obj.SetEndedUI(isDone);
        ObjectiveList.Add(obj);
        obj.objectiveTitle.text = "Objective  " + ObjectiveList.Count.ToString();
    }

    [ClientRpc]
    private void SetOngoingObjectiveUIClientRPC(int id, ushort score, ushort targetScore, ClientRpcParams clientRpcParams = default)
    {
        Objective objective = new();
        objective.Update(id, score, targetScore);
        gameModeManager.SetCurrentObjective(objective);
        ObjectiveUI obj = Instantiate(ObjectiveUIPrefab, ObjectiveParent).GetComponent<ObjectiveUI>();
        obj.UpdateObjective(objective);
        ObjectiveList.Add(obj);
        obj.objectiveTitle.text = "Objective  " + ObjectiveList.Count.ToString();
    }

}
