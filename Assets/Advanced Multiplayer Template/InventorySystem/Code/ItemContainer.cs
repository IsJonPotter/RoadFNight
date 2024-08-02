﻿using Mirror;


public abstract class ItemContainer : NetworkBehaviour {

	public readonly SyncListItemSlot slots = new SyncListItemSlot();
	
	
	
	public int GetSlotIndex(string itemSOUniqueName) {
		for (int i = 0; i < slots.Count; i++) {
			if (slots[i].amount > 0 && slots[i].item.itemSO.uniqueName == itemSOUniqueName) {
				return i;
			}
		}

		return -1;
	}
}

