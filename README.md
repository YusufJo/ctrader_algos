# Trading Algos for cTrader


## Included Algorithms:

  - BufferBreakout.
  
       Uses three moving averages to determine range breakouts.
       Opens positions in the direction of the breakout given number of
       profit pips, loss pips, and buffer size in pips. 

  - Engulfing.
  
      Uses four moving averages, and the average true range indicator to determine the market 
      Then scans for any formed Engulfing pattern.
      Once the pattern is formed, a position is opened given a risk/reward percentage.
      The algo allows for trading at specific market hours.

  - MAs.
    
    Uses two moving averages to determine the market sentiment.
    Opens positions depending on sentiment signals, given a lot size.

  - ParabolicTrader.
    
    Uses the Parabolic SAR indicator to determine the market sentiment.
    Opens positions depending on sentiment signals, given a lot size.
    Determines the position size given the risk percentage of the capital, 
    take profit, and stop loss in pips.

  - SimpleScalping

    A trading algorithm for the CTrader platform.
    Simple scalping algo that depends on two moving averages to 
    open positions.

## Requirements
  * [cTrader](https://ctrader.com)

## Getting Started
   1. Open cTrader.
   2. Navigate to Automate on the left pane.
   3. Create a new cBot.
   4. Replace the default cBot code.
   5. Click Build cBot.
   6. Add the cBot to any Symbol and start it.