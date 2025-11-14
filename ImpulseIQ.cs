// File: ImpulseIQ.cs
// NinjaTrader 8 indicator port of "Impulse IQ [Trading IQ]" Pine Script
// Complete implementation with dual IQ meters, breakout lines, and all features

using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

#region Enums
public enum StrategyTypeEnum
{
    Breakout,
    Cheap
}

public enum TablePositionEnum
{
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft,
    TopCenter,
    MiddleCenter,
    None
}

public enum LabelSizeEnum
{
    Auto,
    Tiny,
    Small,
    Normal,
    Large,
    Huge
}
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class ImpulseIQ : Indicator
    {
        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "Strategy Type", Order = 0, GroupName = "Learning Settings")]
        public StrategyTypeEnum StrategyType { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Long", Order = 1, GroupName = "Learning Settings")]
        public bool TradeLong { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trade Short", Order = 2, GroupName = "Learning Settings")]
        public bool TradeShort { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Close All Positions At EOD", Order = 3, GroupName = "Learning Settings")]
        public bool CloseAtEOD { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Minimum Stop/Trailing (ATR multiple)", Order = 4, GroupName = "Learning Settings")]
        public double MinimumATRMultiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use R Multiple (RR)", Order = 5, GroupName = "R Multiple Settings (Optional)")]
        public bool UseRR { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "RR multiple", Order = 6, GroupName = "R Multiple Settings (Optional)")]
        public double RRMultiple { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "$ Stop Loss Amount (Ideal Amount)", Order = 7, GroupName = "Ideal Amount")]
        public double StopLossAmount { get; set; }

        [NinjaScriptProperty]
        [Range(1, 200)]
        [Display(Name = "Buy/Sell Range %", Order = 8, GroupName = "Learning Settings")]
        public double BuySellRange { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Fibometer", Order = 9, GroupName = "Aesthetics")]
        public bool Fibometer { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Zig Zag Projection", Order = 9, GroupName = "Aesthetics")]
        public bool ShowProjection { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Breakout Labels", Order = 10, GroupName = "Aesthetics")]
        public bool ShowLabels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show HTF ZigZag", Order = 11, GroupName = "Aesthetics")]
        public bool ShowHTFZZ { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "LTF (minutes) for ZigZag", Order = 12, GroupName = "Zig Zag Settings")]
        public int LTFMinutes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "HTF (minutes) for ZigZag", Order = 13, GroupName = "Zig Zag Settings")]
        public int HTFMinutes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "LTF Line Color", Order = 14, GroupName = "Zig Zag Settings")]
        public Brush LTFLineBrush { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "HTF Line Color", Order = 15, GroupName = "Zig Zag Settings")]
        public Brush HTFLineBrush { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Only show last pivot", Order = 16, GroupName = "Aesthetics")]
        public bool LastOnly { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Performance Table Position", Order = 17, GroupName = "Aesthetics")]
        public TablePositionEnum TablePosition { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Label Size", Order = 18, GroupName = "Aesthetics")]
        public LabelSizeEnum LabelSize { get; set; }
        #endregion

        #region Variables
        private const int EndLoop = 99; // Number of boxes for IQ meter
        private const double Buffer = 0.0; // Buffer for zigzag (PineScript uses 0)

        private int ltfBarsInProgress = 1;
        private int htfBarsInProgress = 2;

        // Actual resolved timeframe values (used internally, don't modify user's inputs)
        private int resolvedLTFMinutes = 0;
        private int resolvedHTFMinutes = 0;

        private ATR atrOnPrimary;
        private ATR atrOnLtf;
        private ATR atrOnHtf;

        // Store latest ATR values for cross-BarsInProgress access
        private double lastLtfATR = 0;
        private double lastHtfATR = 0;
        private double firstValidLtfATR = 0;  // Track first valid ATR for backfilling
        private double firstValidHtfATR = 0;  // Track first valid ATR for backfilling
        private bool hasBackfilledATR = false; // Track if we've backfilled

        // Manual ATR calculation for multi-timeframe
        private List<double> ltfTrueRanges = new List<double>();
        private List<double> htfTrueRanges = new List<double>();
        private const int ATR_PERIOD = 14;

        // IQ Meter state tracking
        private bool lastCondUpHTF = false;
        private bool lastCondUpLTF = false;
        private bool lastCondDnHTF = false;
        private bool lastCondDnLTF = false;

        // Previous bar Y1 values for PineScript [1] operator
        private double prevBarY1HTF = 0;
        private double prevBarY1LTF = 0;

        // Trade entry/exit markers (for visual display like PineScript triangles)
        private List<TradeMarker> tradeMarkers = new List<TradeMarker>();

        #region RadiIQ Core Data Structures

        // ============================================================================
        // OPTIMIZATION ENGINE - Parameter Arrays (5,400 combinations)
        // ============================================================================
        private List<double> zzCurrATR = new List<double>();  // LTF ATR multiples: 1.0 to 5.5 (10 values)
        private List<double> zzHTFATR = new List<double>();   // HTF ATR multiples: 1.0 to 5.5 (10 values)
        private List<double> atrPTarr = new List<double>();   // Target multiples: 1.0 to 3.5 (6 values)
        private List<double> atrTarr = new List<double>();    // Trailing multiples: 0.5 to 4.5 (9 values)
        private List<string> stringArr = new List<string>();  // Parameter combination labels

        // ============================================================================
        // PERFORMANCE TRACKING - Arrays per parameter combination
        // ============================================================================
        // Long trades
        private List<double> perfArr = new List<double>();        // Profit factors (longs)
        private List<double> entryArr = new List<double>();       // Entry prices (longs)
        private List<double> exitArr = new List<double>();        // Exit/SL prices (longs)
        private List<double> triggerArr = new List<double>();     // Profit target triggers (longs)
        private List<double> limitArr = new List<double>();       // Limit prices (longs)
        private List<int> tradesArr = new List<int>();            // Trade count (longs)
        private List<int> winsArr = new List<int>();              // Win count (longs)
        private List<double> PFprofitArr = new List<double>();    // Profit sum (longs)
        private List<double> PFlossArr = new List<double>();      // Loss sum (longs)
        private List<int> boolArr = new List<int>();              // Trade state: 0=closed, 1=initial, 2=trailing (longs)

        // Short trades
        private List<double> perfArrS = new List<double>();       // Profit factors (shorts)
        private List<double> entryArrS = new List<double>();      // Entry prices (shorts)
        private List<double> exitArrS = new List<double>();       // Exit/SL prices (shorts)
        private List<double> triggerArrS = new List<double>();    // Profit target triggers (shorts)
        private List<double> limitArrS = new List<double>();      // Limit prices (shorts)
        private List<int> tradesArrS = new List<int>();           // Trade count (shorts)
        private List<int> winsArrS = new List<int>();             // Win count (shorts)
        private List<double> PFprofitArrS = new List<double>();   // Profit sum (shorts)
        private List<double> PFlossArrS = new List<double>();     // Loss sum (shorts)
        private List<int> boolArrS = new List<int>();             // Trade state: 0=closed, -1=initial, -2=trailing (shorts)

        // Risk/Reward arrays
        private List<double> RRarr = new List<double>();          // R:R profit targets (longs)
        private List<double> RRarrS = new List<double>();         // R:R profit targets (shorts)
        private List<int> divArr = new List<int>();               // Division counter (longs): 1=initial, 2=target hit
        private List<int> divArrS = new List<int>();              // Division counter (shorts): 1=initial, 2=target hit

        // ============================================================================
        // ZIGZAG MASTER ARRAYS - For all parameter combinations
        // ============================================================================
        private List<double> y1PriceArrMasterLTF = new List<double>();    // LTF Y1 prices (all combos)
        private List<double> y2PriceArrMasterLTF = new List<double>();    // LTF Y2 prices (all combos)
        private List<int> masterDirArrLTF = new List<int>();              // LTF directions (all combos)
        private List<double> pointArrLTF = new List<double>();            // LTF current pivot points (all combos)

        private List<double> y1PriceArrMasterHTF = new List<double>();    // HTF Y1 prices (all combos)
        private List<double> y2PriceArrMasterHTF = new List<double>();    // HTF Y2 prices (all combos)
        private List<int> masterDirArrHTF = new List<int>();              // HTF directions (all combos)
        private List<double> pointArrHTF = new List<double>();            // HTF current pivot points (all combos)

        // Breakout point arrays
        private List<double> getBreakoutPointUpArr = new List<double>();   // Upward breakout levels (longs)
        private List<double> getBreakoutPointDnArr = new List<double>();   // Downward breakout levels (shorts)

        // ============================================================================
        // HISTORICAL DATA BUFFERS
        // ============================================================================
        private List<double> ltfCloArr = new List<double>();      // LTF close prices (current bar)
        private List<double> ltfCloArr1 = new List<double>();     // LTF close prices (previous bar)
        private List<double> atrArrLTF = new List<double>();      // LTF ATR values
        private List<double> atrArrHTF = new List<double>();      // HTF ATR values
        private List<bool> closer2lowArr = new List<bool>();      // Did bar close closer to low?
        private List<bool> isLastBarArray = new List<bool>();     // Is this the last bar of session?

        // LTF bar change detection (matches PineScript isLastBar logic)
        private int lastProcessedLTFBar = -1;  // Track last LTF bar we processed

        // OHLC history arrays (for backtesting)
        // PRIMARY (Chart timeframe) bars - used for timestamp and primary data
        private List<double> openArrEnd = new List<double>();
        private List<double> highArrEnd = new List<double>();
        private List<double> lowArrEnd = new List<double>();
        private List<double> closeArrEnd = new List<double>();
        private List<DateTime> timeArrEnd = new List<DateTime>();
        private List<double> ohlc4ArrEnd = new List<double>();   // (O+H+L+C)/4

        // ============================================================================
        // HISTORICAL PERFORMANCE AGGREGATION
        // ============================================================================
        private double historicalLongsWins = 0;
        private double historicalLongsPFPFORIT = 0;      // Profit on winners
        private double historicalLongsPFLOSS = 0;        // Loss on losers
        private double historicalLongsTrades = 0;

        private double historicalShortWins = 0;
        private double historicalShortPFPFORIT = 0;
        private double historicalShortPFLOSS = 0;
        private double historicalShortTrades = 0;

        // ============================================================================
        // CURRENT TRADE STATE (Best Parameter Set)
        // ============================================================================
        // Long trade state
        private double entryLong = 0;
        private double exitLong = 0;
        private int inTradeLong = 0;         // 0=closed, 1=initial, 2=trailing
        private double limitLong = 0;        // Stop loss level
        private double triggerLong = 0;      // Profit target trigger
        private double RRtpLong = 0;         // R:R profit target
        private int divLong = 1;             // Division counter

        // Short trade state
        private double entryShort = 0;
        private double exitShort = 0;
        private int inTradeShort = 0;        // 0=closed, -1=initial, -2=trailing
        private double limitShort = 0;
        private double triggerShort = 0;
        private double RRtpShort = 0;
        private int divShort = 1;

        // ============================================================================
        // BEST PARAMETERS (Selected from optimization)
        // ============================================================================
        private double bestATRLTF = 2.0;      // Best LTF ATR multiplier
        private double bestATRHTF = 2.0;      // Best HTF ATR multiplier
        private double bestTrailing = 1.5;    // Best trailing stop multiplier
        private double bestTarget = 3.0;      // Best profit target multiplier

        private double bestATRshortltf = 2.0;
        private double bestATRshorthtf = 2.0;
        private double bestTrailingS = 1.5;
        private double bestTargetS = 3.0;

        private int bestLongsIndex = 0;       // Index of best long parameter combo
        private int bestShortsIndex = 0;      // Index of best short parameter combo

        // Store optimization PF results (display these, not live trading results)
        private double optimizationPFLong = 0;   // PF from optimization for best long params
        private double optimizationPFShort = 0;  // PF from optimization for best short params

        // ============================================================================
        // ZIGZAG STATE - Enhanced with 4 separate trackers
        // ============================================================================
        private class ZigZagState
        {
            // Basic pivot tracking
            public double Point;
            public DateTime TimeP;
            public int Direction; // 1 = up, -1 = down, 0 = init
            public double Y1Price;
            public double Y2Price;
            public DateTime Y1Time;
            public DateTime Y2Time;
            [System.Xml.Serialization.XmlIgnore()]
            [System.ComponentModel.Browsable(false)]
            public List<ZigZagLine> Lines = new List<ZigZagLine>();
            public bool IsHighFirst => Y2Price > Y1Price;
            public double BreakoutLevel = double.NaN;
            [System.Xml.Serialization.XmlIgnore()]
            [System.ComponentModel.Browsable(false)]
            public List<BreakoutLine> BreakoutLines = new List<BreakoutLine>();
            [System.Xml.Serialization.XmlIgnore()]
            [System.ComponentModel.Browsable(false)]
            public List<PivotPriceLine> PivotPriceLines = new List<PivotPriceLine>();  // Y1 price level lines (solid red)

            // Current developing line (SOLID) - matches PineScript zzLine.last()
            // This line updates every bar until pivot confirms, then moves to Lines list
            public ZigZagLine CurrentLine = null;

            // Enhanced RadiIQ tracking
            public List<double> Y1PriceHistory = new List<double>();
            public List<double> Y2PriceHistory = new List<double>();
            public List<DateTime> Y1TimeHistory = new List<DateTime>();
            public List<DateTime> Y2TimeHistory = new List<DateTime>();
            public List<int> DirectionHistory = new List<int>();
            public double BreakoutPointUp = 0;        // For long breakouts
            public double BreakoutPointDn = 20e20;    // For short breakouts (initialized high)
            public DateTime BreakoutPointUpTime;      // Time when BreakoutPointUp was set
            public DateTime BreakoutPointDnTime;      // Time when BreakoutPointDn was set
        }

        private class ZigZagLine
        {
            public DateTime X1;
            public double Y1;
            public DateTime X2;
            public double Y2;
            public bool IsDotted;
        }

        private class BreakoutLine
        {
            public DateTime X1;           // Start time (pivot time)
            public DateTime X2;           // Initially same as X1, extended until mitigated
            public double Y;              // Breakout level (Y1 price)
            public bool IsActive;         // True until price crosses back
            public bool IsLongBreakout;   // True for upward breakout, false for downward
        }

        private class PivotPriceLine
        {
            public DateTime X1;           // Start time (when Y1 pivot was set)
            public DateTime? X2;          // End time (when next pivot confirmed, null = extends to current)
            public double Y;              // Y1 price level
            public bool IsDownPivot;      // True if this is a down pivot (Y2 < Y1)
        }

        private SharpDX.Direct2D1.StrokeStyle dottedStrokeStyle;

        // 4 independent ZigZag trackers
        private ZigZagState zzLTF_Long;      // LTF for long strategy
        private ZigZagState zzLTF_Short;     // LTF for short strategy
        private ZigZagState zzHTF_Long;      // HTF for long strategy
        private ZigZagState zzHTF_Short;     // HTF for short strategy

        // Legacy aliases (for compatibility)
        private ZigZagState zzLTF => zzLTF_Long;
        private ZigZagState zzHTF => zzHTF_Long;

        // ============================================================================
        // PERFORMANCE TABLE DATA
        // ============================================================================
        private List<List<string>> tableString = new List<List<string>>();    // Top 3 long params
        private List<List<string>> tableStringS = new List<List<string>>();   // Top 3 short params
        private double tablePF = 0;       // Best long profit factor
        private double tablePFS = 0;      // Best short profit factor

        // ============================================================================
        // ============================================================================
        // TRADE MARKER CLASS - For visual entry/exit indicators (PineScript triangles)
        // ============================================================================
        private class TradeMarker
        {
            public DateTime Time;        // Bar time
            public double Price;         // Entry/exit price
            public int BarIndex;         // Bar index for position
            public bool IsEntry;         // true = entry, false = exit
            public bool IsLong;          // true = long, false = short
            public string Tooltip;       // Tooltip text with trade details
        }

        // STATE FLAGS
        // ============================================================================
        private bool trained = false;              // Has optimization completed?
        private bool isLastConfirmedHistory = false;  // Are we at last historical bar?
        private int optimizationCompleteBar = -1;  // Bar number when optimization completed

        #endregion

        private string performanceText = "";
        private double profitFactor = 1.5;
        private int debugDrawCounter = 0;

        // SharpDX resources
        private SharpDX.Direct2D1.Brush ltfBrushDX;
        private SharpDX.Direct2D1.Brush htfBrushDX;
        private SharpDX.Direct2D1.Brush ltfBrushDXFaded;
        private SharpDX.Direct2D1.Brush htfBrushDXFaded;
        private SharpDX.Direct2D1.Brush projectionBrushLongDX;
        private SharpDX.Direct2D1.Brush projectionBrushShortDX;
        private SharpDX.Direct2D1.Brush fibBorderBrushDX;
        private SharpDX.Direct2D1.Brush fibBackgroundBrushDX;
        private SharpDX.Direct2D1.Brush textBrushDX;
        private SharpDX.Direct2D1.Brush whiteTextBrushDX;
        private SharpDX.Direct2D1.Brush breakoutBrushDX;
        private SharpDX.DirectWrite.TextFormat textFormat;
        private SharpDX.DirectWrite.TextFormat smallTextFormat;

        // IQ Meter boxes gradients (separate for HTF and LTF)
        private List<SharpDX.Direct2D1.Brush> iqMeterBrushesHTF = new List<SharpDX.Direct2D1.Brush>();
        private List<SharpDX.Direct2D1.Brush> iqMeterBrushesLTF = new List<SharpDX.Direct2D1.Brush>();
        private bool needsIQMeterRefresh = true;

        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "ImpulseIQ";
                Description = "Complete port of Impulse IQ with dual IQ meters";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                ScaleJustification = ScaleJustification.Right;

                // CRITICAL: Load enough bars for optimization (matches PineScript calc_bars_count = 100000)
                // This ensures the optimization has sufficient historical data to test all parameter combinations
                // BarsRequired = 100000;

                // Default values
                StrategyType = StrategyTypeEnum.Breakout;
                TradeLong = true;
                TradeShort = true;
                CloseAtEOD = false;
                MinimumATRMultiplier = 0.4;
                UseRR = false;
                RRMultiple = 2.0;
                StopLossAmount = 100.0;
                BuySellRange = 100.0;  // Default 100% = full range (matches RadiIQ)
                Fibometer = false;
                ShowProjection = true;
                ShowLabels = false;
                ShowHTFZZ = true;
                LTFMinutes = 0;
                HTFMinutes = 0;

                // Create frozen brushes for thread safety
                var ltfBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 217, 144));
                ltfBrush.Freeze();
                LTFLineBrush = ltfBrush;

                var htfBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(105, 41, 242));
                htfBrush.Freeze();
                HTFLineBrush = htfBrush;

                LastOnly = false;
                TablePosition = TablePositionEnum.TopRight;
                LabelSize = LabelSizeEnum.Small;
            }
            else if (State == State.Configure)
            {
                // PineScript logic: empty timeframe (0 in NT8) = chart timeframe for BOTH LTF and HTF
                // In PineScript: var tfltf = timeframe.from_seconds(timeframe.in_seconds(input.timeframe(defval = "")))
                // defval = "" means use chart timeframe

                // Get chart's timeframe value
                int chartTimeframeMinutes = Math.Max(1, Bars.BarsPeriod.Value);
                
                // Print($"Chart Timeframe: {chartTimeframeMinutes} minutes");
                // Resolve LTF: if user set it to 0, use chart timeframe, otherwise use their value
                resolvedLTFMinutes = (LTFMinutes <= 0) ? chartTimeframeMinutes : LTFMinutes;

                // Resolve HTF: if user set it to 0, use chart timeframe, otherwise use their value
                resolvedHTFMinutes = (HTFMinutes <= 0) ? chartTimeframeMinutes : HTFMinutes;

                // IMPORTANT: Only add data series if they differ from chart timeframe
                // BarsInProgress index logic:
                // - If timeframe == chart: use BarsInProgress = 0
                // - If timeframe != chart: add series and track index

                int nextBarsIndex = 1;

                // Handle LTF
                if (resolvedLTFMinutes == chartTimeframeMinutes)
                {
                    ltfBarsInProgress = 0; // Use primary chart bars
                }
                else
                {
                    AddDataSeries(BarsPeriodType.Minute, resolvedLTFMinutes);
                    ltfBarsInProgress = nextBarsIndex++;
                }

                // Handle HTF
                if (resolvedHTFMinutes == chartTimeframeMinutes)
                {
                    htfBarsInProgress = 0; // Use primary chart bars
                }
                else if (resolvedHTFMinutes == resolvedLTFMinutes && ltfBarsInProgress != 0)
                {
                    // HTF same as LTF and LTF was added - reuse same series
                    htfBarsInProgress = ltfBarsInProgress;
                }
                else
                {
                    AddDataSeries(BarsPeriodType.Minute, resolvedHTFMinutes);
                    htfBarsInProgress = nextBarsIndex++;
                }

                // Print($"Configured: Chart={chartTimeframeMinutes}min, LTF={resolvedLTFMinutes}min (BIP={ltfBarsInProgress}), HTF={resolvedHTFMinutes}min (BIP={htfBarsInProgress})");
            }
            else if (State == State.DataLoaded)
            {
                // CRITICAL FIX: Match PineScript's exact ATR calculation
                // PineScript line 83: atr = ta.atr(14)  (calculated on primary chart)
                // PineScript line 129: atrLTF from request.security(tfltf, [atr]) - PRIMARY chart ATR resampled
                // PineScript line 130: htfATR from request.security(tfhtf, [ta.atr(14)]) - HTF chart ATR directly
                atrOnPrimary = ATR(14);  // Primary chart ATR
                atrOnLtf = ATR(BarsArray[ltfBarsInProgress], 14);  // LTF chart ATR directly
                atrOnHtf = ATR(BarsArray[htfBarsInProgress], 14);  // HTF chart ATR directly

                // Initialize 4 separate ZigZag trackers (RadiIQ enhancement)
                zzLTF_Long = new ZigZagState();
                zzLTF_Short = new ZigZagState();
                zzHTF_Long = new ZigZagState();
                zzHTF_Short = new ZigZagState();

                // Initialize directions
                zzLTF_Long.Direction = 0;
                zzLTF_Short.Direction = 0;
                zzHTF_Long.Direction = 0;
                zzHTF_Short.Direction = 0;

                // Initialize parameter arrays for optimization (5,400 combinations)
                InitializeOptimizationParameters();

                // Debug: Print ATR initialization
                // Print($"[DataLoaded] ATR Indicators Created:");
                // Print($"  - Primary ATR: {(atrOnPrimary != null ? "OK" : "NULL")}");
                // Print($"  - LTF ATR (BIP={ltfBarsInProgress}): {(atrOnLtf != null ? "OK" : "NULL")}");
                // Print($"  - HTF ATR (BIP={htfBarsInProgress}): {(atrOnHtf != null ? "OK" : "NULL")}");
                // Print($"  - Both use same BIP? {(ltfBarsInProgress == htfBarsInProgress)}");
                // Print($"[DataLoaded] Optimization arrays initialized: {stringArr.Count} parameter combinations");
            }
        }

        #region Optimization Parameter Initialization

        private void InitializeOptimizationParameters()
        {
            // Clear any existing data
            zzCurrATR.Clear();
            zzHTFATR.Clear();
            atrPTarr.Clear();
            atrTarr.Clear();
            stringArr.Clear();

            // PineScript lines 178-185: Create PAIRED LTF/HTF multipliers
            // count  = 0.5, countx = 0.5
            // for i = 0 to 10
            //     countx := 1
            //     count  += 0.5
            //     for x = 0 to 9
            //         countx += 0.5
            //         zzCurrATR.push(count)
            //         zzHTFATR .push(countx)
            double count = 0.5;
            for (int i = 0; i <= 10; i++)  // 11 iterations (0 to 10 inclusive)
            {
                double countx = 1.0;  // Reset to 1.0 for each outer loop
                count += 0.5;
                for (int x = 0; x <= 9; x++)  // 10 iterations (0 to 9 inclusive)
                {
                    countx += 0.5;
                    zzCurrATR.Add(count);   // LTF multiplier
                    zzHTFATR.Add(countx);   // HTF multiplier (paired)
                }
            }
            // This creates 11 × 10 = 110 LTF/HTF pairs

            // PineScript lines 187-199: Target and Trailing multipliers
            // countTarget   = min - 0.5
            // countTrailing = min - 0.5
            // for i = 0 to 2 (3 iterations)
            //     countTarget   += 1
            //     countTrailing := min - 0.5
            //     for x = 0 to 7 (8 iterations)
            //         countTrailing += 0.5
            //         atrPTarr.push(countTarget)
            //         atrTarr .push(countTrailing)
            // CRITICAL: Use MinimumATRMultiplier property (default 0.4) NOT hardcoded 0.5!
            double min = MinimumATRMultiplier;
            double countTarget = min - 0.5;  // With default 0.4: = -0.1
            for (int i = 0; i <= 2; i++)  // 3 iterations
            {
                countTarget += 1.0;  // First: 0.9, Second: 1.9, Third: 2.9 (with default min=0.4)
                double countTrailing = min - 0.5;  // = -0.1
                for (int x = 0; x <= 7; x++)  // 8 iterations
                {
                    countTrailing += 0.5;  // 0.4, 0.9, 1.4, 1.9, 2.4, 2.9, 3.4, 3.9 (with default min=0.4)
                    atrPTarr.Add(countTarget);
                    atrTarr.Add(countTrailing);
                }
            }
            // This creates 3 × 8 = 24 target/trailing pairs

            // PineScript lines 201-205: Generate all combinations
            // for i = 0 to zzCurrATR.size() - 1
            //     for x = 0 to atrTarr.size() - 1
            //         stringArr.push(str.tostring(zzCurrATR.get(i)) + "_" + str.tostring(zzHTFATR.get(i)) + "_" + str.tostring(atrPTarr.get(x)) + "_" + str.tostring(atrTarr.get(x)))
            // Total: 110 ATR pairs × 24 target/trailing pairs = 2,640 combinations
            for (int i = 0; i < zzCurrATR.Count; i++)
            {
                for (int x = 0; x < atrTarr.Count; x++)
                {
                    // Create parameter string label: "ltf_htf_target_trailing"
                    string paramLabel = $"{zzCurrATR[i]:F1}_{zzHTFATR[i]:F1}_{atrPTarr[x]:F1}_{atrTarr[x]:F1}";
                    stringArr.Add(paramLabel);

                    // Debug: Verify index 1611 is 4.0_5.0
                    if (stringArr.Count - 1 == 1611)
                    {
                        Print($"[PARAM DEBUG] Index 1611 = {paramLabel}");
                    }

                    // Initialize performance tracking arrays for this combination
                    perfArr.Add(0);
                    entryArr.Add(0);
                    exitArr.Add(0);
                    triggerArr.Add(0);
                    limitArr.Add(0);
                    tradesArr.Add(0);
                    winsArr.Add(0);
                    PFprofitArr.Add(0);
                    PFlossArr.Add(0);
                    boolArr.Add(0);

                    // Short arrays
                    perfArrS.Add(0);
                    entryArrS.Add(0);
                    exitArrS.Add(0);
                    triggerArrS.Add(0);
                    limitArrS.Add(0);
                    tradesArrS.Add(0);
                    winsArrS.Add(0);
                    PFprofitArrS.Add(0);
                    PFlossArrS.Add(0);
                    boolArrS.Add(0);

                    // R:R arrays
                    RRarr.Add(0);
                    RRarrS.Add(0);
                    divArr.Add(1);
                    divArrS.Add(1);

                    // ZigZag master arrays for this combination
                    y1PriceArrMasterLTF.Add(0);
                    y2PriceArrMasterLTF.Add(0);
                    masterDirArrLTF.Add(0);
                    pointArrLTF.Add(0);

                    y1PriceArrMasterHTF.Add(0);
                    y2PriceArrMasterHTF.Add(0);
                    masterDirArrHTF.Add(0);
                    pointArrHTF.Add(0);

                    // Breakout points
                    getBreakoutPointUpArr.Add(0);
                    getBreakoutPointDnArr.Add(double.MaxValue);  // Initialize high for shorts
                }
            }

            // Print($"[InitializeOptimizationParameters] Created {stringArr.Count} parameter combinations");
            // Print($"  - LTF ATR multiples: {zzCurrATR.Count} ({zzCurrATR[0]:F1} to {zzCurrATR[zzCurrATR.Count-1]:F1})");
            // Print($"  - HTF ATR multiples: {zzHTFATR.Count} ({zzHTFATR[0]:F1} to {zzHTFATR[zzHTFATR.Count-1]:F1})");
            // Print($"  - Target multiples: {atrPTarr.Count} ({atrPTarr[0]:F1} to {atrPTarr[atrPTarr.Count-1]:F1})");
            // Print($"  - Trailing multiples: {atrTarr.Count} ({atrTarr[0]:F1} to {atrTarr[atrTarr.Count - 1]:F1})");
        }

        #endregion

        protected override void OnBarUpdate()
        {
            // STRATEGY: Collect data from bar 0 to match PineScript, but protect series access
            // - Data collection starts at bar 0 (line 774: CurrentBar >= 0)
            // - Multi-series safety check (line 779: CurrentBars[ltf/htf] >= 0) prevents access before loaded
            // - ATR indicators accessed only after 20 bars (lines 731, 749: CurrentBars >= 20)
            // - First valid ATR captured only when > 1.0 (lines 738, 756: prevents capturing zeros)
            // - Early bars use fallback ATR of 0.01 (lines 788-791)
            // This prevents array index errors while matching PineScript's bar count

            // IMPORTANT: Handle cases where LTF and/or HTF use different BarsInProgress indices
            // When both use the same index (e.g., both = 0 for chart timeframe), we need to update both

            
            bool isLTFBar = (BarsInProgress == ltfBarsInProgress);
            bool isHTFBar = (BarsInProgress == htfBarsInProgress);
            bool bothUseSameBars = (ltfBarsInProgress == htfBarsInProgress);

            // ========================================================================
            // PHASE 2: DATA COLLECTION (RadiIQ enhancement)
            // Collect historical data for optimization and backtesting
            // CRITICAL: Store ATR when each TF updates, but collect ALL data on PRIMARY bars
            // ========================================================================
            // Store latest ATR values when each timeframe updates (for real-time access)
            // CRITICAL: Only access ATR after enough bars for calculation (prevent array index errors)
            if (BarsInProgress == ltfBarsInProgress && CurrentBars[ltfBarsInProgress] >= 20
                && atrOnLtf != null && atrOnLtf[0] > 0)
            {
                lastLtfATR = atrOnLtf[0];

                // Capture first valid ATR for backfilling (only capture realistic values > 1.0)
                // This prevents capturing near-zero values during ATR warmup period
                if (firstValidLtfATR == 0 && lastLtfATR > 1.0)
                {
                    firstValidLtfATR = lastLtfATR;
                    Print($"[ATR BACKFILL] First valid LTF ATR captured: {firstValidLtfATR:F2} at bar {CurrentBars[ltfBarsInProgress]}");
                }

                // Debug: Print ATR values to verify they're correct
                // if (CurrentBars[ltfBarsInProgress] <= 20 || (Time[0].Hour == 12 && Time[0].Minute == 55))
                //     Print($"[{Time[0]:yyyy-MM-dd HH:mm:ss}]: LTF_ATR={lastLtfATR:F1} (BIP={BarsInProgress}, Bar={CurrentBars[ltfBarsInProgress]})");
            }

            if (BarsInProgress == htfBarsInProgress && CurrentBars[htfBarsInProgress] >= 20
                && atrOnHtf != null && atrOnHtf[0] > 0)
            {
                lastHtfATR = atrOnHtf[0];

                // Capture first valid ATR for backfilling (only capture realistic values > 1.0)
                // This prevents capturing near-zero values during ATR warmup period
                if (firstValidHtfATR == 0 && lastHtfATR > 1.0)
                {
                    firstValidHtfATR = lastHtfATR;
                    Print($"[ATR BACKFILL] First valid HTF ATR captured: {firstValidHtfATR:F2} at bar {CurrentBars[htfBarsInProgress]}");
                }

                // Debug: Print ATR values to verify they're correct
                // if (CurrentBars[htfBarsInProgress] <= 20 || (Time[0].Hour == 12 && Time[0].Minute == 55))
                //     Print($"[{Time[0]:yyyy-MM-dd HH:mm:ss}]: HTF_ATR={lastHtfATR:F1} (BIP={BarsInProgress}, Bar={CurrentBars[htfBarsInProgress]})");
            }

            // Collect ALL data on EVERY PRIMARY bar (matches PineScript exactly)
            // CRITICAL: PineScript collects data on every bar, stores isLastBar flag,
            // then only tests ENTRIES on bars where isLastBar==true during optimization
            // CRITICAL FIX: Do NOT skip bars due to ATR warmup - collect from first bar like PineScript!
            if (BarsInProgress == 0 && CurrentBar >= 0)
            {
                // CRITICAL SAFETY CHECK: Ensure LTF and HTF series have at least 1 bar before accessing
                // Multi-timeframe data loads asynchronously, so we must wait for all series to be available
                // This prevents "accessing a series [barsAgo] with value that is invalid" error
                if (CurrentBars[ltfBarsInProgress] < 0 || CurrentBars[htfBarsInProgress] < 0)
                    return;

                // Collect LTF data - use current values even if ATR is warming up
                double ltfClose = Closes[ltfBarsInProgress][0];
                double ltfClosePrev = CurrentBars[ltfBarsInProgress] >= 1 ? Closes[ltfBarsInProgress][1] : ltfClose;
                ltfCloArr.Add(ltfClose);
                ltfCloArr1.Add(ltfClosePrev);

                // Collect ATR values - use first valid ATR for warmup bars (matches PineScript behavior)
                // CRITICAL: PineScript's request.security() returns valid ATR from first bar
                // because it pulls from a timeframe that already has history
                // We mimic this by using the first valid ATR value we captured
                double ltfATRValue = (lastLtfATR > 0) ? lastLtfATR :
                                     (firstValidLtfATR > 0) ? firstValidLtfATR : 0.01;
                double htfATRValue = (lastHtfATR > 0) ? lastHtfATR :
                                     (firstValidHtfATR > 0) ? firstValidHtfATR : 0.01;
                atrArrLTF.Add(ltfATRValue);
                atrArrHTF.Add(htfATRValue);

                // Collect OHLC data for primary bars
                openArrEnd.Add(Open[0]);
                highArrEnd.Add(High[0]);
                lowArrEnd.Add(Low[0]);
                closeArrEnd.Add(Close[0]);
                timeArrEnd.Add(Time[0]);
                ohlc4ArrEnd.Add((Open[0] + High[0] + Low[0] + Close[0]) / 4.0);

                // Calculate closer2low (did bar close closer to low than high?)
                bool closer2low = Math.Abs(Open[0] - Low[0]) < Math.Abs(High[0] - Open[0]);
                closer2lowArr.Add(closer2low);

                // Detect if this is a new LTF bar close (matches PineScript isLastBar)
                int currentLTFBar = CurrentBars[ltfBarsInProgress];
                bool isNewLTFBar = (currentLTFBar != lastProcessedLTFBar);
                if (isNewLTFBar)
                    lastProcessedLTFBar = currentLTFBar;

                // Store isLastBar flag for this bar (used during optimization)
                isLastBarArray.Add(isNewLTFBar);

                // Debug first 5 and last 5 bars collected
                if (ltfCloArr.Count <= 5 || ltfCloArr.Count >= 5325)
                    Print($"[DATA COLLECT] Bar #{ltfCloArr.Count}, CurrentBar={CurrentBar}, Time={Time[0]:HH:mm}, isLastBar={isNewLTFBar}, ltfATR={ltfATRValue:F2}, htfATR={htfATRValue:F2}");

                // Limit historical arrays to reasonable size (keep last 30,000 bars max)
                if (ltfCloArr.Count > 30000)
                {
                    ltfCloArr.RemoveAt(0);
                    ltfCloArr1.RemoveAt(0);
                    atrArrLTF.RemoveAt(0);
                    atrArrHTF.RemoveAt(0);
                    openArrEnd.RemoveAt(0);
                    highArrEnd.RemoveAt(0);
                    lowArrEnd.RemoveAt(0);
                    closeArrEnd.RemoveAt(0);
                    timeArrEnd.RemoveAt(0);
                    ohlc4ArrEnd.RemoveAt(0);
                    closer2lowArr.RemoveAt(0);
                    isLastBarArray.RemoveAt(0);
                }
            }

            // ========================================================================
            // CRITICAL FIX: Update ZigZag on EACH timeframe as bars arrive
            // Don't wait for training - use default or optimized parameters
            // ========================================================================

            // Determine which ATR multiplier to use (default until trained)
            double ltfATRMultiplier = trained ? bestATRLTF : 2.0;
            double htfATRMultiplier = trained ? bestATRHTF : 2.0;

            // Update LTF if this is an LTF bar
            if (isLTFBar && CurrentBars[ltfBarsInProgress] >= 14)
            {
                // Store PREVIOUS bar's Y1 BEFORE updating (for PineScript [1] operator)
                double tempPrevY1LTF = zzLTF.Y1Price;

                UpdateZigZag(zzLTF, ltfBarsInProgress, ltfATRMultiplier);

                // Store the previous value for comparison on THIS bar
                prevBarY1LTF = tempPrevY1LTF;

                // Debug: Verify LTF zigzag is updating
                if (CurrentBars[ltfBarsInProgress] <= 20)
                    // Print($"[LTF ZZ UPDATE] Bar={CurrentBars[ltfBarsInProgress]}, ATR={lastLtfATR:F1}, Multiplier={ltfATRMultiplier:F1}, Trained={trained}");

                // If LTF and HTF use different series, exit here to avoid running primary logic
                if (!bothUseSameBars)
                    return;
            }

            // Update HTF if this is an HTF bar (and different from LTF, or if both use same bars)
            if (isHTFBar && CurrentBars[htfBarsInProgress] >= 14)
            {
                // Store PREVIOUS bar's Y1 BEFORE updating (for PineScript [1] operator)
                double tempPrevY1HTF = zzHTF.Y1Price;

                UpdateZigZag(zzHTF, htfBarsInProgress, htfATRMultiplier);

                // Store the previous value for comparison on THIS bar
                prevBarY1HTF = tempPrevY1HTF;

                // Debug: Verify HTF zigzag is updating
                if (CurrentBars[htfBarsInProgress] <= 20)
                    // Print($"[HTF ZZ UPDATE] Bar={CurrentBars[htfBarsInProgress]}, ATR={lastHtfATR:F1}, Multiplier={htfATRMultiplier:F1}, Trained={trained}");

                // If HTF uses different series than primary, exit here
                if (htfBarsInProgress != 0)
                    return;
            }

            // ========================================================================
            // PRIMARY BAR LOGIC - Only execute on primary bars
            // ========================================================================
            if (BarsInProgress != 0) return;
            if (CurrentBars[ltfBarsInProgress] < 14 || CurrentBars[htfBarsInProgress] < 14) return;


            // REMOVED: Don't run optimization on every bar - only run once at end of history
            // if (CurrentBar >= 50 && !trained)
            // {
            //     RunOptimizationLoop();
            // }

            // ========================================================================
            // DETECT END OF HISTORICAL DATA - Run Optimization & Select Best Parameters
            // ========================================================================
            // CRITICAL FIX: Relaxed trigger condition to work in all modes
            // Original was too restrictive - now triggers when we have sufficient data

            // Trigger training when we have enough bars OR when transitioning to realtime
            bool hasEnoughData = closeArrEnd.Count >= 200;
            bool isTransitioningToRealtime = (State == State.Realtime && !trained);
            bool isNearEndOfHistory = (State == State.Historical && CurrentBar >= Count - 2);

            bool shouldTrain = hasEnoughData && (isTransitioningToRealtime || isNearEndOfHistory);

            if (!trained && shouldTrain)
            {
                Print($"[TRAINING TRIGGERED] Bars collected: {closeArrEnd.Count}, State: {State}, CurrentBar: {CurrentBar}");
                Print($"[TRAINING] isLastBarArray.Count={isLastBarArray.Count}, true count={isLastBarArray.Count(x => x)}");

                // Analyze time range to detect extended hours data
                if (timeArrEnd.Count > 0)
                {
                    DateTime firstBar = timeArrEnd[0];
                    DateTime lastBar = timeArrEnd[timeArrEnd.Count - 1];
                    Print($"[TIME RANGE] First bar: {firstBar:yyyy-MM-dd HH:mm}, Last bar: {lastBar:yyyy-MM-dd HH:mm}");

                    // Count RTH vs Extended Hours bars (RTH = 09:30-16:00 ET)
                    int rthBars = 0;
                    int extendedBars = 0;
                    double rthATRSum = 0;
                    double extendedATRSum = 0;

                    for (int i = 0; i < timeArrEnd.Count; i++)
                    {
                        TimeSpan barTime = timeArrEnd[i].TimeOfDay;
                        bool isRTH = barTime >= new TimeSpan(9, 30, 0) && barTime < new TimeSpan(16, 0, 0);

                        if (isRTH)
                        {
                            rthBars++;
                            if (i < atrArrLTF.Count) rthATRSum += atrArrLTF[i];
                        }
                        else
                        {
                            extendedBars++;
                            if (i < atrArrLTF.Count) extendedATRSum += atrArrLTF[i];
                        }
                    }

                    double avgRthATR = rthBars > 0 ? rthATRSum / rthBars : 0;
                    double avgExtendedATR = extendedBars > 0 ? extendedATRSum / extendedBars : 0;

                    Print($"[DATA ANALYSIS] RTH bars: {rthBars}, Extended hours bars: {extendedBars}, Total: {timeArrEnd.Count}");
                    Print($"[DATA ANALYSIS] Avg RTH ATR: {avgRthATR:F2}, Avg Extended ATR: {avgExtendedATR:F2}");
                    Print($"[DATA ANALYSIS] PineScript expects ~5344-5355 bars. We have {timeArrEnd.Count} bars. Difference: {timeArrEnd.Count - 5344}");
                }

                // CRITICAL FIX: Backfill zero ATR values with first valid ATR (matches PineScript behavior)
                // PineScript's request.security() returns valid ATR from bar 0, but NinjaTrader's ATR
                // needs 14 bars to warm up, leaving zeros in the array for early bars
                if (!hasBackfilledATR && firstValidLtfATR > 0 && firstValidHtfATR > 0)
                {
                    int backfilledLTF = 0;
                    int backfilledHTF = 0;
                    for (int i = 0; i < atrArrLTF.Count; i++)
                    {
                        if (atrArrLTF[i] < 1.0)  // Replace any suspiciously low values (< 1.0)
                        {
                            atrArrLTF[i] = firstValidLtfATR;
                            backfilledLTF++;
                        }
                        if (atrArrHTF[i] < 1.0)  // Replace any suspiciously low values (< 1.0)
                        {
                            atrArrHTF[i] = firstValidHtfATR;
                            backfilledHTF++;
                        }
                    }
                    Print($"[ATR BACKFILL] Replaced {backfilledLTF} LTF zeros with {firstValidLtfATR:F2}");
                    Print($"[ATR BACKFILL] Replaced {backfilledHTF} HTF zeros with {firstValidHtfATR:F2}");
                    hasBackfilledATR = true;
                }

                // We've transitioned to real-time - optimization training is complete

                // Run optimization loop on ALL historical data (like PineScript barstate.islastconfirmedhistory)
                for (int barIndex = 0; barIndex < closeArrEnd.Count; barIndex++)
                {
                    

                    RunOptimizationLoopForBar(barIndex);
                }

                // Print($"[OPTIMIZATION] Sample results from first 10 combos:");
                // for (int i = 0; i < Math.Min(10, stringArr.Count); i++)
                // {
                //     Print($"  Combo {i} ({stringArr[i]}): Trades={tradesArr[i]}, Wins={winsArr[i]}, Profit={PFprofitArr[i]:F2}, Loss={PFlossArr[i]:F2}");
                // }

                trained = true;
                isLastConfirmedHistory = true;
                optimizationCompleteBar = CurrentBar;

                // Select best performing parameters
                SelectBestParameters();

               

                ReplayHistoryWithBestParameters();

                // CRITICAL: Initialize CurrentLine from the final zigzag state after replay
                // The replay populated Lines list, but we need CurrentLine for real-time updates
                InitializeCurrentLineFromReplay(zzLTF);
                InitializeCurrentLineFromReplay(zzHTF);

                Print($"Training complete! Now using optimized parameters:");
                Print($"  LTF Lines: {zzLTF.Lines.Count}, HTF Lines: {zzHTF.Lines.Count}");
                Print($"  LTF: Y1={zzLTF.Y1Price:F2}, Y2={zzLTF.Y2Price:F2}, CurrentLine={(zzLTF.CurrentLine != null ? "Initialized" : "NULL")}");
                Print($"  HTF: Y1={zzHTF.Y1Price:F2}, Y2={zzHTF.Y2Price:F2}, CurrentLine={(zzHTF.CurrentLine != null ? "Initialized" : "NULL")}");
                Print($"  Configured: ltfBarsInProgress={ltfBarsInProgress}, htfBarsInProgress={htfBarsInProgress}");
                Print($"  Best ATR: LTF={bestATRLTF:F4}, HTF={bestATRHTF:F4}, Same?={(bestATRLTF == bestATRHTF)}");
            }

            // ========================================================================
            // PHASE 5: REAL-TIME TRADE TRACKING (After optimization completes)
            // ========================================================================
            if (trained)
            {
                TrackRealTimeTrades();
            }

            // CRITICAL: Detect breakout crossovers and create lines
            // Breakout points are already set in UpdateZigZag when pivots are confirmed
          

            // Update performance text
            // Match PineScript logic: compare current Y2 with PREVIOUS BAR's Y1
            // PineScript: condUpH = y2PriceHTFLFinal.last() > y1PriceHTFLFinal.last()[1]
            // The [1] means "value from 1 bar ago", not array history
            bool condUpH = zzHTF.Y2Price > prevBarY1HTF;
            bool condUpL = zzLTF.Y2Price > prevBarY1LTF;
            bool condDnH = zzHTF.Y2Price < prevBarY1HTF;
            bool condDnL = zzLTF.Y2Price < prevBarY1LTF;

           

            string trend = (condUpH && condUpL) ? "Uptrend" :
                          (condDnH && condDnL) ? "Downtrend" : "Mixed";

            performanceText = $"Impulse IQ\nLongs PF: {profitFactor:F2}\nTrend: {trend}";

            // Refresh IQ meters when trend changes
            if (condUpH != lastCondUpHTF || condUpL != lastCondUpLTF ||
                condDnH != lastCondDnHTF || condDnL != lastCondDnLTF)
            {
                needsIQMeterRefresh = true;
            }
        }

        private void InitializeCurrentLineFromReplay(ZigZagState zz)
        {
            // After ReplayHistoryWithBestParameters completes, the last line in Lines
            // represents the most recent CONFIRMED pivot. We need to create CurrentLine
            // to represent the DEVELOPING pivot from Y1 to current Y2.
            if (zz.Direction != 0 && zz.Y1Price != 0 && zz.Y2Price != 0)
            {
                zz.CurrentLine = new ZigZagLine
                {
                    X1 = zz.Y1Time,
                    Y1 = zz.Y1Price,
                    X2 = zz.Y2Time,
                    Y2 = zz.Y2Price,
                    IsDotted = false  // Current developing line is SOLID
                };

                // Print($"[InitCurrentLine] Created CurrentLine: {zz.Y1Time:HH:mm} ({zz.Y1Price:F2}) -> {zz.Y2Time:HH:mm} ({zz.Y2Price:F2}), Dir={zz.Direction}");
            }
        }

        private void UpdateZigZag(ZigZagState zz, int barsInProgress, double atrMultiplier)
        {
            if (CurrentBars[barsInProgress] < 1) return;

            double atrValue = 0;

            // CRITICAL FIX: Access ATR using the correct BarsInProgress context
            // This method is ONLY called when BarsInProgress == barsInProgress
            // so we can directly access [0] to get the current value
            if (barsInProgress == ltfBarsInProgress && atrOnLtf != null && atrOnLtf[0] > 0)
            {
                atrValue = atrOnLtf[0] * atrMultiplier;
            }
            else if (barsInProgress == htfBarsInProgress && atrOnHtf != null && atrOnHtf[0] > 0)
            {
                atrValue = atrOnHtf[0] * atrMultiplier;
            }
            else
            {
                // Debug: Why are we returning?
                if (barsInProgress == htfBarsInProgress && CurrentBars[htfBarsInProgress] <= 20)
                {
                    double debugVal = (atrOnHtf != null) ? atrOnHtf[0] : 0;
                    // Print($"[UpdateZigZag HTF] RETURNING! BIP={BarsInProgress}, Target BIP={barsInProgress}, atrOnHtf={(atrOnHtf == null ? "NULL" : debugVal.ToString())}");
                }
                return;
            }

            if (atrValue <= 0)
            {
                // Debug ATR issue
                if (barsInProgress == htfBarsInProgress && CurrentBars[htfBarsInProgress] <= 20)
                {
                    double debugATR = (barsInProgress == htfBarsInProgress && atrOnHtf != null) ? atrOnHtf[0] :
                                       (barsInProgress == ltfBarsInProgress && atrOnLtf != null) ? atrOnLtf[0] : 0;
                    // Print($"[UpdateZigZag] ATR is 0! BIP={BarsInProgress}, Target BIP={barsInProgress}, ATR Indicator: {debugATR}, Multiplier: {atrMultiplier}");
                }
                return;
            }

            double high = Highs[barsInProgress][0];
            double low = Lows[barsInProgress][0];

            // CRITICAL: Use the ACTUAL bar time from the series being processed
            // This ensures zigzag pivots are drawn at the correct time, not 1-2 bars ahead
            DateTime time = Times[barsInProgress][0];

            // Debug
            if (barsInProgress != 0 && CurrentBars[barsInProgress] <= 5)
            {
                // Print($"[UpdateZigZag] BIP={barsInProgress}, Bar Time={time:HH:mm}, High={high:F2}, Low={low:F2}");
            }

            // PineScript Lines 1608-1632: Initialize direction
            // if dirLTF == 0
            //     if getHigh >= pointLTF + getAtr
            //         ... set direction to 1 (up)
            //     else if getLow <= pointLTF - getAtr  // NOTE: MINUS not PLUS!
            //         ... set direction to -1 (down)
            if (zz.Direction == 0)
            {
                zz.Y1Price = Closes[barsInProgress][0];
                zz.Y2Price = Closes[barsInProgress][0];
                zz.Point = Closes[barsInProgress][0];
                zz.Y1Time = time;
                zz.Y2Time = time;
                zz.TimeP = time;
                zz.BreakoutLevel = double.NaN;

                if (high >= zz.Point + atrValue)
                {
                    zz.Direction = 1;
                    zz.Point = high;
                    zz.TimeP = time;
                    zz.Y2Price = high;
                    zz.Y2Time = time;

                    // Create CurrentLine (SOLID developing line) instead of adding to Lines
                    zz.CurrentLine = new ZigZagLine
                    {
                        X1 = zz.Y1Time,
                        Y1 = zz.Y1Price,
                        X2 = time,
                        Y2 = high,
                        IsDotted = false  // Current line is SOLID
                    };
                }
                else if (low <= zz.Point - atrValue)  // CORRECTED: was '+' in buggy PineScript line 1623
                {
                    zz.Direction = -1;
                    zz.Point = low;
                    zz.TimeP = time;
                    zz.Y2Price = low;
                    zz.Y2Time = time;

                    // Create CurrentLine (SOLID developing line) instead of adding to Lines
                    zz.CurrentLine = new ZigZagLine
                    {
                        X1 = zz.Y1Time,
                        Y1 = zz.Y1Price,
                        X2 = time,
                        Y2 = low,
                        IsDotted = false  // Current line is SOLID
                    };
                }
                return;
            }

            // Update existing trend
            if (zz.Direction == 1) // Uptrend
            {
                if (high > zz.Point)
                {
                    zz.Point = high;
                    zz.TimeP = time;
                    zz.Y2Price = high;
                    zz.Y2Time = time;

                    // UPDATE CURRENT LINE (matches PineScript zzLine.last().set_x2/set_y2)
                    // This is the SOLID developing line that updates every bar
                    if (zz.CurrentLine != null)
                    {
                        zz.CurrentLine.X2 = zz.TimeP;  // Use TimeP, not current time
                        zz.CurrentLine.Y2 = zz.Point;  // Use Point, not current high
                    }
                }

                // Check for reversal with buffer
                double addition = Math.Abs(zz.Point - zz.Y1Price) * Buffer;
                if (low <= zz.Point - atrValue - addition)
                {
                    // PIVOT CONFIRMED: Move CurrentLine to Lines as DOTTED (historical)
                    // Matches PineScript: zzLine.last().set_style(line.style_dotted)
                    if (zz.CurrentLine != null)
                    {
                        zz.CurrentLine.IsDotted = true;  // Mark as historical/confirmed
                        zz.Lines.Add(zz.CurrentLine);    // Move to historical lines
                    }

                    // Store the breakout level (Y1) to monitor for future crossovers
                    // Don't create breakout LINE yet - wait for price to cross back over Y1
                    zz.BreakoutLevel = zz.Y1Price;

                    // Store previous Y1/Y2 to history before updating (for PineScript [1] operator)
                    zz.Y1PriceHistory.Add(zz.Y1Price);
                    zz.Y2PriceHistory.Add(zz.Y2Price);
                    zz.Y1TimeHistory.Add(zz.Y1Time);
                    zz.Y2TimeHistory.Add(zz.Y2Time);

                    // Start new downtrend
                    zz.Y1Price = zz.Point;
                    zz.Y1Time = zz.TimeP;
                    zz.Y2Price = low;
                    zz.Y2Time = time;
                    zz.Direction = -1;
                    zz.Point = low;
                    zz.TimeP = time;

                    // Add pivot price line (solid red horizontal line at Y1 price)
                    AddPivotPriceLine(zz, zz.Y1Time, zz.Y1Price, null, isDownPivot: true);

                    // Create NEW CurrentLine for the new downtrend (SOLID)
                    // Matches PineScript: zzLine.push(line.new(...))
                    zz.CurrentLine = new ZigZagLine
                    {
                        X1 = zz.Y1Time,
                        Y1 = zz.Y1Price,
                        X2 = time,
                        Y2 = low,
                        IsDotted = false  // New current line is SOLID
                    };

                    needsIQMeterRefresh = true;

                    // Update breakout points (RadiIQ enhancement)
                    // For longs: breakout up occurs when Y1 > Y2 (high pivot after low)
                    // CRITICAL: Store the time when breakout level is SET (at pivot confirmation)
                    if (zz.Y1Price > zz.Y2Price)
                    {
                        zz.BreakoutPointUp = zz.Y1Price;
                        zz.BreakoutPointUpTime = zz.Y1Time;  // Y1Time is the time of the pivot
                    }
                }
            }
            else if (zz.Direction == -1) // Downtrend
            {
                if (low < zz.Point)
                {
                    zz.Point = low;
                    zz.TimeP = time;
                    zz.Y2Price = low;
                    zz.Y2Time = time;

                    // UPDATE CURRENT LINE (matches PineScript zzLine.last().set_x2/set_y2)
                    // This is the SOLID developing line that updates every bar
                    if (zz.CurrentLine != null)
                    {
                        zz.CurrentLine.X2 = zz.TimeP;  // Use TimeP, not current time
                        zz.CurrentLine.Y2 = zz.Point;  // Use Point, not current low
                    }
                }

                // Check for reversal with buffer
                double addition = Math.Abs(zz.Point - zz.Y1Price) * Buffer;
                if (high >= zz.Point + atrValue + addition)
                {
                    // PIVOT CONFIRMED: Move CurrentLine to Lines as DOTTED (historical)
                    // Matches PineScript: zzLine.last().set_style(line.style_dotted)
                    if (zz.CurrentLine != null)
                    {
                        zz.CurrentLine.IsDotted = true;  // Mark as historical/confirmed
                        zz.Lines.Add(zz.CurrentLine);    // Move to historical lines
                    }

                    // Store the breakout level (Y1) to monitor for future crossovers
                    // Don't create breakout LINE yet - wait for price to cross back over Y1
                    zz.BreakoutLevel = zz.Y1Price;

                    // Store previous Y1/Y2 to history before updating (for PineScript [1] operator)
                    zz.Y1PriceHistory.Add(zz.Y1Price);
                    zz.Y2PriceHistory.Add(zz.Y2Price);
                    zz.Y1TimeHistory.Add(zz.Y1Time);
                    zz.Y2TimeHistory.Add(zz.Y2Time);

                    // Start new uptrend
                    zz.Y1Price = zz.Point;
                    zz.Y1Time = zz.TimeP;
                    zz.Y2Price = high;
                    zz.Y2Time = time;
                    zz.Direction = 1;
                    zz.Point = high;
                    zz.TimeP = time;

                    // Add pivot price line (solid red horizontal line at Y1 price)
                    AddPivotPriceLine(zz, zz.Y1Time, zz.Y1Price, null, isDownPivot: false);

                    // Create NEW CurrentLine for the new uptrend (SOLID)
                    // Matches PineScript: zzLine.push(line.new(...))
                    zz.CurrentLine = new ZigZagLine
                    {
                        X1 = zz.Y1Time,
                        Y1 = zz.Y1Price,
                        X2 = time,
                        Y2 = high,
                        IsDotted = false  // New current line is SOLID
                    };

                    needsIQMeterRefresh = true;

                    // Update breakout points (RadiIQ enhancement)
                    // For shorts: breakout down occurs when Y1 < Y2 (low pivot after high)
                    // CRITICAL: Store the time when breakout level is SET (at pivot confirmation)
                    if (zz.Y1Price < zz.Y2Price)
                    {
                        zz.BreakoutPointDn = zz.Y1Price;
                        zz.BreakoutPointDnTime = zz.Y1Time;  // Y1Time is the time of the pivot
                    }
                }
            }

            // Limit history
            if (zz.Lines.Count > 50)
                zz.Lines.RemoveAt(0);

            // Check for breakout line creation (price crossing back over the breakout level)
            CheckBreakoutCrossover(zz, barsInProgress);

            // Update active breakout lines (extend X2 and check for mitigation)
            UpdateBreakoutLines(zz, barsInProgress);

            // Update IQ meter state
            UpdateIQMeterState(zz, barsInProgress);
        }

        private void CheckBreakoutCrossover(ZigZagState zz, int barsInProgress)
        {
            // Only check if we have a breakout level to monitor
            if (double.IsNaN(zz.BreakoutLevel)) return;
            if (CurrentBars[barsInProgress] < 2) return;

            double close0 = Closes[barsInProgress][0];
            double close1 = Closes[barsInProgress][1];
            DateTime time = Times[barsInProgress][0];

            // Determine if current zigzag is moving up or down
            bool isUptrend = zz.Direction == 1;

            // Check for crossover
            if (isUptrend)
            {
                // Uptrend: Check if price crosses ABOVE the breakout level (which was a previous low)
                if (close0 > zz.BreakoutLevel && close1 <= zz.BreakoutLevel)
                {
                    // Create breakout line
                    zz.BreakoutLines.Add(new BreakoutLine
                    {
                        X1 = time,  // Start from crossover time
                        X2 = time,  // Will be extended each bar
                        Y = zz.BreakoutLevel,
                        IsActive = true,
                        IsLongBreakout = true
                    });

                    // Clear breakout level (only create line once)
                    zz.BreakoutLevel = double.NaN;
                }
            }
            else if (zz.Direction == -1)
            {
                // Downtrend: Check if price crosses BELOW the breakout level (which was a previous high)
                if (close0 < zz.BreakoutLevel && close1 >= zz.BreakoutLevel)
                {
                    // Create breakout line
                    zz.BreakoutLines.Add(new BreakoutLine
                    {
                        X1 = time,  // Start from crossover time
                        X2 = time,  // Will be extended each bar
                        Y = zz.BreakoutLevel,
                        IsActive = true,
                        IsLongBreakout = false
                    });

                    // Clear breakout level (only create line once)
                    zz.BreakoutLevel = double.NaN;
                }
            }
        }

        private void UpdateBreakoutLines(ZigZagState zz, int barsInProgress)
        {
            if (zz.BreakoutLines.Count == 0) return;
            if (CurrentBars[barsInProgress] < 1) return;

            DateTime currentTime = Times[barsInProgress][0];
            double close0 = Closes[barsInProgress][0];
            double close1 = CurrentBars[barsInProgress] >= 2 ? Closes[barsInProgress][1] : close0;

            for (int i = zz.BreakoutLines.Count - 1; i >= 0; i--)
            {
                var line = zz.BreakoutLines[i];

                if (!line.IsActive) continue;

                // Extend X2 to current time
                line.X2 = currentTime;

                // Check if price has mitigated (crossed back) the breakout line
                bool mitigated = false;

                if (line.IsLongBreakout)
                {
                    // Long breakout mitigated if price crosses back BELOW the level
                    if (close0 < line.Y && close1 >= line.Y)
                        mitigated = true;
                }
                else
                {
                    // Short breakout mitigated if price crosses back ABOVE the level
                    if (close0 > line.Y && close1 <= line.Y)
                        mitigated = true;
                }

                if (mitigated)
                {
                    line.IsActive = false;
                }
            }

            // Limit to 10 breakout lines
            while (zz.BreakoutLines.Count > 10)
                zz.BreakoutLines.RemoveAt(0);
        }

        private void UpdateIQMeterState(ZigZagState zz, int barsInProgress)
        {
            if (barsInProgress == ltfBarsInProgress)
            {
                bool condUpLTF = zz.Y2Price > zz.Y1Price;
                bool condDnLTF = zz.Y2Price < zz.Y1Price;

                if (condUpLTF != lastCondUpLTF || condDnLTF != lastCondDnLTF)
                {
                    lastCondUpLTF = condUpLTF;
                    lastCondDnLTF = condDnLTF;
                }
            }
            else if (barsInProgress == htfBarsInProgress)
            {
                bool condUpHTF = zz.Y2Price > zz.Y1Price;
                bool condDnHTF = zz.Y2Price < zz.Y1Price;

                if (condUpHTF != lastCondUpHTF || condDnHTF != lastCondDnHTF)
                {
                    lastCondUpHTF = condUpHTF;
                    lastCondDnHTF = condDnHTF;
                }
            }
        }

        private void AddZigZagLine(ZigZagState zz, DateTime x1, double y1, DateTime x2, double y2, bool isDotted)
        {
            // Debug line creation
            if (zz.Lines.Count < 3)
            {
                // Print($"[AddZigZagLine] Line #{zz.Lines.Count}: X1={x1:HH:mm} ({y1:F2}) -> X2={x2:HH:mm} ({y2:F2})");
            }

            zz.Lines.Add(new ZigZagLine
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                IsDotted = isDotted
            });
        }

        private void AddPivotPriceLine(ZigZagState zz, DateTime x1, double y1Price, DateTime? x2, bool isDownPivot)
        {
            // Close the previous pivot price line if it exists (when new pivot confirms)
            if (zz.PivotPriceLines.Count > 0)
            {
                var lastLine = zz.PivotPriceLines[zz.PivotPriceLines.Count - 1];
                if (lastLine.X2 == null)  // If previous line was open-ended
                {
                    lastLine.X2 = x1;  // Close it at the new pivot's start time
                }
            }

            // Add new pivot price line
            zz.PivotPriceLines.Add(new PivotPriceLine
            {
                X1 = x1,
                X2 = x2,  // null means extends to current time
                Y = y1Price,
                IsDownPivot = isDownPivot
            });

            // Debug
            if (zz.PivotPriceLines.Count <= 3)
            {
                // Print($"[AddPivotPriceLine] Line #{zz.PivotPriceLines.Count}: X1={x1:HH:mm}, Y={y1Price:F2}, Direction={(isDownPivot ? "Down" : "Up")}");
            }
        }

        #region Phase 4: Optimization Engine (RadiIQ Core)

        /// <summary>
        /// Process exits for ALL parameter combinations BEFORE checking entries
        /// Based on RadiIQ optiZZentryExit() function (PineScript lines 1480-1520)
        /// This MUST run before TestEntryExitLogic to properly manage existing positions
        /// </summary>
        private void ProcessExitsForAllCombos(int historyIndex)
        {
            if (stringArr.Count == 0) return;
            if (ltfCloArr.Count == 0 || atrArrLTF.Count == 0) return;
            if (historyIndex < 0 || historyIndex >= ltfCloArr.Count) return;

            double ltfATR = atrArrLTF[Math.Min(historyIndex, atrArrLTF.Count - 1)];
            bool isLastBar = CurrentBar >= Count - 2;
            bool isEOD = IsEndOfTradingDay();

            // Process exits for all 5,400 combinations
            for (int i = 0; i < stringArr.Count; i++)
            {
                // Parse parameter combination to get trailing multiplier
                string[] parts = stringArr[i].Split('_');
                double trailingMult = double.Parse(parts[3]);

                // Process long exits
                int tradeLongState = boolArr[i];
                if (tradeLongState != 0)
                {
                    ProcessLongExit(i, ltfATR, trailingMult, isLastBar, isEOD, historyIndex);
                }

                // Process short exits
                int tradeShortState = boolArrS[i];
                if (tradeShortState != 0)
                {
                    ProcessShortExit(i, ltfATR, trailingMult, isLastBar, isEOD, historyIndex);
                }
            }
        }

        /// <summary>
        /// Process exit logic for a long position at specific parameter combination
        /// Based on RadiIQ boolArr1() and boolArr2() functions (PineScript lines 1209-1341)
        /// </summary>
        private void ProcessLongExit(int index, double ltfATR, double trailingMult, bool isLastBar, bool isEOD, int historyIndex)
        {
            if (index >= boolArr.Count) return;
            if (historyIndex < 0 || historyIndex >= ltfCloArr.Count) return;

            int tradeLongState = boolArr[index];
            double entryLong = entryArr[index];
            double exitLong = exitArr[index];
            double triggerLong = triggerArr[index];
            int div = divArr[index];
            double rrTarget = RRarr[index];

            double ltfOpen = openArrEnd[Math.Min(historyIndex, openArrEnd.Count - 1)];
            double ltfHigh = highArrEnd[Math.Min(historyIndex, highArrEnd.Count - 1)];
            double ltfLow = lowArrEnd[Math.Min(historyIndex, lowArrEnd.Count - 1)];
            double ltfClose = ltfCloArr[historyIndex];
            bool closer2low = closer2lowArr[Math.Min(historyIndex, closer2lowArr.Count - 1)];

            bool exitTriggered = false;

            // Handle R:R partial exit (only when div == 1, meaning full position still on)
            if (div == 1 && UseRR && ltfOpen > exitLong)
            {
                // Open above stop - position still alive, check R:R target
                if (ltfOpen >= rrTarget)
                {
                    // Gapped up to R:R target - partial exit at open
                    double pnl = (ltfOpen - entryLong) / 2.0;
                    PFprofitArr[index] += Math.Abs(pnl);
                    winsArr[index]++;
                    divArr[index] = 2;
                    div = 2;
                }
                else if (ltfHigh >= rrTarget)
                {
                    // R:R target hit during bar
                    if (ltfLow > exitLong)
                    {
                        // Stop not hit - clean R:R exit
                        double pnl = (rrTarget - entryLong) / 2.0;
                        PFprofitArr[index] += Math.Abs(pnl);
                        winsArr[index]++;
                        divArr[index] = 2;
                        div = 2;
                    }
                    else
                    {
                        // Both R:R and stop hit on same bar - check closer2low
                        if (!closer2low)
                        {
                            // Price closer to high - assume R:R hit first
                            double pnl = (rrTarget - entryLong) / 2.0;
                            PFprofitArr[index] += Math.Abs(pnl);
                            winsArr[index]++;
                            divArr[index] = 2;
                            div = 2;
                        }
                        // else: closer to low means stop hit first, handled below
                    }
                }
            }

            // STATE 1: Initial position - stop loss is ALWAYS a loss (stop is below entry)
            if (tradeLongState == 1)
            {
                if (ltfOpen <= exitLong)
                {
                    // Gapped down through stop in State 1 - ALWAYS LOSS
                    exitTriggered = true;
                    double loss = Math.Abs((ltfOpen - entryLong) / div);
                    PFlossArr[index] += loss;
                    if (index == 1611)
                        Print($"[EXIT 4.0_5.0 S1 GAP] Entry={entryLong:F2}, Exit={ltfOpen:F2}, Loss={loss:F4}, Total Loss={PFlossArr[index]:F2}");
                    // Print($"[EXIT S1 STOP GAP #{index}] Entry={entryLong:F2}, Exit={ltfOpen:F2}, Loss={loss:F4}, Div={div}");
                }
                else if (ltfLow <= exitLong)
                {
                    // Stop hit during bar in State 1 - ALWAYS LOSS
                    exitTriggered = true;
                    double loss = Math.Abs((exitLong - entryLong) / div);
                    PFlossArr[index] += loss;
                    if (index == 1611)
                        Print($"[EXIT 4.0_5.0 S1 HIT] Entry={entryLong:F2}, Exit={exitLong:F2}, Loss={loss:F4}, Total Loss={PFlossArr[index]:F2}");
                    // Print($"[EXIT S1 STOP HIT #{index}] Entry={entryLong:F2}, Exit={exitLong:F2}, Loss={loss:F4}, Div={div}");
                }
                // Check EOD close for State 1
                else if (isEOD && CloseAtEOD)
                {
                    exitTriggered = true;
                    double pnl = (ltfClose - entryLong) / div;
                    if (pnl >= 0)
                    {
                        PFprofitArr[index] += Math.Abs(pnl);
                        winsArr[index]++;
                    }
                    else
                    {
                        PFlossArr[index] += Math.Abs(pnl);
                    }
                }
                // Check trigger for trailing stop update (state 1 -> state 2)
                else if (ltfClose >= triggerLong && isLastBar)
                {
                    // Move to trailing state and raise stop
                    boolArr[index] = 2;
                    double newStop = Math.Max(exitLong, ltfClose - ltfATR * trailingMult);
                    exitArr[index] = newStop;
                    // Print($"[STATE 1->2 #{index}] Trigger hit at {ltfClose:F2}, raising stop from {exitLong:F2} to {newStop:F2}");
                }
            }
            // STATE 2: Trailing position - stop loss can be profit or loss (stop may be above entry)
            else if (tradeLongState == 2)
            {
                if (ltfOpen <= exitLong)
                {
                    // Gapped down through stop in State 2 - CHECK if profit or loss
                    exitTriggered = true;
                    double pnl = (ltfOpen - entryLong) / div;
                    if (pnl >= 0)
                    {
                        PFprofitArr[index] += Math.Abs(pnl);
                        winsArr[index]++;
                        if (index == 1611)
                            Print($"[EXIT 4.0_5.0 S2 GAP WIN] Entry={entryLong:F2}, Exit={ltfOpen:F2}, PnL={pnl:F4}, Total Profit={PFprofitArr[index]:F2}");
                        // Print($"[EXIT S2 STOP GAP WIN #{index}] Entry={entryLong:F2}, Exit={ltfOpen:F2}, PnL={pnl:F4}, Div={div}");
                    }
                    else
                    {
                        PFlossArr[index] += Math.Abs(pnl);
                        if (index == 1611)
                            Print($"[EXIT 4.0_5.0 S2 GAP LOSS] Entry={entryLong:F2}, Exit={ltfOpen:F2}, PnL={pnl:F4}, Total Loss={PFlossArr[index]:F2}");
                        // Print($"[EXIT S2 STOP GAP LOSS #{index}] Entry={entryLong:F2}, Exit={ltfOpen:F2}, PnL={pnl:F4}, Div={div}");
                    }
                }
                else if (ltfLow <= exitLong)
                {
                    // Stop hit during bar in State 2 - CHECK if profit or loss
                    exitTriggered = true;
                    double pnl = (exitLong - entryLong) / div;
                    if (pnl >= 0)
                    {
                        PFprofitArr[index] += Math.Abs(pnl);
                        winsArr[index]++;
                        if (index == 1611)
                            Print($"[EXIT 4.0_5.0 S2 HIT WIN] Entry={entryLong:F2}, Exit={exitLong:F2}, PnL={pnl:F4}, Total Profit={PFprofitArr[index]:F2}");
                        // Print($"[EXIT S2 STOP HIT WIN #{index}] Entry={entryLong:F2}, Exit={exitLong:F2}, PnL={pnl:F4}, IsProfit={pnl >= 0}, Div={div}");
                    }
                    else
                    {
                        PFlossArr[index] += Math.Abs(pnl);
                        if (index == 1611)
                            Print($"[EXIT 4.0_5.0 S2 HIT LOSS] Entry={entryLong:F2}, Exit={exitLong:F2}, PnL={pnl:F4}, Total Loss={PFlossArr[index]:F2}");
                        // Print($"[EXIT S2 STOP HIT LOSS #{index}] Entry={entryLong:F2}, Exit={exitLong:F2}, PnL={pnl:F4}, IsProfit={pnl >= 0}, Div={div}");
                    }
                }
                // Check EOD close for State 2
                else if (isEOD && CloseAtEOD)
                {
                    exitTriggered = true;
                    double pnl = (ltfClose - entryLong) / div;
                    if (pnl >= 0)
                    {
                        PFprofitArr[index] += Math.Abs(pnl);
                        winsArr[index]++;
                    }
                    else
                    {
                        PFlossArr[index] += Math.Abs(pnl);
                    }
                }
                // Dynamic trailing stop update when in state 2
                else if (isLastBar)
                {
                    // Continuously raise the trailing stop as price moves up
                    exitArr[index] = Math.Max(exitLong, ltfClose - ltfATR * trailingMult);
                }
            }

            if (exitTriggered)
            {
                boolArr[index] = 0;
                entryArr[index] = 0;
                exitArr[index] = 0;
                triggerArr[index] = 0;
            }
            // if (pnl > 0) {
            //     Print($"[PROFIT] i={index}, pnl={pnl:F2}, div={div}");
            //     PFprofitArr[index] += Math.Abs(pnl);
            // }

        }

        /// <summary>
        /// Process exit logic for a short position at specific parameter combination
        /// Based on RadiIQ boolS1() and boolS2() functions (PineScript lines 1343-1478)
        /// </summary>
        private void ProcessShortExit(int index, double ltfATR, double trailingMult, bool isLastBar, bool isEOD, int historyIndex)
        {
            if (index >= boolArrS.Count) return;
            if (historyIndex < 0 || historyIndex >= ltfCloArr.Count) return;

            int tradeShortState = boolArrS[index];
            double entryShort = entryArrS[index];
            double exitShort = exitArrS[index];
            double triggerShort = triggerArrS[index];
            int div = divArrS[index];
            double rrTarget = RRarrS[index];

            double ltfOpen = openArrEnd[Math.Min(historyIndex, openArrEnd.Count - 1)];
            double ltfHigh = highArrEnd[Math.Min(historyIndex, highArrEnd.Count - 1)];
            double ltfLow = lowArrEnd[Math.Min(historyIndex, lowArrEnd.Count - 1)];
            double ltfClose = ltfCloArr[historyIndex];
            bool closer2low = closer2lowArr[Math.Min(historyIndex, closer2lowArr.Count - 1)];

            bool exitTriggered = false;

            // Handle R:R partial exit (only when div == 1, meaning full position still on)
            if (div == 1 && UseRR && ltfOpen < exitShort)
            {
                // Open below stop - position still alive, check R:R target
                if (ltfOpen <= rrTarget)
                {
                    // Gapped down to R:R target - partial exit at open
                    double pnl = (entryShort - ltfOpen) / 2.0;
                    PFprofitArrS[index] += Math.Abs(pnl);
                    winsArrS[index]++;
                    divArrS[index] = 2;
                    div = 2;
                }
                else if (ltfLow <= rrTarget)
                {
                    // R:R target hit during bar
                    if (ltfHigh < exitShort)
                    {
                        // Stop not hit - clean R:R exit
                        double pnl = (entryShort - rrTarget) / 2.0;
                        PFprofitArrS[index] += Math.Abs(pnl);
                        winsArrS[index]++;
                        divArrS[index] = 2;
                        div = 2;
                    }
                    else
                    {
                        // Both R:R and stop hit on same bar - check closer2low
                        if (closer2low)
                        {
                            // Price closer to low - assume R:R hit first for shorts
                            double pnl = (entryShort - rrTarget) / 2.0;
                            PFprofitArrS[index] += Math.Abs(pnl);
                            winsArrS[index]++;
                            divArrS[index] = 2;
                            div = 2;
                        }
                        // else: closer to high means stop hit first, handled below
                    }
                }
            }

            // STATE -1: Initial position - stop loss is ALWAYS a loss (stop is above entry)
            if (tradeShortState == -1)
            {
                if (ltfOpen >= exitShort)
                {
                    // Gapped up through stop in State -1 - ALWAYS LOSS
                    exitTriggered = true;
                    PFlossArrS[index] += Math.Abs((entryShort - ltfOpen) / div);
                }
                else if (ltfHigh >= exitShort)
                {
                    // Stop hit during bar in State -1 - ALWAYS LOSS
                    exitTriggered = true;
                    PFlossArrS[index] += Math.Abs((entryShort - exitShort) / div);
                }
                // Check EOD close for State -1
                else if (isEOD && CloseAtEOD)
                {
                    exitTriggered = true;
                    double pnl = (entryShort - ltfClose) / div;
                    if (pnl >= 0)
                    {
                        PFprofitArrS[index] += Math.Abs(pnl);
                        winsArrS[index]++;
                    }
                    else
                    {
                        PFlossArrS[index] += Math.Abs(pnl);
                    }
                }
                // Check trigger for trailing stop update (state -1 -> state -2)
                else if (ltfClose <= triggerShort && isLastBar)
                {
                    // Move to trailing state and lower stop
                    boolArrS[index] = -2;
                    exitArrS[index] = Math.Min(exitShort, ltfClose + ltfATR * trailingMult);
                }
            }
            // STATE -2: Trailing position - stop loss can be profit or loss (stop may be below entry)
            else if (tradeShortState == -2)
            {
                if (ltfOpen >= exitShort)
                {
                    // Gapped up through stop in State -2 - CHECK if profit or loss
                    exitTriggered = true;
                    double pnl = (entryShort - ltfOpen) / div;
                    if (pnl >= 0)
                    {
                        PFprofitArrS[index] += Math.Abs(pnl);
                        winsArrS[index]++;
                    }
                    else
                    {
                        PFlossArrS[index] += Math.Abs(pnl);
                    }
                }
                else if (ltfHigh >= exitShort)
                {
                    // Stop hit during bar in State -2 - CHECK if profit or loss
                    exitTriggered = true;
                    double pnl = (entryShort - exitShort) / div;
                    if (pnl >= 0)
                    {
                        PFprofitArrS[index] += Math.Abs(pnl);
                        winsArrS[index]++;
                    }
                    else
                    {
                        PFlossArrS[index] += Math.Abs(pnl);
                    }
                }
                // Check EOD close for State -2
                else if (isEOD && CloseAtEOD)
                {
                    exitTriggered = true;
                    double pnl = (entryShort - ltfClose) / div;
                    if (pnl >= 0)
                    {
                        PFprofitArrS[index] += Math.Abs(pnl);
                        winsArrS[index]++;
                    }
                    else
                    {
                        PFlossArrS[index] += Math.Abs(pnl);
                    }
                }
                // Dynamic trailing stop update when in state -2
                else if (isLastBar)
                {
                    // Continuously lower the trailing stop as price moves down
                    exitArrS[index] = Math.Min(exitShort, ltfClose + ltfATR * trailingMult);
                }
            }

            if (exitTriggered)
            {
                boolArrS[index] = 0;
                entryArrS[index] = 0;
                exitArrS[index] = 0;
                triggerArrS[index] = 0;
            }
        }

        /// <summary>
        /// Main optimization loop - tests all 2640 parameter combinations for a specific bar
        /// Based on RadiIQ optimize() function (PineScript lines 170-344)
        /// </summary>
        private void RunOptimizationLoopForBar(int historyIndex)
        {
            if (stringArr.Count == 0) return;
            if (ltfCloArr.Count == 0 || atrArrLTF.Count == 0 || atrArrHTF.Count == 0) return;
            if (historyIndex < 0 || historyIndex >= ltfCloArr.Count) return;
            if (historyIndex >= highArrEnd.Count || historyIndex >= lowArrEnd.Count) return;

            double ltfClose = ltfCloArr[historyIndex];
            double ltfClosePrev = ltfCloArr1[historyIndex];
            double ltfATR = atrArrLTF[Math.Min(historyIndex, atrArrLTF.Count - 1)];
            double htfATR = atrArrHTF[Math.Min(historyIndex, atrArrHTF.Count - 1)];
            double ltfHigh = highArrEnd[historyIndex];
            double ltfLow = lowArrEnd[historyIndex];

            // CRITICAL: Process exits FIRST before checking for new entries
            // This mirrors PineScript's optiZZentryExit() -> optimizeZZ() order
            ProcessExitsForAllCombos(historyIndex);

            // Test all 5,400 parameter combinations
            for (int i = 0; i < stringArr.Count; i++)
            {
                // Parse parameter combination from string (format: "ltf_htf_target_trailing")
                string[] parts = stringArr[i].Split('_');
                double ltfMult = double.Parse(parts[0]);
                double htfMult = double.Parse(parts[1]);
                double targetMult = double.Parse(parts[2]);
                double trailingMult = double.Parse(parts[3]);

                // Update ZigZag for this parameter combination
                UpdateZigZagOptimization(i, ltfMult, htfMult, ltfATR, htfATR, ltfHigh, ltfLow, ltfClose);

                // Test entry logic ONLY if this is a new LTF bar close (matches PineScript isLastBar check)
                // PineScript: if isLastBar and showOPTI and barstate.isconfirmed
                bool isLastBar = historyIndex < isLastBarArray.Count && isLastBarArray[historyIndex];
                if (isLastBar)
                {
                    TestEntryExitLogic(i, ltfClose, ltfClosePrev, ltfATR, targetMult, trailingMult);
                }
            }
        }

        /// <summary>
        /// Update ZigZag tracking for a specific parameter combination
        /// Based on RadiIQ zzOpti() function (PineScript lines 1026-1053)
        /// </summary>
        private void UpdateZigZagOptimization(int index, double ltfMult, double htfMult, double ltfATR, double htfATR,
                                               double ltfHigh, double ltfLow, double ltfClose)
        {
            if (index >= y1PriceArrMasterLTF.Count) return;

            // Get current values for this combo
            double ltfY1 = y1PriceArrMasterLTF[index];
            double ltfY2 = y2PriceArrMasterLTF[index];
            int ltfDir = masterDirArrLTF[index];
            double ltfPoint = pointArrLTF[index];

            double htfY1 = y1PriceArrMasterHTF[index];
            double htfY2 = y2PriceArrMasterHTF[index];
            int htfDir = masterDirArrHTF[index];
            double htfPoint = pointArrHTF[index];

            // LTF ZigZag update - use primary chart data
            UpdateZigZagForCombo(ref ltfY1, ref ltfY2, ref ltfDir, ref ltfPoint,
                                 ltfHigh, ltfLow, ltfClose, ltfATR * ltfMult, index, true);

            // HTF ZigZag update - ALSO use primary chart data
            // In PineScript, both LTF and HTF use the SAME bar (time[0]) but with different ATR multipliers
            // The difference is the ATR PERIOD (14 bars of LTF vs 14 bars of HTF timeframe)
            UpdateZigZagForCombo(ref htfY1, ref htfY2, ref htfDir, ref htfPoint,
                                 ltfHigh, ltfLow, ltfClose, htfATR * htfMult, index, false);

            // Store updated values
            y1PriceArrMasterLTF[index] = ltfY1;
            y2PriceArrMasterLTF[index] = ltfY2;
            masterDirArrLTF[index] = ltfDir;
            pointArrLTF[index] = ltfPoint;

            y1PriceArrMasterHTF[index] = htfY1;
            y2PriceArrMasterHTF[index] = htfY2;
            masterDirArrHTF[index] = htfDir;
            pointArrHTF[index] = htfPoint;

            // NOTE: Breakout points are now updated INSIDE UpdateZigZagForCombo when reversals occur
            // This matches PineScript quick1/quick2/quick3 behavior
        }

        /// <summary>
        /// Core ZigZag calculation for a single parameter combination
        /// Based on RadiIQ quick1/quick2/quick3 functions (PineScript lines 974-1024)
        /// </summary>
        private void UpdateZigZagForCombo(ref double y1, ref double y2, ref int dir, ref double point,
                                          double high, double low, double close, double atr, int index, bool isLTF)
        {
            if (atr <= 0) return;

            // Initialize if first run (quick3 logic)
            if (dir == 0)
            {
                y1 = close;
                y2 = close;
                point = close;

                // PineScript quick3: if high >= point + atr
                if (high >= point + atr)
                {
                    // Store breakout point BEFORE changing direction
                    getBreakoutPointDnArr[index] = point;

                    y1 = point;
                    y2 = high;
                    dir = 1;
                    point = high;
                }
                // PineScript quick3: else if low <= point - atr
                else if (low <= point - atr)
                {
                    // Store breakout point BEFORE changing direction
                    getBreakoutPointUpArr[index] = point;

                    y1 = point;
                    y2 = low;
                    dir = -1;
                    point = low;
                }
                return;
            }

            // Uptrend (quick1 logic)
            if (dir == 1)
            {
                // Update point to highest high
                if (high > point)
                {
                    point = high;
                }

                // Always update y2 to current point
                y2 = point;

                // CRITICAL FIX: Add Buffer to match replay and real-time logic
                // Check for reversal with buffer (matches PineScript)
                double addition = Math.Abs(point - y1) * Buffer;
                if (low <= point - atr - addition)
                {
                    // CRITICAL: Store breakout point BEFORE changing state
                    // This is the Y1 (pivot high) that price must cross for long entries
                    getBreakoutPointUpArr[index] = point;

                    // DEBUG: Track breakout points for 4.0_5.0 (index 1611)
                    if (index == 1611)
                    {
                        Print($"[BREAKOUT 4.0_5.0] UP pivot formed at {point:F2}, ATR={atr:F2}, Dir: 1→-1");
                    }

                    y1 = point;
                    y2 = low;
                    dir = -1;
                    point = low;
                }
            }
            // Downtrend (quick2 logic)
            else if (dir == -1)
            {
                // Update point to lowest low
                if (low < point)
                {
                    point = low;
                }

                // Always update y2 to current point
                y2 = point;

                // CRITICAL FIX: Add Buffer to match replay and real-time logic
                // Check for reversal with buffer (matches PineScript)
                double addition = Math.Abs(point - y1) * Buffer;
                if (high >= point + atr + addition)
                {
                    // CRITICAL: Store breakout point BEFORE changing state
                    // This is the Y1 (pivot low) that price must cross for short entries
                    getBreakoutPointDnArr[index] = point;

                    // DEBUG: Track breakout points for 4.0_5.0 (index 1611)
                    if (index == 1611)
                    {
                        Print($"[BREAKOUT 4.0_5.0] DN pivot formed at {point:F2}, ATR={atr:F2}, Dir: -1→1");
                    }

                    y1 = point;
                    y2 = high;
                    dir = 1;
                    point = high;
                }
            }
        }

        /// <summary>
        /// Test ENTRY logic ONLY for a specific parameter combination
        /// Based on RadiIQ optimizeZZ() function (PineScript lines 1056-1207)
        /// NOTE: Exit logic is now handled separately in ProcessExitsForAllCombos()
        /// </summary>
        private void TestEntryExitLogic(int index, double ltfClose, double ltfClosePrev,
                                        double ltfATR, double targetMult, double trailingMult)
        {
            if (index >= boolArr.Count) return;

            // Get current trade state for this combo (already updated by ProcessExitsForAllCombos)
            int tradeLongState = boolArr[index];
            int tradeShortState = boolArrS[index];

            // Get ZigZag data for this combo
            double htfY1 = y1PriceArrMasterHTF[index];
            double htfY2 = y2PriceArrMasterHTF[index];
            int htfDir = masterDirArrHTF[index];

            double breakoutUp = getBreakoutPointUpArr[index];
            double breakoutDn = getBreakoutPointDnArr[index];

            // CRITICAL FIX: PineScript uses Y2 > Y1 comparison, NOT direction!
            // CRITICAL FIX: Match PineScript definition EXACTLY
            // PineScript line 2070: isHighFirstShortFinal = y2PriceFinalHTFS.first() > y1PriceFinalHTFS.first()
            // isHighFirst2Long = y2 > y1 (uptrend for longs)
            // isHighFirst2Short = y2 > y1 (uptrend - we want NOT this for shorts)
            bool isHighFirst2Long = htfY2 > htfY1;
            bool isHighFirst2Short = htfY2 > htfY1;  // FIXED: Was htfY2 < htfY1 (opposite of PineScript!)

            // Calculate range for validation
            // PineScript line 1083: Range = math.abs(gety2htf - gety1htf) * (buySellRange / 100)
            // CRITICAL: Must multiply by BuySellRange percentage!
            double htfRange = Math.Abs(htfY2 - htfY1) * (BuySellRange / 100.0);

            // ========================================================================
            // LONG ENTRY LOGIC (Breakout Strategy) - Enter on upside breakout
            // ========================================================================
            if (StrategyType == StrategyTypeEnum.Breakout && TradeLong && tradeLongState == 0)
            {
                // DEBUG: Log entry conditions for 4.0_5.0 to understand why 0 trades
                if (index == 1611 && breakoutUp > 0)
                {
                    Print($"[ENTRY CHECK 4.0_5.0] isHighFirst={isHighFirst2Long}, breakoutUp={breakoutUp:F2}, ltfClose={ltfClose:F2}, ltfClosePrev={ltfClosePrev:F2}");
                    Print($"  HTF: Y1={htfY1:F2}, Y2={htfY2:F2}, Range={htfRange:F2}, MaxBreakout={htfY1 + htfRange:F2}");
                    Print($"  Conditions: close>breakout={ltfClose > breakoutUp}, prev<=breakout={ltfClosePrev <= breakoutUp}, breakout<=maxLevel={breakoutUp <= htfY1 + htfRange}");
                }

                // FIXED: Use isHighFirst2Long instead of isUptrend to match PineScript
                // PineScript: if isHighFirst2Long and tradeLong
                if (isHighFirst2Long && breakoutUp > 0 &&
                    ltfClose > breakoutUp && ltfClosePrev <= breakoutUp &&
                    breakoutUp <= htfY1 + htfRange)
                {
                    // Enter long
                    entryArr[index] = ltfClose;
                    exitArr[index] = ltfClose - (ltfATR * trailingMult);
                    triggerArr[index] = ltfClose + (ltfATR * targetMult);
                    boolArr[index] = 1;

                    // R:R targeting
                    if (UseRR)
                    {
                        double stopDistance = ltfClose - (ltfClose - (ltfATR * trailingMult));
                        RRarr[index] = ltfClose + (stopDistance * RRMultiple);
                        divArr[index] = 1;
                    }

                    tradesArr[index]++;

                    // DEBUG: Track entries for 4.0_5.0_0.9_1.9 (index 1611)
                    if (index == 1611)
                    {
                        Print($"[ENTRY 4.0_5.0 #{tradesArr[index]}] Bar={CurrentBar}, Entry={ltfClose:F2}, Stop={exitArr[index]:F2}, Trigger={triggerArr[index]:F2}, Breakout={breakoutUp:F2}");
                        Print($"  HTF: Y1={htfY1:F2}, Y2={htfY2:F2}, isHighFirst2Long={isHighFirst2Long}, htfRange={htfRange:F2}");
                    }

                    // DEBUG LOGGING - Disabled to reduce spam
                    // Print($"[ENTRY LONG BREAKOUT #{index}] Bar={CurrentBar}, Entry={ltfClose:F2}, Stop={exitArr[index]:F2}, Trigger={triggerArr[index]:F2}, Breakout={breakoutUp:F2}");
                }
            }

            // ========================================================================
            // LONG ENTRY LOGIC (Cheap Strategy) - Enter on pullback/counter-trend
            // Based on PineScript lines 1139-1176
            // ========================================================================
            if (StrategyType == StrategyTypeEnum.Cheap && TradeLong && tradeLongState == 0)
            {
                // FIXED: Use isHighFirst2Long to match PineScript
                // PineScript: if isHighFirst2Long and tradeLong
                if (isHighFirst2Long && breakoutUp > 0 &&
                    ltfClose < breakoutUp && ltfClosePrev >= breakoutUp &&
                    breakoutUp <= htfY1 + htfRange)
                {
                    // Enter long on pullback
                    entryArr[index] = ltfClose;
                    exitArr[index] = ltfClose - (ltfATR * trailingMult);
                    triggerArr[index] = ltfClose + (ltfATR * targetMult);
                    boolArr[index] = 1;

                    // R:R targeting
                    if (UseRR)
                    {
                        double stopDistance = ltfClose - (ltfClose - (ltfATR * trailingMult));
                        RRarr[index] = ltfClose + (stopDistance * RRMultiple);
                        divArr[index] = 1;
                    }

                    tradesArr[index]++;

                    // DEBUG LOGGING
                    // Print($"[ENTRY LONG CHEAP #{index}] Bar={CurrentBar}, Entry={ltfClose:F2}, Stop={exitArr[index]:F2}, Trigger={triggerArr[index]:F2}, Breakout={breakoutUp:F2}");
                }
            }

            // ========================================================================
            // SHORT ENTRY LOGIC (Breakout Strategy) - Enter on downside breakout
            // ========================================================================
            if (StrategyType == StrategyTypeEnum.Breakout && TradeShort && tradeShortState == 0)
            {
                // FIXED: Use isHighFirst2Short (NOT isDowntrend) to match PineScript
                // PineScript: if not isHighFirst2Short and tradeShort
                if (!isHighFirst2Short && breakoutDn < 20e20 &&
                    ltfClose < breakoutDn && ltfClosePrev >= breakoutDn &&
                    breakoutDn >= htfY1 - htfRange)
                {
                    // Enter short
                    entryArrS[index] = ltfClose;
                    exitArrS[index] = ltfClose + (ltfATR * trailingMult);
                    triggerArrS[index] = ltfClose - (ltfATR * targetMult);
                    boolArrS[index] = -1;

                    // R:R targeting
                    if (UseRR)
                    {
                        double stopDistance = (ltfClose + (ltfATR * trailingMult)) - ltfClose;
                        RRarrS[index] = ltfClose - (stopDistance * RRMultiple);
                        divArrS[index] = 1;
                    }

                    tradesArrS[index]++;

                    // DEBUG: Verify short entry is in downtrend
                    // if (tradesArrS[index] <= 3)
                    //     Print($"[SHORT ENTRY {tradesArrS[index]}] Breakout: HTF Y2={htfY2:F2} vs Y1={htfY1:F2}, isDowntrend={!isHighFirst2Short}, Entry={ltfClose:F2}");
                }
            }

            // ========================================================================
            // SHORT ENTRY LOGIC (Cheap Strategy) - Enter on pullback/counter-trend
            // Based on PineScript lines 1177-1207
            // ========================================================================
            if (StrategyType == StrategyTypeEnum.Cheap && TradeShort && tradeShortState == 0)
            {
                // FIXED: Use !isHighFirst2Short to match PineScript
                // PineScript: if not isHighFirst2Short and tradeShort
                if (!isHighFirst2Short && breakoutDn < 20e20 &&
                    ltfClose > breakoutDn && ltfClosePrev <= breakoutDn &&
                    breakoutDn >= htfY1 - htfRange)
                {
                    // Enter short on pullback
                    entryArrS[index] = ltfClose;
                    exitArrS[index] = ltfClose + (ltfATR * trailingMult);
                    triggerArrS[index] = ltfClose - (ltfATR * targetMult);
                    boolArrS[index] = -1;

                    // R:R targeting
                    if (UseRR)
                    {
                        double stopDistance = (ltfClose + (ltfATR * trailingMult)) - ltfClose;
                        RRarrS[index] = ltfClose - (stopDistance * RRMultiple);
                        divArrS[index] = 1;
                    }

                    tradesArrS[index]++;
                }
            }

            // NOTE: Exit logic removed - now handled in ProcessExitsForAllCombos() which runs BEFORE this method
        }

        /// <summary>
        /// Detect breakout crossovers and create horizontal lines
        /// Matching RadiIQ lines 1662-1682 (break up) and 1938-1957 (break down)
        /// </summary>
        private void DetectBreakoutCrossovers(ZigZagState zz)
    {
        if (zz == null) return;
        if (CurrentBar < 2) return;

        double currentClose = Close[0];
        double previousClose = Close[1];
        DateTime now = Time[0];

        // helper: tiny epsilon in price for checking duplicates
        double epsPrice = Math.Max(TickSize, 0.0001);

        bool IsDuplicate(ZigZagState z, double y, bool isLong)
        {
            if (z.BreakoutLines.Count == 0) return false;
            var last = z.BreakoutLines[z.BreakoutLines.Count - 1];
            if (last.IsLongBreakout == isLong && Math.Abs(last.Y - y) <= epsPrice)
            {
                // if last X2 is within a small timeframe (same day or few bars), consider duplicate
                TimeSpan delta = now - last.X2;
                if (Math.Abs(delta.TotalMinutes) < Bars.BarsPeriod.Value * 6) // approx 6 bars
                    return true;
            }
            return false;
        }

        // ===== LONG BREAKOUT: price crosses above confirmed pivot price =====
        if (!double.IsNaN(zz.BreakoutPointUp) &&
            currentClose > zz.BreakoutPointUp &&
            previousClose <= zz.BreakoutPointUp)
        {
            // Use BreakoutPointUpTime (pivot time when breakout level was set) — this matches PineScript's y1xFinalLTF
            DateTime startTime = zz.BreakoutPointUpTime;

            // If BreakoutPointUpTime invalid, attempt fallback
            if (startTime == default(DateTime) || startTime >= now)
            {
                // fallback log once in a while

                // Use Y1Time as fallback
                startTime = zz.Y1Time;

                // If Y1Time also invalid, try Y2Time
                if (startTime == default(DateTime) || startTime >= now)
                {
                    if (zz.Y2Time != default(DateTime) && zz.Y2Time < now)
                        startTime = zz.Y2Time;
                    else if (Times[0].Count > 1)
                        startTime = Times[0][1]; // previous bar time as last resort
                    else
                        startTime = now; // worst-case
                }
            }

            // Prevent duplicates
            if (!IsDuplicate(zz, zz.BreakoutPointUp, true))
            {
                var breakoutLine = new BreakoutLine
                {
                    X1 = startTime,
                    X2 = now,
                    Y = zz.BreakoutPointUp,
                    IsActive = false,
                    IsLongBreakout = true
                };

                // Ensure X1 != X2 (if same-bar, give tiny visible stub)
                if (breakoutLine.X1 == breakoutLine.X2)
                {
                    // nudge X1 back by one bar time if available
                    if (Times[0].Count > 1)
                        breakoutLine.X1 = Times[0][1];
                    // still equal? keep but we'll handle rendering stub
                }

                zz.BreakoutLines.Add(breakoutLine);

                // cap history
                const int MAX_LINES = 500;
                if (zz.BreakoutLines.Count > MAX_LINES)
                    zz.BreakoutLines.RemoveAt(0);


            }
        }

        // ===== SHORT BREAKOUT: price crosses below confirmed pivot price =====
        if (!double.IsNaN(zz.BreakoutPointDn) &&
            currentClose < zz.BreakoutPointDn &&
            previousClose >= zz.BreakoutPointDn)
        {
            // Use BreakoutPointDnTime (pivot time when breakout level was set) — this matches PineScript's y1xFinalLTF
            DateTime startTime = zz.BreakoutPointDnTime;

            // If BreakoutPointDnTime invalid, attempt fallback
            if (startTime == default(DateTime) || startTime >= now)
            {
              
                // Use Y1Time as fallback
                startTime = zz.Y1Time;

                // If Y1Time also invalid, try Y2Time
                if (startTime == default(DateTime) || startTime >= now)
                {
                    if (zz.Y2Time != default(DateTime) && zz.Y2Time < now)
                        startTime = zz.Y2Time;
                    else if (Times[0].Count > 1)
                        startTime = Times[0][1];
                    else
                        startTime = now;
                }
            }

            if (!IsDuplicate(zz, zz.BreakoutPointDn, false))
            {
                var breakoutLine = new BreakoutLine
                {
                    X1 = startTime,
                    X2 = now,
                    Y = zz.BreakoutPointDn,
                    IsActive = false,
                    IsLongBreakout = false
                };

                if (breakoutLine.X1 == breakoutLine.X2)
                {
                    if (Times[0].Count > 1)
                        breakoutLine.X1 = Times[0][1];
                }

                zz.BreakoutLines.Add(breakoutLine);

                const int MAX_LINES = 500;
                if (zz.BreakoutLines.Count > MAX_LINES)
                    zz.BreakoutLines.RemoveAt(0);

           
            }
        }
    }

        /// <summary>
        /// Check if a breakout line already exists at the given price level (replay version)
        /// Prevents creating duplicate lines when price crosses the same level multiple times
        /// A line is considered duplicate if ANY existing line exists at the same price level
        /// that was created from the same pivot (same X1 time)
        /// </summary>
        private bool IsDuplicateReplay(ZigZagState z, double y, bool isLong, DateTime currentTime, DateTime pivotTime)
        {
            double epsPrice = Math.Max(TickSize, 0.0001);

            if (z.BreakoutLines.Count == 0) return false;

            // Check if ANY line already exists at this price level from the same pivot
            // This prevents multiple lines when price crosses the same breakdown level repeatedly
            for (int i = z.BreakoutLines.Count - 1; i >= 0; i--)
            {
                var line = z.BreakoutLines[i];
                if (line.IsLongBreakout == isLong &&
                    Math.Abs(line.Y - y) <= epsPrice &&
                    line.X1 == pivotTime)  // Same pivot = same breakdown level
                {
                    return true;  // Duplicate found - line already exists from this pivot
                }
            }
            return false;
        }

        /// <summary>
        /// Extend active breakout lines to current time
        /// In RadiIQ, lines extend from breakout point to current bar until mitigated
        /// When mitigated, the line stops at the mitigation bar (not removed, just stops extending)
        /// </summary>
        private void ExtendActiveBreakoutLines(ZigZagState zz)
        {
            if (zz.BreakoutLines.Count == 0 || CurrentBar < 1) return;

            DateTime currentTime = Time[0];
            double currentClose = Close[0];
            double previousClose = Close[1];

            // Extend all active breakout lines to current time
            for (int i = zz.BreakoutLines.Count - 1; i >= 0; i--)
            {
                var line = zz.BreakoutLines[i];
                if (!line.IsActive) continue;

                // Check if line should be deactivated (price CROSSED back over it)
                // This is the "mitigation" logic from RadiIQ
                // We need a crossover, not just being below/above
                bool mitigated = false;

                if (line.IsLongBreakout)
                {
                    // Long breakout mitigated when price crosses back BELOW the level
                    if (currentClose < line.Y && previousClose >= line.Y)
                    {
                        line.X2 = currentTime;  // Stop at mitigation point
                        line.IsActive = false;
                        mitigated = true;
                        // Print($"[MITIGATED UP] Line at {line.Y:F2} mitigated at {currentTime}, Close={currentClose:F2}");
                    }
                }
                else
                {
                    // Short breakout mitigated when price crosses back ABOVE the level
                    if (currentClose > line.Y && previousClose <= line.Y)
                    {
                        line.X2 = currentTime;  // Stop at mitigation point
                        line.IsActive = false;
                        mitigated = true;
                        // Print($"[MITIGATED DN] Line at {line.Y:F2} mitigated at {currentTime}, Close={currentClose:F2}");
                    }
                }

                // Only extend the line if it wasn't just mitigated
                if (!mitigated)
                {
                    line.X2 = currentTime;
                }
            }

            // Keep only the most recent 100 lines (active or inactive) to avoid memory bloat
            if (zz.BreakoutLines.Count > 100)
            {
                // Remove oldest lines
                int toRemove = zz.BreakoutLines.Count - 100;
                zz.BreakoutLines.RemoveRange(0, toRemove);
            }
        }

        /// <summary>
        /// Draw breakout lines using NinjaTrader's Draw.Line method
        /// Matching RadiIQ lines 1664-1682 (break up) and 1940-1957 (break down)
        /// Called from OnBarUpdate to draw lines on each bar
        /// </summary>
        // REMOVED: DrawBreakoutLinesNative - using SharpDX rendering in OnRender instead
        // The Draw.Line method was creating duplicate labels and lines
        // All breakout line rendering is handled by DrawBreakoutLines() in OnRender()

        /// <summary>
        /// Track real-time trades using optimized parameters
        /// Based on RadiIQ liveTrades() function
        /// </summary>
        private void TrackRealTimeTrades()
        {
            if (CurrentBars[ltfBarsInProgress] < 2) return;

            double ltfClose = Closes[ltfBarsInProgress][0];
            double ltfClosePrev = Closes[ltfBarsInProgress][1];
            // CRITICAL FIX: Use stored LTF ATR value since we're in BarsInProgress 0 context
            double ltfATR = lastLtfATR;

            if (ltfATR <= 0) return;

            // Get HTF ZigZag data
            bool isUptrend = zzHTF.Y2Price > zzHTF.Y1Price;
            bool isDowntrend = zzHTF.Y2Price < zzHTF.Y1Price;
            double htfRange = Math.Abs(zzHTF.Y2Price - zzHTF.Y1Price);

            // ========================================================================
            // LONG TRADE TRACKING
            // ========================================================================
            if (TradeLong)
            {
                // Entry logic - Breakout Strategy
                if (inTradeLong == 0 && StrategyType == StrategyTypeEnum.Breakout)
                {
                    if (isUptrend && zzLTF.BreakoutPointUp > 0 &&
                        ltfClose > zzLTF.BreakoutPointUp && ltfClosePrev <= zzLTF.BreakoutPointUp &&
                        zzLTF.BreakoutPointUp <= zzHTF.Y1Price + htfRange)
                    {
                        // Enter long
                        entryLong = ltfClose;
                        exitLong = ltfClose - (ltfATR * bestTrailing);
                        triggerLong = ltfClose + (ltfATR * bestTarget);
                        inTradeLong = 1;
                        divLong = 1;

                        if (UseRR)
                        {
                            double stopDistance = ltfClose - exitLong;
                            RRtpLong = ltfClose + (stopDistance * RRMultiple);
                        }

                        // Print($"[LONG ENTRY-BREAKOUT] Bar {CurrentBar}: Entry={entryLong:F2}, SL={exitLong:F2}, TP={triggerLong:F2}");

                        // Track for historical aggregation
                        historicalLongsTrades++;
                    }
                }

                // Entry logic - Cheap Strategy (pullback/counter-trend)
                if (inTradeLong == 0 && StrategyType == StrategyTypeEnum.Cheap)
                {
                    // Enter on pullback: price crosses BELOW breakout in uptrend
                    // NOTE: Use Y2 > Y1 comparison (isHighFirst2Long), not Direction!
                    if (zzHTF.Y2Price > zzHTF.Y1Price && zzLTF.BreakoutPointUp > 0 &&
                        ltfClose < zzLTF.BreakoutPointUp && ltfClosePrev >= zzLTF.BreakoutPointUp &&
                        zzLTF.BreakoutPointUp <= zzHTF.Y1Price + htfRange)
                    {
                        // Enter long on pullback
                        entryLong = ltfClose;
                        exitLong = ltfClose - (ltfATR * bestTrailing);
                        triggerLong = ltfClose + (ltfATR * bestTarget);
                        inTradeLong = 1;
                        divLong = 1;

                        if (UseRR)
                        {
                            double stopDistance = ltfClose - exitLong;
                            RRtpLong = ltfClose + (stopDistance * RRMultiple);
                        }

                        // Print($"[LONG ENTRY-CHEAP] Bar {CurrentBar}: Entry={entryLong:F2}, SL={exitLong:F2}, TP={triggerLong:F2}");

                        // Track for historical aggregation
                        historicalLongsTrades++;
                    }
                }

                // Exit logic
                if (inTradeLong != 0)
                {
                    // Check stop loss
                    if (ltfClose <= exitLong)
                    {
                        double pnl = (exitLong - entryLong) / divLong;
                        if (pnl < 0)
                            historicalLongsPFLOSS += Math.Abs(pnl);
                        else
                        {
                            historicalLongsPFPFORIT += Math.Abs(pnl);
                            historicalLongsWins++;
                        }

                        // Print($"[LONG EXIT-SL] Bar {CurrentBar}: Exit={exitLong:F2}, PnL={pnl:F2}");
                        inTradeLong = 0;
                    }
                    // Check R:R target
                    else if (UseRR && ltfClose >= RRtpLong && divLong == 1)
                    {
                        double pnl = (RRtpLong - entryLong) / 2.0;
                        historicalLongsPFPFORIT += Math.Abs(pnl);
                        historicalLongsWins++;
                        divLong = 2;
                        inTradeLong = 2;

                        // Print($"[LONG PARTIAL] Bar {CurrentBar}: RR Target Hit={RRtpLong:F2}, PnL={pnl:F2}");
                    }
                    // Convert to trailing
                    else if (ltfClose >= triggerLong && inTradeLong == 1)
                    {
                        inTradeLong = 2;
                        // Print($"[LONG TRAILING] Bar {CurrentBar}: Converted to trailing");
                    }
                }
            }

            // ========================================================================
            // SHORT TRADE TRACKING
            // ========================================================================
            if (TradeShort)
            {
                // Entry logic - Breakout Strategy
                if (inTradeShort == 0 && StrategyType == StrategyTypeEnum.Breakout)
                {
                    if (isDowntrend && zzLTF.BreakoutPointDn < 20e20 &&
                        ltfClose < zzLTF.BreakoutPointDn && ltfClosePrev >= zzLTF.BreakoutPointDn &&
                        zzLTF.BreakoutPointDn >= zzHTF.Y1Price - htfRange)
                    {
                        // Enter short
                        entryShort = ltfClose;
                        exitShort = ltfClose + (ltfATR * bestTrailingS);
                        triggerShort = ltfClose - (ltfATR * bestTargetS);
                        inTradeShort = -1;
                        divShort = 1;

                        if (UseRR)
                        {
                            double stopDistance = exitShort - ltfClose;
                            RRtpShort = ltfClose - (stopDistance * RRMultiple);
                        }

                        // Print($"[SHORT ENTRY-BREAKOUT] Bar {CurrentBar}: Entry={entryShort:F2}, SL={exitShort:F2}, TP={triggerShort:F2}");

                        // Track for historical aggregation
                        historicalShortTrades++;
                    }
                }

                // Entry logic - Cheap Strategy (pullback/counter-trend)
                if (inTradeShort == 0 && StrategyType == StrategyTypeEnum.Cheap)
                {
                    // Enter on pullback: price crosses ABOVE breakout in downtrend
                    // NOTE: Use Y2 < Y1 comparison (not isHighFirst2Short), not Direction!
                    if (zzHTF.Y2Price < zzHTF.Y1Price && zzLTF.BreakoutPointDn < 20e20 &&
                        ltfClose > zzLTF.BreakoutPointDn && ltfClosePrev <= zzLTF.BreakoutPointDn &&
                        zzLTF.BreakoutPointDn >= zzHTF.Y1Price - htfRange)
                    {
                        // Enter short on pullback
                        entryShort = ltfClose;
                        exitShort = ltfClose + (ltfATR * bestTrailingS);
                        triggerShort = ltfClose - (ltfATR * bestTargetS);
                        inTradeShort = -1;
                        divShort = 1;

                        if (UseRR)
                        {
                            double stopDistance = exitShort - ltfClose;
                            RRtpShort = ltfClose - (stopDistance * RRMultiple);
                        }

                        // Print($"[SHORT ENTRY-CHEAP] Bar {CurrentBar}: Entry={entryShort:F2}, SL={exitShort:F2}, TP={triggerShort:F2}");

                        // Track for historical aggregation
                        historicalShortTrades++;
                    }
                }

                // Exit logic
                if (inTradeShort != 0)
                {
                    // Check stop loss
                    if (ltfClose >= exitShort)
                    {
                        double pnl = (entryShort - exitShort) / divShort;
                        if (pnl < 0)
                            historicalShortPFLOSS += Math.Abs(pnl);
                        else
                        {
                            historicalShortPFPFORIT += Math.Abs(pnl);
                            historicalShortWins++;
                        }

                        // Print($"[SHORT EXIT-SL] Bar {CurrentBar}: Exit={exitShort:F2}, PnL={pnl:F2}");
                        inTradeShort = 0;
                    }
                    // Check R:R target
                    else if (UseRR && ltfClose <= RRtpShort && divShort == 1)
                    {
                        double pnl = (entryShort - RRtpShort) / 2.0;
                        historicalShortPFPFORIT += Math.Abs(pnl);
                        historicalShortWins++;
                        divShort = 2;
                        inTradeShort = -2;

                        // Print($"[SHORT PARTIAL] Bar {CurrentBar}: RR Target Hit={RRtpShort:F2}, PnL={pnl:F2}");
                    }
                    // Convert to trailing
                    else if (ltfClose <= triggerShort && inTradeShort == -1)
                    {
                        inTradeShort = -2;
                        // Print($"[SHORT TRAILING] Bar {CurrentBar}: Converted to trailing");
                    }
                }
            }

            // ========================================================================
            // EOD CLOSE - Close all positions at end of session
            // Based on PineScript lines 1250-1261, 1323-1334, 1386-1397, 1460-1471
            // ========================================================================
            if (CloseAtEOD && IsEndOfTradingDay())
            {
                // Close long position at EOD
                if (inTradeLong != 0)
                {
                    double exitPrice = ltfClose;
                    double pnl = (exitPrice - entryLong) / divLong;

                    if (pnl >= 0)
                    {
                        historicalLongsPFPFORIT += Math.Abs(pnl);
                        historicalLongsWins++;
                    }
                    else
                    {
                        historicalLongsPFLOSS += Math.Abs(pnl);
                    }

                    // Print($"[LONG EXIT-EOD] Bar {CurrentBar}: Closed at {exitPrice:F2}, PnL={pnl:F2}");
                    inTradeLong = 0;
                }

                // Close short position at EOD
                if (inTradeShort != 0)
                {
                    double exitPrice = ltfClose;
                    double pnl = (entryShort - exitPrice) / divShort;

                    if (pnl >= 0)
                    {
                        historicalShortPFPFORIT += Math.Abs(pnl);
                        historicalShortWins++;
                    }
                    else
                    {
                        historicalShortPFLOSS += Math.Abs(pnl);
                    }

                    // Print($"[SHORT EXIT-EOD] Bar {CurrentBar}: Closed at {exitPrice:F2}, PnL={pnl:F2}");
                    inTradeShort = 0;
                }
            }

            // Update profit factors from real-time trades
            if (historicalLongsTrades > 0)
            {
                double realTimePF = historicalLongsPFPFORIT > 0 ?
                    (historicalLongsPFLOSS > 0 ? historicalLongsPFPFORIT / historicalLongsPFLOSS : historicalLongsPFPFORIT) : 0;
                profitFactor = Math.Max(tablePF, realTimePF); // Use better of optimized or real-time PF
            }
        }

        /// <summary>
        /// Detect if we're at the end of trading day
        /// Based on PineScript session.islastbar_regular
        /// </summary>
        private bool IsEndOfTradingDay()
        {
            // For futures and forex that trade 23-24 hours, check if we're in last 30 minutes before midnight ET (16:00-16:30 for ES)
            // For stocks, check if we're near market close (15:30-16:00 ET)
            // Simple approach: Check if current time is after 15:30 ET or near session end


            DateTime currentTime = Time[0];

            // Check if we're in the last 30 minutes before common session end times
            // This is a simplified approach - adjust based on your instrument's session
            TimeSpan currentTimeOfDay = currentTime.TimeOfDay;

            // Common session end times (in local/exchange time):
            // Stock market: 16:00 (4 PM ET)
            // ES futures: 16:15 (4:15 PM ET for regular session)
            TimeSpan sessionEnd = new TimeSpan(16, 0, 0); // 4:00 PM
            TimeSpan eodWindow = new TimeSpan(0, 30, 0);  // 30 minutes before close

            // Check if we're within 30 minutes of session end
            TimeSpan timeUntilEnd = sessionEnd - currentTimeOfDay;

            return timeUntilEnd >= TimeSpan.Zero && timeUntilEnd <= eodWindow;
        }

        /// <summary>
        /// Select best performing parameters after optimization completes
        /// Based on RadiIQ lastBarOpti() function (PineScript lines 3679-3958)
        /// </summary>
        private void SelectBestParameters()
        {
            if (stringArr.Count == 0) return;

            // Print("[SelectBestParameters] Analyzing 5,400 parameter combinations...");

            // Calculate profit factors for all combinations
            // FIXED: Test ALL combinations like PineScript (removed HTF >= LTF filter)
            List<double> pfLongs = new List<double>();
            List<double> pfShorts = new List<double>();

            for (int i = 0; i < stringArr.Count; i++)
            {
                // CRITICAL FIX: Remove HTF >= LTF validation to match PineScript
                // PineScript tests ALL 2,640 combinations without filtering
                // Previous logic eliminated ~50% of combinations, causing different parameter selection
                bool isValid = true;  // Test all combinations like PineScript

                double pfLong = (isValid && PFprofitArr[i] > 0) ?
                    (PFlossArr[i] > 0 ? PFprofitArr[i] / PFlossArr[i] : PFprofitArr[i]) : 0;
                double pfShort = (isValid && PFprofitArrS[i] > 0) ?
                    (PFlossArrS[i] > 0 ? PFprofitArrS[i] / PFlossArrS[i] : PFprofitArrS[i]) : 0;

                pfLongs.Add(pfLong);
                pfShorts.Add(pfShort);
            }

            // Find best long parameters (among VALID combinations only)
            double maxPFLong = pfLongs.Max();
            bestLongsIndex = pfLongs.IndexOf(maxPFLong);
            tablePF = maxPFLong;

            // CRITICAL: Save optimization PF BEFORE resetting arrays for live trading
            // This is what we display in the performance table
            optimizationPFLong = maxPFLong;

            // Parse best long parameters
            string[] bestLongParams = stringArr[bestLongsIndex].Split('_');
            bestATRLTF = double.Parse(bestLongParams[0]);
            bestATRHTF = double.Parse(bestLongParams[1]);
            bestTarget = double.Parse(bestLongParams[2]);
            bestTrailing = double.Parse(bestLongParams[3]);

            // NOTE: Removed HTF >= LTF validation to match PineScript behavior
            // PineScript allows any combination, including HTF < LTF

            // Use the EXACT optimized parameters - do NOT average!
            // Even if LTF==HTF, the optimization tested different multipliers
            // and we must use exactly what was tested to get the same results
            // Print($"[SelectBestParameters] Using EXACT optimized params: LTF_ATR={bestATRLTF:F2}, HTF_ATR={bestATRHTF:F2}, Target={bestTarget:F1}, Trailing={bestTrailing:F1}");

            // Find best short parameters
            double maxPFShort = pfShorts.Max();
            bestShortsIndex = pfShorts.IndexOf(maxPFShort);
            tablePFS = maxPFShort;

            // CRITICAL: Save optimization PF BEFORE resetting arrays for live trading
            // This is what we display in the performance table
            optimizationPFShort = maxPFShort;

            // Parse best short parameters
            string[] bestShortParams = stringArr[bestShortsIndex].Split('_');
            bestATRshortltf = double.Parse(bestShortParams[0]);
            bestATRshorthtf = double.Parse(bestShortParams[1]);
            bestTargetS = double.Parse(bestShortParams[2]);
            bestTrailingS = double.Parse(bestShortParams[3]);

            // VALIDATION: Verify HTF >= LTF for shorts
            if (bestATRshorthtf < bestATRshortltf)
            {
                // Print($"[SelectBestParameters] WARNING! Invalid SHORT parameters: HTF_ATR ({bestATRshorthtf:F2}) < LTF_ATR ({bestATRshortltf:F2})");
                // Print($"[SelectBestParameters] This violates multi-timeframe principles. HTF should be less sensitive (higher multiplier).");
            }

            // Use the EXACT optimized parameters for shorts - do NOT average!
            // Print($"[SelectBestParameters] Shorts using EXACT params: LTF_ATR={bestATRshortltf:F2}, HTF_ATR={bestATRshorthtf:F2}, Target={bestTargetS:F1}, Trailing={bestTrailingS:F1}");

            // Update performance text
            int longTrades = tradesArr[bestLongsIndex];
            int longWins = winsArr[bestLongsIndex];
            double longWinRate = longTrades > 0 ? (double)longWins / longTrades * 100 : 0;

            int shortTrades = tradesArrS[bestShortsIndex];
            int shortWins = winsArrS[bestShortsIndex];
            double shortWinRate = shortTrades > 0 ? (double)shortWins / shortTrades * 100 : 0;

            profitFactor = tablePF;
            performanceText = $"Impulse IQ (Optimized)\n" +
                             $"Longs PF: {tablePF:F2} WR: {longWinRate:F1}%\n" +
                             $"Shorts PF: {tablePFS:F2} WR: {shortWinRate:F1}%\n" +
                             $"Trades: L{longTrades}/S{shortTrades}\n" +
                             $"BestL: LTF={bestATRLTF:F1} HTF={bestATRHTF:F1}\n" +
                             $"BestS: LTF={bestATRshortltf:F1} HTF={bestATRshorthtf:F1}";

            Print($"[SelectBestParameters] atrArrLTF.Count={atrArrLTF.Count}, atrArrHTF.Count={atrArrHTF.Count}");
            Print($"[SelectBestParameters] BEST LONG: PF={tablePF:F2}, Params={stringArr[bestLongsIndex]}, Trades={longTrades}, Wins={longWins}, WinRate={longWinRate:F1}%");
            Print($"[SelectBestParameters] BEST SHORT: PF={tablePFS:F2}, Params={stringArr[bestShortsIndex]}, Trades={shortTrades}, Wins={shortWins}, WinRate={shortWinRate:F1}%");

            // Show top 5 LONG combinations to understand ranking
            var topLongs = pfLongs.Select((pf, idx) => new { PF = pf, Index = idx, Params = stringArr[idx], Trades = tradesArr[idx] })
                .OrderByDescending(x => x.PF).Take(5).ToList();
            Print($"[LONG TOP 5]:");
            foreach (var item in topLongs)
                Print($"  #{item.Index}: PF={item.PF:F2}, Params={item.Params}, Trades={item.Trades}");

            // Show top 5 SHORT combinations
            var topShorts = pfShorts.Select((pf, idx) => new { PF = pf, Index = idx, Params = stringArr[idx], Trades = tradesArrS[idx] })
                .OrderByDescending(x => x.PF).Take(5).ToList();
            Print($"[SHORT TOP 5]:");
            foreach (var item in topShorts)
                Print($"  #{item.Index}: PF={item.PF:F2}, Params={item.Params}, Trades={item.Trades}");

            // DEBUG: Check 4.0_5.0 specifically (PineScript's best LONG param)
            // Find all indices that start with "4.0_5.0_"
            var param_4_5 = pfLongs.Select((pf, idx) => new { PF = pf, Index = idx, Params = stringArr[idx], Trades = tradesArr[idx], Wins = winsArr[idx] })
                .Where(x => x.Params.StartsWith("4.0_5.0_"))
                .OrderByDescending(x => x.PF)
                .ToList();

            if (param_4_5.Count > 0)
            {
                Print($"[4.0_5.0 ANALYSIS] Found {param_4_5.Count} combinations with LTF=4.0, HTF=5.0:");
                foreach (var item in param_4_5.Take(5))
                {
                    double winRate = item.Trades > 0 ? (double)item.Wins / item.Trades * 100 : 0;
                    Print($"  #{item.Index}: {item.Params}, PF={item.PF:F2}, Trades={item.Trades}, Wins={item.Wins}, WR={winRate:F1}%");
                }
                Print($"[4.0_5.0 ANALYSIS] PineScript expects 4.0_5.0 with PF=15.60 as best LONG. Why is ours different?");
            }

            Print($"[SelectBestParameters] *** COMPARE WITH PINESCRIPT: Are these parameters the same? ***");

            // DETAILED OPTIMIZATION LOGGING
            // Print($"[OPTIMIZATION] Best combo: {stringArr[bestLongsIndex]}, OptimPF={tablePF:F4}, OptimProfit={PFprofitArr[bestLongsIndex]:F2}, OptimLoss={PFlossArr[bestLongsIndex]:F2}, OptimWins={winsArr[bestLongsIndex]}, OptimTrades={tradesArr[bestLongsIndex]}");
        }

        /// <summary>
        /// Replay through ALL historical data using best parameters to reconstruct ZigZag
        /// This is equivalent to PineScript lastBarZZlongLTF/HTF methods (lines 1522-1686, 1687-1807)
        /// </summary>
        private void ReplayHistoryWithBestParameters()
        {
            if (closeArrEnd.Count == 0 || highArrEnd.Count == 0 || lowArrEnd.Count == 0) return;
            if (atrArrLTF.Count == 0 || atrArrHTF.Count == 0) return;

            // Print($"[ReplayHistory] Processing {closeArrEnd.Count} historical bars...");

            // CRITICAL FIX: Clear performance arrays for best parameters before replay
            // This ensures we're calculating fresh results with ONLY the best parameters
            // Print($"[REPLAY START] Clearing optimization results. Using params: {bestATRLTF:F2}_{bestATRHTF:F2}_{bestTarget:F2}_{bestTrailing:F2}");

            // Store old values for comparison (LONG)
            double oldProfit = PFprofitArr[bestLongsIndex];
            double oldLoss = PFlossArr[bestLongsIndex];
            int oldWins = winsArr[bestLongsIndex];
            int oldTrades = tradesArr[bestLongsIndex];

            // Clear arrays for the best LONG combo index
            PFprofitArr[bestLongsIndex] = 0;
            PFlossArr[bestLongsIndex] = 0;
            winsArr[bestLongsIndex] = 0;
            tradesArr[bestLongsIndex] = 0;

            // Store and clear SHORT arrays for bestShortsIndex as well
            double oldProfitS = PFprofitArrS[bestShortsIndex];
            double oldLossS = PFlossArrS[bestShortsIndex];
            int oldWinsS = winsArrS[bestShortsIndex];
            int oldTradesS = tradesArrS[bestShortsIndex];

            PFprofitArrS[bestShortsIndex] = 0;
            PFlossArrS[bestShortsIndex] = 0;
            winsArrS[bestShortsIndex] = 0;
            tradesArrS[bestShortsIndex] = 0;

            // Print($"[REPLAY START] Cleared - OldProfit={oldProfit:F2}, OldLoss={oldLoss:F2}, OldWins={oldWins}, OldTrades={oldTrades}");
            // Print($"[REPLAY START] Cleared SHORTS - OldProfit={oldProfitS:F2}, OldLoss={oldLossS:F2}, OldWins={oldWinsS}, OldTrades={oldTradesS}");

            // Clear existing ZigZag states and start fresh
            zzLTF_Long = new ZigZagState();
            zzHTF_Long = new ZigZagState();
            zzLTF_Long.Direction = 0;
            zzHTF_Long.Direction = 0;

            // Initialize tracking variables for LTF
            double ltfPoint = closeArrEnd[0];
            DateTime ltfTimeP = timeArrEnd[0];
            int ltfDir = 0;
            double ltfY1 = ltfPoint;
            double ltfY2 = ltfPoint;
            DateTime ltfY1Time = ltfTimeP;
            DateTime ltfY2Time = ltfTimeP;

            // Initialize tracking variables for HTF
            double htfPoint = closeArrEnd[0];
            DateTime htfTimeP = timeArrEnd[0];
            int htfDir = 0;
            double htfY1 = htfPoint;
            double htfY2 = htfPoint;
            DateTime htfY1Time = htfTimeP;
            DateTime htfY2Time = htfTimeP;

            // Prepare per-bar state arrays to capture HTF/LTF pivots & breakout points during replay
            var replayHtfDir = new List<int>(closeArrEnd.Count);
            var replayHtfY1 = new List<double>(closeArrEnd.Count);
            var replayHtfY2 = new List<double>(closeArrEnd.Count);
            var replayLtfBreakoutUp = new List<double>(closeArrEnd.Count);
            var replayLtfBreakoutDn = new List<double>(closeArrEnd.Count);

            // SHORT-specific replay arrays (calculated with SHORT ATR parameters)
            var replayShortHtfDir = new List<int>(closeArrEnd.Count);
            var replayShortHtfY1 = new List<double>(closeArrEnd.Count);
            var replayShortHtfY2 = new List<double>(closeArrEnd.Count);
            var replayShortLtfBreakoutDn = new List<double>(closeArrEnd.Count);

            // Replay through ALL historical bars
            for (int i = 0; i < Math.Min(ltfCloArr.Count, highArrEnd.Count); i++)
            {
                // Get bar data (primary bars for both zigzags)
                double getHigh = highArrEnd[i];
                double getLow = lowArrEnd[i];
                DateTime getTime = timeArrEnd[Math.Min(i, timeArrEnd.Count - 1)];

                // Get ATR values
                double rawLtfATR = atrArrLTF[Math.Min(i, atrArrLTF.Count - 1)];
                double rawHtfATR = atrArrHTF[Math.Min(i, atrArrHTF.Count - 1)];
                double ltfATR = rawLtfATR * bestATRLTF;
                double htfATR = rawHtfATR * bestATRHTF;
                // Get ATR values - BOTH should use PRIMARY ATR from arrays


                // Debug first few bars to verify ATR values and data synchronization
                if (i < 10 || i == 50 || i == 100)
                {
                    // Print($"[REPLAY BAR {i}] Time={getTime:HH:mm:ss}");
                    // Print($"  OHLC: H={getHigh:F2}, L={getLow:F2}, C={closeArrEnd[i]:F2}, O={openArrEnd[i]:F2}");
                    Print($"  ATR: RawLTF={rawLtfATR:F2}, RawHTF={rawHtfATR:F2}");
                    Print($"  Multiplied: LTF={ltfATR:F2} (raw*{bestATRLTF:F2}), HTF={htfATR:F2} (raw*{bestATRHTF:F2})");
                    // Print($"  LTF State: Dir={ltfDir}, Point={ltfPoint:F2}, Y1={ltfY1:F2}, Y2={ltfY2:F2}");
                    // Print($"  HTF State: Dir={htfDir}, Point={htfPoint:F2}, Y1={htfY1:F2}, Y2={htfY2:F2}");
                }

                // ===== UPDATE LTF ZIGZAG =====
                if (ltfDir == 1) // Uptrend
                {
                    ltfPoint = Math.Max(ltfPoint, getHigh);
                    ltfY2 = ltfPoint;
                    if (getHigh == ltfPoint)
                        ltfY2Time = getTime;

                    // Check for reversal with buffer (matches PineScript)
                    double addition = Math.Abs(ltfPoint - ltfY1) * Buffer;
                    if (getLow <= ltfPoint - ltfATR - addition)
                    {
                        // Add line from previous pivot to new pivot
                        AddZigZagLine(zzLTF, ltfY1Time, ltfY1, ltfY2Time, ltfY2, false);

                        // Set breakout level when pivot is confirmed
                        // PineScript: if getFirstY1 > getFirstY2, store Y1 as breakout level
                        // At this moment: ltfY2 is the confirmed HIGH pivot (about to become ltfY1)
                        // Only update if it's a NEW level (matches PineScript validation)
                        if (Math.Abs(zzLTF.BreakoutPointUp - ltfY2) > TickSize && ltfY2 > 0)
                        {
                            zzLTF.BreakoutPointUp = ltfY2;
                            zzLTF.BreakoutPointUpTime = ltfY2Time; // Save time when level was established
                        }

                        // Store previous Y1/Y2 to history before updating (for PineScript [1] operator)
                        zzLTF.Y1PriceHistory.Add(ltfY1);
                        zzLTF.Y2PriceHistory.Add(ltfY2);
                        zzLTF.Y1TimeHistory.Add(ltfY1Time);
                        zzLTF.Y2TimeHistory.Add(ltfY2Time);

                        ltfY1 = ltfPoint;
                        ltfY1Time = ltfY2Time;

                        // Add pivot price line at new Y1 (downtrend pivot)
                        AddPivotPriceLine(zzLTF, ltfY1Time, ltfY1, null, isDownPivot: true);
                        ltfY2 = getLow;
                        ltfY2Time = getTime;
                        ltfDir = -1;
                        ltfPoint = getLow;
                    }
                }
                else if (ltfDir == -1) // Downtrend
                {
                    ltfPoint = Math.Min(getLow, ltfPoint);
                    ltfY2 = ltfPoint;
                    if (getLow == ltfPoint)
                        ltfY2Time = getTime;

                    // Check for reversal with buffer (matches PineScript)
                    double addition = Math.Abs(ltfPoint - ltfY1) * Buffer;
                    if (getHigh >= ltfPoint + ltfATR + addition)
                    {
                        // Add line from previous pivot to new pivot
                        AddZigZagLine(zzLTF, ltfY1Time, ltfY1, ltfY2Time, ltfY2, false);

                        // Set breakout level when pivot is confirmed
                        // At this moment: ltfY2 is the confirmed LOW pivot (about to become ltfY1)
                        // Only update if it's a NEW level (matches PineScript validation)
                        if (Math.Abs(zzLTF.BreakoutPointDn - ltfY2) > TickSize && ltfY2 < 20e20)
                        {
                            // DEBUG: Log first 5 breakdown level updates with full context
                            if (zzLTF.BreakoutLines.Count(bl => !bl.IsLongBreakout) < 5)
                            {
                                // Print($"[BREAKDOWN LEVEL UPDATE #{zzLTF.BreakoutLines.Count(bl => !bl.IsLongBreakout) + 1}] Bar {i}, Time={ltfY2Time:HH:mm:ss}");
                                // Print($"  OLD BreakoutDn: {zzLTF.BreakoutPointDn:F2}, NEW BreakoutDn: {ltfY2:F2}");
                                // Print($"  LTF Pivot: Y1={ltfY1:F2} (prev high), Y2={ltfY2:F2} (new low)");
                                // Print($"  Current bar: H={getHigh:F2}, L={getLow:F2}, C={closeArrEnd[i]:F2}");
                                // Print($"  Reversal: Low {ltfY2:F2} + ATR {ltfATR:F2} + Buffer {Math.Abs(ltfPoint - ltfY1) * Buffer:F2} = {ltfY2 + ltfATR + Math.Abs(ltfPoint - ltfY1) * Buffer:F2} <= High {getHigh:F2}");
                            }
                            zzLTF.BreakoutPointDn = ltfY2;
                            zzLTF.BreakoutPointDnTime = ltfY2Time; // Save time when level was established
                        }

                        // Store previous Y1/Y2 to history before updating (for PineScript [1] operator)
                        zzLTF.Y1PriceHistory.Add(ltfY1);
                        zzLTF.Y2PriceHistory.Add(ltfY2);
                        zzLTF.Y1TimeHistory.Add(ltfY1Time);
                        zzLTF.Y2TimeHistory.Add(ltfY2Time);

                        ltfY1 = ltfPoint;
                        ltfY1Time = ltfY2Time;

                        // Add pivot price line at new Y1 (uptrend pivot)
                        AddPivotPriceLine(zzLTF, ltfY1Time, ltfY1, null, isDownPivot: false);
                        ltfY2 = getHigh;
                        ltfY2Time = getTime;
                        ltfDir = 1;
                        ltfPoint = getHigh;
                    }
                }
                else // Initial state (dir == 0)
                {
                    if (getHigh >= ltfPoint + ltfATR)
                    {
                        ltfY1 = ltfPoint;
                        ltfY1Time = ltfTimeP;
                        ltfY2 = getHigh;
                        ltfY2Time = getTime;
                        ltfDir = 1;
                        ltfPoint = getHigh;
                    }
                    else if (getLow <= ltfPoint - ltfATR)
                    {
                        ltfY1 = ltfPoint;
                        ltfY1Time = ltfTimeP;
                        ltfY2 = getLow;
                        ltfY2Time = getTime;
                        ltfDir = -1;
                        ltfPoint = getLow;
                    }
                }

                // ===== UPDATE HTF ZIGZAG =====
                if (htfDir == 1) // Uptrend
                {
                    htfPoint = Math.Max(htfPoint, getHigh);
                    htfY2 = htfPoint;
                    if (getHigh == htfPoint)
                        htfY2Time = getTime;

                    // Check for reversal with buffer (matches PineScript)
                    double additionHtf = Math.Abs(htfPoint - htfY1) * Buffer;
                    if (getLow <= htfPoint - htfATR - additionHtf)
                    {
                        // Add line from previous pivot to new pivot
                        AddZigZagLine(zzHTF, htfY1Time, htfY1, htfY2Time, htfY2, false);

                        // Set breakout level when pivot is confirmed (with validation)
                        if (Math.Abs(zzHTF.BreakoutPointUp - htfY2) > TickSize && htfY2 > 0)
                        {
                            zzHTF.BreakoutPointUp = htfY2;
                            zzHTF.BreakoutPointUpTime = htfY2Time;
                        }

                        // Store previous Y1/Y2 to history before updating (for PineScript [1] operator)
                        zzHTF.Y1PriceHistory.Add(htfY1);
                        zzHTF.Y2PriceHistory.Add(htfY2);
                        zzHTF.Y1TimeHistory.Add(htfY1Time);
                        zzHTF.Y2TimeHistory.Add(htfY2Time);

                        htfY1 = htfPoint;
                        htfY1Time = htfY2Time;

                        // Add pivot price line at new Y1 (downtrend pivot)
                        AddPivotPriceLine(zzHTF, htfY1Time, htfY1, null, isDownPivot: true);
                        htfY2 = getLow;
                        htfY2Time = getTime;
                        htfDir = -1;
                        htfPoint = getLow;
                    }
                }
                else if (htfDir == -1) // Downtrend
                {
                    htfPoint = Math.Min(getLow, htfPoint);
                    htfY2 = htfPoint;
                    if (getLow == htfPoint)
                        htfY2Time = getTime;

                    // Check for reversal with buffer (matches PineScript)
                    double additionHtf = Math.Abs(htfPoint - htfY1) * Buffer;
                    if (getHigh >= htfPoint + htfATR + additionHtf)
                    {
                        // Add line from previous pivot to new pivot
                        AddZigZagLine(zzHTF, htfY1Time, htfY1, htfY2Time, htfY2, false);

                        // Set breakout level when pivot is confirmed (with validation)
                        if (Math.Abs(zzHTF.BreakoutPointDn - htfY2) > TickSize && htfY2 < 20e20)
                        {
                            zzHTF.BreakoutPointDn = htfY2;
                            zzHTF.BreakoutPointDnTime = htfY2Time;
                        }

                        // Store previous Y1/Y2 to history before updating (for PineScript [1] operator)
                        zzHTF.Y1PriceHistory.Add(htfY1);
                        zzHTF.Y2PriceHistory.Add(htfY2);
                        zzHTF.Y1TimeHistory.Add(htfY1Time);
                        zzHTF.Y2TimeHistory.Add(htfY2Time);

                        htfY1 = htfPoint;
                        htfY1Time = htfY2Time;

                        // Add pivot price line at new Y1 (uptrend pivot)
                        AddPivotPriceLine(zzHTF, htfY1Time, htfY1, null, isDownPivot: false);
                        htfY2 = getHigh;
                        htfY2Time = getTime;
                        htfDir = 1;
                        htfPoint = getHigh;
                    }
                }
                else // Initial state (dir == 0)
                {
                    if (getHigh >= htfPoint + htfATR)
                    {
                        htfY1 = htfPoint;
                        htfY1Time = htfTimeP;
                        htfY2 = getHigh;
                        htfY2Time = getTime;
                        htfDir = 1;
                        htfPoint = getHigh;
                    }
                    else if (getLow <= htfPoint - htfATR)
                    {
                        htfY1 = htfPoint;
                        htfY1Time = htfTimeP;
                        htfY2 = getLow;
                        htfY2Time = getTime;
                        htfDir = -1;
                        htfPoint = getLow;
                    }
                }

                // CRITICAL: Detect breakout crossovers during replay (matching RadiIQ lines 1662-1682, 1938-1957)
                if (i > 0)
                {
                    double currentClose = closeArrEnd[i];
                    double previousClose = closeArrEnd[i - 1];

                    // Check for LONG breakout (price crosses above BreakoutPointUp)
                    if (zzLTF.BreakoutPointUp > 0 &&
                        currentClose > zzLTF.BreakoutPointUp &&
                        previousClose <= zzLTF.BreakoutPointUp)
                    {
                        // Only create line if not a duplicate (prevents multiple lines at same price level)
                        if (!IsDuplicateReplay(zzLTF, zzLTF.BreakoutPointUp, true, getTime, zzLTF.BreakoutPointUpTime))
                        {
                            var breakoutLine = new BreakoutLine
                            {
                                X1 = zzLTF.BreakoutPointUpTime,  // Use stored time from when breakout level was SET
                                X2 = getTime,    // End at breakout time
                                Y = zzLTF.BreakoutPointUp,
                                IsActive = false,  // STATIC - never extend
                                IsLongBreakout = true
                            };
                            zzLTF.BreakoutLines.Add(breakoutLine);
                        }
                    }

                    // Check for SHORT breakout (price crosses below BreakoutPointDn)
                    if (zzLTF.BreakoutPointDn < 20e20 &&
                        currentClose < zzLTF.BreakoutPointDn &&
                        previousClose >= zzLTF.BreakoutPointDn)
                    {
                        // DEBUG: Log first 5 breakdown line creations
                        if (zzLTF.BreakoutLines.Count(bl => !bl.IsLongBreakout) < 5)
                        {
                            // Print($"[BREAKDOWN LINE #{zzLTF.BreakoutLines.Count(bl => !bl.IsLongBreakout) + 1}] Bar {i}");
                            // Print($"  X1={zzLTF.BreakoutPointDnTime:HH:mm:ss}, X2={getTime:HH:mm:ss}, Y={zzLTF.BreakoutPointDn:F2}");
                            // Print($"  PrevClose={previousClose:F2}, CurrentClose={currentClose:F2}");
                            // Print($"  IsDuplicate={IsDuplicateReplay(zzLTF, zzLTF.BreakoutPointDn, false, getTime, zzLTF.BreakoutPointDnTime)}");
                        }

                        // Only create line if not a duplicate (prevents multiple lines at same price level)
                        if (!IsDuplicateReplay(zzLTF, zzLTF.BreakoutPointDn, false, getTime, zzLTF.BreakoutPointDnTime))
                        {
                            var breakoutLine = new BreakoutLine
                            {
                                X1 = zzLTF.BreakoutPointDnTime,  // Use stored time from when breakout level was SET
                                X2 = getTime,    // End at breakout time
                                Y = zzLTF.BreakoutPointDn,
                                IsActive = false,  // STATIC - never extend
                                IsLongBreakout = false
                            };
                            zzLTF.BreakoutLines.Add(breakoutLine);
                        }
                    }

                    // HTF breakout detection
                    if (zzHTF.BreakoutPointUp > 0 &&
                        currentClose > zzHTF.BreakoutPointUp &&
                        previousClose <= zzHTF.BreakoutPointUp)
                    {
                        // Only create line if not a duplicate (prevents multiple lines at same price level)
                        if (!IsDuplicateReplay(zzHTF, zzHTF.BreakoutPointUp, true, getTime, zzHTF.BreakoutPointUpTime))
                        {
                            var breakoutLine = new BreakoutLine
                            {
                                X1 = zzHTF.BreakoutPointUpTime,  // Use stored time from when breakout level was SET
                                X2 = getTime,
                                Y = zzHTF.BreakoutPointUp,
                                IsActive = false,  // STATIC - never extend
                                IsLongBreakout = true
                            };
                            zzHTF.BreakoutLines.Add(breakoutLine);
                        }
                    }

                    if (zzHTF.BreakoutPointDn < 20e20 &&
                        currentClose < zzHTF.BreakoutPointDn &&
                        previousClose >= zzHTF.BreakoutPointDn)
                    {
                        // Only create line if not a duplicate (prevents multiple lines at same price level)
                        if (!IsDuplicateReplay(zzHTF, zzHTF.BreakoutPointDn, false, getTime, zzHTF.BreakoutPointDnTime))
                        {
                            var breakoutLine = new BreakoutLine
                            {
                                X1 = zzHTF.BreakoutPointDnTime,  // Use stored time from when breakout level was SET
                                X2 = getTime,
                                Y = zzHTF.BreakoutPointDn,
                                IsActive = false,  // STATIC - never extend
                                IsLongBreakout = false
                            };
                            zzHTF.BreakoutLines.Add(breakoutLine);
                        }
                    }
                }

                // Collect per-bar HTF/LTF state for accurate replay testing
                replayHtfDir.Add(htfDir);
                replayHtfY1.Add(htfY1);
                replayHtfY2.Add(htfY2);
                replayLtfBreakoutUp.Add(zzLTF.BreakoutPointUp);
                replayLtfBreakoutDn.Add(zzLTF.BreakoutPointDn);
            }

            // ========== SHORT ZIGZAG CALCULATION (SEPARATE WITH SHORT ATR PARAMETERS) ==========
            // Initialize SHORT-specific tracking variables for LTF
            double shortLtfPoint = closeArrEnd[0];
            DateTime shortLtfTimeP = timeArrEnd[0];
            int shortLtfDir = 0;
            double shortLtfY1 = shortLtfPoint;
            double shortLtfY2 = shortLtfPoint;
            DateTime shortLtfY1Time = shortLtfTimeP;
            DateTime shortLtfY2Time = shortLtfTimeP;

            // Initialize SHORT-specific tracking variables for HTF
            double shortHtfPoint = closeArrEnd[0];
            DateTime shortHtfTimeP = timeArrEnd[0];
            int shortHtfDir = 0;
            double shortHtfY1 = shortHtfPoint;
            double shortHtfY2 = shortHtfPoint;
            DateTime shortHtfY1Time = shortHtfTimeP;
            DateTime shortHtfY2Time = shortHtfTimeP;

            // Create separate SHORT ZigZag state objects
            ZigZagState zzShortLTF = new ZigZagState();
            ZigZagState zzShortHTF = new ZigZagState();
            zzShortLTF.Direction = 0;
            zzShortHTF.Direction = 0;

            // Initialize SHORT breakout points with sentinel values
            zzShortLTF.BreakoutPointDn = 20e20;
            zzShortHTF.BreakoutPointDn = 20e20;

            // Print($"[SHORT ZIGZAG START] Using SHORT ATR multipliers: LTF={bestATRshortltf:F2}, HTF={bestATRshorthtf:F2}");

            // Replay through ALL historical bars with SHORT parameters
            for (int i = 0; i < Math.Min(ltfCloArr.Count, highArrEnd.Count); i++)
            {
                // Get bar data (primary bars for both zigzags)
                double getHigh = highArrEnd[i];
                double getLow = lowArrEnd[i];
                DateTime getTime = timeArrEnd[Math.Min(i, timeArrEnd.Count - 1)];

                // Get ATR values with SHORT multipliers
                double rawLtfATR = atrArrLTF[Math.Min(i, atrArrLTF.Count - 1)];
                double rawHtfATR = atrArrHTF[Math.Min(i, atrArrHTF.Count - 1)];
                double shortLtfATR = rawLtfATR * bestATRshortltf;  // SHORT parameter
                double shortHtfATR = rawHtfATR * bestATRshorthtf;  // SHORT parameter

                // Debug first few bars to verify SHORT ATR values
                if (i < 10 || i == 50 || i == 100)
                {
                    // Print($"[SHORT REPLAY BAR {i}] Time={getTime:HH:mm:ss}");
                    // Print($"  OHLC: H={getHigh:F2}, L={getLow:F2}, C={closeArrEnd[i]:F2}, O={openArrEnd[i]:F2}");
                    Print($"  ATR: RawLTF={rawLtfATR:F2}, RawHTF={rawHtfATR:F2}");
                    Print($"  SHORT Multiplied: LTF={shortLtfATR:F2} (raw*{bestATRshortltf:F2}), HTF={shortHtfATR:F2} (raw*{bestATRshorthtf:F2})");
                    // Print($"  SHORT LTF State: Dir={shortLtfDir}, Point={shortLtfPoint:F2}, Y1={shortLtfY1:F2}, Y2={shortLtfY2:F2}");
                    // Print($"  SHORT HTF State: Dir={shortHtfDir}, Point={shortHtfPoint:F2}, Y1={shortHtfY1:F2}, Y2={shortHtfY2:F2}");
                }

                // ===== UPDATE SHORT LTF ZIGZAG =====
                if (shortLtfDir == 1) // Uptrend
                {
                    shortLtfPoint = Math.Max(shortLtfPoint, getHigh);
                    shortLtfY2 = shortLtfPoint;
                    if (getHigh == shortLtfPoint)
                        shortLtfY2Time = getTime;

                    // Check for reversal with buffer (matches PineScript)
                    double addition = Math.Abs(shortLtfPoint - shortLtfY1) * Buffer;
                    if (getLow <= shortLtfPoint - shortLtfATR - addition)
                    {
                        // Store previous Y1/Y2 to history before updating
                        zzShortLTF.Y1PriceHistory.Add(shortLtfY1);
                        zzShortLTF.Y2PriceHistory.Add(shortLtfY2);
                        zzShortLTF.Y1TimeHistory.Add(shortLtfY1Time);
                        zzShortLTF.Y2TimeHistory.Add(shortLtfY2Time);

                        shortLtfY1 = shortLtfPoint;
                        shortLtfY1Time = shortLtfY2Time;
                        shortLtfY2 = getLow;
                        shortLtfY2Time = getTime;
                        shortLtfDir = -1;
                        shortLtfPoint = getLow;
                    }
                }
                else if (shortLtfDir == -1) // Downtrend
                {
                    shortLtfPoint = Math.Min(getLow, shortLtfPoint);
                    shortLtfY2 = shortLtfPoint;
                    if (getLow == shortLtfPoint)
                        shortLtfY2Time = getTime;

                    // Check for reversal with buffer (matches PineScript)
                    double addition = Math.Abs(shortLtfPoint - shortLtfY1) * Buffer;
                    if (getHigh >= shortLtfPoint + shortLtfATR + addition)
                    {
                        // Set SHORT breakout level when pivot is confirmed
                        if (Math.Abs(zzShortLTF.BreakoutPointDn - shortLtfY2) > TickSize && shortLtfY2 < 20e20)
                        {
                            zzShortLTF.BreakoutPointDn = shortLtfY2;
                            zzShortLTF.BreakoutPointDnTime = shortLtfY2Time;
                        }

                        // Store previous Y1/Y2 to history before updating
                        zzShortLTF.Y1PriceHistory.Add(shortLtfY1);
                        zzShortLTF.Y2PriceHistory.Add(shortLtfY2);
                        zzShortLTF.Y1TimeHistory.Add(shortLtfY1Time);
                        zzShortLTF.Y2TimeHistory.Add(shortLtfY2Time);

                        shortLtfY1 = shortLtfPoint;
                        shortLtfY1Time = shortLtfY2Time;
                        shortLtfY2 = getHigh;
                        shortLtfY2Time = getTime;
                        shortLtfDir = 1;
                        shortLtfPoint = getHigh;
                    }
                }
                else // Initial state (dir == 0)
                {
                    if (getHigh >= shortLtfPoint + shortLtfATR)
                    {
                        shortLtfY1 = shortLtfPoint;
                        shortLtfY1Time = shortLtfTimeP;
                        shortLtfY2 = getHigh;
                        shortLtfY2Time = getTime;
                        shortLtfDir = 1;
                        shortLtfPoint = getHigh;
                    }
                    else if (getLow <= shortLtfPoint - shortLtfATR)
                    {
                        shortLtfY1 = shortLtfPoint;
                        shortLtfY1Time = shortLtfTimeP;
                        shortLtfY2 = getLow;
                        shortLtfY2Time = getTime;
                        shortLtfDir = -1;
                        shortLtfPoint = getLow;
                    }
                }

                // ===== UPDATE SHORT HTF ZIGZAG =====
                if (shortHtfDir == 1) // Uptrend
                {
                    shortHtfPoint = Math.Max(shortHtfPoint, getHigh);
                    shortHtfY2 = shortHtfPoint;
                    if (getHigh == shortHtfPoint)
                        shortHtfY2Time = getTime;

                    // Check for reversal with buffer (matches PineScript)
                    double additionHtf = Math.Abs(shortHtfPoint - shortHtfY1) * Buffer;
                    if (getLow <= shortHtfPoint - shortHtfATR - additionHtf)
                    {
                        // Store previous Y1/Y2 to history before updating
                        zzShortHTF.Y1PriceHistory.Add(shortHtfY1);
                        zzShortHTF.Y2PriceHistory.Add(shortHtfY2);
                        zzShortHTF.Y1TimeHistory.Add(shortHtfY1Time);
                        zzShortHTF.Y2TimeHistory.Add(shortHtfY2Time);

                        shortHtfY1 = shortHtfPoint;
                        shortHtfY1Time = shortHtfY2Time;
                        shortHtfY2 = getLow;
                        shortHtfY2Time = getTime;
                        shortHtfDir = -1;
                        shortHtfPoint = getLow;
                    }
                }
                else if (shortHtfDir == -1) // Downtrend
                {
                    shortHtfPoint = Math.Min(getLow, shortHtfPoint);
                    shortHtfY2 = shortHtfPoint;
                    if (getLow == shortHtfPoint)
                        shortHtfY2Time = getTime;

                    // Check for reversal with buffer (matches PineScript)
                    double additionHtf = Math.Abs(shortHtfPoint - shortHtfY1) * Buffer;
                    if (getHigh >= shortHtfPoint + shortHtfATR + additionHtf)
                    {
                        // Set SHORT breakout level when pivot is confirmed
                        if (Math.Abs(zzShortHTF.BreakoutPointDn - shortHtfY2) > TickSize && shortHtfY2 < 20e20)
                        {
                            zzShortHTF.BreakoutPointDn = shortHtfY2;
                            zzShortHTF.BreakoutPointDnTime = shortHtfY2Time;
                        }

                        // Store previous Y1/Y2 to history before updating
                        zzShortHTF.Y1PriceHistory.Add(shortHtfY1);
                        zzShortHTF.Y2PriceHistory.Add(shortHtfY2);
                        zzShortHTF.Y1TimeHistory.Add(shortHtfY1Time);
                        zzShortHTF.Y2TimeHistory.Add(shortHtfY2Time);

                        shortHtfY1 = shortHtfPoint;
                        shortHtfY1Time = shortHtfY2Time;
                        shortHtfY2 = getHigh;
                        shortHtfY2Time = getTime;
                        shortHtfDir = 1;
                        shortHtfPoint = getHigh;
                    }
                }
                else // Initial state (dir == 0)
                {
                    if (getHigh >= shortHtfPoint + shortHtfATR)
                    {
                        shortHtfY1 = shortHtfPoint;
                        shortHtfY1Time = shortHtfTimeP;
                        shortHtfY2 = getHigh;
                        shortHtfY2Time = getTime;
                        shortHtfDir = 1;
                        shortHtfPoint = getHigh;
                    }
                    else if (getLow <= shortHtfPoint - shortHtfATR)
                    {
                        shortHtfY1 = shortHtfPoint;
                        shortHtfY1Time = shortHtfTimeP;
                        shortHtfY2 = getLow;
                        shortHtfY2Time = getTime;
                        shortHtfDir = -1;
                        shortHtfPoint = getLow;
                    }
                }

                // Collect SHORT per-bar HTF/LTF state for accurate replay testing
                replayShortHtfDir.Add(shortHtfDir);
                replayShortHtfY1.Add(shortHtfY1);
                replayShortHtfY2.Add(shortHtfY2);
                replayShortLtfBreakoutDn.Add(zzShortLTF.BreakoutPointDn);
            }

            // Print($"[SHORT ZIGZAG COMPLETE] Final SHORT LTF BreakoutDn: {zzShortLTF.BreakoutPointDn:F2}");
            // Print($"[SHORT ZIGZAG COMPLETE] Final SHORT HTF Y1: {shortHtfY1:F2}, Y2: {shortHtfY2:F2}");

            // Store final state
            zzLTF.Y1Price = ltfY1;
            zzLTF.Y2Price = ltfY2;
            zzLTF.Y1Time = ltfY1Time;
            zzLTF.Y2Time = ltfY2Time;
            zzLTF.Direction = ltfDir;
            zzLTF.Point = ltfPoint;
            zzLTF.TimeP = ltfTimeP;

            zzHTF.Y1Price = htfY1;
            zzHTF.Y2Price = htfY2;
            zzHTF.Y1Time = htfY1Time;
            zzHTF.Y2Time = htfY2Time;
            zzHTF.Direction = htfDir;
            zzHTF.Point = htfPoint;
            zzHTF.TimeP = htfTimeP;

            Print($"[ReplayHistory] Complete! LTF: {zzLTF.Lines.Count} lines, HTF: {zzHTF.Lines.Count} lines");
            Print($"[ReplayHistory] LTF Breakout Lines: {zzLTF.BreakoutLines.Count}, HTF Breakout Lines: {zzHTF.BreakoutLines.Count}");
            Print($"[ReplayHistory] LTF==HTF? {ltfBarsInProgress == htfBarsInProgress}, bestATRLTF={bestATRLTF:F4}, bestATRHTF={bestATRHTF:F4}, Same? {bestATRLTF == bestATRHTF}");
            Print($"[ReplayHistory] atrArrLTF.Count={atrArrLTF.Count}, atrArrHTF.Count={atrArrHTF.Count}, Same size? {atrArrLTF.Count == atrArrHTF.Count}");
            Print($"[ReplayHistory] LTF Breakout Up: {zzLTF.BreakoutPointUp:F2}, Dn: {zzLTF.BreakoutPointDn:F2}");
            Print($"[ReplayHistory] HTF Breakout Up: {zzHTF.BreakoutPointUp:F2}, Dn: {zzHTF.BreakoutPointDn:F2}");

            // CRITICAL FIX: Now re-test ALL trades using ONLY the best parameters
            // This is what PineScript does - it rebuilds the ZigZag AND re-tests trades
            // Print($"[REPLAY] Re-testing trades with best parameters only...");
            ReplayTradesWithBestParameters(replayHtfDir, replayHtfY1, replayHtfY2, replayLtfBreakoutUp, replayLtfBreakoutDn,
                                          replayShortHtfY1, replayShortHtfY2, replayShortLtfBreakoutDn);

            // Update profit factor with replay results
            double replayProfit = PFprofitArr[bestLongsIndex];
            double replayLoss = PFlossArr[bestLongsIndex];
            int replayWins = winsArr[bestLongsIndex];
            int replayTrades = tradesArr[bestLongsIndex];
            double replayPF = replayProfit > 0 ? (replayLoss > 0 ? replayProfit / replayLoss : replayProfit) : 0;
            double replayWinRate = replayTrades > 0 ? (double)replayWins / replayTrades * 100 : 0;

            // Print($"[REPLAY COMPLETE] NewProfit={replayProfit:F2}, NewLoss={replayLoss:F2}, NewWins={replayWins}, NewTrades={replayTrades}");
            // Print($"[REPLAY COMPLETE] NewPF={replayPF:F4}, NewWinRate={replayWinRate:F1}%");

            // Update displayed metrics
            tablePF = replayPF;
            profitFactor = replayPF;

            // Get SHORT replay metrics too
            int shortTrades = tradesArrS[bestShortsIndex];
            int shortWins = winsArrS[bestShortsIndex];
            double shortWinRate = shortTrades > 0 ? (double)shortWins / shortTrades * 100 : 0;
            double shortProfit = PFprofitArrS[bestShortsIndex];
            double shortLoss = PFlossArrS[bestShortsIndex];
            double shortPF = shortProfit > 0 ? (shortLoss > 0 ? shortProfit / shortLoss : shortProfit) : 0;

            performanceText = $"Impulse IQ (Optimized)\n" +
                             $"Longs PF: {replayPF:F2} WR: {replayWinRate:F1}%\n" +
                             $"Shorts PF: {shortPF:F2} WR: {shortWinRate:F1}%\n" +
                             $"Trades: L{replayTrades}/S{shortTrades}\n" +
                             $"BestL: LTF={bestATRLTF:F1} HTF={bestATRHTF:F1}\n" +
                             $"BestS: LTF={bestATRshortltf:F1} HTF={bestATRshorthtf:F1}";
        }

        /// <summary>
        /// Re-test all trades through history using ONLY the best parameters
        /// This mimics what PineScript does after selecting best parameters
        ///
        /// NOTE: This is a simplified implementation that uses final HTF ZigZag state
        /// for all historical bars. For perfect accuracy, we'd need to track HTF state
        /// bar-by-bar during the loop. However, this provides a reasonable approximation
        /// and is MUCH better than showing optimization results (which test all 5400 combos).
        /// </summary>
        private void ReplayTradesWithBestParameters(List<int> replayHtfDir, List<double> replayHtfY1, List<double> replayHtfY2, List<double> replayLtfBreakoutUp, List<double> replayLtfBreakoutDn,
                                                    List<double> replayShortHtfY1, List<double> replayShortHtfY2, List<double> replayShortLtfBreakoutDn)
        {
            if (closeArrEnd.Count == 0) return;
            // Validate passed arrays length
            int n = Math.Min(closeArrEnd.Count, replayHtfDir != null ? replayHtfDir.Count : 0);
            if (n <= 1)
                return;

            // Parse best parameters
            double ltfATRmult = bestATRLTF;
            double htfATRmult = bestATRHTF;
            double targetMult = bestTarget;
            double trailingMult = bestTrailing;

            // Initialize trade state for best combo
            int tradeLongState = 0;
            double entryLong = 0;
            double exitLong = 0;
            double triggerLong = 0;
            int div = 1;
            double rrTarget = 0;

            // Debug counters
            int debugCheckCount = 0;
            int debugBreakoutUpZero = 0;
            int debugWrongTrend = 0;
            int debugNoBreakout = 0;
            int debugRangeFail = 0;

            // Loop through all historical bars and test trades using the per-bar HTF data
            for (int i = 1; i < n; i++)
            {
                double ltfClose = closeArrEnd[i];
                double ltfClosePrev = closeArrEnd[i - 1];
                double ltfHigh = highArrEnd[i];
                double ltfLow = lowArrEnd[i];
                double ltfOpen = openArrEnd[Math.Min(i, openArrEnd.Count - 1)];

                // Get RAW ATR (without multipliers) for stop/target calculations
                double rawLtfATR = atrArrLTF[Math.Min(i, atrArrLTF.Count - 1)];
                double rawHtfATR = atrArrHTF[Math.Min(i, atrArrHTF.Count - 1)];

                // Applied ATR for zigzag calculations (with ltfATRmult/htfATRmult)
                double ltfATR = rawLtfATR * ltfATRmult;
                double htfATR = rawHtfATR * htfATRmult;

                // Use per-bar HTF/LTF state collected during ReplayHistoryWithBestParameters
                double htfY1 = replayHtfY1[i];
                double htfY2 = replayHtfY2[i];
                int htfDir = replayHtfDir[i];

                // CRITICAL: Use PREVIOUS bar's breakout level (Pine Script line 2409: get(I-1))
                // This ensures we don't enter on the same bar that creates the breakout level
                double breakoutUp = i > 0 ? replayLtfBreakoutUp[i - 1] : 0;
                double breakoutDn = i > 0 ? replayLtfBreakoutDn[i - 1] : double.MaxValue;

                // Calculate range
                double htfRange = Math.Abs(htfY2 - htfY1) * (BuySellRange / 100.0);

                // CRITICAL FIX: Use Y2 > Y1 comparison, NOT direction!
                // This matches bulk optimization logic (TestEntryExitLogic line 1779)
                // PineScript: isHighFirst2Long = y2PriceHLong > y1PriceHLong
                bool isHighFirst2Long = htfY2 > htfY1;
                bool isHighFirst2Short = htfY2 > htfY1;  // FIXED: Was htfY2 < htfY1 (opposite of training logic)

                // PROCESS R:R PARTIAL EXITS FIRST (CRITICAL FIX: Was missing in replay!)
                if (tradeLongState > 0 && div == 1 && UseRR)
                {
                    // Handle R:R partial exit - same logic as ProcessLongExit lines 1507-1546
                    if (ltfOpen >= rrTarget)
                    {
                        // R:R hit on open - half position closed
                        double pnl = (ltfOpen - entryLong) / 2.0;
                        PFprofitArr[bestLongsIndex] += Math.Abs(pnl);
                        winsArr[bestLongsIndex]++;
                        div = 2;
                        // Print($"[REPLAY LONG R:R] Bar={i}, Open hit R:R target, PnL={(ltfOpen - entryLong) / 2.0:F2}, Div now=2");
                    }
                    else if (ltfHigh >= rrTarget)
                    {
                        // R:R potentially hit during bar
                        if (ltfLow > exitLong)
                        {
                            // Stop wasn't hit - R:R definitely hit first
                            double pnl = (rrTarget - entryLong) / 2.0;
                            PFprofitArr[bestLongsIndex] += Math.Abs(pnl);
                            winsArr[bestLongsIndex]++;
                            div = 2;
                            // Print($"[REPLAY LONG R:R] Bar={i}, High hit R:R (stop not hit), PnL={(rrTarget - entryLong) / 2.0:F2}, Div now=2");
                        }
                        else
                        {
                            // Both R:R and stop hit on same bar - use closer2low logic
                            bool closer2low = closer2lowArr[Math.Min(i, closer2lowArr.Count - 1)];
                            if (!closer2low)
                            {
                                // Price closer to high - assume R:R hit first
                                double pnl = (rrTarget - entryLong) / 2.0;
                                PFprofitArr[bestLongsIndex] += Math.Abs(pnl);
                                winsArr[bestLongsIndex]++;
                                div = 2;
                                // Print($"[REPLAY LONG R:R] Bar={i}, Both hit (closer2high), PnL={(rrTarget - entryLong) / 2.0:F2}, Div now=2");
                            }
                            // else: Stop hit first, will be handled below
                        }
                    }
                }

                // PROCESS EXITS FIRST
                if (tradeLongState > 0)
                {
                    bool exitTriggered = false;

                    // State 1 exits
                    if (tradeLongState == 1)
                    {
                        if (ltfOpen <= exitLong)
                        {
                            exitTriggered = true;
                            double loss = Math.Abs((ltfOpen - entryLong) / div);
                            PFlossArr[bestLongsIndex] += loss;
                        }
                        else if (ltfLow <= exitLong)
                        {
                            exitTriggered = true;
                            double loss = Math.Abs((exitLong - entryLong) / div);
                            PFlossArr[bestLongsIndex] += loss;
                        }
                        else if (ltfClose >= triggerLong)
                        {
                            // Transition to state 2
                            tradeLongState = 2;
                            exitLong = Math.Max(exitLong, ltfClose - rawLtfATR * trailingMult);
                        }
                    }
                    // State 2 exits
                    else if (tradeLongState == 2)
                    {
                        if (ltfOpen <= exitLong)
                        {
                            exitTriggered = true;
                            double pnl = (ltfOpen - entryLong) / div;
                            if (pnl >= 0)
                            {
                                PFprofitArr[bestLongsIndex] += Math.Abs(pnl);
                                winsArr[bestLongsIndex]++;
                            }
                            else
                            {
                                PFlossArr[bestLongsIndex] += Math.Abs(pnl);
                            }
                        }
                        else if (ltfLow <= exitLong)
                        {
                            exitTriggered = true;
                            double pnl = (exitLong - entryLong) / div;
                            if (pnl >= 0)
                            {
                                PFprofitArr[bestLongsIndex] += Math.Abs(pnl);
                                winsArr[bestLongsIndex]++;
                            }
                            else
                            {
                                PFlossArr[bestLongsIndex] += Math.Abs(pnl);
                            }
                        }
                        else
                        {
                            // Update trailing stop
                            exitLong = Math.Max(exitLong, ltfClose - rawLtfATR * trailingMult);
                        }
                    }

                    if (exitTriggered)
                    {
                        // Add EXIT marker (PineScript style - DOWN TRIANGLE for long exit)
                        double exitPrice = (tradeLongState == 1) ? exitLong :
                                          ((ltfOpen <= exitLong) ? ltfOpen : exitLong);
                        double profitPercent = ((exitPrice / entryLong - 1) * 100) / div;
                        double profitPoints = (exitPrice - entryLong) / div;

                        string exitTooltip = $"Trade Entry: {entryLong:F2}\n" +
                                           $"Trade Exit: {exitPrice:F2}\n" +
                                           $"Profit: {profitPercent:F2}% ({profitPoints:F2} pts)";

                        tradeMarkers.Add(new TradeMarker
                        {
                            Time = timeArrEnd[Math.Min(i, timeArrEnd.Count - 1)],
                            Price = ltfHigh,  // Place above bar
                            BarIndex = i,
                            IsEntry = false,  // This is an EXIT
                            IsLong = true,
                            Tooltip = exitTooltip
                        });

                        tradeLongState = 0;
                        entryLong = 0;
                        exitLong = 0;
                        triggerLong = 0;
                        div = 1;
                    }
                }

                // CHECK FOR NEW ENTRIES
                if (tradeLongState == 0 && TradeLong)
                {
                    bool entryTriggered = false;
                    debugCheckCount++;

                    // Breakout strategy - FIXED: Use isHighFirst2Long (Y2 > Y1) not direction
                    if (StrategyType == StrategyTypeEnum.Breakout)
                    {
                        // Debug why entry not triggered
                        if (breakoutUp <= 0)
                            debugBreakoutUpZero++;
                        else if (!isHighFirst2Long)
                            debugWrongTrend++;
                        else if (!(ltfClose > breakoutUp && ltfClosePrev <= breakoutUp))
                            debugNoBreakout++;
                        else if (!(breakoutUp <= htfY1 + htfRange))
                            debugRangeFail++;

                        if (isHighFirst2Long && breakoutUp > 0 &&
                            ltfClose > breakoutUp && ltfClosePrev <= breakoutUp &&
                            breakoutUp <= htfY1 + htfRange)
                        {
                            entryTriggered = true;
                        }
                    }
                    // Cheap strategy - FIXED: Use isHighFirst2Long (Y2 > Y1) not direction
                    else if (StrategyType == StrategyTypeEnum.Cheap)
                    {
                        // Debug why entry not triggered
                        if (breakoutUp <= 0)
                            debugBreakoutUpZero++;
                        else if (!isHighFirst2Long)
                            debugWrongTrend++;
                        else if (!(ltfClose < breakoutUp && ltfClosePrev >= breakoutUp))
                            debugNoBreakout++;
                        else if (!(breakoutUp <= htfY1 + htfRange))
                            debugRangeFail++;

                        if (isHighFirst2Long && breakoutUp > 0 &&
                            ltfClose < breakoutUp && ltfClosePrev >= breakoutUp &&
                            breakoutUp <= htfY1 + htfRange)
                        {
                            entryTriggered = true;
                        }
                    }

                    if (entryTriggered)
                    {
                        entryLong = ltfClose;
                        // CRITICAL FIX: Use RAW ATR with trailing/target multipliers (not already-multiplied ltfATR)
                        // Match PineScript: round to tick size (math.round_to_mintick)
                        double tickSize = Instrument.MasterInstrument.TickSize;
                        exitLong = Math.Round((ltfClose - (rawLtfATR * trailingMult)) / tickSize) * tickSize;
                        triggerLong = Math.Round((ltfClose + (rawLtfATR * targetMult)) / tickSize) * tickSize;
                        tradeLongState = 1;
                        div = 1;

                        // Calculate R:R target if enabled
                        if (UseRR)
                        {
                            double stopDistance = entryLong - exitLong;
                            rrTarget = Math.Round((entryLong + (stopDistance * RRMultiple)) / tickSize) * tickSize;
                        }

                        tradesArr[bestLongsIndex]++;

                        // Add triangle marker for entry (PineScript style)
                        // Match PineScript format exactly: Entry, Trailing PT Trigger, PT %, Initial SL, SL %, R:R (if enabled), Ideal Amount
                        double stopPercent = ((exitLong - entryLong) / entryLong * 100);
                        double targetPercent = ((triggerLong - entryLong) / entryLong * 100);

                        // Calculate ideal contracts/shares (match PineScript logic)
                        // PineScript: math.floor(stopLossAmount / risk) for futures
                        double riskAmount = StopLossAmount;  // Risk amount per trade
                        double riskPerContract = Math.Abs(entryLong - exitLong);
                        double idealContracts = riskPerContract > 0 ? Math.Floor(riskAmount / riskPerContract) : 0;

                        string tooltip = $"Entry: {entryLong:F2}\n" +
                                       $"Trailing PT Trigger: {triggerLong:F2} ({targetPercent:F2}%)\n" +
                                       $"Initial SL: {exitLong:F2} ({stopPercent:F2}%)";

                        if (UseRR)
                        {
                            double rrPercent = ((rrTarget - entryLong) / entryLong * 100);
                            tooltip += $"\nR:R Target: {rrTarget:F2} ({rrPercent:F2}%)";
                        }

                        tooltip += $"\nIdeal Amount: {idealContracts:N3}";

                        tradeMarkers.Add(new TradeMarker
                        {
                            Time = timeArrEnd[Math.Min(i, timeArrEnd.Count - 1)],
                            Price = ltfLow,  // Place below bar at low
                            BarIndex = i,
                            IsEntry = true,
                            IsLong = true,
                            Tooltip = tooltip
                        });

                        // Log entry details (only log first few trades to avoid spam)
                        if (tradesArr[bestLongsIndex] <= 5)
                        {
                            // Print($"[REPLAY ENTRY #{tradesArr[bestLongsIndex]}] Bar={i}, Entry={entryLong:F2}, Breakout={breakoutUp:F2}, Close={ltfClose:F2}, ClosePrev={ltfClosePrev:F2}");
                            // Print($"  HTF: Y1={htfY1:F2}, Y2={htfY2:F2}, Range={htfRange:F2}, isHighFirst2Long={isHighFirst2Long}");
                        }
                    }
                }
            }

            // ============================================================================
            // SHORT TRADE REPLAY (if enabled)
            // Based on Pine Script historicalShorTrades (line 3909)
            // ============================================================================
            // Print($"[REPLAY DEBUG] About to check TradeShort={TradeShort}");
            if (TradeShort)
            {
                // Print($"[REPLAY] Re-testing SHORT trades with best parameters...");

                int tradeShortState = 0;
                double entryShort = 0;
                double exitShort = 0;
                double triggerShort = 0;
                int divShort = 1;
                double rrTargetShort = 0;

                // Use SHORT parameters
                double ltfATRmultS = bestATRshortltf;
                double htfATRmultS = bestATRshorthtf;
                double targetMultS = bestTargetS;
                double trailingMultS = bestTrailingS;

                for (int i = 1; i < n; i++)
                {
                    double ltfClose = closeArrEnd[i];
                    double ltfClosePrev = closeArrEnd[i - 1];
                    double ltfHigh = highArrEnd[i];
                    double ltfLow = lowArrEnd[i];
                    double ltfOpen = openArrEnd[Math.Min(i, openArrEnd.Count - 1)];

                    // Get RAW ATR (without multipliers) for stop/target calculations
                    double rawLtfATR = atrArrLTF[Math.Min(i, atrArrLTF.Count - 1)];

                    // Applied ATR for zigzag calculations (with ltfATRmultS)
                    double ltfATR = rawLtfATR * ltfATRmultS;

                    // CRITICAL FIX: Use SHORT-specific ZigZag arrays (calculated with SHORT ATR parameters)
                    double htfY1 = replayShortHtfY1[i];
                    double htfY2 = replayShortHtfY2[i];

                    double breakoutDn = i > 0 ? replayShortLtfBreakoutDn[i - 1] : 20e20;
                    double htfRange = Math.Abs(htfY2 - htfY1) * (BuySellRange / 100.0);

                    // CRITICAL FIX: Match PineScript and LONG section definition
                    // PineScript line 2070: isHighFirstShortFinal = y2 > y1 (uptrend)
                    // We want !isHighFirst2Short for shorts (i.e., downtrend)
                    bool isHighFirst2Short = htfY2 > htfY1;

                    // PROCESS R:R PARTIAL EXITS FIRST (CRITICAL FIX: Was missing in replay!)
                    if (tradeShortState < 0 && divShort == 1 && UseRR)
                    {
                        // Handle R:R partial exit for SHORTS - same logic as ProcessShortExit lines 1690-1730
                        if (ltfOpen <= rrTargetShort)
                        {
                            // R:R hit on open - half position closed
                            double pnl = (entryShort - ltfOpen) / 2.0;
                            PFprofitArrS[bestShortsIndex] += Math.Abs(pnl);
                            winsArrS[bestShortsIndex]++;
                            divShort = 2;
                            // Print($"[REPLAY SHORT R:R] Bar={i}, Open hit R:R target, PnL={(entryShort - ltfOpen) / 2.0:F2}, Div now=2");
                        }
                        else if (ltfLow <= rrTargetShort)
                        {
                            // R:R potentially hit during bar
                            if (ltfHigh < exitShort)
                            {
                                // Stop wasn't hit - R:R definitely hit first
                                double pnl = (entryShort - rrTargetShort) / 2.0;
                                PFprofitArrS[bestShortsIndex] += Math.Abs(pnl);
                                winsArrS[bestShortsIndex]++;
                                divShort = 2;
                                // Print($"[REPLAY SHORT R:R] Bar={i}, Low hit R:R (stop not hit), PnL={(entryShort - rrTargetShort) / 2.0:F2}, Div now=2");
                            }
                            else
                            {
                                // Both R:R and stop hit on same bar - use closer2low logic
                                // For SHORTS: If closer2low=true, price was closer to low, so R:R hit first
                                bool closer2low = closer2lowArr[Math.Min(i, closer2lowArr.Count - 1)];
                                if (closer2low)
                                {
                                    // Price closer to low - assume R:R hit first for shorts
                                    double pnl = (entryShort - rrTargetShort) / 2.0;
                                    PFprofitArrS[bestShortsIndex] += Math.Abs(pnl);
                                    winsArrS[bestShortsIndex]++;
                                    divShort = 2;
                                    // Print($"[REPLAY SHORT R:R] Bar={i}, Both hit (closer2low), PnL={(entryShort - rrTargetShort) / 2.0:F2}, Div now=2");
                                }
                                // else: Stop hit first, will be handled below
                            }
                        }
                    }

                    // PROCESS EXITS FIRST
                    if (tradeShortState < 0)
                    {
                        bool exitTriggered = false;

                        // State -1 exits
                        if (tradeShortState == -1)
                        {
                            if (ltfOpen >= exitShort)
                            {
                                exitTriggered = true;
                                double loss = Math.Abs((entryShort - ltfOpen) / divShort);
                                PFlossArrS[bestShortsIndex] += loss;
                            }
                            else if (ltfHigh >= exitShort)
                            {
                                exitTriggered = true;
                                double loss = Math.Abs((entryShort - exitShort) / divShort);
                                PFlossArrS[bestShortsIndex] += loss;
                            }
                            else if (ltfClose <= triggerShort)
                            {
                                tradeShortState = -2;
                                exitShort = Math.Min(exitShort, ltfClose + (rawLtfATR * trailingMultS));
                            }
                        }
                        // State -2 exits
                        else if (tradeShortState == -2)
                        {
                            if (ltfOpen >= exitShort)
                            {
                                exitTriggered = true;
                                double pnl = (entryShort - ltfOpen) / divShort;
                                if (pnl >= 0)
                                {
                                    PFprofitArrS[bestShortsIndex] += Math.Abs(pnl);
                                    winsArrS[bestShortsIndex]++;
                                }
                                else
                                {
                                    PFlossArrS[bestShortsIndex] += Math.Abs(pnl);
                                }
                            }
                            else if (ltfHigh >= exitShort)
                            {
                                exitTriggered = true;
                                double pnl = (entryShort - exitShort) / divShort;
                                if (pnl >= 0)
                                {
                                    PFprofitArrS[bestShortsIndex] += Math.Abs(pnl);
                                    winsArrS[bestShortsIndex]++;
                                }
                                else
                                {
                                    PFlossArrS[bestShortsIndex] += Math.Abs(pnl);
                                }
                            }
                            else
                            {
                                exitShort = Math.Min(exitShort, ltfClose + (rawLtfATR * trailingMultS));
                            }
                        }

                        if (exitTriggered)
                        {
                            // Add EXIT marker (PineScript style - UP TRIANGLE for short exit)
                            double exitPriceShort = (tradeShortState == -1) ? exitShort :
                                                   (ltfOpen >= exitShort ? ltfOpen : exitShort);
                            double profitPercentShort = ((1 - exitPriceShort / entryShort) * 100) / divShort;
                            double profitPointsShort = (entryShort - exitPriceShort) / divShort;

                            string exitTooltipShort = $"Trade Entry: {entryShort:F2}\n" +
                                                     $"Trade Exit: {exitPriceShort:F2}\n" +
                                                     $"Profit: {profitPercentShort:F2}% ({profitPointsShort:F2} pts)";

                            tradeMarkers.Add(new TradeMarker
                            {
                                Time = timeArrEnd[Math.Min(i, timeArrEnd.Count - 1)],
                                Price = ltfLow,  // Place below bar for short exit
                                BarIndex = i,
                                IsEntry = false,  // This is an EXIT
                                IsLong = false,   // SHORT exit
                                Tooltip = exitTooltipShort
                            });

                            tradeShortState = 0;
                            entryShort = 0;
                            exitShort = 0;
                            triggerShort = 0;
                            divShort = 1;
                        }
                    }

                    // CHECK FOR NEW ENTRIES
                    if (tradeShortState == 0)
                    {
                        bool entryTriggered = false;

                        // Breakout strategy
                        if (StrategyType == StrategyTypeEnum.Breakout)
                        {
                            // FIXED: Use !isHighFirst2Short to match training logic
                            // CRITICAL: Check breakoutDn < 20e20 (NOT double.MaxValue) to match training sentinel
                            if (!isHighFirst2Short && breakoutDn < 20e20 &&
                                ltfClose < breakoutDn && ltfClosePrev >= breakoutDn &&
                                breakoutDn >= htfY1 - htfRange)
                            {
                                // DEBUG: Log first 3 SHORT entry triggers to verify breakout level
                                if (tradesArrS[bestShortsIndex] < 3)
                                {
                                    // Print($"[SHORT ENTRY TRIGGER #{tradesArrS[bestShortsIndex] + 1}] Bar={i}, BreakoutDn={breakoutDn:F2}, ltfClose={ltfClose:F2}, ltfClosePrev={ltfClosePrev:F2}");
                                    // Print($"[SHORT ENTRY TRIGGER #{tradesArrS[bestShortsIndex] + 1}] HTF: Y1={htfY1:F2}, Y2={htfY2:F2}, Range={htfRange:F2}, MinBreakout={htfY1 - htfRange:F2}");
                                }
                                entryTriggered = true;
                            }
                        }
                        // Cheap strategy
                        else if (StrategyType == StrategyTypeEnum.Cheap)
                        {
                            // FIXED: Use !isHighFirst2Short to match training logic
                            // CRITICAL: Check breakoutDn < 20e20 (NOT double.MaxValue) to match training sentinel
                            if (!isHighFirst2Short && breakoutDn < 20e20 &&
                                ltfClose > breakoutDn && ltfClosePrev <= breakoutDn &&
                                breakoutDn >= htfY1 - htfRange)
                            {
                                entryTriggered = true;
                            }
                        }

                        if (entryTriggered)
                        {
                            entryShort = ltfClose;
                            // CRITICAL FIX: Use RAW ATR with trailing/target multipliers (not already-multiplied ltfATR)
                            // Match PineScript: round to tick size (math.round_to_mintick)
                            double tickSize = Instrument.MasterInstrument.TickSize;
                            exitShort = Math.Round((ltfClose + (rawLtfATR * trailingMultS)) / tickSize) * tickSize;
                            triggerShort = Math.Round((ltfClose - (rawLtfATR * targetMultS)) / tickSize) * tickSize;

                            // DEBUG: Print SHORT entry parameters for first 3 trades
                            if (tradesArrS[bestShortsIndex] <= 3)
                            {
                                // Print($"[DEBUG SHORT ENTRY #{tradesArrS[bestShortsIndex]}] rawLtfATR={rawLtfATR:F2}, trailingMultS={trailingMultS:F2}, targetMultS={targetMultS:F2}");
                                // Print($"[DEBUG SHORT ENTRY #{tradesArrS[bestShortsIndex]}] ltfClose={ltfClose:F2}, exitShort={exitShort:F2}, triggerShort={triggerShort:F2}");
                                // Print($"[DEBUG SHORT ENTRY #{tradesArrS[bestShortsIndex]}] Best SHORT params from optimization: {bestATRshortltf:F2}_{bestATRshorthtf:F2}_{bestTargetS:F2}_{bestTrailingS:F2}");
                                // Print($"[DEBUG SHORT ENTRY #{tradesArrS[bestShortsIndex]}] Calculation check: exit={ltfClose:F2} + ({rawLtfATR:F2} * {trailingMultS:F2}) = {ltfClose + (rawLtfATR * trailingMultS):F2}");
                                // Print($"[DEBUG SHORT ENTRY #{tradesArrS[bestShortsIndex]}] Calculation check: trigger={ltfClose:F2} - ({rawLtfATR:F2} * {targetMultS:F2}) = {ltfClose - (rawLtfATR * targetMultS):F2}");
                            }

                            tradeShortState = -1;
                            divShort = 1;

                            if (UseRR)
                            {
                                double stopDistance = exitShort - entryShort;
                                rrTargetShort = Math.Round((entryShort - (stopDistance * RRMultiple)) / tickSize) * tickSize;
                            }

                            tradesArrS[bestShortsIndex]++;

                            // Add triangle marker for SHORT entry (PineScript style)
                            // Match PineScript format exactly: Entry, Trailing PT Trigger, PT %, Initial SL, SL %, R:R (if enabled), Ideal Amount
                            double stopPercentShort = ((exitShort - entryShort) / entryShort * 100);
                            double targetPercentShort = ((entryShort - triggerShort) / entryShort * 100);

                            // Calculate ideal contracts/shares (match PineScript logic)
                            // PineScript: math.floor(stopLossAmount / risk) for futures
                            double riskAmountShort = StopLossAmount;  // Risk amount per trade
                            double riskPerContractShort = Math.Abs(exitShort - entryShort);
                            double idealContractsShort = riskPerContractShort > 0 ? Math.Floor(riskAmountShort / riskPerContractShort) : 0;

                            string tooltipShort = $"Entry: {entryShort:F2}\n" +
                                               $"Trailing PT Trigger: {triggerShort:F2} ({targetPercentShort:F2}%)\n" +
                                               $"Initial SL: {exitShort:F2} ({stopPercentShort:F2}%)";

                            if (UseRR)
                            {
                                double rrPercentShort = ((entryShort - rrTargetShort) / entryShort * 100);
                                tooltipShort += $"\nR:R Target: {rrTargetShort:F2} ({rrPercentShort:F2}%)";
                            }

                            tooltipShort += $"\nIdeal Amount: {idealContractsShort:N3}";

                            tradeMarkers.Add(new TradeMarker
                            {
                                Time = timeArrEnd[Math.Min(i, timeArrEnd.Count - 1)],
                                Price = ltfHigh,  // Place above bar at high
                                BarIndex = i,
                                IsEntry = true,
                                IsLong = false,
                                Tooltip = tooltipShort
                            });

                            if (tradesArrS[bestShortsIndex] <= 5)
                            {
                                // Print($"[REPLAY SHORT ENTRY #{tradesArrS[bestShortsIndex]}] Bar={i}, Entry={entryShort:F2}, Breakout={breakoutDn:F2}");
                            }
                        }
                    }
                }

                // Save final SHORT trade state
                this.entryShort = entryShort;
                this.exitShort = exitShort;
                this.inTradeShort = tradeShortState;
                this.triggerShort = triggerShort;
                this.divShort = divShort;
                this.RRtpShort = rrTargetShort;

                double shortPF = PFprofitArrS[bestShortsIndex] > 0 ?
                    (PFlossArrS[bestShortsIndex] > 0 ? PFprofitArrS[bestShortsIndex] / PFlossArrS[bestShortsIndex] : PFprofitArrS[bestShortsIndex]) : 0;
                double shortWR = tradesArrS[bestShortsIndex] > 0 ? (double)winsArrS[bestShortsIndex] / tradesArrS[bestShortsIndex] * 100 : 0;

                // Print($"[REPLAY COMPLETE SHORTS] Profit={PFprofitArrS[bestShortsIndex]:F2}, Loss={PFlossArrS[bestShortsIndex]:F2}, Wins={winsArrS[bestShortsIndex]}, Trades={tradesArrS[bestShortsIndex]}");
                // Print($"[REPLAY COMPLETE SHORTS] PF={shortPF:F4}, WinRate={shortWR:F1}%");
            }

            // ============================================================================
            // DEBUG OUTPUT: Show why entries are failing
            // ============================================================================
            // Print($"[REPLAY DEBUG] Total bars checked: {debugCheckCount}");
            // Print($"[REPLAY DEBUG] Failures - BreakoutUp=0: {debugBreakoutUpZero}, WrongTrend: {debugWrongTrend}, NoBreakout: {debugNoBreakout}, RangeFail: {debugRangeFail}");
            // Print($"[REPLAY DEBUG] Trades found: {tradesArr[bestLongsIndex]}");
            // Print($"[REPLAY DEBUG] Final profit: {PFprofitArr[bestLongsIndex]:F2}, loss: {PFlossArr[bestLongsIndex]:F2}");

            // ============================================================================
            // CRITICAL: Save final trade state to class-level variables
            // This matches Pine Script's behavior (lines 3868-3874) where historicalLongTrades()
            // returns [entryLong, exitLong, inTradeLong, ...] and these values are stored
            // in persistent variables so trades can continue into real-time
            // ============================================================================
            entryLong = entryLong;
            exitLong = exitLong;
            inTradeLong = tradeLongState;
            triggerLong = triggerLong;
            divLong = div;
            RRtpLong = rrTarget;

            if (tradeLongState != 0)
            {
                // Print($"[REPLAY FINAL STATE LONG] Trade continues into real-time: State={tradeLongState}, Entry={entryLong:F2}, Exit={exitLong:F2}, Trigger={triggerLong:F2}, Div={div}, RRTarget={rrTarget:F2}");
            }
            else
            {
                // Print($"[REPLAY FINAL STATE LONG] No open trade at end of history");
            }

            // TODO: Add SHORT trade state saving when short replay is fully implemented
            // this.entryShort = entryShort;
            // this.exitShort = exitShort;
            // this.inTradeShort = tradeShortState;
            // this.triggerShort = triggerShort;
            // this.divShort = divShort;
        }

        #endregion

        #region OnRender
        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);

            if (Bars == null || ChartControl == null) return;

            // Initialize brushes
            InitializeRenderResources();

            // Draw breakout lines FIRST (behind everything)
            DrawBreakoutLines(chartControl, chartScale, zzHTF);
            DrawBreakoutLines(chartControl, chartScale, zzLTF);

            // Draw pivot price lines (Y1 horizontal lines)
            DrawPivotPriceLines(chartControl, chartScale, zzHTF);
            DrawPivotPriceLines(chartControl, chartScale, zzLTF);

            // Draw IQ Meters (behind zigzag lines)
            if (trained)
            {
                if (ShowHTFZZ && zzHTF.Y1Price != 0 && zzHTF.Y2Price != 0)
                    DrawIQMeter(chartControl, chartScale, zzHTF, true); // HTF meter

                if (zzLTF.Y1Price != 0 && zzLTF.Y2Price != 0)
                    DrawIQMeter(chartControl, chartScale, zzLTF, false); // LTF meter
            }

            // Draw ZigZag lines
            DrawZigZagLines(chartControl, chartScale, zzLTF, ltfBrushDX, ltfBrushDXFaded);

            if (ShowHTFZZ)
                DrawZigZagLines(chartControl, chartScale, zzHTF, htfBrushDX, htfBrushDXFaded);

            // Draw projections
            if (ShowProjection)
            {
                DrawProjections(chartControl, chartScale, zzLTF, ltfBarsInProgress);
                if (ShowHTFZZ)
                    DrawProjections(chartControl, chartScale, zzHTF, htfBarsInProgress);
            }

            // Draw Fibometer
            if (Fibometer && ShowHTFZZ)
                DrawFibometer(chartScale, chartControl);

            // Draw trade entry markers (PineScript style triangles)
            if (trained)
                DrawTradeMarkers(chartControl, chartScale);

            // Draw performance table
            if (TablePosition != TablePositionEnum.None)
                DrawPerformanceTable(chartControl);
        }

        private void InitializeRenderResources()
        {
            if (RenderTarget == null) return;

            if (ltfBrushDX == null || ltfBrushDX.IsDisposed)
            {
                var color = ((System.Windows.Media.SolidColorBrush)LTFLineBrush).Color;
                ltfBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color(color.R, color.G, color.B, (byte)255));
                ltfBrushDXFaded = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color(color.R, color.G, color.B, (byte)100));
            }

            if (htfBrushDX == null || htfBrushDX.IsDisposed)
            {
                var color = ((System.Windows.Media.SolidColorBrush)HTFLineBrush).Color;
                htfBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color(color.R, color.G, color.B, (byte)255));
                htfBrushDXFaded = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color(color.R, color.G, color.B, (byte)100));
            }

            if (projectionBrushLongDX == null || projectionBrushLongDX.IsDisposed)
            {
                projectionBrushLongDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)255));
                projectionBrushShortDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)255));
                breakoutBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color((byte)242, (byte)73, (byte)104, (byte)255)); // #F24968
            }

            if (fibBorderBrushDX == null || fibBorderBrushDX.IsDisposed)
            {
                fibBorderBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color((byte)54, (byte)56, (byte)67, (byte)255));
                fibBackgroundBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color((byte)32, (byte)34, (byte)44, (byte)200));
                textBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color((byte)242, (byte)184, (byte)7, (byte)255)); // Gold for title
                whiteTextBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color((byte)255, (byte)255, (byte)255, (byte)255));
            }

            if (textFormat == null || textFormat.IsDisposed)
            {
                textFormat = new SharpDX.DirectWrite.TextFormat(Core.Globals.DirectWriteFactory,
                    "Segoe UI", 11);
                smallTextFormat = new SharpDX.DirectWrite.TextFormat(Core.Globals.DirectWriteFactory,
                    "Segoe UI", 9);
            }

            // Initialize gradient brushes for IQ meters if needed or refresh
            if (needsIQMeterRefresh || iqMeterBrushesHTF.Count == 0)
            {
                InitializeIQMeterBrushes();
                needsIQMeterRefresh = false;
            }
        }

        private void InitializeIQMeterBrushes()
        {
            // Dispose existing brushes
            foreach (var brush in iqMeterBrushesHTF)
            {
                if (brush != null && !brush.IsDisposed)
                    brush.Dispose();
            }
            iqMeterBrushesHTF.Clear();

            foreach (var brush in iqMeterBrushesLTF)
            {
                if (brush != null && !brush.IsDisposed)
                    brush.Dispose();
            }
            iqMeterBrushesLTF.Clear();

            // Create gradients based on market direction
            // Match PineScript logic: compare current Y2 with PREVIOUS BAR's Y1
            // PineScript: condUpH = y2PriceHTFLFinal.last() > y1PriceHTFLFinal.last()[1]
            // The [1] means "value from 1 bar ago", not array history
            bool condUpH = zzHTF.Y2Price > prevBarY1HTF;
            bool condUpL = zzLTF.Y2Price > prevBarY1LTF;
            bool condDnH = zzHTF.Y2Price < prevBarY1HTF;
            bool condDnL = zzLTF.Y2Price < prevBarY1LTF;

            // HTF IQ Meter colors
            SharpDX.Color startColorHTF, endColorHTF;
            if ((condUpH && condUpL) || inTradeLong > 0)
            {
                startColorHTF = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)64);  // #74ffbc40
                endColorHTF = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)255);    // #74ffbc
            }
            else if ((condDnH && condDnL) || inTradeShort < 0)
            {
                startColorHTF = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)77);   // #ff74744d
                endColorHTF = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)255);     // Full red
            }
            else
            {
                startColorHTF = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)128);
                endColorHTF = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)255);
            }

            // LTF IQ Meter colors
            SharpDX.Color startColorLTF, endColorLTF;
            if ((condUpH && condUpL) || inTradeLong > 0)
            {
                startColorLTF = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)64);
                endColorLTF = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)255);
            }
            else if ((condDnH && condDnL) || inTradeShort < 0)
            {
                startColorLTF = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)77);
                endColorLTF = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)255);
            }
            else
            {
                startColorLTF = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)128);
                endColorLTF = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)255);
            }

            // Mixed trend handling
            if (inTradeLong == 0 && inTradeShort == 0)
            {
                if (condUpH && !condUpL)
                {
                    // HTF up, LTF down - use purple transition
                    startColorHTF = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)64);
                    endColorHTF = new SharpDX.Color((byte)128, (byte)116, (byte)255, (byte)255);
                }
                else if (!condUpH && condUpL)
                {
                    // HTF down, LTF up - use purple transition
                    startColorHTF = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)77);
                    endColorHTF = new SharpDX.Color((byte)128, (byte)116, (byte)255, (byte)255);
                }

                if (condUpL && !condUpH)
                {
                    startColorLTF = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)64);
                    endColorLTF = new SharpDX.Color((byte)128, (byte)116, (byte)255, (byte)255);
                }
                else if (!condUpL && condUpH)
                {
                    startColorLTF = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)77);
                    endColorLTF = new SharpDX.Color((byte)128, (byte)116, (byte)255, (byte)255);
                }
            }

            // Create gradient brushes for both meters
            int middle = EndLoop / 2;
            for (int i = 0; i <= EndLoop; i++)
            {
                float ratio;
                SharpDX.Color colorHTF, colorLTF;

                if (i <= middle)
                {
                    ratio = (float)i / middle;
                    colorHTF = InterpolateColor(startColorHTF, endColorHTF, ratio);
                    colorLTF = InterpolateColor(startColorLTF, endColorLTF, ratio);
                }
                else
                {
                    ratio = (float)(i - middle) / (EndLoop - middle);
                    colorHTF = InterpolateColor(endColorHTF, startColorHTF, ratio);
                    colorLTF = InterpolateColor(endColorLTF, startColorLTF, ratio);
                }

                iqMeterBrushesHTF.Add(new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, colorHTF));
                iqMeterBrushesLTF.Add(new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, colorLTF));
            }
        }

        private SharpDX.Color InterpolateColor(SharpDX.Color c1, SharpDX.Color c2, float ratio)
        {
            byte r = (byte)(c1.R + (c2.R - c1.R) * ratio);
            byte g = (byte)(c1.G + (c2.G - c1.G) * ratio);
            byte b = (byte)(c1.B + (c2.B - c1.B) * ratio);
            byte a = (byte)(c1.A + (c2.A - c1.A) * ratio);
            return new SharpDX.Color(r, g, b, a);
        }
      

        private void DrawIQMeter(ChartControl chartControl, ChartScale chartScale, ZigZagState zz, bool isHTF)
        {
            double y1 = zz.Y1Price;
            double y2 = zz.Y2Price;

            if (y1 == 0 || y2 == 0) return;

            bool isHigh = y2 > y1;

            // PineScript Lines 3227-3228: Use ONLY zigzag range, NO extension
            // newRange = math.abs(getY2PRICEHTFL - getY1PRICEHTFL)
            double newRange = Math.Abs(y2 - y1);
            double range = newRange / (EndLoop + 1);

            // Bar-based positioning for zoom responsiveness
            // Position meters to the RIGHT of the last bar (off-chart area)
            const int METER_WIDTH_BARS = 4;      // Meter is 4 bars wide
            const int LTF_START_OFFSET = 10;     // LTF starts 10 bars to the right of last bar
            const int HTF_START_OFFSET = 25;     // HTF starts 25 bars to the right of last bar

            // Get the rightmost visible bar index
            int lastBarIndex = ChartBars.ToIndex;

            // Calculate meter position (ADD to go right, into future/off-chart area)
            int meterStartBarIndex = isHTF ? (lastBarIndex + HTF_START_OFFSET) : (lastBarIndex + LTF_START_OFFSET);
            int meterEndBarIndex = meterStartBarIndex + METER_WIDTH_BARS;

            // Convert bar indices to X pixel coordinates
            int xStart = (int)chartControl.GetXByBarIndex(ChartBars, meterStartBarIndex);
            int xEnd = (int)chartControl.GetXByBarIndex(ChartBars, meterEndBarIndex);
            int baseWidth = Math.Abs(xEnd - xStart);

            // Safety check: if width is too small (bars off screen), skip drawing
            if (baseWidth < 2 || xStart < 0) return;

            var brushes = isHTF ? iqMeterBrushesHTF : iqMeterBrushesLTF;

            // PineScript Lines 3366-3369: Draw boxes from Y1 price, going up or down based on isHigh
            // true => [color.green, color.red, getY1PRICEHTFL + i * Range, getY1PRICEHTFL + ((i + 1) * Range)]
            // =>   => [color.green, color.red, getY1PRICEHTFL - i * Range, getY1PRICEHTFL - ((i + 1) * Range)]
            for (int i = 0; i <= EndLoop; i++)
            {
                double btm, top;

                if (isHigh)
                {
                    // Y2 > Y1: Start from Y1 (bottom) and build UP
                    btm = y1 + (range * i);
                    top = y1 + (range * (i + 1));
                }
                else
                {
                    // Y2 < Y1: Start from Y1 (top) and build DOWN
                    btm = y1 - (range * i);
                    top = y1 - (range * (i + 1));
                }

                float yBottom = chartScale.GetYByValue(Math.Min(btm, top));
                float yTop = chartScale.GetYByValue(Math.Max(btm, top));
                float boxHeight = Math.Abs(yBottom - yTop);

                // Draw the colored box
                if (i < brushes.Count && boxHeight > 0.5f)
                {
                    RenderTarget.FillRectangle(
                        new RectangleF(xStart, yTop, baseWidth, boxHeight),
                        brushes[i]);
                }
            }

            // Draw label at top with timeframe
            string timeframeText = isHTF ? $"{resolvedHTFMinutes}m" : $"{resolvedLTFMinutes}m";
            // Label at Y2 (the current swing extreme)
            float labelY = chartScale.GetYByValue(y2);
            RenderTarget.DrawText(timeframeText, smallTextFormat,
                new RectangleF(xStart - 2, labelY - 15, baseWidth + 4, 12),
                whiteTextBrushDX, DrawTextOptions.None, MeasuringMode.Natural);

            // PineScript Lines 3233-3236: Calculate Fibonacci levels from Y2 based on direction
            // [fib618, fib382, fib236, fib786] = switch isHigh
            //     true => [getY2PRICEHTFL - newRange * .618, getY2PRICEHTFL - newRange * .382, ...]
            //     =>      [getY2PRICEHTFL + newRange * .618, getY2PRICEHTFL + newRange * .382, ...]
            double fib382, fib618, fib236, fib786, fib50;

            if (isHigh)
            {
                // Uptrend: Y2 is HIGH, subtract to go down toward Y1
                fib382 = y2 - (newRange * 0.382);
                fib618 = y2 - (newRange * 0.618);
                fib236 = y2 - (newRange * 0.236);
                fib786 = y2 - (newRange * 0.786);
                fib50 = y2 - (newRange * 0.5);
            }
            else
            {
                // Downtrend: Y2 is LOW, add to go up toward Y1
                fib382 = y2 + (newRange * 0.382);
                fib618 = y2 + (newRange * 0.618);
                fib236 = y2 + (newRange * 0.236);
                fib786 = y2 + (newRange * 0.786);
                fib50 = y2 + (newRange * 0.5);
            }

            // Always draw .382 and .618 levels
            // Draw Fibonacci label boxes (right side of meter) - PineScript lines 3246-3260
            DrawFibLabel(chartControl, chartScale, fib382, xStart + baseWidth + 2, ".382");
            DrawFibLabel(chartControl, chartScale, fib618, xStart + baseWidth + 2, ".618");

            // Draw Fibonacci price boxes (left side of meter) - PineScript lines 3300-3314
            DrawFibPrice(chartControl, chartScale, fib382, xStart - 35, fib382);
            DrawFibPrice(chartControl, chartScale, fib618, xStart - 35, fib618);

            // Draw dotted separator lines at fib levels (PineScript lines 3432-3444)
            DrawFibSeparator(chartControl, chartScale, fib618, xStart + (baseWidth / 2));
            DrawFibSeparator(chartControl, chartScale, fib382, xStart + (baseWidth / 2));

            // PineScript lines 3262-3340: When Fibometer is enabled, show .236, .786, and .5 levels
            if (Fibometer)
            {
                // Draw additional Fib labels (right side)
                DrawFibLabel(chartControl, chartScale, fib236, xStart + baseWidth + 2, ".236");
                DrawFibLabel(chartControl, chartScale, fib786, xStart + baseWidth + 2, ".786");
                DrawFibLabel(chartControl, chartScale, fib50, xStart + baseWidth + 2, ".5");

                // Draw additional Fib prices (left side)
                DrawFibPrice(chartControl, chartScale, fib236, xStart - 35, fib236);
                DrawFibPrice(chartControl, chartScale, fib786, xStart - 35, fib786);
                DrawFibPrice(chartControl, chartScale, fib50, xStart - 35, fib50);

                // PineScript lines 3343-3353: Highlight active Fib level containing current price
                // This highlights the border of the Fib box that contains the current close price
                double currentClose = Close[0];
                List<(double price, string label)> fibLevels = new List<(double, string)>
                {
                    (fib236, ".236"), (fib382, ".382"), (fib50, ".5"), (fib618, ".618"), (fib786, ".786")
                };

                // Check which Fib level contains the current price and highlight it
                foreach (var level in fibLevels)
                {
                    double levelTop = level.price + (newRange * 0.05);
                    double levelBot = level.price - (newRange * 0.05);

                    if (currentClose >= Math.Min(levelBot, levelTop) && currentClose <= Math.Max(levelBot, levelTop))
                    {
                        // Redraw with highlighted border (using the meter color)
                        DrawFibPriceHighlighted(chartControl, chartScale, level.price, xStart - 35, level.price, isHTF);
                        break;
                    }
                }
            }

            // PineScript Lines 3291-3297: Temperature gauge calculation
            // [fibP, format] = switch fibometer
            //     false => [close, format.mintick]
            //     =>       [(close - getLow) / newRange, "###,###.###"]
            // if fibometer and isHigh
            //     fibP := 1 - fibP
            double currentPrice = Close[0];
            double getLow = Math.Min(y1, y2);
            string tempText;

            if (Fibometer)
            {
                // Show Fibonacci percentage
                double fibPosition = (currentPrice - getLow) / newRange;

                // Invert if isHigh (PineScript line 3296-3297)
                if (isHigh)
                    fibPosition = 1 - fibPosition;

                tempText = $"{(fibPosition * 100):F1}%";
            }
            else
            {
                // Show actual price (format.mintick)
                tempText = currentPrice.ToString("F2");
            }

            float tempY = chartScale.GetYByValue(currentPrice);

            // Draw temperature box with thermometer emoji (PineScript lines 3356-3362, 3580-3586)
            RectangleF tempBox = new RectangleF(xStart - 58, tempY - 10, 38, 20);
            RenderTarget.FillRectangle(tempBox, new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(32, 34, 44, 255)));
            RenderTarget.DrawRectangle(tempBox, new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(54, 56, 67, 255)), 1);
            RenderTarget.DrawText(tempText, smallTextFormat,
                new RectangleF(tempBox.Left + 2, tempBox.Top + 3, tempBox.Width - 4, tempBox.Height - 6),
                whiteTextBrushDX, DrawTextOptions.None, MeasuringMode.Natural);
        }

        private void DrawFibLabel(ChartControl chartControl, ChartScale chartScale, double price, float xPos, string label)
        {
            float yPos = chartScale.GetYByValue(price);
            RectangleF box = new RectangleF(xPos, yPos - 8, 25, 16);

            // Draw box background
            RenderTarget.FillRectangle(box, new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(32, 34, 44, 255)));
            RenderTarget.DrawRectangle(box, ltfBrushDX, 1);

            // Draw text
            RenderTarget.DrawText(label, smallTextFormat,
                new RectangleF(box.Left + 2, box.Top + 2, box.Width - 4, box.Height - 4),
                whiteTextBrushDX, DrawTextOptions.None, MeasuringMode.Natural);
        }

        private void DrawFibPrice(ChartControl chartControl, ChartScale chartScale, double price, float xPos, double value)
        {
            float yPos = chartScale.GetYByValue(price);
            string priceText = value.ToString("F2");
            RectangleF box = new RectangleF(xPos, yPos - 10, 30, 20);

            // Draw box background
            RenderTarget.FillRectangle(box, new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(32, 34, 44, 255)));
            RenderTarget.DrawRectangle(box, new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(54, 56, 67, 255)), 1);

            // Draw text
            RenderTarget.DrawText(priceText, smallTextFormat,
                new RectangleF(box.Left + 2, box.Top + 3, box.Width - 4, box.Height - 6),
                whiteTextBrushDX, DrawTextOptions.None, MeasuringMode.Natural);
        }

        /// <summary>
        /// Draw Fibonacci price box with highlighted border when current price is within this level
        /// Based on PineScript lines 3343-3353
        /// </summary>
        private void DrawFibPriceHighlighted(ChartControl chartControl, ChartScale chartScale, double price, float xPos, double value, bool isHTF)
        {
            float yPos = chartScale.GetYByValue(price);
            string priceText = value.ToString("F2");
            RectangleF box = new RectangleF(xPos, yPos - 10, 30, 20);

            // Determine border color based on market direction
            bool condUpH = zzHTF.Y2Price > zzHTF.Y1Price;
            bool condUpL = zzLTF.Y2Price > zzLTF.Y1Price;
            bool condDnH = zzHTF.Y2Price < zzHTF.Y1Price;
            bool condDnL = zzLTF.Y2Price < zzLTF.Y1Price;

            SharpDX.Color borderColor;
            if ((condUpH && condUpL) || inTradeLong > 0)
            {
                // Both uptrend or in long trade - green
                borderColor = new SharpDX.Color((byte)116, (byte)255, (byte)188, (byte)255); // #74ffbc
            }
            else if ((condDnH && condDnL) || inTradeShort < 0)
            {
                // Both downtrend or in short trade - red
                borderColor = new SharpDX.Color((byte)255, (byte)116, (byte)116, (byte)255);
            }
            else
            {
                // Mixed trend - purple
                borderColor = new SharpDX.Color((byte)128, (byte)116, (byte)255, (byte)255);
            }

            // Draw box background
            RenderTarget.FillRectangle(box, new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(32, 34, 44, 255)));
            // Draw highlighted border
            RenderTarget.DrawRectangle(box, new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, borderColor), 2);

            // Draw text
            RenderTarget.DrawText(priceText, smallTextFormat,
                new RectangleF(box.Left + 2, box.Top + 3, box.Width - 4, box.Height - 6),
                whiteTextBrushDX, DrawTextOptions.None, MeasuringMode.Natural);
        }

        /// <summary>
        /// Draw dotted separator line at Fibonacci level
        /// Based on PineScript lines 3432-3444 and 3655-3666
        /// </summary>
        private void DrawFibSeparator(ChartControl chartControl, ChartScale chartScale, double price, float xCenter)
        {
            float yPos = chartScale.GetYByValue(price);

            // Draw "⋯" text centered on the meter as a subtle separator
            RenderTarget.DrawText("⋯", smallTextFormat,
                new RectangleF(xCenter - 10, yPos - 6, 20, 12),
                new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(54, 56, 67, 255)),
                DrawTextOptions.None, MeasuringMode.Natural);
        }

        /// <summary>
        /// Draw breakout lines when price crosses breakout levels
        /// Matching RadiIQ lines 1664-1682 (break up) and 1940-1957 (break down)
        /// </summary>
        private void DrawBreakoutLines(ChartControl chartControl, ChartScale chartScale, ZigZagState zz)
        {
            

            if (!trained || zz.BreakoutLines.Count == 0)
                return;

            // Create brushes for breakout lines
            var greenBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                new SharpDX.Color(116, 255, 188, 255)); // #74ffbc - long breakouts
            var redBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                new SharpDX.Color(255, 116, 116, 255)); // #ff7474 - short breakouts

            int linesDrawn = 0;
            int skippedInvalid = 0;
            int skippedOffScreen = 0;

            // Draw all breakout lines (both active and static)
            foreach (var line in zz.BreakoutLines)
            {
                // Check if times are valid (allow X1 == X2, we'll handle it below)
                if (line.X1 == default(DateTime) || line.X2 == default(DateTime))
                {
                    skippedInvalid++;
                    continue;
                }

                // Get screen coordinates
                int x1 = chartControl.GetXByTime(line.X1);
                int x2 = chartControl.GetXByTime(line.X2);
                float y = chartScale.GetYByValue(line.Y);

                // Check if line is visible in current viewport
                bool isVisible = (x2 >= 0 && x1 <= chartControl.CanvasRight) || (x1 >= 0 && x1 <= chartControl.CanvasRight);

                
               

                

             

               
                var strokeProps = new StrokeStyleProperties
                    {
                        DashStyle = SharpDX.Direct2D1.DashStyle.Dot,
                        DashCap = CapStyle.Round,
                        StartCap = CapStyle.Round,
                        EndCap = CapStyle.Round
                    };

                // Select brush based on breakout direction
                var brush = line.IsLongBreakout ? greenBrush : redBrush;
                var dottedStrokeStyle = new SharpDX.Direct2D1.StrokeStyle(
                        Core.Globals.D2DFactory,  // ✅ Use D2DFactory (correct)
                        strokeProps
                    );
                // Draw dotted horizontal line (RadiIQ: style = line.style_dotted)
                // using (var style = new StrokeStyle(Core.Globals.D2DFactory,
                //     new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dot }))
                // {
                //     Print($"[DrawBreakoutLines] Drawing breakout line from ({x1},{y}) to ({x2},{y})");
                //     RenderTarget.DrawLine(new Vector2(x1, y), new Vector2(x2, y),
                //         brush, 2, style);
                // }
                RenderTarget.DrawLine(
            new SharpDX.Vector2(x1, y),
            new SharpDX.Vector2(x2, y),
            brush,
            2.0f,
            dottedStrokeStyle  // ← Just reference it
        );

                // Draw labels if enabled (RadiIQ: if showLab)
                if (ShowLabels)
                {
                    string labelText = line.IsLongBreakout ? "Break Up" : "Break Dn";
                    float labelY = line.IsLongBreakout ? y + 5 : y - 15; // Up label below line, Dn label above

                    RenderTarget.DrawText(labelText, smallTextFormat,
                        new RectangleF(x2 - 60, labelY, 60, 15),
                        brush, DrawTextOptions.None, MeasuringMode.Natural);
                }

                linesDrawn++;
            }

           

            // Dispose brushes
            greenBrush.Dispose();
            redBrush.Dispose();
        }

        /// <summary>
        /// Draw solid horizontal lines at Y1 pivot price levels
        /// Matching RadiIQ lines 3454 & 3681 (red/pink Y1 price lines)
        /// </summary>
        private void DrawPivotPriceLines(ChartControl chartControl, ChartScale chartScale, ZigZagState zz)
        {
            if (!trained || zz.PivotPriceLines.Count == 0)
                return;

            // Create red/pink brush for pivot price lines (matching RadiIQ color #F24968)
            var redBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                new SharpDX.Color(242, 73, 104, 255)); // #F24968 - pivot price level

            // Draw only the LAST pivot price line (most recent Y1)
            var line = zz.PivotPriceLines[zz.PivotPriceLines.Count - 1];

            // Check if times are valid (match DrawBreakoutLines validation pattern)
            if (line.X1 != default(DateTime) && (!line.X2.HasValue || line.X2.Value != default(DateTime)))
            {
                // Get screen coordinates
                int x1 = chartControl.GetXByTime(line.X1);

                // If X2 is null, extend to right edge of chart (current time)
                int x2 = line.X2.HasValue
                    ? chartControl.GetXByTime(line.X2.Value)
                    : chartControl.CanvasRight;

                float y = chartScale.GetYByValue(line.Y);

                // Check if coordinates are within valid bounds (handle off-chart times)
                // Skip lines from far off-chart positions that would cause visual glitches
                bool validX1 = x1 >= -5000 && x1 <= chartControl.CanvasRight + 5000;
                bool validX2 = x2 >= -5000 && x2 <= chartControl.CanvasRight + 5000;

                if (validX1 && validX2)
                {
                    // Check if line is visible in current viewport (improved check)
                    bool isVisible = (x2 >= 0 && x1 <= chartControl.CanvasRight) ||
                                     (x1 >= 0 && x1 <= chartControl.CanvasRight);

                    if (isVisible)
                    {
                        // Draw solid horizontal line at Y1 price
                        RenderTarget.DrawLine(
                            new SharpDX.Vector2(x1, y),
                            new SharpDX.Vector2(x2, y),
                            redBrush,
                            1.5f,  // Solid line, slightly thicker than regular
                            null   // No stroke style = solid line
                        );
                    }
                }
            }

            // Dispose brush
            redBrush.Dispose();
        }

        private void DrawZigZagLines(ChartControl chartControl, ChartScale chartScale,
            ZigZagState zz, SharpDX.Direct2D1.Brush brush, SharpDX.Direct2D1.Brush fadedBrush)
        {
           

            // Draw historical lines (DOTTED/FADED) from Lines list
            // These are confirmed pivots that have already reversed
            if (zz.Lines.Count > 0)
            {
                int startIndex = LastOnly ? Math.Max(0, zz.Lines.Count - 1) : 0;
                int linesDrawn = 0;

                for (int i = startIndex; i < zz.Lines.Count; i++)
                {
                    var line = zz.Lines[i];

                    int x1 = chartControl.GetXByTime(line.X1);
                    int x2 = chartControl.GetXByTime(line.X2);
                    float y1 = chartScale.GetYByValue(line.Y1);
                    float y2 = chartScale.GetYByValue(line.Y2);

                  

                    // Historical lines are ALWAYS dotted and faded
                    // Matches PineScript: zzLine.last().set_style(line.style_dotted) + set_color(color.new(lineCol, 50))
                    // TEMP DEBUG: Use Dash instead of Dot to make it more visible
                    using (var style = new StrokeStyle(Core.Globals.D2DFactory,
                        new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash }))
                    {
                        RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2),
                            fadedBrush, 3, style);  // Increased width to 3 for visibility
                    }
                    linesDrawn++;
                }

            
            }

            // Draw CurrentLine (SOLID/BRIGHT) - the developing pivot
            // This is the last pivot that is still forming and updates every bar
            // Matches PineScript: zzLine.last() which is updated with set_x2/set_y2
            if (zz.CurrentLine != null)
            {
                int x1 = chartControl.GetXByTime(zz.CurrentLine.X1);
                int x2 = chartControl.GetXByTime(zz.CurrentLine.X2);
                float y1 = chartScale.GetYByValue(zz.CurrentLine.Y1);
                float y2 = chartScale.GetYByValue(zz.CurrentLine.Y2);

                // Current line is SOLID and uses full-brightness brush
                // No dash style = solid line
                RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2),
                    brush, 2);
            }
        }

        private void DrawProjections(ChartControl chartControl, ChartScale chartScale,
            ZigZagState zz, int barsInProgress)
        {
            if (zz.Lines.Count == 0)
            {
                if (debugDrawCounter++ % 1000 == 0)
                    Print($"[DrawProjections] No lines to draw for BIP={barsInProgress}");
                return;
            }

            // Debug: Print projection info once
            if (debugDrawCounter++ == 1)
                Print($"[DrawProjections] Drawing {zz.Lines.Count} projection lines, BIP={barsInProgress}, Dir={zz.Direction}");

           

            // PineScript draws a polyline connecting ALL zigzag pivot points
            // This creates the dotted/semi-transparent projection line showing the zigzag pattern
            // polyline.new(CPLTF, xloc = xloc.bar_time, line_color = color.new(line_color1, 50),
            //              line_style = line.style_dotted, line_width = 2)

            // Create a semi-transparent brush (less transparent than before for better visibility)
            var baseColor = zz.Direction == 1 ?
                new SharpDX.Color(116, 255, 188, 200) :  // Green with 78% alpha (was 50%)
                new SharpDX.Color(255, 116, 116, 200);    // Red with 78% alpha (was 50%)

            using (var brush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, baseColor))
            using (var style = new StrokeStyle(Core.Globals.D2DFactory,
                new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dot }))
            {
                // Get visible time range to filter lines
                DateTime firstVisibleTime = chartControl.GetTimeByX(0);
                DateTime lastVisibleTime = chartControl.GetTimeByX((int)chartControl.ActualWidth);

                int linesDrawn = 0;
                int linesSkipped = 0;

                // Draw only recent lines (last 50) to avoid off-screen rendering
                int startIndex = Math.Max(0, zz.Lines.Count - 50);

                for (int i = startIndex; i < zz.Lines.Count; i++)
                {
                    var line = zz.Lines[i];

                    // Skip if line is completely before visible time range
                    if (line.X2 < firstVisibleTime)
                    {
                        linesSkipped++;
                        continue;
                    }

                    // Get screen coordinates for the zigzag line
                    int x1 = chartControl.GetXByTime(line.X1);
                    int x2 = chartControl.GetXByTime(line.X2);
                    float y1 = chartScale.GetYByValue(line.Y1);
                    float y2 = chartScale.GetYByValue(line.Y2);

                    // Debug first 3 visible lines
                    if (linesDrawn < 3 && debugDrawCounter < 5)
                    {
                        Print($"[Projection Line #{i}] Time: {line.X1:HH:mm}→{line.X2:HH:mm}, Price: {line.Y1:F2}→{line.Y2:F2}");
                        Print($"  Screen: X({x1},{x2}), Y({y1:F0},{y2:F0}), OnScreen={x2 >= 0}");
                    }

                    // Clamp x coordinates to visible area (don't skip, just clamp)
                    int chartWidth = (int)chartControl.ActualWidth;
                    x1 = Math.Max(-100, Math.Min(chartWidth + 100, x1)); // Allow small overflow
                    x2 = Math.Max(-100, Math.Min(chartWidth + 100, x2));

                    // Draw the dotted semi-transparent projection line segment (width=3 for visibility)
                    RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2),
                        brush, 3, style);

                    linesDrawn++;
                }

                // Debug summary
                if (debugDrawCounter < 5)
                    Print($"[DrawProjections] Drew {linesDrawn} lines, Skipped {linesSkipped} old, Total={zz.Lines.Count}");

                // FUTURE PROJECTION: Extend current zigzag trend into the future
                // This shows where the zigzag might continue based on current direction
                if (trained && zz.Y2Price != 0)
                {
                    // Get current time and calculate future time (project 20 bars ahead)
                    DateTime currentTime = Time[0];
                    TimeSpan barSpan = TimeSpan.FromMinutes(5); // 5-minute bars
                    DateTime futureTime = currentTime.AddMinutes(100); // ~20 bars ahead

                    // Get current Y2 position (last pivot)
                    double currentPrice = zz.Y2Price;

                    // Project forward following current trend
                    // If Direction == 1 (uptrend), project upward
                    // If Direction == -1 (downtrend), project downward
                    double projectionSlope = (zz.Y2Price - zz.Y1Price) / 10.0; // Gentle continuation
                    double futurePrice = currentPrice + (projectionSlope * 2); // Project 2x the last move

                    // Get screen coordinates
                    int xCurrent = chartControl.GetXByTime(currentTime);
                    int xFuture = chartControl.GetXByTime(futureTime);
                    float yCurrent = chartScale.GetYByValue(currentPrice);
                    float yFuture = chartScale.GetYByValue(futurePrice);

                    // Only draw if future point is on screen
                    if (xFuture > 0 && xFuture < chartControl.ActualWidth)
                    {
                        // Draw brighter projection line into future
                        var futureColor = zz.Direction == 1 ?
                            new SharpDX.Color(116, 255, 188, 255) :  // Bright green
                            new SharpDX.Color(255, 116, 116, 255);   // Bright red

                        using (var futureBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, futureColor))
                        {
                            RenderTarget.DrawLine(new Vector2(xCurrent, yCurrent), new Vector2(xFuture, yFuture),
                                futureBrush, 3, style);

                            if (debugDrawCounter < 5)
                                Print($"[Future Projection] From {currentTime:HH:mm} @ {currentPrice:F2} → {futureTime:HH:mm} @ {futurePrice:F2}, Dir={zz.Direction}");
                        }
                    }
                }


            }
        }

        private void DrawFibometer(ChartScale chartScale, ChartControl chartControl)
        {
            if (zzHTF.Lines.Count == 0) return;

            double y1 = zzHTF.Y1Price;
            double y2 = zzHTF.Y2Price;

            if (y1 == 0 || y2 == 0) return;

            bool isHigh = y2 > y1;
            double newRange = Math.Abs(y2 - y1);

            // PineScript Lines 3233-3236: Calculate Fibonacci levels from Y2
            double fib618 = isHigh ? y2 - newRange * 0.618 : y2 + newRange * 0.618;
            double fib382 = isHigh ? y2 - newRange * 0.382 : y2 + newRange * 0.382;
            double fib236 = isHigh ? y2 - newRange * 0.236 : y2 + newRange * 0.236;
            double fib786 = isHigh ? y2 - newRange * 0.786 : y2 + newRange * 0.786;
            double fib50 = (y1 + y2) / 2.0;

            int xBase = (int)chartControl.GetXByBarIndex(ChartBars, ChartBars.ToIndex) + 32;

            // Determine border color
            bool condUpH = zzHTF.Y2Price > zzHTF.Y1Price;
            bool condUpL = zzLTF.Y2Price > zzLTF.Y1Price;

            SharpDX.Direct2D1.Brush borderBrush;
            if ((condUpH && condUpL) || inTradeLong > 0)
                borderBrush = projectionBrushLongDX;
            else if ((!condUpH && !condUpL) || inTradeShort < 0)
                borderBrush = projectionBrushShortDX;
            else
                borderBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
                    new SharpDX.Color((byte)128, (byte)116, (byte)255, (byte)255));

            // Draw fib levels (use newRange for box sizing)
            DrawFibBox(chartScale, xBase, fib382, newRange * 0.025, ".382", borderBrush);
            DrawFibBox(chartScale, xBase, fib618, newRange * 0.025, ".618", borderBrush);

            if (Fibometer)
            {
                DrawFibBox(chartScale, xBase, fib236, newRange * 0.025, ".236", borderBrush);
                DrawFibBox(chartScale, xBase, fib786, newRange * 0.025, ".786", borderBrush);
                DrawFibBox(chartScale, xBase, fib50, newRange * 0.025, ".5", borderBrush);
            }

            // Draw current price indicator
            double currentPrice = Close[0];
            float yPrice = chartScale.GetYByValue(currentPrice);
            float boxHeight = Math.Abs(chartScale.GetYByValue(currentPrice - newRange * 0.05) - yPrice);

            RenderTarget.FillRectangle(new RectangleF(xBase - 8, yPrice, 28, boxHeight),
                fibBackgroundBrushDX);
            RenderTarget.DrawRectangle(new RectangleF(xBase - 8, yPrice, 28, boxHeight),
                borderBrush, 1);

            RenderTarget.DrawText($"{currentPrice:F2}",
                smallTextFormat, new RectangleF(xBase - 6, yPrice + 2, 24, boxHeight - 4),
                whiteTextBrushDX, DrawTextOptions.None, MeasuringMode.Natural);
        }

        private void DrawFibBox(ChartScale chartScale, int x, double price, double heightOffset,
            string label, SharpDX.Direct2D1.Brush borderBrush)
        {
            float y = chartScale.GetYByValue(price);
            float boxHeight = Math.Abs(chartScale.GetYByValue(price - heightOffset) - y);

            RenderTarget.FillRectangle(new RectangleF(x, y, 32, boxHeight), fibBackgroundBrushDX);
            RenderTarget.DrawRectangle(new RectangleF(x, y, 32, boxHeight), borderBrush, 1);

            RenderTarget.DrawText(label, smallTextFormat,
                new RectangleF(x + 2, y + 2, 28, boxHeight - 2), whiteTextBrushDX,
                DrawTextOptions.None, MeasuringMode.Natural);
        }

        private void DrawTradeMarkers(ChartControl chartControl, ChartScale chartScale)
        {
            if (RenderTarget == null || tradeMarkers.Count == 0) return;

            // Create brushes for triangles (matching PineScript colors)
            var longEntryBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(116, 255, 188, 255)); // Bright green #74ffbc (entry)
            var shortEntryBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(255, 116, 116, 255)); // Red/pink (entry)
            var exitBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(128, 116, 255, 255)); // Purple/violet (exit)
            var textBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(255, 255, 255, 200)); // White with slight transparency
            var bgBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(0, 0, 0, 180)); // Semi-transparent black background

            // Create text format for tooltip
            var textFormat = new SharpDX.DirectWrite.TextFormat(Core.Globals.DirectWriteFactory,
                "Arial", SharpDX.DirectWrite.FontWeight.Normal,
                SharpDX.DirectWrite.FontStyle.Normal, 9.0f);

            try
            {
                foreach (var marker in tradeMarkers)
                {
                    // Only draw if the marker is within visible chart area
                    int slotIndex = ChartBars.GetBarIdxByTime(chartControl, marker.Time);
                    if (slotIndex < 0 || slotIndex >= ChartBars.Count) continue;

                    float x = chartControl.GetXByBarIndex(ChartBars, slotIndex);
                    float y = chartScale.GetYByValue(marker.Price);

                    // Draw triangle (size based on LabelSize - using 12 as default)
                    float triangleSize = 12f;

                    if (marker.IsEntry)
                    {
                        // ENTRY MARKERS
                        if (marker.IsLong)
                        {
                            // UP TRIANGLE (▲) for long entry - below bar - GREEN
                            DrawTriangle(x, y, triangleSize, true, longEntryBrush);
                            DrawTooltipLabel(x + 15, y + 5, marker.Tooltip, textBrush, bgBrush, textFormat, longEntryBrush);
                        }
                        else
                        {
                            // DOWN TRIANGLE (▼) for short entry - above bar - RED
                            DrawTriangle(x, y, triangleSize, false, shortEntryBrush);
                            DrawTooltipLabel(x + 15, y - 80, marker.Tooltip, textBrush, bgBrush, textFormat, shortEntryBrush);
                        }
                    }
                    else
                    {
                        // EXIT MARKERS (PineScript: purple/violet color for both long and short exits)
                        if (marker.IsLong)
                        {
                            // DOWN TRIANGLE (▼) for long exit - above bar - PURPLE
                            DrawTriangle(x, y, triangleSize, false, exitBrush);
                            DrawTooltipLabel(x + 15, y - 80, marker.Tooltip, textBrush, bgBrush, textFormat, exitBrush);
                        }
                        else
                        {
                            // UP TRIANGLE (▲) for short exit - below bar - PURPLE
                            DrawTriangle(x, y, triangleSize, true, exitBrush);
                            DrawTooltipLabel(x + 15, y + 5, marker.Tooltip, textBrush, bgBrush, textFormat, exitBrush);
                        }
                    }
                }
            }
            finally
            {
                longEntryBrush?.Dispose();
                shortEntryBrush?.Dispose();
                exitBrush?.Dispose();
                textBrush?.Dispose();
                bgBrush?.Dispose();
                textFormat?.Dispose();
            }
        }

        private void DrawTooltipLabel(float x, float y, string text,
            SharpDX.Direct2D1.SolidColorBrush textBrush,
            SharpDX.Direct2D1.SolidColorBrush bgBrush,
            SharpDX.DirectWrite.TextFormat textFormat,
            SharpDX.Direct2D1.SolidColorBrush borderBrush)
        {
            if (RenderTarget == null || string.IsNullOrEmpty(text)) return;

            // Measure text size
            var textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory,
                text, textFormat, 300, 150);

            float textWidth = textLayout.Metrics.Width;
            float textHeight = textLayout.Metrics.Height;

            // Draw background box with border
            var bgRect = new SharpDX.RectangleF(x - 5, y - 5, textWidth + 10, textHeight + 10);
            RenderTarget.FillRectangle(bgRect, bgBrush);
            RenderTarget.DrawRectangle(bgRect, borderBrush, 1.5f);

            // Draw text
            RenderTarget.DrawTextLayout(new SharpDX.Vector2(x, y), textLayout, textBrush);

            textLayout.Dispose();
        }

        private void DrawTriangle(float x, float y, float size, bool pointUp, SharpDX.Direct2D1.SolidColorBrush brush)
        {
            if (RenderTarget == null) return;

            // Create triangle points
            SharpDX.Vector2[] points;

            if (pointUp)
            {
                // Up triangle: ▲ (apex at top)
                points = new SharpDX.Vector2[]
                {
                    new SharpDX.Vector2(x, y - size),           // Top point
                    new SharpDX.Vector2(x - size/2, y),         // Bottom left
                    new SharpDX.Vector2(x + size/2, y)          // Bottom right
                };
            }
            else
            {
                // Down triangle: ▼ (apex at bottom)
                points = new SharpDX.Vector2[]
                {
                    new SharpDX.Vector2(x, y + size),           // Bottom point
                    new SharpDX.Vector2(x - size/2, y),         // Top left
                    new SharpDX.Vector2(x + size/2, y)          // Top right
                };
            }

            // Create path geometry for filled triangle
            var factory = Core.Globals.D2DFactory;
            var geometry = new SharpDX.Direct2D1.PathGeometry(factory);
            var sink = geometry.Open();

            sink.BeginFigure(points[0], SharpDX.Direct2D1.FigureBegin.Filled);
            sink.AddLine(points[1]);
            sink.AddLine(points[2]);
            sink.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
            sink.Close();

            // Fill the triangle
            RenderTarget.FillGeometry(geometry, brush);

            // Clean up
            sink.Dispose();
            geometry.Dispose();
        }

        private void DrawPerformanceTable(ChartControl chartControl)
        {
            if (!trained) return;

            float x = 10, y = 10;
            float tableWidth = 280;
            float tableHeight = TradeLong && TradeShort ? 120 : 90;

            // Position based on setting
            switch (TablePosition)
            {
                case TablePositionEnum.TopRight:
                    x = (float)chartControl.ActualWidth - tableWidth - 10;
                    y = 10;
                    break;
                case TablePositionEnum.TopLeft:
                    x = 10;
                    y = 10;
                    break;
                case TablePositionEnum.BottomRight:
                    x = (float)chartControl.ActualWidth - tableWidth - 10;
                    y = (float)chartControl.ActualHeight - tableHeight - 10;
                    break;
                case TablePositionEnum.BottomLeft:
                    x = 10;
                    y = (float)chartControl.ActualHeight - tableHeight - 10;
                    break;
                case TablePositionEnum.TopCenter:
                    x = ((float)chartControl.ActualWidth - tableWidth) / 2;
                    y = 10;
                    break;
                case TablePositionEnum.MiddleCenter:
                    x = ((float)chartControl.ActualWidth - tableWidth) / 2;
                    y = ((float)chartControl.ActualHeight - tableHeight) / 2;
                    break;
            }

            // Draw background
            RenderTarget.FillRectangle(new RectangleF(x, y, tableWidth, tableHeight), fibBackgroundBrushDX);
            RenderTarget.DrawRectangle(new RectangleF(x, y, tableWidth, tableHeight), fibBorderBrushDX, 1);

            // Create text format if needed
            if (textFormat == null || textFormat.IsDisposed)
            {
                textFormat = new SharpDX.DirectWrite.TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", 11);
            }

            // Create brushes for colors
            var goldBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(242, 184, 7, 255)); // #F2B807
            var greenBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(116, 255, 188, 255)); // #74ffbc
            var redBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color(255, 116, 116, 255)); // #ff7474

            float yOffset = y + 10;
            float lineHeight = 25;

            // Row 0: Title
            RenderTarget.DrawText("Impulse IQ", textFormat,
                new RectangleF(x + 10, yOffset, tableWidth - 20, lineHeight), goldBrush,
                DrawTextOptions.None, MeasuringMode.Natural);
            yOffset += lineHeight;

            if (TradeLong)
            {
                // Row 1: Longs Performance
                RenderTarget.DrawText("Longs Performance", textFormat,
                    new RectangleF(x + 10, yOffset, 150, lineHeight), greenBrush,
                    DrawTextOptions.None, MeasuringMode.Natural);

                // CRITICAL FIX: Display OPTIMIZATION PF, not live trading PF
                // The arrays get reset and updated during live trading, corrupting the optimization results
                // We saved the optimization PF before resetting, so display that instead
                double longPF = optimizationPFLong;

                RenderTarget.DrawText("Profit Factor", textFormat,
                    new RectangleF(x + 160, yOffset, 60, lineHeight), greenBrush,
                    DrawTextOptions.None, MeasuringMode.Natural);

                RenderTarget.DrawText(longPF.ToString("F2"), textFormat,
                    new RectangleF(x + 220, yOffset, 50, lineHeight), whiteTextBrushDX,
                    DrawTextOptions.None, MeasuringMode.Natural);

                yOffset += lineHeight;
            }

            if (TradeShort)
            {
                // Row 2: Shorts Performance
                RenderTarget.DrawText("Shorts Performance", textFormat,
                    new RectangleF(x + 10, yOffset, 150, lineHeight), redBrush,
                    DrawTextOptions.None, MeasuringMode.Natural);

                // CRITICAL FIX: Display OPTIMIZATION PF, not live trading PF
                // The arrays get reset and updated during live trading, corrupting the optimization results
                // We saved the optimization PF before resetting, so display that instead
                double shortPF = optimizationPFShort;

                RenderTarget.DrawText("Profit Factor", textFormat,
                    new RectangleF(x + 160, yOffset, 60, lineHeight), redBrush,
                    DrawTextOptions.None, MeasuringMode.Natural);

                RenderTarget.DrawText(shortPF.ToString("F2"), textFormat,
                    new RectangleF(x + 220, yOffset, 50, lineHeight), whiteTextBrushDX,
                    DrawTextOptions.None, MeasuringMode.Natural);

                yOffset += lineHeight;
            }

            // Row 3: Best Parameters
            string paramText = $"Best: LTF={bestATRLTF:F1} HTF={bestATRHTF:F1}";
            RenderTarget.DrawText(paramText, textFormat,
                new RectangleF(x + 10, yOffset, tableWidth - 20, lineHeight), whiteTextBrushDX,
                DrawTextOptions.None, MeasuringMode.Natural);

            // Dispose temporary brushes
            goldBrush.Dispose();
            greenBrush.Dispose();
            redBrush.Dispose();
        }

        public override void OnRenderTargetChanged()
        {
            // Dispose old resources
            if (ltfBrushDX != null) { ltfBrushDX.Dispose(); ltfBrushDX = null; }
            if (htfBrushDX != null) { htfBrushDX.Dispose(); htfBrushDX = null; }
            if (ltfBrushDXFaded != null) { ltfBrushDXFaded.Dispose(); ltfBrushDXFaded = null; }
            if (htfBrushDXFaded != null) { htfBrushDXFaded.Dispose(); htfBrushDXFaded = null; }
            if (projectionBrushLongDX != null) { projectionBrushLongDX.Dispose(); projectionBrushLongDX = null; }
            if (projectionBrushShortDX != null) { projectionBrushShortDX.Dispose(); projectionBrushShortDX = null; }
            if (fibBorderBrushDX != null) { fibBorderBrushDX.Dispose(); fibBorderBrushDX = null; }
            if (fibBackgroundBrushDX != null) { fibBackgroundBrushDX.Dispose(); fibBackgroundBrushDX = null; }
            if (textBrushDX != null) { textBrushDX.Dispose(); textBrushDX = null; }
            if (whiteTextBrushDX != null) { whiteTextBrushDX.Dispose(); whiteTextBrushDX = null; }
            if (breakoutBrushDX != null) { breakoutBrushDX.Dispose(); breakoutBrushDX = null; }
            if (textFormat != null) { textFormat.Dispose(); textFormat = null; }
            if (smallTextFormat != null) { smallTextFormat.Dispose(); smallTextFormat = null; }

            foreach (var brush in iqMeterBrushesHTF)
            {
                if (brush != null && !brush.IsDisposed)
                    brush.Dispose();
            }
            iqMeterBrushesHTF.Clear();

            foreach (var brush in iqMeterBrushesLTF)
            {
                if (brush != null && !brush.IsDisposed)
                    brush.Dispose();
            }
            iqMeterBrushesLTF.Clear();

            needsIQMeterRefresh = true;
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ImpulseIQ[] cacheImpulseIQ;
		public ImpulseIQ ImpulseIQ(StrategyTypeEnum strategyType, bool tradeLong, bool tradeShort, bool closeAtEOD, double minimumATRMultiplier, bool useRR, double rRMultiple, double stopLossAmount, double buySellRange, bool fibometer, bool showProjection, bool showLabels, bool showHTFZZ, int lTFMinutes, int hTFMinutes, Brush lTFLineBrush, Brush hTFLineBrush, bool lastOnly, TablePositionEnum tablePosition, LabelSizeEnum labelSize)
		{
			return ImpulseIQ(Input, strategyType, tradeLong, tradeShort, closeAtEOD, minimumATRMultiplier, useRR, rRMultiple, stopLossAmount, buySellRange, fibometer, showProjection, showLabels, showHTFZZ, lTFMinutes, hTFMinutes, lTFLineBrush, hTFLineBrush, lastOnly, tablePosition, labelSize);
		}

		public ImpulseIQ ImpulseIQ(ISeries<double> input, StrategyTypeEnum strategyType, bool tradeLong, bool tradeShort, bool closeAtEOD, double minimumATRMultiplier, bool useRR, double rRMultiple, double stopLossAmount, double buySellRange, bool fibometer, bool showProjection, bool showLabels, bool showHTFZZ, int lTFMinutes, int hTFMinutes, Brush lTFLineBrush, Brush hTFLineBrush, bool lastOnly, TablePositionEnum tablePosition, LabelSizeEnum labelSize)
		{
			if (cacheImpulseIQ != null)
				for (int idx = 0; idx < cacheImpulseIQ.Length; idx++)
					if (cacheImpulseIQ[idx] != null && cacheImpulseIQ[idx].StrategyType == strategyType && cacheImpulseIQ[idx].TradeLong == tradeLong && cacheImpulseIQ[idx].TradeShort == tradeShort && cacheImpulseIQ[idx].CloseAtEOD == closeAtEOD && cacheImpulseIQ[idx].MinimumATRMultiplier == minimumATRMultiplier && cacheImpulseIQ[idx].UseRR == useRR && cacheImpulseIQ[idx].RRMultiple == rRMultiple && cacheImpulseIQ[idx].StopLossAmount == stopLossAmount && cacheImpulseIQ[idx].BuySellRange == buySellRange && cacheImpulseIQ[idx].Fibometer == fibometer && cacheImpulseIQ[idx].ShowProjection == showProjection && cacheImpulseIQ[idx].ShowLabels == showLabels && cacheImpulseIQ[idx].ShowHTFZZ == showHTFZZ && cacheImpulseIQ[idx].LTFMinutes == lTFMinutes && cacheImpulseIQ[idx].HTFMinutes == hTFMinutes && cacheImpulseIQ[idx].LTFLineBrush == lTFLineBrush && cacheImpulseIQ[idx].HTFLineBrush == hTFLineBrush && cacheImpulseIQ[idx].LastOnly == lastOnly && cacheImpulseIQ[idx].TablePosition == tablePosition && cacheImpulseIQ[idx].LabelSize == labelSize && cacheImpulseIQ[idx].EqualsInput(input))
						return cacheImpulseIQ[idx];
			return CacheIndicator<ImpulseIQ>(new ImpulseIQ(){ StrategyType = strategyType, TradeLong = tradeLong, TradeShort = tradeShort, CloseAtEOD = closeAtEOD, MinimumATRMultiplier = minimumATRMultiplier, UseRR = useRR, RRMultiple = rRMultiple, StopLossAmount = stopLossAmount, BuySellRange = buySellRange, Fibometer = fibometer, ShowProjection = showProjection, ShowLabels = showLabels, ShowHTFZZ = showHTFZZ, LTFMinutes = lTFMinutes, HTFMinutes = hTFMinutes, LTFLineBrush = lTFLineBrush, HTFLineBrush = hTFLineBrush, LastOnly = lastOnly, TablePosition = tablePosition, LabelSize = labelSize }, input, ref cacheImpulseIQ);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ImpulseIQ ImpulseIQ(StrategyTypeEnum strategyType, bool tradeLong, bool tradeShort, bool closeAtEOD, double minimumATRMultiplier, bool useRR, double rRMultiple, double stopLossAmount, double buySellRange, bool fibometer, bool showProjection, bool showLabels, bool showHTFZZ, int lTFMinutes, int hTFMinutes, Brush lTFLineBrush, Brush hTFLineBrush, bool lastOnly, TablePositionEnum tablePosition, LabelSizeEnum labelSize)
		{
			return indicator.ImpulseIQ(Input, strategyType, tradeLong, tradeShort, closeAtEOD, minimumATRMultiplier, useRR, rRMultiple, stopLossAmount, buySellRange, fibometer, showProjection, showLabels, showHTFZZ, lTFMinutes, hTFMinutes, lTFLineBrush, hTFLineBrush, lastOnly, tablePosition, labelSize);
		}

		public Indicators.ImpulseIQ ImpulseIQ(ISeries<double> input , StrategyTypeEnum strategyType, bool tradeLong, bool tradeShort, bool closeAtEOD, double minimumATRMultiplier, bool useRR, double rRMultiple, double stopLossAmount, double buySellRange, bool fibometer, bool showProjection, bool showLabels, bool showHTFZZ, int lTFMinutes, int hTFMinutes, Brush lTFLineBrush, Brush hTFLineBrush, bool lastOnly, TablePositionEnum tablePosition, LabelSizeEnum labelSize)
		{
			return indicator.ImpulseIQ(input, strategyType, tradeLong, tradeShort, closeAtEOD, minimumATRMultiplier, useRR, rRMultiple, stopLossAmount, buySellRange, fibometer, showProjection, showLabels, showHTFZZ, lTFMinutes, hTFMinutes, lTFLineBrush, hTFLineBrush, lastOnly, tablePosition, labelSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ImpulseIQ ImpulseIQ(StrategyTypeEnum strategyType, bool tradeLong, bool tradeShort, bool closeAtEOD, double minimumATRMultiplier, bool useRR, double rRMultiple, double stopLossAmount, double buySellRange, bool fibometer, bool showProjection, bool showLabels, bool showHTFZZ, int lTFMinutes, int hTFMinutes, Brush lTFLineBrush, Brush hTFLineBrush, bool lastOnly, TablePositionEnum tablePosition, LabelSizeEnum labelSize)
		{
			return indicator.ImpulseIQ(Input, strategyType, tradeLong, tradeShort, closeAtEOD, minimumATRMultiplier, useRR, rRMultiple, stopLossAmount, buySellRange, fibometer, showProjection, showLabels, showHTFZZ, lTFMinutes, hTFMinutes, lTFLineBrush, hTFLineBrush, lastOnly, tablePosition, labelSize);
		}

		public Indicators.ImpulseIQ ImpulseIQ(ISeries<double> input , StrategyTypeEnum strategyType, bool tradeLong, bool tradeShort, bool closeAtEOD, double minimumATRMultiplier, bool useRR, double rRMultiple, double stopLossAmount, double buySellRange, bool fibometer, bool showProjection, bool showLabels, bool showHTFZZ, int lTFMinutes, int hTFMinutes, Brush lTFLineBrush, Brush hTFLineBrush, bool lastOnly, TablePositionEnum tablePosition, LabelSizeEnum labelSize)
		{
			return indicator.ImpulseIQ(input, strategyType, tradeLong, tradeShort, closeAtEOD, minimumATRMultiplier, useRR, rRMultiple, stopLossAmount, buySellRange, fibometer, showProjection, showLabels, showHTFZZ, lTFMinutes, hTFMinutes, lTFLineBrush, hTFLineBrush, lastOnly, tablePosition, labelSize);
		}
	}
}

#endregion
