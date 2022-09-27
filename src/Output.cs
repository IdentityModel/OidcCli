using System;
using System.Collections.Generic;

namespace OidcCli;

internal class Output
{
    public string error { get; set; }

    public IEnumerable<claim> claims { get; set; }
    public string id_token { get; set; }
    public string access_token { get; set; }
    public string refresh_token { get; set; }
    public DateTimeOffset expires_at { get; set; }
        
    public class claim
    {
        public string type { get; set; }
        public string value { get; set; }
    }
}