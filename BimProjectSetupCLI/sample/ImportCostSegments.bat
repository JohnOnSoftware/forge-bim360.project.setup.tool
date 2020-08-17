set FORGE_CLIENT_ID=<your_client_id_without_quotation_marks>
set FORGE_CLIENT_SECRET=<your_client_secret_without_quotation_marks>
set FORGE_BIM_ACCOUNT_ID=<your_account_id_without_quotation_marks>
set FORGE_CALLBACK=<your_forge_callback_without_quotation_marks>
cd ..
Autodesk.BimProjectSetup.exe -pcs ".\sample\BIM360_CostSegments_Template.csv"
pause