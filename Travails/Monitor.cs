using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Travails
{
    public partial class Monitor : Form
    {
        Dashboard dashboard = new Dashboard();
        public Monitor()
        {
            InitializeComponent();
           // this.elementHost1.Controls.Add(new Dashboard());
            
            this.elementHost1.Child = dashboard;
            this.elementHost1.Dock = DockStyle.Fill;
            
            
        }

        public Model.DataProvider DataProvider { get; set; }

        private void Monitor_Load(object sender, EventArgs e)
        {
            this.Left = (int)SystemParameters.FullPrimaryScreenWidth - this.Width;
            this.Top = (int)SystemParameters.FullPrimaryScreenHeight - this.Height;
        }
    }
}
