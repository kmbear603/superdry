using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDry
{
    interface IScrapable
    {
        void Scrape(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status);
        string Url { get; }
    }
}
