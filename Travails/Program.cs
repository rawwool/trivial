using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Windows.Forms;

namespace Travails
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //var host = new ServiceHost(typeof(TravailsServiceLibrary.TravailService));
            //host.Open();

            Application.EnableVisualStyles();
            //Application.ThreadException += Application_ThreadException;    
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new Form1());
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        //{
        //    MessageBox.Show(e.Exception.Message);
        //}
    }
}
