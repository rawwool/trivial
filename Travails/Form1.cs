using Ionic.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Travails.Model;
using TextRuler;
using System.Diagnostics;
using TrivialModel;
using TrivialModel.Model;
using System.Threading.Tasks;
using RichTextBoxLinks;
using TextRuler.AdvancedTextEditorControl;
using System.Threading;
using TrivialData;
using System.Runtime.InteropServices;
using Ionic;

namespace Travails
{
    public partial class Form1 : Form
    {
        BackgroundWorker _Worker = new BackgroundWorker();
        System.Timers.Timer _Timer = new System.Timers.Timer();
        const int TIME_INTERVAL_IN_MILLISECONDS = 10000;
        const int TIME_INTERVAL_IN_MILLISECONDS2 = 1000;
        const int ALLOWED_PREVNEXT_SIZE = 50;

        System.Threading.Timer _TTimer = null;
        System.Threading.Timer _TTimerDialog = null;
        //NotifyIcon _NotifyIcon = new NotifyIcon();

        Dictionary<DateTime, TreeNode> _DictDairyNodes = new Dictionary<DateTime, TreeNode>();
        Dictionary<string, TreeNode> _DictNamedNodes = new Dictionary<string, TreeNode>();
        Dictionary<DateTime, List<TreeNode>> _DictTracks = new Dictionary<DateTime, List<TreeNode>>();
        List<TreeNode> _ListVisitedNodes = new List<TreeNode>();
        Dictionary<long, int> _DictViewPosition = new Dictionary<long, int>();
        int _CurrentVisitedIndex = 0;

        const string KNOWLEDGE_BASE_NODE_KEY = "KB";
        const string DIARIES_NODE_KEY = "Diaries";
        const string ACTIVE_TRACKS_ROOT_NODE = "Active Tracks";
        const string INPUTS_TO = "Inputs To";
        const string INPUTS_FROM = "Inputs From";
        const string KNOWLEDGE_BASE_FOLDER = ".\\KB";
        const string YEAR_NODE = "Year Node";
        const string MONTH_NODE = "Month Node";
        const string WORK_LOG = "Work Log";

        string _CurrentFile = string.Empty;

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;

            //string[] sentences = "This is test. This is second sentence.  This is the third. ".Split(". ");

            advancedTextEditor1.OnSave += advancedTextEditor1_OnSave;
            advancedTextEditor1.OnOpen += advancedTextEditor1_OnOpen;
            advancedTextEditor1.QueryListItem += advancedTextEditor1_QueryListItem;
            advancedTextEditor1.Query += advancedTextEditor1_Query;
            advancedTextEditor1.PreviousDocument += AdvancedTextEditor1_PreviousDocument;
            advancedTextEditor1.NextDocument += AdvancedTextEditor1_NextDocument;
            advancedTextEditor1.LinkClicked += AdvancedTextEditor1_LinkClicked;

            treeView1.HideSelection = false;
            treeView1.TreeViewNodeSorter = new NodeSorter();

            //_NotifyIcon.Visible = true;
            //_NotifyIcon.Icon = SystemIcons.Application;


            //_Worker.DoWork += _Worker_DoWork;
            //_Worker.WorkerReportsProgress = true;
            //_Worker.ProgressChanged += _Worker_ProgressChanged;
            //_Worker.RunWorkerAsync();

            //_Timer.Interval = 10000;
            //_Timer.Elapsed += _Timer_Elapsed;
            //_Timer.Start();

            //_TTimerDialog = new System.Threading.Timer(CloseOutlookDialog, null, TIME_INTERVAL_IN_MILLISECONDS2, Timeout.Infinite);
            
            //_TTimer = new System.Threading.Timer(Callback, null, TIME_INTERVAL_IN_MILLISECONDS, Timeout.Infinite);

            Application.ApplicationExit += Application_ApplicationExit;
            //var test = MSWordReader.ReadDocument("test.docx");
            DataProvider.ShowRequested += DataProvider_ShowRequested;
        }

        private void AdvancedTextEditor1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            ProcessLinkClick(e);
        }

        private static void ProcessLinkClick(LinkClickedEventArgs e)
        {
            string fileName = string.Empty;
            if (string.IsNullOrEmpty(e.LinkText)) return;
            MatchCollection matches = Regex.Matches(e.LinkText, @"://[.\\/\w\d- ]*.rtf");
            if (matches.Count > 0)
            {
                string matchString = matches[0].Value;
                int lineIndex = 0;
                //https://regex101.com/r/cFHC5q/1
                MatchCollection lineNoMatches = Regex.Matches(e.LinkText, @"([\d]+$\b)");
                if (lineNoMatches.Count > 0)
                {
                    Int32.TryParse(lineNoMatches[0].Value, out lineIndex);
                }
                DataProvider.ShowLinkedDocument(matchString.TrimStart(':','/','\\'), lineIndex);
                return;
            }
            //://\w:[.\\/\w\d- ]*.msg
            matches = Regex.Matches(e.LinkText, @"://\w:[.\\/\w\d- ]*.\w*");

            if (matches.Count > 0)
            {
                string matchString = matches[0].Value.TrimStart(':','/');

                if (File.Exists(matchString))
                {
                    Process.Start(matchString);
                }
                
                return;
            }

            Process.Start(e.LinkText);
        }

        bool _PrevNextSelectionInProgress = false;
        private void AdvancedTextEditor1_NextDocument(object sender, EventArgs e)
        {
            _PrevNextSelectionInProgress = true;
            int index = _CurrentVisitedIndex + 1;
            if (_ListVisitedNodes.Count() > index)
            {
                _CurrentVisitedIndex = index;
                this.treeView1.SelectedNode = _ListVisitedNodes[index];
            }
        }

        private void AdvancedTextEditor1_PreviousDocument(object sender, EventArgs e)
        {
            _PrevNextSelectionInProgress = true;
            int index = _CurrentVisitedIndex - 1;
            if (index >= 0 && _ListVisitedNodes.Count() > index)
            {
                _CurrentVisitedIndex = index;
                this.treeView1.SelectedNode = _ListVisitedNodes[index];
            }
        }
        
        private void UpdatePrevNextNodeList(TreeNode node)
        {
            if (_PrevNextSelectionInProgress) return;
            //if (_ListVisitedNodes.LastOrDefault() == null || (_ListVisitedNodes.Last() != node && _ListVisitedNodes[_CurrentVisitedIndex] != node))
            {
                {
                    _ListVisitedNodes.Add(node);
                }
                _ListVisitedNodes = _ListVisitedNodes.NonConsecutive().ToList();
                if (_ListVisitedNodes.Count() > ALLOWED_PREVNEXT_SIZE)
                {
                    int toSkip = _ListVisitedNodes.Count() - ALLOWED_PREVNEXT_SIZE;
                    _ListVisitedNodes = _ListVisitedNodes.Skip(toSkip).ToList();
                }

                _CurrentVisitedIndex = _ListVisitedNodes.LastIndexOf(node);
            }
        }

        /*
        const int WM_COMMAND = 0x0111;
        const int BN_CLICKED = 0;
        const int ButtonId = 0x79;
        private const int BM_SETCHECK = 0x00f1;
        private const int BST_CHECKED = 0x0001;
        const int BM_CLICK = 0x00F5;
        int CB_GETLBTEXT = 0x0148;
        int CB_SETCURSEL = 0x014E;
        const string fn = @"C:\Windows\system32\calc.exe";
        [DllImport("user32.dll")]
        static extern IntPtr GetDlgItem(IntPtr hWnd, int nIDDlgItem);
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

           // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.

        [DllImport("user32.dll", EntryPoint="FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        // You can also call FindWindow(default(string), lpWindowName) or FindWindow((string)null, lpWindowName)


        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        */
        const int IDALLOW = 0x00012A6;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
        private void CloseOutlookDialog(object state)
        {
            var dialog = User32.FindWindow(null, "Microsoft Outlook");
            if (dialog != null)
            {
                var allChildWindows = new WindowHandleInfo(dialog).GetAllChildHandles();
                var checkBoxAllowAccess = allChildWindows.Where(s => s.Item2 == "&Allow access for").FirstOrDefault();
                if (checkBoxAllowAccess != null)
                {
                    //int wParam = (BN_CLICKED << 16) | (ButtonId & 0xffff);
                    //SendMessage(dialog, WM_COMMAND, wParam, checkBoxAllowAccess.Item1);
                    //PostMessage(checkBoxAllowAccess.Item1, BM_SETCHECK, BST_CHECKED, IntPtr.Zero);
                    User32.PostMessage(checkBoxAllowAccess.Item1, (int)User32.Msgs.BM_SETCHECK, (int)User32.Msgs.BST_CHECKED, 0);
                    //var comboBox = allChildWindows.Where(s => s.Item2 == "1 minute").FirstOrDefault();
                    //if (comboBox != null)
                    {
                        //allChildWindows.ForEach(s => PostMessage(s.Item1, CB_SETCURSEL, 3, IntPtr.Zero));
                        allChildWindows.ForEach(s => User32.PostMessage(s.Item1, (int)User32.Msgs.CB_SETCURSEL, 3, 0));
                        Thread.Sleep(100);
                        //PostMessage(comboBox.Item1, CB_SETCURSEL, 3, IntPtr.Zero);
                        var buttonAllowAccess = allChildWindows.Where(s => s.Item2 == "Allow").FirstOrDefault();
                        if (buttonAllowAccess != null)
                        {
                            //User32.SetForegroundWindowNative(dialog);
                            //Thread.Sleep(100);
                            //User32.SetForegroundWindowNative(buttonAllowAccess.Item1);
                            //User32.SetFocus(buttonAllowAccess.Item1);
                            //PostMessage(buttonAllowAccess.Item1, BM_CLICK, 0, IntPtr.Zero);
                            //User32.PostMessage(buttonAllowAccess.Item1, (int)User32.Msgs.WM_LBUTTONDOWN, 0, 0);
                            
                            //User32.PostMessage(buttonAllowAccess.Item1, (int)User32.Msgs.WM_LBUTTONUP, 0, 0);
                            //http://stackoverflow.com/questions/14962081/click-on-ok-button-of-message-box-using-winapi-in-c-sharp
                            //http://stackoverflow.com/questions/23186469/sendmessagehwnd-msg-wparam-lparam-difficulties
                            //PostMessage(dialog, (int)User32.Msgs.WM_COMMAND, ((int)User32.Msgs.BM_CLICK << 16) | IDALLOW, buttonAllowAccess.Item1);
                            int WM_COMMAND = 0x111;
                            int BN_CLICKED = 245;

                            int controlID = 0x00012A6; // Something you don't know
                            int wParam = BN_CLICKED >> 16 | controlID;

                            PostMessage(dialog, WM_COMMAND, wParam , buttonAllowAccess.Item1);
                            //User32.SetFocus(buttonAllowAccess.Item1);
                            //Thread.Sleep(100);
                            //User32.SetFocus(buttonAllowAccess.Item1);
                            //User32.PostMessage(buttonAllowAccess.Item1, (int)User32.Msgs.BM_CLICK, 0, 0);
                        }
                    }
                }
            }

            _TTimerDialog.Change(TIME_INTERVAL_IN_MILLISECONDS2, Timeout.Infinite);
            /*
            var processes = Process.GetProcesses();
            var names = processes.Select(s => s.ProcessName).ToList();
            var process = processes.Where(s => s.ProcessName.Contains("OUTLOOK")).FirstOrDefault();
            if (process != null)
            {
                IntPtr handle = process.Handle;
                IntPtr hWndButton = GetDlgItem(handle, 0x12A3);
                int wParam = (BN_CLICKED << 16) | (ButtonId & 0xffff);
                SendMessage(handle, WM_COMMAND, wParam, hWndButton);
                //IntPtr hWndButton = GetDlgItem(handle, ButtonId);
                /*
                    TestStack.White.Application app = TestStack.White.Application.Attach(process.Id);
                    var window = app.GetWindow("Microsot Outlook");
                    if (window != null)
                    {
                        var button = window.Get<TestStack.White.UIItems.Button>("save");
                    }
                }
                 *
            } */

        }

        void advancedTextEditor1_Query(object sender, QueryArgs args)
        {
         if (args.Query == null || args.Query.Length == 0) return;

            if (args.Query == "<This Week>")
            {
                var thisWeekActions = DataProvider.GetActions(DateTime.Today, DateTime.MaxValue /*DateTime.Today.AddDays(DayOfWeek.Friday - DateTime.Today.DayOfWeek)*/);
                var oldDueActions = DataProvider.GetActions(DateTime.MinValue, DateTime.Today.AddDays(-1)).OrderByDescending(s=>s.DateDue).ToList();
                ShowQueryresults("<This Week>", thisWeekActions, "Today and future", oldDueActions, "Past");
            }
            //string verb = args.Query.Split(" ").FirstOrDefault().ToLower();
            //if (verb == "search")
            {
                //List<string> terms = args.Query.Split(" ")/*.Skip(1)*/.Select(s => s.Trim()).ToList();
                
                // Get the terms with % and #
                //var tagTerms = terms.Where(s => s.StartsWith("%") || s.StartsWith("#")).ToList();
                var tagTerms = Regex.Matches(args.Query, @"[#%]\w*")
                    .Cast<Match>()
                    .Select(s => s.Value)
                    .ToList();
                if (tagTerms.FirstOrDefault() != null)
                {
                    //var tagLines = DB.GetTags(terms.First()).OrderBy(s => s.DateTime);
                    var tagLines = DB.GetTags(tagTerms).OrderBy(s => s.DateTime);
                    if (tagLines.Count() > 0)
                    {
                        ShowQueryResults(tagLines, tagTerms);
                    }
                }
                //var userTerms = terms.Where(s => s.StartsWith("@")).ToList();
                var userTerms = Regex.Matches(args.Query, @"@\w*")
                    .Cast<Match>()
                    .Select(s => s.Value)
                    .ToList();
                if (userTerms.FirstOrDefault() != null)
                {
                    string user = userTerms.FirstOrDefault().Trim('@');
                    var fromActions = DataProvider.GetFromActions(user);
                    var toActions = DataProvider.GetToActions(user);
                    ShowQueryresults(userTerms.First(), fromActions, "From", toActions, "To");
                }
            }
        }

        public class TrivialQueryArgs
        {
            public string Context { get; set; }
            public long Id { get; set; }
        }

        private void ShowQueryresults(string name, List<ActionData> currentActions, string fromText,
            List<ActionData> pastActions, string toText)
        {
            long key = DateTime.Now.Ticks;
            ListView lView = new ListView();
            lView.MultiSelect = true;
            lView.KeyDown += lView_KeyDown;
            
            //small image list hack
            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(10,50);
            lView.SmallImageList = imgList;
            lView.Dock = DockStyle.Fill;
            lView.View = View.Details;
            lView.Groups.Clear();
            lView.ShowGroups = true;
            ColumnHeader header = new ColumnHeader();
            header.Name = "Name";
            header.TextAlign = HorizontalAlignment.Left;
            header.Width = -2;
            header.Text = "Action";
            header.Width = 540;
            lView.Columns.Add(header);
            lView.Columns.Add("Context", 100, HorizontalAlignment.Left);
            lView.Columns.Add("From", 100, HorizontalAlignment.Left);
            lView.Columns.Add("To", 100, HorizontalAlignment.Left);
            lView.Columns.Add("FollowUps", 30, HorizontalAlignment.Left);
            lView.Columns.Add("Due Date", 80, HorizontalAlignment.Left);
            
            //lView.ItemSelectionChanged += lView_ItemSelectionChanged;
            lView.ItemActivate += lView_ItemActivate;
            lView.ItemSelectionChanged += LView_ItemSelectionChanged;
            lView.Activation = ItemActivation.Standard;
            
            lView.Tag = new TrivialQueryArgs() { Context = name, Id = key }; ;

            PopulateItems(currentActions, lView, fromText);
            PopulateItems(pastActions, lView, toText);


            Form form = new Form();
            form.Top = this.Top;
            form.Left = this.Left;
            form.Width = this.Width;
            form.Height = this.Height;
            form.Tag = key;
            //form.Owner = this;
            //form.TopMost = false;
            //form.StartPosition = FormStartPosition.CenterParent;
            form.Controls.Add(lView);
            form.FormClosed += Form_FormClosed;
            form.Show(/*this*/);
        }

        private void LView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (sender is ListView && (sender as ListView).Tag != null && (sender as ListView).Tag is TrivialQueryArgs)
            {
                long key = ((TrivialQueryArgs)(sender as ListView).Tag).Id;
                int index = (sender as ListView).TopItem.Index;
                if (!_DictViewPosition.ContainsKey(key))
                {
                    _DictViewPosition.Add(key, e.ItemIndex);
                }
                else
                {
                    _DictViewPosition[key] = e.ItemIndex;
                }
            }
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void lView_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ListView && e.KeyCode == Keys.F5)
            {
                (sender as ListView).Items.Clear();
                (sender as ListView).Groups.Clear();
                if ((sender as ListView).Tag != null && (sender as ListView).Tag is TrivialQueryArgs)
                {
                    string term = ((sender as ListView).Tag as TrivialQueryArgs).Context;
                    if (term == "<This Week>")
                    {
                        var thisWeekActions = DataProvider.GetActions(DateTime.Today, DateTime.MaxValue /*DateTime.Today.AddDays(DayOfWeek.Friday - DateTime.Today.DayOfWeek)*/);
                        var oldDueActions = DataProvider.GetActions(DateTime.MinValue, DateTime.Today.AddDays(-1)).OrderByDescending(s => s.DateDue).ToList();
                        //ShowQueryresults("<Today>", thisWeekActions, "Future", oldDueActions, "Past");
                        PopulateItems(thisWeekActions, (sender as ListView), "Today and future");
                        PopulateItems(oldDueActions, (sender as ListView), "Past");
                    }
                    else
                    {
                        term = term.Trim('@');
                        var fromActions = DataProvider.GetFromActions(term);
                        var toActions = DataProvider.GetToActions(term);
                        PopulateItems(fromActions, (sender as ListView), "From");
                        PopulateItems(toActions, (sender as ListView), "To");
                    }
                    long id = ((sender as ListView).Tag as TrivialQueryArgs).Id;
                    int index = -1;
                    if (_DictViewPosition.TryGetValue(id, out index))
                    {
                        (sender as ListView).EnsureVisible(index);
                    }
                }
            }
            if (sender is ListView && e.Control && e.KeyCode == Keys.E)
            {
                var builder = new StringBuilder();
                foreach(ColumnHeader colum in  (sender as ListView).Columns)
                {
                    builder.Append(colum.Text + "\t");
                }
                builder.Append("\n");
                foreach (ListViewItem item in (sender as ListView).Items)
                {
                    foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                    {
                        builder.Append(subItem.Text + "\t");
                    }
                    builder.Append("\n");
                }
                Clipboard.SetText(builder.ToString());
            }
        }

        private static void PopulateItems(List<ActionData> actions, ListView lView, string group)
        {
            if (actions != null && actions.Count > 0)
            {
                string grName = string.Format("{0} ({1})", group, actions.Count());
                ListViewGroup fromGroup = null;
                foreach(var agroup in lView.Groups.Cast<ListViewGroup>())
                {
                    if (agroup.Name == grName)
                    {
                        fromGroup = agroup;
                        break;
                    }
                }
                if (fromGroup == null)
                {
                    fromGroup = new ListViewGroup(grName);
                    fromGroup.HeaderAlignment = HorizontalAlignment.Left;

                    lView.Groups.Add(fromGroup);
                }
                foreach (ActionData action in actions)
                {
                    ListViewItem item = new ListViewItem()
                    {
                        Text = action.Name,
                        Tag = action,
                        ForeColor = Color.DarkSlateGray,
                        Font = new Font("Lucida Grande", 10)
                    };
                    item.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "From", Text = action.TrackList.Count() == 0 ? "" : action.TrackList.Aggregate((a, b) => a + ", " + b) });
                    item.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "From", Text = action.InputFrom.Count() == 0 ? "" : action.InputFrom.Aggregate((a, b) => a + ", " + b) });
                    item.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "To", Text = action.InputTo.Count() == 0 ? "" : action.InputTo.Aggregate((a, b) => a + ", " + b) });
                    item.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "FollowUps", Text = action.InfoList.Count().ToString() });
                    item.SubItems.Add(new ListViewItem.ListViewSubItem() { Name = "Due Date", Text = action.DateDue.ToShortDateString() });
                    Color bgColor = item.BackColor;
                    if (action.DateDue.Date == DateTime.Today) item.BackColor = Color.LightYellow;
                    else
                    if (action.DateDue.Date > DateTime.Today)
                    {
                        if (action.DateDue.Date > DateTime.Today.AddYears(1))
                        {
                            item.BackColor = bgColor;
                        }
                        else
                        {
                            item.BackColor = Color.LightGreen;
                        }
                    }
                    else
                    {
                        item.BackColor = Color.LightSalmon;
                    }
                   
                    lView.Items.Add(item);
                    item.Group = fromGroup;
                }
            }
        }

        void lView_ItemActivate(object sender, EventArgs e)
        {
            if (sender is ListView)
            {
                ListViewItem lvItem = (sender as ListView).SelectedItems[0];
                ActionData data = lvItem.Tag as ActionData;
                if (data != null)
                {
                    DataProvider.ShowAction(data.Parent.Date, data.Name);
                }
            }
        }

        void lView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && e.Item.Tag != null)
            {
                ActionData data = e.Item.Tag as ActionData;
                DataProvider.ShowAction(data.Parent.Date, data.Name);
            }
        }

        void panel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (sender is Label)
            {
                ActionData data = (sender as Label).Tag as ActionData;
                DataProvider.ShowAction(data.Parent.Date, data.Name);
            }
        }

        private void ShowQueryResults(IEnumerable<TrivialData.Tag> tagLines, IEnumerable<string> terms)
        {
            //RichTextBox rtb = new RichTextBox();
            RichTextBoxEx rtb = new RichTextBoxEx();
            rtb.LinkClickedEx += Rtb_LinkClickedEx;

            rtb.Font = new Font("Myriad Pro", 10);// new Font("Ariel Unicode MS", 11.0f);
            rtb.Dock = DockStyle.Fill;
            rtb.ForeColor = Color.DarkSlateGray;
            
            foreach (Tag tagLine in tagLines)
            {
                //rtb.AppendText(tagLine.Id + "\n");
                //public void InsertLink(string text, string hyperlink, int position)
                rtb.InsertLink(string.Format("<<file://{0}>>", tagLine.Id));
                rtb.AppendText("\n");
                rtb.AppendText(tagLine.Line + "\n");
                foreach(string followUp in tagLine.FollowUps)
                {
                    rtb.AppendText("\t" + followUp + "\n");
                }
                rtb.AppendText("\n\n");
            }
            rtb.HighlightText(terms, Color.Yellow);
            

            Form form = new Form();
            form.Top = this.Top;
            form.Left = this.Left;
            form.Width = this.Width;
            form.Height = this.Height;
            //form.Owner = this;
            //form.TopMost = false;
            //form.StartPosition = FormStartPosition.CenterParent;
            form.Controls.Add(rtb);
            form.Show(/*this*/);
           
        }

        private void Rtb_LinkClickedEx(object sender, LinkClickedEventArgs e)
        {
            ProcessLinkClick(e);
        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            //_NotifyIcon.Dispose();
            //_NotifyIcon = null;
        }

        private void Callback(Object state)
        {
            return;
            // Long running operation
            if (ProcessOutlookAppointments())
            {
                _TTimer.Change(TIME_INTERVAL_IN_MILLISECONDS, Timeout.Infinite);
            }
        }

        //void _Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //}

        //void _Worker_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    while (true)
        //    {
        //        int duration =  ProcessOutlookAppointments();
        //        System.Threading.Thread.Sleep(duration * 1000);
        //    }
        //}
        
        

        ///*async */void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    /*await */ProcessOutlookAppointments();
        //}

        private bool ProcessOutlookAppointments()
        {
            int iReturn = 10;
            bool success = true;
            try
            {
                //https://social.technet.microsoft.com/Forums/office/en-US/64c14bd3-0e7f-4ba9-b2bd-26cf62ce5883/how-to-stop-outlook-security-message-a-program-is-trying-to-access-email-addresses-allow-access?forum=outlook
                //http://www.ryadel.com/en/how-to-stop-the-outlook-a-program-is-trying-to-access-pop-up/
                OutlookManager manager = new OutlookManager();
                List<TextRuler.AdvancedTextEditorControl.OutlookManager.Appointment> list =
                    manager.GetAppointmentsInRange(DateTime.Now, DateTime.Now.Date.AddDays(1).AddSeconds(-1));
                TextRuler.AdvancedTextEditorControl.OutlookManager.Appointment curent = list.OrderBy(s => s.Start).FirstOrDefault();
                if (curent != null)
                {
                    double minutesToGo = curent.Start.Subtract(DateTime.Now).TotalMinutes;
                    if (minutesToGo < 10)
                    {
                        //MessageBox.Show(string.Format("Meeting in {0} minutes : {1}", minutesToGo, curent.Subject));
                        string title = string.Format("{0}'s Meeting in {1:0.00} minutes at {2}", curent.Organiser, minutesToGo, curent.Location);
                        /*success = await */ShowBalloon(title, curent.Subject, 2000);
                        iReturn = Math.Max(5, (int)Math.Round(minutesToGo));
                    }
                    if (minutesToGo < 1.5 && minutesToGo >= -0.25)
                    {
                        //System.Media.SystemSounds.Beep.Play();
                        try
                        {
                            //this.BeginInvoke((Action)(() => Ball.Bouncer.Bounce()));
                        }
                        catch { }
                        Console.Beep();
                    }
                    else
                    {
                        //Ball.Bouncer.Hide();
                       // this.BeginInvoke((Action)(() => Ball.Bouncer.Hide()));
                    }
                }
                else
                {
                    //Ball.Bouncer.Hide();
                    //this.BeginInvoke((Action)(() => Ball.Bouncer.Hide()));
                }

            }
            catch
            {
                //Ball.Bouncer.Hide();
                //this.BeginInvoke((Action)(() => Ball.Bouncer.Hide()));
                success = false;
            }

            //return iReturn;
            return success;
        }

        //TODO: Notification using WPF: http://stackoverflow.com/questions/3034741/create-popup-toaster-notifications-in-windows-with-net

        object _BaloonLock = new object();

        private /*async Task<bool>*/void ShowBalloon(string title, string body, int duration)
        {
            /*await Task.Run(() =>*/
            lock(_BaloonLock)
            {
                NotifyIcon notifyIcon = new NotifyIcon()
                {
                    Visible = true,
                    Icon = System.Drawing.SystemIcons.Information,
                };

                //notifyIcon.BalloonTipClosed += notifyIcon_BalloonTipClosed;
                
                if (title != null || title.Length >0)
                {
                    notifyIcon.BalloonTipTitle = title;
                }
                else
                {
                    notifyIcon.BalloonTipTitle = "<none>";
                }

                if (body != null)
                {
                    notifyIcon.BalloonTipText = body;
                }

                notifyIcon.ShowBalloonTip(duration);

                Thread.Sleep(6000);

                notifyIcon.Visible = false;

                notifyIcon.Dispose();

                /*);

                return true;*/
            }
        }

        //void notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        //{
        //    (sender as NotifyIcon).Dispose();
        //}
    
        delegate bool OpenDelegate(DateTime date, string name);

        bool DataProvider_ShowRequested(object sender, ActionEventArgs e)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    return ((bool)this.Invoke(new OpenDelegate(Open), e.Date, e.Name));
                }
                else
                {
                    if (!string.IsNullOrEmpty(e.FileName))
                    {
                        return Open(e.FileName, e.LineIndex);
                    }
                    else
                    {
                        return Open(e.Date, e.Name);
                    }
                }
            }
            catch { return false; }
        }

        bool OpenSync(DateTime date, string name)
        {
            string file = GetPath(string.Format("{0}.rtf", date.ToString("yyyy-MM-dd")));
            if (File.Exists(file))
            {
                bool open = advancedTextEditor1.OpenAsync(file, date).Result;
                if (open)
                {
                    advancedTextEditor1.Find(name);
                    return true;
                }
                else
                {
                    treeView1.SelectedNode = _CurrentSelectedNode;
                }
            }

            return false;
        }

        bool Open(DateTime date, string name)
        {
            TreeNode diaryNode = treeView1.Nodes.Find(DIARIES_NODE_KEY, false).FirstOrDefault();
            TreeNode yeaN = diaryNode.Nodes.Find(date.Date.ToString("yyyy-MM-dd"), true).FirstOrDefault();
            //TODO: Deal with else condition
            if (yeaN != null)
            {
                Application.DoEvents();
                this.Activate();
                this.BringToFront();
                treeView1.SelectedNode = yeaN;
                Application.DoEvents();
                Thread.Sleep(200);
                advancedTextEditor1.Find(name);
                return true;
            }

            return false;
        }

        bool Open(string fileName, int lineIndex)
        {
            TreeNode diaryNode = treeView1.Nodes.Find(DIARIES_NODE_KEY, false).FirstOrDefault();
            TreeNode kbRootNode = treeView1.Nodes.Find(KNOWLEDGE_BASE_NODE_KEY, false).FirstOrDefault();
            
            TreeNode tNode = diaryNode.Nodes.Find(fileName.TrimEnd('r', 't', 'f').TrimEnd('.'), true).FirstOrDefault();
            if (tNode == null)
            {
                tNode = kbRootNode.Nodes.Find(".\\" + fileName/*.TrimEnd('r', 't', 'f').TrimEnd('.')*/, true).FirstOrDefault();
            }
            if (tNode != null)
            {
                Application.DoEvents();
                this.Activate();
                this.BringToFront();
                treeView1.SelectedNode = tNode;
                Application.DoEvents();
                Thread.Sleep(200);
                //advancedTextEditor1.Find(name);
                
                advancedTextEditor1.GotoRealLine(lineIndex);
                return true;
            }

            return false;

            //TreeNode diaryNode = treeView1.Nodes.Find(KNOWLEDGE_BASE_FOLDER, false).FirstOrDefault();
            //TreeNode yeaN = diaryNode.Nodes.Find(fileName, true).FirstOrDefault();
            ////TODO: Deal with else condition
            //if (yeaN != null)
            //{
            //    Application.DoEvents();
            //    this.Activate();
            //    this.BringToFront();
            //    treeView1.SelectedNode = yeaN;
            //    Application.DoEvents();
            //    Thread.Sleep(200);
            //    advancedTextEditor1.Find(name);
            //    return true;
            //}

            //return false;

        }

        void advancedTextEditor1_OnOpen(object o, TextRuler.AdvancedTextEditorControl.AdvancedTextEditor.SaveArgs e)
        {
            _CurrentFile = e.File;
            SetFormTitle();
        }

        private void SetFormTitle(bool gcCollect = false)
        {
            string appMemory = MemorySize.SizeSuffix(GC.GetTotalMemory(gcCollect));
            Process process = Process.GetCurrentProcess();
            process.Refresh();
            string processMemory = MemorySize.SizeSuffix(process.WorkingSet64);

            if (_CurrentFile != null && _CurrentFile.Length > 0)
            {
                this.Text = string.Format("Trivial - {0} (app memory - {1}, process memory - {2})", 
                    Path.GetFileNameWithoutExtension(_CurrentFile),
                    appMemory,
                    processMemory);
            }
            else
            {
                this.Text = "Trivial";
                this.Text = string.Format("Trivial (app memory - {0}, process memory - {1})",
                    appMemory,
                    processMemory);
            }
        }

        void advancedTextEditor1_QueryListItem(object sender, RichTextBoxLinks.QueryListItemsArgs args)
        {
            List<string> list = new List<string>();
            Console.WriteLine(args.Fragment);
            Console.WriteLine(args.PreviousWOrd);
            Console.WriteLine(args.PreviousToPreviousWord);
            if (args.Fragment != null && args.Fragment.Length > 0)
            {
                var words = DataProvider.UsedWords.Where(s => s.ToLower().StartsWith(args.Fragment.ToLower())).Take(4).ToList();
                Console.WriteLine();
                foreach (string word in words) Console.WriteLine(word);
                list.AddRange(words);
            }
            else if (args.PreviousWOrd != null && args.PreviousWOrd.Length > 0)
            {
                string keyOne = string.Empty;
                string keyTwo = string.Empty;
                if (args.PreviousToPreviousWord != null && args.PreviousToPreviousWord.Length > 0)
                {
                    keyTwo = string.Format("[{0}][{1}]", args.PreviousToPreviousWord, args.PreviousWOrd).ToLower();
                }
                
                keyOne = string.Format("[{0}]", args.PreviousWOrd).ToLower();
                
                // First fill up with 2-Gram predictions
                if (keyTwo.Length > 0 && DataProvider._DictNGram.ContainsKey(keyTwo))
                {
                    list.AddRange(DataProvider._DictNGram[keyTwo].OrderByDescending(s => s.Frequency).Take(4).Select(s => s.Word));
                }

                //Then the 1-Gram predictions
                if (keyOne.Length > 0 && DataProvider._DictNGram.ContainsKey(keyOne))
                {
                    list.AddRange(DataProvider._DictNGram[keyOne].OrderByDescending(s => s.Frequency).Take(7 - list.Count()).Select(s => s.Word));
                }
            }

            args.List = list.Where(s=>s.Trim().Length > 0).Distinct();
        }

        async void advancedTextEditor1_OnSave(object o, TextRuler.AdvancedTextEditorControl.AdvancedTextEditor.SaveArgs e)
        {
            try
            {
                DayData data = new DayData();
                DateTime date;
                Queue<TreeNode> queueSelectedNodes = new Queue<TreeNode>();
                TreeNode selectedNodeBeforeSave = treeView1.SelectedNode;
                TreeNode selectedNodeParent = null;
                if (selectedNodeBeforeSave != null)
                {
                    selectedNodeParent = selectedNodeBeforeSave.Parent;
                    // while (selectedNodeBeforeSave != null)
                    {
                        if (selectedNodeBeforeSave.NextNode != null)
                        {
                            queueSelectedNodes.Enqueue(selectedNodeBeforeSave.NextNode);
                        }
                        if (selectedNodeBeforeSave.PrevNode != null)
                        {
                            queueSelectedNodes.Enqueue(selectedNodeBeforeSave.PrevNode);
                        }
                        //selectedNodeBeforeSave = selectedNodeBeforeSave.Parent;
                    }
                }
                treeView1.AfterSelect -= treeView1_AfterSelect;

                treeView1.BeginUpdate();
                List<string> usedWords = new List<string>();
                string documentName = GetDocumentNameWithPathRelativeToTrivial(e.File);//System.IO.Path.GetFileName(e.File);
                if (DateTime.TryParse(System.IO.Path.GetFileNameWithoutExtension(e.File), out date))
                {
                    UpdateDiaryCollection(date);
                }
                else
                {
                    date = DateTime.Today;
                }
                { 
                    data.Date = date;
                    await Task.Run(() =>
                    {
                        DataProvider.RemoveDocumentTags(documentName);
                        int lineIndex = 0;
                        foreach (string line in advancedTextEditor1.RealLines)
                        {
                            ActionData actionData = null;
                            WorkData workData = null;
                            //DB.RemoveDocumentTags(documentName);
                            if (DataProvider.TryParse(documentName, line, lineIndex, data, Travails.Model.DataProvider.DataBuildMode.Update, out actionData, out workData))
                            {
                                if (actionData != null) data.Actions.Add(actionData);
                            }
                            DataProvider.ProcessNGrams(line);
                            lineIndex++;
                        }
                    });

                    if (_DictDairyNodes.ContainsKey(date.Date))
                    {
                        // node = treeView1.Nodes.Find(date.Date.ToString("yyyy-MM-dd"), true).FirstOrDefault();
                        TreeNode node = _DictDairyNodes[date.Date];
                        //System.Diagnostics.Debug.Assert(node != null, "Diary not found!");
                        if (node != null)
                        {
                            node.Tag = data;
                            await Task.Run(() =>
                            {
                                if (this.InvokeRequired)
                                {
                                    this.Invoke((Action)(() => node.Nodes.Clear()));
                                    this.Invoke((Action)(() => ClearInputNodes(data, INPUTS_TO)));
                                    this.Invoke((Action)(() => ClearInputNodes(data, INPUTS_FROM)));

                                    DataProvider.AddDayData(data);
                                    this.Invoke((Action)(() => PopulateNode(node, data)));
                                }
                                else
                                {
                                    node.Nodes.Clear();
                                    ClearInputNodes(data, INPUTS_TO);
                                    ClearInputNodes(data, INPUTS_FROM);
                                    DataProvider.AddDayData(data);
                                    PopulateNode(node, data);
                                }


                            });
                        }
                    }
                }

                await Task.Run(() =>
                {
                    foreach (string line in advancedTextEditor1.RealLines)
                    {
                        //ProcessNGrams(line); Canno enable right now otherwise there would be duplicate entries for repeated saves.

                        //usedWords.AddRange(line.Split(' ', ',', ':', '\'', ';', '"', '.', ')', ']', '}')
                        //    .Where(s => !s.Trim().StartsWith("000") && s.Trim().Length > 0).Select(s=>s.Trim().ToLower()));

                        usedWords.AddRange(line.Split(' ', ',', ':', '\'', ';', '"', '.', ')', ']', '}')
                                    .Select(s => s.Trim(TextRuler.Model.DataProvider.WordStartingSymbols).Trim())//('Ⓘ', '✓', '▶', 'Ⓢ', 'Ⓔ', ' '))
                                    .Where(s => !s.StartsWith("000") && s.Length > 0));
                    }
                    //foreach (string word in usedWords)
                    //{
                    //    if (!DataProvider.UsedWords.Contains(word))
                    //    {
                    //        DataProvider.Add(word);
                    //    }
                    //}
                    DataProvider.Add(usedWords, true);
                });
                treeView1.Sort();
               
                treeView1.EndUpdate();

                // Select the suitable node
                treeView1.AfterSelect += treeView1_AfterSelect;


                if (selectedNodeParent != null)
                {
                    TreeNode node = FindNodeFuzzy(selectedNodeBeforeSave.Text, selectedNodeParent);
                    if (node != null)
                    {
                        treeView1.SelectedNode = node;
                    }
                    else
                    {
                        bool found = false;
                        while (queueSelectedNodes.Count > 0)
                        {
                            TreeNode queueNode = queueSelectedNodes.Dequeue();
                            // If this node has not been removed fromt the tree
                            if (queueNode.Parent != null)
                            {
                                treeView1.SelectedNode = queueNode;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            treeView1.SelectedNode = selectedNodeParent;
                        }
                    }
                }
            }
            catch { }
        }

        private string GetDocumentNameWithPathRelativeToTrivial(string file)
        {
            string trivialPath = Path.GetDirectoryName(Application.ExecutablePath).ToLower();
            file = file.ToLower();
            int index = file.IndexOf(trivialPath);
            if (index == 0)
            {
                return file.Substring(trivialPath.Length, file.Length - trivialPath.Length)
                    .TrimStart('\\');
            }
            return file;
        }

        private static TreeNode FindNodeFuzzy(string selectedNodeBeforeSaveText, TreeNode selectedNodeParent)
        {
            TreeNode node = selectedNodeParent.Nodes.Find(selectedNodeBeforeSaveText, false).FirstOrDefault();
            if (node == null)
            {
                string nodeName = selectedNodeBeforeSaveText.Substring(0, selectedNodeBeforeSaveText.Length / 2);
                foreach (TreeNode treeNode in selectedNodeParent.Nodes)
                {
                    if (treeNode.Text.StartsWith(nodeName))
                    {
                        node = treeNode;
                        break;
                    }
                }
            }
            return node;
        }

        private void UpdateDiaryCollection(DateTime date)
        {
            //TODO: Deal with false (else) condition
            if (!_DictDairyNodes.ContainsKey(date.Date))
            {
                //Get the year node
                TreeNode diaryNode = treeView1.Nodes.Find(DIARIES_NODE_KEY, false).FirstOrDefault();
                TreeNode yeaN = diaryNode.Nodes.Find(date.Year.ToString(), false).FirstOrDefault();
                //TODO: Deal with else condition
                if (yeaN != null)
                {
                    TreeNode monthN = yeaN.Nodes.Find(date.Month.ToString(), false).FirstOrDefault();
                    //TODO: deal with else conditiom
                    if (monthN != null)
                    {
                        TreeNode[] nodesArray = new TreeNode[monthN.Nodes.Count];
                        monthN.Nodes.CopyTo(nodesArray, 0);
                        var dd = nodesArray.Select(s => DateTime.Parse(s.Text))
                            .OrderByDescending(s => s).FirstOrDefault(s => s < date);
                        //TODO: Deal with else condition
                        if (dd != null)
                        {
                            TreeNode dayN = monthN.Nodes.Find(dd.ToString("yyyy-MM-dd"), false).FirstOrDefault();
                            //TODO: Deal with else condition
                            if (dayN != null)
                            {
                                TreeNode node = monthN.Nodes.Insert(dayN.Index, date.ToString("yyyy-MM-dd"), date.ToString(("yyyy-MM-dd")));
                                _DictDairyNodes.Add(date, node);
                                node.Tag = date;
                            }
                        }
                    }
                }
            }
        }

        private void ClearInputNodes(DayData data, string nodeText)
        {
            TreeNode[] nodes = treeView1.Nodes.Find(nodeText, false);
            if (nodes.Count() > 0)
            {
                List<TreeNode> listNodesToRemove = new List<TreeNode>();
                foreach (TreeNode itNode in nodes[0].Nodes)
                {
                    DayData dayData = itNode.Tag as DayData;

                    if (dayData != null /*&& dayData.Date == data.Date*/)
                    {
                        foreach(TreeNode actionNode in itNode.Nodes)
                        {
                            if (actionNode.Tag != null 
                                && actionNode.Tag is DayData
                                && (actionNode.Tag as DayData).Date == data.Date)
                            {
                                listNodesToRemove.Add(actionNode);
                            }
                        }
                    }
                }

                foreach (TreeNode ntr in listNodesToRemove)
                {
                    nodes[0].Nodes.Remove(ntr);
                }
            }
        }

        private async void advancedTextEditor1_Load(object sender, EventArgs e)
        {
            await LoadData(Travails.Model.DataProvider.DataBuildMode.None);
        }

        private async Task LoadData(Travails.Model.DataProvider.DataBuildMode mode)
        {
            ClearData();
            if (mode == DataProvider.DataBuildMode.Rebuild) DB.Clear();
            LoadDiaries(mode);
            LoadKnowledgeBase(mode);
            await Today();
        }

        private async Task Today()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (await advancedTextEditor1.OpenAsync(GetPath(string.Format("{0}.rtf", today)), DateTime.Today))
            {
                TreeNode diaryNode = treeView1.Nodes.Find(DIARIES_NODE_KEY, false).FirstOrDefault();
                if (diaryNode != null)
                {
                    TreeNode node = diaryNode.Nodes.Find(today, true).FirstOrDefault();
                    if (node != null)
                    {
                        treeView1.SelectedNode = node;
                        if (!node.IsVisible) node.EnsureVisible();
                        _CurrentSelectedNode = treeView1.SelectedNode;
                    }
                }
            }
        }

        private string GetPath(string v)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), v);
        }

        private async void LoadKnowledgeBase(DataProvider.DataBuildMode mode)
        {
            if (!Directory.Exists(KNOWLEDGE_BASE_FOLDER)) Directory.CreateDirectory(KNOWLEDGE_BASE_FOLDER);
            string[] files = System.IO.Directory.GetFiles(KNOWLEDGE_BASE_FOLDER, "*.rtf", System.IO.SearchOption.AllDirectories);
            TreeNode knowledgeBaseNode = treeView1.Nodes.Add(KNOWLEDGE_BASE_NODE_KEY, KNOWLEDGE_BASE_NODE_KEY);
            toolStripProgressBar2.Maximum = files.Count();
            int index = 0;
            foreach (string file in files)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                string directoryName = Path.GetDirectoryName(file);
                TreeNode node = EnsureDirectoryNodeExists(directoryName);
                if (node != null)
                {
                    TreeNode docNode = node.Nodes.Add(file.ToLower(), fileName);
                    
                    docNode.Tag = "Document";
                    DayData dayData = await DataProvider.GetDayData(DateTime.Today, file, mode);
                    if (dayData != null)
                    {
                    }
                    toolStripProgressBar2.Value = ++index;
                    toolStripStatusLabel2.Text = string.Format("{0} of {1} Processing {2}", toolStripProgressBar2.Value, toolStripProgressBar2.Maximum, file);
                }
            }

            DataProvider.CompactWordList();

            toolStripProgressBar2.Value = files.Count();
            toolStripStatusLabel2.Text = "All articles processed.";
        }

        private TreeNode EnsureDirectoryNodeExists(string directoryName)
        {
            string[] names = directoryName.Split('\\').Skip(2).ToArray();
            TreeNode knowledgeBaseNode = treeView1.Nodes.Find(KNOWLEDGE_BASE_NODE_KEY, false).FirstOrDefault();
            if (knowledgeBaseNode == null) return null;
            TreeNode leafNode = knowledgeBaseNode;
            TreeNodeCollection rootNodeCollection = knowledgeBaseNode.Nodes;
            foreach (string nodeName in names)
            {
                string rootNodeName = nodeName;
                TreeNode rootNode = rootNodeCollection.Find(rootNodeName, false).FirstOrDefault();
                if (rootNode == null)
                {
                    rootNode = rootNodeCollection.Add(rootNodeName, rootNodeName);
                    rootNode.Tag = "Folder";
                }
                rootNodeCollection = rootNode.Nodes;
                leafNode = rootNode;
            }
            return leafNode;
        }


        private void ClearData()
        {
            treeView1.Nodes.Clear();
            _DictDairyNodes.Clear();
            _DictNamedNodes.Clear();
            _DictTracks.Clear();
            _ListVisitedNodes.Clear();
            Travails.Model.DataProvider.Clear();
        }

        private async void LoadDiaries(Travails.Model.DataProvider.DataBuildMode mode)
        {
            treeView1.AfterSelect -= treeView1_AfterSelect;
            treeView1.AfterSelect += treeView1_AfterSelect;
            //treeView1.BeginUpdate();
            TreeNode diaryNode = treeView1.Nodes.Add(DIARIES_NODE_KEY, DIARIES_NODE_KEY);
            diaryNode.Tag = DIARIES_NODE_KEY;
            IEnumerable<DateTime> dates = await DataProvider.GetAllDiaries();
            toolStripProgressBar1.Maximum = dates.Count();
            dates = dates.OrderByDescending(s => s);
            var groups = dates
                .GroupBy(s=>s.Year)
                .Select(s=> new {Year = s.Key, MonthDays = s.GroupBy(m=>m.Month).Select(d=>new {Month = d.Key, Days = d})});
            int index = 0;
            foreach(var yearGroup in groups)
            {
                TreeNode yearNode = diaryNode.Nodes.Add(yearGroup.Year.ToString(), yearGroup.Year.ToString());
                yearNode.Tag = YEAR_NODE;

                foreach (var monthDays in yearGroup.MonthDays)
                {
                    TreeNode monthNode = yearNode.Nodes.Add(monthDays.Month.ToString(), monthDays.Month.ToString());
                    monthNode.Tag = MONTH_NODE;
                    foreach (var date in monthDays.Days)
                    {
                        TreeNode dateNode =
                            monthNode.Nodes.Add(date.ToString("yyyy-MM-dd"), date.ToString("yyyy-MM-dd"));
                        dateNode.Tag = date;
                        DayData dayData = await DataProvider.GetDayData(date, null, mode);
                        if (dayData != null)
                        {
                            dateNode.Tag = dayData;
                            DataProvider.AddDayData(dayData);
                            PopulateNode(dateNode, dayData);

                            if (!_DictDairyNodes.ContainsKey(date.Date))
                            {
                                _DictDairyNodes.Add(date.Date, dateNode);
                            }
                        }
                        toolStripProgressBar1.Value = ++index;
                        toolStripStatusLabel1.Text = string.Format("{0} of {1} Processing {2}", toolStripProgressBar1.Value, toolStripProgressBar1.Maximum, date);
                    }
                }

                DataProvider.CompactWordList();

                toolStripProgressBar1.Value = dates.Count();
                toolStripStatusLabel1.Text = "All diaries processed.";
            }

            treeView1.Sort();
            treeView1.EndUpdate();
        }

        private void PopulateNode(TreeNode node, DayData dayData)
        {
            if (dayData != null && dayData.Actions != null)
            {
                TreeNode inputToNode = CreateInputsNode(INPUTS_TO);
                TreeNode inputFromNode = CreateInputsNode(INPUTS_FROM);
                TreeNode tracksNode = CreateInputsNode(ACTIVE_TRACKS_ROOT_NODE);
                
                TreeNode workNode = node.Nodes.Add(WORK_LOG);

                PopulateWorkLog(workNode, dayData.WorkList.OrderBy(s=>s.From));

                if (_DictTracks.ContainsKey(dayData.Date))
                {
                    foreach(TreeNode trackActionNode in _DictTracks[dayData.Date].ToArray())
                    {
                        _DictTracks[dayData.Date].Remove(trackActionNode);
                        trackActionNode.Remove();
                    }
                }

                foreach (ActionData aData in dayData.Actions)
                {
                    TreeNode tnode = node.Nodes.Add(aData.Name);
                    //AddInputsPerson(aData, tnode, aData.InputFrom, "Inputs From");
                   // AddInputsPerson(aData, tnode, aData.InputTo, "Inputs To");

                    if (aData.InputTo.Count > 0)
                    {
                        TreeNode ifNode = tnode.Nodes.Add(INPUTS_TO);
                        ProcessSubNodes(inputToNode, aData, aData.InputTo, ifNode);
                    }

                    if (aData.InputFrom.Count > 0)
                    {
                        TreeNode ifNode = tnode.Nodes.Add(INPUTS_FROM);
                        ProcessSubNodes(inputFromNode, aData, aData.InputFrom, ifNode);
                    }

                    ProcessTracks(tracksNode, aData);
                }
            }
        }

        private void PopulateWorkLog(TreeNode workNode, IEnumerable<WorkData> list)
        {
            foreach(WorkData data in list)
            {
                TreeNode node = workNode.Nodes.Add(string.Format(@"{0} - {1} {2}", data.From.ToString("HH:mm"), data.To.ToString("HH:mm"), data.Name));
                node.Tag = data.From;
            }
        }

        private class NodeSorter : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                TreeNode node1 = (TreeNode)x;
                TreeNode node2 = (TreeNode)y;
                if (node1.Level == 1 && node1.Parent.Name == ACTIVE_TRACKS_ROOT_NODE)
                {
                    return Convert.ToDateTime(node2.Tag).CompareTo(Convert.ToDateTime(node1.Tag));
                }
                else if (node1.Level == 2 && node1.Tag is DayData && node2.Tag is DayData)
                {
                    return ((DayData)node2.Tag).Date.CompareTo(((DayData)node1.Tag).Date);
                }
                else
                {
                    return node1.Name.CompareTo(node2.Name);// node1.Index.CompareTo(node2.Index);
                }
            }
        }


        private void ProcessTracks(TreeNode trackNode, ActionData aData)
        {
            if (aData.TrackList.Count() == 0)
            {
                ProcessTrackDictionary(aData.Parent, AddNodeAndChild(trackNode, aData.Parent, "<Unassigned>", aData));
            }
            foreach(string track in aData.TrackList)
            {
                ProcessTrackDictionary(aData.Parent, AddNodeAndChild(trackNode, aData.Parent, track, aData));
            }
        }

        private void ProcessTrackDictionary(DayData dayData, TreeNode treeNode)
        {
            if (!_DictTracks.ContainsKey(dayData.Date))
            {
                _DictTracks.Add(dayData.Date, new List<TreeNode>());
            }
            _DictTracks[dayData.Date].Add(treeNode);
        }

        private TreeNode AddNodeAndChild(TreeNode parent, DayData dayData, string nodeName, ActionData aData)
        {
            TreeNode trackNode = parent.Nodes.Find(nodeName, false).FirstOrDefault();
            if (trackNode == null)
            {
                int index = GetInsertIndex(parent.Nodes, nodeName);
                trackNode = parent.Nodes.Insert(index, nodeName, nodeName);
            }
            if (trackNode.Tag == null || (DateTime)trackNode.Tag < aData.DateLogged)
            {
                // Tag with the latest date
                trackNode.Tag = aData.DateLogged;
            }
            
            string childNodeName = aData.Name;
            
            TreeNode actionNode = trackNode.Nodes.Find(childNodeName, false).FirstOrDefault();
            if (actionNode == null)
            {
                int index = GetInsertIndexForAction(trackNode.Nodes, aData);
                actionNode = trackNode.Nodes.Insert(index, childNodeName, childNodeName);
                actionNode.Tag = dayData;
            }

            TreeNode hashNode = trackNode.Nodes.Find("<TAGS>", false).FirstOrDefault();
            foreach (string tag in aData.HashTags)
            {
                //Temp
                if (tag.StartsWith("0000")) continue;

                if (hashNode == null)
                {
                    hashNode = trackNode.Nodes.Insert(0, "<TAGS>", "<TAGS>");
                }
                TreeNode tagNode = hashNode.Nodes.Find(tag, false).FirstOrDefault();
                if (tagNode == null)
                {
                    int index = GetInsertIndex(hashNode.Nodes, tag);
                    hashNode.Nodes.Insert(index, tag, tag);
                }
                
            }

            return actionNode;
        }

        private static void AddInputsPerson(ActionData aData, TreeNode tnode, List<string> persons, string nodeText)
        {
            if (aData.InputFrom.Count > 0)
            {
                TreeNode ofNode = tnode.Nodes.Add(nodeText);
                foreach (string person in persons.OrderBy(s=>s))
                {
                    ofNode.Nodes.Add(person);
                }
            }
        }

        private TreeNode CreateInputsNode(string name)
        {
            TreeNode[] inputToNodes = treeView1.Nodes.Find(name, false);
            TreeNode inputToNode = null;
            if (inputToNodes.Count() == 0)
            {
                inputToNode = treeView1.Nodes.Add(name, name);
            }
            else
            {
                inputToNode = inputToNodes[0];
            }
            return inputToNode;
        }

        private static void ProcessSubNodes(TreeNode inputToNode, ActionData aData, List<string> persons, TreeNode ifNode)
        {
            foreach (string person in persons.OrderBy(s=>s))
            {
                ifNode.Nodes.Add(person);

                TreeNode[] itNodes = inputToNode.Nodes.Find(person, false);
                TreeNode inputToPerson = null;
                if (itNodes.Count() == 0)
                {
                    int indexToInsert = GetInsertIndex(inputToNode.Nodes, person);
                    //inputToPerson = inputToNode.Nodes.Add(person, person);
                    inputToPerson = inputToNode.Nodes.Insert(indexToInsert, person, person);
                    inputToPerson.Tag = aData.Parent;
                }
                else
                {
                    inputToPerson = itNodes[0];
                }
                int indexToInsertAction = GetInsertIndexForAction(inputToPerson.Nodes, aData);
                TreeNode aNode = inputToPerson.Nodes.Insert(indexToInsertAction, aData.Name, aData.Name);
                aNode.Tag = aData.Parent;
            }
        }

        private static int GetInsertIndexForAction(TreeNodeCollection treeNodeCollection, ActionData aData)
        {
            List<DateTime> list = new List<DateTime>();
            foreach (TreeNode node in treeNodeCollection)
            {
                if (node.Tag == null)
                {
                    list.Add(DateTime.MinValue);
                }
                else
                {
                    list.Add((node.Tag as DayData).Date);
                }
            }
            list.Add(aData.DateLogged);
            return list.OrderBy(s => s).ToList().IndexOf(aData.DateLogged);
        }

        private static int GetInsertIndex(TreeNodeCollection treeNodeCollection, string person)
        {
            List<string> list = new List<string>();
            foreach(TreeNode node in treeNodeCollection)
            {
                list.Add(node.Text);
            }
            list.Add(person);
            return list.OrderBy(s => s).ToList().IndexOf(person);
        }

        TreeNode _CurrentSelectedNode = null;
        async void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

            //if (e.Node == _CurrentSelectedNode) return;
            if (e.Node.Tag is DateTime)
            {
                DateTime date = (DateTime)e.Node.Tag;
                string file = GetPath(string.Format("{0}.rtf", date.ToString("yyyy-MM-dd")));
                if (File.Exists(file))
                {
                    if (await advancedTextEditor1.OpenAsync(file, date) == false)
                    {
                        treeView1.SelectedNode = _CurrentSelectedNode;
                        return;
                    }
                    else
                    {
                        UpdatePrevNextNodeList(e.Node);
                        //advancedTextEditor1.Find(e.Node.Text);
                    }
                }
            }
            if (e.Node.Tag is DayData)
            {
                string file = GetPath(string.Format("{0}.rtf", (e.Node.Tag as DayData).Date.ToString("yyyy-MM-dd")));
                if (File.Exists(file))
                {
                    if (await advancedTextEditor1.OpenAsync(file, (e.Node.Tag as DayData).Date))
                    {
                        UpdatePrevNextNodeList(e.Node);
                        advancedTextEditor1.Find(e.Node.Text);
                    }
                    else
                    {
                        treeView1.SelectedNode = _CurrentSelectedNode;
                        return;
                    }
                }
            }
            if (e.Node.Tag == null)
            {
                if (e.Node.Parent != null && e.Node.Parent.Tag is DateTime)
                {
                    DateTime date = (DateTime)e.Node.Parent.Tag;
                    string file = GetPath(string.Format("{0}.rtf", date.ToString("yyyy-MM-dd")));
                    if (File.Exists(file))
                    {
                        if (await advancedTextEditor1.OpenAsync(file, date))
                        {
                            UpdatePrevNextNodeList(e.Node);

                            //advancedTextEditor1.Find(e.Node.Text);
                        }
                        else
                        {
                            treeView1.SelectedNode = _CurrentSelectedNode;
                            return;
                        }
                    }
                }
            }
            if (e.Node.Tag != null && e.Node.Tag.ToString() == "Document")
            {
                string file = GetPath(string.Format("{0}.rtf", GetFullPath(e)));
                if (File.Exists(file))
                {
                    if (await advancedTextEditor1.OpenAsync(file, DateTime.Today) == false)
                    {
                        treeView1.SelectedNode = _CurrentSelectedNode;
                        return;
                    }
                    else
                    {
                        UpdatePrevNextNodeList(e.Node);
                    }
                }
            }

            _CurrentSelectedNode = e.Node;
            _PrevNextSelectionInProgress = false;
        }


        

        private string GetFullPath(TreeViewEventArgs e)
        {
            string dir = e.Node.Text;
            TreeNode node = e.Node;
            while (node.Parent != null)
            {
                dir = node.Parent.Text + '\\' + dir;
                node = node.Parent;
            }
            return dir;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (advancedTextEditor1.CheckSave())
            {
                advancedTextEditor1.ComDispose();
            }
            else
            {
                e.Cancel = true;
            }
            //Ball.Bouncer.Hide();
        }

        

        private void contextMenuStripTree_Opening(object sender, CancelEventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;

            if (node == null) return;

            reloadDiariesToolStripMenuItem.Visible = false;
            addFolderToolStripMenuItem.Visible = false;
            addDocumentToolStripMenuItem.Visible = false;
            openFolderToolStripMenuItem.Visible = false;
            extractWorklogToolStripMenuItem.Visible = false;

            while (node.Parent != null) node = node.Parent;
            //if (node.Text != KNOWLEDGE_BASE_NODE_KEY) e.Cancel = true;
            if (treeView1.SelectedNode.Text == KNOWLEDGE_BASE_NODE_KEY)
            {
                addFolderToolStripMenuItem.Visible = true;
                return;
            }
            if (treeView1.SelectedNode.Tag != null)
            {
                if (treeView1.SelectedNode.Tag.ToString() == DIARIES_NODE_KEY)
                {
                    reloadDiariesToolStripMenuItem.Visible = true;
                    return;
                }
                if (treeView1.SelectedNode.Tag.ToString() == "Folder")
                {
                    addDocumentToolStripMenuItem.Visible = true;
                    return;
                }
                if (treeView1.SelectedNode.Tag.ToString() == MONTH_NODE)
                {
                    extractWorklogToolStripMenuItem.Visible = true;
                    return;
                }
                if (treeView1.SelectedNode.Tag.ToString() == "Document")
                {
                    e.Cancel = true;
                }
            }
            e.Cancel = true;
        }

        private void addDocumentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;
            TreeNode node = treeView1.SelectedNode.Nodes.Add("New Document");
            node.Tag = "Document";
            treeView1.LabelEdit = true;
            if (!node.IsEditing)
            {
                node.BeginEdit();
            }
            
        }

        private void treeView1_AfterLabelEdit(object sender,
         System.Windows.Forms.NodeLabelEditEventArgs e)
        {
            if (e.Label != null)
            {
                if (e.Label.Length > 0)
                {
                    if (e.Label.IndexOfAny(new char[] { '@', '.', ',', '!' }) == -1)
                    {
                        

                        // Stop editing without canceling the label change.
                        e.Node.EndEdit(false);
                        e.CancelEdit = false;

                        //http://stackoverflow.com/questions/10364580/getting-treenode-text-after-an-edit
                        try
                        {
                            if (this.InvokeRequired)
                            {
                                this.BeginInvoke(new Action(() => afterAfterEdit(e.Node)));
                            }
                            else
                            {
                                afterAfterEdit(e.Node);
                            }
                        }
                        catch { }

                    }
                    else
                    {
                        /* Cancel the label edit action, inform the user, and 
                           place the node in edit mode again. */
                        e.CancelEdit = true;
                        MessageBox.Show("Invalid tree node label.\n" +
                           "The invalid characters are: '@','.', ',', '!'",
                           "Node Label Edit");
                        e.Node.BeginEdit();
                    }
                }
                else
                {
                    /* Cancel the label edit action, inform the user, and 
                       place the node in edit mode again. */
                    e.CancelEdit = true;
                    MessageBox.Show("Invalid tree node label.\nThe label cannot be blank",
                       "Node Label Edit");
                    e.Node.BeginEdit();
                }
            }
            else
            {
                try
                {
                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(() => afterAfterEdit(e.Node)));
                    }
                    else
                    {
                        afterAfterEdit(e.Node);
                    }
                }
                catch { }
            }
        }

        private void afterAfterEdit(TreeNode node)
        {
            //Check for duplicates
            if (node.Parent != null)
            {
                var nodes = node.Parent.Nodes;
                foreach (TreeNode child in nodes)
                {
                    if (child != node && child.Text == node.Text)
                    {
                        MessageBox.Show("Document name must be unique in this group",
                       "Node Label Edit");
                        if (!node.IsEditing)
                        {
                            node.BeginEdit();
                        }
                        return;
                    }
                }
            }
            CreateDocument(node);
            treeView1.SelectedNode = node;
        }

        private void CreateDocument(TreeNode treeNode)
        {
            if (treeNode != null && treeNode.Parent != null)
            {
                string fileName = treeNode.Text;
                string directoryName = treeNode.Parent.Text;
                treeNode = treeNode.Parent;
                while(treeNode.Parent != null)
                {
                    directoryName = treeNode.Parent.Text + '\\' + directoryName;
                    treeNode = treeNode.Parent;
                }
                directoryName = ".\\" + directoryName;

                if (Directory.Exists(directoryName))
                {
                    RichTextBoxEx rtb = new RichTextBoxEx();
                    rtb.SaveFile(string.Format("{0}\\{1}.rtf", directoryName, fileName));
                }
            }
        }

        

        private void addFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string directory = Path.GetDirectoryName(this.GetType().Assembly.Location);
            string directoryToOpen = Path.Combine(directory + "\\", GetPath(treeView1.SelectedNode));
            Process.Start(directoryToOpen);
        }

        private string GetPath(TreeNode treeNode)
        {
            string path = string.Empty;

            while(treeNode != null && treeNode.Parent != null)
            {
                path = treeNode.Text + "\\"+ path;
                treeNode = treeNode.Parent;
            }

            path = treeNode.Text + "\\" + path;

            return path;
        }

        //private string GetParentPath(TreeNode treeNode)
        //{
        //    string path = string.Empty;
        //    TreeNode node = treeNode.Parent;

        //    if (node == null)
        //    {
        //        return path;
        //    }
        //    while(node != null && node.Parent != null)
        //    {
        //        path = node.Text + "\\"+ path;
        //        node = node.Parent;
        //    }

        //    path = node.Text + "\\" + path;

        //    return path;
        //}

        private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string directory = Path.GetDirectoryName(this.GetType().Assembly.Location);
            TreeNode node = treeView1.SelectedNode;
            if (node == null) return;

            string fileToOpen = string.Empty;
            if (node.Tag is DateTime || node.Tag is DayData)
            {
                //TreeNode diaryNode = treeView1.Nodes.Find(DIARIES_NODE_KEY, false).FirstOrDefault();
                //fileToOpen = Path.Combine(directory + "\\", /*GetPath(diaryNode) + */treeView1.SelectedNode.Text + ".rtf");
                fileToOpen = Path.Combine(directory, GetDateString(node.Tag) + ".rtf");
            }
            else
            {
                fileToOpen = Path.Combine(directory + "\\", GetPath(node).TrimEnd('\\') + ".rtf");
            }
            if (File.Exists(fileToOpen))
            {
                Process.Start(fileToOpen);
            }
        }

        //https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx
        private string GetDateString(object p)
        {
            if (p is DateTime)
            {
                return ((DateTime)p).ToString("yyyy-MM-dd");
            }
            if (p is DayData)
            {
                return (p as DayData).Date.ToString("yyyy-MM-dd");
            }
            return null;
        }

       
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //List<ResultLines> list = DataProvider.GetLinesWithHashTag("#PDH").ToList();
             Monitor monitor = new Monitor();
            //monitor.DataProvider = DataProvider.DataProviderInstance;
             if (monitor.Visible)
            {
                monitor.Close();
                monitor = null;
            }
            else
            {
                monitor.Show();
            }
        }

        private async void todayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            advancedTextEditor1.Clear();
            await Today();
        }
        //ToolTip _Tooltip = new ToolTip();
        private void treeView1_NodeMouseHover(object sender, TreeNodeMouseHoverEventArgs e)
        {
            //_Tooltip.ToolTipTitle = "Some thing";
            //_Tooltip.
            ////if (!string.IsNullOrEmpty(e.Node.ToolTipText))
            //if (!string.IsNullOrEmpty(e.Node.Text))
            //{
            //    _Tooltip.Show(e.Node.Text, treeView1);
            //}
        }

        private async void reloadDiariesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await LoadData(Travails.Model.DataProvider.DataBuildMode.Rebuild);
        }

        private void extractWorklogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            

        }

        private void hoursPerDayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            TreeNode yearNode = treeView1.SelectedNode;
            if (yearNode != null)
            {
                foreach (TreeNode dayNode in yearNode.Nodes)
                {
                    DayData data = dayNode.Tag as DayData;
                    if (data != null)
                    {
                        var tracks = data.Tracks;

                        double duration = tracks.Where(s => s.Item1 != "Lunch").Sum(s => s.Item2);
                        long gaps = data
                            .WorkList.OrderBy(s => s.From).Zip(data.WorkList.Skip(1), (a, b) => Tuple.Create(a, b)).Sum(s => s.Item2.From.Subtract(s.Item1.To).Ticks);
                        duration += TimeSpan.FromTicks(gaps).TotalHours;
                        var lunchTrack = tracks.Where(s => s.Item1 == "Lunch").FirstOrDefault();
                        //double duration = data.StartEnd.Item2.Subtract(data.StartEnd.Item1).TotalHours - 1;// One hour for lunch
                        if (duration < 0) duration = 0;
                        sb.AppendFormat("{0:dd/MM/yyyy}\t{1:HH:mm}\t{2:HH:mm}\t{3:0.00}\tLunch\t{4:0.00}\t{5}\n",
                            data.Date,
                            data.StartEnd.Item1,
                            data.StartEnd.Item2,
                            duration,
                            lunchTrack != null ? lunchTrack.Item2 : 0.00,
                            tracks
                            .Select(s=>s.Item1)
                            .Where(s=>s != "<None>")
                            .Aggregate("", (a, b) => string.Format("{0}, {1}", a, b).TrimStart(','))
                            );
                    }
                }
            }

            Clipboard.SetDataObject(sb.ToString());
        }

        private void hoursPerPerDayPertrackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            TreeNode yearNode = treeView1.SelectedNode;
            if (yearNode != null)
            {
                foreach (TreeNode dayNode in yearNode.Nodes)
                {
                    DayData data = dayNode.Tag as DayData;
                    if (data != null)
                    {
                        var tracks = data.Tracks;
                        foreach (var track in tracks)
                        {
                            sb.AppendFormat("{0:dd/MM/yyyy}\t{1}\t{2:0.00}\n",
                            data.Date,
                            track.Item1,
                            track.Item2);
                        }
                        long gaps = data
                            .WorkList.OrderBy(s => s.From).Zip(data.WorkList.Skip(1), (a, b) => Tuple.Create(a, b)).Sum(s => s.Item2.From.Subtract(s.Item1.To).Ticks);
                        double gapHours = TimeSpan.FromTicks(gaps).TotalHours;
                        if (gapHours > 0)
                        {
                            sb.AppendFormat("{0:dd/MM/yyyy}\t<Misc.>\t{1:0.00}\n", data.Date, gapHours);
                        }
                    }
                }
            }

            Clipboard.SetDataObject(sb.ToString());
        }

        private void rebuildDataToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            SetFormTitle(true);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F5)
            {
                SetFormTitle(true);
            }
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
