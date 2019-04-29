using Microsoft.AspNetCore.Mvc;
using API_Usage.DataAccess;
using API_Usage.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;

/*
 * Acknowledgments
 *  v1 of the project was created for the Fall 2018 class by Dhruv Dhiman, MS BAIS '18
 *    This example showed how to use v1 of the IEXTrading API
 *    
 *  Kartikay Bali (MS BAIS '19) extended the project for Spring 2019 by demonstrating 
 *    how to use similar methods to access Azure ML models
*/



namespace API_Usage.Controllers
{
    public class HomeController : Controller
    {
        public ApplicationDbContext dbContext;

        //Base URL for the IEXTrading API. Method specific URLs are appended to this base URL.
        string BASE_URL = "https://api.iextrading.com/1.0/";
        HttpClient httpClient;

        /// <summary>
        /// Initialize the database connection and HttpClient object
        /// </summary>
        /// <param name="context"></param>
        public HomeController(ApplicationDbContext context)
        {
            dbContext = context;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new
                System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public IActionResult Index()
        {
            return View();
        }

        /****
         * The Symbols action calls the GetSymbols method that returns a list of Companies.
         * This list of Companies is passed to the Symbols View.
        ****/
        public IActionResult Symbols()
        {
            //Set ViewBag variable first
            ViewBag.dbSucessComp = 0;
            List<Company> companies = GetSymbols();

            //Save companies in TempData, so they do not have to be retrieved again
            TempData["Companies"] = JsonConvert.SerializeObject(companies);
            //TempData["Companies"] = companies;

            return View(companies);
        }

        
        /****
         * The Chart action calls the GetChart method that returns 1 year's equities for the passed symbol.
         * A ViewModel CompaniesEquities containing the list of companies, prices, volumes, avg price and volume.
         * This ViewModel is passed to the Chart view.
        ****/
        /// <summary>
        /// The Chart action calls the GetChart method that returns 1 year's equities for the passed symbol.
        /// A ViewModel CompaniesEquities containing the list of companies, prices, volumes, avg price and volume.
        /// This ViewModel is passed to the Chart view.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public IActionResult Chart(string symbol)
        {
            //Set ViewBag variable first
            ViewBag.dbSuccessChart = 0;
            List<Equity> equities = new List<Equity>();

            if (symbol != null)
            {
                equities = GetChart(symbol);
                equities = equities.OrderBy(c => c.date).ToList(); //Make sure the data is in ascending order of date.
            }

            CompaniesEquities companiesEquities = getCompaniesEquitiesModel(equities);

            return View(companiesEquities);
        }

        /// <summary>
        /// Calls the IEX reference API to get the list of symbols
        /// </summary>
        /// <returns>A list of the companies whose information is available</returns>
        public List<Company> GetSymbols()
        {
            string IEXTrading_API_PATH = BASE_URL + "ref-data/symbols";
            string companyList = "";
            List<Company> companies = null;

            // connect to the IEXTrading API and retrieve information
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();

            // read the Json objects in the API response
            if (response.IsSuccessStatusCode)
            {
                companyList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            // now, parse the Json strings as C# objects
            if (!companyList.Equals(""))
            {
                // https://stackoverflow.com/a/46280739
                //JObject result = JsonConvert.DeserializeObject<JObject>(companyList);
                companies = JsonConvert.DeserializeObject<List<Company>>(companyList);
                companies = companies.GetRange(0, 20);
            }

            return companies;
        }

        public List<Company> GetSymbols1()
        {
            string IEXTrading_API_PATH = BASE_URL + "ref-data/symbols";
            string companyList = "";
            List<Company> companies = null;

            // connect to the IEXTrading API and retrieve information
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();

            // read the Json objects in the API response
            if (response.IsSuccessStatusCode)
            {
                companyList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            // now, parse the Json strings as C# objects
            if (!companyList.Equals(""))
            {
                // https://stackoverflow.com/a/46280739
                //JObject result = JsonConvert.DeserializeObject<JObject>(companyList);
                companies = JsonConvert.DeserializeObject<List<Company>>(companyList);
               
            }
            response.Content.Dispose();
            return companies;
        }

        /// <summary>
        /// Calls the IEX stock API to get 1 year's chart for the supplied symbol
        /// </summary>
        /// <param name="symbol">Stock symbol of the company whose quotes are to be retrieved</param>
        /// <returns></returns>
        public List<Equity> GetChart(string symbol)
        {
            // string to specify information to be retrieved from the API
            string IEXTrading_API_PATH = BASE_URL + "stock/" + symbol + "/batch?types=chart&range=1y";

            // initialize objects needed to gather data
            string charts = "";
            List<Equity> Equities = new List<Equity>();
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);

            // connect to the API and obtain the response
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();

            // now, obtain the Json objects in the response as a string
            if (response.IsSuccessStatusCode)
            {
                charts = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            // parse the string into appropriate objects
            if (!charts.Equals(""))
            {
                ChartRoot root = JsonConvert.DeserializeObject<ChartRoot>(charts,
                  new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                Equities = root.chart.ToList();
            }

            // fix the relations. By default the quotes do not have the company symbol
            //  this symbol serves as the foreign key in the database and connects the quote to the company
            foreach (Equity Equity in Equities)
            {
                Equity.symbol = symbol;
            }
            response.Content.Dispose();
            return Equities;
        }
        /// <summary>
        /// Call the ClearTables method to delete records from a table or all tables.
        ///  Count of current records for each table is passed to the Refresh View
        /// </summary>
        /// <param name="tableToDel">Table to clear</param>
        /// <returns>Refresh view</returns>
        public IActionResult Refresh(string tableToDel)
        {
            ClearTables(tableToDel);
            Dictionary<string, int> tableCount = new Dictionary<string, int>();
            tableCount.Add("Companies", dbContext.Companies.Count());
            tableCount.Add("Charts", dbContext.Equities.Count());
            tableCount.Add("Top Gainers", dbContext.GainersList.Count());
            tableCount.Add("Sector", dbContext.Sector.Count());
            tableCount.Add("Financial", dbContext.FinancialList.Count());
            tableCount.Add("Buy/Sell", dbContext.stockSuggest.Count());
            return View(tableCount);
        }

        /// <summary>
        /// save the quotes (equities) in the database
        /// </summary>
        /// <param name="symbol">Company whose quotes are to be saved</param>
        /// <returns>Chart view for the company</returns>
        public IActionResult SaveCharts(string symbol)
        {
            List<Equity> equities = GetChart(symbol);

            // save the quote if the quote has not already been saved in the database
            foreach (Equity equity in equities)
            {
                if (dbContext.Equities.Where(c => c.date.Equals(equity.date)).Count() == 0)
                {
                    dbContext.Equities.Add(equity);
                }
            }

            // persist the data
            dbContext.SaveChanges();

            // populate the models to render in the view
            ViewBag.dbSuccessChart = 1;
            CompaniesEquities companiesEquities = getCompaniesEquitiesModel(equities);
            return View("Chart", companiesEquities);
        }

        /// <summary>
        /// Use the data provided to assemble the ViewModel
        /// </summary>
        /// <param name="equities">Quotes to dsiplay</param>
        /// <returns>The view model to include </returns>
        public CompaniesEquities getCompaniesEquitiesModel(List<Equity> equities)
        {
            List<Company> companies = dbContext.Companies.ToList();

            if (equities.Count == 0)
            {
                return new CompaniesEquities(companies, null, "", "", "", 0, 0);
            }

            Equity current = equities.Last();

            // create appropriately formatted strings for use by chart.js
            string dates = string.Join(",", equities.Select(e => e.date));
            string prices = string.Join(",", equities.Select(e => e.high));
            float avgprice = equities.Average(e => e.high);

            //Divide volumes by million to scale appropriately
            string volumes = string.Join(",", equities.Select(e => e.volume / 1000000));
            double avgvol = equities.Average(e => e.volume) / 1000000;

            return new CompaniesEquities(companies, equities.Last(), dates, prices, volumes, avgprice, avgvol);
        }
        public IActionResult Stats()
        {
            //Set ViewBag variable first
            ViewBag.dbSucessComp = 0;
            
            List<Stats> stats = GetStats(GetSymbols()).Take(5).ToList();
            return View(stats);
        }

        public IActionResult BuyOrSell()
        {
            ViewBag.dbSucessComp = 0;
            List<Company> companies = GetSymbols1();
            List<outputModel> output = output1(companies);
            TempData["BuyOrSell"] = JsonConvert.SerializeObject(output);
            return View(output);
        }

        public List<Stats> GetStats(List<Company> companies)
        {
            string symbols = "";
            Dictionary<string, Dictionary<string, Stats>> statsDict = null;
            List<Stats> statsList = new List<Stats>();
            int start = 0;
            int end = 100;
            int iter = 100;
            List<Company> setCompanies = null;
            while (end <= companies.Count)
            {
                int count = 0;
                symbols = "";
                setCompanies = new List<Company>();
                setCompanies = companies.GetRange(start, iter);
                foreach (var company in setCompanies)
                {
                    count++;
                    symbols = symbols + company.symbol + ",";
                }


                string IEXTrading_API_PATH = BASE_URL + "stock/market/batch?symbols=" + symbols + "&types=quote&filter=symbol,companyName,close,week52High,week52Low";
                string statsResponse = "";
                statsDict = new Dictionary<string, Dictionary<string, Stats>>();

                HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    statsResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }

                if (!string.IsNullOrEmpty(statsResponse))
                {
                    statsDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Stats>>>(statsResponse);
                }

                foreach (var item in statsDict)
                {

                    foreach (var i in item.Value)
                    {
                        if (i.Value != null)
                        {
                            statsList.Add(i.Value);
                        }
                    }
                }


                start = end;
                end = end + 100;
                if (end > companies.Count)
                {
                    iter = end - companies.Count;
                }
                response.Content.Dispose();
            }
            return statsList;
        }

        public List<outputModel> output1(List<Company> companies)
        {
            List<Stats> stats = new List<Stats>();
            List<outputModel> statsResult = new List<outputModel>();
            outputModel value = null;
            stats = GetStats(companies);
            List<outputModel> statsList = new List<outputModel>();
            foreach (var i in stats)
            {
                value = new outputModel();
                value.symbol = i.symbol;
                value.companyName = i.companyName;
                value.close = i.close;
                if ((i.week52High - i.week52Low) != 0)
                {
                    value.value = ((i.close - i.week52Low) / (i.week52High - i.week52Low))*100;
                }
                statsList.Add(value);
                // statsResult = statsList.Where(a => a.value > 0.82).ToList();
                statsResult = statsList.OrderByDescending(a => a.value).Take(5).ToList();
            }
            foreach (var item in statsResult)
            {
                if (item.companyName.Equals(""))
                {
                    string companyList = "";
                    outputModel companyName = new outputModel();
                    String withoutLast = item.symbol.Substring(0, (item.symbol.Length - 1));
                    string IEXTrading_API_PATH = BASE_URL + "stock/" + withoutLast + "/company?filter=companyName";

                    HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        companyList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }

                    if (!companyList.Equals(""))
                    {
                        companyName = JsonConvert.DeserializeObject<outputModel>(companyList);
                    }

                    item.companyName = companyName.companyName;
                    response.Content.Dispose();
                }
            }
            return statsResult.OrderByDescending(a => a.value).ToList();
        }
        ///Function for getting the opening and closing stock prices 
        public IActionResult Gainers()
        {
            ViewBag.dbSucessComp = 0;

            List<GainersList> gainerList = getGainers();
            TempData["Gainer"] = JsonConvert.SerializeObject(gainerList.Take(5));
            return View("Top_Gainers",gainerList);
        }
        public List<GainersList> getGainers()
        {
            string IEXTrading_API_PATH = BASE_URL + "stock/market/list/gainers";
            string gList = "";
            List<GainersList> gainersList = null;

            // connect to the IEXTrading API and retrieve information
            //httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();

            // read the Json objects in the API response
            if (response.IsSuccessStatusCode)
            {
                gList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            if (!gList.Equals(""))
            {
                List<GainersList> gL = JsonConvert.DeserializeObject<List<GainersList>>(gList, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                gainersList = gL;
            }
            response.Content.Dispose();
           
            return gainersList;
            
        }
        //public IActionResult Sector(string sector)
        //{
        //    sectorData sectorList = new sectorData();
            
        //    sectorList.SectorL = getSector();
        //    if (sector != null)
        //    {
        //        sectorList.Gain = getSectorStock(sector);
        //    }
        //    else
        //    {
        //    sectorList.Gain = getSectorStock("Health%20Care");
        //    }
        //    TempData["Sector"] = JsonConvert.SerializeObject(sectorList.Gain.Take(10));
        //    sectorList.Gain = sectorList.Gain.Take(10).ToList();
        //    return View("Sector_Stock_Data", sectorList);
            
            
        //}
        public IActionResult SectorList()
        {
            ViewBag.dbSucessComp = 0;
            List<Sector> sectorList = new List<Sector>();
            sectorList = getSector();
            TempData["Sector"] = JsonConvert.SerializeObject(sectorList);
            return View("Sector_list", sectorList);
        }
        public IActionResult PopulateSector()
        {
            List<Sector> sector = JsonConvert.DeserializeObject<List<Sector>>(TempData["Sector"].ToString());
            TempData.Keep("Sector");
            foreach (Sector item in sector)
            {

                //Database will give PK constraint violation error when trying to insert record with existing PK.
                //So add company only if it doesnt exist, check existence using symbol (PK)
                if (dbContext.Sector.Where(c => c.ID.Equals(item.ID)).Count() == 0)
                {
                    dbContext.Sector.Add(item);
                }
            }
            dbContext.SaveChanges();
            ViewBag.dbSuccessComp = 1;
            return View("Sector_list", sector);
        }
        public IActionResult PopulateStock()
        {
           
                List<outputModel> output = JsonConvert.DeserializeObject<List<outputModel>>(TempData["BuyOrSell"].ToString());
                TempData.Keep("BuyOrSell");

            foreach (outputModel item in output)
                {

                    //Database will give PK constraint violation error when trying to insert record with existing PK.
                    //So add company only if it doesnt exist, check existence using symbol (PK)
                    if (dbContext.stockSuggest.Where(c => c.companyName.Equals(item.companyName)).Count() == 0)
                    {
                        dbContext.stockSuggest.Add(item);
                    }
                }
                dbContext.SaveChanges();
                ViewBag.dbSuccessComp = 1;
                return View("BuyOrSell", output);
            
           
        }
        public IActionResult PopulateFinancial()
        {
            financialData finance = JsonConvert.DeserializeObject<financialData>(TempData["Financial"].ToString());
            TempData.Keep("Financial");
            foreach (Company item in finance.company)
            {

                //Database will give PK constraint violation error when trying to insert record with existing PK.
                //So add company only if it doesnt exist, check existence using symbol (PK)
                if (dbContext.FinancialList.Where(c => c.symbol.Equals(item.symbol)).Count() == 0)
                {
                    dbContext.FinancialList.Add(finance.finance);
                }
            }
            dbContext.SaveChanges();
            ViewBag.dbSuccessComp = 1;
            return View("companyFinancials", finance);
        }
        public IActionResult PopulateGainer()
        {
            List<GainersList> gainer = JsonConvert.DeserializeObject<List<GainersList>>(TempData["Gainer"].ToString());
            TempData.Keep("Gainer");
            foreach (GainersList item in gainer)
            {

                //Database will give PK constraint violation error when trying to insert record with existing PK.
                //So add company only if it doesnt exist, check existence using symbol (PK)
                if (dbContext.GainersList.Where(c => c.ID.Equals(item.ID)).Count() == 0)
                {
                    dbContext.GainersList.Add(item);
                }
            }
            dbContext.SaveChanges();
            ViewBag.dbSuccessComp = 1;
            return View("Top_Gainers", gainer);
        }
        public List<Sector> getSector()
        {
            string IEXTrading_API_PATH = BASE_URL + "stock/market/sector-performance";
            string sList = "";
            List<Sector> sectorList = null;

            // connect to the IEXTrading API and retrieve information
            //httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();

            // read the Json objects in the API response
            if (response.IsSuccessStatusCode)
            {
                sList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            if (!sList.Equals(""))
            {
                sectorList = JsonConvert.DeserializeObject<List<Sector>>(sList, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                
            }
            response.Content.Dispose();
            return sectorList;
        }
        //public List<GainersList> getSectorStock(string sector)
        //{
        //    string IEXTrading_API_PATH = BASE_URL + "stock/market/collection/sector?collectionName="+sector;
        //    string gList = "";
        //    List<GainersList> sectorStock = null;

        //    // connect to the IEXTrading API and retrieve information
        //    //httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
        //    HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();

        //    // read the Json objects in the API response
        //    if (response.IsSuccessStatusCode)
        //    {
        //        gList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        //    }
        //    if (!gList.Equals(""))
        //    {
        //        sectorStock = JsonConvert.DeserializeObject<List<GainersList>>(gList, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                
        //    }
        //    response.Content.Dispose();
        //    return sectorStock;
        //}
        public IActionResult financials(string symbol)
        {
            ViewBag.dbSucessComp = 0;
            financialData vfinancial = new financialData();
            vfinancial.company = dbContext.Companies.ToList();
            try
            {
                if (symbol != null)
                {
                    vfinancial.finance = getFinancials(symbol);
                }
                else
                {
                    vfinancial.finance = getFinancials("aapl");
                }
                 
                
                    TempData["Financial"] = JsonConvert.SerializeObject(vfinancial);
                    return View("companyFinancials", vfinancial);
                
               
               
                
            }
            catch(Exception)
            {
                return View("companyFinancials", vfinancial);
            }
        }
        public IActionResult AboutUS()
        {
            return View("AboutUs");
        }
        public FinancialList getFinancials(string symbols)
        {
            string IEXTrading_API_PATH = BASE_URL + "stock/"+ symbols+ "/financials";
            string fList = "";
            FinancialList finance = null;

            // connect to the IEXTrading API and retrieve information
            
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();

            // read the Json objects in the API response
            if (response.IsSuccessStatusCode)
            {
                fList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            if (!fList.Equals(""))
            {
                finance = JsonConvert.DeserializeObject<FinancialList>(fList, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            }
            response.Content.Dispose();
            return finance;
        }
        /// <summary>
        /// Save the available symbols in the database
        /// </summary>
        /// <returns></returns>
        public IActionResult PopulateSymbols()
        {
            // retrieve the companies that were saved in the symbols method
            // saving in TempData is extremely inefficient - the data circles back from the browser
            // better methods would be to serialize to the hard disk, or save directly into the database
            //  in the symbols method. This example has been structured to demonstrate one way to save object data
            //  and retrieve it later
            List<Company> companies = JsonConvert.DeserializeObject<List<Company>>(TempData["Companies"].ToString());
            TempData.Keep("Companies");

            foreach (Company company in companies)
            {
                //Database will give PK constraint violation error when trying to insert record with existing PK.
                //So add company only if it doesnt exist, check existence using symbol (PK)
                if (dbContext.Companies.Where(c => c.symbol.Equals(company.symbol)).Count() == 0)
                {
                    dbContext.Companies.Add(company);
                }
            }
        

            dbContext.SaveChanges();
            ViewBag.dbSuccessComp = 1;
            return View("Symbols", companies);
        }

        /// <summary>
        /// Delete all records from tables
        /// </summary>
        /// <param name="tableToDel">Table to clear</param>
        public void ClearTables(string tableToDel)
        {
            if ("all".Equals(tableToDel))
            {
                //First remove equities and then the companies
                dbContext.Equities.RemoveRange(dbContext.Equities);
                dbContext.Companies.RemoveRange(dbContext.Companies);
                dbContext.Financial.RemoveRange(dbContext.Financial);
                dbContext.FinancialList.RemoveRange(dbContext.FinancialList);
                dbContext.GainersList.RemoveRange(dbContext.GainersList);
                dbContext.Sector.RemoveRange(dbContext.Sector);
                dbContext.stockSuggest.RemoveRange(dbContext.stockSuggest);
            }
            else if ("Companies".Equals(tableToDel))
            {
                //Remove only those companies that don't have related quotes stored in the Equities table
                dbContext.Companies.RemoveRange(dbContext.Companies
                                                         .Where(c => c.Equities.Count == 0)
                                                                      );
            }
            else if ("Charts".Equals(tableToDel))
            {
                dbContext.Equities.RemoveRange(dbContext.Equities);
            }
            else if ("Top Gainers".Equals(tableToDel))
            {
                dbContext.GainersList.RemoveRange(dbContext.GainersList);
            }
            else if ("Sector".Equals(tableToDel))
            {
                dbContext.Sector.RemoveRange(dbContext.Sector);
            }
            else if ("Financial".Equals(tableToDel))
            {
                dbContext.Financial.RemoveRange(dbContext.Financial);
                dbContext.FinancialList.RemoveRange(dbContext.FinancialList);
            }
            else if ("Buy/Sell".Equals(tableToDel))
            {
                dbContext.stockSuggest.RemoveRange(dbContext.stockSuggest);
            }
            dbContext.SaveChanges();
        }
    }
}

