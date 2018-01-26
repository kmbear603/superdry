using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDry
{
    abstract class Scrapable
    {
        public Scrapable(string url)
        {
            this.Url = url;
        }

        protected abstract void ScrapeCore(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status);

        public string Url { get; private set; }

        protected static void NagivateBrowserToUrlWithTimeout(BrowserSession.BrowserSession browser, string url, TimeSpan timeout)
        {
            AutoResetEvent ev = new AutoResetEvent(false);

            var thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    browser.GoTo(url);
                    ev.Set();
                }
                catch (ThreadAbortException)
                {
                }
            }));
            thread.Start();

            if (!ev.WaitOne(timeout))
            {
                browser.Stop();
                Thread.Sleep(5000);
                thread.Abort();
                throw new TimeoutException();
            }
        }

#if kill
        public void Scrape(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status, TimeSpan timeout)
        {
            AutoResetEvent ev = new AutoResetEvent(false);

            var thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    this.ScrapeCore(browser, update_status);
                }
                catch (ThreadAbortException) { }

                ev.Set();
            }));

            thread.Start();

            if (!ev.WaitOne(timeout))
            {
                thread.Abort();
                throw new TimeoutException();
            }
        }
#else // kill
        public void Scrape(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status)
        {
            ScrapeCore(browser, update_status);
        }
#endif // kill
    }
}
