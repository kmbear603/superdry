using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SuperDry
{
    class Item : Scrapable
    {
        public string Id;
        public string Title;
        public string ImageUrl;
        public float OriginalPrice;
        public float CheckoutPrice;
        public string Color;
        public string[] Sizes;

        public Item(string item_url)
            : base(item_url)
        {
            this.Id = ExtractProductIdFromUrl(item_url);
        }

        public static string ExtractProductIdFromUrl(string url)
        {
            // eg. https://www.superdry.com/us/womens/sale-jackets/details/63882/microfibre-windbomber-jacket-black
            string temp = url;
            var pos = temp.LastIndexOf('/');
            temp = temp.Substring(0, pos);
            pos = temp.LastIndexOf('/');
            temp = temp.Substring(pos + 1);
            return temp;
        }

        public void GetProductDetailAndAddToBag(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status, out bool sold_out)
        {
            sold_out = false;

            // navigate to the item page
#if kill
            browser.GoTo(this.Url);
#else // kill
            NagivateBrowserToUrlWithTimeout(browser, this.Url, TimeSpan.FromMinutes(1));
#endif // kill

            // find the detail element
            BrowserSession.WebElement.WebElement product_detail_container;
            {
                var product_detail_containers = browser.DefaultFrame.FindElementByClassName("product-detail");
                if (product_detail_containers == null || product_detail_containers.Count < 1)
                    throw new Exception("cant find .product-detail");
                product_detail_container = product_detail_containers[0];
            }

            // title
            {
                var title_containers = product_detail_container.FindElementByTagName("h1");
                if (title_containers == null || title_containers.Count < 1)
                    throw new Exception("cant find .product-detail h1");
                this.Title = title_containers[0].Text.Trim();
            }

            // image
            {
                var image_container = browser.DefaultFrame.FindElementById<BrowserSession.WebElement.Image>("main-image");
                if (image_container == null)
                    throw new Exception("cant find #image_container");
                this.ImageUrl = image_container.Src;
            }

            // original price
            {
                var price_info_containers = product_detail_container.FindElementByClassName("price");
                if (price_info_containers == null || price_info_containers.Count < 1)
                    throw new Exception("cant find .product-detail .price");
                string txt = string.Concat(price_info_containers[0].Text.ToCharArray().Where(c => c == '.' || (c >= '0' && c <= '9')));
                this.OriginalPrice = float.Parse(txt);
            }

            // color
            {
                var color_container = product_detail_container.FindElementById("product-colour");
                if (color_container == null)
                    throw new Exception("cant find .product-detail #product-colour");
                this.Color = color_container.Text.Trim();
            }

            // sizes
            {
                List<string> options = new List<string>();

                var available_sizes_container = product_detail_container.FindElementById("available-sizes");
                if (available_sizes_container != null)
                {
                    var select_containers = product_detail_container.FindElementByTagName("select");
                    if (select_containers != null && select_containers.Count == 1)
                    {
                        var option_containers = available_sizes_container.FindElementByTagName<BrowserSession.WebElement.Option>("option");
                        if (option_containers != null)
                        {
                            foreach (var option_container in option_containers)
                            {
                                if (option_container.Value == "0")
                                    continue;

                                options.Add(option_container.InnerHTML.Trim().Replace("\"", "\'"));

                                // select this size for later stage of add-to-bag
                                browser.ExecuteJavascriptAsync("document.getElementById('product_id').value='" + option_container.Value + "';").Wait();
                            }
                        }
                    }
                }

                this.Sizes = options.ToArray();
            }

            // add to bag
            {
                var form = product_detail_container.FindElementById<BrowserSession.WebElement.Form>("add-to-bag");
                if (form == null)
                {
                    sold_out = true;
                    return;
                    //throw new Exception("cant find .product-detail #add-to-bag");
                }

                var cart_count_container = browser.DefaultFrame.FindElementById("minicart-count");
                if (cart_count_container == null)
                    throw new Exception("cant find #minicart-count");

                var original_count = cart_count_container.Text.Trim();

                form.SubmitAsync().Wait();

                // wait until cart is updated
                int failed = 0;
                bool added = false;
                DateTime start_wait = DateTime.Now;
                while (DateTime.Now < start_wait + TimeSpan.FromMinutes(1))
                {
                    try
                    {
                        cart_count_container = browser.DefaultFrame.FindElementById("minicart-count");
                        var new_count = cart_count_container.Text.Trim();
                        if (original_count != new_count)
                        {
                            added = true;
                            break;
                        }
                    }
                    catch
                    {
                        failed++;
                        if (failed >= 50)
                            throw new Exception("cant update cart");
                    }

                    Task.Delay(200).Wait();
                }

                if (!added)
                {
                    sold_out = true;
                    update_status(this.Title + " is sold out");
                    return;
                }
            }
        }

        public static void CheckoutAndSelectDomesticDelivery(BrowserSession.BrowserSession browser)
        {
#if kill
            browser.GoTo(URL.CheckoutUrl);
#else // kill
            NagivateBrowserToUrlWithTimeout(browser, URL.CheckoutUrl, TimeSpan.FromMinutes(1));
#endif // kill

            // select domestic delivery
            {
                var domestic_button = browser.DefaultFrame.FindElementById<BrowserSession.WebElement.Anchor>("delivery-type-domestic");
                if (domestic_button == null)
                    throw new Exception("cant find #delivery-type-domestic");
                domestic_button.ClickAsync().Wait();
                Task.Delay(1000).Wait();
            }
        }

        public static float GetTotalPriceOnCheckoutPage(BrowserSession.BrowserSession browser)
        {
            // get summary container first
            var summary_container = browser.DefaultFrame.FindElementById("checkout-section-basket-summary");
            if (summary_container == null)
                throw new Exception("cant find #checkout-section-basket-summary");

            // get the last div with class "summary-item"
            BrowserSession.WebElement.WebElement div;
            {
                var summary_item_containers = summary_container.FindElementByClassName("summary-item");
                if (summary_item_containers == null || summary_item_containers.Count < 1)
                    throw new Exception("cant find #checkout-section-basket-summary .summary-item");
                div = summary_item_containers[summary_item_containers.Count - 1];
            }

            // price is stored at the second div
            {
                var divs = div.FindElementByTagName("div");
                if (divs == null || divs.Count != 3)
                    throw new Exception("unknown format inside #checkout-section-basket-summary .summary-item");
                var txt = string.Concat(divs[1].Text.Trim().ToArray().Where(c => c == '.' || (c >= '0' && c <= '9')));
                return float.Parse(txt);
            }
        }

        public static void RemoveItemFromCheckoutPage(BrowserSession.BrowserSession browser, int item_index)
        {
            var remove_button_containers = browser.DefaultFrame.FindElementByClassName<BrowserSession.WebElement.Anchor>("remove-consignment-line");
            if (remove_button_containers == null || remove_button_containers.Count < 1)
                throw new Exception("cant find .remove-consignment-line");

            // click cross button
            remove_button_containers[item_index].ClickWithoutScrollAsync().Wait();

            // click yes button
            var modal = browser.DefaultFrame.FindElementById("remove-consignment-modal");
            if (modal == null)
                throw new Exception("cant find #remove-consignment-modal");

            Func<BrowserSession.WebElement.Button> get_yes_button = () =>
            {
                try
                {
                    var buttons = modal.FindElementByClassName<BrowserSession.WebElement.Button>("btn-primary");
                    if (buttons == null || buttons.Count != 1)
                        throw new Exception("cant find #remove-consignment-modal .btn-primary");
                    return buttons[0];
                }
                catch   // stale exception
                {
                    return null;
                }
            };

            // wait until yes button appears
            DateTime start_wait = DateTime.Now;
            while (DateTime.Now < start_wait + TimeSpan.FromMinutes(1))
            {
                try
                {
                    var yes_button = get_yes_button();
                    if (yes_button != null)
                        break;
                }
                catch { }
                Task.Delay(100).Wait();
            }
            if (get_yes_button() == null)
                throw new Exception("yes button doesnt appear");

            //get_yes_button().ClickWithoutScrollAsync().Wait();

            // wait until yes button disappears
            start_wait = DateTime.Now;
            while (DateTime.Now < start_wait + TimeSpan.FromMinutes(1))
            {
                try
                {
                    var yes_button = get_yes_button();
                    if (yes_button == null)
                        break;
                    yes_button.ClickWithoutScrollAsync().Wait();
                }
                catch { }
                Task.Delay(500).Wait();
            }

            if (get_yes_button() != null)
                throw new Exception("yes button doesnt disappear");
        }

        protected override void ScrapeCore(BrowserSession.BrowserSession browser, UpdateStatusDelegate update_status)
        {
            for (int trial = 0; trial < 5; trial++)
            {
                try
                {
                    // stop current loading on browser
                    browser.Stop();

                    bool sold_out;
                    GetProductDetailAndAddToBag(browser, update_status, out sold_out);
                    if (sold_out)
                    {
                        SoldOut = true;
                        return;
                    }

                    // get checkout price by checking out
                    {
                        var original_tab = browser.CurrentTabHandle;

                        // open a new tab to show cart
                        try
                        {
                            // find the checkout tab
                            string checkout_tab = null;
                            {
                                var tabs = browser.AllTabHandles;
                                foreach (var tab in tabs)
                                {
                                    browser.SwitchToTab(tab);
                                    if (browser.Url == URL.CheckoutUrl)
                                    {
                                        checkout_tab = tab;
                                        break;
                                    }
                                }
                            }

                            if (checkout_tab == null)
                            {
                                // no checkout tab yet

                                // create tab
                                checkout_tab = browser.CreateNewTabAsync().Result;
                                browser.SwitchToTab(checkout_tab);
                            }

                            CheckoutAndSelectDomesticDelivery(browser);

                            // remove all items in bag except the last one
                            {
                                var remove_button_containers = browser.DefaultFrame.FindElementByClassName<BrowserSession.WebElement.Anchor>("remove-consignment-line");
                                if (remove_button_containers == null || remove_button_containers.Count < 1)
                                    throw new Exception("cant find .remove-consignment-line");
                                for (int i = 0; i < remove_button_containers.Count - 1; i++)
                                    RemoveItemFromCheckoutPage(browser, 0);
                            }

                            // get the total price in cart
                            this.CheckoutPrice = GetTotalPriceOnCheckoutPage(browser);
                        }
                        finally
                        {
                            // switch back to original tab
                            browser.SwitchToTab(original_tab);
                        }
                    }

                    WriteProgress();

                    update_status(string.Join(" ", this.Title, this.Color, this.OriginalPrice.ToString("f2"), this.CheckoutPrice.ToString("f2")));

                    Success = true;
                    SoldOut = false;
                    return;
                }
                catch (Exception ex)
                {
                    update_status("failed=" + this.Url + " trial=" + trial + " err=" + ex.Message);
                }
            }

            WriteFailed();
            Success = false;
        }

        public bool? Success
        {
            get; set;
        } = null;

        public bool? SoldOut
        {
            get; set;
        } = null;

        private static Object _WriteLock = new Object();
        private static bool _FirstWriteProgress = true;
        public void WriteProgress()
        {
            try
            {
                lock (_WriteLock)
                {
                    var file_name = "progress.csv";
                    if (_FirstWriteProgress)
                    {
                        if (File.Exists(file_name))
                            File.Delete(file_name);
                    }

                    Func<string, string> wrap_with_quote = str =>
                    {
                        return "\"" + str + "\"";
                    };

                    string line = string.Join(",",
                        new string[]
                        {
                            wrap_with_quote(this.Id),
                            wrap_with_quote(this.Title),
                            wrap_with_quote(this.Color),
                            this.OriginalPrice.ToString("f2"),
                            this.CheckoutPrice.ToString("f2"),
                            wrap_with_quote(this.Sizes != null ? string.Join(" / ", this.Sizes) : ""),
                            this.Url,
                        }
                    );

                    for (int retry = 0; retry < 10; retry++)
                    {
                        try
                        {
                            using (var sw = new StreamWriter(file_name, true))
                                sw.WriteLine(line);
                            break;
                        }
                        catch { }
                    }

                    _FirstWriteProgress = false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static bool _FirstWriteFailed = true;
        public void WriteFailed()
        {
            try
            {
                lock (_WriteLock)
                {
                    var file_name = "failed.log";

                    if (_FirstWriteFailed)
                    {
                        if (File.Exists(file_name))
                            File.Delete(file_name);
                    }

                    for (int retry = 0; retry < 10; retry++)
                    {
                        try
                        {
                            using (var sw = new StreamWriter(file_name, true))
                                sw.WriteLine(DateTime.Now.ToString("yyyyMMdd HHmmssfff") + " " + this.Url);
                            break;
                        }
                        catch { }
                    }

                    _FirstWriteFailed = false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
