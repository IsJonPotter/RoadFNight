using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class OutfitManager : NetworkBehaviour
{
    [SerializeField] private GameObject defaultOutfit;
    [SerializeField] public OutfitItem[] outfits;
    [SyncVar] public string currentOutfitName = "defaultOutfit";
    public Sprite defaultInventoryCharacterSprite;

    public RedicionStudio.InventorySystem.PlayerInventoryModule inventoryModule;

    RedicionStudio.InventorySystem.UIPlayerInventory uiPlayerInventory;

    private void Update()
    {
        if (uiPlayerInventory == null)
            uiPlayerInventory = GameObject.Find("UIPlayerInventory").GetComponent<RedicionStudio.InventorySystem.UIPlayerInventory>();

        if (currentOutfitName != "defaultOutfit")
        {
            foreach (OutfitItem outfit in outfits)
            {
                if (outfit.name == currentOutfitName)
                {
                    foreach (OutfitItem of in outfits)
                        of.outfitGameObject.SetActive(false);
                    defaultOutfit.SetActive(false);

                    outfit.outfitGameObject.SetActive(true);
                    if (uiPlayerInventory != null)
                        uiPlayerInventory.currentCharacter.sprite = outfit.inventoryCharacterSprite;
                }
            }
        }
        else
        {
            foreach (OutfitItem outfit in outfits)
                outfit.outfitGameObject.SetActive(false);
            defaultOutfit.SetActive(true);
            if (uiPlayerInventory != null)
                uiPlayerInventory.currentCharacter.sprite = defaultInventoryCharacterSprite;
        }

        if (!isLocalPlayer){
            return;
        }

        if (GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>().Slots[2].item.itemSO != null) // expensive but we need to fire an event to update this when we change the slot to change to null or active
        {
            if (inventoryModule.Slots[2].item.itemSO is RedicionStudio.InventorySystem.OutfitItemSO)
            {
                foreach(OutfitItem outfit in outfits)
                {
                    if (currentOutfitName != inventoryModule.Slots[2].item.itemSO.uniqueID)
                    {
                        CmdSetOutfit(inventoryModule.Slots[2].item.itemSO.uniqueID);
                    }
                }
            }
        }
        else
        {
            CmdSetOutfit("defaultOutfit");
        }
    }

    /// <summary>
    /// Setups up the outfit to send via RPC to the server to change for all clients online
    /// </summary>
    /// <param name="outfitName"></param>
    [Command]
    void CmdSetOutfit(string outfitName)
    {
        currentOutfitName = outfitName;

        RpcSetOutfit(outfitName);
    }

    /// <summary>
    /// Pushes the outfit to the server
    /// </summary>
    /// <param name="outfitName"></param>
    [ClientRpc]
    void RpcSetOutfit(string outfitName)
    {
        currentOutfitName = outfitName;
    }
}

[System.Serializable]
public class OutfitItem
{
    [Tooltip("The name of the outfit must match the unique name of the outfitSO")]
    public string name = "defaultOutfit";
    [Space]
    public RedicionStudio.InventorySystem.ItemSO itemSO;
    public GameObject outfitGameObject;
    [Tooltip("The image of the character displayed in the inventory.")]
    public Sprite inventoryCharacterSprite;
}
