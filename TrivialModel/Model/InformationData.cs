using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace TrivialModel
{
    [DataContract]
    public class InformationData
    {
        [DataMember]
        public DateTime DateTime { get; set; }
        [DataMember]
        public string Information { get; set; }
        public override string ToString()
        {
            return string.Format("{0}{1} {2}", 'Ⓘ', DateTime, Information);
        }
    }
}
