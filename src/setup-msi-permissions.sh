
functionName="Az_FUNC_NAME"
echo "Retrieving the MSI ID for Function app: $functionName"
msiObjectId=$(az resource list -n $functionName --query [*].identity.principalId --out tsv)
echo "MSI Object ID: $msiObjectId"


echo "Retrieving the MS Graph App's Object ID"
graphAppID=$(az ad sp list --display-name "Microsoft Graph" --query [0].objectId --out tsv)
echo "MS Graph App's Object ID: $graphAppID"

echo "Retrieving the MS Graph Role ID for 'Application.ReadWrite.OwnedBy' with Application Permission..."
appReadWriteOwnedByRoleId=$(az ad sp list --display-name "Microsoft Graph" --query "[0].appRoles[?value=='Application.ReadWrite.OwnedBy' && contains(allowedMemberTypes, 'Application')].id" --output tsv)
echo "Role ID: $appReadWriteOwnedByRoleId"



echo "Retrieving the MS Graph Role ID for 'Application.ReadWrite.All' with Application Permission..."
appReadWriteAllRoleId=$(az ad sp list --display-name "Microsoft Graph" --query "[0].appRoles[?value=='Application.ReadWrite.All' && contains(allowedMemberTypes, 'Application')].id" --output tsv)
echo "Role ID: $appReadWriteAllRoleId"

uri=https://graph.microsoft.com/v1.0/servicePrincipals/$msiObjectId/appRoleAssignments

echo "Assigning role ('Application.ReadWrite.OwnedBy')..."
body="{'principalId':'$msiObjectId','resourceId':'$graphAppID','appRoleId':'$appReadWriteOwnedByRoleId'}"
az rest --method post --uri $uri --body $body --headers "Content-Type=application/json"
echo "Assigning role ('Application.ReadWrite.OwnedBy') completed."


echo "Assigning role ('Application.ReadWrite.All')..."
body="{'principalId':'$msiObjectId','resourceId':'$graphAppID','appRoleId':'$appReadWriteAllRoleId'}"
az rest --method post --uri $uri --body $body --headers "Content-Type=application/json"
echo "Assigning role ('Application.ReadWrite.All') completed."



# The following is not needed for service principal and app registration management.
# It's only for testing the graph API connectivity - in production we don't need this.

echo "Retrieving the MS Graph Role ID for 'User.Read.All' with Application Permission..."
userReadRoleId=$(az ad sp list --display-name "Microsoft Graph" --query "[0].appRoles[?value=='User.Read.All' && contains(allowedMemberTypes, 'Application')].id" --output tsv)
echo "Role ID: $userReadRoleId"

echo "Assigning role ('User.Read.All')..."
body="{'principalId':'$msiObjectId','resourceId':'$graphAppID','appRoleId':'$userReadRoleId'}"
az rest --method post --uri $uri --body $body --headers "Content-Type=application/json"
echo "Assigning role ('User.Read.All') completed."