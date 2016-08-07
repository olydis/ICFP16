using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ICFP16
{
    class WC : WebClient
    {
        static WC instance = new WC();
        public static WC GetInstance()
        {
            return instance;
        }

        private WC() : base()
        {
            Headers.Add("X-API-Key", "113-47b74a2f778ff27c368558e78b0b72b0");
            Headers.Add("Accept-Encoding", "gzip");
            Encoding = Encoding.UTF8;
            last = DateTime.Now;
        }
        int throttle = 1300;
        DateTime last;

        private void Throttle()
        {
            var now = DateTime.Now;
            var elapsed = (int)(now - last).TotalMilliseconds;
            if (elapsed < throttle)
                Thread.Sleep(throttle - elapsed);
            last = DateTime.Now;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = 5000;
            request.ServicePoint.Expect100Continue = false;
            return request;
        }

        private string Get(string url)
        {
            Throttle();
            Debug.WriteLine(url);
            string res = null;
            redo:
            try
            {
                Throttle();
                res = this.DownloadString("http://2016sv.icfpcontest.org/api/" + url);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                goto redo;
            }
            return res;
        }
        private string Post(string url, NameValueCollection data)
        {
            Debug.WriteLine(url);
            string res = null;
            redo:
            try
            {
                Throttle();
                var resB = this.UploadValues("http://2016sv.icfpcontest.org/api/" + url, data);
                res = Encoding.UTF8.GetString(resB);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("400"))
                    throw new FormatException();
                if (e.Message.Contains("own problem"))
                    throw new NotImplementedException("own problem");
                Debug.WriteLine(e);
                goto redo;
            }
            return res;
        }
        private string GetBlob(string hash)
        {
            return Get("blob/" + hash);
        }
        public snapshot GetRecentSnapshot()
        {
            var res = JObject.Parse(Get("snapshot/list"));
            var snapshots = res["snapshots"]
                .OrderByDescending(x => x.Value<int>("snapshot_time"))
                .Select(x => x.Value<string>("snapshot_hash"))
                .ToArray();
            return JObject.Parse(GetBlob(snapshots[0])).ToObject<snapshot>();
        }
        public string GetProbDesc(string hash)
        {
            var res = GetBlob(hash);
            return res;
        }
        public JObject PostSolution(int id, string spec)
        {
            NameValueCollection data = new NameValueCollection();
            data.Add("problem_id", id.ToString());
            data.Add("solution_spec", spec);
            var res = Post("solution/submit", data);
            return JObject.Parse(res);
        }
    }
}
