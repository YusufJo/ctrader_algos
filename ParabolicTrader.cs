// Copyright (c) Abdulhamid Yusuf. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

// Description: A trading algorithm for the CTrader platform.
//              Uses the Parabolic SAR indicator to determine the market sentiment.
//              Opens positions depending on sentiment signals, given a lot size.
//              Determins the position size given the risk percentage of the capital, 
//              take profit, and stop loss in pips.

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ParabolicTrader : Robot
    {
        [Parameter("Mini AF", DefaultValue = 0.2)]
        public double miniAF { get; set; }

        [Parameter("Max AF", DefaultValue = 0.2)]
        public double maxAF { get; set; }

        [Parameter("Stop Loss", DefaultValue = 100)]
        public double stopLoss { get; set; }

        [Parameter("Risk %", DefaultValue = 10)]
        public double riskPercentage { get; set; }

        [Parameter("Take Profit", DefaultValue = 10)]
        public double tp { get; set; }

        [Parameter("Stop Loss", DefaultValue = 10)]
        public double sl { get; set; }

        private ParabolicSAR parabolicSAR;
        private enum Signal
        {
            BUY,
            SELL,
            NONE
        }

        protected override void OnStart()
        {
            parabolicSAR = Indicators.ParabolicSAR(miniAF, maxAF);
            // Put your initialization logic here
        }

        protected override void OnBar()
        {
            var penultimateResult = parabolicSAR.Result.Last(2);
            var penultimateBar = Bars.Last(2);

            var previousResult = parabolicSAR.Result.Last(1);
            var previousBar = Bars.Last(1);

            if (hasReversed(penultimateResult, penultimateBar, previousResult, previousBar))
            {
                var signal = getSignal(previousResult, previousBar);
                if (signal == Signal.BUY)
                {
                    closeAllPositions();
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, 10, null, sl, tp);
                }
                else if (signal == Signal.SELL)
                {
                    closeAllPositions();
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, 10, null, sl, tp);
                }
            }
        }

        private void closeAllPositions()
        {
            foreach (var p in Positions)
            {
                ClosePositionAsync(p);
            }
        }

        private double getVolumeSize()
        {
            var volume = Account.Balance * (riskPercentage / 100.0) / stopLoss * 1000;
            return Symbol.NormalizeVolumeInUnits(volume);
        }

        private Signal getSignal(double previousResult, Bar previousBar)
        {
            return getSignalForSingleBar(previousBar.High, previousBar.Low, previousResult);
        }

        private bool hasReversed(double penultimateResult, Bar penultimateBar, double previousResult, Bar previousBar)
        {
            var penultimateHigh = penultimateBar.High;
            var penultimateLow = penultimateBar.Low;

            var penultimateSignal = getSignalForSingleBar(penultimateHigh, penultimateLow, penultimateResult);

            var previousHigh = previousBar.High;
            var preiousLow = previousBar.Low;

            var previousSignal = getSignalForSingleBar(previousHigh, preiousLow, previousResult);
            if (penultimateSignal != Signal.NONE && previousSignal != Signal.NONE)
            {
                if (penultimateSignal != previousSignal)
                    return true;
            }

            return false;
        }


        private Signal getSignalForSingleBar(double high, double low, double parapolicResult)
        {
            if (parapolicResult > high)
                return Signal.SELL;
            else if (parapolicResult < low)
                return Signal.BUY;
            else
                return Signal.NONE;
        }
    }
}
