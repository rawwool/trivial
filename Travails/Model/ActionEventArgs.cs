using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Travails.Model
{
    public class ActionEventArgs: EventArgs
    {
        public DateTime Date { get; set; }
        public string Name { get; set; }

        public string FileName { get; set; }

        public int LineIndex { get; set; }
    }
}
