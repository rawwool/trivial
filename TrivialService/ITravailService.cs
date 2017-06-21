using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using TrivialModel;
using TrivialModel.Model;

namespace TrivialService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ITravailService" in both code and config file together.
    [ServiceContract]
    public interface ITravailService
    {
        [OperationContract]
        [WebGet(UriTemplate = "Hello", ResponseFormat = WebMessageFormat.Json)]
        string SayHello();

        [OperationContract]
        [WebGet(UriTemplate = "GetActions/{max}", ResponseFormat = WebMessageFormat.Json)]
        ActionData[] GetActions(string max);

        [OperationContract]
        [WebGet(UriTemplate = "GetFutureActions/{max}", ResponseFormat = WebMessageFormat.Json)]
        ActionData[] GetFutureActions(string max);

        [OperationContract]
        [WebGet(UriTemplate = "ShowAction/{date}/{name}", ResponseFormat = WebMessageFormat.Json)]
        bool ShowAction(string date, string name);

        [OperationContract]
        [WebGet(UriTemplate = "GetDateLines/{tag}", ResponseFormat = WebMessageFormat.Json)]
        ResultLines[] GetDateLines(string tag);

        //[OperationContract]
        //[WebInvoke(UriTemplate = "Invoke/{clientId}/{serviceName}/{*commaSeparatedNameValueParameters}"
        //    , ResponseFormat = WebMessageFormat.Json
        //    ,RequestFormat=WebMessageFormat.Json)]
        //TrivialResponse Invoke(TrivialRequest request);

        //[OperationContract]
        //string GetData(int value);

        //[OperationContract]
        //CompositeType GetDataUsingDataContract(CompositeType composite);

        // TODO: Add your service operations here
    }

    [DataContract]
    public class TrivialResponse
    {
        [DataMember]
        public string JobId { get; set; }
        [DataMember]
        public string ClientId { get; set; }
        [DataMember]
        public string Results { get; set; }
    }

    [DataContract]
    public class TrivialRequest
    {
        [DataMember]
        public string RequesterId { get; set; }
        [DataMember]
        public string ServiceName { get; set; }
        [DataMember]
        public RequestType RequestType { get; set; }
        [DataMember]
        public List<Tuple<string,string>> NameValueParameters { get; set; }

        public bool IsValid
        {
            get { return RequesterId != null && RequesterId.Length > 0 && ServiceName != null && ServiceName.Length > 0; }
        }
    }

    public enum RequestType
    {
        RequestData,
        SupplyData
    }

    //[DataContract]
    //public class Action
    //{
    //    [DataMember]
    //    public string Name { get; set; }
    //    [DataMember]
    //    public DateTime DueDate { get; set; }
    //}

    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    // You can add XSD files into the project. After building the project, you can directly use the data types defined there, with the namespace "TravailsServiceLibrary.ContractType".
    //[DataContract]
    //public class CompositeType
    //{
    //    bool boolValue = true;
    //    string stringValue = "Hello ";

    //    [DataMember]
    //    public bool BoolValue
    //    {
    //        get { return boolValue; }
    //        set { boolValue = value; }
    //    }

    //    [DataMember]
    //    public string StringValue
    //    {
    //        get { return stringValue; }
    //        set { stringValue = value; }
    //    }
    //}
}
