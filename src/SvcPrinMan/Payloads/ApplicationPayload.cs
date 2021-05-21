using System;
using System.Collections.Generic;
using System.Text;

namespace SvcPrinMan.Payloads
{
    public class ApplicationPayload
    {
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public List<string> Tags { get; set; }
    }
}
