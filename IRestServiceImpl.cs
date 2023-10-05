/** 
 * Rest Service for web portal and mobile apps - Author Nick Paton 2015
 * 
 * Program History 
 * Date        Programmer           Description
 * 22.05.17  - Joshua Jusuf         Add getBuyerTransactions, retrieveSubAccount methods.
                                    Create an overload method for JSONData to include subUserNo parameter.
 * 02.07.18  - Nick Paton           Add buyer account detail endpoint
 *                                  Add separate endpoint for gentnextCatalogue(user) to be used by buyer App
 * Addition of product removal and crate balances
 * 17.10.22 - Nick Paton            Add SFMblue credit reservations to buyACcountData
 **/

using System.Collections.Generic;
using System.Net.Mime;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace RestService
{
   
    /***************NEW FOR MANIFESTING *******************/
    public class species
    {
        public string speciesCode { get; set; }
        public string abbrev { get; set; }
        public string descript { get; set; }
    }

    public class processType
    {
        public string processCode { get; set; }
        public string processDesc { get; set; }
    }

    public class size
    {
        public string sizeCode { get; set; }
        public string sizeDesc { get; set; }
        public double sortOrder { get; set; }
    }

    public class qualityGrade
    {
        public string qualityCode { get; set; }
    }


    /*******************************************************/


    public class order
    {
        public double transId { get; set; }
        public double stockId { get; set; }
        public string shortDesc { get; set; }
        public double orderQty { get; set; }
        //public double sfmUserNo { get; set; }
        public string transDate { get; set; }
        public string statusFlg { get; set; }
        public string comment { get; set; }
        public double price { get; set; }
        public double weight { get; set; }
        public string priceUnit { get; set; }
    }

    public class stock
    {
        public double stockId { get; set; }
        public string shortDesc { get; set; }
        public string longDesc { get; set; }
        public string stockAvailability { get; set; }
        public double price { get; set; }
        public double weight { get; set; }
        public string priceUnit { get; set; }
        public string location { get; set; }
        public string orderByDate { get; set; }
        public string orderByTime { get; set; }
        public string deliveryDate { get; set; }
        public string logoLink { get; set; }
        public string saleType { get; set; }
    }

    public class buyerTransactions
    {
        public string abbrev { get; set; }
        public string prCode { get; set; }
        public string sizeCode { get; set; }
        public string condition { get; set; }
        public double wgtPerCrate { get; set; }
        public double boxes { get; set; }
        public double price { get; set; }
        public double grossValue { get; set; }
        public string shortName { get; set; }
        public string saleType { get; set; }
    }

    public class salesData
    {
        public string abbrev { get; set; }
        public string prCode {get;set;}
        public string sizeCode { get; set; }
        public string condition { get; set; }
        public double boxes { get; set; }
        public double kgs { get; set; }
        public double minPrice { get; set; }
        public double maxPrice { get; set; }
        public double avePrice { get; set; }
        public double grossValue { get; set; }
    }

     public class supplyData
    {
        public string abbrev { get; set; }
        public string prCode { get; set; }
        public string sizeCode { get; set; }
        public string condition { get; set; }
        public double boxes { get; set; }
        public double kgs { get; set; }
    }

    public class clientMessage
    {
        public double msgId { get; set; }
        public string dateStart { get; set; }
        public string individualFlg { get; set; }
        public string messageText { get; set; }
    }
    
    public struct LoginReturnResult
    {
        public string status;
        public string token;
        public string tradingName;
        public string category;
    }

    public struct subAccountData
    {
        public string userNo { get; set; }
        public string tradingName { get; set; }
        public string userType { get; set; }
        public string userRole { get; set; }
    }

    public struct buyAccountData
    {
        public string responseCode { get; set; }
        public string token { get; set; }
        public double yesterdayBal { get; set; }
        public double currentBal { get; set; }
        public double sevenDayBal { get; set; }
        public double forteenDayBal { get; set; }
        public double twentyOneDayBal { get; set; }
        public double twentyEightDayBal { get; set; }
        public double todaysPurchases { get; set; }
        public double todaysPayments { get; set; }
        public double creditLimit { get; set; }
        public double totalBal { get; set; }
        public double crateBalLarge { get; set; }
        public double crateBalSmall { get; set; }
        public double crateBalLidded { get; set; }
        public double creditReservations{ get; set; }

    }

     public class buyerRemovalProduct
    {
        public string labelNo { get; set; }
        public string supName {get;set;}
        public string speciesAbbrev { get; set; }
        public string processCode { get; set; }
        public string sizeCode { get; set; }
        public double weight { get; set; }
        public string crateTypeDesc { get; set; }
        public string auctArea { get; set; }
        public string auctPosition { get; set; }
        public string removed { get; set; }
        public string seized { get; set; }
        public string coldStorage { get; set; }
    }

    public struct orderReturnResult
    {
        public string responseCode { get; set; }
        public string price { get; set; }
        public string weight { get; set; }
        public string token { get; set; }
    }

    [ServiceContract]
    public interface IRestServiceImpl
    {
        /* [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "json/{userNo}/{lId}")] */

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "chkUser")]
        LoginReturnResult chkUser(string userNo, string lId);

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "JSONData")]
        List<salesData> JSONData(string userNo, string lId, string queryDate, string queryType, string token);

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getMessages")]
        List<clientMessage> getMessages(string userNo, string lId, string token);
        
        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getNextCatalogue")]
        List<supplyData> getNextCatalogue(string token);

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getNextCatalogueUSer")]
        List<supplyData> getNextCatalogueUser(string userNo, string subUserNo, string lId, string token);

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "retrieveSubAccount")]
        List<subAccountData> retrieveSubAccount(string userNo, string lId, string token);

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getBuyerTransactions")]
        List<buyerTransactions> getBuyerTransactions(string userNo, string subUserNo, string lId, string queryDate, string token);

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getBuyerRemovalProduct")]
        List<buyerRemovalProduct> getBuyerRemovalProduct(string userNo, string subUserNo, string lId, string removedFlag, string token);

        /********************************* FOR MANIFESTING ON SUPPLIER APP ***********************/
        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getSpecies")]
        List<species> getSpecies();

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getProcesses")]
        List<processType> getProcesses();

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getSizes")]
        List<size> getSizes();

        [OperationContract]
        [WebInvoke(Method = "POST",
           RequestFormat = WebMessageFormat.Json,
           ResponseFormat = WebMessageFormat.Json,
           BodyStyle = WebMessageBodyStyle.Wrapped,
           UriTemplate = "getGrades")]
        List<qualityGrade> getGrades();

        /*** Account data for buyer App and web portal ***/
        [OperationContract]
        [WebInvoke(Method = "POST",
           RequestFormat = WebMessageFormat.Json,
           ResponseFormat = WebMessageFormat.Json,
           BodyStyle = WebMessageBodyStyle.Wrapped,
           UriTemplate = "getBuyAccountDtl")]
        List<buyAccountData> getBuyAccountDtl(string userNo, string subUserNo, string lId, string token);

        
        [OperationContract]
        [WebInvoke(Method = "POST",
         RequestFormat = WebMessageFormat.Json,
         ResponseFormat = WebMessageFormat.Json,
         BodyStyle = WebMessageBodyStyle.Wrapped,
         UriTemplate = "getOrders")]
        List<order> getOrders(string userNo, string subUserNo, string lId, string token);
        
        [OperationContract]
        [WebInvoke(Method = "POST",
        RequestFormat = WebMessageFormat.Json,
        ResponseFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.Wrapped,
        UriTemplate = "getStock")]
        List<stock> getStock(string userNo, string subUserNo, string lId, string token);
        
        [OperationContract]
        [WebInvoke(Method = "POST",
        RequestFormat = WebMessageFormat.Json,
        ResponseFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.Wrapped,
        UriTemplate = "putOrder")]
        orderReturnResult putOrder(string userNo, string subUserNo, string lId, double stockId, double orderQty, string token);    
    }
}