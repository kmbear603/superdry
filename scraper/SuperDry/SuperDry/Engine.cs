using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SuperDry
{
    class Engine
    {
        /// <summary>
        /// number of simultaneous working threads
        /// </summary>
        private const int CONCURRENT_WORKER = 4;

        /// <summary>
        /// output json file name
        /// </summary>
        private const string OUTPUT_FILENAME = "items.json";

        /// <summary>
        /// unprocessed scrapers
        /// </summary>
        private List<IScrapable> _Scrapers = null;

        /// <summary>
        /// currently working scrapers
        /// </summary>
        private List<IScrapable> _WorkingScrapers = null;

#if kill
        /// <summary>
        /// output file writer
        /// </summary>
        private JsonTextWriter _OutputJson = null;
        private StreamWriter _OutputStream = null;
#endif // kill

        /// <summary>
        /// scrapped result
        /// </summary>
        private Dictionary<string, Item> _Items = null;

        /// <summary>
        /// browsers
        /// </summary>
        private List<BrowserSession.BrowserSession> _Browsers = null;

        /// <summary>
        /// worker threads
        /// </summary>
        private List<Thread> _WorkerThreads = null;

        /// <summary>
        /// callback function
        /// </summary>
        private UpdateStatusDelegate _StatusCallback = null;

        /// <summary>
        /// random generator
        /// </summary>
        private static Random _RandomGenerator = new Random(Environment.TickCount);

        private void Work()
        {
        }

        /// <summary>
        /// append to output json file
        /// </summary>
        /// <param name="item"></param>
        private void WriteToJson(JsonTextWriter _OutputJson, Item item)
        {
            _OutputJson.WriteStartObject();

            // url
            _OutputJson.WritePropertyName("url");
            _OutputJson.WriteValue(item.Url);

            // id
            _OutputJson.WritePropertyName("id");
            _OutputJson.WriteValue(item.Id);

            // title
            _OutputJson.WritePropertyName("name");
            _OutputJson.WriteValue(item.Title);

            // img
            _OutputJson.WritePropertyName("img");
            _OutputJson.WriteValue(item.ImageUrl);

            // price
            _OutputJson.WritePropertyName("price");
            _OutputJson.WriteValue(item.OriginalPrice);

            // checkout price
            _OutputJson.WritePropertyName("checkout-price");
            _OutputJson.WriteValue(item.CheckoutPrice);

            // sizes
            if (item.Sizes != null)
            {
                _OutputJson.WritePropertyName("sizes");
                _OutputJson.WriteStartArray();
                foreach (var s in item.Sizes)
                    _OutputJson.WriteValue(s);
                _OutputJson.WriteEndArray();
            }

            // color
            _OutputJson.WritePropertyName("color");
            _OutputJson.WriteValue(item.Color);

            _OutputJson.WriteEndObject();
        }

        /// <summary>
        /// get next unprocessed scraper
        /// </summary>
        /// <param name="exit">true if no more scraper will be processed</param>
        /// <returns>next unprocessed scraper</returns>
        private IScrapable GetUnprocessedScraper(out bool exit)
        {
            lock (_Scrapers)
            {
                exit = false;

                if (_Scrapers.Count > 0)
                {
                    // there is unprocessed scraper

#if future
                    var worker = _Scrapers[_RandomGenerator.Next(_Scrapers.Count)];
#else // future
                    var worker = _Scrapers.Find(s => s is Category);    // handle the categories first
                    if (worker == null)
                        worker = _Scrapers[_RandomGenerator.Next(_Scrapers.Count)];
#endif // future
                    _Scrapers.Remove(worker);

                    // mark the scraper as working
                    _WorkingScrapers.Add(worker);

                    return worker;
                }

                if (_WorkingScrapers.Count > 0)
                {
                    // someone is working, dont exit yet
                    return null;
                }

                // no more unprocessed scraper and working scraper, exit
                exit = true;
                return null;
            }
        }

        /// <summary>
        /// remove scraper from working list
        /// </summary>
        /// <param name="worker"></param>
        private void FinishProcessingScraper(IScrapable worker)
        {
            lock (_Scrapers)
            {
                int before = _WorkingScrapers.Count;
                _WorkingScrapers.Remove(worker);
                int after = _WorkingScrapers.Count;
                if (before == after)
                    throw new Exception();
            }
        }

        /// <summary>
        /// return true if no more scraping job is scheduled
        /// </summary>
        private bool IsFinishedAll
        {
            get
            {
                bool exit;
                var worker = GetUnprocessedScraper(out exit);
                return exit;
            }
        }

        private int _CreatedBrowserCount = 0;

        /// <summary>
        /// get idle browser
        /// </summary>
        /// <returns></returns>
        private BrowserSession.BrowserSession GetIdleBrowser()
        {
            BrowserSession.BrowserSession browser;
            lock (_Browsers)
            {
                if (_Browsers.Count == 0)
                {
                    if (_CreatedBrowserCount < CONCURRENT_WORKER)
                    {
                        _CreatedBrowserCount++;
                        var new_browser = new BrowserSession.ChromeBrowserSession();
                        new_browser.StartAsync(new BrowserSession.StartOption() { Headless = true }).Wait();
                        return new_browser;
                    }
                    else
                        return null;
                }
                browser = _Browsers[_RandomGenerator.Next(_Browsers.Count)];
                _Browsers.Remove(browser);
            }
            return browser;
        }

        /// <summary>
        /// number of unprocessed scraper queued
        /// </summary>
        private int UnprocessedScraperCount
        {
            get
            {
                lock (_Scrapers)
                {
                    return _Scrapers.Count;
                }
            }
        }

        /// <summary>
        /// number of working scraper queued
        /// </summary>
        private int WorkingScraperCount
        {
            get
            {
                lock (_Scrapers)
                {
                    return _WorkingScrapers.Count;
                }
            }
        }

        /// <summary>
        /// number of finished items
        /// </summary>
        private int FinishedItemCount
        {
            get
            {
                lock (_Items)
                {
                    return _Items.Count;
                }
            }
        }

        /// <summary>
        /// recycle a browser
        /// </summary>
        /// <param name="browser"></param>
        private void FinishUsingBrowser(BrowserSession.BrowserSession browser)
        {
            lock (_Browsers)
            {
                _Browsers.Add(browser);
            }
        }


        private bool GetOneAndProcess(out bool exit)
        {
            // get browser
            var browser = GetIdleBrowser();

            if (browser == null)
            {
                // no browser to use, skip
                exit = false;
                return false;
            }

            try
            {
                var worker = GetUnprocessedScraper(out exit);

                if (exit)   // need to exit
                    return false;
                else if (worker == null)
                {
                    // no worker and not exit yet, try again after 5 seconds
                    return false;
                }

                try
                {
                    // run the scraper job
                    worker.Scrape(browser, _StatusCallback);

                    if (worker is Category)
                    {
                        var category = worker as Category;
                        if (category.Items == null) // no item found in this category
                            return true;

                        int added_count = 0;
                        foreach (var item in category.Items)
                        {
                            lock (_Scrapers)
                                _Scrapers.Add(item);    // queue the item

                            added_count++;
#if DEBUGxx
                        if (added_count > 1)
                            break;
#endif // DEBUG
                        }
                    }
                    else if (worker is Item)
                    {
                        var item = worker as Item;

                        if (!item.Success)  // TODO: handle failure
                            return true;

                        lock (_Items)
                        {
                            if (_Items.ContainsKey(item.Id))    // already added to result
                                return true;
                            _Items.Add(item.Id, item);
                        }
                    }
                    else
                        throw new NotImplementedException();
                }
                finally
                {
                    FinishProcessingScraper(worker);
                }
            }
            finally
            {
                FinishUsingBrowser(browser);
            }

            return true;
        }

        private void WorkerFunc()
        {
            while (true)
            {
                bool exit;
                bool worked = GetOneAndProcess(out exit);
                if (exit)
                    break;
                else if (!worked)
                    Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// prepare for running worker threads
        /// </summary>
        private void Prepare(UpdateStatusDelegate update_status)
        {
            _StatusCallback = update_status;

            _Items = new Dictionary<string, Item>();

            // create Category worker from category urls and add to unprocessed list
            _Scrapers = new List<IScrapable>();
            foreach (var category_url in URL.CategoryUrls)
                _Scrapers.Add(new Category(category_url));

            _WorkingScrapers = new List<IScrapable>();

            _Browsers = new List<BrowserSession.BrowserSession>();
            _WorkerThreads = new List<Thread>();
            for (int i = 0; i < CONCURRENT_WORKER; i++)
            {
                var browser = new BrowserSession.ChromeBrowserSession();
                browser.StartAsync(new BrowserSession.StartOption() { Headless = true }).Wait();
                _Browsers.Add(browser);

                _WorkerThreads.Add(new Thread(new ThreadStart(WorkerFunc)));
            }

#if kill
            _OutputStream = new StreamWriter(OUTPUT_FILENAME);
            _OutputJson = new JsonTextWriter(_OutputStream)
            {
                Formatting = Formatting.None
            };
            _OutputJson.WriteStartArray();
#endif // kill
        }

        /// <summary>
        /// clean up
        /// </summary>
        private void CleanUp()
        {
            foreach (var browser in _Browsers)
                browser.Dispose();

#if kill
            _OutputJson.Close();
            _OutputJson = null;
            _OutputStream.Dispose();
            _OutputStream = null;
#endif // kill

            _WorkerThreads = null;
            _Browsers = null;
            _WorkingScrapers = null;
            _Scrapers = null;
            _Items = null;
            _StatusCallback = null;
        }

        /// <summary>
        /// save all finished items to items.json
        /// </summary>
        private void FlushToOutputJson()
        {
            lock (_Items)
            {
                using (var sw = new StreamWriter(OUTPUT_FILENAME))
                {
                    using (var writer = new JsonTextWriter(sw)
                    {
                        Formatting = Formatting.None
                    })
                    {
                        writer.WriteStartArray();

                        foreach (var item in _Items.Values)
                            WriteToJson(writer, item);

                        writer.WriteEndArray();
                    }
                }
            }
        }

        /// <summary>
        /// start all worker threads and wait until exit
        /// </summary>
        private void RunWorkerThreads()
        {
            // start threads
            foreach (var thread in _WorkerThreads)
                thread.Start();

            DateTime last_flush = DateTime.MinValue;

            // wait until all threads exited
            DateTime start_time = DateTime.Now;
            while (true)
            {
                bool all_exited = true;
                foreach (var thread in _WorkerThreads)
                    all_exited &= !thread.IsAlive;

                if (all_exited)
                    break;

                string estimate = "N/A";
                DateTime now = DateTime.Now;
                int finished = FinishedItemCount;
                int remaining = UnprocessedScraperCount;
                int working = WorkingScraperCount;

                if (finished > 0)
                {
                    int seconds_per_worker = (int)Math.Round((now - start_time).TotalSeconds / finished);
                    int seconds_remaining = seconds_per_worker * remaining;
                    TimeSpan estimate_timespan = TimeSpan.FromSeconds(seconds_remaining);
                    estimate = estimate_timespan.Hours + "h" + estimate_timespan.Minutes + "m";
                }

                _StatusCallback("completed: " + finished + ", working: " + working + ", remaining: " + UnprocessedScraperCount + ", estimate=" + estimate);

                if (DateTime.Now >= last_flush + TimeSpan.FromMinutes(5))
                {
                    FlushToOutputJson();
                    _StatusCallback("flushed to " + OUTPUT_FILENAME);
                    last_flush = DateTime.Now;
                }

                Thread.Sleep(15 * 1000);
            }
        }

        /// <summary>
        /// scrape
        /// </summary>
        /// <param name="update_status">callback function for working status</param>
        public void Run(UpdateStatusDelegate update_status)
        {
            Prepare(update_status);
            RunWorkerThreads();
            CleanUp();
        }
    }
}
