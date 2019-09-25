using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TextRuler;

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
        public DateTime EffectiveDueDate {
            get
            {
                DateTime dueDate = DateDue;
                if (InfoList != null && InfoList.FirstOrDefault() != null)
                {
                    DateTime infoMaxDate = InfoList.OrderByDescending(d => d.DateTime).First().DateTime;
                    if (infoMaxDate > dueDate) dueDate = infoMaxDate;
                }
                return dueDate;
            }
        }
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

        public string GetTitle()
        {
            List<string> titles = new List<string>();
            if (Name.GetTitle().Length > 0) titles.Add(Name.GetTitle());
            foreach(var info in InfoList)
            {
                if (info.Information.GetTitle().Length > 0) titles.Add(info.Information.GetTitle());
            }
            var title = titles.LastOrDefault();
            if (title != null) return title;
            return Name.RemoveBetweenAngBracketsInclusive();
        }
    }
}
