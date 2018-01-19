using Caliburn.Micro;
using ClosedXML.Excel;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ShopeeStock.Models;
using ShopeeStock.Processor;
using ShopeeStock.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ShopeeStock.ViewModels
{
    public class ShellViewModel : Screen, IDataErrorInfo
    {
        public ShellViewModel()
        {
            Initialize();
        }

        #region -- Properties --

        private BindableCollection<string> _accounts = new BindableCollection<string>();
        private string _selectedAccount;
        private string _account = "";

        public BindableCollection<string> Accounts
        {
            get { return _accounts; }
            set
            {
                _accounts = value;
                NotifyOfPropertyChange(() => Accounts);
            }
        }

        public string SelectedAccount
        {
            get { return _selectedAccount; }
            set
            {
                _selectedAccount = value;
                NotifyOfPropertyChange(() => SelectedAccount);
                NotifyOfPropertyChange(() => CanSingleAccountQuery);
                NotifyOfPropertyChange(() => CanRemoveAccount);
            }
        }

        public string Account
        {
            get { return _account; }
            set
            {
                _account = value;
                IsAccountValid(value);
                NotifyOfPropertyChange(() => Account);
                NotifyOfPropertyChange(() => CanAddAccount);
            }
        }

        #endregion

        #region -- Methods --

        private void Initialize()
        {
            LoadAccounts();
            SelectedAccount = null;
            Account = "";
        }

        private void LoadAccounts()
        {
            if (File.Exists(GlobalConfig.AccountFilePath))
            {
                Accounts = new BindableCollection<string>(File.ReadAllLines(GlobalConfig.AccountFilePath).ToList());
            }
            else
            {
                Accounts = new BindableCollection<string>();
            }
        }

        private void SaveAccounts()
        {
            File.WriteAllLines(GlobalConfig.AccountFilePath, Accounts.ToList());
        }

        public void WindowClosing()
        {
            SaveAccounts();
        }

        public bool CanAddAccount
        {
            get
            {
                return IsAccountValid(Account);
            }
        }

        public void AddAccount()
        {
            Accounts.Add(Account);
            Account = "";
        }

        public bool CanRemoveAccount
        {
            get
            {
                return (SelectedAccount != null);
            }
        }

        public void RemoveAccount()
        {
            Accounts.Remove(SelectedAccount);
            SelectedAccount = null;
        }

        public bool CanSingleAccountQuery
        {
            get { return (SelectedAccount != null); }
        }

        public void SingleAccountQuery()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--disable-infobars");
            IWebDriver driver = new ChromeDriver(options);
            driver.Manage().Window.Maximize();
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5.00));

            List<Excel> data = new List<Excel>();
            ShopeeStockProcessor.GetStockByAccount(driver, wait, SelectedAccount).ForEach(x => data.Add(x));
            driver.Quit();

            if (data.Count > 0)
            {
                WriteExcel(data);
            }
        }

        public void MultiAccountsQuery()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--disable-infobars");
            IWebDriver driver = new ChromeDriver(options);
            driver.Manage().Window.Maximize();
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5.00));

            List<Excel> data = new List<Excel>();
            foreach (string account in Accounts)
            {
                ShopeeStockProcessor.GetStockByAccount(driver, wait, account).ForEach(x => data.Add(x));
            }

            driver.Quit();

            if (data.Count > 0)
            {
                WriteExcel(data);
            }
        }

        public void AppClosing()
        {
            TryClose();
        }

        private void WriteExcel(List<Excel> data)
        {
            string filepath = GlobalConfig.ExcelFilePath;
            XLWorkbook wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Sheet1");

            ws.Row(1).Cell(1).SetValue("賣場帳號");
            ws.Row(1).Cell(2).SetValue("商品名稱");
            ws.Row(1).Cell(3).SetValue("商品價格");
            ws.Row(1).Cell(4).SetValue("商品規格");
            ws.Row(1).Cell(5).SetValue("商品庫存");

            foreach (Excel d in data)
            {
                int rowIndex = ws.LastRowUsed().RowNumber() + 1;

                ws.Row(rowIndex).Cell(1).SetValue(d.Account);
                ws.Row(rowIndex).Cell(2).SetValue(d.ProductName);
                ws.Row(rowIndex).Cell(3).SetValue(d.ProductPrice);
                ws.Row(rowIndex).Cell(4).SetValue(d.ProductSpec);
                ws.Row(rowIndex).Cell(5).SetValue(d.ProductStock);
            }

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
            wb.SaveAs(filepath);

            // 開啟檔案
            Process.Start(filepath);
        }

        #endregion

        #region -- IDataErrorInfo Member --

        public string Error
        {
            get { return null; }
        }

        public string this[string propertyName]
        {
            get
            {
                return (!errors.ContainsKey(propertyName) ? null : String.Join(Environment.NewLine, errors[propertyName]));
            }
        }

        #endregion

        #region -- Validations --

        private Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

        public void AddError(string propertyName, string error)
        {
            if (!errors.ContainsKey(propertyName))
            {
                errors[propertyName] = new List<string>();
            }

            if (!errors[propertyName].Contains(error))
            {
                errors[propertyName].Add(error);
            }
        }

        public void RemoveError(string propertyName, string error)
        {
            if (errors.ContainsKey(propertyName) && errors[propertyName].Contains(error))
            {
                errors[propertyName].Remove(error);

                if (errors[propertyName].Count == 0)
                {
                    errors.Remove(propertyName);
                }
            }
        }

        public bool IsAccountValid(string value)
        {
            bool isValid = true;
            Regex rgx = new Regex(@"[^A-z.0-9]");

            if (String.IsNullOrEmpty(value) && String.IsNullOrWhiteSpace(value))
            {
                AddError("Account", "必須輸入帳號");
                isValid = false;
            }
            else
            {
                RemoveError("Account", "必須輸入帳號");
            }

            if (rgx.IsMatch(value))
            {
                AddError("Account", "帳號不可包含非英文字元");
                isValid = false;
            }
            else
            {
                RemoveError("Account", "帳號不可包含非英文字元");
            }

            if (Accounts.Any(x => x == value))
            {
                AddError("Account", "該帳號已存在清單中");
                isValid = false;
            }
            else
            {
                RemoveError("Account", "該帳號已存在清單中");
            }

            return isValid;
        }

        #endregion
    }
}
