using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoreKit.Options
{
    public class HttpServiceOptions
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; set; } = 3;
        public Dictionary<string, string>? DefaultHeaders { get; set; }
    }
}
