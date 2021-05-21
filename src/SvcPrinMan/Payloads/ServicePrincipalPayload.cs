using System;
using System.Collections.Generic;
using System.Text;

namespace SvcPrinMan.Payloads
{
    public class ServicePrincipalPayload
    {
        public string Description { get; set; }
        public Guid AppId { get; set; }
        public List<string> Tags { get; set; }
    }
}
