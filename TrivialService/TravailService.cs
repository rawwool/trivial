using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Travails.Model;
using TrivialModel;

namespace TrivialService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class TravailService : ITravailService
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        //public CompositeType GetDataUsingDataContract(CompositeType composite)
        //{
        //    if (composite == null)
        //    {
        //        throw new ArgumentNullException("composite");
        //    }
        //    if (composite.BoolValue)
        //    {
        //        composite.StringValue += "Suffix";
        //    }
        //    return composite;
        //}

        public string SayHello()
        {
            return "Hello and welcome to Travail service!";
        }

        public ActionData[] GetActions(string max)
        {
            int iMax = 0;
            //if (Int32.TryParse(max, out iMax))
            //{
            //    var a = DataProvider.GetActions(iMax);
            //    return a;
            //}
            return new ActionData[] { new ActionData() { Name = "No actions" },
                };
        }


        public ActionData[] GetFutureActions(string max)
        {
            int iMax = 0;
            //if (Int32.TryParse(max, out iMax))
            //{
            //    //Console.WriteLine(iMax);
            //    var a = DataProvider.GetFutureActions(iMax);
            //    return a;
            //}
            return new ActionData[] { new ActionData() { Name = "No actions" },
                };
        }


        public bool ShowAction(string datestring, string name)
        {
            //long date;
            //try
            //{
            //    if (Int64.TryParse(datestring, out date))
            //    {
            //        return DataProvider.ShowAction(new DateTime(date), Recover(name.TrimEnd('.')));
            //    }
            //}
            //catch
            //{
            //}

            return false;
        }

        private static string Recover(string title)
        {
            return title.Replace('֎', '/').Replace('Φ', '#');
        }


        //public Travails.Model.DataProvider.ResultLines[] GetDateLines(string tag)
        //{
        //    return DataProvider.GetLinesWithHashTag(tag).ToArray();
        //}

        class Job
        {
            public string JobId { get; set; }
            public DateTime DateTimeCreated { get; set; }
            public string RequesterId { get; set; }
            public string ServiceName { get; set; }
            public List<Tuple<string,string>> Parameters { get; set; }

            public Job()
            {
                JobId = Guid.NewGuid().ToString();
                DateTimeCreated = DateTime.UtcNow;
            }

            public override string ToString()
            {
                return string.Format("{4} {3} {0} {1} {2}", RequesterId, ServiceName, 
                    Parameters == null ? string.Empty :
                    Parameters.Select(s=>s.Item1 + "=" + s.Item2).Aggregate((a,b)=>a+","+b),
                    DateTimeCreated,
                    JobId);
            }
        }

        List<Job> _Jobs = new List<Job>();
        List<TrivialResponse> _Responses = new List<TrivialResponse>();

        public TrivialResponse Invoke(TrivialRequest request)
        {
            lock (this)
            {
                if (request.IsValid)
                {
                    var job = _Jobs.OrderBy(s => s.DateTimeCreated).FirstOrDefault(s => s.RequesterId == request.RequesterId && s.ServiceName == request.ServiceName);
                    if (request.RequestType == RequestType.RequestData)
                    {

                        if (job != null)
                        {
                            var response = _Responses.FirstOrDefault(s => s.ClientId == request.RequesterId && s.JobId == job.JobId);
                            if (response != null)
                            {
                                _Responses.Remove(response);
                                return response;
                            }
                        }
                        else
                        {
                            job = new Job() { RequesterId = request.RequesterId, ServiceName = request.ServiceName };
                            if (request.NameValueParameters != null)
                            {
                                job.Parameters = request.NameValueParameters;
                                _Jobs.Add(job);
                            }
                        }
                    }
                    else if (request.RequestType == RequestType.SupplyData)
                    {
                        var response = _Responses.FirstOrDefault(s => s.ClientId == request.RequesterId && s.JobId == job.JobId);
                        if (response != null)
                        {
                            response.Results = request.NameValueParameters[0].Item2;
                        }
                    }


                    return null;// new TrivialResponse() { ClientId = clientId, Results = new List<string>() { job.ToString() } };
                }
                return null;
            }
        }


        public TrivialModel.Model.ResultLines[] GetDateLines(string tag)
        {
            return DataProvider.GetLinesWithHashTag(tag).ToArray();
        }
    }
}
