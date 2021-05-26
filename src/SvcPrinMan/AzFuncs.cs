using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SvcPrinMan.Payloads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SvcPrinMan
{
    public static class AzFuncs
    {
        [FunctionName("QueueTrigger")]
        public static async Task QueueTrigger(
        [QueueTrigger("inbox")] CredentialRotatePayload payload, ILogger log)
        {
            await Funcs.RotateCredentailsIfRequiredAsync(payload, log);
        }
    }
}
