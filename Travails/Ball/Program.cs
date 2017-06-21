using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Ball
{
    public static class Bouncer
    {
        static BallForm _BallForm = null;
        static bool _HideRequested = false;
        static object lockObject = new object();
        public static void Bounce()
        {
            if (_BallForm == null)
            {
                _BallForm = new BallForm();
            }
            lock (lockObject)
            {
                _HideRequested = false;
            }
            try
            {
                /*
                _BallForm.BeginInvoke(
                            (Action)(() =>
                            {
                                //_BallForm.Reset();
                                _BallForm.Show();
                                Application.DoEvents();
                                while (true && !_HideRequested)
                                {
                                    _BallForm.Tick();
                                    Application.DoEvents();
                                    Thread.Sleep(10);
                                }
                            }));
                 */
                _BallForm.Show();
                Application.DoEvents();
                while (true && !_HideRequested)
                {
                    _BallForm.Tick();
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
            }
            catch { }
        }

        internal static void Hide()
        {
            lock (lockObject)
            {
                try
                {
                    _HideRequested = true;
                    if (_BallForm != null && _BallForm.Handle != IntPtr.Zero/* && !_BallForm.IsDisposed*/)
                    {
                        /*
                        _BallForm.BeginInvoke(
                            (Action)(() =>
                            {
                                if (_BallForm != null)
                                {
                                    _BallForm.Hide(); _BallForm.Close(); _BallForm.Dispose(); _BallForm = null;
                                }
                            }));
                         */
                        if (_BallForm != null)
                        {
                            _BallForm.Hide(); _BallForm.Close(); _BallForm.Dispose(); _BallForm = null;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
