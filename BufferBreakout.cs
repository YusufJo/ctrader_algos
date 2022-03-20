// Copyright (c) Abdulhamid Yusuf. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

// Description: A trading algorithm for the CTrader platform.
//              Uses three moving averages to determine range breakouts.
//              Opens positions in the direction of the breakout given number of
//              profit pips, loss pips, and buffer size in pips.

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
    public class BufferBreakout : Robot
    {
        [Parameter("Data Source")] public DataSeries dataSeries { get; set; }

        [Parameter("Take profit", DefaultValue = 10)]
        public int takeProfit { get; set; }

        [Parameter("Stop loss", DefaultValue = 10)]
        public int stopLoss { get; set; }

        [Parameter("Volume", DefaultValue = 10)]
        public int volume { get; set; }

        [Parameter("Buffer in pips", DefaultValue = 200)]
        public int bufferPips { get; set; }


        private SimpleMovingAverage sma1;
        private SimpleMovingAverage sma2;
        private SimpleMovingAverage sma3;


        protected override void OnStart()
        {
            sma1 = Indicators.SimpleMovingAverage(dataSeries, 14);
            sma2 = Indicators.SimpleMovingAverage(dataSeries, 5);
            sma3 = Indicators.SimpleMovingAverage(dataSeries, 200);
        }

        protected override void OnBar()
        {
            var bufferUpperLimit = sma3.Result.Last() + (Symbol.PipSize * bufferPips);
            var bufferLowerLimit = sma3.Result.Last() - (Symbol.PipSize * bufferPips);

            Print("SMA200: " + sma3.Result.Last());
            Print("Buffer upper: " + bufferUpperLimit);
            Print("Buffer lower: " + bufferLowerLimit);

            if (sma2.Result.HasCrossedAbove(sma1.Result, 0) && Symbol.Bid > bufferUpperLimit)
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, Symbol.NormalizeVolumeInUnits(volume), "Buy", stopLoss,
                    takeProfit);
            }

            if (sma2.Result.HasCrossedBelow(sma1.Result, 0) && Symbol.Ask < bufferLowerLimit)
            {
                ExecuteMarketOrder(TradeType.Sell, SymbolName, Symbol.NormalizeVolumeInUnits(volume), "Sell", stopLoss,
                    takeProfit);
            }
        }


        protected override void OnTick()
        {
            if (Symbol.Bid > sma3.Result.Last())
            {
                foreach (var p in Positions)
                {
                    if (p.TradeType == TradeType.Sell)
                        ClosePositionAsync(p);
                }
            }
            else if (Symbol.Ask < sma3.Result.Last())
            {
                foreach (var p in Positions)
                {
                    if (p.TradeType == TradeType.Buy)
                        ClosePositionAsync(p);
                }
            }
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}