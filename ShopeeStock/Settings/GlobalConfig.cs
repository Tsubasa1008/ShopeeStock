using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopeeStock.Settings
{
    public static class GlobalConfig
    {
        public static string AccountFilePath
        {
            get
            {
                return $@"{ Directory.GetCurrentDirectory() }\AccountList.txt";
            }
        }

        public static string ExcelFilePath
        {
            get
            {
                string filepath = $@"{ Directory.GetCurrentDirectory() }\Result\{ DateTime.Now.ToString("yyyyMMddHHmmss") }_shopeestock.xlsx";
                FileInfo fp = new FileInfo(filepath);
                fp.Directory.Create();

                return filepath;
            }
        }
    }
}
