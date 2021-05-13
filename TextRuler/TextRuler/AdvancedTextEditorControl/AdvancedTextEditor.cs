using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TextRuler;
using System.Drawing.Imaging;
using System.Linq;
using RichTextBoxLinks;
using System.Threading.Tasks;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace TextRuler.AdvancedTextEditorControl
{
    public partial class AdvancedTextEditor : UserControl
    {
        public class SaveArgs : EventArgs
        {
            public string File { get; set;}
        }

        public event SaveHandler OnSave;
        public event SaveHandler OnOpen;
        public delegate void SaveHandler(object o, SaveArgs e);
        public event QueryListItemHandler QueryListItem;
        public event QueryHandler Query;
        public event WeeklyUpdatesHandler WeeklyUpdates;
        public event EventHandler PreviousDocument;
        public event EventHandler NextDocument;
        public event LinkClickedEventHandler LinkClicked;

        string _path = "";
        int checkPrint = 0;


        public string[] Lines { get { return this.TextEditor.Lines; } }
        public IEnumerable<string> RealLines { get { return this.TextEditor.RealLines; } }
        private string GetFilePath()
        {
            OpenFileDialog o = new OpenFileDialog();
            o.Multiselect = false;
            o.RestoreDirectory = true;
            o.ShowReadOnly = false;
            o.ReadOnlyChecked = false;
            o.Filter = "RTF (*.rtf)|*.rtf|TXT (*.txt)|*.txt";
            if (o.ShowDialog(this) == DialogResult.OK)
            {
                return o.FileName;
            }
            else
            {
                return "";
            }
        }

        public void SetDocumentDate(DateTime date)
        {
            this.TextEditor.DocumentDate = date;
        }

        private string SetFilePath()
        {
            SaveFileDialog s = new SaveFileDialog();
            s.Filter = "RTF (*.rtf)|*.rtf|TXT (*.txt)|*.txt";
            if (s.ShowDialog(this) == DialogResult.OK)
            {
                return s.FileName;
            }
            else
            {
                return "";
            }
        }
        private Color GetColor(Color initColor)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = initColor;
                cd.AllowFullOpen = true;
                cd.AnyColor = true;
                cd.FullOpen = true;
                cd.ShowHelp = false;
                cd.SolidColorOnly = false;
                if (cd.ShowDialog() == DialogResult.OK)
                    return cd.Color;
                else
                    return initColor;
            }
        }
        private Font GetFont(Font initFont)
        {
            using (FontDialog fd = new FontDialog())
            {
                fd.Font = initFont;
                fd.AllowSimulations = true;
                fd.AllowVectorFonts = true;
                fd.AllowVerticalFonts = true;
                fd.FontMustExist = true;
                fd.ShowHelp = false;
                fd.ShowEffects = true;
                fd.ShowColor = false;
                fd.ShowApply = false;
                fd.FixedPitchOnly = false;

                if (fd.ShowDialog() == DialogResult.OK)
                    return fd.Font;
                else
                    return initFont;
            }
        }
        private string GetImagePath()
        {
            OpenFileDialog o = new OpenFileDialog();
            o.Multiselect = false;
            o.ShowReadOnly = false;
            o.RestoreDirectory = true;
            o.ReadOnlyChecked = false;
            o.Filter = "Images|*.png;*.bmp;*.jpg;*.jpeg;*.gif;*.tif;*.tiff,*.wmf;*.emf";
            if (o.ShowDialog(this) == DialogResult.OK)
            {
                return o.FileName;
            }
            else
            {
                return "";
            }
        }

        public void Find(string text)
        {
            this.TextEditor.FindAndSelect(text);
        }

        public void GotoRealLine(int realLineIndex)
        {
            this.TextEditor.GotoRealLine(realLineIndex);
        }

        //
        public void ProcessRichText()
        {
            ////⊳ ⋫ Ⓦ ▷  ✓ ✱ Ⓘ
            int index = 0;
            foreach (string l in Lines)
            {
                string line = l.Trim();
                if (line.Length > 0)
                {
                    char c = line[0];
                    switch (c)
                    {
                        case 'Ⓘ': SetLineColor(line, Color.Black, Color.FromArgb(238, 235, 250)); break;
                        case '✓': SetLineColor(line, Color.Black, Color.FromArgb(235, 250, 238)); break;
                        case '▶': SetLineColor(line, Color.Black, Color.FromArgb(255, 219, 219)); break;
                        case 'Ⓢ': SetLineColor(line, Color.Black, Color.FromArgb(255, 254, 219)); break;
                        case 'Ⓔ': SetLineColor(line, Color.Black, Color.FromArgb(219, 255, 254)); break;
                    }
                }
                index++;
            }

            TextEditor.Select(0, 0);
            TextEditor.SelectionBackColor = Color.White;
        }

        public void SetLineColor(string lineText, Color fgColor, Color bgColor)
        {
            try
            {
                int startIndex = TextEditor.Find(lineText, RichTextBoxFinds.MatchCase);
                TextEditor.Select(startIndex, lineText.Length);

                //Set the selected text fore and background color
                //TextEditor.SelectionColor = System.Drawing.Color.White;
                TextEditor.SelectionBackColor = bgColor;
            }
            catch { }
        }

        public void Clear()
        {
            _path = "";
            _Hash = null;
            this.TextEditor.Clear();

            //set indents to default positions
            this.TextEditor.Select(0, 0);
            this.Ruler.LeftIndent = 0;
            this.Ruler.LeftHangingIndent = 0;
            this.Ruler.RightIndent = 0;
            this.TextEditor.SelectionIndent = 0;
            this.TextEditor.SelectionRightIndent = 0;
            this.TextEditor.SelectionHangingIndent = 0;

            //clear tabs on the ruler
            this.Ruler.SetTabPositionsInPixels(null);
            this.TextEditor.SelectionTabs = null;

            ExtendedRichTextBox.ParaListStyle pls = new ExtendedRichTextBox.ParaListStyle();

            pls.Type = ExtendedRichTextBox.ParaListStyle.ListType.None;
            pls.Style = ExtendedRichTextBox.ParaListStyle.ListStyle.NumberAndParenthesis;

            this.TextEditor.SelectionListType = pls;
        }

        Dictionary<string, Tuple<int, int>> _DictPosition = new Dictionary<string, Tuple<int, int>>();
        public async Task<bool> OpenAsync(string file, DateTime date)
        {
            try
            {
                if (file != "" && file != _path && File.Exists(file))
                {
                    UpdatePostionDictionary();
                    if (await HasChanges())
                    {
                        //Save();
                        DialogResult result = MessageBox.Show("Do you want to save the changes?", "Changes found", MessageBoxButtons.YesNoCancel);
                        switch (result)
                        {
                            case DialogResult.Yes: Save(); break;
                            case DialogResult.Cancel: return false;
                        }
                    }

                    Clear();
                    var eventPtr = this.TextEditor.BeginUpdateAndSuspendEvents();
                    this.TextEditor.DocumentDate = date;
                    this.TextEditor.Rtf = System.IO.File.ReadAllText(file, System.Text.Encoding.Default);
                    this.TextEditor.EndUpdateAndResumeEvents(eventPtr);
                    _LastSavedLength = this.TextEditor.Text.Length;
                    _path = file;
                    OnOpen?.Invoke(this, new SaveArgs() { File = file });


                }
                file = null;

            }
            catch (System.Exception ex)
            {
                Clear();
                _path = string.Empty;
                MessageBox.Show(ex.Message);
                return false;
            }
            this.SetUpTextEditor();
            await ComputeHash();
            btnSave.Enabled = await HasChanges();

            return true;
        }

        private async Task<int> ComputeHash()
        {
            string rtf = this.TextEditor.Rtf;
            _Hash = await Task.Run(() => rtf.GetHashCode()).ConfigureAwait(false);
            return _Hash.Value;
        }

        int? _Hash = null;
        public async Task<bool> HasChanges()
        {
             return  _Hash != null && _Hash.Value != await ComputeHash().ConfigureAwait(false);
        }

        private void Open()
        {
            try
            {
                string file = GetFilePath();

                if (file != "")
                {
                    Clear();
                    try
                    {
                        this.TextEditor.Rtf = System.IO.File.ReadAllText(file, System.Text.Encoding.Default);
                        _LastSavedLength = this.TextEditor.Text.Length;
                        OnOpen?.Invoke(this, new SaveArgs() { File = file });
                    }
                    catch (Exception) //error occured, that means we loaded invalid RTF, so load as plain text
                    {
                        this.TextEditor.Text = System.IO.File.ReadAllText(file, System.Text.Encoding.Default);
                        _LastSavedLength = this.TextEditor.Text.Length;
                    }
                    _path = file;
                }
                file = null;
            }
            catch (Exception)
            {
                Clear();
            }
        }

        public bool CheckSave()
        {
            if (HasChanges().Result)
            {
                //Save();
                DialogResult result = MessageBox.Show("Do you want to save the changes?", "Changes found", MessageBoxButtons.YesNoCancel);
                switch (result)
                {
                    case DialogResult.Yes: Save(); break;
                    case DialogResult.Cancel: return false;
                }
            }
            return true;
        }

        public void Save()
        {
            Save(false);
        }

        private int _LastSavedLength = 0;
        private async void Save(bool SaveAs)
        {
            bool success = true;

            if (((this.TextEditor.Lines.Count() == 0 || 
                this.TextEditor.Text.Trim().Length == 0)
                && _LastSavedLength > 0
                && ((MessageBox.Show("This is an empty or truncated file, do you want to save?", 
                "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)))
                || this.TextEditor.Text.Trim().Length > 0)
            {
                try
                {

                    //TODO: First back up the file, so that it is not reduced to a zero byte by the TextEditor.SaveFile() method
                    string file = string.Empty;
                    if (SaveAs == true)
                    {
                        file = SetFilePath();

                        if (file != "")
                        {
                            this.TextEditor.SaveFile(file, RichTextBoxStreamType.RichText);
                            _LastSavedLength = TextEditor.Text.Length;
                            _path = file;
                            file = null;
                        }
                    }
                    else
                    {
                        if (_path == "")
                        {
                            file = string.Format("{0}.rtf", DateTime.Now.ToString("yyyy-MM-dd"));// SetFilePath();

                            if (File.Exists(file)) throw new InvalidOperationException(string.Format("File {0} already exists", file));

                            if (file != "")
                            {
                                this.TextEditor.SaveFile(file, RichTextBoxStreamType.RichText);
                                _LastSavedLength = TextEditor.Text.Length;
                                _path = file;
                                file = null;
                            }
                        }
                        else
                        {
                            this.TextEditor.SaveFile(_path, RichTextBoxStreamType.RichText);
                            _LastSavedLength = TextEditor.Text.Length;
                        }
                    }

                    OnSave?.Invoke(this, new SaveArgs() { File = _path });

                    UpdatePostionDictionary();
                    PositionCaret();
                    //TODO: First back up the file, so that it is not reduced to a zero byte by the TextEditor.SaveFile() method

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    success = false;
                }
                if (success)
                {
                    await ComputeHash();
                }
                btnSave.Enabled = await HasChanges();
            }
        }

        private void UpdatePostionDictionary()
        {
            int position = this.TextEditor.SelectionStart;
            //http://stackoverflow.com/a/18134915/4564049
            //get the first visible char index
            int topPosition = this.TextEditor.GetCharIndexFromPosition(new Point(0, 0));
            if (!_DictPosition.ContainsKey(_path))
            {
                _DictPosition.Add(_path, null);
            }
            _DictPosition[_path] = new Tuple<int, int>(topPosition, position);
        }

        private void PositionCaret()
        {
            Tuple<int, int> positionTuple = null;
            if (_DictPosition.TryGetValue(_path, out positionTuple))
            {
                this.TextEditor.Focus();
                PositionCaret(positionTuple.Item2, positionTuple.Item1);
            }
        }

        private void PositionCaret(int position, int topPosition)
        {
            this.TextEditor.SelectionLength = 0;
            this.TextEditor.SelectionStart = topPosition;
            this.TextEditor.ScrollToCaret();
            this.TextEditor.SelectionLength = 0;
            this.TextEditor.SelectionStart = position;
        }

        OutlookManager _OManager = new OutlookManager();
        protected bool IsDesginMode { get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; } }

        public AdvancedTextEditor()
        {
            InitializeComponent();

            if (!IsDesginMode)
            {
                LoadWords();

                //_OManager.WatchMailItemChanged();
                //var test = _OManager.GetToDoItems().ToList();

                listBox1.Items.AddRange(new object[] { "Aayush", "Isha", "Mohua", "Rio", "Gojubaba" });

                this.TextEditor.Protected += TextEditor_Protected;
                //TextEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
                TextEditor.KeyDown += TextEditor_KeyDown;
                //TextEditor.KeyPress += TextEditor_KeyPress;
                //listBox1.DoubleClick += listBox1_DoubleClick;

                TextEditor.QueryListItem += TextEditor_QueryListItem;
                TextEditor.Query += TextEditor_Query;
                TextEditor.LinkClickedEx += TextEditor_LinkClicked2;
                
                this.TextEditor.ShowLineNumbers = true;

                this.mnuRuler.Checked = true;
                this.mnuMainToolbar.Checked = true;
                this.mnuFormatting.Checked = true;

                System.Drawing.Text.InstalledFontCollection col = new System.Drawing.Text.InstalledFontCollection();

                this.cmbFontName.Items.Clear();

                int index = 0;
                int selection = 0;
                foreach (FontFamily ff in col.Families)
                {
                    if (ff.Name.StartsWith("Arial Unicode")) selection = index;
                    index++;
                    this.cmbFontName.Items.Add(ff.Name);
                }

                this.cmbFontName.SelectedIndex = selection;

                this.TextEditor.FontChanged += TextEditor_FontChanged;

                col.Dispose();
            }
        }

        private void TextEditor_LinkClicked2(object sender, LinkClickedEventArgs e)
        {
            LinkClicked?.Invoke(sender, e);
        }

        void TextEditor_FontChanged(object sender, EventArgs e)
        {
            
        }

        void TextEditor_Query(object sender, QueryArgs args)
        {
            Query?.Invoke(sender, args);
        }

        private void SetUpTextEditor()
        {
            this.Ruler.LeftIndent = 0;
            this.Ruler.LeftHangingIndent = 0;
            this.Ruler.RightIndent = 0;
            this.TextEditor.SelectionIndent = 0;
            this.TextEditor.SelectionRightIndent = 0;
            this.TextEditor.SelectionHangingIndent = 0;

            this.TextEditor.Font = new Font("Arial Unicode MS", 14);
            this.TextEditor.ForeColor = Color.DarkSlateGray;
            this.TextEditor.TextChanged += TextEditor_TextChanged;

            this.TextEditor.ScrollBars = RichTextBoxScrollBars.Both;
            Tuple<int, int> position = null;
            if (_DictPosition.TryGetValue(_path, out position))
            {
                this.TextEditor.Focus();
                PositionCaret(position.Item2, position.Item1);
            }
            else
            {
                this.TextEditor.Select(0, 0);
            }
        }

        List<string> _WordList = new List<string>();
        private void LoadWords()
        {
            if (File.Exists(@"Data\UKACD17.TXT"))
            {
            using (StreamReader sr = new StreamReader(@"Data\UKACD17.TXT"))
            {
                while (sr.Peek() >= 0)
                {
                    string line = sr.ReadLine().Trim();
                    _WordList.Add(line.ToLower());
                    //foreach (string word in words)
                    //{
                    //    foreach (Char c in word)
                    //    {
                    //        // ...
                    //    }
                    //}
                }
            }
            }
        }


        void TextEditor_QueryListItem(object sender, RichTextBoxLinks.QueryListItemsArgs args)
        {
            //string fragment = args.Fragment.ToLower();
           List<string> list = new List<string>();
            //if (args.Fragment.Length > 0)
            {
                if (QueryListItem != null)
                {
                    QueryListItem(this, args);
                    if (args.List != null)
                    {
                        list.AddRange(args.List);
                    }
                }
                if (args.List.Count() < 7 && args.Fragment != null && args.Fragment.Length > 0)
                {
                    list.AddRange(_WordList
                        //.Where(s => s.StartsWith(args.Fragment.ToLower()))
                        .Where(s => s.IndexOf(args.Fragment.ToLower()) == 0)
                        .Where(s => s.Split().Length == 1)
                        .OrderBy(s => s)
                        .Take(7 - args.List.Count())
                        .ToList());
                }
                args.List = list.Distinct();
            }
            //else
            //{
            //    args.List = new List<string>();
            //}
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void mnuNew_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void btnCut_Click(object sender, EventArgs e)
        {
            this.TextEditor.Cut();
        }

        private void mnuCut_Click(object sender, EventArgs e)
        {
            this.TextEditor.Cut();
        }

        private void mnuCopy_Click(object sender, EventArgs e)
        {
            this.TextEditor.Copy();
        }

        private void mnuPaste_Click(object sender, EventArgs e)
        {
            this.TextEditor.Paste();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            this.TextEditor.Copy();
        }

        private void btnPaste_Click(object sender, EventArgs e)
        {
            this.TextEditor.Paste();
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            this.TextEditor.Undo();
        }

        private void btnRedo_Click(object sender, EventArgs e)
        {
            this.TextEditor.Redo();
        }

        private void mnuUndo_Click(object sender, EventArgs e)
        {
            this.TextEditor.Undo();
        }

        private void mnuRedo_Click(object sender, EventArgs e)
        {
            this.TextEditor.Redo();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void mnuOpen_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void TextEditor_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                #region Alignment
                if (TextEditor.SelectionAlignment == ExtendedRichTextBox.RichTextAlign.Left)
                {
                    this.btnAlignLeft.Checked = true;
                    this.btnAlignCenter.Checked = false;
                    this.btnAlignRight.Checked = false;
                    this.btnJustify.Checked = false;
                }
                else if (TextEditor.SelectionAlignment == ExtendedRichTextBox.RichTextAlign.Center)
                {
                    this.btnAlignLeft.Checked = false;
                    this.btnAlignCenter.Checked = true;
                    this.btnAlignRight.Checked = false;
                    this.btnJustify.Checked = false;
                }
                else if (TextEditor.SelectionAlignment == ExtendedRichTextBox.RichTextAlign.Right)
                {
                    this.btnAlignLeft.Checked = false;
                    this.btnAlignCenter.Checked = false;
                    this.btnAlignRight.Checked = true;
                    this.btnJustify.Checked = false;
                }
                else if (TextEditor.SelectionAlignment == ExtendedRichTextBox.RichTextAlign.Justify)
                {
                    this.btnAlignLeft.Checked = false;
                    this.btnAlignRight.Checked = false;
                    this.btnAlignCenter.Checked = false;
                    this.btnJustify.Checked = true;
                }
                else
                {
                    this.btnAlignLeft.Checked = true;
                    this.btnAlignCenter.Checked = false;
                    this.btnAlignRight.Checked = false;
                }

                #endregion

                #region Tab positions
                this.Ruler.SetTabPositionsInPixels(this.TextEditor.SelectionTabs);
                #endregion

                #region Font
                try
                {
                    this.cmbFontSize.Text = Convert.ToInt32(this.TextEditor.SelectionFont2.Size).ToString();
                }
                catch
                {
                    this.cmbFontSize.Text = "";
                }

                try
                {
                    this.cmbFontName.Text = this.TextEditor.SelectionFont2.Name;
                }
                catch
                {
                    this.cmbFontName.Text = "";
                }

                if (this.cmbFontName.Text != "")
                {
                    FontFamily ff = new FontFamily(this.cmbFontName.Text);
                    if (ff.IsStyleAvailable(FontStyle.Bold) == true)
                    {
                        this.btnBold.Enabled = true;
                        this.btnBold.Checked = this.TextEditor.SelectionCharStyle.Bold;
                    }
                    else
                    {
                        this.btnBold.Enabled = false;
                        this.btnBold.Checked = false;
                    }

                    if (ff.IsStyleAvailable(FontStyle.Italic) == true)
                    {
                        this.btnItalic.Enabled = true;
                        this.btnItalic.Checked = this.TextEditor.SelectionCharStyle.Italic;
                    }
                    else
                    {
                        this.btnItalic.Enabled = false;
                        this.btnItalic.Checked = false;
                    }

                    if (ff.IsStyleAvailable(FontStyle.Underline) == true)
                    {
                        this.btnUnderline.Enabled = true;
                        this.btnUnderline.Checked = this.TextEditor.SelectionCharStyle.Underline;
                    }
                    else
                    {
                        this.btnUnderline.Enabled = false;
                        this.btnUnderline.Checked = false;
                    }

                    if (ff.IsStyleAvailable(FontStyle.Strikeout) == true)
                    {
                        this.btnStrikeThrough.Enabled = true;
                        this.btnStrikeThrough.Checked = this.TextEditor.SelectionCharStyle.Strikeout;
                    }
                    else
                    {
                        this.btnStrikeThrough.Enabled = false;
                        this.btnStrikeThrough.Checked = false;
                    }

                    ff.Dispose();
                }
                else
                {
                    this.btnBold.Checked = false;
                    this.btnItalic.Checked = false;
                    this.btnUnderline.Checked = false;
                    this.btnStrikeThrough.Checked = false;
                }
                #endregion

                if (this.TextEditor.SelectionLength < this.TextEditor.TextLength - 1)
                {
                    this.Ruler.LeftIndent = (int)(this.TextEditor.SelectionIndent / this.Ruler.DotsPerMillimeter); //convert pixels to millimeter

                    this.Ruler.LeftHangingIndent = (int)((float)this.TextEditor.SelectionHangingIndent / this.Ruler.DotsPerMillimeter) + this.Ruler.LeftIndent; //convert pixels to millimeters

                    this.Ruler.RightIndent = (int)(this.TextEditor.SelectionRightIndent / this.Ruler.DotsPerMillimeter); //convert pixels to millimeters                
                }

                switch (this.TextEditor.SelectionListType.Type)
                {
                    case ExtendedRichTextBox.ParaListStyle.ListType.None:
                        this.btnNumberedList.Checked = false;
                        this.btnBulletedList.Checked = false;
                        break;
                    case ExtendedRichTextBox.ParaListStyle.ListType.SmallLetters:
                        this.btnNumberedList.Checked = false;
                        this.btnBulletedList.Checked = false;
                        break;
                    case ExtendedRichTextBox.ParaListStyle.ListType.CapitalLetters:
                        this.btnNumberedList.Checked = false;
                        this.btnBulletedList.Checked = false;
                        break;
                    case ExtendedRichTextBox.ParaListStyle.ListType.SmallRoman:
                        this.btnNumberedList.Checked = false;
                        this.btnBulletedList.Checked = false;
                        break;
                    case ExtendedRichTextBox.ParaListStyle.ListType.CapitalRoman:
                        this.btnNumberedList.Checked = false;
                        this.btnBulletedList.Checked = false;
                        break;
                    case ExtendedRichTextBox.ParaListStyle.ListType.Bullet:
                        this.btnNumberedList.Checked = false;
                        this.btnBulletedList.Checked = true;
                        break;
                    case ExtendedRichTextBox.ParaListStyle.ListType.Numbers:
                        this.btnNumberedList.Checked = true;
                        this.btnBulletedList.Checked = false;
                        break;
                    case ExtendedRichTextBox.ParaListStyle.ListType.CharBullet:
                        this.btnNumberedList.Checked = true;
                        this.btnBulletedList.Checked = false;
                        break;
                    default:
                        break;
                }

                this.TextEditor.UpdateObjects();                
            }
            catch (Exception)
            {
            }
        }

        private void AdvancedTextEditor_Load(object sender, EventArgs e)
        {
            //code below will cause refreshing formatting by adding and removing (changing) text
            this.TextEditor.Select(0, 0);
            this.TextEditor.AppendText("some text");
            this.TextEditor.Select(0, 0);
            this.TextEditor.Clear();
            this.TextEditor.SetLayoutType(ExtendedRichTextBox.LayoutModes.WYSIWYG);
        }

        private void cmbFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!this.cmbFontSize.Focused) return;
                this.TextEditor.SelectionFont2 = new Font(this.cmbFontName.Text, Convert.ToInt32(this.cmbFontSize.Text), this.TextEditor.SelectionFont.Style);
            }
            catch (Exception)
            {
                
            }
        }

        private void cmbFontSize_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    this.TextEditor.SelectionFont2 = new Font(this.cmbFontName.Text, Convert.ToSingle(this.cmbFontSize.Text));
                    this.TextEditor.Focus();
                }
                catch (Exception)
                {
                }
            }
        }

        #region Old style formatting

        private FontStyle SwitchBold()
        {
            FontStyle fs = new FontStyle();

            fs = FontStyle.Regular;

            if (this.TextEditor.SelectionFont.Italic == true)
            {
                fs = FontStyle.Italic;
            }

            if (this.TextEditor.SelectionFont.Underline == true)
            {
                fs = fs | FontStyle.Underline;
            }

            if (this.TextEditor.SelectionFont.Strikeout == true)
            {
                fs = fs | FontStyle.Strikeout;
            }

            if (this.TextEditor.SelectionFont.Bold == false)
            {
                fs = fs | FontStyle.Bold;
            }

            return fs;
        }
        private FontStyle SwitchItalic()
        {
            FontStyle fs = new FontStyle();

            fs = FontStyle.Regular;

            if (this.TextEditor.SelectionFont.Underline == true)
            {
                fs = fs | FontStyle.Underline;
            }

            if (this.TextEditor.SelectionFont.Strikeout == true)
            {
                fs = fs | FontStyle.Strikeout;
            }

            if (this.TextEditor.SelectionFont.Bold == true)
            {
                fs = fs | FontStyle.Bold;
            }

            if (this.TextEditor.SelectionFont.Italic == false)
            {
                fs = fs | FontStyle.Italic;
            }

            return fs;
        }
        private FontStyle SwitchUnderline()
        {
            FontStyle fs = new FontStyle();

            fs = FontStyle.Regular;

            if (this.TextEditor.SelectionFont.Strikeout == true)
            {
                fs = fs | FontStyle.Strikeout;
            }

            if (this.TextEditor.SelectionFont.Bold == true)
            {
                fs = fs | FontStyle.Bold;
            }

            if (this.TextEditor.SelectionFont.Italic == true)
            {
                fs = fs | FontStyle.Italic;
            }

            if (this.TextEditor.SelectionFont.Underline == false)
            {
                fs = fs | FontStyle.Underline;
            }

            return fs;
        }
        private FontStyle SwitchStrikeout()
        {
            FontStyle fs = new FontStyle();

            fs = FontStyle.Regular;

            if (this.TextEditor.SelectionFont.Bold == true)
            {
                fs = fs | FontStyle.Bold;
            }

            if (this.TextEditor.SelectionFont.Italic == true)
            {
                fs = fs | FontStyle.Italic;
            }

            if (this.TextEditor.SelectionFont.Underline == true)
            {
                fs = fs | FontStyle.Underline;
            }

            if (this.TextEditor.SelectionFont.Strikeout == false)
            {
                fs = fs | FontStyle.Strikeout;
            }

            return fs;
        }

        #endregion

        private void btnBold_Click(object sender, EventArgs e)
        {
            if (this.TextEditor.SelectionCharStyle.Bold == true)
            {
                this.btnBold.Checked = false;
                ExtendedRichTextBox.CharStyle cs = this.TextEditor.SelectionCharStyle;
                cs.Bold = false;
                this.TextEditor.SelectionCharStyle = cs;
                cs = null;
            }
            else
            {
                this.btnBold.Checked = true;
                ExtendedRichTextBox.CharStyle cs = this.TextEditor.SelectionCharStyle;
                cs.Bold = true;
                this.TextEditor.SelectionCharStyle = cs;
                cs = null;
            }
        }

        private void btnAlignLeft_Click(object sender, EventArgs e)
        {
            this.TextEditor.SelectionAlignment = ExtendedRichTextBox.RichTextAlign.Left;
            this.btnAlignLeft.Checked = true;
            this.btnAlignRight.Checked = false;
            this.btnAlignCenter.Checked = false;
            this.btnJustify.Checked = false;
        }

        private void btnAlignCenter_Click(object sender, EventArgs e)
        {
            this.TextEditor.SelectionAlignment = ExtendedRichTextBox.RichTextAlign.Center;
            this.btnAlignLeft.Checked = false;
            this.btnAlignRight.Checked = false;
            this.btnAlignCenter.Checked = true;
            this.btnJustify.Checked = false;
        }

        private void btnAlignRight_Click(object sender, EventArgs e)
        {
            this.TextEditor.SelectionAlignment = ExtendedRichTextBox.RichTextAlign.Right;
            this.btnAlignLeft.Checked = false;
            this.btnAlignRight.Checked = true;
            this.btnAlignCenter.Checked = false;
            this.btnJustify.Checked = false;
        }

        private void Ruler_LeftIndentChanging(int NewValue)
        {
            try
            {
                this.TextEditor.SelectionIndent = (int)(this.Ruler.LeftIndent * this.Ruler.DotsPerMillimeter);
                this.TextEditor.SelectionHangingIndent = (int)(this.Ruler.LeftHangingIndent * this.Ruler.DotsPerMillimeter) - (int)(this.Ruler.LeftIndent * this.Ruler.DotsPerMillimeter);                
            }
            catch (Exception)
            {
            }
        }

        private void Ruler_LeftHangingIndentChanging(int NewValue)
        {
            try
            {                
                this.TextEditor.SelectionHangingIndent = (int)(this.Ruler.LeftHangingIndent * this.Ruler.DotsPerMillimeter) - (int)(this.Ruler.LeftIndent * this.Ruler.DotsPerMillimeter);
            }
            catch (Exception)
            {
            }
        }

        private void Ruler_RightIndentChanging(int NewValue)
        {
            try
            {
                this.TextEditor.SelectionRightIndent = (int)(this.Ruler.RightIndent * this.Ruler.DotsPerMillimeter);
            }
            catch (Exception)
            {
            }
        }

        private void cmbFontName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!this.cmbFontName.Focused) return;
                this.TextEditor.SelectionFont2 = new Font(this.cmbFontName.Text, Convert.ToInt32(this.cmbFontSize.Text));
            }
            catch (Exception)
            {                
            }
        }

        private void cmbFontName_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    this.TextEditor.SelectionFont2 = new Font(this.cmbFontName.Text, Convert.ToInt32(this.cmbFontSize.Text));
                    this.TextEditor.Focus();
                }
            }
            catch (Exception)
            {                
            }
        }

        private void mnuRuler_Click(object sender, EventArgs e)
        {
            if (this.Ruler.Visible == true)
            {
                this.Ruler.Visible = false;
                this.mnuRuler.Checked = false;
            }
            else
            {
                this.Ruler.Visible = true;
                this.mnuRuler.Checked = true;
            }
        }

        private void mnuMainToolbar_Click(object sender, EventArgs e)
        {
            if (this.Toolbox_Main.Visible == true)
            {
                this.Toolbox_Main.Visible = false;
                this.mnuMainToolbar.Checked = false;
            }
            else
            {
                this.Toolbox_Main.Visible = true;
                this.mnuMainToolbar.Checked = true;
            }
        }

        private void mnuFormatting_Click(object sender, EventArgs e)
        {
            if (this.Toolbox_Formatting.Visible == true)
            {
                this.Toolbox_Formatting.Visible = false;
                this.mnuFormatting.Checked = false;
            }
            else
            {
                this.Toolbox_Formatting.Visible = true;
                this.mnuFormatting.Checked = true;
            }
        }

        private void mnuFont_Click(object sender, EventArgs e)
        {
            try
            {
                this.TextEditor.SelectionFont2 = GetFont(this.TextEditor.SelectionFont);
            }
            catch (Exception)
            {
            }
        }

        private void mnuTextColor_Click(object sender, EventArgs e)
        {
            try
            {
                this.TextEditor.SelectionColor2 = GetColor(this.TextEditor.SelectionColor);
            }
            catch (Exception)
            {
            }
        }

        private void mnuHighlightColor_Click(object sender, EventArgs e)
        {
            try
            {
                this.TextEditor.SelectionBackColor2 = GetColor(this.TextEditor.SelectionBackColor);
            }
            catch (Exception)
            {
            }
        }

        private void Ruler_TabAdded(TextRuler.TextRulerControl.TextRuler.TabEventArgs args)
        {
            try
            {
                this.TextEditor.SelectionTabs = this.Ruler.TabPositionsInPixels.ToArray();
            }
            catch (Exception)
            {
            }
        }

        private void Ruler_TabChanged(TextRuler.TextRulerControl.TextRuler.TabEventArgs args)
        {
            try
            {
                this.TextEditor.SelectionTabs = this.Ruler.TabPositionsInPixels.ToArray();
            }
            catch (Exception)
            {
            }
        }

        private void Ruler_TabRemoved(TextRuler.TextRulerControl.TextRuler.TabEventArgs args)
        {
            try
            {
                this.TextEditor.SelectionTabs = this.Ruler.TabPositionsInPixels.ToArray();
            }
            catch (Exception)
            {
            }
        }

        private void cmbFontSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.D1 || e.KeyCode == Keys.D2 ||
                e.KeyCode == Keys.D3 || e.KeyCode == Keys.D4 || e.KeyCode == Keys.D5 ||
                e.KeyCode == Keys.D6 || e.KeyCode == Keys.D7 || e.KeyCode == Keys.D8 ||
                e.KeyCode == Keys.D9 || e.KeyCode == Keys.NumPad0 || e.KeyCode == Keys.NumPad1 ||
                e.KeyCode == Keys.NumPad2 || e.KeyCode == Keys.NumPad3 || e.KeyCode == Keys.NumPad4 ||
                e.KeyCode == Keys.NumPad5 || e.KeyCode == Keys.NumPad6 || e.KeyCode == Keys.NumPad7 ||
                e.KeyCode == Keys.NumPad8 || e.KeyCode == Keys.NumPad9 || e.KeyCode == Keys.Back ||
                e.KeyCode == Keys.Enter || e.KeyCode == Keys.Delete)
            {
                //allow key
            }
            else
            {
                e.SuppressKeyPress = true;
            }
        }

        private void mnuInsertPicture_Click(object sender, EventArgs e)
        {
            string _imgPath = GetImagePath();
            if (_imgPath == "")
                return;            
            this.TextEditor.InsertImage(_imgPath);
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("This control was created by Krassovskikh Aleksei. You can freely use it in your application, but if it possible, mention about creator of that control (this is not required but desired :)  )");
        }

        private void prtDoc_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            checkPrint = this.TextEditor.Print(checkPrint, this.TextEditor.TextLength, e);

            if (checkPrint < this.TextEditor.TextLength)
                e.HasMorePages = true;
            else
                e.HasMorePages = false;
        }

        private void prtDoc_BeginPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            checkPrint = 0;
        }

        private void mnuPageSettings_Click(object sender, EventArgs e)
        {
            this.PageSettings.ShowDialog(this);
        }

        private void mnuPrintPreview_Click(object sender, EventArgs e)
        {
            this.DocPreview.ShowDialog(this);
        }

        private void btnPrintPreview_Click(object sender, EventArgs e)
        {
            this.DocPreview.ShowDialog(this);
        }

        delegate void printDialogHelperDelegate(); // Helper delegate for PrintDialog bug

        /// <summary>
        /// Helper thread which sole purpose is to invoke PrintDialogHelper function
        /// to circumvent the PrintDialog focus problem reported on
        /// https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=234179
        /// </summary>
        private void PrintHelpThread()
        {
            if (InvokeRequired)
            {
                printDialogHelperDelegate d = new printDialogHelperDelegate(PrintHelpThread);
                Invoke(d);
            }
            else
            {
                PrintDialogHelper();
            }
        }

        /// <summary>
        /// Shows the print dialog (invoked from a different thread to get the focus to the dialog)
        /// </summary>
        private void PrintDialogHelper()
        {
            if (PrintWnd.ShowDialog(this) == DialogResult.OK)
            {
                this.prtDoc.Print();
            }
        }
        
        private void btnPrint_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(PrintHelpThread);            
            t.Start();
        }

        private void mnuPrint_Click(object sender, EventArgs e)
        {
            this.PrintWnd.ShowDialog(this);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Save(false);
        }

        private void mnuSave_Click(object sender, EventArgs e)
        {
            Save(false);
        }

        private void mnuSaveAs_Click(object sender, EventArgs e)
        {
            Save(true);
        }

        private void mnuInsertDateTime_DropDownOpening(object sender, EventArgs e)
        {
            this.cmbDateTimeFormats.Items.Clear();

            this.cmbDateTimeFormats.Items.Add("Select date/time format");
            this.cmbDateTimeFormats.Items.Add(DateTime.Now.ToString("D"));
            this.cmbDateTimeFormats.Items.Add(DateTime.Now.ToString("f"));
            this.cmbDateTimeFormats.Items.Add(DateTime.Now.ToString("F"));
            this.cmbDateTimeFormats.Items.Add(DateTime.Now.ToString("g"));
            this.cmbDateTimeFormats.Items.Add(DateTime.Now.ToString("G"));
            this.cmbDateTimeFormats.Items.Add(DateTime.Now.ToString("m"));
            this.cmbDateTimeFormats.Items.Add(DateTime.Now.ToString("t"));
            this.cmbDateTimeFormats.Items.Add(DateTime.Now.ToString("T"));

            this.cmbDateTimeFormats.SelectedIndex = 0;
        }

        private void cmbDateTimeFormats_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cmbDateTimeFormats.SelectedIndex == 0)
                return;

            this.TextEditor.AppendText(Environment.NewLine + this.cmbDateTimeFormats.SelectedItem.ToString());
            this.mnuInsert.DropDown.Close();
        }

        private void txtCustom_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.txtCustom.Text == "")
                {
                    return;
                }

                try
                {
                    this.TextEditor.AppendText(Environment.NewLine + DateTime.Now.ToString(txtCustom.Text));
                }
                catch (Exception)
                {                    
                }
                txtCustom.Text = "specify custom date/time format";
                this.mnuInsert.DropDown.Close();
            }
        }

        private void txtCustom_Leave(object sender, EventArgs e)
        {
            txtCustom.Text = "specify custom date/time format";
            this.mnuInsert.DropDown.Close();
        }

        private void txtCustom_MouseDown(object sender, MouseEventArgs e)
        {
            txtCustom.Text = "";
        }

        private void txtCustom_Enter(object sender, EventArgs e)
        {
            txtCustom.Text = "";
        }

        private void btnItalic_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.TextEditor.SelectionCharStyle.Italic == true)
                {
                    this.btnItalic.Checked = false;
                    ExtendedRichTextBox.CharStyle cs = this.TextEditor.SelectionCharStyle;
                    cs.Italic = false;
                    this.TextEditor.SelectionCharStyle = cs;
                    cs = null;
                }
                else
                {
                    this.btnItalic.Checked = true;
                    ExtendedRichTextBox.CharStyle cs = this.TextEditor.SelectionCharStyle;
                    cs.Italic = true;
                    this.TextEditor.SelectionCharStyle = cs;
                    cs = null;
                }
            }
            catch (Exception)
            {
            }            
        }

        private void btnUnderline_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.TextEditor.SelectionCharStyle.Underline == true)
                {
                    this.btnUnderline.Checked = false;
                    ExtendedRichTextBox.CharStyle cs = this.TextEditor.SelectionCharStyle;
                    cs.Underline = false;
                    this.TextEditor.SelectionCharStyle = cs;
                    cs = null;
                }
                else
                {
                    this.btnUnderline.Checked = true;
                    ExtendedRichTextBox.CharStyle cs = this.TextEditor.SelectionCharStyle;
                    cs.Underline = true;
                    this.TextEditor.SelectionCharStyle = cs;
                    cs = null;
                }
            }
            catch (Exception)
            {
            }
        }

        private void btnStrikeThrough_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.TextEditor.SelectionCharStyle.Strikeout == true)
                {
                    this.btnStrikeThrough.Checked = false;
                    ExtendedRichTextBox.CharStyle cs = this.TextEditor.SelectionCharStyle;
                    cs.Strikeout = false;
                    this.TextEditor.SelectionCharStyle = cs;
                    cs = null;
                }
                else
                {
                    this.btnStrikeThrough.Checked = true;
                    ExtendedRichTextBox.CharStyle cs = this.TextEditor.SelectionCharStyle;
                    cs.Strikeout = true;
                    this.TextEditor.SelectionCharStyle = cs;
                    cs = null;
                }
            }
            catch (Exception)
            {
            }
        }

        private void mnuFind_Click(object sender, EventArgs e)
        {
            Dialogs.dlgFind find = new TextRuler.Dialogs.dlgFind();
            find.txtFindThis.Text = this.TextEditor.SelectedText;
            find.Caller = this;
            find.Show(this);
        }

        private void TextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            //HandleAutoComplete(sender, e);

            //if (e.KeyCode == Keys.B && e.Control == true)
            //{
            //    this.btnBold.PerformClick();
            //}

            //if (e.Control == true && e.KeyCode == Keys.I)
            //{
            //    this.btnItalic.PerformClick();
            //    e.SuppressKeyPress = true;
            //}

            //if (e.Control == true && e.KeyCode == Keys.U)
            //{
            //    this.btnUnderline.PerformClick();
            //}
        }

        int _AutoCompleteCount = 0;
        bool _AutoCompleteListShow = false;
        string _AutoCompleteKeyword = string.Empty;

        void TextEditor_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
        }

        void TextEditor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (_AutoCompleteListShow == true) /*Section 1*/
            {
                _AutoCompleteKeyword += e.KeyChar;
                _AutoCompleteCount++;
                Point point = this.TextEditor.GetPositionFromCharIndex(TextEditor.SelectionStart);
                point.Y += (int)Math.Ceiling(this.TextEditor.Font.GetHeight()) + 13; //13 is the .y postion of the richtectbox
                point.X += TextEditor.Location.X; //105 is the .x postion of the richtectbox
                listBox1.Location = point;
                listBox1.Show();
                listBox1.SelectedIndex = 0;
                listBox1.SelectedIndex = listBox1.FindString(_AutoCompleteKeyword);
                TextEditor.Focus();
            }
        }


        void listBox1_DoubleClick(object sender, EventArgs e)
        {
            string autoText = listBox1.SelectedItem.ToString();
            int beginPlace = TextEditor.SelectionStart - _AutoCompleteCount;
            TextEditor.Select(beginPlace, _AutoCompleteCount);
            TextEditor.SelectedText = "";
            TextEditor.Text += autoText;
            TextEditor.Focus();
            _AutoCompleteListShow = false;
            listBox1.Hide();
            int endPlace = autoText.Length + beginPlace;
            TextEditor.SelectionStart = endPlace;
            _AutoCompleteCount = 0;
        }

        private void HandleAutoComplete(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Space)
            {
                _AutoCompleteListShow = true;
                Point point = this.TextEditor.GetPositionFromCharIndex(TextEditor.SelectionStart);
                point.Y += (int)Math.Ceiling(this.TextEditor.Font.GetHeight()) + 13; //13 is the .y postion of the richtectbox
                point.X += TextEditor.Location.X; //105 is the .x postion of the richtectbox
                listBox1.Location = point;
                _AutoCompleteCount++;
                listBox1.Show();
                listBox1.BringToFront();
                listBox1.SelectedIndex = 0;
                listBox1.SelectedIndex = listBox1.FindString(_AutoCompleteKeyword);
                TextEditor.Focus();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter) /*Section 1*/
            {
                _AutoCompleteCount = 0;
                _AutoCompleteKeyword = "<";
                _AutoCompleteListShow = false;
                listBox1.Hide();

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
                if (e.KeyCode == Keys.Up)
                {
                    listBox1.Focus();
                    if (listBox1.SelectedIndex != 0)
                    {
                        listBox1.SelectedIndex -= 1;
                    }
                    else
                    {
                        listBox1.SelectedIndex = 0;
                    }
                    TextEditor.Focus();

                }
                else if (e.KeyCode == Keys.Down)
                {
                    listBox1.Focus();
                    try
                    {
                        listBox1.SelectedIndex += 1;
                    }
                    catch
                    {
                    }
                    TextEditor.Focus();
                }

                if (e.KeyCode == Keys.Tab) /*Section 3*/
                {

                    string autoText = listBox1.SelectedItem.ToString();

                    int beginPlace = TextEditor.SelectionStart - _AutoCompleteCount;
                    TextEditor.Select(beginPlace, _AutoCompleteCount);
                    TextEditor.SelectedText = "";
                    TextEditor.Text += autoText;
                    TextEditor.Focus();
                    _AutoCompleteListShow = false;
                    listBox1.Hide();
                    int endPlace = autoText.Length + beginPlace;
                    TextEditor.SelectionStart = endPlace;
                    _AutoCompleteCount = 0;

                }
            }
        }

        private void btnJustify_Click(object sender, EventArgs e)
        {
            this.TextEditor.SelectionAlignment = ExtendedRichTextBox.RichTextAlign.Justify;
            this.btnAlignLeft.Checked = false;
            this.btnAlignRight.Checked = false;
            this.btnAlignCenter.Checked = false;
            this.btnJustify.Checked = true;
        }

        private void Ruler_BothLeftIndentsChanged(int LeftIndent, int HangIndent)
        {
            this.TextEditor.SelectionIndent = (int)(this.Ruler.LeftIndent * this.Ruler.DotsPerMillimeter);
            this.TextEditor.SelectionHangingIndent = (int)(this.Ruler.LeftHangingIndent * this.Ruler.DotsPerMillimeter) - (int)(this.Ruler.LeftIndent * this.Ruler.DotsPerMillimeter);            
        }

        private void TextEditor_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                Process.Start(e.LinkText);
            }
            catch (Exception)
            {
            }
        }

        private void btnNumberedList_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.btnNumberedList.Checked)
                {
                    this.btnBulletedList.Checked = false;
                    this.btnNumberedList.Checked = false;
                    ExtendedRichTextBox.ParaListStyle pls = new ExtendedRichTextBox.ParaListStyle();

                    pls.Type = ExtendedRichTextBox.ParaListStyle.ListType.None;
                    pls.Style = ExtendedRichTextBox.ParaListStyle.ListStyle.NumberAndParenthesis;

                    this.TextEditor.SelectionListType = pls;
                }
                else
                {
                    this.btnBulletedList.Checked = false;
                    this.btnNumberedList.Checked = true;
                    ExtendedRichTextBox.ParaListStyle pls = new ExtendedRichTextBox.ParaListStyle();

                    pls.Type = ExtendedRichTextBox.ParaListStyle.ListType.Numbers;
                    pls.Style = ExtendedRichTextBox.ParaListStyle.ListStyle.NumberInPar;

                    this.TextEditor.SelectionListType = pls;
                }
            }
            catch (Exception)
            {
            }
        }

        private void btnBulletedList_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.btnBulletedList.Checked)
                {
                    this.btnBulletedList.Checked = false;
                    this.btnNumberedList.Checked = false;
                    ExtendedRichTextBox.ParaListStyle pls = new ExtendedRichTextBox.ParaListStyle();

                    pls.Type = ExtendedRichTextBox.ParaListStyle.ListType.None;
                    pls.Style = ExtendedRichTextBox.ParaListStyle.ListStyle.NumberAndParenthesis;

                    this.TextEditor.SelectionListType = pls;
                }
                else
                {
                    this.btnBulletedList.Checked = true;
                    this.btnNumberedList.Checked = false;
                    ExtendedRichTextBox.ParaListStyle pls = new ExtendedRichTextBox.ParaListStyle();

                    pls.Type = ExtendedRichTextBox.ParaListStyle.ListType.Bullet;
                    pls.Style = ExtendedRichTextBox.ParaListStyle.ListStyle.NumberAndParenthesis;

                    this.TextEditor.SelectionListType = pls;
                }
            }
            catch (Exception)
            {
            }
        }

        Point _MouseRightPos;
        private void TextEditor_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _MouseRightPos = e.Location;
                if (this.TextEditor.SelectionType == RichTextBoxSelectionTypes.Object ||
                    this.TextEditor.SelectionType == RichTextBoxSelectionTypes.MultiObject)
                {
                    MessageBox.Show(Convert.ToString(this.TextEditor.SelectedObject().sizel.Width));
                }
            }
        }

        private void TextEditor_MouseMove(object sender, MouseEventArgs e)
        {
            
        }

        private void mnuULWave_Click(object sender, EventArgs e)
        {
            this.TextEditor.SelectionUnderlineStyle = ExtendedRichTextBox.UnderlineStyle.Wave;
        }

        private void mnuULineSolid_Click(object sender, EventArgs e)
        {
            this.TextEditor.SelectionUnderlineStyle = ExtendedRichTextBox.UnderlineStyle.DashDotDot;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            int charIndex = this.TextEditor.GetFirstCharIndexOfCurrentLine();
            this.TextEditor.Select(charIndex, 0);
            DateTime date = ExtractDiaryDate(_path);
            this.TextEditor.SelectedText = 'Ⓢ' + date.ToString() + " ";
            //TextEditor.SelectionBackColor = Color.FromArgb(255, 254, 219);
        }

        private DateTime ExtractDiaryDate(string _path)
        {
            try
            {
                if (_path != null && _path.Length > 0)
                {
                    string name = Path.GetFileNameWithoutExtension(_path);
                    Match match = Regex.Match(name, @"\d{4}-\d{2}-\d{2}");
                    if (match != null)
                    {
                        return DateTime.Parse(match.Value).Add(DateTime.Now.Subtract(DateTime.UtcNow.Date));
                    }
                }
                return DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            int charIndex = this.TextEditor.GetFirstCharIndexOfCurrentLine();
            this.TextEditor.Select(charIndex, 0);
            this.TextEditor.SelectedText = "▶ ";//⊳ ⋫ Ⓦ ▷  ✓ ✱ Ⓘ
            //TextEditor.SelectionBackColor = Color.FromArgb(255, 219, 219);

        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            int charIndex = this.TextEditor.GetFirstCharIndexOfCurrentLine();
            this.TextEditor.Select(charIndex, 0);
            DateTime date = ExtractDiaryDate(_path);
            this.TextEditor.SelectedText = 'Ⓔ' + date.ToString() + " ";
            //TextEditor.SelectionBackColor = Color.FromArgb(219, 255, 254);

        }

        private void AdvancedTextEditor_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                Save();
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            //this.TextEditor.WordWrap = false;
            int charIndex = this.TextEditor.GetFirstCharIndexOfCurrentLine();
            //int lineIndex = this.TextEditor.GetLineFromCharIndex(charIndex);
            int index = this.TextEditor.SelectionStart;
            //int line = this.TextEditor.GetLineFromCharIndex(index);
            this.TextEditor.Select(charIndex, index);
            string text = this.TextEditor.SelectedText;
            if (text.StartsWith("▶"))
            {
                this.TextEditor.Select(charIndex, 1);
                this.TextEditor.SelectedText = string.Format("✓ {0} ", DateTime.Now.ToString());//⊳ ⋫ Ⓦ ▷  ✓ ✱ Ⓘ
                //TextEditor.SelectionBackColor = Color.FromArgb(235, 250, 238);
            }
            //this.TextEditor.WordWrap = true;
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            int charIndex = this.TextEditor.GetFirstCharIndexOfCurrentLine();
            this.TextEditor.Select(charIndex, 0);
            this.TextEditor.SelectedText = "Ⓘ"+ DateTime.Now.ToString()+" ";//⊳ ⋫ Ⓦ ▷  ✓ ✱ Ⓘ
            //TextEditor.SelectionBackColor = Color.FromArgb(238, 235, 250);
        }

        private async void TextEditor_TextChanged(object sender, EventArgs e)
        {
            // Hashing could be a better way to determining if the content has changed.
            if (btnSave.Enabled == false)
            {
                btnSave.Enabled = true;
                //btnSave.Enabled = await HasChanges();
            }
        }

        private void toolStripButtonOutlookLink_Click(object sender, EventArgs e)
        {
            try
            {
                Microsoft.Office.Interop.Outlook.Application application = new Microsoft.Office.Interop.Outlook.Application();
                if (application == null) return;
                Microsoft.Office.Interop.Outlook.Explorer explorer = application.ActiveExplorer();
                if (explorer == null) return;

                Microsoft.Office.Interop.Outlook.Selection selection = explorer.Selection;
                Microsoft.Office.Interop.Outlook._NameSpace ns = application.GetNamespace("MAPI");
                if (selection.Count > 0)   // Check that selection is not empty.
                {
                    object selectedItem = selection[1];   // Index is one-based.
                    Microsoft.Office.Interop.Outlook.MailItem mailItem = selectedItem as Microsoft.Office.Interop.Outlook.MailItem;

                    if (mailItem != null)    // Check that selected item is a message.
                    {
                        string path = string.Empty;
                        if (!SaveMailItem(mailItem, out path))
                        {
                            CreateLink(mailItem);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private bool SaveMailItem(Outlook.MailItem mailItem, out string path)
        {
            try
            {
                path = string.Format(@"{0}\{1} {2} {3}.msg", 
                    GetMessagePath(), 
                    mailItem.SenderName.CleanUpName(), 
                    mailItem.ReceivedTime.ToString("yyyyMMdd HHmmss"),
                    (mailItem.SenderName + mailItem.Subject + mailItem.ReceivedTime.ToString() + mailItem.Body).GetHashCode()
                    );

                // ... set file name using message attributes
                // string fullPath = "something" + ".msg"
                mailItem.SaveAs(path, Outlook.OlSaveAsType.olMSG);
                //this.TextEditor.InsertLink(string.Format("<<file://{0}>>", path));
                string textStart = string.Format("<<file://{0}>>", path);
                int start = this.TextEditor.SelectionStart;
                this.TextEditor.SelectedText = textStart;
                Font original = this.TextEditor.SelectionFont;
                this.TextEditor.SelectionFont = new Font("Arial Narrow", 8, FontStyle.Regular);
                //this.TextEditor.SelectionStart = start + textStart.Length;
                //this.TextEditor.SelectedText = hyperlink;
                //this.TextEditor.SelectionFont = original;
                //this.TextEditor.SelectionStart = start + textStart.Length + hyperlink.Length;
                //this.TextEditor.SelectedText = ">>";
                this.TextEditor.Select(start, textStart.Length);
                this.TextEditor.SelectionProtected = true;

                return true;
            }
            catch { path = string.Empty; return false; }
        }

        private string GetMessagePath()
        {
            string path =  Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Mails");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        private void CreateLink(Outlook.MailItem mailItem)
        {
            // Process mail item here.
            String htmlBody = mailItem.HTMLBody;
            String Body = mailItem.Body;
            var iTemId = mailItem.EntryID;
            string subject = mailItem.Subject;
            string senderName = mailItem.SenderName;

            //"[[outlook:" + objMail.EntryID + "][MESSAGE: " + objMail.Subject + " (" + objMail.SenderName + ")]]"
            string hyperlink = string.Format("{0}", iTemId);
            string textStart = string.Format("<<mail:{0} {1} ({2}) ^"/*{2}>>"*/, subject.Truncate(15), mailItem.ReceivedTime, senderName/*, hyperlink*/);
            //this.TextEditor.SelectedText = link;
            //this.TextEditor.InsertLink("test");
            //this.TextEditor.InsertLink(text, mailItem.EntryID);
            //this.TextEditor.SelectedRtf = @"{\rtf1\ansi " + text + @"\v #" + hyperlink + @"\v0}";
            int start = this.TextEditor.SelectionStart;
            this.TextEditor.SelectedText = textStart;
            Font original = this.TextEditor.SelectionFont;
            this.TextEditor.SelectionFont = new Font("Arial Narrow", 1, FontStyle.Underline);
            this.TextEditor.SelectionStart = start + textStart.Length;
            this.TextEditor.SelectedText = hyperlink;
            this.TextEditor.SelectionFont = original;
            this.TextEditor.SelectionStart = start + textStart.Length + hyperlink.Length;
            this.TextEditor.SelectedText = ">>";
            this.TextEditor.Select(start, textStart.Length + hyperlink.Length + ">>".Length);
            this.TextEditor.SelectionProtected = true;


            //http://superuser.com/questions/71786/can-i-create-a-link-to-a-specific-email-message-in-outlook

            //http://msdn.microsoft.com/en-us/library/office/ff868618(v=office.15).aspx
            //Microsoft.Office.Interop.Outlook.MailItem mI = ns.GetItemFromID(iTemId);
            //mI.Display(false);

            //TODO:Saving links: http://stackoverflow.com/questions/10406786/richtextbox-help-saving-custom-links
        }

        private void contextMenuStripRichText_Opening(object sender, CancelEventArgs e)
        {

        }

        private void showRTFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.TextEditor.Rtf);
            MessageBox.Show(this.TextEditor.getLinkPositions());
        }
        /*
        {\rtf1\ansi\ansicpg1252\deff0\deflang2057{\fonttbl{\f0\fnil\fcharset0 Arial Unicode MS;}}
{\colortbl ;\red47\green79\blue79;}
{\*\generator Msftedit 5.41.21.2510;}\viewkind4\uc1\pard\cf1\f0\fs24 <<mail:Spam Manager - you have Spam messages (Spam Manager)>>\v #00000000566DF2693A9C8546AF8EDB94F10FE4E80700BD2C7789C8107342BD11437F1E03FA6D000000FAB6F70000BD2C7789C8107342BD11437F1E03FA6D00000A3F05770000\v0\par
}
*/
        private void showSelectedRTFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //MessageBox.Show(this.TextEditor.SelectedRtf);
                //string rtf = this.TextEditor.SelectedRtf;
                string rtf = this.TextEditor.SelectedText;
                Match match = Regex.Match(rtf, @"(?<=^)[A-Z 0-9]*");
                if (match == null || match.Success == false)
                {
                    match = Regex.Match(rtf, @"[A-Z 0-9]*");
                }
                if (match != null && match.Success && match.Value.Length > 0)
                {
                    Microsoft.Office.Interop.Outlook.Application application = new Microsoft.Office.Interop.Outlook.Application();
                    Microsoft.Office.Interop.Outlook.Explorer explorer = application.ActiveExplorer();
                    Microsoft.Office.Interop.Outlook._NameSpace ns = application.GetNamespace("MAPI");

                    Microsoft.Office.Interop.Outlook.MailItem mI = ns.GetItemFromID(match.Value);
                    mI.Display(false);


                    _oApplication = application;
                    var inbox = ns.GetDefaultFolder(Microsoft.Office.Interop.Outlook.OlDefaultFolders.olFolderInbox);
                    _oItems = inbox.Items;
                    _oHandler = new Microsoft.Office.Interop.Outlook.ItemsEvents_ItemChangeEventHandler(items_ItemChange);
                    _oItems.ItemChange += _oHandler;

                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void ComDispose()
        {
            _OManager.ComDispose();

            if (_oItems != null)
            _oItems.ItemChange -= _oHandler;
            _oItems = null;
            //_oApplication.Quit(); Closes desktop Outlook!
            _oApplication = null;

        }

        Microsoft.Office.Interop.Outlook.ItemsEvents_ItemChangeEventHandler _oHandler = null;
        void TextEditor_Protected(object sender, EventArgs e)
        {
            //Match match = Regex.Match(TextEditor.SelectedText, @"(?<=#)[A-Z 0-9]*");
            string text = this.TextEditor.SelectedText.Trim();
            if (text.StartsWith("<<") && text.EndsWith(">>"))
            //if (match != null && match.Success && match.Value.Length > 0)
            {
                this.TextEditor.SelectionProtected = false;
                this.TextEditor.SelectedText = string.Empty;
            }
        }
        private void showMailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int start = this.TextEditor.SelectionStart;
            int index = this.TextEditor.GetCharIndexFromPosition(_MouseRightPos);
            char selectedChar = this.TextEditor.GetCharFromPosition(_MouseRightPos);
            
            //int line = this.TextEditor.GetLineFromChar(start);
            
            int line2 = this.TextEditor.GetRealLineForCharIndex(index);
            
            //this.TextEditor.Select(this.TextEditor.GetFirstCharIndexFromLine(line2), 10);
            //MessageBox.Show( this.TextEditor.Lines[line2]);
            string text = this.TextEditor.Lines[line2];
            int lineStart = this.TextEditor.Text.IndexOf(text);
            int depthOfClickedPos = index - lineStart;
            MatchCollection collection = Regex.Matches(text, @"<<mail:[\w\d\s:\-(),./]*>>"
                , RegexOptions.Singleline );
            foreach(Match m in collection)
            {
                int index2 = text.IndexOf(m.Value);
                if (depthOfClickedPos >= index2 && depthOfClickedPos <= index2+m.Value.Length)
                {
                    //this.TextEditor.Select(lineStart+index2, m.Value.Length);
                    //this.TextEditor.SelectionStart = lineStart + index2;
                    //this.TextEditor.SelectionLength = m.Value.Length;

                    this.TextEditor.SetSelection(lineStart + index2, m.Value.Length);
                    
                    MessageBox.Show(this.TextEditor.SelectedRtf);
                    //http://msdn.microsoft.com/en-us/library/office/ff868618(v=office.15).aspx
                    Microsoft.Office.Interop.Outlook.Application application = new Microsoft.Office.Interop.Outlook.Application();
                    Microsoft.Office.Interop.Outlook.Explorer explorer = application.ActiveExplorer();
                    Microsoft.Office.Interop.Outlook._NameSpace ns = application.GetNamespace("MAPI");

                    

                    //Microsoft.Office.Interop.Outlook.MailItem mI = ns.GetItemFromID(iTemId);
                    //mI.Display(false);

                }
            }
            //int charIndex = this.TextEditor.GetFirstCharIndexOfCurrentLine();
            ////int lineIndex = this.TextEditor.GetLineFromCharIndex(charIndex);
            //int index = this.TextEditor.SelectionStart;
            ////int line = this.TextEditor.GetLineFromCharIndex(index);
            //this.TextEditor.Select(charIndex, index);
            //string text = this.TextEditor.SelectedText;
            //MessageBox.Show(text);
        }

        Microsoft.Office.Interop.Outlook.Items _oItems = null;
        Microsoft.Office.Interop.Outlook.Application _oApplication = null;

        void items_ItemChange(object item)
        {
            //MessageBox.Show("changed");
            Microsoft.Office.Interop.Outlook.MailItem mailItem = item as Microsoft.Office.Interop.Outlook.MailItem;

            if (mailItem != null)
            {
                
            }
        }

        

        private void TextEditor_MouseDown(object sender, MouseEventArgs e)
        {
            _MouseRightPos = e.Location;
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            // Create a new thread for demonstration purposes.
            Thread thread = new Thread(() =>
            {
                // Determine the size of the "virtual screen", which includes all monitors.
                int screenLeft = SystemInformation.VirtualScreen.Left;
                int screenTop = SystemInformation.VirtualScreen.Top;
                int screenWidth = SystemInformation.VirtualScreen.Width;
                int screenHeight = SystemInformation.VirtualScreen.Height;

                // Create a bitmap of the appropriate size to receive the screenshot.
                using (Bitmap bmp = new Bitmap(screenWidth, screenHeight))
                {
                    // Draw the screenshot into our bitmap.
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);
                    }

                    // Do something with the Bitmap here, like save it to a file:
                   // bmp.Save("G:\\TestImage.jpg", ImageFormat.Jpeg);
                    double aspectRatio = (double)screenHeight / (double)screenWidth;
                    Clipboard.SetImage(new Bitmap(bmp, new Size(600, (int)(600*aspectRatio))));
                    //this.TextEditor.Paste();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            this.TextEditor.Paste();
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Query != null)
            {
                string terms = this.TextEditor.SelectedText;
                if (terms.Length == 0) terms = this.TextEditor.GetCurrentFullWord();
                Query(sender, new QueryArgs() { Query =  terms});
            }
        }

        private void todayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Query?.Invoke(sender, new QueryArgs() { Query = "<This Week>" });
        }

        private void tsPreviousDocument_Click(object sender, EventArgs e)
        {
            PreviousDocument?.Invoke(this, EventArgs.Empty);
        }

        private void tsNextDocument_Click(object sender, EventArgs e)
        {
            NextDocument?.Invoke(this, EventArgs.Empty);
        }

        public void SetTextAtCusror(string text)
        {
            this.TextEditor.SelectedText = text;
        }
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            int charIndex = this.TextEditor.GetFirstCharIndexOfCurrentLine();
            int index = this.TextEditor.SelectionStart;
            this.TextEditor.Select(charIndex, index);
            string text = this.TextEditor.SelectedText;
            if (text.StartsWith("▶"))
            {
                this.TextEditor.Select(charIndex, 1);
                this.TextEditor.SelectedText = string.Format("⋫ {0} <Obsolete>", DateTime.Now.ToString());//ø ⊳ ⋫ Ⓦ ▷  ✓ ✱ Ⓘ
            }
        }

        private void tsbInsertLink_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string textStart = string.Format("<<file://{0}>>", ofd.FileName);
                    int start = this.TextEditor.SelectionStart;
                    this.TextEditor.SelectedText = textStart;
                    Font original = this.TextEditor.SelectionFont;
                    this.TextEditor.SelectionFont = new Font("Arial Narrow", 8, FontStyle.Regular);
                    this.TextEditor.Select(start, textStart.Length);
                    this.TextEditor.SelectionProtected = true;
                }
            }
        }

        private void WeeklyUpdatesCtrWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (WeeklyUpdates != null)
            {
                
                WeeklyUpdates(sender, new WeeklyUpdatesArgs() { Date = DateTime.Today.AddDays(1) });
            }
        }
    }
}
