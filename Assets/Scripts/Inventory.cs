using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour {
    public Image[] hotbarSlots;
    public ItemSlot[] inventory = new ItemSlot[36];

    public PlayerController playerController;

    void Start() {
        playerController = GetComponent<PlayerController>();
    }

    public int PickUpItem(byte ID, byte amount) {
        for (int i = 0; i < 36; i++) {
            if (inventory[i].ID == ID) {
                inventory[i].quantity += amount;
                UpdateText(i);
                return i;
            }
        }
        for (int i = 0; i < 36; i++) {
            if (inventory[i].quantity == 0) {
                inventory[i].ID = ID;
                inventory[i].quantity += amount;
                UpdateSlotImage(i);
                UpdateText(i);
                return i;
            }
        }
        return 0;
    }

    public void RemoveItem(int index, byte quantity) {
        inventory[index].quantity -= quantity;
        if (inventory[index].quantity == 0) {
            inventory[index].ID = 0;
            if (index <= 8) {
                hotbarSlots[index].gameObject.SetActive(false);
            }
        } else {
            UpdateText(index);
        }
    }

    public void UpdateSlotImage(int index) {
        if (index <= 8) {
            if (inventory[index].quantity > 0) {
                hotbarSlots[index].gameObject.SetActive(true);
                hotbarSlots[index].sprite = Resources.Load<Sprite>("Sprites/" + inventory[index].ID.ToString());
            } else {
                hotbarSlots[index].gameObject.SetActive(false);
            }
            if (index == playerController.currentItem) {
                playerController.UpdateArm();
            }
        }
    }

    public void UpdateText(int index) {
        if (index <= 8) {
            Text[] hotbarAmount = hotbarSlots[index].GetComponentsInChildren<Text>();
            foreach (Text amount in hotbarAmount) {
                amount.text = (inventory[index].quantity > 1) ? inventory[index].quantity.ToString() : "";
            }
        }
    }
}

public struct ItemSlot {
    public byte ID;
    public byte quantity;

    public ItemSlot(byte ID = 0, byte quantity = 0) {
        this.ID = ID;
        this.quantity = quantity;
    }
}