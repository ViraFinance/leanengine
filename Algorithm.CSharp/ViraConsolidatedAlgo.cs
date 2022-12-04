#region imports
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using QuantConnect.Orders;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Util;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public class ViraConsolidatedAlgo : QCAlgorithm
    {
        private Symbol _minuteNifty50;
        private Symbol _fiveMinuteNifty50;
        private Symbol _dayNifty50;
        // private BaseDataConsolidator _fiveMinConsolidator;
        private RollingWindow<DailyNifty50> _dailyBarsWindow;
        private RollingWindow<FiveMinuteNifty50> _bullRollingWindow;
        private RollingWindow<FiveMinuteNifty50> _bearRollingWindow;
        private decimal _last_fill_price;

        public override void Initialize()
        {
            SetTimeZone(TimeZones.Kolkata);
            SetStartDate(2021, 1, 1);
            SetEndDate(2022, 10, 21);
            SetAccountCurrency("INR");
            SetCash(100000);

            // SetBenchmark(_fiveMinuteNifty50);

            _last_fill_price = 0;

            _minuteNifty50 = AddData<MinuteNifty50>("MIN_NIFTY50", null, TimeZones.Kolkata).Symbol;
            _fiveMinuteNifty50 = AddData<FiveMinuteNifty50>("5MIN_NIFTY50", null, TimeZones.Kolkata).Symbol;
            _dayNifty50 = AddData<DailyNifty50>("DAY_NIFTY50", Resolution.Daily, TimeZones.Kolkata).Symbol;


            _bullRollingWindow = new RollingWindow<FiveMinuteNifty50>(4);
            _bearRollingWindow = new RollingWindow<FiveMinuteNifty50>(4);

            _dailyBarsWindow = new RollingWindow<DailyNifty50>(2);

            // Schedule.On(DateRules.EveryDay(_minuteNifty50), TimeRules.BeforeMarketClose(_minuteNifty50, 5), ExitPositions);
        }

        public void OnData(DailyNifty50 data)
        {
            _dailyBarsWindow.Add(data);
            _bullRollingWindow.Reset();
            _bearRollingWindow.Reset();
        }

        public void OnData(FiveMinuteNifty50 data)
        {
            _bullRollingWindow.Add(data);
            _bearRollingWindow.Add(data);

            // if (!(Time.Hour == 9 && Time.Minute == 15)) return;
            if (Portfolio.Invested) return;
            if (!_dailyBarsWindow.IsReady) { return; }

            if (!_bullRollingWindow.IsReady || !_bearRollingWindow.IsReady) return;
            for (int i = 0; i < _bullRollingWindow.Size; i++)
            {
                if (_bullRollingWindow[i].Open >= _bullRollingWindow[i].Close) return;
            }
            // for (int i = 0; i < _bearRollingWindow.Size; i++)
            // {
            //     if (_bearRollingWindow[i].Close >= _bearRollingWindow[i].Open) return;
            // }

            if ((_bullRollingWindow[0].Open < _dailyBarsWindow[0].TopPivot) && (_bullRollingWindow[0].Close > _dailyBarsWindow[0].TopPivot))
            {
                Log($"Going Long on {data.EndTime}");
                SetHoldings(_minuteNifty50, 1);
                _bullRollingWindow.Reset();
            }
            // if ((_bearRollingWindow[0].Open < _dailyBarsWindow[0].TopPivot) && (_bearRollingWindow[0].Close > _dailyBarsWindow[0].TopPivot))
            // {
            //     SetHoldings(_minuteNifty50, -1);
            //     _bearRollingWindow.Reset();
            // }
        }

        public void OnData(MinuteNifty50 data)
        {
            // If invested check for exit strategy else entry strategy
            if (!Portfolio.Invested) return;
            if (
                data.Price < (_dailyBarsWindow[0].BottomPivot - 5) ||
                data.Price >= (_last_fill_price + 60)
            )
            {
                Liquidate();
                _bullRollingWindow.Reset();
                _bearRollingWindow.Reset();
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                _last_fill_price = orderEvent.FillPrice;
                Log($"bar at {orderEvent.FillPrice} | Quantity:{orderEvent.FillQuantity} | Time: {orderEvent.UtcTime.ToLocalTime()} | BOTTOM PIVOT: {_dailyBarsWindow[0].BottomPivot}");
            }
        }

        public void ExitPositions()
        {
            Liquidate();
        }
        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 154311;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "155.211%"},
            {"Drawdown", "84.800%"},
            {"Expectancy", "0"},
            {"Net Profit", "5123.242%"},
            {"Sharpe Ratio", "2.067"},
            {"Probabilistic Sharpe Ratio", "68.833%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.732"},
            {"Beta", "0.043"},
            {"Annual Standard Deviation", "0.841"},
            {"Annual Variance", "0.707"},
            {"Information Ratio", "1.902"},
            {"Tracking Error", "0.848"},
            {"Treynor Ratio", "40.467"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "2.236"},
            {"Return Over Maximum Drawdown", "1.83"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "0d80bb47bd16b5bc6989a4c1c7aa8349"}
        };


    }
    /// <summary>
    /// NIFTY50 Minute Resolution Data Class
    /// </summary>
    public class MinuteNifty50 : BaseData
    {
        /// <summary>
        /// Closing Price
        /// </summary>
        public decimal Close;
        /// <summary>
        /// High Price
        /// </summary>
        public decimal High;
        /// <summary>
        /// Low Price
        /// </summary>
        public decimal Low;
        /// <summary>
        /// Opening Price
        /// </summary>
        public decimal Open;

        /// <summary>
        /// Default initializer for NIFTY50.
        /// </summary>
        public MinuteNifty50()
        {
            Symbol = "MIN_NIFTY50";
        }
        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(Path.Combine(Globals.DataFolder, "index", "india", "minute", "nifty50", $"NIFTY_50_2015_2022.csv"), SubscriptionTransportMedium.LocalFile);
        }
        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            //New MinuteNifty50 object
            var index = new MinuteNifty50();
            try
            {
                //Example File Format:
                //Date,                         Close       High        Low       Open     Volume    
                //2011-09-13 09:15:00+05:30     7792.9      7799.9      7722.65   7748.7   116534670  
                var data = line.Split(',');
                index.Time = DateTime.Parse(data[0], CultureInfo.InvariantCulture);
                index.EndTime = index.Time.AddMinutes(1);
                index.Close = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                index.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                index.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                index.Open = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                index.Symbol = config.Symbol;
                index.Value = index.Close;
            }
            catch
            {
                return null;
            }
            return index;
        }
    }
    /// <summary>
    /// NIFTY50 Minute Resolution Data Class
    /// </summary>

    public class FiveMinuteNifty50 : BaseData
    {
        /// <summary>
        /// Closing Price
        /// </summary>
        public decimal Close;
        /// <summary>
        /// High Price
        /// </summary>
        public decimal High;
        /// <summary>
        /// Low Price
        /// </summary>
        public decimal Low;
        /// <summary>
        /// Opening Price
        /// </summary>
        public decimal Open;

        /// <summary>
        /// Default initializer for NIFTY50.
        /// </summary>
        public FiveMinuteNifty50()
        {
            Symbol = "5MIN_NIFTY50";
        }
        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(Path.Combine(Globals.DataFolder, "index", "india", "minute", "nifty50", $"NIFTY50_5mins.csv"), SubscriptionTransportMedium.LocalFile);
        }
        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            //New MinuteNifty50 object
            var index = new FiveMinuteNifty50();
            try
            {
                //Example File Format:
                //Date,                         Close       High        Low       Open     Volume    
                //2011-09-13 09:15:00+05:30     7792.9      7799.9      7722.65   7748.7   116534670  
                var data = line.Split(',');
                index.Time = DateTime.Parse(data[0], CultureInfo.InvariantCulture);
                index.EndTime = index.Time.AddMinutes(5);
                index.Close = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                index.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                index.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                index.Open = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                index.Symbol = config.Symbol;
                index.Value = index.Close;
            }
            catch
            {
                return null;
            }
            return index;
        }
    }

    /// <summary>
    /// NIFTY50 Day Resolution Data Class
    /// </summary>

    public class DailyNifty50 : BaseData
    {
        /// <summary>
        /// Closing Price
        /// </summary>
        public decimal Close;
        /// <summary>
        /// High Price
        /// </summary>
        public decimal High;
        /// <summary>
        /// Low Price
        /// </summary>
        public decimal Low;
        /// <summary>
        /// Opening Price
        /// </summary>
        public decimal Open;

        /// <summary>
        /// Pivot Point
        /// </summary>
        public decimal PivotPoint;
        /// <summary>
        /// Bottom Pivot
        /// </summary>
        public decimal BottomPivot;
        /// <summary>
        /// Top Pivot
        /// </summary>
        public decimal TopPivot;

        /// <summary>
        /// Default initializer for NIFTY50.
        /// </summary>
        public DailyNifty50()
        {
            Symbol = "DAY_NIFTY50";
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(Path.Combine(Globals.DataFolder, "index", "india", "day", "nifty50", $"NIFTY50_2015_2022.csv"), SubscriptionTransportMedium.LocalFile);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            //New DailyNifty50 object
            var index = new DailyNifty50();

            try
            {
                //Example File Format:
                //Date,                         Close       High        Low       Open     Volume    
                //2011-09-13 09:15:00+05:30     7792.9      7799.9      7722.65   7748.7   116534670  
                var data = line.Split(',');
                index.Time = DateTime.Parse(data[0], CultureInfo.InvariantCulture);
                index.EndTime = index.Time.AddDays(1);
                index.Close = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                index.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                index.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                index.Open = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                index.PivotPoint = Convert.ToDecimal(((index.High + index.Low + index.Close) / 3), CultureInfo.InvariantCulture);
                index.BottomPivot = Convert.ToDecimal(((index.High + index.Low) / 2), CultureInfo.InvariantCulture);
                index.TopPivot = Convert.ToDecimal(((index.PivotPoint - index.BottomPivot) + index.PivotPoint), CultureInfo.InvariantCulture);
                if (index.TopPivot < index.BottomPivot) (index.BottomPivot, index.TopPivot) = (index.TopPivot, index.BottomPivot);

                index.Symbol = config.Symbol;
                index.Value = index.Close;
            }
            catch
            {
                return null;
            }
            return index;
        }
    }
}