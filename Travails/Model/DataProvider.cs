using Ionic.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrivialModel;
using TextRuler;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using TrivialModel.Model;
using TrivialData;
using System.Threading;

namespace Travails.Model
{
    // A delegate type for hooking up change notifications.
    public delegate bool ShowEventHandler(object sender, ActionEventArgs e);

    public class MemorySize
    {
        static readonly string[] SizeSuffixes =
                       { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }

    public class DataProvider
    {
        private static List<string> _UsedWords = new List<string>();
        private static List<string> _UsedWordsCompacted = new List<string>();
        internal static Dictionary<DateTime, DayData> _DictDayData = new Dictionary<DateTime, DayData>();
        private static DataProvider _DataProvider;
        private static Dictionary<int, Tuple<List<Tag>, ActionData>> _DictThreadContext = new Dictionary<int, Tuple<List<Tag>, ActionData>>();
        internal static Dictionary<string, List<NGramWord>> _DictNGram = new Dictionary<string, List<NGramWord>>();
        internal static event ShowEventHandler ShowRequested;

        internal static void Clear()
        {
            _UsedWords.Clear();
            _UsedWordsCompacted.Clear();
            _DictDayData.Clear();
            _DictThreadContext.Clear();
            _DictNGram.Clear();
        }

        internal static IEnumerable<string> UsedWords
        {
            get
            {
                return _UsedWordsCompacted;
            }
        }
        public static DataProvider DataProviderInstance
        {
            get { if (_DataProvider == null) _DataProvider = new DataProvider(); return _DataProvider; }
        }



        internal static void CompactWordList()
        {
            try
            {
                _UsedWordsCompacted = _UsedWords
                        .Select(s => s.TrimStart(TextRuler.Model.DataProvider.WordStartingSymbols))
                        .GroupBy(s => s)
                        .Select(s => new { Word = s.GroupBy(p => p).OrderByDescending(p => p.Count()).First().Key, Count = s.Count() })
                        .OrderByDescending(s => s.Count)
                        .Select(s => s.Word)
                        //.Select(s => s.Word.TrimStart('Ⓘ', '✓', '▶', 'Ⓢ', 'Ⓔ', '>', '<'))
                        .Distinct()//Why after grouping?
                        .ToList();
            }
            catch { }
        }

        internal static void AddDayData(DayData dayData)
        {
            if (_DictDayData.ContainsKey(dayData.Date))
            {
                _DictDayData[dayData.Date] = dayData;
            }
            else
            {
                _DictDayData.Add(dayData.Date, dayData);
            }
        }

        public static ActionData[] GetActions(int count)
        {
            var actions = _DictDayData.Values.SelectMany(s => s.Actions);
            return actions.OrderBy(s => s.DateDue).Take(count).ToArray();

        }
        public static List<ActionData> GetFromActions(string actionSubject)
        {
            return _DictDayData
                .Values.
                SelectMany(s => s.Actions)
                .Where(s => s.InputFrom.Contains(actionSubject))
                .OrderByDescending(s=>s.DateDue)
                .ToList();
        }

        public static List<ActionData> GetToActions(string actionSubject)
        {
            return _DictDayData
                .Values.
                SelectMany(s => s.Actions)
                .OrderByDescending(s => s.DateDue)
                .Where(s => s.InputTo.Contains(actionSubject))
                .ToList();
        }

        public static List<ActionData> GetActions(DateTime fromDate, DateTime toDate)
        {
            return _DictDayData
                .Values.
                SelectMany(s => s.Actions)
                .Where(s => s.DateDue >= fromDate && s.DateDue <= toDate)
                .OrderBy(s => s.DateDue)
                .ToList();
        }

        public static ActionData[] GetFutureActions(int count)
        {
            var actions = _DictDayData.Values.SelectMany(s => s.Actions).Where(s => s.DateDue > DateTime.Now.AddDays(-7));
            return actions.OrderBy(s => s.DateDue).Take(count).ToArray();
        }

        public static async Task<ActionData[]> GetFutureActionsAsync(int count)
        {
            //var duedates = DictDayData.Values.SelectMany(s => s.Actions.Select(d=>d.DateDue));
            var actions = await Task.Run(() => _DictDayData.Values.SelectMany(s => s.Actions).Where(s => s.DateDue > DateTime.Now.AddDays(-28)));
            return actions.OrderBy(s => s.DateDue).Take(count).ToArray();
        }

        public static bool ShowAction(DateTime date, string name)
        {
            if (ShowRequested != null)
            {
                return ShowRequested(null, new ActionEventArgs() { Date = date, Name = name });
            }

            return false;
        }

        public static bool ShowLinkedDocument(string fileName, int lineIndex)
        {
            if (ShowRequested != null)
            {
                return ShowRequested(null, new ActionEventArgs() { FileName = fileName, LineIndex = lineIndex });
            }

            return false;
        }

        internal static void Add(string word)
        {
            _UsedWords.Add(word);
            CompactWordList();
        }

        internal static void Add(IEnumerable<string> words, bool compact)
        {
            foreach (string word in words)
            {
                //if (!_UsedWords.Contains(word))
                if (word != null)
                    _UsedWords.Add(word);
            }
            if (compact)
                CompactWordList();
        }

        //internal static void AddRangeWithoutCompacting(IEnumerable<string> enumerable)
        //{
        //    _UsedWords.AddRange(enumerable);
        //    //CompactWordList();
        //}


        public static IEnumerable<ResultLines> GetLinesWithHashTag(string hashTag)
        {
            string[] files = System.IO.Directory.GetFiles(".", "*.rtf", System.IO.SearchOption.AllDirectories);
            List<ResultLines> list = new List<ResultLines>();
            foreach (string file in files)
            {
                var lines = GetLinesWithHashTagEx(file, Recover(hashTag)).ToList();
                if (lines.Count > 0)
                {
                    list.Add(new ResultLines() { Name = Path.GetFileNameWithoutExtension(file), Lines = lines });
                }
            }

            return list;
        }

        private static string Sanitise(string title)
        {
            return title.Replace('/', '֎').Replace('#', 'Φ');
        }

        private static string Recover(string title)
        {
            return title.Replace('֎', '/').Replace('Φ', '#');
        }

        private static IEnumerable<string> GetLinesWithHashTag(string file, string hashTag)
        {
            List<string> list = new List<string>();
            try
            {
                if (File.Exists(file))
                {
                    using (RichTextBoxExx rtb = new RichTextBoxExx())
                    {
                        rtb.LoadFile(file);

                        foreach (string line in /*rtb.Lines*/rtb.RealLines)
                        {
                            //if (line.Contains(hashTag))
                            if (Regex.IsMatch(line, hashTag, RegexOptions.IgnoreCase))
                            {
                                list.Add(line);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                list.Add(ex.Message);
            }

            return list;
        }

        private static IEnumerable<string> GetLinesWithHashTagEx(string file, string hashTag)
        {
            List<string> list = new List<string>();
            try
            {
                if (File.Exists(file))
                {
                    using (RichTextBoxExx rtb = new RichTextBoxExx())
                    {
                        rtb.LoadFile(file);

                        if (Regex.IsMatch(rtb.Text, hashTag, RegexOptions.IgnoreCase))
                        {
                            DateTime date;

                            if (DateTime.TryParse(System.IO.Path.GetFileNameWithoutExtension(file), out date))
                            {
                                DayData dayData = GetDayData(date, null, DataBuildMode.None).Result;
                                var lines = rtb.RealLines.ToArray();
                                string linesString = string.Empty;
                                bool keeper = false;
                                foreach (WorkData wdata in dayData.WorkList)
                                {
                                    keeper = false;
                                    linesString = string.Empty;
                                    for (int i = wdata.FromLine; i < wdata.ToLine; i++)
                                    {
                                        string line = lines[i].TrimEnd();
                                        if (line.Length > 0)
                                        {
                                            linesString += line + '\n';
                                            if (Regex.IsMatch(line, hashTag, RegexOptions.IgnoreCase))
                                            {
                                                keeper = true;
                                            }
                                        }
                                        else
                                        {
                                            if (keeper)
                                            {
                                                list.Add(linesString);
                                                list.Add("---");
                                            }
                                            keeper = false;
                                            linesString = string.Empty;
                                        }

                                    }
                                    if (keeper)
                                    {
                                        list.Add(linesString);
                                        list.Add("***");
                                    }
                                }
                            }
                            else
                            {
                                var lines = rtb.RealLines.ToArray();
                                string linesString = string.Empty;
                                bool keeper = false;
                                foreach (string linewa in lines)
                                {
                                    string line = linewa.TrimEnd();
                                    if (line.Length > 0)
                                    {
                                        linesString += line + '\n';
                                        if (Regex.IsMatch(line, hashTag, RegexOptions.IgnoreCase))
                                        {
                                            keeper = true;
                                        }
                                    }
                                    else
                                    {
                                        if (keeper)
                                        {
                                            list.Add(linesString);
                                            list.Add("---");
                                            keeper = false;
                                        }
                                    }
                                }
                                if (keeper)
                                {
                                    list.Add(linesString);
                                    list.Add("***");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                list.Add(ex.Message);
            }

            return list;
        }

        internal static async Task<IEnumerable<DateTime>> GetAllDiaries()
        {
            string[] files = await Task.Run(() => System.IO.Directory.GetFiles(".", "*.rtf", System.IO.SearchOption.TopDirectoryOnly));
            List<DateTime> list = new List<DateTime>();
            DateTime date;
            //Parallel.ForEach(files, s =>
            //{
            //    if (DateTime.TryParse(System.IO.Path.GetFileNameWithoutExtension(s), out date))
            //    {
            //        list.Add(date);
            //    }
            //});
            foreach (string file in files)
            {
                if (DateTime.TryParse(System.IO.Path.GetFileNameWithoutExtension(file), out date))
                {
                    list.Add(date);
                }
            }
            return list.OrderByDescending(s => s);
        }

        internal static async Task<IEnumerable<string>> GetAllKB()
        {
            string[] files = await Task.Run(() => System.IO.Directory.GetFiles(".", "*.rtf", System.IO.SearchOption.TopDirectoryOnly));
            
            return files.ToList();
        }

        public enum DataBuildMode
        {
            None,
            Rebuild,
            Update
        }

        internal static async Task<DayData> GetDayData(DateTime date, string fileNameWithPath, DataBuildMode dataBuildMode)
        {
            try
            {
                if (string.IsNullOrEmpty(fileNameWithPath) && !File.Exists(fileNameWithPath))
                {
                    if (date != null)
                    {
                        fileNameWithPath = string.Format("{0}.rtf", date.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        return null;
                    }
                }
                DayData data = new DayData();
                List<DayData> list = new List<DayData>();
                if (File.Exists(fileNameWithPath))
                {
                    await Task.Run(() =>
                    {
                        using (RichTextBoxExx rtb = new RichTextBoxExx())
                        {
                            rtb.LoadFile(fileNameWithPath);
                            if (date == null) date = DateTime.Today;
                            data.Date = date;
                            int lineIndex = 0;
                            foreach (string line in /*rtb.Lines*/rtb.RealLines)
                            {
                                ActionData actionData = null;
                                WorkData workData = null;
                                if (TryParse(fileNameWithPath, line, lineIndex, data, dataBuildMode, out actionData, out workData))
                                {
                                    //actionData.Parent = data;
                                    if (actionData != null) data.Actions.Add(actionData);
                                }
                                lineIndex++;
                                ProcessNGrams(line);
                                //(TextRuler.Model.DataProvider.WordStartingSymbols)
                                DataProvider
                                    .Add(line.Split(' ', ',', ':', '\'', ';', '"', '.', ')', ']', '}')
                                    .Select(s => s.Trim(TextRuler.Model.DataProvider.WordStartingSymbols).Trim())//('Ⓘ', '✓', '▶', 'Ⓢ', 'Ⓔ', ' '))
                                    .Where(s => !s.StartsWith("000") && s.Length > 0)
                                    , false);
                            }

                            if (data.WorkList != null)
                            {
                                WorkData wd = data.WorkList.LastOrDefault();
                                if (wd != null)
                                {
                                    if (wd.To == default(DateTime))
                                    {
                                        wd.To = wd.From.AddHours(1);
                                        wd.ToLine = rtb.RealLines.Count();
                                    }
                                }
                            }
                        }

                    }).ConfigureAwait(false);


                }

                return data;
            }
            catch
            {
                return null;
            }
        }

        internal class NGramWord
        {
            public string Word { get; set; }
            public int Frequency { get; set; }
        }
        internal static async void ProcessNGrams(string line)
        {
            await Task.Run(() =>
            {
                string[] sentences = line.Split(". ");
                string[] words = line.CleanUpSentence().Split().Select(s => s.Trim()).ToArray();
                string first = string.Empty;
                string second = string.Empty;
                string third = string.Empty;

                int count = words.Count();
                if (count >= 2)
                {
                    for (int i = 0; i < count - 2; i++)
                    {
                        first = words[i].SanitiseForNGram();

                        second = words[i + 1].SanitiseForNGram();
                        AddToNGramDictionary(string.Format("[{0}]", first).ToLower(), second);
                        if (count > 2)
                        {
                            third = words[i + 2].SanitiseForNGram();
                            AddToNGramDictionary(string.Format("[{0}][{1}]", first, second).ToLower(), third);
                        }
                        if (i == count - 3)
                        {
                            first = words[i + 1].SanitiseForNGram();
                            second = words[i + 2].SanitiseForNGram();
                            AddToNGramDictionary(string.Format("[{0}]", first).ToLower(), second);
                        }
                    }
                }
            });
        }

        private static void AddToNGramDictionary(string key, string second)
        {
            lock (_DictNGram)
            {
                if (!_DictNGram.ContainsKey(key))
                {
                    _DictNGram.Add(key, new List<NGramWord>());
                }

                var list = _DictNGram[key];
                var item = list.FirstOrDefault(s => s.Word == second);
                if (item != null)
                {
                    item.Frequency++;
                }
                else
                {
                    list.Add(new NGramWord() { Word = second, Frequency = 1 });
                }
            }
        }

        static void AddOrUpdateThreadConext(int threadId, List<Tag> tags, ActionData actionData)
        {
            if (!_DictThreadContext.ContainsKey(threadId))
            {
                _DictThreadContext.Add(threadId, new Tuple<List<Tag>, ActionData>(tags, actionData));
            }
            else
            {
                _DictThreadContext[threadId] = new Tuple<List<Tag>, ActionData>(tags, actionData);
            }
        }

        internal static bool TryParse(string documentName, string line, int lineIndex, DayData dayData, DataBuildMode dataBuildMode, out ActionData actionData, out WorkData workData)
        {
            bool success = true;
            actionData = null;
            workData = null;

            try
            {
                line = line.Trim();

                if (line.Length == 0) return false;

                //Parse Actions
                if (line.StartsWith("▶"))
                {
                    actionData = new ActionData();
                    actionData.Name = line;
                    actionData.Parent = dayData;
                    actionData.DateDue = dayData.Date;
                    actionData.DateLogged = dayData.Date;
                    ParseActions(line, actionData);
                    AddOrUpdateThreadConext(Thread.CurrentThread.ManagedThreadId, null, null);
                    var tags = RebuildTagData(documentName, line, lineIndex, dayData, dayData.Date, "Action", dataBuildMode);
                    AddOrUpdateThreadConext(Thread.CurrentThread.ManagedThreadId, tags, actionData);
                    return success;
                }
                if (line.StartsWith("✓"))
                {
                    AddOrUpdateThreadConext(Thread.CurrentThread.ManagedThreadId, null, null);
                    var tags = RebuildTagData(documentName, line, lineIndex, dayData, dayData.Date, "Action Completed", dataBuildMode);
                    AddOrUpdateThreadConext(Thread.CurrentThread.ManagedThreadId, tags, null);
                    return success;
                }
                if (line.StartsWith("Ⓘ"))
                {

                    if (_DictThreadContext.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                    {
                        var tuple = _DictThreadContext[Thread.CurrentThread.ManagedThreadId];
                        if (tuple != null && tuple.Item2 != null)
                        {
                            Match match = Regex.Match(line, @"Ⓘ[\d/]+ [\d/:]+");
                            if (match != null && match.Success)
                            {
                                DateTime dateTime;
                                if (DateTime.TryParse(match.Value.TrimStart('Ⓘ'), out dateTime))
                                {
                                    var informattion = line.Substring(match.Value.Length, line.Length - match.Value.Length);
                                    var info = tuple.Item2.InfoList.Where(s => s.DateTime == dateTime && s.Information == informattion).FirstOrDefault();
                                    if (info != null)
                                    {
                                        tuple.Item2.InfoList.Remove(info);
                                    }

                                    tuple.Item2.InfoList.Add(new InformationData()
                                    { DateTime = dateTime, Information = informattion });

                                    //TrySettingDueDate(tuple.Item2, line);
                                    ParseActions(informattion, tuple.Item2);
                                }
                            }
                        }
                    }

                    RebuildTagData(documentName, line, lineIndex, dayData, dayData.Date, "Information", dataBuildMode);

                    return success;
                }
                if (line.StartsWith("Ⓢ"))
                {
                    AddOrUpdateThreadConext(Thread.CurrentThread.ManagedThreadId, null, null);
                    Match match = Regex.Match(line, @"Ⓢ[\d/]+ [\d/:]+");
                    if (match != null && match.Success)
                    {
                        DateTime dateTime; 
                        if (DateTime.TryParse(match.Value.TrimStart('Ⓢ'), out dateTime))
                        {
                            workData = new WorkData() { From = dateTime, 
                                Name = line.Substring(match.Value.Length, line.Length - match.Value.Length),
                                FromLine = lineIndex};
                            dayData.WorkList.Add(workData);

                            MatchCollection trackMatches = Regex.Matches(line, @"(?<= %)\w+");
                            foreach (Match trackMatch in trackMatches)
                            {
                                workData.TrackList.Add(trackMatch.Value);
                            }

                            // If there is any work data which has started before this and not been 'ended' end that before this work data
                            WorkData data = dayData.WorkList.OrderByDescending(s => s.From).FirstOrDefault(s => s.From < dateTime && s.To == default(DateTime));
                            if (data != null)
                            {
                                data.To = dateTime.AddSeconds(-1);
                                data.ToLine = lineIndex;
                            }
                            RebuildTagData(documentName, line, lineIndex, dayData, dayData.Date, "Meeting", dataBuildMode);
                        }
                    }

                    return success;
                }
                if (line.StartsWith("Ⓔ"))
                {
                    AddOrUpdateThreadConext(Thread.CurrentThread.ManagedThreadId, null, null);

                    Match match = Regex.Match(line, @"Ⓔ[\d/]+ [\d/:]+");
                    if (match != null && match.Success)
                    {
                        DateTime dateTime;
                        if (DateTime.TryParse(match.Value.TrimStart('Ⓔ'), out dateTime))
                        {
                            WorkData data = dayData.WorkList.OrderByDescending(s => s.From).FirstOrDefault(s => s.From < dateTime);
                            if (data != null)
                            {
                                if (data.To == default(DateTime))
                                {
                                    data.To = dateTime;
                                    data.ToLine = lineIndex;
                                }
                                else
                                {
                                    WorkData newWD = new WorkData();
                                    newWD.Name = "<Created>";
                                    newWD.From = data.To.AddSeconds(1);
                                    newWD.To = dateTime;
                                    dayData.WorkList.Add(newWD);
                                    workData = newWD;
                                }
                            }
                        }
                    }
                    return success;
                }
                AddOrUpdateThreadConext(Thread.CurrentThread.ManagedThreadId, null, null);

                RebuildTagData(documentName, line, lineIndex, dayData, dayData.Date, "General", dataBuildMode);
                
            }
            catch
            {
                success = false;
            }

            return success;
        }

        private static List<Tag> RebuildTagData(string documentName, string line, int lineIndex, DayData dayData, DateTime dateTime, string type, DataBuildMode dataBuildMode)
        {
            List<Tag> tags = null;
            if (dataBuildMode == DataBuildMode.None) return null;
            bool processed = false;
            var matchesTags = Regex.Matches(line, @"[#%]\w*");
            //if (matchesTags != null && matchesTags.Count > 0)
            //{
                switch (dataBuildMode)
                {
                    case DataBuildMode.Rebuild:
                    /*{
                        processed = false;
                        ////DB.RemoveDocumentTags(documentName);

                        if (_DictThreadContext.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                        {
                            var actionTags = _DictThreadContext[Thread.CurrentThread.ManagedThreadId];
                            if (actionTags != null )
                            {
                                if (actionTags.Item1 != null)
                                {
                                    actionTags.Item1.ForEach(s => s.FollowUps.Add(line));
                                    if (matchesTags != null && matchesTags.Count > 0)
                                    {
                                        var actionTag = actionTags.Item1.FirstOrDefault();
                                        if (actionTag != null)
                                        {
                                            tags = matchesTags.Cast<Match>().Select(s =>
                                                GetTag(actionTag.DocumentName, actionTag.Line, -1, actionTag.DateTime, "Action", s)
                                                ).GroupBy(s => s.Id).Select(s => s.First()).ToList();
                                            processed = true;
                                        }
                                    }
                                }
                                else if ()
                                {

                                }
                                DB.UpsertTags(actionTags.Item1);
                            }
                        }
                        if (!processed && matchesTags != null && matchesTags.Count > 0)
                        {
                            tags = matchesTags.Cast<Match>().Select(s =>
                                GetTag(documentName, line, lineIndex, dateTime, type, s)
                                ).GroupBy(s => s.Id).Select(s => s.First()).ToList();
                            DB.AddTags(tags);
                        }

                        break;
                    }*/
                    case DataBuildMode.Update:
                    {
                        processed = false;
                        Tuple<List<Tag>, ActionData> actionTags = null;
                        if (_DictThreadContext.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                        {
                            actionTags = _DictThreadContext[Thread.CurrentThread.ManagedThreadId];
                            if (actionTags != null )
                            {
                                if (actionTags.Item1 != null)
                                {
                                    foreach (var tag in actionTags.Item1)
                                    {
                                        if (!tag.FollowUps.Contains(line))
                                        {
                                            tag.FollowUps.Add(line);
                                            DB.UpsertTag(tag);
                                        }
                                    }
                                }
                                if (matchesTags != null && matchesTags.Count > 0 && actionTags.Item2 != null)
                                {
                                    // Use the ActionData
                                    tags = matchesTags.Cast<Match>().Select(s =>
                                        GetTag(documentName, actionTags.Item2.Name, lineIndex, dateTime, "Action", s)
                                        ).GroupBy(s => s.Id).Select(s => s.First()).ToList();
                                    tags.ForEach(s => s.FollowUps.AddRange(actionTags.Item2.InfoList.Select(t => t.ToString())));
                                    _DictThreadContext[Thread.CurrentThread.ManagedThreadId]
                                        = new Tuple<List<Tag>, ActionData>(tags, actionTags.Item2);
                                    DB.UpsertTags(tags);
                                    processed = true;
                                }
                            }
                        }
                        if (!processed && matchesTags != null && matchesTags.Count > 0)
                        {
                            tags = matchesTags
                                .Cast<Match>()
                                .Select(s =>
                                GetTag(documentName, line, lineIndex, dateTime, type, s)
                                ).GroupBy(s => s.Id).Select(s => s.First())
                                .ToList();
                            
                            tags.ForEach(s => DB.UpsertTag(s));
                        }

                        break;
                    }
                }
            
            //}

            return tags;
        }

        private static Tag GetTag(string documentName, string line, int lineIndex, DateTime dateTime, string type, Match s)
        {
            return new Tag()
            {
                Name = s.Value.ToLower(),
                ContainerName = documentName,
                DateTime = dateTime,
                DocumentName = documentName,
                Id = string.Format("{0}/{1}/{2}/{3}", documentName, s.Value, type, lineIndex),
                Line = line,
                ParentContentType = type
            };
        }

        private static void ParseActions(string line, ActionData actionData)
        {
            TrySettingDueDate(actionData, line);
            //int countInputs = 0;

            var regex = new Regex(@"(?<=@)\w+");
            var matches = regex.Matches(line);
            foreach (Match m in matches)
            {
                actionData.PeopleList.Add(m.Value);
            }

            regex = new Regex(@"(?<=\[@)\w+(?<!\])");
            matches = regex.Matches(line);
            foreach (Match m in matches)
            {
                actionData.InputFrom.Add(m.Value);
                //countInputs++;
            }

            regex = new Regex(@"(?<=>@)\w+");
            matches = regex.Matches(line);
            foreach (Match m in matches)
            {
                actionData.InputTo.Add(m.Value);
                var splits = m.Value.Split('_');
                if (splits.Count() > 1)
                {
                    actionData.InputTo.Add(splits[1]);
                }
                //countInputs++;
            }

            regex = new Regex(@"(?<=<@)\w+");
            matches = regex.Matches(line);
            foreach (Match m in matches)
            {
                actionData.InputFrom.Add(m.Value);
                var splits = m.Value.Split('_');
                if (splits.Count() > 1)
                {
                    actionData.InputFrom.Add(splits[1]);
                }
                //countInputs++;
            }

            regex = new Regex(@"(?<= #)\w+");
            matches = regex.Matches(line);
            foreach (Match m in matches)
            {
                actionData.HashTags.Add(m.Value);
            }

            regex = new Regex(@"(?<= %)\w+");
            matches = regex.Matches(line);
            foreach (Match m in matches)
            {
                actionData.TrackList.Add(m.Value);
            }

            actionData.InputFrom = actionData.InputFrom.GroupBy(s => s).Select(s => s.Key).ToList();
            actionData.InputTo = actionData.InputTo.GroupBy(s => s).Select(s => s.Key).ToList();
            actionData.TrackList = actionData.TrackList.GroupBy(s => s).Select(s => s.Key).ToList();
            actionData.PeopleList = actionData.PeopleList.GroupBy(s => s).Select(s => s.Key).ToList();

            if (actionData.InputFrom.Count() + actionData.InputTo.Count() == 0)
            {
                actionData.InputFrom.Add("<Self>");
            }
        }

        internal static void RemoveDocumentTags(string documentName)
        {
            DB.RemoveDocumentTags(documentName);
        }

        private static bool TrySettingDueDate(ActionData actionData, string line)
        {
            string text = line.ToLower();
            List<DateTime> dates = new List<DateTime>();
            if (text.Contains("by today"))
            {
                dates.Add(actionData.DateLogged.Date.AddDays(1).AddSeconds(-1));
            }
            if (text.Contains("by tomorrow"))
            {
                dates.Add(actionData.DateLogged.Date.AddDays(2).AddSeconds(-1));
            }
            if (text.Contains("by the end of the week"))
            {
                dates.Add(actionData.DateLogged.Date.AddDays(DayOfWeek.Saturday - DateTime.Now.DayOfWeek).AddSeconds(-1));
            }
            if (text.Contains("by the end of the next week"))
            {
                dates.Add(actionData.DateLogged.Date.AddDays(7 + DayOfWeek.Saturday - DateTime.Now.DayOfWeek).AddSeconds(-1));
            }
            ExtractDueDates(text, dates);
            foreach(InformationData infoData in actionData.InfoList)
            {
                ExtractDueDates(infoData.Information, dates);
            }
            if (dates.Count() > 0)
            {
                var latestDate = dates.OrderBy(s => s).Last();
                if (latestDate > actionData.DateDue) actionData.DateDue = latestDate;
                return true;
            }

            return false;
        }

        private static void ExtractDueDates(string text, List<DateTime> dates)
        {
            var matches = Regex.Matches(text, @"by [\d,/,' ',':']+");
            if (matches != null && matches.Count > 0)
            {

                matches
                    .Cast<Match>()
                    .ToList()
                    .ForEach(s =>
                    {
                        DateTime dateTime = default(DateTime);
                        if (DateTime.TryParse(s.Value.Substring(3, s.Value.Length - 3), out dateTime))
                        {
                            dates.Add(dateTime);
                        }
                    });
            }
        }
    }
}
