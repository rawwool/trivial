using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Travails.Model;
using TrivialModel;
using TrivialModel.Model;

namespace TravailsServiceLibrary
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class TravailService : ITravailService
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        public string SayHello()
        {
            return "Hello and welcome to Travail service!";
        }

        public ActionData[] GetActions(string max)
        {
            int iMax = 0;
            if (Int32.TryParse(max, out iMax))
            {
                var a = DataProvider.GetActions(iMax);
                return a;
            }
            return new ActionData[] { new ActionData() { Name = "No actions" },
                };
        }


        public ActionData[] GetFutureActions(string max)
        {
            int iMax = 0;
            if (Int32.TryParse(max, out iMax))
            {
                var a = DataProvider.GetFutureActions(iMax);
                return a;
            }
            return new ActionData[] { new ActionData() { Name = "No actions" },
                };
        }


        public bool ShowAction(string datestring, string name)
        {
            long date;
            try
            {
                if (Int64.TryParse(datestring, out date))
                {
                    return  DataProvider.ShowAction(new DateTime(date), Recover(name.TrimEnd('.')));
                }
            }
            catch
            {
            }

            return false;
        }

        private static string Recover(string title)
        {
            return title.Replace('֎', '/').Replace('Φ', '#');
        }


        public ResultLines[] GetDateLines(string tag)
        {
            return DataProvider.GetLinesWithHashTag(tag).ToArray();
        }
    }
}
