using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TextRuler
{
    public static class Extensions
    {
        public static string RemoveBetweenAngBracketsInclusive(this string source)
        {
            var pattern = @"<(.*?)>";
            var matches = Regex.Matches(source, pattern);
            foreach(Match match in matches)
            {
                source = source.Replace(match.Value, "<link>");
            }
            return source;
        }

        public static string GetTitle(this string source)
        {
            string result = string.Empty;
            var pattern = @"""(.*?)""";
            var matches = Regex.Matches(source, pattern);
            foreach (Match match in matches)
            {
                result += match.Value + ", ";
            }
            result =  result.TrimEnd(' ', ',');
            return result;
        }

        public static int FindText(this RichTextBox rtb, string textToFind)
        {
            int index = -1;
            var words = textToFind.Split(' ');
            int wordCount = words.Count();
            int findIndex = wordCount - 1;
            index = rtb.Find(textToFind, RichTextBoxFinds.MatchCase);
            while(index < 0 && findIndex > 0)
            {
                string subTextToFind = textToFind.Substring(0, textToFind.LastIndexOf(words[findIndex--]));
                index = rtb.Find(subTextToFind, RichTextBoxFinds.MatchCase);
            }

            return index;
        }

        public static void HighlightText(this RichTextBox myRtb, IEnumerable<string> words, Color color)
        {
            if (words == null || words.Count() == 0) return;

            foreach (string word in words)
            {
                if (word == string.Empty)
                    continue;

                int s_start = myRtb.SelectionStart, startIndex = 0, index;

                while ((index = myRtb.Text.IndexOf(word, startIndex)) != -1)
                {
                    myRtb.Select(index, word.Length);
                    myRtb.SelectionBackColor = color;

                    startIndex = index + word.Length;
                }

                myRtb.SelectionStart = s_start;
                myRtb.SelectionLength = 0;
                myRtb.SelectionColor = Color.Black;
            }
        }

        public static DateTime ExtractDateTimeAfterSymbol(this string line, char symbol)
        {
            Match match = Regex.Match(line, symbol + @" [\d/]+ [\d/:]+");
            if (match != null && match.Success)
            {
                DateTime dateTime;
                if (DateTime.TryParse(match.Value.TrimStart(symbol), out dateTime))
                {
                    return dateTime;
                }
            }
            return DateTime.MaxValue;
        }
        public static DateTime ExtractLineDate(this string text)
        {
            var match = Regex.Match(text, @"^\S{1}\d{2}\/\d{2}\/\d{4}");
            if (match.Success)
            {
                DateTime dateTime = default(DateTime);
                if (DateTime.TryParse(match.Value.Substring(1, match.Value.Length - 1), out dateTime))
                {
                    return dateTime.Date;
                }
            }
            return DateTime.MinValue;
        }

        public static List<DateTime> ExtractDueDates(this string text)
        {
            List<DateTime> dates = new List<DateTime>();
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
                            dates.Add(dateTime.Date);
                        }
                    });
            }
            return dates;
        }

        // For completeness, this is two methods to ensure that the null check 
        // is done eagerly while the loop is done lazily. If that's not an issue, 
        // you can forego the check and just use the main function.

        public static IEnumerable<T> NonConsecutive<T>(this IEnumerable<T> input)
        {
            if (input == null) throw new ArgumentNullException("input");
            return NonConsecutiveImpl(input);
        }

        static IEnumerable<T> NonConsecutiveImpl<T>(this IEnumerable<T> input)
        {
            bool isFirst = true;
            T last = default(T);
            foreach (var item in input)
            {
                if (isFirst || !object.Equals(item, last))
                {
                    yield return item;
                    last = item;
                    isFirst = false;
                }
            }
        }

        public static string ChangeFileExtension(this string source, string newExtension)
        {
            return Path.GetFileNameWithoutExtension(source) + "." + newExtension;
        }
        public static string[] Split(this string source, string splitter)
        {
            if (splitter == null) return new string[] { source };
            int index = source.IndexOf(splitter);
            List<string> splits = new List<string>();
            int lastIndex = 0;
            while(index >= 0)
            {
                splits.Add(source.Substring(lastIndex, index - lastIndex).Trim());
                lastIndex = index + splitter.Length;
                index = source.IndexOf(splitter, lastIndex);
            }
            if (lastIndex < source.Length)
            {
                splits.Add(source.Substring(lastIndex, source.Length - lastIndex).Trim());
            }
            return splits.ToArray();
        }
        public static string SanitiseForNGram(this string source)
        {
            return source;
            //char[] filtered = source.Where(c => char.IsLetterOrDigit(c)).ToArray();
            //return new string(filtered);//.ToLower();
        }

        public static string CleanUpSentence(this string source)
        {
            return source;
            char[] filtered = source.Where(c => char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c)).ToArray();
            return new string(filtered);
        }

        public static string CleanUpName(this string source)
        {
            char[] filtered = source.Where(c => char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c)).ToArray();
            return new string(filtered);
        }

        public static bool IsLetter(this string source)
        {
            foreach(char c in source)
            {
                if (!char.IsLetter(c)) return false;
            }
            return true;
        }

        public static string Truncate(this string source, int length)
        {
            if (source == null) return string.Empty;
            if (source.Length <= length)
            {
                return source;
            }
            else
            {
                return source.Substring(0, length - 3) + "...";
            }
        }

        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect)
        {
            g.FillPath(brush, RoundedRectangle.Create(rect.Left, rect.Top, rect.Width, rect.Height,
                2, RoundedRectangle.RectangleCorners.All));
        }
    }

  public abstract class RoundedRectangle
  {
    public enum RectangleCorners
    {
      None = 0, TopLeft = 1, TopRight = 2, BottomLeft = 4, BottomRight = 8,
      All = TopLeft | TopRight | BottomLeft | BottomRight
    }

    public static GraphicsPath Create(int x, int y, int width, int height, 
                                      int radius, RectangleCorners corners)
    {
      int xw = x + width;
      int yh = y + height;
      int xwr = xw - radius;
      int yhr = yh - radius;
      int xr = x + radius;
      int yr = y + radius;
      int r2 = radius * 2;
      int xwr2 = xw - r2;
      int yhr2 = yh - r2;

      GraphicsPath p = new GraphicsPath();
      p.StartFigure();

      //Top Left Corner
      if ((RectangleCorners.TopLeft & corners) == RectangleCorners.TopLeft)
      {
        p.AddArc(x, y, r2, r2, 180, 90);
      }
      else
      {
        p.AddLine(x, yr, x, y);
        p.AddLine(x, y, xr, y);
      }

      //Top Edge
      p.AddLine(xr, y, xwr, y);

      //Top Right Corner
      if ((RectangleCorners.TopRight & corners) == RectangleCorners.TopRight)
      {
        p.AddArc(xwr2, y, r2, r2, 270, 90);
      }
      else
      {
        p.AddLine(xwr, y, xw, y);
        p.AddLine(xw, y, xw, yr);
      }

      //Right Edge
      p.AddLine(xw, yr, xw, yhr);

      //Bottom Right Corner
      if ((RectangleCorners.BottomRight & corners) == RectangleCorners.BottomRight)
      {
        p.AddArc(xwr2, yhr2, r2, r2, 0, 90);
      }
      else
      {
        p.AddLine(xw, yhr, xw, yh);
        p.AddLine(xw, yh, xwr, yh);
      }

      //Bottom Edge
      p.AddLine(xwr, yh, xr, yh);

      //Bottom Left Corner
      if ((RectangleCorners.BottomLeft & corners) == RectangleCorners.BottomLeft)
      {
        p.AddArc(x, yhr2, r2, r2, 90, 90);
      }
      else
      {
        p.AddLine(xr, yh, x, yh);
        p.AddLine(x, yh, x, yhr);
      }

      //Left Edge
      p.AddLine(x, yhr, x, yr);

      p.CloseFigure();
      return p;
    }

    public static GraphicsPath Create(Rectangle rect, int radius, RectangleCorners c)
    { return Create(rect.X, rect.Y, rect.Width, rect.Height, radius, c); }

    public static GraphicsPath Create(int x, int y, int width, int height, int radius)
    { return Create(x, y, width, height, radius, RectangleCorners.All); }

    public static GraphicsPath Create(Rectangle rect, int radius)
    { return Create(rect.X, rect.Y, rect.Width, rect.Height, radius); }

    public static GraphicsPath Create(int x, int y, int width, int height)
    { return Create(x, y, width, height, 5); }

    public static GraphicsPath Create(Rectangle rect)
    { return Create(rect.X, rect.Y, rect.Width, rect.Height); }
  }
}
