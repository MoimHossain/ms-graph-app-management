
using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SvcPrinMan.Payloads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SvcPrinMan
{
    public class AzFuncs
    {
        private readonly TelemetryClient telemetryClient;

        public AzFuncs(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        [FunctionName("QueueTrigger")]
        public async Task QueueTrigger(
        [QueueTrigger("inbox")] CredentialRotatePayload payload, ILogger log)
        {
            var response = await SecretRotationOrchestrator.RotateSecretAsync(payload);
            telemetryClient.TrackEvent("Secret Rotation completed", response.Item2);
        }
    }
}
