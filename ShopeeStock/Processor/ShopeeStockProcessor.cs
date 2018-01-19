using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using ShopeeStock.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShopeeStock.Processor
{
    public static class ShopeeStockProcessor
    {
        public static List<Excel> GetStockByAccount(IWebDriver driver, WebDriverWait wait, string account)
        {
            List<Excel> output = new List<Excel>();

            int page = 0;
            string url = $@"https://shopee.tw/{ account }?page={ page }";
            driver.Navigate().GoToUrl(url);

            try
            {
                wait.Until(x => x.FindElement(By.ClassName("shop-search-result-view")));
                wait.Until(x => x.FindElement(By.ClassName("shop-search-result-view__item")));
            }
            catch
            {
                return output;
            }

            HtmlDocument accountDoc = new HtmlDocument();
            accountDoc.LoadHtml(driver.PageSource);

            // 取得最大頁數
            if (!int.TryParse(accountDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'shopee-mini-page-controller__total')]").InnerText, out int totalPage))
            {
                return output;
            }

            while (page < totalPage)
            {
                HtmlNodeCollection products = accountDoc.DocumentNode.SelectNodes("//*[contains(@class, 'shop-search-result-view__item')]");

                if (products.Count > 0)
                {
                    foreach (HtmlNode product in products)
                    {
                        string productUrl = $"https://shopee.tw{ product.SelectSingleNode("a").GetAttributeValue("href", "") }";
                        driver.Navigate().GoToUrl(productUrl);

                        try
                        {
                            wait.Until(x => x.FindElement(By.ClassName("shopee-product-detail")));
                            wait.Until(x => x.FindElement(By.ClassName("shopee-product-info-body__order-quantity__stock-count")));
                        }
                        catch
                        {
                            continue;
                        }

                        HtmlDocument productDoc = new HtmlDocument();
                        productDoc.LoadHtml(driver.PageSource);

                        // 是否含有規格資料
                        bool hasSpec = (productDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'shopee-product-info-body__variations-row')]") != null);

                        if (hasSpec)
                        {
                            var variations = driver.FindElements(By.ClassName("product-variation"));

                            foreach (var variation in variations)
                            {
                                // shopee-product-info__header__text

                                try
                                {
                                    variation.Click();
                                }
                                catch
                                {
                                    output.Add(new Excel
                                    {
                                        Account = account,
                                        ProductName = "",
                                        ProductPrice = "",
                                        ProductSpec = variation.Text,
                                        ProductStock = "Error"
                                    });
                                    continue;
                                }

                                productDoc.LoadHtml(driver.PageSource);
                                variation.GetAttribute("class");

                                HtmlNode productName = productDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'shopee-product-info__header__text')]");
                                // shopee-product-info__header__real-price
                                HtmlNode productPrice = productDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'shopee-product-info__header__real-price')]");
                                //shopee-product-info-body__order-quantity__stock-count
                                HtmlNode productStock = productDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'shopee-product-info-body__order-quantity__stock-count')]");
                                Regex rgx = new Regex(@"還剩(?<Stock>[0-9]*)件");
                                GroupCollection groups = rgx.Match(productStock.InnerText).Groups;
                                rgx = new Regex(@"product-variation--disabled");

                                if (!rgx.IsMatch(variation.GetAttribute("class")))
                                {
                                    output.Add(new Excel
                                    {
                                        Account = account,
                                        ProductName = productName.InnerText,
                                        ProductPrice = productPrice.InnerText,
                                        ProductSpec = variation.Text,
                                        ProductStock = groups["Stock"].Value
                                    });
                                }
                                else
                                {
                                    output.Add(new Excel
                                    {
                                        Account = account,
                                        ProductName = productName.InnerText,
                                        ProductPrice = productPrice.InnerText,
                                        ProductSpec = variation.Text,
                                        ProductStock = "0"
                                    });
                                }
                            }

                        }
                        else
                        {
                            // shopee-product-info__header__text
                            HtmlNode productName = productDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'shopee-product-info__header__text')]");
                            // shopee-product-info__header__real-price
                            HtmlNode productPrice = productDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'shopee-product-info__header__real-price')]");
                            //shopee-product-info-body__order-quantity__stock-count
                            HtmlNode productStock = productDoc.DocumentNode.SelectSingleNode("//*[contains(@class, 'shopee-product-info-body__order-quantity__stock-count')]");
                            Regex rgx = new Regex(@"還剩(?<Stock>[0-9]*)件");
                            GroupCollection groups = rgx.Match(productStock.InnerText).Groups;

                            output.Add(new Excel
                            {
                                Account = account,
                                ProductName = productName.InnerText,
                                ProductPrice = productPrice.InnerText,
                                ProductSpec = "",
                                ProductStock = groups["Stock"].Value
                            });
                        }
                    }
                }

                ++page;
                if (page == totalPage)
                {
                    break;
                }

                url = $@"https://shopee.tw/{ account }?page={ page }";
                driver.Navigate().GoToUrl(url);

                try
                {
                    wait.Until(x => x.FindElement(By.ClassName("shop-search-result-view")));
                    wait.Until(x => x.FindElement(By.ClassName("shop-search-result-view__item")));
                }
                catch
                {
                    continue;
                }

                accountDoc = new HtmlDocument();
                accountDoc.LoadHtml(driver.PageSource);
            }

            return output;
        }
    }
}
