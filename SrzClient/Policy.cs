using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SrzClient
{
    public class Policy
    {
        public string Organisation { get; set; }
        public string Kind { get; set; }
        public string Number { get; set; }
        public DateTime? Issued { get; set; }
        public DateTime? ValidTo { get; set; }
        public DateTime? Closed { get; set; }
        public string CloseReason { get; set; }

        public override string ToString()
        {
            return this.PropertiesToString(Environment.NewLine);
        }
    }
}
