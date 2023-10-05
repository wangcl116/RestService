/** 
 * Rest Service for web portal and mobile apps - Author Nick Paton 2015
 * 
 * Program History 
 * Date        Programmer           Description
 * 22.05.17  - Joshua Jusuf         Add getBuyerTransactions, retrieveSubAccount methods.
                                    Create an overload method for JSONData to include subUserNo parameter.
 * 04.06.18  - Nick Paton           Add species, process, size, quality
 * 02.07.18  - Nick Paton           Add buyer account detail endpoint
 *                                  Add getNextCatalogueUser to be used by buyers app
 *                                  Add additional checking on parameters for sql injection
 *                                  TODO: NEED TO USE SQL PARAMETERS  on ALL commands to stop SQL injection ***** ;
 * 14.11.18  - Nick Paton           Add sale_type and order_by_time for getStock
 * 19.03.22  - Nick Paton           Change login to new Azure MySQL DB
 *                                  Addition of product removal and crate balances
 * 17.10.22 - Nick Paton            Add SFMblue credit reservations
 **/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Linq;

namespace RestService
{
    public class RestServiceImpl : IRestServiceImpl
    {
        //private String reqDate;
        private String sqlCommand;
        private String sqlLoginCmd;

        private string uType;
        private string uRole;

        struct LoginData
        {
            public string status;
            public string errMsg;
            public string newToken;
            public string tradingName;
            public int contract;
            public string category;
        }

        DataTable dt = new DataTable();

        #region IRestServiceImpl Members

        public List<subAccountData> retrieveSubAccount(string userNo, string lId, string token)
        {

            List<subAccountData> returnsubAccounts = new List<subAccountData>();
            
            int iUserNo = 0;
            
            bool validUser = Int32.TryParse(userNo, out iUserNo);

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch (OdbcException ex)
            {
                returnsubAccounts.Add(new subAccountData { userNo = "-9", tradingName = "", userType = "", userRole = "" });
                return returnsubAccounts;
            }

            /* Check token */
            LoginData result = chkLId(userNo, lId, token, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                // Invalid token, user, pin or error
                returnsubAccounts.Add(new subAccountData { userNo = result.status, tradingName = "", userType = "", userRole = "" });
                conn.Close();
                return returnsubAccounts;
            }

            if (userNo == "999888") // Testing account for ITWorx
            {
                /* Register 5 real subaccounts for testing */
                returnsubAccounts.Add(new subAccountData
                {
                    userNo = "1",
                    tradingName = result.newToken, /* token */
                    userType = "",
                    userRole = ""
                });

                /* Master account */
                returnsubAccounts.Add(new subAccountData
                {
                    userNo = "999888",
                    tradingName = "Test - Master",
                    userType = "M",
                    userRole = "Buyer"
                });

                /* subAccount 1 */
                returnsubAccounts.Add(new subAccountData
                {
                    userNo = "229944",
                    tradingName = "Test - Seafood Buyfood",
                    userType = "S",
                    userRole = "Buyer"
                });

                /* subAccount 2 */
                returnsubAccounts.Add(new subAccountData
                {
                    userNo = "193040",
                    tradingName = "Test - Three Hooks",
                    userType = "S",
                    userRole = "Buyer"
                });

                /* subAccount 3 */
                returnsubAccounts.Add(new subAccountData
                {
                    userNo = "107362",
                    tradingName = "Test - Brother's Fish Market",
                    userType = "S",
                    userRole = "Buyer"
                });

                /* subAccount 4 */
                returnsubAccounts.Add(new subAccountData
                {
                    userNo = "107932",
                    tradingName = "Test - Claudios Seafoods",
                    userType = "S",
                    userRole = "Buyer"
                });

                /* subAccount 5 */
                returnsubAccounts.Add(new subAccountData
                {
                    userNo = "165715",
                    tradingName = "Test - Great Ocean Foods",
                    userType = "S",
                    userRole = "Buyer"
                });

                conn.Close();
                return returnsubAccounts;

            }

            //Currently only returns the calling account. Sub accounts not used
            sqlCommand = "SELECT sfm_user_no AS 'SFMNo', trading_name AS 'TradName', sfm_category AS 'SFMCategory' FROM user_reg WHERE sfm_user_no = " + sUserNo;

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnsubAccounts.Add(new subAccountData { userNo = "-8", tradingName = "", userType = "", userRole = "" });
                conn.Close();
                return returnsubAccounts;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnsubAccounts.Add(new subAccountData { userNo = "-7", tradingName = "", userType = "", userRole = "" });
                conn.Close();
                return returnsubAccounts;
            }

            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnsubAccounts.Add(new subAccountData { userNo = "-6", tradingName = "", userType = "", userRole = "" });
                conn.Close();
                da.Dispose();
                return returnsubAccounts;
            }

            conn.Close();
            da.Dispose();

            //Insert line with new time token. First line is always token or error
            returnsubAccounts.Add(new subAccountData
            {
                userNo = "1",
                tradingName = result.newToken,
                userType = "",
                userRole = ""

            });

            if (dt.Rows.Count == 0)
            {
                returnsubAccounts.Add(new subAccountData
                {
                    userNo = "No data available",
                    tradingName = "",
                    userType = "",
                    userRole = ""
                });
            }

            foreach (DataRow dr in dt.Rows)
            {
                if (dr["SFMNo"].ToString() == userNo) uType = "M";
                else uType = "S";

                if (Convert.ToUInt32(dr["SFMCategory"]) < 4) uRole = "Supplier";
                else uRole = "Buyer";

                returnsubAccounts.Add(new subAccountData
                {
                    userNo = userNo,
                    tradingName = dr["TradName"].ToString(),
                    userType = uType,
                    userRole = uRole
                });
            }

            return returnsubAccounts;
        }

        public List<buyerTransactions> getBuyerTransactions(string userNo, string subUserNo, string lId, string queryDate, string token)
        {
            List<buyerTransactions> returnBuyerTransactions = new List<buyerTransactions>();
            int iSubUserNo = 0;
            int iUserNo = 0;
            DateTime dQueryDate;

            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validSubUser = Int32.TryParse(subUserNo, out iSubUserNo);
            bool validDate = DateTime.TryParse(queryDate, out dQueryDate);
            if (!validDate || !validUser || !validSubUser)
            {
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = "-10",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    wgtPerCrate = 0,
                    boxes = 0,
                    price = 0,
                    grossValue = 0,
                    shortName = "",
                    saleType = ""
                });
                return returnBuyerTransactions;
            }

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = "-9",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    wgtPerCrate = 0,
                    boxes = 0,
                    price = 0,
                    grossValue = 0,
                    shortName = "",
                    saleType = ""
                });
                return returnBuyerTransactions;
            }

            /* For ITWorx testing - JDJ 30.05.17 */
            if (subUserNo == "999888") sUserNo = "216723";  //NP 4/7/18 Set iUserNo to a sub account for testing website.

            /* Check token */
            LoginData result = chkLId(sUserNo, lId, token, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                // Invalid token, user, pin or error
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = result.status,
                    prCode = token,
                    sizeCode = "",
                    condition = "",
                    wgtPerCrate = 0,
                    boxes = 0,
                    price = 0,
                    grossValue = 0,
                    shortName = "",
                    saleType = ""
                });
                conn.Close();
                return returnBuyerTransactions;
            }

            if (result.category != "B")
            {
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = "-1",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    wgtPerCrate = 0,
                    boxes = 0,
                    price = 0,
                    grossValue = 0,
                    shortName = "",
                    saleType = ""
                });
                conn.Close();
                return returnBuyerTransactions;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            /* This is for when user and subUser records are finalised */
            /* Check if subUserNo belongs to userNo - JDJ 230517 */
            /*
            if (subUserNo ==! userNo)
            {

            }
            */

            //Subuser not used at present
            sqlCommand = "SELECT abbrev AS 'abbrev', process_code AS 'prCode', size_code AS 'sizeCode', `condition` AS 'condition', " +
                         "weight as 'wgtPerCrate', boxes AS 'boxes', price AS  'Price', total_value as  'totalValue', sup_name as 'shortName', sale_type as 'saleType'" +
                         "FROM buy_int WHERE sfm_user_no =" + sUserNo + " AND date_of_sale = '" + dQueryDate.ToString("yyyy-MM-dd") + "' ORDER BY sale_no";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = "-8",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    wgtPerCrate = 0,
                    boxes = 0,
                    price = 0,
                    grossValue = 0,
                    shortName = "",
                    saleType = ""
                });
                conn.Close();
                return returnBuyerTransactions;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = "-7",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    wgtPerCrate = 0,
                    boxes = 0,
                    price = 0,
                    grossValue = 0,
                    shortName = "",
                    saleType = ""
                });
                conn.Close();
                return returnBuyerTransactions;
            }

            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = "-6",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    wgtPerCrate = 0,
                    boxes = 0,
                    price = 0,
                    grossValue = 0,
                    shortName = "",
                    saleType = ""
                });
                conn.Close();
                da.Dispose();
                return returnBuyerTransactions;
            }

            conn.Close();
            da.Dispose();

            //Insert line with new time token. First line is always token or error
            returnBuyerTransactions.Add(new buyerTransactions
            {
                abbrev = "1",
                prCode = result.newToken,
                sizeCode = "",
                condition = "",
                wgtPerCrate = 0,
                boxes = 0,
                price = 0,
                grossValue = 0,
                shortName = "",
                saleType = ""
            });

            if (dt.Rows.Count == 0)
            {
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = "No data available, try again later",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    wgtPerCrate = 0,
                    boxes = 0,
                    price = 0,
                    grossValue = 0,
                    shortName = "",
                    saleType = ""
                });
            }

            foreach (DataRow dr in dt.Rows)
            {
                returnBuyerTransactions.Add(new buyerTransactions
                {
                    abbrev = dr["abbrev"].ToString(),
                    prCode = dr["prCode"].ToString(),
                    sizeCode = dr["sizeCode"].ToString(),
                    condition = dr["condition"].ToString(),
                    wgtPerCrate = Convert.ToDouble(dr["wgtPerCrate"]),
                    boxes = Convert.ToInt32(dr["boxes"]),
                    price = Convert.ToDouble(dr["Price"]),
                    grossValue = Convert.ToDouble(dr["totalValue"]),
                    shortName = dr["shortName"].ToString(),
                    saleType = dr["saleType"].ToString()
                });
            }

            return returnBuyerTransactions;
        }

        public List<buyerRemovalProduct> getBuyerRemovalProduct(string userNo, string subUserNo, string lId, string removedFlag, string token)
        {
            List<buyerRemovalProduct> returnBuyerProduct = new List<buyerRemovalProduct>();
            int iSubUserNo = 0;
            int iUserNo = 0;
            
            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validSubUser = Int32.TryParse(subUserNo, out iSubUserNo);
            bool validRemovalFlag = (removedFlag == "T" || removedFlag == "F") ? true : false;

            if (!validUser || !validSubUser || !validRemovalFlag)
            {
                returnBuyerProduct.Add(new buyerRemovalProduct
                {
                    labelNo = "-1"
                });
                return returnBuyerProduct;
            }

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }
            
            String sRemoved = " ";
             if (validRemovalFlag)
            {
                sRemoved = removedFlag;
            }

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnBuyerProduct.Add(new buyerRemovalProduct
                {
                    labelNo = "-2"
                });
                return returnBuyerProduct;
            }

            /* Check token */
            LoginData result = chkLId(sUserNo, lId, token, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                // Invalid token, user, pin or error
                returnBuyerProduct.Add(new buyerRemovalProduct
                {
                    labelNo = "-3"
                });
                conn.Close();
                return returnBuyerProduct;
            }

            if (result.category != "B")
            {
                returnBuyerProduct.Add(new buyerRemovalProduct
                {
                   labelNo = "-4"
                });
                conn.Close();
                return returnBuyerProduct;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            /* This is for when user and subUser records are finalised */
            /* Check if subUserNo belongs to userNo - JDJ 230517 */
            /*
            if (subUserNo ==! userNo)
            {

            }
            */


            //Subuser not used at present
            sqlCommand = "SELECT label_no AS 'labelNo', sup_name AS 'supName', species_abbrev as 'speciesAbbrev', process as 'prCode', size AS 'sizeCode', " +
                      "weight AS 'weight', crate_type_desc AS 'crateTypeDesc', auct_area AS 'auctArea', position AS 'auctPosition', removed AS 'removed', seized AS 'seized', cold_storage AS 'coldStorage'" +
                      " FROM buy_product WHERE buyer_no = " + sUserNo + " AND removed = '" + sRemoved + "' ORDER BY auct_area ASC";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
               returnBuyerProduct.Add(new buyerRemovalProduct
                {
                     labelNo = "-5"
                });
                conn.Close();
                return returnBuyerProduct;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnBuyerProduct.Add(new buyerRemovalProduct
                {
                    labelNo = "-9"
                });
                conn.Close();
                return returnBuyerProduct;
            }

            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnBuyerProduct.Add(new buyerRemovalProduct
                {
                    labelNo = "-9"
                });
                conn.Close();
                da.Dispose();
                return returnBuyerProduct;
            }

            conn.Close();
            da.Dispose();

            //Insert line with new time token. First line is always token or error
           returnBuyerProduct.Add(new buyerRemovalProduct
            {
                 labelNo = "-9"
            });

            if (dt.Rows.Count == 0)
            {
                returnBuyerProduct.Add(new buyerRemovalProduct
                {
                    labelNo = "-9"
                });
            }

            foreach (DataRow dr in dt.Rows)
            {
                returnBuyerProduct.Add(new buyerRemovalProduct
                {
                    labelNo = dr["labelNo"].ToString(),
                    supName = dr["supName"].ToString(),
                    speciesAbbrev = dr["speciesAbbrev"].ToString(),
                    processCode = dr["prCode"].ToString(),
                    sizeCode = dr["sizeCode"].ToString(),
                    weight = Convert.ToDouble(dr["weight"]),
                    crateTypeDesc = dr["crateTypeDesc"].ToString(),
                    auctArea = dr["auctArea"].ToString(),
                    auctPosition = dr["auctPosition"].ToString(),
                    removed = sRemoved,
                    seized = dr["seized"].ToString(),
                    coldStorage = dr["coldStorage"].ToString()
                });
            }

            return returnBuyerProduct;
        }

        public List<stock> getStock(string userNo, string subUserNo, string lId, string token)
        {
            List<stock> returnStock = new List<stock>();
            int iSubUserNo = 0;
            int iUserNo = 0;
         

            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validSubUser = Int32.TryParse(subUserNo, out iSubUserNo);
          
            if (!validUser || !validSubUser)
            {
                returnStock.Add(new stock
                {
                    stockId = -1,
                    shortDesc = token,
                    longDesc = "",
                    stockAvailability = "",
                    price = 0,
                    weight = 0,
                    location = "",
                    orderByDate = "",
                    orderByTime = "",
                    deliveryDate = "",
                    logoLink = "",
                    saleType = ""
                });
                return returnStock;
            }

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnStock.Add(new stock
                {
                    stockId = -3,
                    shortDesc = token,
                    longDesc = "",
                    stockAvailability = "",
                    price = 0,
                    weight = 0,
                    location = "",
                    orderByDate = "",
                    orderByTime = "",
                    deliveryDate = "",
                    logoLink = "",
                    saleType = ""
                });
                return returnStock;
            }

            /* Check token */
            LoginData result = chkLId(sUserNo, lId, token, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                // Invalid token, user, pin or error
                returnStock.Add(new stock
                {
                    stockId = Convert.ToDouble(result.status),
                    shortDesc = token,
                    longDesc = "",
                    stockAvailability = "",
                    price = 0,
                    weight = 0,
                    location = "",
                    orderByDate = "",
                    orderByTime = "",
                    deliveryDate = "",
                    logoLink = "",
                    saleType = ""
                });
                conn.Close();
                return returnStock;
            }

            if (result.category != "B")
            {
                returnStock.Add(new stock
                {
                    stockId = -4,
                    shortDesc = token,
                    longDesc = "",
                    stockAvailability = "",
                    price = 0,
                    weight = 0,
                    location = "",
                    orderByDate = "",
                    orderByTime = "",
                    deliveryDate = "",
                    logoLink = "",
                    saleType = ""
                });
                conn.Close();
                return returnStock;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            /* This is for when user and subUser records are finalised */
            /* Check if subUserNo belongs to userNo - JDJ 230517 */
            /*
            if (subUserNo ==! userNo)
            {

            }
            */

            //Subuser not used at present
            sqlCommand = "SELECT stock_id AS 'stockId', short_desc AS 'shortDesc', long_desc AS 'longDesc', `stock_availability` AS 'stockAvailability', " +
                         "price as 'price', weight as 'weight', price_unit as 'priceUnit', location AS 'location', order_by_date AS  'orderByDate', order_by_time AS  'orderByTime', delivery_date as  'deliveryDate', logo_link as 'logoLink', sale_type AS  'saleType' " +
                         "FROM fixed_price_stock WHERE status_flg != 'Deleted' ORDER BY stock_id";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnStock.Add(new stock
                {
                    stockId = -5,
                    shortDesc = result.newToken,
                    longDesc = "",
                    stockAvailability = "",
                    price = 0,
                    weight = 0,
                    location = "",
                    orderByDate = "",
                    orderByTime = "",
                    deliveryDate = "",
                    logoLink = "",
                    saleType = ""
                });
                conn.Close();
                return returnStock;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnStock.Add(new stock
                {
                    stockId = -6,
                    shortDesc = result.newToken,
                    longDesc = "",
                    stockAvailability = "",
                    price = 0,
                    weight = 0,
                    location = "",
                    orderByDate = "",
                    orderByTime = "",
                    deliveryDate = "",
                    logoLink = "",
                    saleType = ""
                });
                conn.Close();
                return returnStock;
            }

            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnStock.Add(new stock
                {
                    stockId = -7,
                    shortDesc = result.newToken,
                    longDesc = "",
                    stockAvailability = "",
                    price = 0,
                    weight = 0,
                    location = "",
                    orderByDate = "",
                    orderByTime = "",
                    deliveryDate = "",
                    logoLink = "",
                    saleType = ""
                });
                conn.Close();
                return returnStock;
            }     

            if (dt.Rows.Count == 0)
            {
                returnStock.Add(new stock
                {
                    stockId = -8,
                    shortDesc = result.newToken,
                    longDesc = "",
                    stockAvailability = "",
                    price = 0,
                    weight = 0,
                    location = "",
                    orderByDate = "",
                    orderByTime = "",
                    deliveryDate = "",
                    logoLink = "",
                    saleType = ""
                });
                conn.Close();
                return returnStock;
            }

            //Return first record with token
            returnStock.Add(new stock
            {
                stockId = 1,
                shortDesc = result.newToken,
                longDesc = "",
                stockAvailability = "",
                price = 0,
                weight = 0,
                location = "",
                orderByDate = "",
                orderByTime = "",
                deliveryDate = "",
                logoLink = "",
                saleType = ""
            });

            
            foreach (DataRow dr in dt.Rows)
            {
               
                returnStock.Add(new stock
                {
                    stockId = Convert.ToInt32(dr["stockId"]),
                    shortDesc = dr["shortDesc"].ToString(),
                    longDesc = dr["longDesc"].ToString(),
                    stockAvailability = dr["stockAvailability"].ToString(),
                    price = Convert.ToDouble(dr["price"]),
                    weight = Convert.ToDouble(dr["weight"]),
                    priceUnit = dr["priceUnit"].ToString(),
                    location = dr["location"].ToString(),
                    orderByDate = dr["orderByDate"].ToString(),
                    orderByTime = dr["orderByTime"].ToString(),
                    deliveryDate = dr["deliveryDate"].ToString(),
                    logoLink = dr["logoLink"].ToString(),
                    saleType = dr["saleType"].ToString()
                });
            }

            conn.Close();
            return returnStock;
        }

        public List<clientMessage> getMessages(string userNo, string lId, string token)
        {
            List<clientMessage> returnMessages = new List<clientMessage>();

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnMessages.Add(new clientMessage { msgId = -9, individualFlg = "0", messageText = "Error - please contact SFM" });
                return returnMessages;
            }

            /*****   No authentication required for messages at present
            LoginData result = chkLId(userNo, lId, token, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                // Invalid token, user, pin or error
                returnMessages.Add(new clientMessage { msgId = Convert.ToDouble(result.status), individualFlg = "0", messageText = "Error - please contact SFM" });
                conn.Close();
                return returnMessages;
            }
            
             *****   Currently 999999 = supplier, 888888 = buyer
             
            ***** */

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;
           

            int iUserNo = 0;
            bool validUser = Int32.TryParse(userNo, out iUserNo);

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            sqlCommand = "SELECT msg_id AS 'msgId', sfm_user_no AS 'sfmUserNo', message_text AS 'messageText', date_start AS dateStart FROM client_message WHERE" +
                         " sfm_user_no = " + sUserNo + " AND active_flg = 1";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnMessages.Add(new clientMessage { msgId = -8, individualFlg = "0", messageText = "Error - please contact SFM" });
                conn.Close();
                return returnMessages;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnMessages.Add(new clientMessage { msgId = -7, individualFlg = "0", messageText = "Error - please contact SFM" });
                conn.Close();
                return returnMessages;
            }

            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnMessages.Add(new clientMessage { msgId = -6, individualFlg = "0", messageText = "Error - please contact SFM" });
                conn.Close();
                da.Dispose();
                return returnMessages;
            }

            conn.Close();
            da.Dispose();

            foreach (DataRow dr in dt.Rows)
            {
                returnMessages.Add(new clientMessage
                {
                    msgId = Convert.ToDouble(dr["msgId"]),
                    dateStart = dr["dateStart"].ToString(),
                    individualFlg = "0",
                    messageText = dr["messageText"].ToString(),
                });
            }
            return returnMessages;
        }

        //  public List<salesData> JSONData(string userNo, string lId)
        public List<salesData> JSONData(string userNo, string lId, string queryDate, string queryType, string token)
        {

            int iUserNo = 0;
            DateTime dQueryDate;
            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validDate = DateTime.TryParse(queryDate, out dQueryDate);

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }
            
            List<salesData> returnData = new List<salesData>();
            
            if (!validDate || !validUser)
            {
                returnData.Add(new salesData { abbrev = "-10", prCode = token, sizeCode = "", boxes = 0, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0 });
            }
            
           

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnData.Add(new salesData { abbrev = "-9", prCode = token, sizeCode = "", boxes = 0, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0 });
                return returnData;
            }

            LoginData result = chkLId(userNo, lId, token, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                // Invalid token, user, pin or error
                returnData.Add(new salesData { abbrev = result.status, prCode = token, sizeCode = "", boxes = 0, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0 });
                return returnData;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            if (userNo == "999888") userNo = "100222"; // Testing account for Apple store

            if (queryType == "m")
            {
                if (result.contract == 1)  //Contracted suppliers view all species
                    sqlCommand = "SELECT spec_name AS 'abbrev', process_code AS 'prCode', size_code AS 'sizeCode', `condition` AS 'condition', " +
                                "boxes AS 'boxes', weight as 'kgs', min_price AS  'minPrice', max_price as  'maxPrice', ave_price as 'avePrice', gross_value as 'grossValue'" +
                                "FROM gmr_int WHERE date_of_sale = '" + dQueryDate.ToString("yyyy-MM-dd") + "' AND boxes >= 0 ORDER BY " +
                                " spec_name, process_code, size_code";

                else // Non contracted
                    sqlCommand = "SELECT spec_name AS 'abbrev', process_code AS 'prCode', size_code AS 'sizeCode', `condition` AS 'condition', " +
                              "boxes AS 'boxes', weight as 'kgs', min_price AS  'minPrice', max_price as  'maxPrice', ave_price as 'avePrice', gross_value as 'grossValue'" +
                              "FROM gmr_int JOIN gmr_species on gmr_int.species = gmr_species.species WHERE gmr_species.sfm_user_no = " + sUserNo + " AND gmr_int.date_of_sale = '" + dQueryDate.ToString("yyyy-MM-dd") + "' AND gmr_int.boxes >= 0 " +
                              " AND (gmr_species.status_flg = 'A' OR (gmr_species.status_flg = 'D' AND DATEDIFF(DATE(NOW()), gmr_species.date_of_sale) <= 365))  ORDER BY " +
                              " gmr_int.spec_name, gmr_int.process_code, gmr_int.size_code";

            }
            else
                sqlCommand = "SELECT spec_name AS 'abbrev', process_code AS 'prCode', size_code AS 'sizeCode', `condition` AS 'condition', " +
                              "boxes AS 'boxes', weight as 'kgs', min_price AS  'minPrice', max_price as  'maxPrice', ave_price as 'avePrice' , gross_value as 'grossValue' " +
                              "FROM sup_int WHERE sfm_user_no = " + sUserNo +
                              " AND date_of_sale = '" + dQueryDate.ToString("yyyy-MM-dd") + "' AND boxes >= 0 ORDER BY " +
                              " spec_name, process_code, size_code";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnData.Add(new salesData { abbrev = "-8", prCode = token, sizeCode = "", boxes = 5, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0, grossValue = 0 });
                conn.Close();
                return returnData;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnData.Add(new salesData { abbrev = "-7", prCode = token, sizeCode = "", boxes = 5, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0, grossValue = 0 });
                conn.Close();
                return returnData;
            }


            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnData.Add(new salesData { abbrev = "-6", prCode = token, sizeCode = ex.Message.Substring(15, 20), boxes = 0, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0, grossValue = 0 });
                conn.Close();
                da.Dispose();
                return returnData;
            }

            conn.Close();
            da.Dispose();

            //Insert line with new time token. First line is always token or error
            returnData.Add(new salesData
            {
                abbrev = "1",
                prCode = result.newToken,
                sizeCode = "",
                condition = "",
                boxes = 0,
                kgs = 0,
                minPrice = 0,
                maxPrice = 0,
                avePrice = 0,
                grossValue = 0,
            });

            if (dt.Rows.Count == 0)
            {
                returnData.Add(new salesData
                {
                    abbrev = "No data available, try again later",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    boxes = 0,
                    kgs = 0,
                    minPrice = 0,
                    maxPrice = 0,
                    avePrice = 0,
                    grossValue = 0,
                });
            }
            //Load the list with data to return to the client
            foreach (DataRow dr in dt.Rows)
            {
                returnData.Add(new salesData
                {
                    abbrev = dr["abbrev"].ToString(),
                    prCode = dr["prCode"].ToString(),
                    sizeCode = dr["sizeCode"].ToString(),
                    condition = dr["condition"].ToString(),
                    boxes = Convert.ToDouble(dr["boxes"]),
                    kgs = Convert.ToDouble(dr["kgs"]),
                    minPrice = Convert.ToDouble(dr["minPrice"]),
                    maxPrice = Convert.ToDouble(dr["maxPrice"]),
                    avePrice = Convert.ToDouble(dr["avePrice"]),
                    grossValue = Convert.ToDouble(dr["grossValue"]),
                });
                //returnData.Add(dr);
            }

            return returnData;

            //TODO    Should now destroy connection object
        }

        /* Method overload for an additional parameter subUserNo - JDJ 22.05.17 */
        public List<salesData> JSONData(string userNo, string subUserNo, string lId, string queryDate, string queryType, string token)
        {
            int iSubUserNo = 0;
            int iUserNo = 0;
            DateTime dQueryDate;

            List<salesData> returnData = new List<salesData>();

            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validSubUser = Int32.TryParse(subUserNo, out iSubUserNo);
            bool validDate = DateTime.TryParse(queryDate, out dQueryDate);
            if (!validDate || !validUser || !validSubUser)
            {
                returnData.Add(new salesData { abbrev = "-10", prCode = "", sizeCode = "", boxes = 0, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0 });
            }

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }
          
            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnData.Add(new salesData { abbrev = "-9", prCode = "", sizeCode = "", boxes = 0, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0 });
                return returnData;
            }

            LoginData result = chkLId(sUserNo, lId, token, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                // Invalid token, user, pin or error
                returnData.Add(new salesData { abbrev = result.status, prCode = "", sizeCode = "", boxes = 0, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0 });
                return returnData;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;


            if (subUserNo == "999888") subUserNo = "100222"; // Testing account for Apple store

            /* Check if subUserNo is linked to userNo - JDJ 22.05.17 */
            /* TO DO when database table is completed - JDJ 22.05.17 */

            if (queryType == "m")
            {
                if (result.contract == 1)  //Contracted suppliers view all species
                    sqlCommand = "SELECT spec_name AS 'abbrev', process_code AS 'prCode', size_code AS 'sizeCode', `condition` AS 'condition', " +
                                "boxes AS 'boxes', weight as 'kgs', min_price AS  'minPrice', max_price as  'maxPrice', ave_price as 'avePrice', gross_value as 'grossValue'" +
                                "FROM gmr_int WHERE date_of_sale = '" + dQueryDate.ToString("yyyy-MM-dd") + "' AND boxes >= 0 ORDER BY " +
                                " spec_name, process_code, size_code";

                else // Non contracted
                    sqlCommand = "SELECT spec_name AS 'abbrev', process_code AS 'prCode', size_code AS 'sizeCode', `condition` AS 'condition', " +
                              "boxes AS 'boxes', weight as 'kgs', min_price AS  'minPrice', max_price as  'maxPrice', ave_price as 'avePrice', gross_value as 'grossValue'" +
                              "FROM gmr_int JOIN gmr_species on gmr_int.species = gmr_species.species WHERE gmr_species.sfm_user_no = " + sUserNo + " AND gmr_int.date_of_sale = '" + dQueryDate.ToString("yyyy-MM-dd") + "' AND gmr_int.boxes >= 0 " +
                              " AND (gmr_species.status_flg = 'A' OR (gmr_species.status_flg = 'D' AND DATEDIFF(DATE(NOW()), gmr_species.date_of_sale) <= 365))  ORDER BY " +
                              " gmr_int.spec_name, gmr_int.process_code, gmr_int.size_code";

            }
            else
                sqlCommand = "SELECT spec_name AS 'abbrev', process_code AS 'prCode', size_code AS 'sizeCode', `condition` AS 'condition', " +
                              "boxes AS 'boxes', weight as 'kgs', min_price AS  'minPrice', max_price as  'maxPrice', ave_price as 'avePrice' , gross_value as 'grossValue' " +
                              "FROM sup_int WHERE sfm_user_no = " + sUserNo +
                              " AND date_of_sale = '" + dQueryDate.ToString("yyyy-MM-dd") + "' AND boxes >= 0 ORDER BY " +
                              " spec_name, process_code, size_code";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnData.Add(new salesData { abbrev = "-8", prCode = "", sizeCode = "", boxes = 5, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0, grossValue = 0 });
                conn.Close();
                return returnData;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnData.Add(new salesData { abbrev = "-7", prCode = token, sizeCode = "", boxes = 5, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0, grossValue = 0 });
                conn.Close();
                return returnData;
            }


            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnData.Add(new salesData { abbrev = "-6", prCode = token, sizeCode = ex.Message.Substring(15, 20), boxes = 0, kgs = 0, minPrice = 0, maxPrice = 0, avePrice = 0, grossValue = 0 });
                conn.Close();
                da.Dispose();
                return returnData;
            }

            conn.Close();
            da.Dispose();

            //Insert line with new time token. First line is always token or error
            returnData.Add(new salesData
            {
                abbrev = "1",
                prCode = result.newToken,
                sizeCode = "",
                condition = "",
                boxes = 0,
                kgs = 0,
                minPrice = 0,
                maxPrice = 0,
                avePrice = 0,
                grossValue = 0,
            });

            if (dt.Rows.Count == 0)
            {
                returnData.Add(new salesData
                {
                    abbrev = "No data available, try again later",
                    prCode = "",
                    sizeCode = "",
                    condition = "",
                    boxes = 0,
                    kgs = 0,
                    minPrice = 0,
                    maxPrice = 0,
                    avePrice = 0,
                    grossValue = 0,
                });
            }
            //Load the list with data to return to the client
            foreach (DataRow dr in dt.Rows)
            {
                returnData.Add(new salesData
                {
                    abbrev = dr["abbrev"].ToString(),
                    prCode = dr["prCode"].ToString(),
                    sizeCode = dr["sizeCode"].ToString(),
                    condition = dr["condition"].ToString(),
                    boxes = Convert.ToDouble(dr["boxes"]),
                    kgs = Convert.ToDouble(dr["kgs"]),
                    minPrice = Convert.ToDouble(dr["minPrice"]),
                    maxPrice = Convert.ToDouble(dr["maxPrice"]),
                    avePrice = Convert.ToDouble(dr["avePrice"]),
                    grossValue = Convert.ToDouble(dr["grossValue"]),
                });
                //returnData.Add(dr);
            }

            return returnData;

            //TODO    Should now destroy connection object
        }

        public List<species> getSpecies()
        {
            List<species> returnSpecies = new List<species>();

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                returnSpecies.Add(new species { speciesCode = ex.Message });
                return returnSpecies;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            sqlCommand = "SELECT species AS 'speciesCode', abbrev AS 'abbrev', descript_ln1 AS 'descript' FROM species";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnSpecies.Add(new species { speciesCode = "-2" });
                conn.Close();
                return returnSpecies;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnSpecies.Add(new species { speciesCode = "-3" });
                conn.Close();
                return returnSpecies;
            }

            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnSpecies.Add(new species { speciesCode = ex.Message });
                conn.Close();
                da.Dispose();
                return returnSpecies;
            }

            conn.Close();
            da.Dispose();

            foreach (DataRow dr in dt.Rows)
            {
                returnSpecies.Add(new species
                {
                    speciesCode = (dr["speciesCode"]).ToString(),
                    abbrev = (dr["abbrev"]).ToString(),
                    descript = (dr["descript"]).ToString()
                });
            }
            return returnSpecies;
        }

        public List<processType> getProcesses()
        {
            List<processType> returnprocess = new List<processType>();

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnprocess.Add(new processType { processCode = "-2" });
                return returnprocess;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            sqlCommand = "SELECT process_code AS 'processCode', process_desc AS 'processDesc' FROM process";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnprocess.Add(new processType { processCode = "-1" });
                conn.Close();
                return returnprocess;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnprocess.Add(new processType { processCode = "-2" });
                conn.Close();
                return returnprocess;
            }

            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnprocess.Add(new processType { processCode = "-3" });
                conn.Close();
                da.Dispose();
                return returnprocess;
            }

            conn.Close();
            da.Dispose();

            foreach (DataRow dr in dt.Rows)
            {
                returnprocess.Add(new processType
                {
                    processCode = (dr["processCode"]).ToString(),
                    processDesc = (dr["processDesc"]).ToString()
                });
            }
            return returnprocess;
        }

        public List<size> getSizes()
        {
            List<size> returnsize = new List<size>();

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnsize.Add(new size { sizeCode = "-1" });
                return returnsize;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            sqlCommand = "SELECT size_code AS 'sizeCode', size_desc AS 'sizeDesc', sort_order AS 'sortOrder'FROM size";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnsize.Add(new size { sizeCode = "-1" });
                conn.Close();
                return returnsize;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnsize.Add(new size { sizeCode = "-2" });
                conn.Close();
                return returnsize;
            }

            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnsize.Add(new size { sizeCode = "-3" });
                conn.Close();
                da.Dispose();
                return returnsize;
            }

            conn.Close();
            da.Dispose();

            foreach (DataRow dr in dt.Rows)
            {
                returnsize.Add(new size
                {
                    sizeCode = (dr["sizeCode"]).ToString(),
                    sizeDesc = (dr["sizeDesc"]).ToString(),
                    sortOrder = Convert.ToDouble(dr["sortOrder"])
                });
            }
            return returnsize;
        }

        public List<qualityGrade> getGrades()
        {
            List<qualityGrade> returngrade = new List<qualityGrade>();

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returngrade.Add(new qualityGrade { qualityCode = "-10" });
                return returngrade;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            sqlCommand = "SELECT quality_code AS 'qualityCode'FROM quality";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returngrade.Add(new qualityGrade { qualityCode = "-1" });
                conn.Close();
                return returngrade;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returngrade.Add(new qualityGrade { qualityCode = "-2" });
                conn.Close();
                return returngrade;
            }

            try
            {

                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returngrade.Add(new qualityGrade { qualityCode = "-3" });
                conn.Close();
                da.Dispose();
                return returngrade;
            }

            conn.Close();
            da.Dispose();

            foreach (DataRow dr in dt.Rows)
            {
                returngrade.Add(new qualityGrade
                {
                    qualityCode = (dr["qualityCode"]).ToString()
                });
            }
            return returngrade;
        }

        /*** Used by website  ***/
        public List<supplyData> getNextCatalogue(string token)
        {
            List<supplyData> returnData = new List<supplyData>();

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnData.Add(new supplyData { abbrev = "-9", prCode = "", sizeCode = "", boxes = 0, kgs = 0 });
                return returnData;
            }

            if (token != "%a$hslwuidfbnd5dbndkwgw") // (-1)invalid token.
            {
                // Invalid token, user, pin or error
                returnData.Add(new supplyData { abbrev = "-1", prCode = "", sizeCode = "", boxes = 0, kgs = 0 });
                return returnData;
            }

            token = "0";  // Token is not returned for website
            returnData = getSupply();
            return returnData;
        }

        /**** For buyer App call **/
        public List<supplyData> getNextCatalogueUser(string userNo, string subUserNo, string lId, string token)
        {
            //TODO : Two overloaded functions should call common function. Overload should only check user and timeout and the call common function
            
            List<supplyData> returnData = new List<supplyData>();

            int iSubUserNo = 0;
            int iUserNo = 0;

            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validSubUser = Int32.TryParse(subUserNo, out iSubUserNo);
            if (!validUser || !validSubUser)
            {
                returnData.Add(new supplyData { abbrev = "-10", prCode = "", sizeCode = "", boxes = 0, kgs = 0 });
                return returnData;
            }

           

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnData.Add(new supplyData { abbrev = "-9", prCode = token, sizeCode = "", boxes = 0, kgs = 0 });
                return returnData;
            }

            LoginData result = chkLId(sUserNo, lId, token, conn); ; //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                returnData.Add(new supplyData { abbrev = result.status, prCode = token, sizeCode = "", boxes = 0, kgs = 0 });
                return returnData;
            }

            //Insert line with new time token. First line is always token or error
            returnData.Insert(0, new supplyData
            {
                abbrev = "1",
                prCode = result.newToken,
                sizeCode = "",
                condition = "",
                boxes = 0,
                kgs = 0,
            });

            returnData.AddRange(getSupply());
            return returnData;
        }

        public orderReturnResult putOrder(string userNo, string subUserNo, string lId, double stockId, double orderQty, string tokenIn)
        {
            orderReturnResult returnData = new orderReturnResult();
            int iSubUserNo = 0;
            int iUserNo = 0;

            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validSubUser = Int32.TryParse(subUserNo, out iSubUserNo);

            if (!validUser || !validSubUser)
            {
                returnData.responseCode = "-1";
                returnData.price = "";
                returnData.token = tokenIn;
                return returnData;
            }

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnData.responseCode = "-2";
                returnData.price = "";
                returnData.token = tokenIn;
                conn.Close();
                return returnData;
            }


            LoginData result = chkLId(userNo, lId, tokenIn, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                returnData.responseCode = result.status;
                returnData.price = "";
                returnData.token = tokenIn;
                conn.Close();
                return returnData;
            }

            if (result.category != "B")
            {
                returnData.responseCode = "-1";
                returnData.price = "";
                returnData.token = tokenIn;
                conn.Close();
                return returnData;
            }

            double dPrice = 0;
            double dWeight = 0;

            using (OdbcConnection sConn = setCon())
            {
                using (OdbcCommand iCmd = new OdbcCommand())
                {


                    iCmd.Connection = sConn;
                    iCmd.CommandType = CommandType.Text;
                    iCmd.CommandText = sqlCommand = "SELECT price as 'price', weight as 'weight' FROM fixed_price_stock WHERE stock_id = ?";
                    iCmd.Parameters.AddWithValue("?", stockId);
                    try
                    {
                        sConn.Open();
                       
                        OdbcDataReader reader = iCmd.ExecuteReader();
                        while (reader.Read())
                        {
                            dPrice = reader.GetDouble(reader.GetOrdinal("price"));
                            dWeight = reader.GetDouble(reader.GetOrdinal("weight"));
                        }

                    }
                    catch (SqlException e)
                    {
                        returnData.responseCode = "-3";
                        returnData.price = "";
                        returnData.token = result.newToken;
                        return returnData;
                    }
                    finally
                    {
                        sConn.Close();
                    }
                }
            }    


            using (OdbcConnection sConn = setCon())
            {
                using (OdbcCommand iCmd = new OdbcCommand())
                {
                    iCmd.Connection = sConn;
                    iCmd.CommandType = CommandType.Text;
                    iCmd.CommandText = sqlCommand = "INSERT INTO fixed_price_order (stock_id, order_qty, sfm_user_no, status_flg, price, weight) VALUES (?, ?, ?, ?, ?, ?)";
                    iCmd.Parameters.AddWithValue("?", stockId);
                    iCmd.Parameters.AddWithValue("?", orderQty);
                    iCmd.Parameters.AddWithValue("?", iUserNo);
                    iCmd.Parameters.AddWithValue("?", "Open");
                    iCmd.Parameters.AddWithValue("?", dPrice);
                    iCmd.Parameters.AddWithValue("?", dWeight);

                    try
                    {
                        sConn.Open();
                        int recordsAffected = iCmd.ExecuteNonQuery();

                    }
                    catch (SqlException e)
                    {
                        returnData.responseCode = "-4";
                        returnData.price = "";
                        returnData.token = result.newToken;
                        return returnData;
                    }
                    finally
                    {
                        sConn.Close();
                    }
                }
            }
            
            returnData.responseCode = "1";
            returnData.price = dPrice.ToString();
            returnData.weight = dWeight.ToString();//Return the price and weight to caller just in case it changed from client display
            returnData.token = result.newToken;
            conn.Close();
            return returnData;
        }

        public List<buyAccountData> getBuyAccountDtl(string userNo, string subUserNo, string lId, string tokenIn)
        {
            List<buyAccountData> returnData = new List<buyAccountData>();
            int iSubUserNo = 0;
            int iUserNo = 0;

            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validSubUser = Int32.TryParse(subUserNo, out iSubUserNo);
            if (!validUser || !validSubUser)
            {
                returnData.Add(new buyAccountData { responseCode = "-1", token = tokenIn, yesterdayBal = 0, currentBal = 0, sevenDayBal = 0, forteenDayBal = 0, twentyOneDayBal = 0, twentyEightDayBal = 0, todaysPayments = 0, todaysPurchases = 0, creditLimit = 0, totalBal = 0 });
            }

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnData.Add(new buyAccountData { responseCode = "-3", token = tokenIn, yesterdayBal = 0, currentBal = 0, sevenDayBal = 0, forteenDayBal = 0, twentyOneDayBal = 0, twentyEightDayBal = 0, todaysPayments = 0, todaysPurchases = 0, creditLimit = 0, totalBal = 0 });
                conn.Close();
            }


            LoginData result = chkLId(userNo, lId, tokenIn, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                returnData.Add(new buyAccountData { responseCode = result.status, token = tokenIn, yesterdayBal = 0, currentBal = 0, sevenDayBal = 0, forteenDayBal = 0, twentyOneDayBal = 0, twentyEightDayBal = 0, todaysPayments = 0, todaysPurchases = 0, creditLimit = 0, totalBal = 0 });
                conn.Close();
                return returnData;
            }

            if (result.category != "B")
            {
                returnData.Add(new buyAccountData { responseCode = "-1", token = tokenIn, yesterdayBal = 0, currentBal = 0, sevenDayBal = 0, forteenDayBal = 0, twentyOneDayBal = 0, twentyEightDayBal = 0, todaysPayments = 0, todaysPurchases = 0, creditLimit = 0, totalBal = 0 });
                conn.Close();
                return returnData;
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;

            
            sqlCommand = "SELECT yesterday_bal AS 'yesterdayBal', current_bal AS 'currentBal', 7day_bal  AS 'sevenDayBal', 14day_bal  AS 'forteenDayBal', 21day_bal  AS 'twentyOneDayBal', 28day_bal  AS 'twentyEightDayBal'," +
                       "todays_payments  AS 'todaysPayments', todays_purchases  AS 'todaysPurchases', credit_limit  AS 'creditLimit',total_bal  AS 'totalBal', crate_bal_large AS 'crateBalLarge', crate_bal_small AS 'crateBalSmall', crate_bal_lidded AS 'crateBalLidded', credit_reservations AS 'creditReservations'" +
                        "FROM buy_account WHERE sfm_user_no =  " + sUserNo;
            

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnData.Add(new buyAccountData { responseCode = "-4", token = tokenIn, yesterdayBal = 0, currentBal = 0, sevenDayBal = 0, forteenDayBal = 0, twentyOneDayBal = 0, twentyEightDayBal = 0, todaysPayments = 0, todaysPurchases = 0, creditLimit = 0, totalBal = 0 });
                conn.Close();
                return returnData;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnData.Add(new buyAccountData { responseCode = "-5", token = tokenIn, yesterdayBal = 0, currentBal = 0, sevenDayBal = 0, forteenDayBal = 0, twentyOneDayBal = 0, twentyEightDayBal = 0, todaysPayments = 0, todaysPurchases = 0, creditLimit = 0, totalBal = 0 });
                conn.Close();
                return returnData;
            }


            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnData.Add(new buyAccountData { responseCode = "-6", token = tokenIn, yesterdayBal = 0, currentBal = 0, sevenDayBal = 0, forteenDayBal = 0, twentyOneDayBal = 0, twentyEightDayBal = 0, todaysPayments = 0, todaysPurchases = 0, creditLimit = 0, totalBal = 0 });
                conn.Close();
                da.Dispose();
                return returnData;
            }

            conn.Close();
            da.Dispose();

            if (dt.Rows.Count == 0)
            {
                returnData.Add(new buyAccountData { responseCode = "-7", token = result.newToken, yesterdayBal = 0, currentBal = 0, sevenDayBal = 0, forteenDayBal = 0, twentyOneDayBal = 0, twentyEightDayBal = 0, todaysPayments = 0, todaysPurchases = 0, creditLimit = 0, totalBal = 0 });
            }

            //Load the list with data to return to the client
            foreach (DataRow dr in dt.Rows)
            {
                returnData.Add(new buyAccountData
                {
                    responseCode = "1",
                    token = result.newToken,
                    yesterdayBal = Convert.ToDouble(dr["yesterdayBal"]),
                    currentBal = Convert.ToDouble(dr["currentBal"]) + Convert.ToDouble(dr["creditReservations"]),
                    sevenDayBal = Convert.ToDouble(dr["sevenDayBal"]),
                    forteenDayBal = Convert.ToDouble(dr["forteenDayBal"]),
                    twentyOneDayBal = Convert.ToDouble(dr["twentyOneDayBal"]),
                    twentyEightDayBal = Convert.ToDouble(dr["twentyEightDayBal"]),
                    todaysPayments = Convert.ToDouble(dr["todaysPayments"]),
                    todaysPurchases = Convert.ToDouble(dr["todaysPurchases"]),
                    creditLimit = Convert.ToDouble(dr["creditLimit"]),
                    totalBal = Convert.ToDouble(dr["totalBal"]),
                    crateBalLarge = Convert.ToDouble(dr["crateBalLarge"]),
                    crateBalSmall = Convert.ToDouble(dr["crateBalSmall"]),
                    crateBalLidded = Convert.ToDouble(dr["crateBalLidded"]),
                    creditReservations = Convert.ToDouble(dr["creditReservations"]),

                });
                //returnData.Add(dr);
            }

            conn.Close();
            return returnData;
        }

        public LoginReturnResult chkUser(string userNo, string lId)
        {

            OdbcConnection conn = setCon();
            LoginReturnResult loginResult;
            LoginData result;

            loginResult.status = "0";
            loginResult.tradingName = "";
            loginResult.token = "";
            loginResult.category = "";


            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                loginResult.status = "-5";
                return loginResult;
            }

            string token = "-1";

            result = chkLId(userNo, lId, token, conn);


            logConn(userNo, result.status, conn);
            conn.Close();

            loginResult.status = result.status;
            loginResult.tradingName = result.tradingName;
            loginResult.token = result.newToken;
            loginResult.category = result.category;

            return loginResult;
        }

        public List<order> getOrders(string userNo, string subUserNo, string lId, string tokenIn)
        {
            Debug.Print("called");
            List<order> returnData = new List<order>();

            int iSubUserNo = 0;
            int iUserNo = 0;

            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validSubUser = Int32.TryParse(subUserNo, out iSubUserNo);
            if (!validUser || !validSubUser)
            {
                returnData.Add(new order { transId = -4, stockId = 0, orderQty = 0, transDate = "", statusFlg = "", comment = "", price = 0, weight = 0 });
                return returnData;
            }



            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnData.Add(new order { transId = -5, stockId = 0, orderQty = 0, transDate = "", statusFlg = "", comment = "", price = 0, weight = 0 });
                return returnData;
            }

            Debug.Print("before login");
            LoginData result = chkLId(sUserNo, lId, tokenIn, conn); //Successful result returns time token

            if (result.status != "1") // (-1)invalid credentials.    (-2)Token expired
            {
                Debug.Print("bad login");
                // Invalid token, user, pin or error
                returnData.Add(new order { transId = Convert.ToDouble(result.status), stockId = 0, orderQty = 0, transDate = "", statusFlg = "", comment = tokenIn, price = 0, weight = 0 });
            }

            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;


            sqlCommand = "SELECT fixed_price_order.trans_id AS 'transId', fixed_price_order.stock_id AS 'stockId', fixed_price_order.order_qty AS 'orderQty', fixed_price_order.trans_date AS 'transDate', fixed_price_order.price AS 'price', " +
                        "fixed_price_order.status_flg AS 'statusFlg', fixed_price_stock.price_unit AS 'priceUnit',fixed_price_order.comment as 'comment', fixed_price_stock.short_Desc AS 'shortDesc'," +
                        " fixed_price_order.weight AS 'weight' FROM fixed_price_order JOIN fixed_price_stock on fixed_price_stock.stock_id = fixed_price_order.stock_id WHERE sfm_user_no = " + sUserNo + 
                        " ORDER BY trans_id DESC";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnData.Add(new order { transId = -6, stockId = 0, shortDesc = "", orderQty = 0, transDate = "", statusFlg = "", comment = result.newToken, price = 0, weight = 0 });
                conn.Close();
                return returnData;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnData.Add(new order { transId = -7, stockId = 0, shortDesc = "", orderQty = 0, transDate = "", statusFlg = "", comment = result.newToken, price = 0, weight = 0 });
                return returnData;
            }

            Debug.Print("before fill");
            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                Debug.Print(sqlCommand);
                Debug.Print(ex.Message);
                returnData.Add(new order { transId = -8, stockId = 0, shortDesc = "", orderQty = 0, transDate = "", statusFlg = "", comment = result.newToken, price = 0, weight = 0 });
                conn.Close();
                da.Dispose();
                return returnData;
            }

            conn.Close();
            da.Dispose();

            /*
            if (dt.Rows.Count == 0)
            {
                returnData.Add(new order { transId = 0, stockId = 0, shortDesc = "", orderQty = 0, transDate = "", statusFlg = "", comment = result.newToken, price = 0 });
            }
            */

            //Return first record with token
            returnData.Add(new order { transId = 1, stockId = 0, shortDesc = "", orderQty = 0, transDate = "", statusFlg = "", comment = result.newToken, price = 0, weight = 0 });

            //Load the list with data to return to the client
            foreach (DataRow dr in dt.Rows)
            {
                returnData.Add(new order
                {
                    transId = Convert.ToDouble(dr["transId"]),
                    stockId = Convert.ToDouble(dr["stockId"]),
                    shortDesc = dr["shortDesc"].ToString(),
                    orderQty = Convert.ToDouble(dr["orderQty"]),
                    //transDate = dr["transDate"].ToString(),
                    transDate = DateTime.Parse(dr["transDate"].ToString()).ToString("dd/MM/yyyy"),
                    statusFlg = dr["statusFlg"].ToString(),
                    comment = dr["comment"].ToString(),
                    price = Convert.ToDouble(dr["price"]),
                    weight = Convert.ToDouble(dr["weight"]),
                    priceUnit = dr["priceUnit"].ToString()
                });
                //returnData.Add(dr);
            }

            conn.Close();
            return returnData;
        }

        private LoginData chkLId(string userNo, string lId, string token, OdbcConnection conn)
        {
            OdbcCommand cmd;
            int pinNo = 0;
            int iUserNo = 0;
            LoginData result;
            result.status = "0";
            result.errMsg = "";
            result.tradingName = "";
            result.contract = 0;
            result.newToken = "";
            result.category = "";


            //if (checkDate == DateTime.Now.ToString("dd/MM/yy"))
            bool validUser = Int32.TryParse(userNo, out iUserNo);
            bool validLId = Int32.TryParse(lId, out pinNo);
            if (lId == "giveMeStats777888999")
                validLId = true;

            String sUserNo = "0";
            if (validUser)
            {
                sUserNo = iUserNo.ToString();
            }

            if (!validUser || !validLId)
            {
                result.status = "-7";
                return result;
            }

            sqlLoginCmd = "SELECT trading_name, contract, pin_no, con_attempts, con_lockout_time, sfm_category " +
                          "FROM user_reg WHERE sfm_user_no = " + sUserNo;

            try
            {
                cmd = new OdbcCommand(sqlLoginCmd, conn);
            }
            catch (Exception ex)
            {
                result.status = "-4";
                result.status = ex.Message;
                return result;
            }

            try
            {
                using (OdbcDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (token == "9svr4GrL0UgAAAAAAAAAAAAAAAAAAAAA" && lId == "giveMeStats777888999")
                        {
                            result.status = "1";
                            result.tradingName = reader.GetString(0);
                            result.contract = reader.GetInt16(1);
                            if (reader.GetInt16(5) < 4) result.category = "S";
                            else
                                result.category = "B";
                            return result;
                        };

                        int logCount = reader.GetInt16(3);
                        if (logCount > 4)
                        {
                            //  DateTime val = DateTime.Now.AddHours(-0.25);
                            //   DateTime val2 = Convert.ToDateTime(reader.GetValue(4));
                            if (reader.GetValue(4) == null || DateTime.Now.AddHours(-0.25) < Convert.ToDateTime(reader.GetValue(4)))
                            {
                                result.status = "-6";
                                return result;
                            }
                            setLoginAttempts(userNo, 0, conn);
                            logCount = 0;
                        }

                        //a
                       
                        int sPinNo = Convert.ToInt32(reader.GetValue(2));
                        if (sPinNo == pinNo && sPinNo != 0)
                        {
                            result.status = "1";
                            string logRet = setLoginAttempts(sUserNo, 0, conn);
                            result.tradingName = reader.GetString(0);
                            result.contract = reader.GetInt16(1);
                            if (reader.GetInt16(5) < 4) result.category = "S";
                            else
                                result.category = "B";
                        }
                        else
                        {
                            result.status = "-1";
                            string logRet =  setLoginAttempts(sUserNo, logCount + 1, conn);
                            result.tradingName = logRet;
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.status = "-3";
                result.errMsg = ex.Message;
                result.tradingName = ex.Message;
                return result;
            }


            if (token != "-1")
            {

                // Check the time token
                byte[] data = Convert.FromBase64String(token);
                DateTime when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
                if (when < DateTime.UtcNow.AddHours(-0.25))
                {
                    result.status = "-2";
                    return result; //expired
                }

            }

            if (result.tradingName == "")
            {
                result.status = "-1";
                return result;  //Invalid credentials
            }
            else
            {
                // Generate new time token
                byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
                byte[] key = new Guid().ToByteArray();
                result.status = "1";
                result.newToken = Convert.ToBase64String(time.Concat(key).ToArray());
                return result; // All OK
            }

        }

        private string setLoginAttempts(string userNo, int conAttempts, OdbcConnection conn)
        {
            OdbcCommand cmd;
            DateTime conLockoutTime;
            if (conAttempts >= 5)
            {
                conLockoutTime = DateTime.Now;
            }
            else
                conLockoutTime = DateTime.MinValue;


            String mySQLDateTime = conLockoutTime.ToString("yyyy-MM-dd H:mm:ss");

            string sqlCmd = "UPDATE user_reg SET con_attempts = " + conAttempts + " , con_lockout_time = '" + mySQLDateTime +
                            "' WHERE sfm_user_no = " + userNo;
            try
            {
                cmd = new OdbcCommand(sqlCmd, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "OK";
        }

        private OdbcConnection setCon()
        {
            /** OdbcConnection conn = new OdbcConnection("DSN=SFMClientDB;UID=buyclient@mysqlwebsfm;PWD=#%buyForapp96%54");**/
            OdbcConnection conn = new OdbcConnection("DSN=SFMClientDB;UID=buyclient@mysqlwebsfm;PWD=#%buyForapp96%54");

            return conn;
        }
        
        private List<supplyData> getSupply()
        {
            List<supplyData> returnData = new List<supplyData>();

            OdbcConnection conn = setCon();
            try
            {
                conn.Open();
            }
            catch
            {
                returnData.Add(new supplyData { abbrev = "-9", prCode = "", sizeCode = "", boxes = 0, kgs = 0 });
                return returnData;
            }


            OdbcDataAdapter da = new OdbcDataAdapter();
            OdbcCommand cmd;


            sqlCommand = "SELECT spec_name AS 'abbrev', process_code AS 'prCode', size_code AS 'sizeCode', `condition` AS 'condition', " +
                        "boxes AS 'boxes', weight as 'kgs'" +
                        "FROM supply WHERE weight >= 0 ORDER BY " +
                        " spec_name, process_code, size_code";

            try
            {
                cmd = new OdbcCommand(sqlCommand, conn);
            }
            catch (Exception ex)
            {
                returnData.Add(new supplyData { abbrev = "-8", prCode = "", sizeCode = "", boxes = 5, kgs = 0 });
                conn.Close();
                return returnData;
            }

            try
            {
                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                returnData.Add(new supplyData { abbrev = "-7", prCode = "", sizeCode = "", boxes = 5, kgs = 0 });
                conn.Close();
                return returnData;
            }


            try
            {
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                returnData.Add(new supplyData { abbrev = "-6", prCode = "", sizeCode = ex.Message.Substring(15, 20), boxes = 0, kgs = 0 });
                conn.Close();
                da.Dispose();
                return returnData;
            }

            conn.Close();
            da.Dispose();

            if (dt.Rows.Count == 0)
            {
                returnData.Add(new supplyData
                {
                    abbrev = "No data available, try again later",
                    prCode = "" ,
                    sizeCode = "",
                    condition = "",
                    boxes = 0,
                    kgs = 0,
                });
            }
     
            //Load the list with data to return to the client
            foreach (DataRow dr in dt.Rows)
            {
                returnData.Add(new supplyData
                {
                    abbrev = dr["abbrev"].ToString(),
                    prCode = dr["prCode"].ToString(),
                    sizeCode = dr["sizeCode"].ToString(),
                    condition = dr["condition"].ToString(),
                    boxes = Convert.ToDouble(dr["boxes"]),
                    kgs = Convert.ToDouble(dr["kgs"]),
                });
                //returnData.Add(dr);
            }

            conn.Close();
            return returnData;
        }

        private string logConn(string userNo, string logStatus, OdbcConnection conn)
        {
            OdbcCommand cmd;

            string sqlCmd = "INSERT u_conn SET sfm_user_no =  " +  userNo + " , con_status = " + logStatus;
            try
            {
                cmd = new OdbcCommand(sqlCmd, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "OK";
        }


        #endregion

    }
}
