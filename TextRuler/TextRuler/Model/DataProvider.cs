using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextRuler.Model
{
    public static class DataProvider
    {
        static char[] _WordStartingSymbols = null;
        public static char[] WordStartingSymbols
        {
            get
            {
                if (_WordStartingSymbols == null)
                    _WordStartingSymbols = new char[] { 'Ⓘ', '✓', '▶', 'Ⓢ', 'Ⓔ', '>', '<' };
                return _WordStartingSymbols;
            }
        }
    }
}
