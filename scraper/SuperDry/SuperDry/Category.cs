using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDry
{
    class Category : Scrapable
    {
        public Category(string category_url)
            : base(category_url)
        {
        }

        /// <summary>
        /// get url of all items in this category page
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="update_status"></param>
        /// <returns></returns>
        private string[] GetAllItemUrls(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status)
        {
            for (int trial = 0; trial < 10; trial++)
            {
                try
                {
                    // stop current loading on browser
                    browser.Stop();

                    // go to the category page
#if kill
                    browser.GoTo(this.Url);
#else // kill
                    NagivateBrowserToUrlWithTimeout(browser, this.Url, TimeSpan.FromMinutes(1));
#endif // kill

                    var item_urls = new HashSet<string>();

                    while (true)
                    {
                        var product_containers = browser.DefaultFrame.FindElementByClassName("hproduct");
                        if (product_containers == null)
                            break;

                        bool added = false;
                        foreach (var product_container in product_containers)
                        {
                            var anchors = product_container.FindElementByTagName<BrowserSession.WebElement.Anchor>("a");
                            if (anchors == null)
                                continue;
                            foreach (var anchor in anchors)
                            {
                                if (item_urls.Contains(anchor.Href))
                                    continue;
                                item_urls.Add(anchor.Href);
                                added = true;
                            }
                        }

                        if (!added) // no more
                            break;

#if future
                        // scroll to bottom 5 times to load more items
                        for (int i = 0; i < 5; i++)
                            browser.ExecuteJavascriptAsync("window.scrollTo(0, document.body.scrollHeight);").Wait();
#else // future
                        break;
#endif // future
                    }

                    if (item_urls.Count == 0)
                        throw new Exception("force retry");

                    return item_urls.ToArray();
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(5000);
                }
            }

            update_status("category failed: " + this.Url);
            return null;
        }

        protected override void ScrapeCore(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status)
        {
            Items = null;

            var items = new Dictionary<string, Item>();

            var item_urls = GetAllItemUrls(browser, update_status);
            if (item_urls != null)
            {
                foreach (var item_url in item_urls)
                {
                    var item_worker = new Item(item_url);
                    if (items.ContainsKey(item_worker.Id))
                        continue;
                    items.Add(item_worker.Id, item_worker);
                }
            }

            this.Items = items.Values.ToList();
            update_status(this.Url + ": " + this.Items.Count + " items");
        }

        public IReadOnlyList<Item> Items
        {
            get; private set;
        }
    }
}
