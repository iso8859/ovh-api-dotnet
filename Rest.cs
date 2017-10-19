using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace ovh.api
{
    public class Rest
    {
        string _ak, _as, _ck;
        public Rest(string AK, string AS, string CK)
        {
            _ak = AK;
            _as = AS;
            _ck = CK;
        }

        public static string[] HexTbl = Enumerable.Range(0, 256).Select(v => v.ToString("x2")).ToArray();
        public static string ToHex(IEnumerable<byte> array)
        {
            StringBuilder s = new StringBuilder();
            foreach (var v in array)
                s.Append(HexTbl[v]);
            return s.ToString();
        }
        public static string ToHex(byte[] array)
        {
            StringBuilder s = new StringBuilder(array.Length * 2);
            foreach (var v in array)
                s.Append(HexTbl[v]);
            return s.ToString();
        }

        public static string HashSHA1(string sInputString)
        {
            return ToHex(System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(sInputString)));
        }

        public static int TimeStamp()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public string Signature(string method, string query, string body, int timestamp)
        {
            //calcul de la signature
            return "$1$" + HashSHA1(_as + "+" + _ck + "+" + method + "+" + query + "+" + body + "+" + timestamp);
        }

        public HttpClient Client()
        {
            var result = new HttpClient();
            foreach (var h in Headers)
                result.DefaultRequestHeaders.Add(h.Key, h.Value);
            return result;
        }

        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string query, string body = null)
        {
            var request = new HttpRequestMessage(method, query);
            if (body != null)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            var client = Client();
            client.DefaultRequestHeaders.Add("X-Ovh-Application", _ak);
            client.DefaultRequestHeaders.Add("X-Ovh-Consumer", _ck);
            int timestamp = TimeStamp();
            client.DefaultRequestHeaders.Add("X-Ovh-Signature", Signature(method.Method.ToUpper(), query, body, timestamp));
            client.DefaultRequestHeaders.Add("X-Ovh-Timestamp", timestamp.ToString());

            var response = await client.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception("Request failed:" + await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            return response;
        }

        public async Task<T> GetAsync<T>(string uri)
        {
            var response = await SendAsync(HttpMethod.Get, uri);
            response.EnsureSuccessStatusCode();
            string s = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(s);
        }

        public async Task<T> PostAsync<T>(string uri, object data = null)
        {
            var response = await SendAsync(HttpMethod.Post, uri, JsonConvert.SerializeObject(data));
            response.EnsureSuccessStatusCode();
            var s = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(s);
        }

        public async Task<T> PutAsync<T>(string uri, object data = null)
        {
            var response = await SendAsync(HttpMethod.Put, uri, JsonConvert.SerializeObject(data));
            response.EnsureSuccessStatusCode();
            var s = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(s);
        }
    }
}
