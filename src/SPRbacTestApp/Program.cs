using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Linq;


var clientId = "";
var clientSecret = "";
var tenantId = "";
var subId = "";

var creds =  SdkContext.AzureCredentialsFactory
    .FromServicePrincipal(
        clientId, clientSecret, 
        tenantId, AzureEnvironment.AzureGlobalCloud);

var azure = ResourceManager
    .Configure()
    .Authenticate(creds)
    .WithSubscription(subId);

var resources = await azure.ResourceGroups.ListAsync();
var responseMessage = string.Join($"{Environment.NewLine}", resources.Select(r => r.Name).ToList());

Console.WriteLine(responseMessage);
Console.ReadKey();
