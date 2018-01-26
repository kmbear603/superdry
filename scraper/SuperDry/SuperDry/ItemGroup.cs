using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDry
{
    class ItemGroup : Scrapable
    {
        public Item[] Items { get; private set; }

        public ItemGroup(Item[] items)
            :base(string.Join(",", Array.ConvertAll(items, itm => itm.Url.Substring(itm.Url.LastIndexOf('/')))))
        {
            this.Items = items;
        }

        protected override void ScrapeCore(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status)
        {
            for (int retry = 0; retry < 30; retry++)
            {
                try
                {
                    // stop current loading on browser
                    browser.Stop();

                    // clear bag
                    RemoveAllItemsInBag(browser);

                    // add all items to bag
                    foreach (var item in this.Items)
                    {
                        bool sold_out;
                        item.GetProductDetailAndAddToBag(browser, update_status, out sold_out);
                        item.SoldOut = sold_out;
                    }

                    // check out
                    Item.CheckoutAndSelectDomesticDelivery(browser);

                    // set checkout price
                    GetCheckoutPriceByRemovingItemOneByOne(browser);

                    // report status
                    foreach (var item in this.Items)
                    {
                        if (!item.Success.HasValue)
                            item.Success = true;

                        if (item.SoldOut.HasValue && item.SoldOut.Value)
                            continue;

                        item.SoldOut = false;
                        item.WriteProgress();
                        update_status(System.Threading.Thread.CurrentThread.ManagedThreadId + ": " + string.Join(" ", item.Title, item.Color, item.OriginalPrice.ToString("f2"), item.CheckoutPrice.ToString("f2")));
                    }
                    return;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(2000);
                }
            }

            foreach (var item in this.Items)
                item.WriteFailed();

            update_status("group failed");
        }

        private void GetCheckoutPriceByRemovingItemOneByOne(BrowserSession.BrowserSession browser)
        {
            var total = Item.GetTotalPriceOnCheckoutPage(browser);

            for (int i = 0; i < this.Items.Length; i++)
            {
                var item = this.Items[i];
                if (item.SoldOut.HasValue && item.SoldOut.Value)
                    continue;

                var first_item = browser.DefaultFrame.FindElementByClassName("consignment-item")[0];
                var info_container = first_item.FindElementByClassName("consignment-item-info")[0];
                var name_of_first_item = info_container.FindElementByTagName("p")[0].Text.Trim();

                bool is_last_item = first_item.HasClass("last-item");

                float checkout_price_of_this_item;
                if (is_last_item)
                    checkout_price_of_this_item = total;
                else
                {
                    Item.RemoveItemFromCheckoutPage(browser, 0);
                    var new_total = Item.GetTotalPriceOnCheckoutPage(browser);
                    checkout_price_of_this_item = total - new_total;
                    total = new_total;
                }

                item.CheckoutPrice = checkout_price_of_this_item;

                if (is_last_item)
                    break;
            }
        }

        private void RemoveAllItemsInBag(BrowserSession.BrowserSession browser)
        {
            // open bag page
#if kill
            browser.GoTo(URL.BagUrl);
#else // kill
            NagivateBrowserToUrlWithTimeout(browser, URL.BagUrl, TimeSpan.FromMinutes(1));
#endif // kill

            DateTime start = DateTime.Now;
            while (DateTime.Now < start + TimeSpan.FromMinutes(5))
            {
                browser.ExecuteJavascriptAsync("document.body.scrollTop = document.documentElement.scrollTop = 0;").Wait();
                //var remove_button = browser.DefaultFrame.FindElementById("BasketRemove:0");
                var remove_button = browser.DefaultFrame.FindElementByXPath("//span[starts-with(@id, 'BasketRemove:')]");
                if (remove_button == null)
                    return;

                try
                {
                    //while (!remove_button.IsVisibleOnWindow)
                    //    browser.ExecuteJavascriptAsync("window.scrollTo(0, 10)").Wait();
                    remove_button.ClickWithoutScrollAsync().Wait();
                    //remove_button.ClickAsync().Wait();
                }
                catch (Exception ex)
                {
                    while (!browser.IsCompletedLoading)
                        System.Threading.Thread.Sleep(1000);
                }
            }

            throw new Exception("failed to clear item from bag");
        }
    }
}
