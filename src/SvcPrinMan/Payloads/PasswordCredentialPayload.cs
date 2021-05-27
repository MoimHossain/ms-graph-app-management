using System;
using System.Collections.Generic;
using System.Text;

namespace SvcPrinMan.Payloads
{
    public abstract class CredentialPayload
    {
        public string DisplayName { get; set; }
        public DateTimeOffset? EndDateTime { get; set; }
        public DateTimeOffset? StartDateTime { get; set; }
    }

    public class PasswordCredentialPayload : CredentialPayload
    {
        public string Hint { get; set; }

        public string SecretText { get; set; }
    }

    public class CertificateCredentialPayload : CredentialPayload
    {
        public byte[] RawPfxContent { get; set; }
    }
}
