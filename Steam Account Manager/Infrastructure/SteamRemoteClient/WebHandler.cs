using SteamKit2;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Steam_Account_Manager.Infrastructure.SteamRemoteClient
{
    internal sealed class WebHandler
    {
        internal static readonly string SteamCommunityDomain = "steamcommunity.com";

        public string Token { get; private set; }
        public string SessionID { get; private set; }
        public string TokenSecure { get; private set; }
        private CookieContainer _cookies = new CookieContainer();

        public string AcceptLanguageHeader { get { return acceptLanguageHeader; } set { acceptLanguageHeader = value; } }
        private string acceptLanguageHeader =
            Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName == "en" ?
            Thread.CurrentThread.CurrentCulture.ToString() + ",en;q=0.8" :
            Thread.CurrentThread.CurrentCulture.ToString() + "," + Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName + ";q=0.8,en;q=0.6";

        internal string Fetch(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = true)
        {
            using (HttpWebResponse response = Request(url, method, data, ajax, referer, fetchError))
            {
                if (response == null)
                    return String.Empty;

                using (Stream responseStream = response.GetResponseStream())
                {
                    // If the response stream is null it cannot be read. So return an empty string.
                    if (responseStream == null)
                    {
                        return "";
                    }
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        internal async Task<string> AsyncFetch(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = true)
        {
            return await Task.Factory.StartNew(() =>
            {
                using (HttpWebResponse response = Request(url, method, data, ajax, referer, fetchError))
                {
                    if (response == null)
                        return null;

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null)
                        {
                            return null;
                        }
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            });
        }

        public HttpWebResponse Request(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = true)
        {
            // Append the data to the URL for GET-requests.
            bool isGetMethod = (method.ToLower() == "get");
            string dataString = data == null ? null : String.Join("&", Array.ConvertAll(data.AllKeys, key =>
                string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(data[key]))
            ));

            // Append the dataString to the url if it is a GET request.
            if (isGetMethod && !string.IsNullOrEmpty(dataString))
            {
                url += (url.Contains("?") ? "&" : "?") + dataString;
            }

            // Setup the request.
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "application/json, text/javascript;q=0.9, */*;q=0.5";
            request.Headers[HttpRequestHeader.AcceptLanguage] = AcceptLanguageHeader;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            // request.Host is set automatically.
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
            request.Referer = string.IsNullOrEmpty(referer) ? "http://steamcommunity.com/trade/1" : referer;
            request.Timeout = 50000; // Timeout after 50 seconds.
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            // If the request is an ajax request we need to add various other Headers, defined below.
            if (ajax)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7");
            }

            // Cookies
            request.CookieContainer = _cookies;
            // If the request is a GET request return now the response. If not go on. Because then we need to apply data to the request.
            if (isGetMethod || string.IsNullOrEmpty(dataString))
            {
                try
                {
                    return request.GetResponse() as HttpWebResponse;
                }
                catch (WebException e)
                {
#if DEBUG
                    System.Windows.Forms.MessageBox.Show(e.ToString());
#endif
                    return null;
                }
            }

            // Write the data to the body for POST and other methods.
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
            request.ContentLength = dataBytes.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(dataBytes, 0, dataBytes.Length);
            }

            // Get the response and return it.
            try
            {
                return request.GetResponse() as HttpWebResponse;
            }
            catch (WebException e)
            {
                //this is thrown if response code is not 200
                if (fetchError)
                {
#if DEBUG
                    System.Windows.Forms.MessageBox.Show(e.ToString());
#endif
                }
                throw;
            }
        }

        public bool Authenticate(string myUniqueId, SteamClient client, string myLoginKey)
        {
            Token = TokenSecure = String.Empty;
            SessionID = Convert.ToBase64String(Encoding.UTF8.GetBytes(myUniqueId));

            _cookies = new CookieContainer();

            using (dynamic userAuth = WebAPI.GetInterface("ISteamUserAuth"))
            {

                // Generate an AES session key.
                var sessionKey = CryptoHelper.GenerateRandomBlock(32);

                // rsa encrypt it with the public key for the universe we're on
                byte[] cryptedSessionKey;

                using (RSACrypto rsa = new RSACrypto(KeyDictionary.GetPublicKey(client.Universe)))
                {
                    cryptedSessionKey = rsa.Encrypt(sessionKey);
                }
                byte[] loginKey = new byte[20];

                Array.Copy(Encoding.ASCII.GetBytes(myLoginKey), loginKey, myLoginKey.Length);

                // AES encrypt the loginkey with our session key.
                byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

                KeyValue authResult;

                // Get the Authentification Result.

                using (dynamic iSteamUserAuth = WebAPI.GetInterface("ISteamUserAuth"))
                {
                    // iSteamUserAuth.Timeout = Timeout;

                    try
                    {
                        Dictionary<string, object> sessionArgs = new Dictionary<string, object>()
                      {
                          { "steamid", client.SteamID.ConvertToUInt64() },
                          { "sessionkey", cryptedSessionKey },
                          { "encrypted_loginkey", cryptedLoginKey }
                      };
                        authResult = userAuth.Call(HttpMethod.Post, "AuthenticateUser", args: sessionArgs);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        System.Windows.Forms.MessageBox.Show(e.ToString());
#endif
                        return false;
                    }
                }

                if (authResult == null)
                {
                    return false;
                }

                Token = authResult["token"].AsString();
                if (string.IsNullOrEmpty(Token))
                {
                    return false;
                }
                TokenSecure = authResult["tokensecure"].AsString();
                if (string.IsNullOrEmpty(TokenSecure))
                {
                    return false;
                }

                // Adding cookies to the cookie container.             -string.Empty
                _cookies.Add(new Cookie("sessionid", SessionID, "/", SteamCommunityDomain));
                _cookies.Add(new Cookie("steamLogin", Token, "/", SteamCommunityDomain));
                _cookies.Add(new Cookie("steamLoginSecure", TokenSecure, "/", SteamCommunityDomain));
                _cookies.Add(new Cookie("Steam_Language", "english", "/", SteamCommunityDomain));
                return true;
            }
        }

        public void Authenticate(IEnumerable<Cookie> cookies)
        {
            var cookieContainer = new CookieContainer();
            string token = null;
            string tokenSecure = null;
            string sessionId = null;
            foreach (var cookie in cookies)
            {
                if (cookie.Name == "sessionid")
                    sessionId = cookie.Value;
                else if (cookie.Name == "steamLogin")
                    token = cookie.Value;
                else if (cookie.Name == "steamLoginSecure")
                    tokenSecure = cookie.Value;
                cookieContainer.Add(cookie);
            }

            Token = token ?? throw new ArgumentException("Cookie with name \"steamLogin\" is not found.");
            TokenSecure = tokenSecure ?? throw new ArgumentException("Cookie with name \"steamLoginSecure\" is not found.");
            SessionID = sessionId ?? throw new ArgumentException("Cookie with name \"sessionid\" is not found.");
            _cookies = cookieContainer;
        }

        public bool VerifyCookies()
        {
            using (HttpWebResponse response = Request("https://steamcommunity.com/my/", "HEAD"))
            {
                return response.Cookies["steamLogin"] == null || !response.Cookies["steamLogin"].Value.Equals("deleted");
            }
        }

        static void SubmitCookies(CookieContainer cookies)
        {
            // Check, if the request is null.
            if (!(WebRequest.Create("https://steamcommunity.com/") is HttpWebRequest httpWebRequest))
            {
                return;
            }

            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.CookieContainer = cookies;
            httpWebRequest.ContentLength = 0;
            httpWebRequest.GetResponse().Close();
        }


    }
}
