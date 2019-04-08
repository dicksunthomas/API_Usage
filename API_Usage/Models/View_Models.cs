using System.Collections.Generic;
//using API_Usage.Models;

namespace API_Usage.Models
{
  public class CompaniesEquities
  {
    public List<Company> Companies { get; set; }
    public Equity Current { get; set; }
    public string Dates { get; set; }
    public string Prices { get; set; }
    public string Volumes { get; set; }
    public float AvgPrice { get; set; }
    public double AvgVolume { get; set; }

    public CompaniesEquities(List<Company> companies, Equity current,
                                      string dates, string prices, string volumes,
                                      float avgprice, double avgvolume)
    {
      Companies = companies;
      Current = current;
      Dates = dates;
      Prices = prices;
      Volumes = volumes;
      AvgPrice = avgprice;
      AvgVolume = avgvolume;
    }
  }

    public class DelayedQuote
    {
        public string symbol { get; set; }
        public float delayedPrice { get; set; }
        public float high { get; set; }
        public float low { get; set; }
        public int delayedSize { get; set; }
        public long delayedPriceTime { get; set; }
        public long processedTime { get; set; }
    }

    public class AzureMLModel
  {
    public string Message { get; set; }
    public string JsonObject { get; set; }
  }
    public class Rootobject
    {
        public string symbol { get; set; }
        public Earning[] earnings { get; set; }
    }

    public class Earning
    {
        public float actualEPS { get; set; }
        public float consensusEPS { get; set; }
        public float estimatedEPS { get; set; }
        public string announceTime { get; set; }
        public int numberOfEstimates { get; set; }
        public float EPSSurpriseDollar { get; set; }
        public string EPSReportDate { get; set; }
        public string fiscalPeriod { get; set; }
        public string fiscalEndDate { get; set; }
        public float yearAgo { get; set; }
        public float yearAgoChangePercent { get; set; }
        public float estimatedChangePercent { get; set; }
        public int symbolId { get; set; }
    }
    public class sectorData {
        public List<GainersList> Gain { get; set; }
        public List<Sector> SectorL { get; set; }
    }
    public class financialData
    { 
        public FinancialList finance { get; set; }
        public List<Company> company { get; set; }
    }

}

