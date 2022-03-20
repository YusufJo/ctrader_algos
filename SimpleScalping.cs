// Copyright (c) Abdulhamid Yusuf. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

// Description: A trading algorithm for the CTrader platform.
//              Simple scalping algo that depends on two moving averages to 
//              open positions.

using System;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SimpleScalping : Robot
    {
        [Parameter("Data Source")]
        public DataSeries dataSeries { get; set; }

        [Parameter("Take profit", DefaultValue = 10)]
        public int takeProfit { get; set; }

        [Parameter("Stop loss", DefaultValue = 10)]
        public int stopLoss { get; set; }

        [Parameter("Volume", DefaultValue = 10)]
        public int volume { get; set; }


        private SimpleMovingAverage sma1;
        private SimpleMovingAverage sma2;

        private double begEquity;

        protected override void OnStart()
        {
            sma1 = Indicators.SimpleMovingAverage(dataSeries, 14);
            sma2 = Indicators.SimpleMovingAverage(dataSeries, 5);
            begEquity = Account.Equity;
        }

        protected override void OnBar()
        {
            if (sma2.Result.HasCrossedAbove(sma1.Result, 0))
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, Symbol.NormalizeVolumeInUnits(volume), "Buy", stopLoss, takeProfit);
            }

            if (sma2.Result.HasCrossedBelow(sma1.Result, 0))
            {
                ExecuteMarketOrder(TradeType.Sell, SymbolName, Symbol.NormalizeVolumeInUnits(volume), "Sell", stopLoss, takeProfit);
            }
        }

        
    }
}

