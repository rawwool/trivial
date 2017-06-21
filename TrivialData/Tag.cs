using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrivialData
{
    // POCO class
    public class Tag
    {
        public Tag()
        {
            FollowUps = new List<string>();
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string ContainerName { get; set; }
        public string ParentContentType { get; set; }
        public string DocumentName { get; set; }
        public string DocumentPath { get; set; }
        public string Line { get; set; }
        public DateTime DateTime { get; set; }
        public List<string> FollowUps { get; set; }
    }

}
