using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using TrivialModel;
using TrivialWPF.Model;

namespace TrivialWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ActionDO> _Actions = new ObservableCollection<ActionDO>();
        public ObservableCollection<ActionDO> Actions
        {
            get { return _Actions; }
        }

        System.Windows.Threading.DispatcherTimer _DispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Deactivated += MainWindow_Deactivated;

            _DispatcherTimer.Tick += _DispatcherTimer_Tick;
            _DispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            _DispatcherTimer.Start();

            CustomHeight = 400;
            Height = 200;//Why??? when the above is data bound in XAML?
        }

        public event PropertyChangedEventHandler PropertyChanged;
        //private double _height;
        //public double CustomHeight
        //{
        //    get { return _height; }
        //    set
        //    {
        //        if (value != _height)
        //        {
        //            _height = value;
        //            if (PropertyChanged != null)
        //                PropertyChanged(this, new PropertyChangedEventArgs("CustomHeight"));
        //        }
        //    }
        //}

        public double CustomHeight
        {
            get { return (double)GetValue(CustomHeightProperty); }
            set 
            { 
                SetValue(CustomHeightProperty, value); 
                if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("CustomHeight"));
            }
        }

        public static readonly DependencyProperty CustomHeightProperty =
           DependencyProperty.Register("CustomHeight", typeof(double),
             typeof(MainWindow), new PropertyMetadata(default(double)));

       

        async void _DispatcherTimer_Tick(object sender, EventArgs e)
        {
            await RefreshActions();
        }

        void MainWindow_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
            //window.Activate();
        }

        async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshActions();
        }

        private async Task RefreshActions()
        {
            //ActionPanel.Items.Clear();
            this.CustomHeight = 200;
            Height = 200;//Why when the above is data bound?
            this.Left = SystemParameters.FullPrimaryScreenWidth - this.Width;
            this.Top = SystemParameters.FullPrimaryScreenHeight - this.Height;

            string json = await DataProvider.GetActionsAsync();
            var c = new JsonSerializer();
            dynamic jsonObject = c.Deserialize(new StringReader(json), typeof(ActionData[]));
            if (jsonObject == null) return;
            Actions.Clear();
            foreach (ActionData action in jsonObject)
            {
                Actions.Add(new ActionDO(action));
                /*
                TextBlock textBlock = new TextBlock(new Run(Abbreviate(action.Name, 50)));
                textBlock.Height = 45;
                textBlock.Background = Brushes.AntiqueWhite;
                if (action.DateDue > DateTime.Now)
                {
                    textBlock.Foreground = Brushes.Navy;
                }
                else
                {
                    textBlock.Foreground = Brushes.DarkRed;
                }

                textBlock.FontFamily = new FontFamily("Segoe UI");
                textBlock.FontSize = 15;
                //textBlock.FontStretch = FontStretches.UltraExpanded;
                textBlock.FontStyle = FontStyles.Normal;
                textBlock.FontWeight = FontWeights.UltraBold;

                //textBlock.LineHeight = Double.NaN;
                //textBlock.Padding = new Thickness(5, 10, 5, 10);
                textBlock.TextAlignment = TextAlignment.Left;
                textBlock.TextWrapping = TextWrapping.Wrap;

                textBlock.Typography.NumeralStyle = FontNumeralStyle.OldStyle;
                textBlock.Typography.SlashedZero = true;
                textBlock.Typography.CapitalSpacing = true;

                textBlock.ToolTip = action.Name;
                textBlock.Tag = action;
                textBlock.MouseLeftButtonUp += textBlock_MouseLeftButtonUp;

                //ActionPanel.Items.Add(textBlock);
                height += textBlock.Height;

                */
            }
        }

        async void textBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender != null && sender is TextBlock && (sender as TextBlock).Tag is ActionData)
            {
                ActionData actionData = (sender as TextBlock).Tag as ActionData;
                await DataProvider.ShowAction(actionData);
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

        private void ActionPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
