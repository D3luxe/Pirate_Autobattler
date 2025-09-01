using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI; // Added
using PirateRoguelike.Data; // Added for Tag enum
using PirateRoguelike.Services; // Added for SlotId
using PirateRoguelike.Events; // Added for ItemManipulationEvents

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform _inventorySlotsParent;
    [SerializeField] private Transform _equipmentSlotsParent;
    [SerializeField] private GameObject _inventorySlotPrefab;

    private List<InventorySlotUI> _inventorySlots = new List<InventorySlotUI>();
    private List<InventorySlotUI> _equipmentSlots = new List<InventorySlotUI>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Removed this line
        }
    }

    public void Initialize()
    {
        if (GameSession.Inventory == null || GameSession.PlayerShip == null)
        {
            Debug.LogError("GameSession is not ready. Cannot initialize InventoryUI.");
            return;
        }

        // Clear existing slots before creating new ones
        foreach (Transform child in _inventorySlotsParent) Destroy(child.gameObject);
        foreach (Transform child in _equipmentSlotsParent) Destroy(child.gameObject);
        _inventorySlots.Clear();
        _equipmentSlots.Clear();

        // Initialize inventory slots based on RunConfig
        for (int i = 0; i < GameSession.Inventory.MaxSize; i++)
        {
            var slot = CreateSlot(_inventorySlotsParent, InventorySlotUI.SlotType.Inventory, i);
            _inventorySlots.Add(slot);
        }

        // Initialize equipment slots based on the ship's capacity
        for (int i = 0; i < GameSession.PlayerShip.Equipped.Length; i++)
        {
            var slot = CreateSlot(_equipmentSlotsParent, InventorySlotUI.SlotType.Equipped, i);
            _equipmentSlots.Add(slot);
        }

        // Subscribe to ItemManipulationEvents
        ItemManipulationEvents.OnItemMoved += HandleItemMoved;
        ItemManipulationEvents.OnItemAdded += HandleItemAdded;
        ItemManipulationEvents.OnItemRemoved += HandleItemRemoved;

        RefreshAll();
    }

    private InventorySlotUI CreateSlot(Transform parent, InventorySlotUI.SlotType type, int index)
    {
        if (_inventorySlotPrefab == null)
        {
            Debug.LogError("Inventory Slot Prefab is not assigned in InventoryUI!");
            return null;
        }
        GameObject slotGO = Instantiate(_inventorySlotPrefab, parent);
        InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
        if (slotUI == null)
        {
            Debug.LogError($"Inventory Slot Prefab does not have an InventorySlotUI component! Slot index: {index}");
            return null;
        }
        slotUI.SetData(type, index, null); // Initialize with empty data
        return slotUI;
    }

    void OnDestroy()
    {
        // Unsubscribe from ItemManipulationEvents
        ItemManipulationEvents.OnItemMoved -= HandleItemMoved;
        ItemManipulationEvents.OnItemAdded -= HandleItemAdded;
        ItemManipulationEvents.OnItemRemoved -= HandleItemRemoved;
    }

    public void RefreshAll()
    {
        if (_inventorySlots == null || _equipmentSlots == null) return;

        // Refresh inventory slots
        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            if (_inventorySlots[i] != null)
            {
                _inventorySlots[i].SetData(InventorySlotUI.SlotType.Inventory, i, GameSession.Inventory.GetItemAt(i));
            }
        }

        // Refresh equipment slots
        if (GameSession.PlayerShip == null)
        {
            Debug.LogWarning("GameSession.PlayerShip is null. Cannot refresh equipped slots.");
            return;
        }

        for (int i = 0; i < _equipmentSlots.Count; i++)
        {
            if (_equipmentSlots[i] != null)
            {
                _equipmentSlots[i].SetData(InventorySlotUI.SlotType.Equipped, i, GameSession.PlayerShip.GetEquippedItem(i));
            }
        }
    }

    private void HandleItemMoved(ItemInstance item, SlotId from, SlotId to)
    {
        RefreshAll();
    }

    private void HandleItemAdded(ItemInstance item, SlotId to)
    {
        RefreshAll();
    }

    private void HandleItemRemoved(ItemInstance item, SlotId from)
    {
        RefreshAll();
    }

    public void HandleItemDrop(InventorySlotUI fromSlot, InventorySlotUI toSlot)
    {
        if (fromSlot == null || toSlot == null || fromSlot == toSlot) return;

        var fromType = fromSlot.GetSlotType();
        var toType = toSlot.GetSlotType();
        int fromIndex = fromSlot.Index;
        int toIndex = toSlot.Index;

        ItemInstance fromItem = GetItemFromSlot(fromType, fromIndex);
        ItemInstance toItem = GetItemFromSlot(toType, toIndex);

        // If dragging from an empty slot, do nothing
        if (fromItem == null) return;

        // --- Attempt Merge First ---
        // For now, a simple merge: if items are identical and target slot is empty, just move.
        // Rarity progression merge will be more complex.
        if (fromItem.Def.id == toItem?.Def.id) // Check if items are of the same type
        {
            // If target slot is empty, just move the item
            if (toItem == null)
            {
                SetItemInSlot(fromType, fromIndex, null); // Clear source
                SetItemInSlot(toType, toIndex, fromItem); // Place item
                RefreshAll();
                return;
            }
            // TODO: Implement rarity progression merge here
            // For now, if items are identical and target is not empty, treat as a swap
        }

        // --- Perform Swap/Move if no merge or merge not applicable ---

        // Validate if 'fromItem' can be placed in 'toSlot'
        if (!CanPlaceItem(fromItem, toType, toIndex))
        {
            Debug.Log(string.Format("Cannot place {0} in {1} slot {2}.", fromItem.Def.displayName, toType, toIndex));
            RefreshAll(); // Ensure UI is consistent
            return;
        }

        // Validate if 'toItem' (if exists) can be placed back in 'fromSlot'
        if (toItem != null && !CanPlaceItem(toItem, fromType, fromIndex))
        {
            Debug.Log(string.Format("Cannot place {0} back in {1} slot {2}.", toItem.Def.displayName, fromType, fromIndex));
            RefreshAll(); // Ensure UI is consistent
            return;
        }

        // Perform the actual swap/move
        SetItemInSlot(fromType, fromIndex, toItem); // Place toItem (or null) in fromSlot
        SetItemInSlot(toType, toIndex, fromItem);   // Place fromItem in toSlot

        RefreshAll();
    }

    // Helper to get item from a slot
    private ItemInstance GetItemFromSlot(InventorySlotUI.SlotType type, int index)
    {
        if (type == InventorySlotUI.SlotType.Inventory)
        {
            return GameSession.Inventory.GetItemAt(index);
        }
        else // Equipped
        {
            return GameSession.PlayerShip.GetEquippedItem(index);
        }
    }

    // Helper to set item in a slot
    private void SetItemInSlot(InventorySlotUI.SlotType type, int index, ItemInstance item)
    {
        if (type == InventorySlotUI.SlotType.Inventory)
        {
            GameSession.Inventory.SetItemAt(index, item);
        }
        else // Equipped
        {
            GameSession.PlayerShip.SetEquipment(index, item);
        }
    }

    // Helper to check if an item can be placed in a specific slot
    private bool CanPlaceItem(ItemInstance item, InventorySlotUI.SlotType targetType, int targetIndex)
    {
        // If item is null, it can always be placed (clearing a slot)
        if (item == null) return true;

        // Inventory slots can always hold any item
        if (targetType == InventorySlotUI.SlotType.Inventory) return true;

        // For Equipped slots, any item can be placed
        if (targetType == InventorySlotUI.SlotType.Equipped)
        {
            return true;
        }
        return false; // Should not reach here
    }

    public void SetInventoryVisibility(bool showInventory, bool showEquipment)
    {
        if (_inventorySlotsParent != null)
        {
            _inventorySlotsParent.gameObject.SetActive(showInventory);
        }
        if (_equipmentSlotsParent != null)
        {
            _equipmentSlotsParent.gameObject.SetActive(showEquipment);
        }
    }

    /// <summary>
    /// Sets the interactability of the equipment slots.
    /// </summary>
    /// <param name="interactable">True to enable interaction, false to disable.</param>
    public void SetEquipmentInteractability(bool interactable)
    {
        foreach (var slotUI in _equipmentSlots)
        {
            if (slotUI != null)
            {
                // Disable raycasts to prevent drag/drop interaction
                if (slotUI.TryGetComponent<CanvasGroup>(out CanvasGroup canvasGroup))
                {
                    canvasGroup.blocksRaycasts = interactable;
                }

                // Disable button interaction if the slot has a button
                if (slotUI.TryGetComponent<Button>(out Button button))
                {
                    button.interactable = interactable;
                }
            }
        }
    }
}