using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;

namespace LuaExtensions
{
    public class Curl
    {
        public Curl(Script script)
        {
            this.script = script;
        }

        public DynValue Table()
        {
            DynValue tableVal = DynValue.NewTable(script);
            Table table = tableVal.Table;
            table["OPT_POST"] = DynValue.NewString("OPT_POST");
            table["OPT_POSTFIELDS"] = DynValue.NewString("OPT_POSTFIELDS");
            table["OPT_ACCEPT_ENCODING"] = DynValue.NewString("OPT_ACCEPT_ENCODING");
            table["OPT_USERAGENT"] = DynValue.NewString("OPT_USERAGENT");
            table["OPT_COOKIE"] = DynValue.NewString("OPT_COOKIE");
            table["OPT_PROXY"] = DynValue.NewString("OPT_PROXY");
            table["INFO_REDIRECT_URL"] = DynValue.NewString("INFO_REDIRECT_URL");
            table["INFO_RESPONSE_CODE"] = DynValue.NewString("INFO_RESPONSE_CODE");
            table["easy"] = (Func<DynValue>)Easy;
            return tableVal;
        }

        private DynValue Easy()
        {
            DynValue tableVal = DynValue.NewTable(script);
            Table table = tableVal.Table;

            // Functions
            table["setopt_url"] = (Action<Table, string>)SetOptUrl;
            table["setopt_writefunction"] = (Action<Table, DynValue>)SetOptWriteFunction;
            table["setopt"] = (Action<Table, string, DynValue>)SetOpt;
            table["getinfo"] = (Func<Table, string, DynValue>)GetInfo;
            table["perform"] = (Action<Table>)Perform;
            table["close"] = (Action<Table>)Close;

            // Private data
            int pdataid = nextId++;
            table["pdataid"] = DynValue.NewNumber(pdataid);
            internalDataMap[pdataid] = new InternalData();

            return tableVal;
        }

        private void SetOptUrl(Table easy, string url)
        {
            InternalData internalData = GetInternalData(easy);
            internalData.url = url;
        }

        private void SetOptWriteFunction(Table easy, DynValue func)
        {
            InternalData internalData = GetInternalData(easy);
            internalData.dataFunc = func;
        }

        private void SetOpt(Table easy, string opt, DynValue value)
        {
            InternalData internalData = GetInternalData(easy);
            internalData.optionsMap[opt] = value;
        }

        private DynValue GetInfo(Table easy, string info)
        {
            InternalData internalData = GetInternalData(easy);
            switch (info)
            {
                case "INFO_REDIRECT_URL":
                    return DynValue.NewString(internalData.redirectUrl);

                case "INFO_RESPONSE_CODE":
                    return DynValue.NewNumber(internalData.responseCode);
            }
            return DynValue.NewNil();
        }

        private void Perform(Table easy)
        {
            InternalData internalData = GetInternalData(easy);
            if (internalData == null || internalData.url == null)
            {
                return;
            }

            bool doPost = internalData.optionsMap["OPT_POST"]?.CastToBool() ?? false;
            string proxy = internalData.optionsMap["OPT_PROXY"]?.CastToString() ?? "";
            string cookies = internalData.optionsMap["OPT_COOKIE"]?.CastToString() ?? "";
            string userAgent = internalData.optionsMap["OPT_USERAGENT"]?.CastToString() ?? "";
            string[] cookiePairs = cookies.Split(';');

            Uri fullUri = new Uri(internalData.url);
            Uri baseAddress = new Uri(fullUri.GetLeftPart(UriPartial.Authority));
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler()
            {
                CookieContainer = cookiePairs.Length > 0 ? cookieContainer : null,
                UseCookies = cookiePairs.Length > 0,
                Proxy = proxy.Length > 0 ? new WebProxy(proxy) : null,
                UseProxy = proxy.Length > 0,
                AllowAutoRedirect = false,
            }
            )
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                if (userAgent.Length > 0)
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                }

                foreach (string cookiePair in cookiePairs)
                {
                    string[] vals = cookiePair.Trim().Split('=');
                    if (vals.Length == 2)
                    {
                        cookieContainer.Add(baseAddress, new Cookie(vals[0].Trim(), vals[1].Trim()));
                    }
                }

                try
                {
                    HttpResponseMessage response;
                    if (!doPost)
                    {
                        response = client.GetAsync(fullUri.PathAndQuery).Result;
                    }
                    else
                    {
                        string postContent = internalData.optionsMap["OPT_POSTFIELDS"]?.CastToString() ?? "";
                        response = client.PostAsync(fullUri.PathAndQuery, new StringContent(postContent)).Result;
                    }
                    internalData.responseCode = (int)response.StatusCode;
                    if (internalData.responseCode >= 300 && internalData.responseCode < 400)
                    {
                        internalData.redirectUrl = response.Headers.Location.ToString();
                    }
                    else if (response.IsSuccessStatusCode)
                    {
                        if (internalData.dataFunc != null)
                        {
                            internalData.dataFunc.Function.Call(DynValue.NewString(response.Content.ReadAsStringAsync().Result));
                        }
                    }
                }
                catch (Exception)
                {
                    internalData.responseCode = 500;
                }
            }
        }

        private void Close(Table easy)
        {
            internalDataMap.Remove((int)easy.Get("pdataid").Number);
        }

        private class InternalData
        {
            public Dictionary<string, DynValue> optionsMap = new Dictionary<string, DynValue>();
            public string url = null;
            public DynValue dataFunc = null;
            public string redirectUrl = null;
            public int responseCode = 0;
        }
        private InternalData GetInternalData(Table easy)
        {
            return internalDataMap[(int)easy.Get("pdataid").Number];
        }

        private readonly Script script;
        private readonly Dictionary<int, InternalData> internalDataMap = new Dictionary<int, InternalData>();
        private int nextId = 1;
    }
}
