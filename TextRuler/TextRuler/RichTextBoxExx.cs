// RichTextBoxEx.cs
// ------------------------------------------------------------------
//
// An extended RichTextBox that provides a few extra capabilities:
//
//  1. line numbering, fast and easy.  It numbers the lines "as
//     displayed" or according to the hard newlines in the text.  The UI
//     of the line numbers is configurable: color, font, width, leading
//     zeros or not, etc.  One limitation: the line #'s are always
//     displayed to the left.
//
//  2. Programmatic scrolling
//
//  3. BeginUpdate/EndUpdate and other bells and whistles.  Theres also
//     BeginUpdateAndSateState()/EndUpdateAndRestoreState(), to keep the
//     cursor in place across select/updates.
//
//  4. properties: FirstVisibleLine / NumberOfVisibleLines - in support of
//     line numbering.
//
//
// Copyright (c) 2010 Dino Chiesa.
// All rights reserved.
//
// This file is part of the source code disribution for Ionic's
// XPath Visualizer Tool.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.rtf or License.txt for the license details.
// More info on: http://XPathVisualizer.codeplex.com
//
// ------------------------------------------------------------------
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using TextRuler;
using System.Text;
using System.Text.RegularExpressions;

namespace Ionic.WinForms
{
    /// <summary>
    ///   Defines methods for performing operations on RichTextBox.
    /// </summary>
    ///
    /// <remarks>
    ///   <para>
    ///     The methods in this class could be defined as "extension methods" but
    ///     for efficiency I'd like to retain some state between calls - for
    ///     example the handle on the richtextbox or the buffer and structure for
    ///     the EM_SETCHARFORMAT message, which can be called many times in quick
    ///     succession.
    ///   </para>
    ///
    ///   <para>
    ///     We define these in a separate class for speed and efficiency. For the
    ///     RichTextBox, in order to make a change in format of some portion of
    ///     the text, the app must select the text.  When the RTB has focus, it
    ///     will scroll when the selection is updated.  If we want to retain state
    ///     while highlighting text then, we'll have to restore the scroll state
    ///     after a highlight is applied.  But this will produce an ugly UI effect
    ///     where the scroll jumps forward and back repeatedly.  To avoid that, we
    ///     need to suppress updates to the RTB, using the WM_SETREDRAW message.
    ///   </para>
    ///
    ///   <para>
    ///     As a complement to that, we also have some speedy methods to get and
    ///     set the scroll state, and the selection state.
    ///   </para>
    ///
    /// </remarks>
    [ToolboxBitmap(typeof(RichTextBox))]
    public class RichTextBoxExx : RichTextBox
    {
        private User32.CHARFORMAT charFormat;
        private IntPtr lParam1;

        private int _savedScrollLine;
        private int _savedSelectionStart;
        private int _savedSelectionEnd;
        private Pen _borderPen;
        private System.Drawing.StringFormat _stringDrawingFormat;
        private System.Security.Cryptography.HashAlgorithm alg;   // used for comparing text values
        public DateTime DocumentDate { get; set; }

        public bool HasDueDateErrors {  get { return DueDateErrors.Count(s => s) > 0; } }

        protected bool IsDesginMode { get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; } }
        public RichTextBoxExx():base()
        {
            charFormat = new User32.CHARFORMAT()
                {
                    cbSize = Marshal.SizeOf(typeof(User32.CHARFORMAT)),
                    szFaceName= new char[32]
                };

            lParam1= Marshal.AllocCoTaskMem( charFormat.cbSize );
            if (!IsDesginMode)
            {
                // defaults
                NumberFont = new System.Drawing.Font("Consolas",
                                                    9.75F,
                                                    System.Drawing.FontStyle.Regular,
                                                    System.Drawing.GraphicsUnit.Point, ((byte)(0)));

                NumberColor = Color.White;// Color.FromName("DarkGray");
                NumberLineCounting = LineCounting.CRLF;
                NumberAlignment = StringAlignment.Center;
                NumberBorder = SystemColors.ControlDark;
                NumberBorderThickness = 1;
                NumberPadding = 2;
                NumberBackground1 = SystemColors.ControlLight;
                NumberBackground2 = SystemColors.Window;
                SetStringDrawingFormat();
            }
            alg = System.Security.Cryptography.SHA1.Create();

            this.DoubleBuffered = true;
        }

        ~RichTextBoxExx()
        {
            // Free the allocated memory
            Marshal.FreeCoTaskMem(lParam1);
        }

        private void SetStringDrawingFormat()
        {
            _stringDrawingFormat = new System.Drawing.StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = NumberAlignment,
                    Trimming = StringTrimming.None,
                };
        }


        protected override void OnTextChanged(EventArgs e)
        {
            NeedRecomputeOfLineNumbers();
            
            base.OnTextChanged(e);
        }

        public void BeginUpdate()
        {
            if (!IsDesginMode) User32.SendMessage(this.Handle, (int)User32.Msgs.WM_SETREDRAW, 0, IntPtr.Zero);
        }

        public void EndUpdate()
        {
            if (!IsDesginMode) User32.SendMessage(this.Handle, (int)User32.Msgs.WM_SETREDRAW, 1, IntPtr.Zero);
        }


        public IntPtr BeginUpdateAndSuspendEvents()
        {
            // Stop redrawing:
            if (!IsDesginMode) User32.SendMessage(this.Handle, (int)User32.Msgs.WM_SETREDRAW, 0, IntPtr.Zero);
            // Stop sending of events:
            IntPtr eventMask = User32.SendMessage(this.Handle, User32.Msgs.EM_GETEVENTMASK, 0, IntPtr.Zero);

            return eventMask;
        }

        public void EndUpdateAndResumeEvents(IntPtr eventMask)
        {
            // turn on events
            if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.EM_SETEVENTMASK, 0, eventMask);
            // turn on redrawing
            if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_SETREDRAW, 1, IntPtr.Zero);
            NeedRecomputeOfLineNumbers();
            this.Invalidate();
        }



        public void GetSelection(out int start, out int end)
        {
            User32.SendMessageRef(this.Handle, (int)User32.Msgs.EM_GETSEL, out start, out end);
        }

        public void SetSelection(int start, int end)
        {
            User32.SendMessage(this.Handle, (int)User32.Msgs.EM_SETSEL, start, end);
        }

        public void BeginUpdateAndSaveState()
        {
            User32.SendMessage(this.Handle, (int)User32.Msgs.WM_SETREDRAW, 0, IntPtr.Zero);
            // save scroll position
            _savedScrollLine = FirstVisibleDisplayLine;

            // save selection
            GetSelection(out _savedSelectionStart, out _savedSelectionEnd);
        }

        public void EndUpdateAndRestoreState()
        {
            // restore scroll position
            int Line1 = FirstVisibleDisplayLine;
            Scroll(_savedScrollLine - Line1);

            // restore the selection/caret
            SetSelection(_savedSelectionStart, _savedSelectionEnd);

            // allow redraw
            User32.SendMessage(this.Handle, (int)User32.Msgs.WM_SETREDRAW, 1, IntPtr.Zero);

            // explicitly ask for a redraw?
            Refresh();
        }

        private String _sformat;
        private int _ndigits;
        private int _lnw = -1;
        private int _circleWidth = -1;
        
        private int LineNumberWidth
        {
            get
            {
                _lnw = 21;
                if (_lnw > 0) return _lnw;
                if (NumberLineCounting == LineCounting.CRLF)
                {
                    _ndigits = (CharIndexForTextLine.Length == 0)
                        ? 1
                        : (int)(1 + Math.Log((double)CharIndexForTextLine.Length, 10));
                }
                else
                {
                    int n = GetDisplayLineCount();
                    _ndigits = (n == 0)
                        ? 1
                        : (int)(1 + Math.Log((double)n, 10));
                }
                var s = new String('0', _ndigits);
                var b = new Bitmap(400,400); // in pixels
                var g = Graphics.FromImage(b);
                SizeF size = g.MeasureString(s, NumberFont);
                g.Dispose();
                _lnw = NumberPadding * 2 + 4 + (int) (size.Width + 0.5 + NumberBorderThickness);
                _sformat = "{0:D" + _ndigits + "}";
                return _lnw;
            }
        }

        public bool _lineNumbers;
        public bool ShowLineNumbers
        {
            get
            {
                return _lineNumbers;
            }
            set
            {
                if (value == _lineNumbers) return;
                SetLeftMargin(value ? LineNumberWidth + Margin.Left : Margin.Left);
                _lineNumbers = value;
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        private void NeedRecomputeOfLineNumbers()
        {
            //System.Console.WriteLine("Need Recompute of line numbers...");
            _CharIndexForTextLine = null;
            _Text2 = null;
            _lnw = -1;

            if (_paintingDisabled) return;

            if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
        }

        private Font _NumberFont;
        public Font NumberFont
        {
            get { return _NumberFont; }
            set
            {
                if (_NumberFont == value) return;
                _lnw = -1;
                _NumberFont = value;
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        private LineCounting _NumberLineCounting;
        public LineCounting NumberLineCounting
        {
            get { return  _NumberLineCounting; }
            set
            {
                if (_NumberLineCounting == value) return;
                _lnw = -1;
                _NumberLineCounting = value;
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        private StringAlignment _NumberAlignment;
        public StringAlignment NumberAlignment
        {
            get { return  _NumberAlignment; }
            set
            {
                if (_NumberAlignment == value) return;
                _NumberAlignment = value;
                SetStringDrawingFormat();
                if (!IsDesginMode)
                {
                    User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
                }
            }
        }

        private Color _NumberColor;
        public Color NumberColor
        {
            get { return _NumberColor; }
            set
            {
                if (_NumberColor.ToArgb() == value.ToArgb()) return;
                _NumberColor = value;
                if (!IsDesginMode)
                    User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        private bool _NumberLeadingZeroes;
        public bool NumberLeadingZeroes
        {
            get { return _NumberLeadingZeroes; }
            set
            {
                if (_NumberLeadingZeroes == value) return;
                _NumberLeadingZeroes = value;
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        private Color _NumberBorder;
        public Color NumberBorder
        {
            get { return _NumberBorder; }
            set
            {
                if (_NumberBorder.ToArgb() == value.ToArgb()) return;
                _NumberBorder = value;
                NewBorderPen();
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        private int _NumberPadding;
        public int NumberPadding
        {
            get { return _NumberPadding; }
            set
            {
                if (_NumberPadding == value) return;
                _lnw = -1;
                _NumberPadding = value;
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        public Single _NumberBorderThickness;
        public Single NumberBorderThickness
        {
            get { return _NumberBorderThickness; }
            set
            {
                if (_NumberBorderThickness == value) return;
                _lnw = -1;
                _NumberBorderThickness = value;
                NewBorderPen();
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        private Color _NumberBackground1;
        public Color NumberBackground1
        {
            get { return _NumberBackground1; }
            set
            {
                if (_NumberBackground1.ToArgb() == value.ToArgb()) return;
                _NumberBackground1 = value;
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }

        private Color _NumberBackground2;
        public Color NumberBackground2
        {
            get { return _NumberBackground2; }
            set
            {
                if (_NumberBackground2.ToArgb() == value.ToArgb()) return;
                _NumberBackground2 = value;
                if (!IsDesginMode) User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
            }
        }


        private bool _paintingDisabled;
        public void SuspendLineNumberPainting()
        {
            _paintingDisabled = true;
        }
        public void ResumeLineNumberPainting()
        {
            _paintingDisabled = false;
        }


        private void NewBorderPen()
        {
            _borderPen = new Pen(NumberBorder);
            _borderPen.Width = NumberBorderThickness;
            _borderPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
        }



        private DateTime _lastMsgRecd = new DateTime(1901,1,1);

        protected override void WndProc(ref Message m)
        {
            bool handled = false;
            switch (m.Msg)
            {
                case (int)User32.Msgs.WM_PAINT:
                    //System.Console.WriteLine("{0}", User32.Mnemonic(m.Msg));
                    //System.Console.Write(".");
                    if (_paintingDisabled) return;
                    if (_lineNumbers)
                    {
                        base.WndProc(ref m);
                        this.PaintLineNumbers();
                        handled = true;
                    }
                    break;

                case (int)User32.Msgs.WM_CHAR:
                    // the text is being modified
                    NeedRecomputeOfLineNumbers();
                    break;

//                 case (int)User32.Msgs.EM_POSFROMCHAR:
//                 case (int)User32.Msgs.WM_GETDLGCODE:
//                 case (int)User32.Msgs.WM_ERASEBKGND:
//                 case (int)User32.Msgs.OCM_COMMAND:
//                 case (int)User32.Msgs.OCM_NOTIFY:
//                 case (int)User32.Msgs.EM_CHARFROMPOS:
//                 case (int)User32.Msgs.EM_LINEINDEX:
//                 case (int)User32.Msgs.WM_NCHITTEST:
//                 case (int)User32.Msgs.WM_SETCURSOR:
//                 case (int)User32.Msgs.WM_KEYUP:
//                 case (int)User32.Msgs.WM_KEYDOWN:
//                 case (int)User32.Msgs.WM_MOUSEMOVE:
//                 case (int)User32.Msgs.WM_MOUSEACTIVATE:
//                 case (int)User32.Msgs.WM_NCMOUSEMOVE:
//                 case (int)User32.Msgs.WM_NCMOUSEHOVER:
//                 case (int)User32.Msgs.WM_NCMOUSELEAVE:
//                 case (int)User32.Msgs.WM_NCLBUTTONDOWN:
//                     break;
//
//                   default:
//                       // divider
//                       var now = DateTime.Now;
//                       if ((now - _lastMsgRecd) > TimeSpan.FromMilliseconds(850))
//                           System.Console.WriteLine("------------ {0}", now.ToString("G"));
//                       _lastMsgRecd = now;
//
//                       System.Console.WriteLine("{0}", User32.Mnemonic(m.Msg));
//                       break;
            }

            if (!handled)
                base.WndProc(ref m);
        }

        Color GetLineColor(int lineNumber)
        {
            if (lineNumber <= (Lines.Count() - 1) && Lines[lineNumber].Length > 0)
            {
                char c = Lines[lineNumber][0];
                switch (c)
                {
                    case 'Ⓘ': return Color.FromArgb(238, 235, 250);
                    case '✓': return Color.FromArgb(235, 250, 238);
                    case '▶': return Color.FromArgb(255, 219, 219);
                    case '⋫': return Color.FromArgb(200, 200, 219);
                    case 'Ⓢ': return Color.FromArgb(255, 254, 219);
                    case 'Ⓔ': return Color.FromArgb(219, 255, 254);
                    default: return Color.LightGray;
                }
            }
            return Color.LightGray;

        }

        bool GetIsDueDateError(int lineNumber)
        {
            if (lineNumber < DueDateErrors.Length && lineNumber < CharsForTextLine.Length)
            {
                char c = CharsForTextLine[lineNumber];
                bool b = DueDateErrors[lineNumber];
                //if (lineNumber <= (Lines.Count() - 1) && Lines[lineNumber].Length > 0)
                {
                    //char c = Lines[lineNumber][0];
                    switch (c)
                    {
                        
                        case '▶': return b;
                        case '⋫': return b;
                        case 'Ⓘ': return b;


                        default: return false;
                    }
                }

                
            }
            return false;
        }

        Color GetLineColor2(int lineNumber, bool light)
        {
            //int realLineNumber = (NumberLineCounting == LineCounting.CRLF)
            //        ? GetCharIndexForTextLine(lineNumber)
            //        : GetCharIndexForDisplayLine(lineNumber) - 1;
            if (lineNumber < CharsForTextLine.Length)
            {
                char c = CharsForTextLine[lineNumber];
                //if (lineNumber <= (Lines.Count() - 1) && Lines[lineNumber].Length > 0)
                {
                    //char c = Lines[lineNumber][0];
                    switch (c)
                    {
                        case 'Ⓘ': if (light) return Color.LightBlue; else return Color.DarkBlue;
                        case '✓': if (light) return Color.LightGreen; else return Color.DarkGreen;
                        case '▶': if (light) return Color.LightSalmon; else return Color.DarkRed;
                        case '⋫': if (light) return Color.LightGoldenrodYellow; else return Color.Gold;
                        case 'Ⓢ': if (light) return Color.LightGray; else return Color.DarkGray;
                        case 'Ⓔ': if (light) return Color.LightGray; else return Color.DarkGray;
                        default: if (light) return Color.White; else return Color.LightGray;
                    }
                }
            }
            return Color.LightGray;

        }



        int _lastWidth = 0;
        private void PaintLineNumbers()
        {
            //System.Console.WriteLine(">> PaintLineNumbers");
            // To reduce flicker, double-buffer the output

            if (_paintingDisabled) return;

            int w = LineNumberWidth;
            int circleWidth = 12;// (int)CircleWidth;//TODO: Magic number????
            if (w!=_lastWidth)
            {
                //System.Console.WriteLine("  WIDTH change {0} != {1}", _lastWidth, w);
                SetLeftMargin(w + Margin.Left);
                _lastWidth = w;
                // Don't bother painting line numbers - the margin isn't wide enough currently.
                // Ask for a new paint, and paint them next time round.
                User32.SendMessage(this.Handle, User32.Msgs.WM_PAINT, 0, 0);
                return;
            }

            
           

            Bitmap buffer = new Bitmap(w, this.Bounds.Height);
            Graphics g = Graphics.FromImage(buffer);
            g.SmoothingMode = SmoothingMode.HighQuality;
            

            Brush forebrush = new SolidBrush(NumberColor);
            var rect = new Rectangle (0, 0, w, this.Bounds.Height);

            bool wantDivider = NumberBackground1.ToArgb() == NumberBackground2.ToArgb();
            Brush backBrush = (wantDivider)
                ? (Brush) new SolidBrush(NumberBackground2)
                : SystemBrushes.Window;

            g.FillRectangle(backBrush, rect);

            int n = (NumberLineCounting == LineCounting.CRLF)
                ? NumberOfVisibleTextLines
                : NumberOfVisibleDisplayLines;

            int first = (NumberLineCounting == LineCounting.CRLF)
                ? FirstVisibleTextLine
                : FirstVisibleDisplayLine+1;

            int py = 0;
            int w2 = w - 2 - (int)NumberBorderThickness;
            LinearGradientBrush brush;
            Pen dividerPen = new Pen(NumberColor);

            List<Point> points = new List<Point>();
            List<Tuple<Point, Bitmap>> pointBitmap = new List<Tuple<Point, Bitmap>>();
            for (int i=0; i <= n; i++)
            {
                int ix = first + i;
                int c = (NumberLineCounting == LineCounting.CRLF)
                    ? GetCharIndexForTextLine(ix)
                    : GetCharIndexForDisplayLine(ix)-1;
                //c is the start char index for the line
                

                var p = GetPosFromCharIndex(c+1);
                //p.Offset(-(int)w2, 0);
                //points.Add(p);
                pointBitmap.Add(new Tuple<Point, Bitmap>(p, GetBitmap(this.Bounds.Width - w, 14, GetPointsForWords(c+1, ix))));
                

                Rectangle r4 = Rectangle.Empty;
                Rectangle r5 = Rectangle.Empty;
                Rectangle r6 = Rectangle.Empty;
                Rectangle r4_2 = Rectangle.Empty;


                if (i==n) // last line?
                {
                    if (this.Bounds.Height <= py) continue;
                    r4 = new Rectangle (1, py, w2, this.Bounds.Height-py);
                    r5 = Rectangle.Empty;
                }
                else
                {
                    if (p.Y <= py) continue;
                    r4 = new Rectangle (1, py, w2, p.Y-py);
                    int newpy = (int)((double)(p.Y - py)/2 - (double)w2/4);
                    r5 = new Rectangle(1, py + newpy, circleWidth, circleWidth);
                    r6 = new Rectangle(1, py + newpy, circleWidth, circleWidth);
                    r6.Inflate(new Size(2,2));
                }

                //r4.Inflate(-1, -1);
                //r4.Offset(1, 1);

                if (wantDivider)
                {
                    if (i!=n)
                    g.DrawLine(dividerPen, 1, p.Y+1, w2, p.Y+1); // divider line
                }
                else
                {
                    Color colorOfCurrentRTLineStart = GetLineColor2(ix, true);
                    Color colorOfCurrentRTLineEnd = GetLineColor2(ix, false);
                    // new brush each time for gradient across variable rect sizes
                    brush = new LinearGradientBrush( r4,
                                                     //NumberBackground1,
                                                     colorOfCurrentRTLineStart,
                                                     colorOfCurrentRTLineEnd, 
                                                     //45
                                                     LinearGradientMode.ForwardDiagonal
                                                     );
                    //g.FillRectangle(Brushes.LightGray, r4);
                    g.FillEllipse(brush, r5);
                    r4.Offset(w2-3, 0);
                    g.FillRoundedRectangle(brush, r4);

                    // Draw symbol to show that the due date is wrongly typed
                    if (GetIsDueDateError(ix))
                    {
                        g.DrawEllipse(Pens.DarkSlateGray, r6);
                        g.DrawLine(Pens.DarkSlateGray, r6.Left, r6.Top, r6.Right, r6.Bottom);
                    }
                }

                

                if (NumberLineCounting == LineCounting.CRLF) ix++;

                // conditionally slide down
                if (NumberAlignment == StringAlignment.Near)
                    rect.Offset(0, 3);

                //var s = (NumberLeadingZeroes) ? String.Format(_sformat, ix) : ix.ToString();
                //g.DrawString(s, NumberFont, forebrush, r5, _stringDrawingFormat);
                py = p.Y;
            }

            if (NumberBorderThickness != 0.0)
            {
                int t = (int)(w-(NumberBorderThickness+0.5)/2) - 1;
                g.DrawLine(_borderPen, t, 0, t, this.Bounds.Height);
                
                //g.DrawLine(_borderPen, w-2, 0, w-2, this.Bounds.Height);
            }

            // paint that buffer to the screen
            Graphics g1 = this.CreateGraphics();
            //Pen pen1 = new Pen(Color.Red, 3);
            //foreach(var tuple in pointBitmap)
            //{
            //    Point point = tuple.Item1;
            //    point.Offset(-w2 - (int)NumberBorderThickness, 0);
            //    g1.DrawImage(tuple.Item2, point);
            //}
            g1.DrawImage(buffer, new Point(0,0));
            //pointBitmap.ForEach(s => g1.DrawImage(s.Item2, s.Item1));
            //testG.DrawLine(pen1, new PointF(0, 0), new PointF(100, 0));
            
            //g1.DrawImage(GetBitmap(this.Bounds.Width - w, 3), points.First());
            //g1.DrawImage(buffer, new Point(100,0));
            //g1.DrawImage(test, new Point(w, 0));
            g1.Dispose();
            g.Dispose();
        }

        private List<Tuple<Point, Point>> GetPointsForWords(int c, int ix)
        {
            Point start = GetPosFromCharIndex(c);
            Point end = GetPosFromCharIndex(c);

            if (_RealLines.Count() > ix)
            {
                string linewa = _RealLines[ix];
                List<string> words = linewa.Split(' ').ToList();
                string first = words.LastOrDefault();
                if (first != null)
                {

                    start = GetPosFromCharIndex(linewa.IndexOf(first));
                    end = GetPosFromCharIndex(linewa.IndexOf(first) + first.Length);
                    return new List<Tuple<Point, Point>>() { new Tuple<Point, Point>(start, end) };
                }
            }

            return null;
        }

        private Bitmap GetBitmap(int width, int height, List<Tuple<Point, Point>> points)
        {
            Bitmap test = new Bitmap(width, height);
            if (points != null)
            {
                Graphics testG = Graphics.FromImage(test);
                testG.SmoothingMode = SmoothingMode.HighQuality;
                foreach (Tuple<Point, Point> tupPoints in points)
                {
                    DrawWave(testG, Pens.Gray, tupPoints.Item1, tupPoints.Item2);
                }
                //testG.DrawString("TTTTTTT", NumberFont, Brushes.Red, points.First().Item1);
                testG.Dispose();
            }
            return test;
        }

        private void DrawWave(Graphics bufferGraphics, Pen pen,  Point start, Point end)
        {
            //Pen pen = Pens.Red;
            if ((end.X - start.X) > 4)
            {
                var pl = new List<Point>();
                for (int i = start.X; i <= (start.X + end.X - start.X - 2); i += 4)
                {
                    pl.Add(new Point(i, 0/* start.Y*/));
                    pl.Add(new Point(i + 2, /*start.Y +*/ 2));
                }
                if (pl.Count() > 0)
                {
                    Point[] p = (Point[])pl.ToArray();
                    bufferGraphics.DrawLines(pen, p);
                }
            }
            else
            {
                bufferGraphics.DrawLine(pen, start, end);
            }
        }

        private int GetCharIndexFromPos(int x, int y)
        {
            var p = new User32.POINTL { X= x, Y = y };
            int rawSize = Marshal.SizeOf( typeof(User32.POINTL) );
            IntPtr lParam = Marshal.AllocHGlobal( rawSize );
            Marshal.StructureToPtr(p, lParam, false);
            int r = User32.SendMessage(this.Handle, (int)User32.Msgs.EM_CHARFROMPOS, 0, lParam);
            Marshal.FreeHGlobal( lParam );
            return r;
        }


        private Point GetPosFromCharIndex(int ix)
        {
            int rawSize = Marshal.SizeOf( typeof(User32.POINTL) );
            IntPtr wParam = Marshal.AllocHGlobal( rawSize );
            int r = User32.SendMessage(this.Handle, (int)User32.Msgs.EM_POSFROMCHAR, /*(int)wParam*/wParam, ix);

            User32.POINTL p1 = (User32.POINTL) Marshal.PtrToStructure(wParam, typeof(User32.POINTL));

            Marshal.FreeHGlobal( wParam );
            var p = new Point { X= p1.X, Y = p1.Y };
            return p;
        }


        private int GetLengthOfLineContainingChar(int charIndex)
        {
            int r = User32.SendMessage(this.Handle, (int)User32.Msgs.EM_LINELENGTH, 0,0);
            return r;
        }

        public int GetLineFromChar(int charIndex)
        {
            return User32.SendMessage(this.Handle, (int)User32.Msgs.EM_LINEFROMCHAR, charIndex, 0);
        }

        private int GetCharIndexForDisplayLine(int line)
        {
            return User32.SendMessage(this.Handle, (int)User32.Msgs.EM_LINEINDEX, line, 0);
        }

        private int GetDisplayLineCount()
        {
            return User32.SendMessage(this.Handle, (int)User32.Msgs.EM_GETLINECOUNT, 0, 0);
        }


        /// <summary>
        ///   Sets the color of the characters in the given range.
        /// </summary>
        ///
        /// <remarks>
        /// Calling this is equivalent to calling
        /// <code>
        ///   richTextBox.Select(start, end-start);
        ///   this.richTextBox1.SelectionColor = color;
        /// </code>
        /// ...but without the error and bounds checking.
        /// </remarks>
        ///
        public void SetSelectionColor(int start, int end, System.Drawing.Color color)
        {
            if (!IsDesginMode) User32.SendMessage(this.Handle, (int)User32.Msgs.EM_SETSEL, start, end);

            charFormat.dwMask = 0x40000000;
            charFormat.dwEffects = 0;
            charFormat.crTextColor = System.Drawing.ColorTranslator.ToWin32(color);

            Marshal.StructureToPtr(charFormat, lParam1, false);
            if (!IsDesginMode) User32.SendMessage(this.Handle, (int)User32.Msgs.EM_SETCHARFORMAT, User32.SCF_SELECTION, lParam1);
        }


        private void SetLeftMargin(int widthInPixels)
        {
            if (!IsDesginMode) User32.SendMessage(this.Handle, (int)User32.Msgs.EM_SETMARGINS, User32.EC_LEFTMARGIN,
                               widthInPixels);
        }

        public TupleNew<int,int> GetMargins()
        {
            int r = User32.SendMessage(this.Handle, (int)User32.Msgs.EM_GETMARGINS, 0,0);
            return TupleNew.New(r & 0x0000FFFF, (int)((r>>16) & 0x0000FFFF));
        }

        public void Scroll(int delta)
        {
            if (!IsDesginMode) User32.SendMessage(this.Handle, (int)User32.Msgs.EM_LINESCROLL, 0, delta);
        }


        private int FirstVisibleDisplayLine
        {
            get
            {
                return User32.SendMessage(this.Handle, (int)User32.Msgs.EM_GETFIRSTVISIBLELINE, 0, 0);
            }
            set
            {
                // scroll
                int current = FirstVisibleDisplayLine;
                int delta = value - current;
                User32.SendMessage(this.Handle, (int)User32.Msgs.EM_LINESCROLL, 0, delta);
            }
        }

        private int NumberOfVisibleDisplayLines
        {
            get
            {
                int topIndex = this.GetCharIndexFromPosition(new System.Drawing.Point(1, 1));
                int bottomIndex = this.GetCharIndexFromPosition(new System.Drawing.Point(1, this.Height - 1));
                int topLine = this.GetLineFromCharIndex(topIndex);
                int bottomLine = this.GetLineFromCharIndex(bottomIndex);
                int n = bottomLine - topLine + 1;
                return n;
            }
        }

        private int FirstVisibleTextLine
        {
            get
            {
                int c = GetCharIndexFromPos(1,1);
                for (int i=0; i < CharIndexForTextLine.Length; i++)
                {
                    if (c < CharIndexForTextLine[i]) return i;
                }
                return CharIndexForTextLine.Length;
            }
        }

        private int LastVisibleTextLine
        {
            get
            {
                int c = GetCharIndexFromPos(1,this.Bounds.Y+this.Bounds.Height);
                for (int i=0; i < CharIndexForTextLine.Length; i++)
                {
                    if (c < CharIndexForTextLine[i]) return i;
                }
                return CharIndexForTextLine.Length;
            }
        }

        private int NumberOfVisibleTextLines
        {
            get
            {
                return LastVisibleTextLine - FirstVisibleTextLine;
            }
        }


        public int FirstVisibleLine
        {
            get
            {
                if (this.NumberLineCounting == LineCounting.CRLF)
                    return FirstVisibleTextLine;
                else
                    return FirstVisibleDisplayLine;
            }
        }

        public int NumberOfVisibleLines
        {
            get
            {
                if (this.NumberLineCounting == LineCounting.CRLF)
                    return NumberOfVisibleTextLines;
                else
                    return NumberOfVisibleDisplayLines;
            }
        }

        public int GetCharIndexForTextLine(int ix)
        {
            if (ix >= CharIndexForTextLine.Length) return 0;
            if (ix < 0) return 0;
            return CharIndexForTextLine[ix];
        }



        // The char index is expensive to compute.

        private string[] _RealLines;
        private int[] _CharIndexForTextLine;
        private char[] _CharsForTextLine;
        private bool[] _DueDateErrors;
        private char[] CharsForTextLine
        {
            get
            {
                if (_CharsForTextLine == null)
                {
                    ProcessNewLines();
                }

                return _CharsForTextLine;
            }
        }
       private bool[] DueDateErrors
        {
            get
            {
                if (_DueDateErrors == null)
                {
                    ProcessNewLines();
                }

                return _DueDateErrors;
            }
        }

        private int[] CharIndexForTextLine
        {
            get
            {
                if (_CharIndexForTextLine == null)
                {
                    ProcessNewLines();
                }
                return _CharIndexForTextLine;
            }

        }

        

        internal void ProcessNewLines()
        {
            var list = new List<int>();
            var charlist = new List<char>();
            List<string> realLines = new List<string>();
            var listDateErros = new List<bool>();
            _DueDateErrors = listDateErros.ToArray();
            bool lastCharWasNewLine = false;
            int ix = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var c in Text2)
            {
                if (ix == 0 || lastCharWasNewLine) 
                { 
                    charlist.Add(c);
                    lastCharWasNewLine = false;
                }
                sb.Append(c);
                if (c == '\n')
                {
                    list.Add(ix);
                    string lineString = sb.ToString();
                    var dates = lineString.ExtractDueDates();
                    var lineDate = lineString.ExtractLineDate();
                    DateTime refDate = DocumentDate;
                    if (refDate < lineDate) refDate = lineDate;
                    if (dates != null && dates.Count() > 0)
                    {
                        // Even the least date should be GT or EQ to the document date.
                        listDateErros.Add(dates.OrderBy(s => s).First() < refDate);
                    }
                    else
                    {
                        listDateErros.Add(false);
                    }
                    realLines.Add(lineString.TrimEnd('\n'));
                    sb.Clear();
                    lastCharWasNewLine = true;
                }
                ix++;
            }
            if (sb.Length > 0)
            {
                realLines.Add(sb.ToString());
            }
            _CharIndexForTextLine = list.ToArray();
            _CharsForTextLine = charlist.ToArray();
            _RealLines = realLines.ToArray();
            _DueDateErrors = listDateErros.ToArray();
            if (HasDueDateErrors)
                this.BackColor = Color.LightGoldenrodYellow;
            else
                this.BackColor = Color.White;
        }

        public bool GotoRealLine(int index)
        {
            if (index < _RealLines.Count())
            {
                string line = _RealLines[index];
                FindAndSelect(line);
            }

            return false;
        }

        public void FindAndSelect(string text)
        {
            //int index = Find(text, RichTextBoxFinds.MatchCase);
            int index = this.FindText(text);
            if (index >= 0)
            {
                Select(index, text.Length);
                ScrollToCaret();
            }
        }

        public IEnumerable<string> RealLines
        {
            get
            {
                if (_RealLines == null)
                {
                    ProcessNewLines();
                }
                return _RealLines;
            }
        }

        public int GetRealLineForCharIndex(int charIndex)
        {
            //int c = (NumberLineCounting == LineCounting.CRLF)
            //        ? GetCharIndexForTextLine(ix)
            //        : GetCharIndexForDisplayLine(ix) - 1;
            if (NumberLineCounting == LineCounting.CRLF)
            {
                return CharIndexForTextLine.Where(s => s <= charIndex).Count();
            }
            else
            {
                return base.GetLineFromCharIndex(charIndex);
            }
        }

        private String _Text2;
        private String Text2
        {
            get
            {
                if (_Text2 == null)
                    _Text2 = this.Text;
                return _Text2;
            }
        }

        private bool CompareHashes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i=0; i < a.Length; i++)
            {
                if (a[i]!=b[i]) return false;
            }
            return true;  // they are equal
        }



        public enum LineCounting
        {
            CRLF,
            AsDisplayed
        }

    }



    public static class TupleNew
    {
        // Allows Tuple.New(1, "2") instead of new Tuple<int, string>(1, "2")
        public static TupleNew<T1, T2> New<T1, T2>(T1 v1, T2 v2)
        {
            return new TupleNew<T1, T2>(v1, v2);
        }
    }

    public class TupleNew<T1, T2>
    {
        public TupleNew(T1 v1, T2 v2)
        {
            V1 = v1;
            V2 = v2;
        }

        public T1 V1 { get; set; }
        public T2 V2 { get; set; }
    }

}


