using System;
using System.Collections.Generic;
using System.Text;

namespace SvcPrinMan.Payloads
{
    public class PasswordCredentialPayload
    {
        public string DisplayName { get; set; }
        public DateTimeOffset? EndDateTime { get; set; }
        public DateTimeOffset? StartDateTime { get; set; }
        public string Hint { get; set; }

        public string SecretText { get; set; }
    }
}
