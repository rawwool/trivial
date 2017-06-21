using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Travails.Model;
using TrivialModel;

namespace Travails
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<ActionDO> _Actions = new ObservableCollection<ActionDO>();
        public ObservableCollection<ActionDO> Actions
        {
            get { return _Actions; }
        }

        System.Windows.Threading.DispatcherTimer _DispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        public Dashboard()
        {
            InitializeComponent();
             RefreshActions();
            _DispatcherTimer.Tick += _DispatcherTimer_Tick;
            _DispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            _DispatcherTimer.Start();
        }

        async void _DispatcherTimer_Tick(object sender, EventArgs e)
        {
            await RefreshActions();
        }

        private async Task RefreshActions()
        {
            var actions = await DataProvider.GetFutureActionsAsync(100);
           
            Actions.Clear();
            foreach (ActionData action in actions)
            {
                Actions.Add(new ActionDO(action));
            }
        }

        private string Abbreviate(string p1, int p2)
        {
            if (p1.Length > p2)
            {
                return p1.Substring(0, p2 - 3) + "...";
            }

            return p1;
        }
    }
}
