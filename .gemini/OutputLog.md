While in a shop, the player can attempt to purchase an item by either clicking on it or dragging it to a slot in their inventory or equipment containers. 

When a purchase is attempted, it should execute the following checks:
Does the player have enough gold to afford the purchase?

If yes:
    (Click to purchase): 
    - Does the player have an available slot in their inventory?
        - Yes: Move item to first available slot
        - No: Does the player have an available slot in their equipment?
            - Yes: Move item to first available slot in equipment
            - No: Reject purchase, leaving the item in the shop slot
    
    (Drag to purchase):
    - Did the player drag the item to an empty inventory or equipment slot?
        - Yes: Move item to that empty slot
        - No: Check for available slots, following the same steps as Click to Purchase
            - If no available slots, reject purchase
If no:
    Reject purchase, leaving the item in the shop slot