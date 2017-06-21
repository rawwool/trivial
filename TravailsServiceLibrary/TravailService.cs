using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

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

        public Action[] GetActions(string max)
        {
            int iMax = 0;
            if (Int32.TryParse(max, out iMax))
            {
            }
            return new Action[] { new Action() { Name = "To buy presents for Rio", DueDate = DateTime.Now },
                new Action() { Name = "To buy toys for Isha", DueDate = DateTime.Now.AddDays(3) }};
        }
    }
}
