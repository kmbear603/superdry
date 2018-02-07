using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace SuperDry
{
    class Uploader
    {
        public Uploader(string base_url)
        {
            this.BaseUrl = base_url.Trim().TrimEnd('/');
        }

        public string BaseUrl
        {
            get; set;
        }

        public void Upload(Item item)
        {
            var url = this.BaseUrl + "/item/" + item.Id;
            var json_text = item.SaveAsJsonString();

            var content = new StringContent(json_text, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                using (var res = client.PostAsync(url, content).Result)
                {
                    res.EnsureSuccessStatusCode();
                }
            }
        }

        public void Delete(string id)
        {
            var url = this.BaseUrl + "/item/" + id;

            using (var client = new HttpClient())
            {
                using (var res = client.DeleteAsync(url).Result)
                {
                    res.EnsureSuccessStatusCode();
                }
            }
        }

        /// <summary>
        /// get all product ids on server
        /// </summary>
        /// <returns></returns>
        public string[] List()
        {
            var url = this.BaseUrl + "/item";

            using (var client = new HttpClient())
            {
                using (var res = client.GetAsync(url).Result)
                {
                    res.EnsureSuccessStatusCode();
                    var json_text = res.Content.ReadAsStringAsync().Result;
                    var arr = Newtonsoft.Json.Linq.JArray.Parse(json_text);
                    List<string> ids = new List<string>();
                    foreach (var jt in arr)
                        ids.Add((string)jt);
                    return ids.ToArray();
                }
            }
        }
    }
}
