using System.Linq;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ObjectiveUIManager : NetworkBehaviour
{
    private static ObjectiveUIManager instance;
    public static ObjectiveUIManager Instance => instance;

    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject ObjectiveUIPrefab;
    [SerializeField] private List<ObjectiveUI> ObjectiveList;
    [SerializeField] private Transform ObjectiveParent;

    private void Awake()
    {
        InitSingleton();
    }

    private void InitSingleton()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void UpdateTimer(float time, bool isRelax)
    {
        if (isRelax) timerText.color = new Color(1, 0.185f, 0.185f);
        else timerText.color = new Color(1, 1, 1);
        string secondText = ((int)time % 60).ToString("00");
        timerText.text = ((int)time / 60).ToString() + ":" + secondText;
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
        if (GameManager.instance.GetGameState() != GameState.INGAME) return;
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
                    Objective objective = GameManager.instance.GetCurrentObjective();
                    int id = objective.GetID();
                    ushort score = objective.score;
                    ushort targetScore = objective.targetScore;
                    SetOngoingObjectiveUIClientRPC(id, score, targetScore, clientRpcParams);
                }
            }
        }
    }

    [ClientRpc]
    void SetEndedObjectiveUIClientRPC(bool isDone, ClientRpcParams clientRpcParams = default)
    {
        ObjectiveUI obj = Instantiate(ObjectiveUIPrefab, ObjectiveParent).GetComponent<ObjectiveUI>();
        obj.SetEndedUI(isDone);
        ObjectiveList.Add(obj);
        obj.objectiveTitle.text = "Objective  " + ObjectiveList.Count.ToString();
    }

    [ClientRpc]
    void SetOngoingObjectiveUIClientRPC(int id, ushort score, ushort targetScore, ClientRpcParams clientRpcParams = default)
    {
        Objective objective = new();
        objective.Update(id, score, targetScore);
        GameManager.instance.SetCurrentObjective(objective);
        ObjectiveUI obj = Instantiate(ObjectiveUIPrefab, ObjectiveParent).GetComponent<ObjectiveUI>();
        obj.UpdateObjective(objective);
        ObjectiveList.Add(obj);
        obj.objectiveTitle.text = "Objective  " + ObjectiveList.Count.ToString();
    }

}
