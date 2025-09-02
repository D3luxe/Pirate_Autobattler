using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using PirateRoguelike.Data; // Added for ItemInstance

namespace PirateRoguelike.UI
{
    public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum SlotType { Inventory, Equipped }

    [Header("References")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite filledSlotSprite;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTagsText; // New: For displaying item tags
    [SerializeField] private GameObject itemDisplayParent;
    [SerializeField] private Image rarityOverlayImage; // New: For rarity color overlay
    [SerializeField] private Color[] rarityColors; // New: Array of colors for rarities (Bronze, Silver, Gold, Diamond)

    private ItemInstance _itemInstance;
    private SlotType _slotType;
    private int _slotIndex; // Added

    // Dragging variables
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Transform _originalParent;
    private GameObject _draggedItemVisual;

    public ItemInstance GetItem() => _itemInstance;
    public SlotType GetSlotType() => _slotType;
    public int Index => _slotIndex; // Added

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetData(SlotType type, int index, ItemInstance item) // Modified to set all data
    {
        _slotType = type;
        _slotIndex = index;
        SetItem(item);
    }

    public void SetItem(ItemInstance item)
    {
        _itemInstance = item;
        if (_itemInstance != null)
        {
            itemDisplayParent.SetActive(true);
            if (itemIcon != null && _itemInstance.Def.icon != null) itemIcon.sprite = _itemInstance.Def.icon;
            else if (itemIcon != null) itemIcon.sprite = null;
            if (itemNameText != null) itemNameText.text = _itemInstance.Def.displayName;
            if (itemTagsText != null) itemTagsText.text = string.Join(", ", _itemInstance.Def.tags.Select(t => t.displayName)); // Display tags
            if (slotBackground != null && filledSlotSprite != null) slotBackground.sprite = filledSlotSprite; // Set filled sprite

            // Set rarity color overlay
            if (rarityOverlayImage != null && rarityColors != null && (int)_itemInstance.Def.rarity < rarityColors.Length)
            {
                rarityOverlayImage.color = rarityColors[(int)_itemInstance.Def.rarity];
            }
        }
        else
        {
            ClearItem();
        }
    }

    public void ClearItem()
    {
        _itemInstance = null;
        itemDisplayParent.SetActive(false);
        if (slotBackground != null && emptySlotSprite != null) slotBackground.sprite = emptySlotSprite; // Set empty sprite
    }

    // --- Drag Handlers ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_itemInstance == null) return;

        _originalParent = transform.parent;
        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false;

        _draggedItemVisual = new GameObject("DraggedItemVisual");
        Image dragImage = _draggedItemVisual.AddComponent<Image>();
        dragImage.sprite = itemIcon.sprite;
        dragImage.rectTransform.sizeDelta = _rectTransform.sizeDelta;
        _draggedItemVisual.transform.SetParent(InventoryUI.Instance.transform.parent); // Parent to main Canvas
        _draggedItemVisual.transform.SetAsLastSibling(); // Bring to front
        _draggedItemVisual.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_draggedItemVisual != null)
        {
            _draggedItemVisual.GetComponent<RectTransform>().position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        if (_draggedItemVisual != null)
        {
            Destroy(_draggedItemVisual);
            _draggedItemVisual = null;
        }
    }

    // --- Drop Handler ---
    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI droppedSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (droppedSlot != null && droppedSlot != this)
        {
            InventoryUI.Instance.HandleItemDrop(droppedSlot, this);
        }
    }

    // --- Pointer Handlers for Merge Preview ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            InventorySlotUI draggedSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
            if (draggedSlot != null && draggedSlot.GetItem() != null && _itemInstance != null)
            {
                // Check for merge condition (same item type, same rarity)
                if (draggedSlot.GetItem().Def.id == _itemInstance.Def.id && draggedSlot.GetItem().Def.rarity == _itemInstance.Def.rarity)
                {
                    Debug.Log($"Merge preview: {draggedSlot.GetItem().Def.displayName} with {itemNameText.text}");
                    // TODO: Implement visual merge preview (e.g., glow, preview icon of next rarity)
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Clear merge preview
        // TODO: Clear visual merge preview
    }
}
}