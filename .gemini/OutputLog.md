PlantUML 1.2025.5beta3
[From string (line 126) ]
 
@startuml
skinparam style strictuml
skinparam shadowing true
skinparam defaultFontName "Segoe UI"
skinparam defaultFontSize 16
...
... ( skipping 98 lines )
...
activate ShopM
ShopM --> IMS :
deactivate ShopM
 
IMS --> PIC : bool (purchaseSuccessful)
deactivate IMS
 
alt Purchase successful
PIC -> ShopM : DisplayMessage("Purchased Item!")
else Purchase failed (should not happen if CanExecute is robust)
PIC -> GE : AddGold(itemCost) (refund)
PIC -> ShopM : DisplayMessage("Purchase failed!")
end
 
deactivate PIC
UCP --> SM : Command executed
deactivate UCP
 
 
note over GI, GPS, IMS
Syntax Error? (Assumed diagram type: sequence)