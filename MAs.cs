// Copyright (c) Abdulhamid Yusuf. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

// Description: A trading algorithm for the CTrader platform.
//              Uses two moving averages to determine the market sentiment.
//              Opens positions depending on sentiment signals, given a lot size.

using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Linq;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MAs : Robot
    {

        [Parameter("Data Series")]
        public DataSeries source { get; set; }

        [Parameter("MA1 Period", DefaultValue = 14)]
        public int ma1Period { get; set; }

        [Parameter("MA1 type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType ma1Type { get; set; }

        [Parameter("MA2 Period", DefaultValue = 5)]
        public int ma2Period { get; set; }

        [Parameter("MA2 type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType ma2 { get; set; }

        [Parameter("TimeFrame", DefaultValue = "Minute1")]
        public TimeFrame timeFrame { get; set; }

        [Parameter("Take Profit Pips", DefaultValue = 5)]
        public int tp { get; set; }

        [Parameter("Stop Loss Pips", DefaultValue = 5)]
        public int sl { get; set; }

        [Parameter("Lot", DefaultValue = 1000)]
        public int lot { get; set; }

        private MovingAverage _ma1;
        private MovingAverage _ma2;
        enum Signal
        {
            BUY,
            SELL,
            NONE
        }
        protected override void OnStart()
        {
            _ma1 = Indicators.MovingAverage(source, ma1Period, ma1Type);
            _ma2 = Indicators.MovingAverage(source, ma2Period, ma1Type);
        }

        protected override void OnBar()
        {
            var signal = getCurrentSignal();
            if (signal == Signal.BUY)
            {
                Print("Buy Signal");
                foreach (var p in Positions)
                {
                    if (p.TradeType == TradeType.Sell)
                        p.Close();
                }
            }
            else if (signal == Signal.SELL)
            {
                Print("Sell Signal");
                foreach (var p in Positions)
                {
                    if (p.TradeType == TradeType.Buy)
                        p.Close();
                }
            }
        }

        protected override void OnTick()
        {
            var myTakeProfitPips = Symbol.Spread + tp;

            if (Positions.Count == 0)
            {
                if (_ma2.Result.HasCrossedAbove(_ma1.Result, 1))
                {
                    ExecuteMarketOrder(TradeType.Buy, this.Symbol.Name, lot, "Buy", sl, myTakeProfitPips);
                }
                else if (_ma2.Result.HasCrossedBelow(_ma1.Result, 1))
                {
                    ExecuteMarketOrder(TradeType.Sell, this.Symbol.Name, lot, "Sell", sl, myTakeProfitPips);
                }
            }
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        private Signal getCurrentSignal()
        {
            var lastSmall = _ma2.Result.Last(1);
            var lastLarge = _ma1.Result.Last(1);

            if (lastSmall > lastLarge)
                return Signal.BUY;
            else if (lastSmall < lastLarge)
                return Signal.SELL;
            else
                return Signal.NONE;
        }
    }
}
