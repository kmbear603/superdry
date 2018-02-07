using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SuperDry
{
    class Configuration
    {
        public Configuration(string file_name)
        {
            this.FileName = file_name;
        }

        private Dictionary<string, string> _KeyValues = new Dictionary<string, string>();

        public string FileName
        {
            get; set;
        }

        public void Load()
        {
            _KeyValues.Clear();

            if (!File.Exists(this.FileName))
                return;

            var json_text = File.ReadAllText(this.FileName);

            JObject json = JObject.Parse(json_text);
            foreach (var prop in json.Properties())
                _KeyValues.Add(prop.Name, (string)prop);
        }

        public string UploadBaseUrl
        {
            get
            {
                if (!_KeyValues.ContainsKey("upload-url"))
                    return null;
                return _KeyValues["upload-url"];
            }
        }
    }
}
