using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace TrivialModel
{
    [DataContract]
    public class DayData
    {
        [DataMember]
        public DateTime Date { get; set; }
        //[DataMember]
        public List<ActionData> Actions = new List<ActionData>();
        //[DataMember]
        public List<string> HashTags = new List<string>();
        //[DataMember]
        public List<string> PeopleList = new List<string>();
        //[DataMember]
        public List<WorkData> WorkList = new List<WorkData>();

        public Tuple<DateTime, DateTime> StartEnd
        {
            get
            {
                var fromLList = WorkList.Select(s => s.From).OrderBy(s => s);
                var toLList = WorkList.Select(s => s.To).OrderByDescending(s => s);
                DateTime first = fromLList.FirstOrDefault();
                DateTime last = toLList.FirstOrDefault();
                if (first <= Date || first.Date != Date) first = Date.AddHours(8.5);
                if (last <= first) last = first.AddHours(8);
                return new Tuple<DateTime, DateTime>(first, last);
            }
        }

        public IEnumerable<Tuple<string, Double>> Tracks
        {
            get
            {
                var tracks = WorkList
                    .Select(s => new { Work = s, MainTrack = s.TrackList.FirstOrDefault() == default(string) ? "<None>" : s.TrackList.FirstOrDefault() })
                    .GroupBy(s => s.MainTrack)
                    .Select(s => new Tuple<string, double>(s.Key, s.Sum(p => p.Work.To.Subtract(p.Work.From).TotalHours)));
                return tracks;
            }
        }

        public DayData()
        {

        }
    }
}
