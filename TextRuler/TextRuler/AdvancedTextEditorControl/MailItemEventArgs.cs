using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextRuler.AdvancedTextEditorControl
{
    public class MailItemEventArgs: EventArgs
    {
        public string MailId { get; set; }
        public bool IsFalgged { get; set; }
    }
}
