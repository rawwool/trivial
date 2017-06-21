﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace TrivialModel
{
    [DataContract]
    public class ActionData
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<string> InputFrom = new List<string>();
        [DataMember]
        public List<string> InputTo = new List<string>();
        [DataMember]
        public DateTime DateLogged { get; set; }
        [DataMember]
        public DateTime DateDue { get; set; }
        [DataMember]
        public DateTime DateCompleted { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public List<string> HashTags = new List<string>();
        [DataMember]
        public List<string> PeopleList = new List<string>();
        [DataMember]
        public List<string> TrackList = new List<string>();
        [DataMember]
        public DayData Parent { get; set; }

        [DataMember]
        public List<InformationData> InfoList = new List<InformationData>();
    }
}