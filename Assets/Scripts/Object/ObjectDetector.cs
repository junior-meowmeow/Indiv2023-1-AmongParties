using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectDetector : MonoBehaviour
{

    [SerializeField] private string locationName;
    [SerializeField] private Color baseColor;

    void Awake()
    {
        //Not working yet, Check it later :(
        GetComponent<MeshRenderer>().material.SetColor("_BaseColor", baseColor);
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent(out PickableObject obj))
        {
            GameplayManager.Instance.GetObject(obj, locationName);
            if (obj.holdPlayer != null)
            {
                obj.holdPlayer.Drop();
                obj.Drop();
            }
            obj.gameObject.SetActive(false);
            foreach (PlayerData ps in GameDataManager.Instance.GetPlayerList())
            {
                ps.player.interactionCollider.RemoveObject(obj);
            }
        }
    }
}
