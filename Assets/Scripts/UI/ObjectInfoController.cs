using UnityEngine;
using TMPro;

public class ObjectInfoController : MonoBehaviour
{
    private static ObjectInfoController instance;
    public static ObjectInfoController Instance => instance;

    [SerializeField] private TMP_Text text;
    [SerializeField] private bool isHolding;
    [SerializeField] private string holdingItemName;
    [SerializeField] private string holdingItemDescription;
    [SerializeField] private string itemName;
    [SerializeField] private bool hasItemNearby;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        text = GetComponentInChildren<TMP_Text>();
    }

    void Start()
    {
        isHolding = false;
        hasItemNearby = false;
    }

    public void SetHoldingText(string holdingItemName, string holdingItemDescription)
    {
        isHolding = true;
        this.holdingItemName = holdingItemName;
        this.holdingItemDescription = holdingItemDescription;
        UpdateText();
    }

    public void ResetHoldingText()
    {
        isHolding = false;
        UpdateText();
    }

    public void SetItemText(string itemName)
    {
        hasItemNearby = true;
        this.itemName = itemName;
        UpdateText();
    }

    public void ResetItemText()
    {
        hasItemNearby = false;
        UpdateText();
    }

    private void UpdateText()
    {
        if(isHolding)
        {
            text.text = "Holding: " + holdingItemName + "\n" + holdingItemDescription;
        }
        else if(hasItemNearby)
        {
            text.text = "Press [E] to pick up " + itemName;
        }
        else
        {
            text.text = "Holding: NONE";
        }
    }
}
