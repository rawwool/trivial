using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using Ionic;
using System.Globalization;
using TextRuler;
using TextRuler.Model;
using System.Threading.Tasks;

//http://www.codeproject.com/Articles/9196/Links-with-arbitrary-text-in-a-RichTextBox
namespace RichTextBoxLinks
{
    public delegate void QueryListItemHandler(object sender, QueryListItemsArgs args);
    public class QueryListItemsArgs : EventArgs
    {
        public string Fragment { get; set; }
        public string PreviousWOrd { get; set; }
        public string PreviousToPreviousWord { get; set; }
        public IEnumerable<string> List { get; set; }
    }
    public delegate void QueryHandler(object sender, QueryArgs args);
    public class QueryArgs : EventArgs
    {
        public string Query { get; set; }
    }

    public delegate void WeeklyUpdatesHandler(object sender, WeeklyUpdatesArgs args);
    public class WeeklyUpdatesArgs: EventArgs
    {
        public DateTime Date { get; set; }
    }

    public class RichTextBoxEx : ExtendedRichTextBox
	{
		#region Interop-Defines
		[ StructLayout( LayoutKind.Sequential )]
		private struct CHARFORMAT2_STRUCT
		{
			public UInt32	cbSize; 
			public UInt32   dwMask; 
			public UInt32   dwEffects; 
			public Int32    yHeight; 
			public Int32    yOffset; 
			public Int32	crTextColor; 
			public byte     bCharSet; 
			public byte     bPitchAndFamily; 
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=32)]
			public char[]   szFaceName; 
			public UInt16	wWeight;
			public UInt16	sSpacing;
			public int		crBackColor; // Color.ToArgb() -> int
			public int		lcid;
			public int		dwReserved;
			public Int16	sStyle;
			public Int16	wKerning;
			public byte		bUnderlineType;
			public byte		bAnimation;
			public byte		bRevAuthor;
			public byte		bReserved1;
		}

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		private const int WM_USER			 = 0x0400;
		private const int EM_GETCHARFORMAT	 = WM_USER+58;
		private const int EM_SETCHARFORMAT	 = WM_USER+68;

		private const int SCF_SELECTION	= 0x0001;
		private const int SCF_WORD		= 0x0002;
		private const int SCF_ALL		= 0x0004;

		#region CHARFORMAT2 Flags
		private const UInt32 CFE_BOLD		= 0x0001;
		private const UInt32 CFE_ITALIC		= 0x0002;
		private const UInt32 CFE_UNDERLINE	= 0x0004;
		private const UInt32 CFE_STRIKEOUT	= 0x0008;
		private const UInt32 CFE_PROTECTED	= 0x0010;
		private const UInt32 CFE_LINK		= 0x0020;
		private const UInt32 CFE_AUTOCOLOR	= 0x40000000;
		private const UInt32 CFE_SUBSCRIPT	= 0x00010000;		/* Superscript and subscript are */
		private const UInt32 CFE_SUPERSCRIPT= 0x00020000;		/*  mutually exclusive			 */

		private const int CFM_SMALLCAPS		= 0x0040;			/* (*)	*/
		private const int CFM_ALLCAPS		= 0x0080;			/* Displayed by 3.0	*/
		private const int CFM_HIDDEN		= 0x0100;			/* Hidden by 3.0 */
		private const int CFM_OUTLINE		= 0x0200;			/* (*)	*/
		private const int CFM_SHADOW		= 0x0400;			/* (*)	*/
		private const int CFM_EMBOSS		= 0x0800;			/* (*)	*/
		private const int CFM_IMPRINT		= 0x1000;			/* (*)	*/
		private const int CFM_DISABLED		= 0x2000;
		private const int CFM_REVISED		= 0x4000;

		private const int CFM_BACKCOLOR		= 0x04000000;
		private const int CFM_LCID			= 0x02000000;
		private const int CFM_UNDERLINETYPE	= 0x00800000;		/* Many displayed by 3.0 */
		private const int CFM_WEIGHT		= 0x00400000;
		private const int CFM_SPACING		= 0x00200000;		/* Displayed by 3.0	*/
		private const int CFM_KERNING		= 0x00100000;		/* (*)	*/
		private const int CFM_STYLE			= 0x00080000;		/* (*)	*/
		private const int CFM_ANIMATION		= 0x00040000;		/* (*)	*/
		private const int CFM_REVAUTHOR		= 0x00008000;


		private const UInt32 CFM_BOLD		= 0x00000001;
		private const UInt32 CFM_ITALIC		= 0x00000002;
		private const UInt32 CFM_UNDERLINE	= 0x00000004;
		private const UInt32 CFM_STRIKEOUT	= 0x00000008;
		private const UInt32 CFM_PROTECTED	= 0x00000010;
		private const UInt32 CFM_LINK		= 0x00000020;
		private const UInt32 CFM_SIZE		= 0x80000000;
		private const UInt32 CFM_COLOR		= 0x40000000;
		private const UInt32 CFM_FACE		= 0x20000000;
		private const UInt32 CFM_OFFSET		= 0x10000000;
		private const UInt32 CFM_CHARSET	= 0x08000000;
		private const UInt32 CFM_SUBSCRIPT	= CFE_SUBSCRIPT | CFE_SUPERSCRIPT;
		private const UInt32 CFM_SUPERSCRIPT= CFM_SUBSCRIPT;

		private const byte CFU_UNDERLINENONE		= 0x00000000;
		private const byte CFU_UNDERLINE			= 0x00000001;
		private const byte CFU_UNDERLINEWORD		= 0x00000002; /* (*) displayed as ordinary underline	*/
		private const byte CFU_UNDERLINEDOUBLE		= 0x00000003; /* (*) displayed as ordinary underline	*/
		private const byte CFU_UNDERLINEDOTTED		= 0x00000004;
		private const byte CFU_UNDERLINEDASH		= 0x00000005;
		private const byte CFU_UNDERLINEDASHDOT		= 0x00000006;
		private const byte CFU_UNDERLINEDASHDOTDOT	= 0x00000007;
		private const byte CFU_UNDERLINEWAVE		= 0x00000008;
		private const byte CFU_UNDERLINETHICK		= 0x00000009;
		private const byte CFU_UNDERLINEHAIRLINE	= 0x0000000A; /* (*) displayed as ordinary underline	*/

		#endregion

		#endregion

        public event QueryListItemHandler QueryListItem;
        public event QueryHandler Query;
        public event LinkClickedEventHandler LinkClickedEx;

        int _AutoCompleteCount = 0;
        bool _AutoCompleteListShow = false;
        string _AutoCompleteKeyword = string.Empty;
        ListBox _ListBox = new ListBox();
        public class AsyncId
        {

        }
        Stack<AsyncId> _StackAsync = new Stack<AsyncId>();
        
		public RichTextBoxEx():base()
		{
			// Otherwise, non-standard links get lost when user starts typing
			// next to a non-standard link
			this.DetectUrls = true;
            
            //this.LinkClicked2 += RichTextBoxEx_LinkClicked;
            this.LinkClicked += RichTextBoxEx_LinkClicked;
            this.KeyDown += RichTextBoxEx_KeyDown;
            this.KeyPress += RichTextBoxEx_KeyPress;
            this.KeyUp += RichTextBoxEx_KeyUp;
            _ListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _ListBox.Items.AddRange(new object[] {"Aayush", "Isha", "Mohua", "Rio", "Gojubaba"});
            _ListBox.FormattingEnabled = true;
            _ListBox.Location = new System.Drawing.Point(3, 78);
            _ListBox.Name = "listBox1";
            _ListBox.Size = new System.Drawing.Size(250, 180);
            _ListBox.TabIndex = 10;
            _ListBox.Font = new Font("Arial Unicode MS", 12); //this.Font;
            _ListBox.ForeColor = Color.Gray;
            this.Controls.Add(_ListBox);
            _ListBox.Hide();
            _ListBox.Items.Clear();
            _ListBox.KeyDown += _ListBox_KeyDown;
            //_ListBox.VisibleChanged += _ListBox_VisibleChanged;
		}

        private void RichTextBoxEx_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            LinkClickedEx?.Invoke(this, e);
        }

        void RichTextBoxEx_KeyUp(object sender, KeyEventArgs e)
        {
            
        }

        protected override bool ProcessCmdKey(ref Message m, Keys keyData)
        {
            if (keyData == Keys.Enter || keyData == Keys.Tab)
            {
                if (_ListBox.Visible == false) return false;
                string autoText = string.Empty;
                if (_ListBox.SelectedItem != null)
                {
                    autoText = _ListBox.SelectedItem.ToString();
                    autoText = autoText.Substring(_AutoCompleteKeyword.Length, autoText.Length - _AutoCompleteKeyword.Length);
                }
                int beginPlace = this.SelectionStart;
                this.SelectedText = autoText;
                this.SelectionStart = beginPlace + _AutoCompleteKeyword.Length;

                this.Focus();
                _AutoCompleteListShow = false;
                _ListBox.Hide();
                _ListBox.Items.Clear();
                int endPlace = autoText.Length + beginPlace;
                this.SelectionStart = endPlace;
                _AutoCompleteCount = 0;
                _AutoCompleteKeyword = string.Empty;
                return true;
            }
            return base.ProcessCmdKey(ref m, keyData);
        }

        void _ListBox_VisibleChanged(object sender, EventArgs e)
        {
            ListBox box = sender as ListBox;
            if (box != null && box.Visible && box.Left <= 100)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        async void RichTextBoxEx_KeyPress(object sender, KeyPressEventArgs e)
        {
            _ListBox.Items.Clear();
            string currentWord = GetCurrentWord();
            int currentWordLength = currentWord.Length;
            if ((!_AutoCompleteListShow && (e.KeyChar =='%' || e.KeyChar == '@' || e.KeyChar == '#' || char.IsLetter(e.KeyChar)))
                || (currentWordLength == 0 && (e.KeyChar == '%' || e.KeyChar == '@' || e.KeyChar == '#')))
            {
                //string currentWord = GetCurrentWord();
                _AutoCompleteKeyword = currentWord + e.KeyChar;
                QueryListItemsArgs args = new QueryListItemsArgs() { Fragment = _AutoCompleteKeyword };
                if (QueryListItem != null)
                {
                    AsyncId id = new AsyncId();
                    _StackAsync.Push(id);
                    await Task.Run(() => QueryListItem(this, args));
                    if (_StackAsync.Count == 0 || (_StackAsync.Count > 0 && _StackAsync.Peek() != id)) return;

                    _StackAsync.Clear();
                }
                if (args.List == null || args.List.FirstOrDefault() == null)
                {
                    return;
                }

                _ListBox.Items.Clear();
                _ListBox.Items.AddRange(args.List.ToArray());
                _ListBox.SelectedIndex = 0;
                Point point = this.GetPositionFromCharIndex(this.SelectionStart - _AutoCompleteKeyword.Length + 1);
                
                SizeF size = MeasureString(
                        _ListBox.Font,
                        args.List.ToArray().OrderByDescending(s => s.Length).FirstOrDefault()
                        );
                _ListBox.Width = (int)size.Width + 15;
                _ListBox.Height = Math.Min(150, ((int)size.Height + 3) * args.List.Count());
                if (point.Y > this.ClientSize.Height - _ListBox.Height - 20)
                {
                    point.Y -= _ListBox.Height;
                }
                else
                {
                    point.Y += (int)Math.Ceiling(this.Font.GetHeight());
                }
                if (point.X > this.ClientSize.Width - _ListBox.Width - 20)
                {
                    point.X -= _ListBox.Width;
                }
                else
                {
                    point.X += Location.X; //105 is the .x postion of the richtectbox
                }
                _ListBox.Location = point;
                _AutoCompleteCount++;
                _ListBox.Show();
                _ListBox.BringToFront();
                //_ListBox.SelectedIndex = _ListBox.FindString(_AutoCompleteKeyword);
                this.Focus();

                //e.Handled = true;
                _AutoCompleteListShow = true;
                return;
            }

            
        }

        void _ListBox_KeyUp(object sender, KeyEventArgs e)
        {
            
        }


        void _ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return || e.KeyCode == Keys.Tab)
            {
                this.SelectedText = _ListBox.SelectedItem.ToString();
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ListBox.Hide();
            }
        }

 
        private void RichTextBoxEx_KeyDown(object sender, KeyEventArgs e)
        {
            if (Query != null && Lines != null && (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter))
            {
                var pos = this.GetRealLineForCharIndex(this.SelectionStart);
                if (pos >= 0 && Lines.Count() > pos)
                {
                    string text = Lines[pos];

                    if (text != null && text.Length > 0 && text.Trim().StartsWith(">"))
                    {
                        Query(this, new QueryArgs() { Query = text.Trim().TrimStart('>') });
                        //e.Handled = true;
                    }
                }
            }
            if (!e.Handled) HandleAutoComplete(sender, e);
        }

        private async void HandleAutoComplete(object sender, KeyEventArgs e)
        {
            char currentChar = Convert.ToChar(e.KeyValue);
            if (!e.Control && e.KeyCode == Keys.Space)
            {
                _AutoCompleteListShow = false;
                _ListBox.Hide();
                _ListBox.Items.Clear();
            }

            if ((e.Control && e.KeyCode == Keys.Space)) /*|| ((e.Shift && currentChar == '#') || currentChar == '@')*/
            {
                string currentWord = GetCurrentWord();
                _AutoCompleteKeyword = currentWord;
                QueryListItemsArgs args = new QueryListItemsArgs() { Fragment = _AutoCompleteKeyword };
                if (QueryListItem != null)
                {
                    AsyncId id = new AsyncId();
                    _StackAsync.Push(id);
                    await Task.Run(() => QueryListItem(this, args));
                    if (_StackAsync.Count == 0 || (_StackAsync.Count > 0 && _StackAsync.Peek() != id)) return;

                    _StackAsync.Clear();
                }
                if (args.List == null || args.List.FirstOrDefault() == null)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                
                _ListBox.Items.Clear();
                _ListBox.Items.AddRange(args.List.ToArray());
                _ListBox.SelectedIndex = 0;
                Point point = this.GetPositionFromCharIndex(this.SelectionStart - _AutoCompleteKeyword.Length);
                SizeF size = MeasureString(
                        _ListBox.Font,
                        args.List.ToArray().OrderByDescending(s => s.Length).FirstOrDefault()
                        );
                _ListBox.Width = (int)size.Width + 15;
                _ListBox.Height = Math.Min(150, ((int)size.Height + 3) * args.List.Count());
                if (point.Y > this.ClientSize.Height - _ListBox.Height - 20)
                {
                    point.Y -= _ListBox.Height;
                }
                else
                {
                    point.Y += (int)Math.Ceiling(this.Font.GetHeight());
                }
                if (point.X > this.ClientSize.Width - _ListBox.Width - 20)
                {
                    point.X -= _ListBox.Width;
                }
                else
                {
                    point.X += Location.X; //105 is the .x postion of the richtectbox
                }
                
                _ListBox.Location = point;
                _AutoCompleteCount++;
                    this.SelectionStart--;
                _ListBox.Show();
                _ListBox.BringToFront();
                //_ListBox.SelectedIndex = _ListBox.FindString(_AutoCompleteKeyword);
                this.Focus();
                
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                _AutoCompleteListShow = true;
                return;
            }

            //if (e.KeyCode == Keys.Space)
            //{
            //    _AutoCompleteCount = 0;
            //    _AutoCompleteKeyword = "<";
            //    _AutoCompleteListShow = false;
            //    listBox1.Hide();
            //}

            if (_AutoCompleteListShow == true) /*Section 2*/
            {
                int backAdjustiment = 0;
                if (e.KeyCode != Keys.Tab
                    && e.KeyCode != Keys.Enter
                    && e.KeyCode != Keys.Up
                    && e.KeyCode != Keys.Down
                    && (char.IsLetter(currentChar) || e.KeyCode == Keys.Back))
                {
                    string word = _AutoCompleteKeyword;
                    if (e.KeyCode != Keys.Back)
                    {
                        word += Convert.ToChar(e.KeyValue);
                    }
                    else
                    {
                        backAdjustiment = 1;
                        if (word.Length > 0)
                        {
                            word = word.Substring(0, word.Length - 1);
                        }
                        if (word.Length <= 0)
                        {
                            _AutoCompleteCount = 0;
                            _AutoCompleteKeyword = "";
                            _AutoCompleteListShow = false;
                            _ListBox.Hide();
                            _ListBox.Items.Clear();
                        }
                    }

                    if (_AutoCompleteKeyword != word && word.Length > 0)
                    {
                        _AutoCompleteKeyword = word;
                        QueryListItemsArgs args = new QueryListItemsArgs() { Fragment = _AutoCompleteKeyword };
                        if (QueryListItem != null)
                        {
                            AsyncId id = new AsyncId();
                            _StackAsync.Push(id);
                            await Task.Run(() => QueryListItem(this, args));
                            if (_StackAsync.Count == 0 || (_StackAsync.Count > 0 && _StackAsync.Peek() != id)) return;

                            _StackAsync.Clear();
                        }
                        _ListBox.Items.Clear();
                        if (args.List == null || args.List.FirstOrDefault() == null)
                        {
                            _ListBox.Items.Clear();
                        }
                        else
                        {
                            _ListBox.Items.AddRange(args.List.ToArray());
                            _ListBox.SelectedIndex = 0;
                        }
                        if (_ListBox.Items.Count > 0)
                        {
                            int positionOfListBox = this.SelectionStart - _AutoCompleteKeyword.Length + 1 - backAdjustiment;
                            if (positionOfListBox < 0) positionOfListBox = 0;
                            Point point = this.GetPositionFromCharIndex(positionOfListBox);
                            SizeF size = MeasureString(
                                _ListBox.Font,
                                args.List.ToArray().OrderByDescending(s => s.Length).FirstOrDefault()
                                );
                            _ListBox.Width = (int)size.Width + 15;
                            _ListBox.Height = Math.Min(150, ((int)size.Height + 3) * args.List.Count());
                            if (point.Y > this.ClientSize.Height - _ListBox.Height - 20)
                            {
                                point.Y -= _ListBox.Height;
                            }
                            else
                            {
                                point.Y += (int)Math.Ceiling(this.Font.GetHeight());
                            }
                            if (point.X > this.ClientSize.Width - _ListBox.Width - 20)
                            {
                                point.X -= _ListBox.Width;
                            }
                            else
                            {
                                point.X += Location.X; //105 is the .x postion of the richtectbox
                            }
                            _ListBox.Location = point;
                            _ListBox.Show();
                        }
                        else
                        {
                            _AutoCompleteListShow = false;
                            _ListBox.Hide();
                            _ListBox.Items.Clear();
                        }
                    }
                }

                if (e.KeyCode == Keys.Up)
                {
                    _ListBox.Focus();
                    if (_ListBox.SelectedIndex != 0)
                    {
                        _ListBox.SelectedIndex -= 1;
                    }
                    else
                    {
                        _ListBox.SelectedIndex = 0;
                    }
                    this.Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;

                }
                else if (e.KeyCode == Keys.Down)
                { 
                    _ListBox.Focus();
                    try
                    {
                        _ListBox.SelectedIndex += 1;
                    }
                    catch
                    {
                    }
                    this.Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

               

                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Space || !char.IsLetterOrDigit(currentChar)) /*Section 1*/
                {
                    _AutoCompleteCount = 0;
                    _AutoCompleteKeyword = "";
                    _AutoCompleteListShow = false;
                    _ListBox.Hide();
                    _ListBox.Items.Clear();

                }
            }

            if (e.KeyCode == Keys.Space)
            {
                string prevWord = GetPreviousWord();
                SpellCheck(prevWord);

                string[] previousWords = GetPreviousWordsInThisSentence();
                if (previousWords == null || previousWords.Length == 0) return;

                AutoCorrectPreviousWord(previousWords);


                QueryListItemsArgs args = new QueryListItemsArgs() { PreviousWOrd = previousWords[0], 
                    PreviousToPreviousWord = previousWords.Length > 1 ? previousWords[1] : string.Empty};
                if (QueryListItem != null)
                {
                    AsyncId id = new AsyncId();
                    _StackAsync.Push(id);
                    await Task.Run(() => QueryListItem(this, args));
                    if (_StackAsync.Count == 0 || (_StackAsync.Count > 0 && _StackAsync.Peek() != id)) return;

                    _StackAsync.Clear();
                }
                if (args.List != null && args.List.FirstOrDefault() != null)
                {
                    _ListBox.Items.Clear();
                    if (args.List == null || args.List.FirstOrDefault() == null)
                    {
                        _ListBox.Items.Clear();
                    }
                    else
                    {
                        _ListBox.Items.AddRange(args.List.ToArray());
                        _ListBox.SelectedIndex = 0;
                    }
                    SizeF size = MeasureString(
                        _ListBox.Font,
                        args.List.ToArray().OrderByDescending(s => s.Length).FirstOrDefault()
                        );
                    _ListBox.Width = (int)size.Width + 15;
                    _ListBox.Height = Math.Min(150, ((int)size.Height + 3) * args.List.Count());

                    int positionOfListBox = this.SelectionStart;
                    if (positionOfListBox < 0) positionOfListBox = 0;
                    Point point = this.GetPositionFromCharIndex(positionOfListBox);
                    
                    if (point.Y > this.ClientSize.Height - _ListBox.Height - 20)
                    {
                        point.Y -= _ListBox.Height;
                    }
                    else
                    {
                        point.Y += (int)Math.Ceiling(this.Font.GetHeight());
                    }
                    if (point.X > this.ClientSize.Width - _ListBox.Width - 20)
                    {
                        point.X -= _ListBox.Width;
                    }
                    else
                    {
                        point.X += Location.X; //105 is the .x postion of the richtectbox
                    }
                    _ListBox.Items.Add("Count="+_ListBox.Items.Count.ToString());
                    _ListBox.Location = point;
                    _ListBox.Show();
                    _ListBox.BringToFront();
                    _ListBox.Refresh();
                    //_ListBox.Update();
                    _AutoCompleteListShow = true;
                    _AutoCompleteKeyword = string.Empty;
                    this.Focus();
                }
            }

            if (e.Control && e.KeyCode == Keys.V)
            {
                if (!Clipboard.ContainsImage())
                {
                    string text = Clipboard.GetText(TextDataFormat.UnicodeText);
                    //((RichTextBox)sender).Paste(DataFormats.GetFormat("Text"));
                    this.SelectedText = text;
                    e.Handled = true;
                }
            }
        }

        private SizeF MeasureString(Font font, string text)
        {
            SizeF size = default(SizeF);
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(new Bitmap(1, 1)))
            {
                size = graphics.MeasureString(text, font);
            }
            return size;
        }

        private void SpellCheck(string previousWord)
        {
            //int pos = this.SelectionStart;
            //int firstCharPos = pos - previousWord.Length;
            //this.Select(firstCharPos, previousWord.Length);
            //this.SelectionFont = new Font(this.SelectionFont, FontStyle.Underline);
            //contactsTextBox.DeselectAll();
        }

        private void AutoCorrectPreviousWord(string[] previousWords)
        {
            if (previousWords == null || previousWords.Length < 1 || previousWords.Length > 1) return;
            string previousWord = previousWords[0];
            if (previousWord.Length > 0)
            {
                // Upper case
                if (char.IsLetter(previousWord[0]) && char.IsLower(previousWord[0]))
                {
                    int pos = this.SelectionStart;
                    int firstCharPos = pos - previousWord.Length;
                    this.Select(firstCharPos, 1);
                    this.SelectedText = char.ToUpper(previousWord[0]).ToString();//CultureInfo.CurrentCulture.TextInfo.ToTitleCase(previousWord);
                    this.SelectionStart = pos;
                }
                //Spelling: TODO
            }
        }

        internal string GetCurrentFullWord()
        {
            string fragment = string.Empty;
            int pos = this.SelectionStart - 1;
            if (pos >= 0)
            {
                char c = this.Text[pos];
                
                while (c != ' ' && c != '\n' && pos >= 0)
                {
                    //if (c != '\n')
                    //{
                    //    fragment = c + fragment;
                    //}
                    pos--;
                    if (pos >= 0) c = this.Text[pos];
                }
                //if (Text.Length > pos - 1)
                if (pos < Text.Length - 1)
                {
                    pos++;
                    c = this.Text[pos];
                }

                while (c != ' ' && c != '\n' && c != '\'' && c != '.')
                {
                    fragment = fragment + c;
                    pos++;
                    if (pos >= Text.Length) break;
                    c = this.Text[pos];
                }
            }
            return fragment.TrimStart(DataProvider.WordStartingSymbols);
        }

        private string GetCurrentWord()
        {
            string fragment = string.Empty;
            int pos = this.SelectionStart-1;
            if (pos >= 0)
            {
                char c = this.Text[pos];

                while (c != ' ' && c != '\n' && pos >= 0)
                {
                    if (c != '\n')
                    {
                        fragment = c + fragment;
                    }
                    pos--;
                    if (pos >= 0) c = this.Text[pos];
                }
            }
            return fragment.TrimStart(DataProvider.WordStartingSymbols);
        }

        private string GetPreviousWord()
        {
            int pos = this.SelectionStart - 1;
            string previousWord = string.Empty;
            if (pos >= 0)
            {
                char c = this.Text[pos];

                while(!char.IsLetter(c) && pos > 0)
                {
                    pos--;
                    c = this.Text[pos];
                }

                while (char.IsLetter(c) && pos >= 0)
                {
                    previousWord = c + previousWord;
                    pos--;
                    if (pos >= 0) c = this.Text[pos];
                }
            }
            return previousWord;
        }

        private string[] GetPreviousWordsInThisSentence()
        {
            List<string> words = new List<string>();
            int pos = this.SelectionStart - 1;
            string previousWord = string.Empty;
            string previousToPreviousWord = string.Empty;
            if (pos >= 0)
            {
                char c = this.Text[pos];
                bool isSpecialSymbolPartOfWord = (pos-1> 0) && Text[pos-1] != ' ' && (c == '.' || c == '!' || c == '?');

                while (c != ' ' && c != '\n' && pos >= 0 &&  c != '.' && c != '!')
                {
                    if (c != '\n' && char.IsLetterOrDigit(c))
                    {
                        previousWord = c + previousWord;
                    }
                    pos--;
                    if (pos >= 0) c = this.Text[pos];
                }
                if (previousWord.Length > 0)
                {
                    words.Add(previousWord);

                    if (pos >= 0)
                    {
                        c = this.Text[pos];
                        while ((c == ' ' || c == '\n') && pos >= 0)
                        {
                            pos--;
                            if (pos>=0)
                                c = this.Text[pos];
                        }
                    }

                    while (c != ' ' && c != '\n' && c!= '.' && c != '!' && pos >= 0)
                    {
                        if (c != '\n' && char.IsLetterOrDigit(c))
                        {
                            previousToPreviousWord = c + previousToPreviousWord;
                        }
                        pos--;
                        if (pos >= 0) c = this.Text[pos];
                    }
                    if (previousToPreviousWord.Length > 0)
                    {
                        words.Add(previousToPreviousWord);
                    }
                }
            }
            return words.ToArray();
        }

        //protected override void OnLinkClicked(LinkClickedEventArgs e)
        //{
        //    LinkClicked2?.Invoke(this, e);
        //    //base.OnLinkClicked(e);
        //}

        //void RichTextBoxEx_LinkClicked(object sender, LinkClickedEventArgs e)
        //{
        //    //MessageBox.Show(e.LinkText);
        //    string linkText = e.LinkText;
        //    if (LinkClicked2 != null)
        //    {
        //        LinkClicked2(sender, e);
        //    }
        //}

		[DefaultValue(false)]
		public new bool DetectUrls
		{
			get { return base.DetectUrls; }
			set { base.DetectUrls = value; }
		}

		/// <summary>
		/// Insert a given text as a link into the RichTextBox at the current insert position.
		/// </summary>
		/// <param name="text">Text to be inserted</param>
		public void InsertLink(string text)
		{
			InsertLink(text, this.SelectionStart);
		}

		/// <summary>
		/// Insert a given text at a given position as a link. 
		/// </summary>
		/// <param name="text">Text to be inserted</param>
		/// <param name="position">Insert position</param>
		public void InsertLink(string text, int position)
		{
            //if (position < 0 || position > this.Text.Length)
            //    throw new ArgumentOutOfRangeException("position");

			this.SelectionStart = position;
			this.SelectedText = text;
			this.Select(position, text.Length);
			this.SetSelectionLink(true);
			this.Select(position + text.Length, 0);
		}
		
		/// <summary>
		/// Insert a given text at at the current input position as a link.
		/// The link text is followed by a hash (#) and the given hyperlink text, both of
		/// them invisible.
		/// When clicked on, the whole link text and hyperlink string are given in the
		/// LinkClickedEventArgs.
		/// </summary>
		/// <param name="text">Text to be inserted</param>
		/// <param name="hyperlink">Invisible hyperlink string to be inserted</param>
		public void InsertLink(string text, string hyperlink)
		{
			InsertLink(text, hyperlink, this.SelectionStart);
		}

		/// <summary>
		/// Insert a given text at a given position as a link. The link text is followed by
		/// a hash (#) and the given hyperlink text, both of them invisible.
		/// When clicked on, the whole link text and hyperlink string are given in the
		/// LinkClickedEventArgs.
		/// </summary>
		/// <param name="text">Text to be inserted</param>
		/// <param name="hyperlink">Invisible hyperlink string to be inserted</param>
		/// <param name="position">Insert position</param>
		public void InsertLink(string text, string hyperlink, int position)
		{
            //if (position < 0 || position > this.Text.Length)
            //    throw new ArgumentOutOfRangeException("position");

			this.SelectionStart = position;
            
			this.SelectedRtf = @"{\rtf1\ansi "+text+@"\v #"+hyperlink+@"\v0}";
			this.Select(position, text.Length + hyperlink.Length + 1);
			this.SetSelectionLink(true);
			this.Select(position + text.Length + hyperlink.Length + 1, 0);
            
            
		}

		/// <summary>
		/// Set the current selection's link style
		/// </summary>
		/// <param name="link">true: set link style, false: clear link style</param>
		public void SetSelectionLink(bool link)
		{
			SetSelectionStyle(CFM_LINK, link ? CFE_LINK : 0);
		}
		/// <summary>
		/// Get the link style for the current selection
		/// </summary>
		/// <returns>0: link style not set, 1: link style set, -1: mixed</returns>
		public int GetSelectionLink()
		{
			return GetSelectionStyle(CFM_LINK, CFE_LINK);
		}


		private void SetSelectionStyle(UInt32 mask, UInt32 effect)
		{
			CHARFORMAT2_STRUCT cf = new CHARFORMAT2_STRUCT();
			cf.cbSize = (UInt32)Marshal.SizeOf(cf);
			cf.dwMask = mask;
			cf.dwEffects = effect;

			IntPtr wpar = new IntPtr(SCF_SELECTION);
			IntPtr lpar = Marshal.AllocCoTaskMem( Marshal.SizeOf( cf ) ); 
			Marshal.StructureToPtr(cf, lpar, false);

			IntPtr res = SendMessage(Handle, EM_SETCHARFORMAT, wpar, lpar);

			Marshal.FreeCoTaskMem(lpar);
		}

		private int GetSelectionStyle(UInt32 mask, UInt32 effect)
		{
			CHARFORMAT2_STRUCT cf = new CHARFORMAT2_STRUCT();
			cf.cbSize = (UInt32)Marshal.SizeOf(cf);
			cf.szFaceName = new char[32];

			IntPtr wpar = new IntPtr(SCF_SELECTION);
			IntPtr lpar = 	Marshal.AllocCoTaskMem( Marshal.SizeOf( cf ) ); 
			Marshal.StructureToPtr(cf, lpar, false);

			IntPtr res = SendMessage(Handle, EM_GETCHARFORMAT, wpar, lpar);

			cf = (CHARFORMAT2_STRUCT)Marshal.PtrToStructure(lpar, typeof(CHARFORMAT2_STRUCT));

			int state;
			// dwMask holds the information which properties are consistent throughout the selection:
			if ((cf.dwMask & mask) == mask) 
			{
				if ((cf.dwEffects & effect) == effect)
					state = 1;
				else
					state = 0;
			}
			else
			{
				state = -1;
			}
			
			Marshal.FreeCoTaskMem(lpar);
			return state;
		}

        /// <summary>
        /// This additional code block checks the locations of links
        /// and desc. it via a string which contains informations of how many links are there
        /// .Split('&')-1 and the select information .Select(.Split('&')[i].Split('-')[0],.Split('&')[i].Split('-')[1])
        /// After we select the links we can SetSelectionLink(true) to get our links back.
        /// </summary>
        public string getLinkPositions()
        {
            string pos = "";
            for (int i = 0; i < this.TextLength; i++)
            {
                this.Select(i, 1);
                int isLink = GetSelectionLink();
                if (isLink == 1)
                {
                    //the selected first character is a part of link, now find its last character
                    for (int j = i + 1; j <= this.TextLength; j++)
                    {
                        this.Select(j, 1);
                        isLink = GetSelectionLink();
                        if (isLink != 1 || j == this.TextLength)
                        {
                            //we found the last character's +1 so end char is (j-1), start char is (i)
                            pos += (i) + "-" + ((j - 1) - (i - 1)) + "&"; //j-1 to i but i inserted -1 one more so we can determine the right pos
                            i = j; //cont. from j+1
                            break; //exit second for cont. from i = j+1 (i will increase on new i value)
                        }
                    }
                }
            }
            this.DeselectAll();
            return pos;
        }
        /// <summary>
        /// This method generates the links back only created via InsertLink(string text)
        /// and overloaded InsertLink(string text,int position)
        /// </summary>
        /// <param name="pos">the pos string from getLinkPositions</param>
        public void setLinkPositions(string pos)
        {
            string[] positions = pos.Split('&');
            for (int i = 0; i < positions.Length - 1; i++)
            {
                string[] xy = positions[i].Split('-');
                this.Select(Int32.Parse(xy[0]), Int32.Parse(xy[1]));
                this.SetSelectionLink(true);
                this.Select(Int32.Parse(xy[0]) + Int32.Parse(xy[1]), 0);
            }
            this.DeselectAll();
        }
	}
}
