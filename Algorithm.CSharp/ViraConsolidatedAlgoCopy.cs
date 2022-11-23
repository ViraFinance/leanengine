#region imports
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    public class ViraConsolidatedAlgoCopy : QCAlgorithm
    {
        private Symbol _symbol;
        private decimal _centerPivot;
        private decimal _topPivot;
        private decimal _bottomPivot;
        private RollingWindow<TradeBar> _bullRollingWindow;
        private RollingWindow<TradeBar> _bearRollingWindow;

        public override void Initialize()
        {
            SetTimeZone(TimeZones.Kolkata);

            SetStartDate(2021, 1, 1);
            SetEndDate(2022, 10, 21);
            SetAccountCurrency("INR");
            SetCash(100000);

            _symbol = AddData<MinutelyNifty50>("MIN_NIFTY50", Resolution.Minute).Symbol;

            _centerPivot = 0;
            _topPivot = 0;
            _bottomPivot = 0;

            var fiveMinConsolidator = new BaseDataConsolidator(TimeSpan.FromMinutes(5));
            fiveMinConsolidator.DataConsolidated += FiveMinDataHandler;
            var oneDayConsolidator = BaseDataConsolidator.FromResolution(Resolution.Daily);
            oneDayConsolidator.DataConsolidated += OneDayDataHandler;

            _bullRollingWindow = new RollingWindow<TradeBar>(4);
            _bearRollingWindow = new RollingWindow<TradeBar>(4);

            SubscriptionManager.AddConsolidator(_symbol, fiveMinConsolidator);
            SubscriptionManager.AddConsolidator(_symbol, oneDayConsolidator);
        }


        private void FiveMinDataHandler(object sender, TradeBar baseData)
        {
            _bullRollingWindow.Add(baseData);
            _bearRollingWindow.Add(baseData);

            if (!_bullRollingWindow.IsReady || !_bearRollingWindow.IsReady) return;
            // if (!(Time.Hour == 9 && Time.Minute == 15)) return;
            // if (Portfolio.Invested) return;
            for (int i = 0; i < _bullRollingWindow.Size; i++)
            {
                if (_bullRollingWindow[i].Open >= _bullRollingWindow[i].Close) return;
            }

            if ((_bullRollingWindow[0].Open < _topPivot) && (_bullRollingWindow[0].Close > _topPivot))
            {
                // Log($"Buy the next bar at {baseData.EndTime}: TOP PIVOT: {_topPivot} BOTTOM PIVOT: {_bottomPivot}");
                _bullRollingWindow.Reset();
            }
        }

        private void OneDayDataHandler(object sender, TradeBar baseData)
        {
            _centerPivot = Convert.ToDecimal(((baseData.High + baseData.Low + baseData.Close) / 3), CultureInfo.InvariantCulture);
            _bottomPivot = Convert.ToDecimal(((baseData.High + baseData.Low) / 2), CultureInfo.InvariantCulture);
            _topPivot = Convert.ToDecimal(((_centerPivot - _bottomPivot) + _centerPivot), CultureInfo.InvariantCulture);
            if (_topPivot < _bottomPivot) (_bottomPivot, _topPivot) = (_topPivot, _bottomPivot);
            Log($"bar at {baseData.EndTime}: LOW: {baseData.Low} | PIVOT: {_centerPivot} | TOP PIVOT: {_topPivot} | BOTTOM PIVOT: {_bottomPivot}");
        }

        // public void OnData(MinutelyNifty50 data)
        // {
        //     _bullRollingWindow.Add(data);
        //     _bearRollingWindow.Add(data);
        //     if (!_bullRollingWindow.IsReady || !_bearRollingWindow.IsReady) return;
        //     // if (!(Time.Hour == 9 && Time.Minute == 15)) return;
        //     // if (Portfolio.Invested) return;
        //     for (int i = 0; i < _bullRollingWindow.Size; i++)
        //     {
        //         if (_bullRollingWindow[i].Open >= _bullRollingWindow[i].Close) return;
        //     }

        //     if ((_bullRollingWindow[0].Open < data.TopPivot) && (_bullRollingWindow[0].Close > data.TopPivot))
        //     {
        //         Log($"Buy the next bar at {data.EndTime}: Close: {data.Close} TOP PIVOT: {data.TopPivot} BOTTOM PIVOT: {data.BottomPivot}");
        //         _bullRollingWindow.Reset();
        //     }
        // }

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


}