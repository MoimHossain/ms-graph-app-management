using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SvcPrinMan.Payloads
{
    public class CredentialRotatePayload
    {
        public string OrgName { get; set; }
        public string PAT { get; set; }
        public Guid ProjectId { get; set; }
        public bool RotateAllServiceConnections { get; set; }
        public List<Guid> ServiceEndpoints { get; set; }
        public int DaysBeforeExpire { get; set; }
        public int LifeTimeInDays { get; set; }
    }
}
