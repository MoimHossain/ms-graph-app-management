# Following PowerShell scripts can be executed from a pipeline in Azure DevOps.
# It creates a request for rotating secrets for all service connections (or selectively) in a AzDO
# project. The request is created as a messge to a storage queue. The Az Function has a trigger 
# for storage queue, therefore, once the message is posted, the function will do the rest in Azure.

$CONNSTR=[System.Environment]::GetEnvironmentVariable('CONNSTR')
$OrgName=[System.Environment]::GetEnvironmentVariable('ORGNAME')
$PAT=[System.Environment]::GetEnvironmentVariable('PAT')
$ProjID="c335132d-ff5f-4371-ae28-ba7940f6eb92"
$EPID="a85edcc0-c1b6-4194-bb9e-fb165fc0dbcf"


$message="{""OrgName"":""$OrgName"",""PAT"":""$PAT"",""ProjectId"":""$ProjID"",""RotateAllServiceConnections"":""false"",""ServiceEndpoints"":[""$EPID""],""DaysBeforeExpire"":""5"",""LifeTimeInDays"":""5""}"
$b = [System.Text.Encoding]::UTF8.GetBytes($message)
$message64Base = [System.Convert]::ToBase64String($b)
az storage message put --content $message64Base --queue-name "inbox" --connection-string $CONNSTR
