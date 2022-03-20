// Copyright (c) Abdulhamid Yusuf. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

// Description: A trading algorithm for the CTrader platform.
//              Uses four moving averages, and the average true range indicator to determine the market sentiment.
//              Then scans for any formed Engulfing pattern.
//              Once the pattern is formed, a position is opened given a risk/reward percentage.
//              The algo allows for trading at specific market hours.

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]

    public class Engulfing : Robot
    {
        private enum OrderType
        {
            BUY,
            SELL
        }
        private MovingAverage _ema6;
        private MovingAverage _ema18;
        private MovingAverage _ema50;
        private MovingAverage _sma200;
        private AverageTrueRange _atr;

        [Parameter("Engulfing baby to mother ratio", DefaultValue = 50)]
        public double _babyToMotherRatio { get; set; }

        [Parameter("Max Allowed Spread", DefaultValue = 2.9, Step = 0.01)]
        public double MaxSpread { get; set; }

        [Parameter("Risk per trade in %", DefaultValue = 2.0)]
        public double RiskPerTrade { get; set; }

        [Parameter("Take Profit to Stop Loss %", DefaultValue = 2.0)]
        public double ProfitToLoss { get; set; }

        [Parameter("Start trading after market open in minutes", DefaultValue = 60)]
        public int AfterOpenInMinutes { get; set; }

        [Parameter("Stop Trading Before Market close in minutes", DefaultValue = 120)]
        public int BeforeCloseInMinutes { get; set; }


        protected override void OnStart()
        {
            // Put your initialization logic here
            _ema6 = Indicators.ExponentialMovingAverage(this.Bars.ClosePrices, 6);
            _ema18 = Indicators.ExponentialMovingAverage(this.Bars.ClosePrices, 18);
            _ema50 = Indicators.ExponentialMovingAverage(this.Bars.ClosePrices, 50);
            _sma200 = Indicators.SimpleMovingAverage(this.Bars.ClosePrices, 200);
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);


        }

        protected override void OnBar()
        {
            CloseStopOrdersOnOppositeDirection();
            if (IsSpreadWide()) CloseAllPositionsAndOrders();
            if (PendingOrders.Count > 2) return;
            if (IsEarlyToTrade()) return;
            if (IsLateToTrade()) return;
            if (IsMovingAveragesBullishSignal() && IsBullishEngulfing()) PlaceBuyStopOrder();
            else if (IsMovingAveragesBearishSignal() && IsBearishEngulfing()) PlaceSellStopOrder();

        }

        protected override void OnTick()
        {
            // Put your core logic here
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        private bool IsMovingAveragesBullishSignal()
        {
            if (_ema6.Result.IsFalling()) return false;
            if (_ema18.Result.IsFalling()) return false;
            if (_ema50.Result.IsFalling()) return false;
            if (_sma200.Result.IsFalling()) return false;
            if (_ema50.Result.LastValue <= _sma200.Result.LastValue) return false;
            if (_ema18.Result.LastValue <= _ema50.Result.LastValue) return false;
            if (_ema6.Result.LastValue <= _ema18.Result.LastValue) return false;
            return true;

        }

        private bool IsMovingAveragesBearishSignal()
        {
            if (_ema6.Result.IsRising()) return false;
            if (_ema18.Result.IsRising()) return false;
            if (_ema50.Result.IsRising()) return false;
            if (_sma200.Result.IsRising()) return false;
            if (_ema50.Result.LastValue >= _sma200.Result.LastValue) return false;
            if (_ema18.Result.LastValue >= _ema50.Result.LastValue) return false;
            if (_ema6.Result.LastValue >= _ema18.Result.LastValue) return false;
            return true;

        }

        private bool IsBullishEngulfing()
        {
            var bar1 = this.Bars.Last(2);
            var bar2 = this.Bars.Last(1);
            if (bar1.Open <= bar1.Close) return false;
            if (bar2.Close <= bar2.Open) return false;
            if (bar1.Open >= bar2.Close) return false;
            if (bar1.Close < bar2.Open) return false;

            var bar1SizeInPips = Math.Abs(bar1.Open - bar1.Close) / Symbol.PipSize;
            var bar2SizeInPips = Math.Abs(bar2.Open - bar2.Close) / Symbol.PipSize;
            if (((bar1SizeInPips / bar2SizeInPips) * 100) < _babyToMotherRatio) return false;
            return true;
        }

        private bool IsBearishEngulfing()
        {
            var bar1 = this.Bars.Last(2);
            var bar2 = this.Bars.Last(1);

            if (bar1.Close <= bar1.Open) return false;
            if (bar2.Open <= bar2.Close) return false;

            if (bar1.Close > bar2.Open) return false;
            if (bar1.Open <= bar2.Close) return false;

            var bar1SizeInPips = Math.Abs(bar1.High - bar1.Low) / Symbol.PipSize;
            var bar2SizeInPips = Math.Abs(bar2.High - bar2.Low) / Symbol.PipSize;
            if (((bar1SizeInPips / bar2SizeInPips) * 100) < _babyToMotherRatio) return false;
            return true;
        }

        private bool IsSpreadWide()
        {
            var spreadInPips = this.Symbol.Spread / this.Symbol.PipSize;
            if (spreadInPips - MaxSpread >= this.Symbol.PipSize)
            {
                Print(String.Format("Spread is Wide [{0:00.00}].", spreadInPips));
                return true;
            }
            return false;
        }

        private bool IsEarlyToTrade()
        {
            if (Symbol.MarketHours.TimeTillClose().TotalMinutes >= TimeSpan.FromHours(24).Subtract(TimeSpan.FromMinutes(AfterOpenInMinutes)).TotalMinutes)
            {
                Print("Trade time not sutable, Market just opened");
                return true;
            }
            return false;
        }

        private bool IsLateToTrade()
        {
            if (Symbol.MarketHours.TimeTillClose().TotalMinutes <= TimeSpan.FromMinutes(BeforeCloseInMinutes).TotalMinutes)
            {
                Print("Trade time not sutable, Market about to close");
                return true;
            }
            return false;
        }

        private void PlaceBuyStopOrder()
        {
            CloseAllSellStopOrder();
            var atrInPips = _atr.Result.LastValue / Symbol.PipSize;
            var stopLossInPips = atrInPips * 1.5;
            var takeProfitInPips = stopLossInPips * ProfitToLoss;
            var volume = (Account.Balance * (RiskPerTrade / 100)) / (stopLossInPips * Symbol.PipValue);
            var targetPrice = Symbol.Spread / Symbol.PipSize < 2 ? Symbol.Bid + (Symbol.PipSize * 2) : Symbol.Ask;
            PlaceStopOrder(TradeType.Buy, SymbolName, Symbol.NormalizeVolumeInUnits(volume), targetPrice, OrderType.BUY.ToString(), stopLossInPips, takeProfitInPips);
        }

        private void PlaceSellStopOrder()
        {
            CloseAllBuyStopOrder();
            var atrInPips = _atr.Result.LastValue / Symbol.PipSize;
            var stopLossInPips = atrInPips * 1.5;
            var takeProfitInPips = stopLossInPips * ProfitToLoss;
            var volume = (Account.Balance * (RiskPerTrade / 100)) / (stopLossInPips * Symbol.PipValue);
            var targetPrice = Symbol.Bid - (Symbol.PipSize * 2);
            PlaceStopOrder(TradeType.Sell, SymbolName, Symbol.NormalizeVolumeInUnits(volume), targetPrice, OrderType.SELL.ToString(), stopLossInPips, takeProfitInPips);
        }

        private void CloseStopOrdersOnOppositeDirection()
        {
            foreach (PendingOrder po in PendingOrders)
            {
                if (Math.Abs((po.TargetPrice - Symbol.Ask)) / Symbol.PipSize >= 10)
                    CancelPendingOrderAsync(po);
            }
        }

        private void CloseAllBuyStopOrder()
        {
            foreach (PendingOrder po in PendingOrders)
            {
                if (po.Label.Equals(OrderType.BUY))
                    CancelPendingOrderAsync(po);
            }
        }
        private void CloseAllSellStopOrder()
        {
            foreach (PendingOrder po in PendingOrders)
            {
                if (po.Label.Equals(OrderType.SELL))
                    CancelPendingOrderAsync(po);
            }
        }
        private void CloseAllPositionsAndOrders()
        {
            foreach (Position p in Positions)
                ClosePositionAsync(p);
            CloseAllBuyStopOrder();
            CloseAllSellStopOrder();
        }
    }
}

