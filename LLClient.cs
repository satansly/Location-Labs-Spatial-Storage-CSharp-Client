
//  Created by Omar Hussain on 12/12/11.
//  Copyright 2011 Omar Hussain. All rights reserved.
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.	



using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;
using System.Net;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace USVIHAWebApplication.LocationLabs
{
    public class HttpError : Exception
    {
        public String status;
        public String reason;
        public String url;
        public String method;
		/*
		 *  Custom exception
		 */
		 
        public HttpError(String status, String reason, String url, String method)
        {
            this.status = status;
            this.reason = reason;
            this.url = url;
            this.method = method;
        }
		/*
		 * Formats exception into human readable string
		 */
        public String ToString()
        {
            return String.Format("{0} {1} ({2} {3})", this.status, this.reason, this.method, this.url);
        }
    }
    public class LLProperty
    {
        private string name = null;
        private string value = null;

        public LLProperty(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public string Name
        {
            get { return name; }
        }

        public string Value
        {
            get { return value; }
        }
    }
    public class LLProperties
    {
        public Dictionary<String, String> properties;
        public LLProperties(Dictionary<String, String> _properties)
        {
            this.properties = _properties;
        }
    }
    public class LLContent
    {
        public String id;
        public double lon;
        public double lat;
        //public String label;
        //public DateTime time;
		
        public Dictionary<String, String> properties;

        [JsonConstructor()]
        public LLContent(String id, double lon, double lat, String label, DateTime time, Dictionary<String, String> properties)
        {
            this.id = id;
            this.lon = lon;
            this.lat = lat;
            //this.label = label;
            //this.time = time;
            this.properties = properties;
        }

        public LLContent(String id, double lon, double lat, String label, DateTime time, LLProperties properties)
        {
            this.id = id;
            this.lon = lon;
            this.lat = lat;
            //this.label = label;
            //this.time = time;
            this.properties = properties.properties;
        }
        public String ToString()
        {
            return JsonConvert.SerializeObject(new LLContent[] { this });
        }
    }
    public class LLCircle
    {
        public double lon;
        public double lat;
        public double radius;
        public LLCircle(double lon, double lat, double radius)
        {
            this.lon = lon;
            this.lat = lat;
            this.radius = radius;
        }
        public String ToQueryString()
        {
            return "lat=" + lat.ToString() + "&lon=" + lon.ToString() + "&radius=" + radius.ToString();

        }
        public String ToString()
        {
            return String.Format("Within {0} meters of ({1},{2})", this.radius, this.lat, this.lon);
        }
    }
    public class LLClient
    {
        public String SiteUrl;
        public String ConsumerKey;
        public String ConsumerSecret;
        public String OauthSignatureMethod;
        public String OauthToken;
        public String OauthTokenSecret;
        public String OauthVersion;

        public LLClient(String url, String oauth_token, String oauth_token_secret, String oauth_consumer_key, String oauth_consumer_secret)
        {
            this.SiteUrl = url;
            this.ConsumerKey = Encode(oauth_consumer_key);
            this.ConsumerSecret = Encode(oauth_consumer_secret);
            this.OauthSignatureMethod = "HMAC-SHA1";
            this.OauthToken = Encode(oauth_token);
            this.OauthTokenSecret = Encode(oauth_token_secret);
            this.OauthVersion = "1.0";


        }
        public LLClient()
        {
            System.Configuration.Configuration rootWebConfig1 =
                System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~/Web.config");
            if (rootWebConfig1.AppSettings.Settings.Count > 0)
            {
                System.Configuration.KeyValueConfigurationElement SiteUrlKeyVal =
                       rootWebConfig1.AppSettings.Settings["SiteUrl"];
                System.Configuration.KeyValueConfigurationElement ConsumerKeyKeyVal =
                    rootWebConfig1.AppSettings.Settings["ConsumerKey"];
                System.Configuration.KeyValueConfigurationElement ConsumerSecretKeyVal =
                    rootWebConfig1.AppSettings.Settings["ConsumerSecret"];
                System.Configuration.KeyValueConfigurationElement OauthTokenKeyVal =
                                    rootWebConfig1.AppSettings.Settings["OauthToken"];
                System.Configuration.KeyValueConfigurationElement OauthTokenSecretKeyVal =
                    rootWebConfig1.AppSettings.Settings["OauthTokenSecret"];
                if (SiteUrlKeyVal != null &&
                    ConsumerKeyKeyVal != null &&
                    ConsumerSecretKeyVal != null &&
                    OauthTokenKeyVal != null &&
                    OauthTokenSecretKeyVal != null)
                {
                    this.SiteUrl = SiteUrlKeyVal.Value;
                    this.ConsumerKey = Encode(ConsumerKeyKeyVal.Value);
                    this.ConsumerSecret = Encode(ConsumerSecretKeyVal.Value);
                    this.OauthSignatureMethod = "HMAC-SHA1";
                    this.OauthToken = Encode(OauthTokenKeyVal.Value);
                    this.OauthTokenSecret = Encode(OauthTokenSecretKeyVal.Value);
                    this.OauthVersion = "1.0";
                }



            }
        }
        public String Encode(String key)
        {
            return Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(key));
        }
		/// <summary>
		/// Makes OAuth request and executes.
		/// </summary>
		/// <param name="http_url">
		/// A <see cref="String"/>
		/// </param>
		/// <param name="http_method">
		/// A <see cref="String"/>
		/// </param>
		/// <param name="http_headers">
		/// A <see cref="Dictionary<String, String>"/>
		/// </param>
		/// <param name="body">
		/// A <see cref="String"/>
		/// </param>
		/// <param name="parameters">
		/// A <see cref="List<KeyValuePair<String, String>>"/>
		/// </param>
		/// <returns>
		/// A <see cref="String"/> 
		/// </returns>
        public String MakeRequest(String http_url, String http_method, Dictionary<String, String> http_headers, String body, List<KeyValuePair<String, String>> parameters)
        {


            String auth_header = "OAuth realm=\"\", oauth_version=\"" + this.OauthVersion + "\",";
            auth_header += " oauth_signature=\"" + this.ConsumerSecret + "&" + this.OauthTokenSecret + "\",";
            auth_header += " oauth_signature_method=\"PLAINTEXT\", oauth_token=\"" + this.OauthToken + "\",";
            auth_header += " oauth_consumer_key=\"" + this.ConsumerKey + "\"";

            HttpWebRequest connection = (HttpWebRequest)HttpWebRequest.Create(http_url);
            connection.Method = http_method;
            connection.ContentType = "application/json; charset=UTF-8";
            connection.Headers.Add(HttpRequestHeader.Authorization, auth_header);
            if (connection.Method == "POST" || connection.Method == "PUT" || connection.Method == "DELETE")
            {
                byte[] reqData = Encoding.UTF8.GetBytes(body);
                connection.ContentLength = reqData.Length;
                Stream reqStream = connection.GetRequestStream();
                reqStream.Write(reqData, 0, reqData.Length);
            }
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)connection.GetResponse();
                Stream respStream = response.GetResponseStream();
                byte[] respData = new byte[response.ContentLength];
                respStream.Read(respData, 0, respData.Length);
                String respString = Encoding.UTF8.GetString(respData);
                return respString;
            }
            catch (WebException exc)
            {
                HttpWebResponse errResp = (HttpWebResponse)exc.Response;
                throw new HttpError(errResp.StatusCode.ToString(), errResp.StatusDescription.ToString(), http_url.ToString(), http_method);
            }

        }
		/// <summary>
		/// Creates a new geofence.
		/// </summary>
		/// <param name="content">
		/// A <see cref="LLContent"/> object containing properties with which to populate the new geofence.
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/> response indicating success or failure of the operation.
		/// </returns>
        public bool Create(LLContent content)
        {
            try
            {
                Dictionary<String, String> http_headers = new Dictionary<string, string>();
                http_headers.Add("Accept", "application/json");
                http_headers.Add("Content-Type", "application/json");
                String body = content.ToString();
                this.MakeRequest(this.SiteUrl, "POST", http_headers, body, null);
                return true;
            }
            catch (HttpError err)
            {
                return false;
            }
        }
		/// <summary>
		/// Searches POIs within the limit indicated by <see cref="LLCircle"/> object.
		/// </summary>
		/// <param name="circle">
		/// A <see cref="LLCircle"/> object indicating the circle to search within.
		/// </param>
		/// <returns>
		/// A <see cref="List<LLContent>"/> array is returned consisting of the POIs found within the searched radius.
		/// </returns>
        public List<LLContent> Search(LLCircle circle)
        {
            List<LLContent> pois = new List<LLContent>();
            try
            {
                String http_url = this.SiteUrl + "?" + circle.ToQueryString();
                Dictionary<String, String> http_headers = new Dictionary<string, string>();
                http_headers.Add("Content-Type", "application/json");

                String response = this.MakeRequest(http_url, "GET", http_headers, "", null);
                pois = (List<LLContent>)JsonConvert.DeserializeObject<List<LLContent>>(response);
            }
            catch (HttpError err)
            {
                
            }
            return pois;
        }
		/// <summary>
		/// Delete a geofence referred by the provided id.
		/// </summary>
		/// <param name="id">
		/// A <see cref="String"/> identifying the geofence to be deleted.
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/> indicating success or failure of the operation
		/// </returns>
        public bool Delete(String id)
        {
            try
            {
                String http_url = this.SiteUrl + "/" + HttpUtility.HtmlEncode(id);
                this.MakeRequest(http_url, "DELETE", null, "", null);
                return true;
            }
            catch (HttpError err)
            {
                return false;
            }
        }
		/// <summary>
		/// Gets the geofence referred by the provided id.
		/// </summary>
		/// <param name="id">
		/// A <see cref="String"/> identifying geofence to be fetched
		/// </param>
		/// <returns>
		/// A <see cref="LLContent"/> object if call is successful. Null is returned otherwise.
		/// </returns>
        public LLContent Get(String id)
        {
            try
            {
                String http_url = this.SiteUrl + "/" + HttpUtility.HtmlEncode(id);
                Dictionary<String, String> http_headers = new Dictionary<string, string>();
                http_headers.Add("Content-Type", "application/json");
                String body = this.MakeRequest(http_url, "GET", http_headers, "", null);
                return JsonConvert.DeserializeObject<LLContent>(body);
            }
            catch (HttpError err)
            {
                return null;
            }
        }
		/// <summary>
		/// Updates an existing geofence referred by the provided id.
		/// </summary>
		/// <param name="id">
		/// A <see cref="String"/> identify the geofence to be updated
		/// </param>
		/// <param name="properties">
		/// A <see cref="LLProperties"/> object with updated values
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/> indicating success or failure of the call
		/// </returns>
        public bool Update(String id, LLProperties properties)
        {
            try
            {
                String http_url = this.SiteUrl + "/" + HttpUtility.HtmlEncode(id);
                Dictionary<String, String> http_headers = new Dictionary<string, string>();
                http_headers.Add("Accept", "application/json");
                http_headers.Add("Content-Type", "application/json");
                String body = JsonConvert.SerializeObject(properties, Formatting.Indented);
                this.MakeRequest(http_url, "PUT", http_headers, body, null);
                return true;
            }
            catch (HttpError err)
            {
                return false;
            }

        }
    }
    public class Utils
    {
        /// <summary>
        /// method for converting a System.DateTime value to a UNIX Timestamp
        /// </summary>
        /// <param name="value">date to convert</param>
        /// <returns></returns>
        public static double ConvertToTimestamp(DateTime value)
        {
            //create Timespan by subtracting the value provided from
            //the Unix Epoch
            TimeSpan span = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

            //return the total seconds (which is a UNIX timestamp)
            return (double)span.TotalSeconds;
        }
        private String GenerateNonce()
        {
            byte[] nonce = Encoding.ASCII.GetBytes(DateTime.Now.ToString("s"));
            return Convert.ToBase64String(nonce);
        }
    }

    
}