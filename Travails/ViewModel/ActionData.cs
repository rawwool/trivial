using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using TrivialModel;

namespace Travails.Model
{
    public class ActionDO: DependencyObject
    {
        public ActionDO(ActionData data)
        {
            Title = Abbreviate(data.Name, 60);
            Date = data.DateLogged;
            if (data.DateDue > DateTime.Now)
            {
                Fill = Brushes.Green;
                Color = Colors.Green;
            }
            else
            {
                Fill = Brushes.Red;
                Color = Colors.Red;
            }
        }

        public bool IsRed
        {
            get { return Color == Colors.Red; }
        }

        public bool DoAnimate
        {
            get { return Color == Colors.Red; }
            set { ;}
        }

        private string Abbreviate(string p1, int p2)
        {
            if (p1.Length > p2)
            {
                return p1.Substring(0, p2 - 3) + "...";
            }

            return p1;
        }

        // Note how the TitleProperty
        // is a DependencyProperty,
        // and the Title CLR property is
        // here just for convenience. The string itself
        // is not stored in the object, it's
        // stored in the WPF framework.

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string),
              typeof(ActionDO), new UIPropertyMetadata(""));

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush),
              typeof(ActionDO), new PropertyMetadata(default(Brush)));

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color),
              typeof(ActionDO), new PropertyMetadata(default(Color)));

        public DateTime Date
        {
            get { return (DateTime)GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register("Date", typeof(DateTime),
              typeof(ActionDO), new PropertyMetadata(default(DateTime)));



        public static readonly DependencyProperty MyCustomCommandProperty =
       DependencyProperty.Register("MyCustomCommand", typeof(ICommand), typeof(Dashboard));

        private static RoutedCommand myCustomCommand;

        public ICommand MyCustomCommand
        {
            get
            {
                if (myCustomCommand == null)
                {
                    myCustomCommand = new RoutedCommand("MyCustomCommand", typeof(Dashboard));

                    var binding = new CommandBinding();
                    binding.Command = myCustomCommand;
                    binding.Executed += binding_Executed;

                    CommandManager.RegisterClassCommandBinding(typeof(Dashboard), binding);
                }
                return myCustomCommand;
            }
        }

        private static void binding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //MessageBox.Show("Command Handled!");
            var values = (object[])e.Parameter;
            if (values == null) return;
            ////////var date = (DateTime)values[0];
            var title = (string)values[0];
            var date = (DateTime)values[1];
            DataProvider.ShowAction(date, title.TrimEnd('.'));
        }

    }

 }
