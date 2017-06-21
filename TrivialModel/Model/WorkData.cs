using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TrivialModel.Model;

namespace TrivialModel
{
    [DataContract]
    public class WorkData
    {
        [DataMember]
        public DateTime From { get; set; }
        [DataMember]
        public DateTime To { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int FromLine { get; set; }
        [DataMember]
        public int ToLine { get; set; }
        //[DataMember]
        public List<InformationData> InfoList = new List<InformationData>();
        //[DataMember]
        public List<string> TrackList = new List<string>();
        public List<InfoFlow> InfoFlows = new List<InfoFlow>();
    }
}
