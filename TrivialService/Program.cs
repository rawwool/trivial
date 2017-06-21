using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace TrivialService
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new ServiceHost(typeof(TrivialServiceLibrary.TrivialService));
            host.Open();
            Console.WriteLine("Service is live now. Listening at http://localhost:4680/TrivialService/ITrivialService/");
             Console.ReadKey(); 
        }
    }
}
