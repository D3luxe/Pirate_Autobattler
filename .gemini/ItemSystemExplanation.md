Inventory & Equipment System Summary

ItemSlot: a container that holds 0 or 1 Item.

Inventory: a fixed SlotContainer with a number of slots defined in the GameConfigSO

Equipment: a dynamic SlotContainer whose slots can be added or removed at runtime.

Move/Swap Logic: moving an item into a slot places it if empty, or swaps if occupied. Same-slot moves = no-op.

Events: slots and containers raise the relevant event so the UI auto-updates when items/slots change.

Policies: removing a slot with an item requires a rule (auto-move to inventory, block if no available space).

UI: listens to events; spawns/removes slot widgets dynamically; updates icons/counts when items change.

This makes the system flexible, data-driven, and safe for dynamic runtime changes (e.g., equipment slots scaling with gameplay).