using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDry
{
    class URL
    {
        private const string US = "/us";
        private const string URL_PREFIX = "https://www.superdry.com";
        private const string MEN = "/mens";
        private const string WOMEN = "/womens";
        private const string BAG = "/shopping-bag";
        private const string CHECKOUT = "/checkout";
        private const string CHECKOUT_LOGIN = "/checkout/log-in";

        public static string BagUrl
        {
            get { return URL_PREFIX + US + BAG; }
        }

        public static string CheckoutUrl
        {
            get { return URL_PREFIX + US + CHECKOUT; }
        }

        public static string CheckoutLoginUrl
        {
            get { return URL_PREFIX + US + CHECKOUT_LOGIN; }
        }

        public static string[] CategoryUrls
        {
            get
            {
                return new string[]
                {
                    URL_PREFIX + US + MEN + "/sale-jackets",
                    URL_PREFIX + US + MEN + "/sale-hoodies",
                    URL_PREFIX + US + MEN + "/sale-knitwear",
                    URL_PREFIX + US + MEN + "/sale-polos",
                    URL_PREFIX + US + MEN + "/sale-tops",
                    URL_PREFIX + US + MEN + "/sale-t-shirts",
                    URL_PREFIX + US + MEN + "/sale-sport",
                    URL_PREFIX + US + MEN + "/sale-accessories",
                    URL_PREFIX + US + MEN + "/sale-shirts",
                    URL_PREFIX + US + MEN + "/sale-footwear",
                    URL_PREFIX + US + MEN + "/sale-trousers",
                    URL_PREFIX + US + MEN + "/sale-shorts",

                    URL_PREFIX + US + WOMEN + "/sale-jackets",
                    URL_PREFIX + US + WOMEN + "/sale-hoodies",
                    URL_PREFIX + US + WOMEN + "/sale-t-shirts",
                    URL_PREFIX + US + WOMEN + "/sale-dresses",
                    URL_PREFIX + US + WOMEN + "/sale-sport",
                    URL_PREFIX + US + WOMEN + "/sale-tops",
                    URL_PREFIX + US + WOMEN + "/sale-knitwear",
                    URL_PREFIX + US + WOMEN + "/sale-skirts-a-shorts",
                    URL_PREFIX + US + WOMEN + "/sale-trousers",
                    URL_PREFIX + US + WOMEN + "/sale-accessories",
                    URL_PREFIX + US + WOMEN + "/sale-shirts",
                    URL_PREFIX + US + WOMEN + "/sale-footwear",
                };
            }
        }
    }
}
