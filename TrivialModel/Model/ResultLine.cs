using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TrivialModel.Model
{
    [DataContract]
    public class ResultLines
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<string> Lines { get; set; }
    }
}
