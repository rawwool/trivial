using Microsoft.Office.Interop.Outlook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace TextRuler.AdvancedTextEditorControl
{
    public class OutlookManager
    {
        Microsoft.Office.Interop.Outlook.Application _oApplication = null;
        Microsoft.Office.Interop.Outlook.Items _oItems = null;
        Microsoft.Office.Interop.Outlook.ItemsEvents_ItemChangeEventHandler _oHandler = null;
        public delegate void HandleMailItemFalgged(object sender, MailItemEventArgs e);
        public event HandleMailItemFalgged MailItemFlagged;

        void items_ItemChange(object item)
        {
            //MessageBox.Show("changed");
            Microsoft.Office.Interop.Outlook.MailItem mailItem = item as Microsoft.Office.Interop.Outlook.MailItem;

            if (mailItem != null)
            {
                //if (mailItem.FlagStatus == OlFlagStatus.olFlagMarked)
                if (mailItem.IsMarkedAsTask)
                {
                    MessageBox.Show("Follow up");
                }
            }
        }
        public Microsoft.Office.Interop.Outlook.Application Application
        {
            get
            {
                if (_oApplication == null)
                {
                    _oApplication = new Microsoft.Office.Interop.Outlook.Application();
                }

                return _oApplication;
            }
        }

        public void WatchMailItemChanged()
        {
            Microsoft.Office.Interop.Outlook._NameSpace ns = Application.GetNamespace("MAPI");
            var inbox = ns.GetDefaultFolder(Microsoft.Office.Interop.Outlook.OlDefaultFolders.olFolderInbox);
            _oItems = inbox.Items;
            _oHandler = new Microsoft.Office.Interop.Outlook.ItemsEvents_ItemChangeEventHandler(items_ItemChange);
            _oItems.ItemChange += _oHandler;
        }

        public void ComDispose()
        {
            if (_oItems != null)
            {
                _oItems.ItemChange -= _oHandler;
                _oItems = null;
            }
            //_oApplication.Quit(); Closes desktop Outlook!
            _oApplication = null;

        }

        public IEnumerable<string> GetToDoItems()
        {
            // Obtain Inbox
            Outlook.Folder folder =
                Application.Session.GetDefaultFolder(
                Outlook.OlDefaultFolders.olFolderInbox)
                as Outlook.Folder;
            // DASL filter for IsMarkedAsTask
            string filter = "@SQL=" + "\"" +
                "http://schemas.microsoft.com/mapi/proptag/0x0E2B0003"
                + "\"" + " = 1";
            Outlook.Table table =
                folder.GetTable(filter,
                Outlook.OlTableContents.olUserItems);
            table.Columns.Add("TaskStartDate");
            table.Columns.Add("TaskDueDate");
            table.Columns.Add("TaskCompletedDate");
            // Use GUID/ID to represent TaskSubject
            table.Columns.Add(
                "http://schemas.microsoft.com/mapi/id/" +
                "{00062008-0000-0000-C000-000000000046}/85A4001E");
            while (!table.EndOfTable)
            {
                Outlook.Row nextRow = table.GetNextRow();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Task Subject: " + nextRow[9]);
                sb.AppendLine("Start Date: "
                    + nextRow["TaskStartDate"]);
                sb.AppendLine("Due Date: "
                    + nextRow["TaskDueDate"]);
                sb.AppendLine("Completed Date: "
                    + nextRow["TaskCompletedDate"]);
                sb.AppendLine("Id: "
                    + nextRow["EntryID"]);
                sb.AppendLine();
                //Debug.WriteLine(sb.ToString());
                yield return  sb.ToString();
            }
        }

        public class Appointment
        {
            public string Subject { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Organiser { get; set; }
            public string Location { get; set; }
        }

        public List<Appointment> GetAppointmentsInRange(DateTime from, DateTime to)
        {
            List<Appointment> list = new List<Appointment>();
            try
            {
                Outlook.Folder calFolder =
                    Application.Session.GetDefaultFolder(
                    Outlook.OlDefaultFolders.olFolderCalendar)
                    as Outlook.Folder;
                DateTime start = from;
                DateTime end = to;
                Outlook.Items rangeAppts = GetAppointmentsInRange(calFolder, start, end);
                if (rangeAppts != null)
                {
                    foreach (Outlook.AppointmentItem appt in rangeAppts)
                    {
                        Debug.WriteLine("Subject: " + appt.Subject
                            + " Start: " + appt.Start.ToString("g"));

                        list.Add(new Appointment()
                        {
                            Subject = appt.Subject,
                            Start = appt.Start,
                            End = appt.End,
                            Organiser = appt.Organizer,
                            Location = appt.Location
                        });
                    }
                }
                return list;
            }
            catch
            {
                return list;
            }
        }

        /// <summary>
        /// Get recurring appointments in date range.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns>Outlook.Items</returns>
        private Outlook.Items GetAppointmentsInRange(
            Outlook.Folder folder, DateTime startTime, DateTime endTime)
        {
            string filter = "[Start] >= '"
                + startTime.ToString("g")
                + "' AND [End] <= '"
                + endTime.ToString("g") + "'";
            Debug.WriteLine(filter);
            try
            {
                Outlook.Items calItems = folder.Items;
                calItems.IncludeRecurrences = true;
                calItems.Sort("[Start]", Type.Missing);
                Outlook.Items restrictItems = calItems.Restrict(filter);
                if (restrictItems.Count > 0)
                {
                    return restrictItems;
                }
                else
                {
                    return null;
                }
            }
            catch { return null; }
        }
    }
}
