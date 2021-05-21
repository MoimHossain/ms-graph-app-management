# ms-graph-app-management

Application and service principal management via MS graph API and Managed Identity



> Important Notice: The Azure function in this demo application doesn't do anything for securing the API endpoints, you should bring your owner authentiation mechanism to protect these endpoints. By no means, you should just deploy the Azure functions with nacked endpoints exposed in the wild internet.

# Details

The implementation is all about Managed Identities. The code offers all the APIs to manage application registrations and service principals in Azure Active directory as REST API and you can deploy it as Azure Functions.

## Managed Identity and MS Graph permissions
After deploying it as Azure Functions, you need to make sure the Managed Identity is enabled and Microsoft Graph Application permissions are granted to that idenity. 

You can use the bash script examples as [described](./src/setup-msi-permissions.sh) in ```setup-msi-permissions.sh``` file. 

Then you can use that Azure function REST endpoints to orchestrate your workflows to manage app registrations and service principals. 

# Examples

The example payload schema and REST endpoints can be found in [```example.http```](./src/example.http).