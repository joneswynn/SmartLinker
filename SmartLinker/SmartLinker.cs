using System;
using System.Web;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;
using System.Web.UI;
using System.Configuration;

namespace SmartLinker
{
    public class SmartLinker : IHttpModule
    {
        public delegate void MyEventHandler(Object s, EventArgs e);
        private MyEventHandler _eventHandler = null;

        public void Init(System.Web.HttpApplication application)
        {
            application.BeginRequest += new EventHandler(application_BeginRequest);
        }

        public event MyEventHandler MyEvent
        {
            add { _eventHandler += value; }
            remove { _eventHandler -= value; }
        }

        private void application_BeginRequest(Object source, EventArgs e)
        {
            HttpApplication application = (HttpApplication)source;
            HttpContext context = application.Context;

            //First thing to do is see if we have a smart link.
            string regExp = "^[a-zA-Z0-9]*$"; //Alphanumeric and 7 characters long.
            string guid = context.Request.FilePath.ToString();

            Regex r = new Regex(regExp, RegexOptions.IgnoreCase);

            // Match the regular expression pattern against a text string.
            guid = guid.Replace("/", "");
            if (guid.Length == 7)
            {
                Match m = r.Match(guid);
                while (m.Success) 
                {
                    //If we have a smart link- next we need to lookup in the database where to send them.
                    SqlCommand oSQL = new SqlCommand();
                    //string _ConnectionString = "Data Source=JONESWYNNAFB2\\SQLEXPRESS;Initial Catalog=SmartLinks;User ID=mscc;Password=kjh$#@dkfM#";
                    string _ConnectionString = "Data Source=VSERVER611;Initial Catalog=SmartLinks;User ID=mscc;Password=kjh$#@dkfM#";

                    SqlConnection dbconn = new SqlConnection(_ConnectionString);

                    try
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            dbconn.Open();
                            cmd.Connection = dbconn;

                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = @"SELECT * FROM SmartLink
	                        INNER JOIN SmartLink_Domain on SmartLink.domainID = SmartLink_Domain.id
	                        LEFT JOIN SmartLink_Affiliate on SmartLink_Affiliate.id = SmartLink.affiliateID
	                        LEFT JOIN SmartLink_Offer on SmartLink_Offer.id = SmartLink.offerID
                                WHERE SmartLink.guid = '" + guid + "'";

                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable oDT = new DataTable();
                                da.Fill(oDT);

                                //Send an email of the ones we expired.
                                if (oDT.Rows.Count > 0)
                                {
                                    //We found one.
                                    foreach (DataRow oRow in oDT.Rows)
                                    {
                                        //Get the database values
                                        string targetURL = oRow["targetURL"].ToString();
                                        string var1 = oRow["var1"].ToString();
                                        string transactionId = oRow["transactionID"].ToString();
                                        DateTime expireDate;
                                        if (oRow["expirationDate"].ToString() != "")
                                        {
                                            expireDate = Convert.ToDateTime(oRow["expirationDate"]);
                                        } else
                                        {
                                            expireDate = Convert.ToDateTime("1/1/9999 12:00:00");
                                        }
                                        
                                        string affiliateCode = oRow["affiliateCode"].ToString(); //SRC Code
                                        string offerCode = oRow["offerGUID"].ToString();
                                        string domainURL = oRow["domainURL"].ToString();

                                        //Check if expired first

                                        if (expireDate.ToString() != "")
                                        {
                                            if (expireDate <= DateTime.Now)
                                            {
                                                //This link has expired - so send to the root of the site.
                                                context.Response.Redirect(domainURL);
                                            }
                                        }

                                        //OK-Build the link

                                        string redirect = targetURL.ToString();
                                        string strTemp = redirect.Substring(redirect.Length - 1);

                                        //Add a slash to the end of the target URL if not specified.
                                        if (strTemp != "/")
                                        {
                                            redirect += "/";
                                        }

                                        strTemp = redirect.Substring(redirect.Length - 1);

                                        if (strTemp != "?")
                                        {
                                            redirect += "?";
                                        }

                                        //Add our affiliate code - src
                                        if (affiliateCode.ToString() != "")
                                        {
                                            redirect += "src=" + affiliateCode.ToString();
                                        }

                                        //Add an Offer code if one exists
                                        if (offerCode.ToString() != "")
                                        {
                                            strTemp = redirect.Substring(redirect.Length - 1);

                                            if (strTemp != "&")
                                            {
                                                redirect += "&";
                                            }

                                            redirect += "shnq=" + offerCode.ToString();
                                        }

                                        //Add a var1 if one exists
                                        if (var1.ToString() != "")
                                        {
                                            strTemp = redirect.Substring(redirect.Length - 1);

                                            if (strTemp != "&")
                                            {
                                                redirect += "&";
                                            }

                                            redirect += "var1=" + var1.ToString();
                                        }

                                        //Add a transactionid if one exists
                                        if (transactionId.ToString() != "")
                                        {
                                            strTemp = redirect.Substring(redirect.Length - 1);

                                            if (strTemp != "&")
                                            {
                                                redirect += "&";
                                            }

                                            redirect += "transactionid=" + transactionId.ToString();
                                        }

                                        context.Response.Redirect(redirect);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message.ToString());//EmailError("Error connecting to the database. Error Details: '" + ex.Message + "'");
                    }
                    finally
                    {
                        dbconn.Close();
                    }
                }

            }
        }
        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
