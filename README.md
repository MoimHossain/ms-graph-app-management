# Application and Service Principal management via Microsoft Graph API 

The repository contains a Proof of concept **Azure Function** implementation that does the following:

- Triggered via __Azure Storage Queue__ message.

The message that needs to be pushed into the storage queue accepts the following schema:

```json
{
    "OrgName": "Your Organization name (just the name not the entire URL)",
    "PAT": "A personal access token",
    "ProjectId": "Must be the Guid of an Azure DevOps Project",
    "RotateAllServiceConnections": false,
    "ServiceEndpoints": ["Must be a guid - a Service Connection ID"],
    "DaysBeforeExpire": 1,
    "LifeTimeInDays": 5
}
```
- The function has a **Managed Identity**
- The Managed Identity needs to have ```Application.ReadWrite.OwnedBy```  Graph permissions and consent granted by Azure AD Administrators.
- The function read the endpoint (aka. AzDO **Service Connection**) (or multiple endpoints) and determine the corresponding Azure AD Application (via __Service Principal__)

:zap: Password Credentails

- If the Service Connection was created with **Client Secret** (password based authentication).
  * Function will generate a **new** password credentail (based on ```DaysBeforeExpire``` and ```LifeTimeInDays``` provided in storage message).
  * Update the service connection in Azure DevOps to use that.
  * Delete the old password credentails for the AAD application.
- 

:zap: Certificate credentails
- If the service connection was created with **Certificate (PEM)** based authentication.
  * Function will generate a **Self-Signed certificate**.
  * Update the Application in Azure AD - creating the certificate credentials with the newly created certificate (essentially a self-signed PFX). **Note:** This operation will remove the old certificate from the application in Azure AD.
  * Update the Azure DevOps service connection to use the newly created certificate (through the PEM)


---

> :loudspeaker: Important Notice: The Azure function in this demo application doesn't do anything for securing the API endpoints (there are some test/temporary endpoints), you should either remove them or bring your owner authentiation mechanism with ***Web Application Firewall (WAF)*** to protect these endpoints. You must **NOT** deploy the Azure functions with nacked endpoints exposed in the wild internet.


# Details

The implementation is all about Managed Identities. The code offers all the APIs to manage application registrations and service principals in Azure Active directory as REST API and you can deploy it as Azure Functions.

## Managed Identity and MS Graph permissions
After deploying it as Azure Functions, you need to make sure the Managed Identity is enabled and Microsoft Graph Application permissions are granted to that idenity. 

You can use the bash script examples as [described](./src/setup-msi-permissions.sh) in ```setup-msi-permissions.sh``` file.

Then you can use that Azure function REST endpoints to orchestrate your workflows to manage app registrations and service principals. 

### Examples
There are some example payload schema and REST endpoints can be found in [```example.http```](./src/example.http).

# Contribution
You are more than welcome to contribute to the repository! :clinking_glasses:

# License

This is under MIT license, you are free to use, modify the code anyway you want. Of course, I would appreciate if you acknoledge if this code helped you. That surely motivates and makes my day!

Enjoy!