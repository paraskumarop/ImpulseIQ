// //pinescript indicator below
// // This Pine Script™ code is subject to the terms of the Mozilla Public License 2.0 at https://mozilla.org/MPL/2.0/
// // © KioseffTrading

// //@version=6
// indicator("Impulse IQ [Trading IQ]", overlay=true, max_lines_count=500, max_labels_count=500, calc_bars_count = 100000, max_boxes_count = 500,  max_polylines_count = 100)
// // 

// ATR = ta.atr(14)

// tradeTypeMaster = input.string(defval = "Breakout", title = "Strategy Type", options = ["Breakout", "Cheap"], group = "Learning Settings")

// sort            = "PF"  // Sorting parameter for internal logic

// tradeLong       = input.bool(defval = true, title = "Trade Long", group = "Learning Settings", 
//                              tooltip = "Enable or disable long trades (buy positions).")

// tradeShort      = input.bool(defval = true, title = "Trade Short", group = "Learning Settings", 
//                              tooltip = "Enable or disable short trades (sell positions).")

// eodClose        = input.bool(defval = false, title = "Close All Positions At End Of Session",group = "Learning Settings", tooltip = "Select whether to exit all active positions at the end of the trading session.")

// min             = input.float(defval = 0.4, minval = 0.4, title = "Minimum Stop Loss And Trailing Target Distance", 
//                              group = "Learning Settings", 
//                              tooltip = "Sets the minimum distance for stop loss and trailing target using ATR.")

// useRR           = input.bool(defval = false, title = "Use R Multiple (RR)", inline = "RR", 
//                              group = "R Multiple Settings (Optional)", 
//                              tooltip = "Enable to use R Multiple (Reward-to-Risk Ratio) for setting profit targets. For example, '3' sets a profit target 3 times greater than the stop loss (3:1 reward-risk ratio), where as '1' sets a profit target equal in distance to the stop loss. Diabling this setting forces Impulse IQ to exit all trades at a a trailing stop loss with no fixed profit target.")

// RRmult          = input.float(defval = 2, title = "", minval = 0.25, inline = "RR", 
//                              group = "R Multiple Settings (Optional)", 
//                              tooltip = "Sets the R Multiple for calculating profit targets. For example, '3' sets a profit target 3 times greater than the stop loss (3:1 reward-risk ratio)."),                                                                import KioseffTrading/TRADINGIQIMPULSEAUTOLIB__dsfa89sdf435j123_DSAF__PRODUCTION/48 as ZZCRP

// stopLossAmount  = input.float(defval = 100, minval = 0.001, title = "$ Stop Loss Amount (Does Not Affect Stop Loss)", 
//                              group = "Ideal Amount", 
//                              tooltip = "This value is used to calculate theideal number of coins/contracts/shares to trade for the asset but does not affect the actual stop loss placement. Useful for sizing and risk management.")

// fibometer       = input.bool(defval = false, title = "Fibometer", 
//                              group = "Aesthetics", 
//                              tooltip = "Enable to display Fibonacci levels on the chart for potential support and resistance zones.")

// showProj        = input.bool(defval = true, title = "Show Zig Zag Projection", 
//                              group = "Aesthetics", 
//                              tooltip = "Show projected Zig Zag lines to anticipate future price movements.")

// showLab         = input.bool(defval = false, title = "Show Breakout Labels", 
//                              group = "Aesthetics", 
//                              tooltip = "Display breakout labels on the chart to highlight potential breakout zones.")

// showHTFzz       = input.bool(defval = true, title = "Show HTF Zig Zag", 
//                              group = "Aesthetics", 
//                              tooltip = "Show Higher Time Frame (HTF) Zig Zag lines for a broader view of market trends.")

// buffer          = 0  // Buffer value for internal calculations

// buySellRange    = 100  // Range for buy/sell signals

// var tfltf       = timeframe.from_seconds(timeframe.in_seconds(input.timeframe(defval = "", 
//                              title = "LTF Zig Zag", 
//                              group = "Zig Zag Settings", 
//                              inline = "ZZ1", 
//                              tooltip = "Select the lower time frame for the Zig Zag indicator to refine entry and exit points.")))

// line_color1     = input(title="", defval=#14D990, 
//                              group = "Zig Zag Settings", 
//                              inline = "ZZ1", 
//                              tooltip = "Choose the color for the Lower Time Frame (LTF) Zig Zag lines.")

// var tfhtf       = timeframe.from_seconds(timeframe.in_seconds(input.timeframe(defval = "", 
//                              title = "HTF Zig Zag", 
//                              group = "Zig Zag Settings", 
//                              inline = "ZZ2", 
//                              tooltip = "Select the higher time frame for the Zig Zag indicator to view the broader trend.")))

// line_color2     = input(title="", defval=#6929F2, 
//                              group = "Zig Zag Settings", 
//                              inline = "ZZ2", 
//                              tooltip = "Choose the color for the Higher Time Frame (HTF) Zig Zag lines.")

// var endLoop     = 99  // End loop value for internal logic

// atr             = ta.atr(14)  // Average True Range (ATR) calculation

// labelSize       = input.string(defval = "Small", title = "Label Size", 
//                              options = ["Auto", "Tiny", "Small", "Normal", "Large", "Huge"], 
//                              tooltip = "Select the size for labels displayed on the chart.")

// lastOnly        = input.bool(defval = false, title = "Only Show Last Zig Zag Pivot", 
//                              tooltip = "Enable to show only the last pivot point of the Zig Zag indicator for cleaner chart visualization.")

// tablePlace1   = input.string(defval = "Top Right", title = "Performance Table", group = "Aesthetics", options = ["Top Right", "Middle Right", "Bottom Right", "Top Center", "Middle Center", "Bottom Center", "Top Left", "Middle Left", "Bottom Left", "None"], inline = "Tables P")
// tableSize1    = input.string(defval = "Normal", title = "", group = "Aesthetics", options = ["Tiny", "Small", "Normal", "Large"], inline = "Tables P")


// var tableLoc1 = switch tablePlace1 

//     "Top Right" => position.top_right
//     "Middle Right" => position.middle_right
//     "Bottom Right" => position.bottom_right
//     "Top Center" => position.top_center
//     "Middle Center"  => position.middle_center
//     "Bottom Center"  => position.bottom_center
//     "Top Left"  => position.top_left
//     "Middle Left" => position.middle_left
//     "Bottom Left" => position.bottom_left
//     =>               position.top_right


// var tableTxtSize1 = switch tableSize1

//     "Tiny"   => size.tiny
//     "Small"  => size.small 
//     "Normal" => size.normal
//     "Large"  => size.large



// var labSize = switch labelSize

// 	"Auto"   => size.auto
// 	"Tiny"   => size.tiny
// 	"Small"  => size.small
// 	"Normal" => size.normal
// 	"Large"  => size.large 
// 	"Huge"   => size.huge


// [getLTFclo, getLTFclo1, atrLTF, atrLTFEnd]  = request.security(syminfo.tickerid, tfltf, [close, close[1], atr, atr])
// [getHTFclo, getHTFclo1, htfATR, atrHTFEnd]  = request.security(syminfo.tickerid, tfhtf, [close, close[1], ta.atr(14), atr])

// isLastBar     = time(tfltf) != time(tfltf, -1)
// var ltfCloArr = array.new<float>(), var ltfCloArr1 = array.new<float>()

// ltfCloArr.push(getLTFclo), ltfCloArr1.push(getLTFclo1)

// var openArrEnd = array.new<float>(), var highArrEnd = array.new<float>(), var closeArrEnd = array.new<float>(),var closeArrEnd1 = array.new<float>(), var lowArrEnd = array.new<float>(), var timeArrEnd = array.new<int>(), var ohlc4ArrEnd = array.new<float>()
// openArrEnd.push(open), closeArrEnd.push(close), closeArrEnd1.push(close[1]), lowArrEnd.push(low), highArrEnd.push(high), timeArrEnd.push(time), ohlc4ArrEnd.push(ohlc4)
// var closer2lowArr = array.new<bool>()

// closer2lowArr.push(math.abs(open - low) < math.abs(high - open))

// var atrArrLTF = array.new<float>(), var atrArrHTF = array.new<float>()
// closer2low = math.abs(open - low) < math.abs(high - open)

// var isLastBarArray = array.new<bool>()
// isLastBarArray.push(session.islastbar_regular)

// var trained = false 

// if barstate.islastconfirmedhistory
//     trained := true

// atrArrLTF.push(atrLTFEnd)
// atrArrHTF.push(atrHTFEnd)

// var historicalShortWins     = array.new<float>(1, 0)
// var historicalShortPFPFORIT = array.new<float>(1, 0) 
// var historicalShortPFLOSS   = array.new<float>(1, 0) 
// var historicalShortTrades   = array.new<float>(1, 0)

// var historicalLongsWins     = array.new<float>(1, 0)
// var historicalLongsPFPFORIT = array.new<float>(1, 0)
// var historicalLongsPFLOSS   = array.new<float>(1, 0)
// var historicalLongsTrades   = array.new<float>(1, 0) 

// var float bestLongsIndexNow = na 
// var float bestShortsIndexNow = na

// optimize(hlw, hlpfp, hlpfl, hlpft, hsw, hspfp, hspfl, hspft) => 

//     var atrPTarr = array.new_float(), var atrTarr = array.new_float(),  var zzCurrATR = array.new_float(), var zzHTFATR = array.new_float(), var stringArr = array.new_string()
    
//     if barstate.isfirst

//         count  = 0.5
//         countx = 0.5 
//         for i = 0 to 10
//             countx := 1 
//             count  += 0.5
//             for x = 0 to 9
//                 countx += 0.5
//                 zzCurrATR.push(count)
//                 zzHTFATR .push(countx)
 

//         countTarget   = min - 0.5
//         countTrailing = min - 0.5

//         for i = 0 to 2

//             countTarget   += 1
//             countTrailing := min - 0.5

//             for x = 0 to 7

//                 countTrailing += 0.5
//                 atrPTarr.push(countTarget)
//                 atrTarr .push(countTrailing)

//         for i = 0 to zzCurrATR.size() - 1
    
//             for x = 0 to atrTarr.size() - 1
    
//                 stringArr.push(str.tostring(zzCurrATR.get(i)) + "_" + str.tostring(zzHTFATR.get(i)) + "_" + str.tostring(atrPTarr.get(x)) + "_" + str.tostring(atrTarr.get(x))) 


//     var arrSize     = stringArr.size()
//     var perfArr     = array.new_float(arrSize, 0), var entryArr   = array.new_float(arrSize, 0), var limitArr    = array.new_float(arrSize, 0), var exitArr    = array.new_float(arrSize, 0), var triggerArr = array.new_float(arrSize, 0)
//     var tradesArr   = array.new_float(arrSize, 0), var winsArr    = array.new_float(arrSize, 0), var PFprofitArr = array.new_float(arrSize, 0), var PFlossArr  = array.new_float(arrSize, 0), var boolArr    = array.new_int(arrSize, 0)

//     var newSize     = arrSize

//     var RRarr       = array.new_float(newSize, na)
//     var divArr      = array.new_int  (newSize, 1)

//     var RRarrS      = array.new_float(newSize, na)
//     var divArrS     = array.new_int  (newSize, 1)

//     var perfArrS    = array.new_float(arrSize, 0), var entryArrS   = array.new_float(arrSize, 0), var limitArrS    = array.new_float(arrSize, 0), var exitArrS    = array.new_float(arrSize, 0), var triggerArrS = array.new_float(arrSize, 0)
//     var tradesArrS  = array.new_float(arrSize, 0), var winsArrS    = array.new_float(arrSize, 0), var PFprofitArrS = array.new_float(arrSize, 0), var PFlossArrS  = array.new_float(arrSize, 0), var boolArrS    = array.new_int(arrSize, 0)


//     ZZCRP.optiZZentryExit( isLastBar,  true,    boolArr,
//     			 	  entryArr,  atrLTF,  triggerArr,  exitArr,  boolArrS,  entryArrS, 
//     				 	  triggerArrS,  exitArrS, PFlossArr,  limitArr,  PFprofitArr,
//     					 	   PFlossArrS,  limitArrS,  PFprofitArrS, zzCurrATR, atrTarr, divArr, useRR, RRarr, closer2low, divArrS, RRarrS, eodClose)

//     var y1PriceArrMaster = array.new_float(arrSize, close), var y2PriceArrMaster = array.new_float(arrSize, 0), var masterDirArr = array.new_int(arrSize, 0), var y1PriceArrMasterHTF = array.new_float(arrSize, close), var y2PriceArrMasterHTF = array.new_float(arrSize, 0), var masterDirArrHTF = array.new_int(arrSize, 0), var pointArr = array.new_float(arrSize, close), var pointArrHTF = array.new_float(arrSize, close), var getBreakoutPointUpArr = array.new_float(arrSize, 0), var getBreakoutPointDnArr = array.new_float(arrSize, 0), //var getBreakoutPointUpArrTrail = array.new_float(arrSize, 0), var getBreakoutPointDnArrTrail = array.new_float(arrSize, 0)

//     getBreakoutPointDnArrTrail = getBreakoutPointDnArr.copy()
//     getBreakoutPointUpArrTrail = getBreakoutPointUpArr.copy()

//     var zzSize = zzCurrATR.size()

//     ZZCRP.zzOpti(
//              atrLTF, 
//              zzCurrATR,
//              y2PriceArrMaster,
//              y1PriceArrMaster,
//              masterDirArr,
//              true, 
//              getBreakoutPointUpArr, 
//              getBreakoutPointDnArr, 
//              zzSize,
//              true
//              )  

//     ZZCRP.zzOpti(
//                  htfATR, 
//                  zzHTFATR, 
//                  y2PriceArrMasterHTF, 
//                  y1PriceArrMasterHTF, 
//                  masterDirArrHTF, 
//                  false, 
//                  getBreakoutPointUpArr, 
//                  getBreakoutPointDnArr, 
//                  zzSize,
//                  true
//                  )  


//     ZZCRP.optimizeZZ( isLastBar, 
//                      true ,  
//                      tradeTypeMaster,  
//                      boolArr,
//     	             y1PriceArrMasterHTF,
//     	 	         masterDirArrHTF,    
//                      getBreakoutPointUpArrTrail, 
//     		 	     getBreakoutPointDnArrTrail,    
//                      getLTFclo, 
//     			 	 entryArr,  
//                      atrLTF,  
//                      getLTFclo1,  
//                      triggerArr,  
//                      exitArr,  
//                      boolArrS,  
//                      tradeShort,  
//                      entryArrS, 
//     				 triggerArrS,  
//                      exitArrS, 
//                      y2PriceArrMasterHTF, 
//                      buySellRange, 
//                      zzCurrATR, atrTarr, atrPTarr, useRR, RRmult, RRarr, RRarrS, divArr, divArrS
//                      )

//     var y1PriceFinalLTF = array.new_float(2, 0), var y2PriceFinalLTF = array.new_float(2, 0), var y1xFinalLTF     = array.new_int  (1, 0), var y2xFinalLTF     = array.new_int  (1, 0), var zzLineFinalLTF = array.new_line(), var zzLineChangeFinalLTF = array.new_line(1), var y1PriceHistoryLTF = array.new<float>(), var y2PriceHistoryLTF = array.new<float>(), var isHighFirstLongArrLTF = array.new<bool>(), var getBreakoutPointUpArrLongLTF = array.new<float>()
//     var y1PriceFinalHTF = array.new_float(2, 0), var y2PriceFinalHTF = array.new_float(2, 0), var y1xFinalHTF     = array.new_int  (1, 0), var y2xFinalHTF     = array.new_int  (1, 0), var zzLineFinalHTF = array.new_line(), var zzLineChangeFinalHTF = array.new_line(1), var y1PriceHistoryHTF = array.new<float>(), var y2PriceHistoryHTF = array.new<float>(), var isHighFirstLongArrHTF = array.new<bool>(), var getBreakoutPointUpArrLongHTF = array.new<float>()

//     var y1PriceFinalLTFS = array.new_float(2, 0), var y2PriceFinalLTFS = array.new_float(2, 0), var y1xFinalLTFS     = array.new_int  (1, 0), var y2xFinalLTFS     = array.new_int  (1, 0), var zzLineFinalLTFS = array.new_line(), var zzLineChangeFinalLTFS = array.new_line(1), var y1PriceHistoryLTFS = array.new<float>(), var y2PriceHistoryLTFS = array.new<float>(), var isHighFirstShortArrLTF = array.new<bool>(), var getBreakoutPointDnArrShortLTF = array.new<float>()
//     var y1PriceFinalHTFS = array.new_float(2, 0),var y2PriceFinalHTFS = array.new_float(2, 0),var y1xFinalHTFS     = array.new_int  (1, 0),var y2xFinalHTFS     = array.new_int  (1, 0),var zzLineFinalHTFS = array.new_line(),var zzLineChangeFinalHTFS = array.new_line(1),var y1PriceHistoryHTFS = array.new<float>(),var y2PriceHistoryHTFS = array.new<float>(),var isHighFirstShortArrHTF = array.new<bool>(),var getBreakoutPointDnArrShortHTF = array.new<float>()

//     [bestATRlongltf ,  bestATRlonghtf ,bestTarget ,  bestTrailing ,bestTargetS ,  bestTrailingS ,bestATRshortltf ,  bestATRshorthtf, zzltfpL ,zzltftL ,zzltfdL    ,zzhtfpL ,zzhtftL ,zzhtfdL    ,zzltfpS ,zzltftS ,zzltfdS    ,zzhtfpS ,zzhtftS ,zzhtfdS,
//           tableString, tableStringS, tablePF, tablePFS,  entryLong, exitLong, inTradeLong, limitLong, triggerLong, entryShort, exitShort, inTradeShort, limitShort, triggerShort, divLong, divShort, RRtpLong, RRtpShort, ltfPoly, htfPoly, bestLongsIndex, bestShortsIndex] =

//          ZZCRP.lastBarOpti( isLastBar, 
//     				 	     PFlossArr,     PFprofitArr,
//     					 	   PFlossArrS,  PFprofitArrS,
//     						 	  sort,  stringArr,  tradeLong,  line_color1,  line_color2,
//                                    closeArrEnd,  highArrEnd,  lowArrEnd,  timeArrEnd,  atrArrLTF,  atrArrHTF,
//                                       y2PriceFinalHTF,  y2xFinalHTF,  y1PriceFinalHTF,  buffer, 
//     	 	                           y1xFinalHTF,
//     		                          	  isHighFirstLongArrHTF, 
//     		                          	 	  getBreakoutPointUpArrLongHTF,  y2PriceHistoryHTF,  y1PriceHistoryHTF,  getBreakoutPointDnArrShortLTF,  buySellRange,  tradeShort,  ltfCloArr,  ltfCloArr1,   ohlc4ArrEnd,  y2PriceFinalHTFS,  y2xFinalHTFS,  y1PriceFinalHTFS,  y1xFinalHTFS,
//     		 	                               y2PriceHistoryHTFS, y1PriceHistoryHTFS,  isHighFirstShortArrHTF, 
//     			 	                              getBreakoutPointDnArrShortHTF,  y2PriceFinalLTFS,  y2xFinalLTFS,  y1PriceFinalLTFS,  
//     	 	                          y1xFinalLTFS,
//     		 	  y2PriceHistoryLTFS, y1PriceHistoryLTFS,  isHighFirstShortArrLTF,  y2PriceFinalLTF,  y2xFinalLTF,  y1PriceFinalLTF, 
//     	 	   y1xFinalLTF,
//     		 	  y2PriceHistoryLTF, y1PriceHistoryLTF,  isHighFirstLongArrLTF, 
//     			 	  getBreakoutPointUpArrLongLTF,  closeArrEnd1,  openArrEnd,  showLab, hlpfp, hlpfl, hspfp, hspfl,tradeTypeMaster, true, closer2lowArr, useRR, RRmult, closer2lowArr, stopLossAmount, labSize, eodClose, isLastBarArray)



//     [bestATRlongltf ,  bestATRlonghtf ,bestTarget ,  bestTrailing ,bestTargetS ,  bestTrailingS ,bestATRshortltf ,  bestATRshorthtf, zzltfpL ,zzltftL ,zzltfdL    ,zzhtfpL ,zzhtftL ,zzhtfdL    ,zzltfpS ,zzltftS ,zzltfdS    ,zzhtfpS ,zzhtftS ,zzhtfdS,  tableString, tableStringS,  tablePF, tablePFS, 
//      y1xFinalLTF, y1PriceFinalLTF, y2xFinalLTF, y2PriceFinalLTF, y1xFinalHTF, y1PriceFinalHTF, y2xFinalHTF, y2PriceFinalHTF,
//      y1xFinalLTFS, y1PriceFinalLTFS, y2xFinalLTFS, y2PriceFinalLTFS, y1xFinalHTFS, y1PriceFinalHTFS, y2xFinalHTFS, y2PriceFinalHTFS, getBreakoutPointUpArrLongLTF, getBreakoutPointDnArrShortLTF, entryLong, exitLong, inTradeLong, limitLong, triggerLong, entryShort, exitShort, inTradeShort, limitShort, triggerShort, divLong, divShort, RRtpLong, RRtpShort, ltfPoly, htfPoly, bestLongsIndex, bestShortsIndex]




// [bestATRlongltf ,  bestATRlonghtf ,bestTarget ,  bestTrailing ,bestTargetS ,  bestTrailingS ,bestATRshortltf ,  bestATRshorthtf, zzltfpL ,zzltftL ,zzltfdL    ,zzhtfpL ,zzhtftL ,zzhtfdL    ,zzltfpS ,zzltftS ,zzltfdS    ,zzhtfpS ,zzhtftS ,zzhtfdS,  tableString, tableStringS, tablePF,  tablePFS, y1xFinalLTF, y1PriceFinalLTF, y2xFinalLTF, y2PriceFinalLTF, y1xFinalHTF, y1PriceFinalHTF, y2xFinalHTF, y2PriceFinalHTF, y1xFinalLTFS, y1PriceFinalLTFS, y2xFinalLTFS, y2PriceFinalLTFS, y1xFinalHTFS, y1PriceFinalHTFS, y2xFinalHTFS, y2PriceFinalHTFS, getBreakoutPointUpArrLongLTF, getBreakoutPointDnArrShortLTF,
//      entryLong, exitLong, inTradeLong, limitLong, triggerLong, entryShort, exitShort, inTradeShort, limitShort, triggerShort, divLong, divShort, RRtpLong, RRtpShort, ltfPoly, htfPoly, bestLongsIndex, bestShortsIndex] =
//      optimize(historicalLongsWins ,historicalLongsPFPFORIT,historicalLongsPFLOSS  ,historicalLongsTrades,  historicalShortWins    , historicalShortPFPFORIT, historicalShortPFLOSS  , historicalShortTrades)

// if not na(bestLongsIndex)
//     bestLongsIndexNow := bestLongsIndex
// if not na(bestShortsIndex)
//     bestShortsIndexNow := bestShortsIndex

// // DEBUG: Log optimization results
// if not na(bestLongsIndexNow) or not na(bestShortsIndexNow)
//     log.info("========================================")
//     log.info("[PINESCRIPT] OPTIMIZATION RESULTS:")
//     log.info("Chart TF: " + str.tostring(timeframe.period))
//     log.info("LTF: " + tfltf + ", HTF: " + tfhtf)
//     log.info("")
//     if not na(bestLongsIndexNow)
//         log.info("BEST LONG Parameters:")
//         log.info("  Index: " + str.tostring(bestLongsIndexNow) + " of 2640")
//         log.info("  LTF_ATR=" + str.tostring(bestATRlongltf) + ", HTF_ATR=" + str.tostring(bestATRlonghtf) + ", Target=" + str.tostring(bestTarget) + ", Trailing=" + str.tostring(bestTrailing))
//         log.info("  Profit Factor: " + str.tostring(na(tablePF) ? 0 : tablePF))
//         log.info("")
//     if not na(bestShortsIndexNow)
//         log.info("BEST SHORT Parameters:")
//         log.info("  Index: " + str.tostring(bestShortsIndexNow) + " of 2640")
//         log.info("  LTF_ATR=" + str.tostring(bestATRshortltf) + ", HTF_ATR=" + str.tostring(bestATRshorthtf) + ", Target=" + str.tostring(bestTargetS) + ", Trailing=" + str.tostring(bestTrailingS))
//         log.info("  Profit Factor: " + str.tostring(na(tablePFS) ? 0 : tablePFS))
//     log.info("========================================")
//     log.info(str.tostring(bestLongsIndexNow) + "  " + str.tostring(bestShortsIndexNow))


// var bestStringEnd = "Breakout"
// var bestStringEndS = "Breakout"

// var bestATRlongltfEnd                = 0.
// var bestATRlonghtfEnd                = 0.
// var bestTargetEnd                    = 0.
// var bestTrailingEnd                  = 0.
// var zzltfpLEnd                       = 0.
// var zzltftLEnd                       = 0
// var zzltfdLEnd                       = 0
// var zzhtfpLEnd                       = 0.
// var zzhtftLEnd                       = 0
// var zzhtfdLEnd                       = 0
// var tableStringNEnd                  = matrix.new<string>()
// var tablePFNEnd                      = array.new<float>()
// var tableWRNEnd                      = array.new<float>()
// var tablePERFNEnd                    = array.new<float>()
// var y1xFinalLTFEnd                   = array.new<int>()
// var y1PriceFinalLTFEnd               = array.new<float>()
// var y2xFinalLTFEnd                   = array.new<int>()
// var y2PriceFinalLTFEnd               = array.new<float>()
// var y1xFinalHTFEnd                   = array.new<int>()
// var y1PriceFinalHTFEnd               = array.new<float>()
// var y2xFinalHTFEnd                   = array.new<int>()
// var y2PriceFinalHTFEnd               = array.new<float>()
// var getBreakoutPointUpArrLongLTFEnd  = array.new<float>()
// var entryLongEnd                     = 0.
// var exitLongEnd                      = 0.
// var inTradeLongEnd                   = 0.
// var limitLongEnd                     = 0.
// var triggerLongEnd                   = 0.
// var divLongEnd                       = 0 
// var RRtpLongEnd                      = 0.


// var bestATRshortltfEndS               = 0.
// var bestATRshorthtfEndS               = 0.
// var bestTargetSEndS                   = 0.
// var bestTrailingSEndS                 = 0.
// var zzltfpSEndS                       = 0.
// var zzltftSEndS                       = 0
// var zzltfdSEndS                       = 0
// var zzhtfpSEndS                       = 0.
// var zzhtftSEndS                       = 0
// var zzhtfdSEndS                       = 0
// var tableStringNSEndS                 = matrix.new<string>()
// var tablePFNSEndS                     = array.new<float>()
// var tableWRNSEndS                     = array.new<float>()
// var tablePERFNSEndS                   = array.new<float>()
// var y1xFinalLTFEndS                   = array.new<int>()
// var y1PriceFinalLTFEndS               = array.new<float>()
// var y2xFinalLTFEndS                   = array.new<int>()
// var y2PriceFinalLTFEndS               = array.new<float>()
// var y1xFinalHTFEndS                   = array.new<int>()
// var y1PriceFinalHTFEndS               = array.new<float>()
// var y2xFinalHTFEndS                   = array.new<int>()
// var y2PriceFinalHTFEndS               = array.new<float>()
// var getBreakoutPointDnArrShortLTFEndS = array.new<float>()
// var entryShortEndS                    = 0.
// var exitShortEndS                     = 0.
// var inTradeShortEndS                  = 0.
// var limitShortEndS                    = 0.
// var triggerShortEndS                  = 0.
// var divShortEnd                       = 0 
// var RRtpShortEnd                      = 0.

// var trainedConvert = false 

// if barstate.islastconfirmedhistory

//     bestATRlongltfEnd   := bestATRlongltf
//     bestATRlonghtfEnd   := bestATRlonghtf
//     bestTargetEnd       := bestTarget 
//     bestTrailingEnd     := bestTrailing
//     zzltfpLEnd          := zzltfpL
//     zzltftLEnd          := zzltftL
//     zzltfdLEnd          := zzltfdL
//     zzhtfpLEnd          := zzhtfpL
//     zzhtftLEnd          := zzhtftL
//     zzhtfdLEnd          := zzhtfdL
//     tableStringNEnd     := tableString.copy()
//     tablePFNEnd         := tablePF.copy()
//     y1xFinalLTFEnd      :=  y1xFinalLTF.copy()
//     y1PriceFinalLTFEnd  :=  y1PriceFinalLTF.copy()
//     y2xFinalLTFEnd      :=  y2xFinalLTF.copy()
//     y2PriceFinalLTFEnd  :=  y2PriceFinalLTF.copy()
//     y1xFinalHTFEnd      :=  y1xFinalHTF.copy()
//     y1PriceFinalHTFEnd  :=  y1PriceFinalHTF.copy()
//     y2xFinalHTFEnd      :=  y2xFinalHTF.copy()
//     y2PriceFinalHTFEnd  :=  y2PriceFinalHTF.copy()
//     getBreakoutPointUpArrLongLTFEnd :=  getBreakoutPointUpArrLongLTF.copy()
//     entryLongEnd   := entryLong
//     exitLongEnd    := exitLong
//     inTradeLongEnd := inTradeLong 
//     limitLongEnd   := limitLong
//     triggerLongEnd := triggerLong

//     divLongEnd  := divLong
//     RRtpLongEnd := RRtpLong

//     bestATRshortltfEndS := bestATRshortltf
//     bestATRshorthtfEndS := bestATRshorthtf
//     bestTargetSEndS     := bestTargetS
//     bestTrailingSEndS   := bestTrailingS
//     zzltfpSEndS         := zzltfpS
//     zzltftSEndS         := zzltftS
//     zzltfdSEndS         := zzltfdS
//     zzhtfpSEndS         := zzhtfpS
//     zzhtftSEndS         := zzhtftS
//     zzhtfdSEndS         := zzhtfdS
//     tableStringNSEndS   := tableStringS.copy()
//     tablePFNSEndS       := tablePFS.copy()
//     y1xFinalLTFEndS     := y1xFinalLTFS.copy()
//     y1PriceFinalLTFEndS := y1PriceFinalLTFS.copy()
//     y2xFinalLTFEndS     := y2xFinalLTFS.copy()
//     y2PriceFinalLTFEndS := y2PriceFinalLTFS.copy()
//     y1xFinalHTFEndS     := y1xFinalHTFS.copy()
//     y1PriceFinalHTFEndS := y1PriceFinalHTFS.copy()
//     y2xFinalHTFEndS     := y2xFinalHTFS.copy()
//     y2PriceFinalHTFEndS := y2PriceFinalHTFS.copy()
//     bestStringEndS      := ""
//     getBreakoutPointDnArrShortLTFEndS := getBreakoutPointDnArrShortLTF.copy()
//     entryShortEndS   := entryShort
//     exitShortEndS    := exitShort
//     inTradeShortEndS := inTradeShort 
//     limitShortEndS   := limitShort
//     triggerShortEndS := triggerShort

//     divShortEnd  := divShort
//     RRtpShortEnd := RRtpShort

//     trainedConvert := true



// if not na(tablePF) and barstate.isconfirmed and barstate.islast

//     bestATRlongltfEnd   := bestATRlongltf
//     bestATRlonghtfEnd   := bestATRlonghtf
//     bestTargetEnd       := bestTarget 
//     bestTrailingEnd     := bestTrailing
//     zzltfpLEnd          := zzltfpL
//     zzltftLEnd          := zzltftL
//     zzltfdLEnd          := zzltfdL
//     zzhtfpLEnd          := zzhtfpL
//     zzhtftLEnd          := zzhtftL
//     zzhtfdLEnd          := zzhtfdL
//     tableStringNEnd     := tableString.copy()
//     tablePFNEnd         := tablePF.copy()
//     y1xFinalLTFEnd      :=  y1xFinalLTF.copy()
//     y1PriceFinalLTFEnd  :=  y1PriceFinalLTF.copy()
//     y2xFinalLTFEnd      :=  y2xFinalLTF.copy()
//     y2PriceFinalLTFEnd  :=  y2PriceFinalLTF.copy()
//     y1xFinalHTFEnd      :=  y1xFinalHTF.copy()
//     y1PriceFinalHTFEnd  :=  y1PriceFinalHTF.copy()
//     y2xFinalHTFEnd      :=  y2xFinalHTF.copy()
//     y2PriceFinalHTFEnd  :=  y2PriceFinalHTF.copy()
//     getBreakoutPointUpArrLongLTFEnd :=  getBreakoutPointUpArrLongLTF.copy()

//     divLongEnd  := divLong
//     RRtpLongEnd := RRtpLong


//     bestATRshortltfEndS := bestATRshortltf
//     bestATRshorthtfEndS := bestATRshorthtf
//     bestTargetSEndS     := bestTargetS
//     bestTrailingSEndS   := bestTrailingS
//     zzltfpSEndS         := zzltfpS
//     zzltftSEndS         := zzltftS
//     zzltfdSEndS         := zzltfdS
//     zzhtfpSEndS         := zzhtfpS
//     zzhtftSEndS         := zzhtftS
//     zzhtfdSEndS         := zzhtfdS
//     tableStringNSEndS   := tableStringS.copy()
//     tablePFNSEndS       := tablePFS.copy()
//     y1xFinalLTFEndS     := y1xFinalLTFS.copy()
//     y1PriceFinalLTFEndS := y1PriceFinalLTFS.copy()
//     y2xFinalLTFEndS     := y2xFinalLTFS.copy()
//     y2PriceFinalLTFEndS := y2PriceFinalLTFS.copy()
//     y1xFinalHTFEndS     := y1xFinalHTFS.copy()
//     y1PriceFinalHTFEndS := y1PriceFinalHTFS.copy()
//     y2xFinalHTFEndS     := y2xFinalHTFS.copy()
//     y2PriceFinalHTFEndS := y2PriceFinalHTFS.copy()
//     bestStringEndS      := ""
//     getBreakoutPointDnArrShortLTFEndS := getBreakoutPointDnArrShortLTF.copy()

//     divShortEnd  := divShort
//     RRtpShortEnd := RRtpShort



// var finalLONGPFPROFIT  = array.new<float>(1, 0) 
// var finalWRLONG        = array.new<float>(1, 0) 
// var finalSHORTPFPROFIT = array.new<float>(1, 0) 
// var finalWRSHORT       = array.new<float>(1, 0) 
// var finalTLONG         = array.new<float>(1, 0) 
// var finalTSHORT        = array.new<float>(1, 0) 
// var finalLONGPFLOSS    = array.new<float>(1, 0)
// var finalSHORTPFLOSS   = array.new<float>(1, 0)
// var y1xLTFL            = array.new<int>(1)
// var y2xLTFL            = array.new<int>(1)
// var y1PriceLTFL        = array.new<float>(1)
// var y2PriceLTFL        = array.new<float>(1)
// var y1xHTFL            = array.new<int>(1)
// var y2xHTFL            = array.new<int>(1)
// var y1PriceHTFL        = array.new<float>(1)
// var y2PriceHTFL        = array.new<float>(1)
// var y1xLTFS            = array.new<int>(1)
// var y2xLTFS            = array.new<int>(1)
// var y1PriceLTFS        = array.new<float>(1)
// var y2PriceLTFS        = array.new<float>(1)
// var y1xHTFS            = array.new<int>(1)
// var y2xHTFS            = array.new<int>(1)
// var y1PriceHTFS        = array.new<float>(1)
// var y2PriceHTFS        = array.new<float>(1)

// // DEBUG: Log ATR usage for LTF zigzag (first bar only after training)
// if barstate.isconfirmed and bar_index == 1
//     log.info("[PINESCRIPT] LTF ZigZag ATR: Raw atrLTF=" + str.tostring(atrLTF) + ", Multiplier=" + str.tostring(bestATRlongltfEnd) + ", Final=" + str.tostring(bestATRlongltfEnd * atrLTF))
//
// ZZCRP.zz(bestATRlongltfEnd * atrLTF,
//              y1xLTFL,
//              y2xLTFL,
//              y1PriceLTFL,
//              y2PriceLTFL,
//              line_color1,
//              zzltfpLEnd,
//              zzltftLEnd,
//              zzltfdLEnd,
//              y1xFinalLTFEnd,
//              y1PriceFinalLTFEnd,
//              y2xFinalLTFEnd,
//              y2PriceFinalLTFEnd, 
//              true, 
//              showProj, 
//              trained
//              ) 

// // DEBUG: Log ATR usage for HTF zigzag (first bar only after training)
// if barstate.isconfirmed and bar_index == 1
//     log.info("[PINESCRIPT] HTF ZigZag ATR: Raw htfATR=" + str.tostring(htfATR) + ", Multiplier=" + str.tostring(bestATRlonghtfEnd) + ", Final=" + str.tostring(bestATRlonghtfEnd * htfATR))
//
// ZZCRP.zz(bestATRlonghtfEnd * htfATR,
//              y1xHTFL,
//              y2xHTFL,
//              y1PriceHTFL,
//              y2PriceHTFL,
//              line_color2,
//              zzhtfpLEnd,
//              zzhtftLEnd,
//              zzhtfdLEnd,
//              y1xFinalHTFEnd,
//              y1PriceFinalHTFEnd,
//              y2xFinalHTFEnd,
//              y2PriceFinalHTFEnd,
//              true,
//              showProj,
//              trained
//              ) 

// ZZCRP.zzS(bestATRshortltfEndS * atrLTF,  
//              y1xLTFS,  
//              y2xLTFS,  
//              y1PriceLTFS,
//               y2PriceLTFS, 
//              zzltfpSEndS, 
//              zzltftSEndS, 
//              zzltfdSEndS, 
//              y1xFinalLTFEndS, 
//              y1PriceFinalLTFEndS, 
//              trained 
//              ) 

// ZZCRP.zzS(bestATRshorthtfEndS * htfATR,  
//              y1xHTFS,  
//              y2xHTFS,  
//              y1PriceHTFS,
//               y2PriceHTFS, 
//              zzhtfpSEndS, 
//              zzhtftSEndS, 
//              zzhtfdSEndS, 
//              y1xFinalHTFEndS, 
//              y1PriceFinalHTFEndS, 
//              trained) 


// isHighFirstLong    = false        
// isHighFirst2Long   = false       
// isHighFirstShort   = false       
// isHighFirst2Short  = false     

// isHighFirstLong        := y2PriceLTFL.first() > y1PriceLTFL.first()
// isHighFirst2Long       := y2PriceHTFL.first() > y1PriceHTFL.first()
// isHighFirstShort       := y2PriceLTFS.first() > y1PriceLTFS.first()
// isHighFirst2Short      := y2PriceHTFS.first() > y1PriceHTFS.first()



// [inTradeLongFinal, inTradeShortFinal, longStop, shortStop, longTP, shortTP, tradeEntryLong, tradeEntryShort, rrTPlong, rrTPshort, getDivLong, getDivShort] = ZZCRP.liveTrades( y1PriceLTFL,  y2PriceLTFL,  y1PriceLTFS,  y2PriceLTFS,  y1xLTFL,  y1xLTFS,
//       isLastBar,   isHighFirst2Long,  tradeLong,  y2PriceHTFL,  y1PriceHTFL,  buySellRange,  getLTFclo,
//           getLTFclo1,  bestTrailingEnd,  bestTargetEnd,  atrLTF,  isHighFirst2Short,  tradeShort,  y2PriceHTFS,  y1PriceHTFS,
//               bestTrailingSEndS,  bestTargetSEndS,  showLab, 
//                  finalLONGPFPROFIT, finalLONGPFLOSS, 
//                  finalSHORTPFPROFIT, finalSHORTPFLOSS, getBreakoutPointUpArrLongLTFEnd, getBreakoutPointDnArrShortLTFEndS,
//                      exitLongEnd, exitShortEndS, triggerLongEnd, triggerShortEndS, inTradeLongEnd, entryLongEnd, entryShortEndS, inTradeShortEndS, tradeTypeMaster,  RRtpLongEnd, divLongEnd, RRtpShortEnd, divShortEnd, RRmult, useRR, closer2low, stopLossAmount, labSize, eodClose)

// var longEntry = 0.
// var shortEntry = 0.

// longEntry := tradeEntryLong
// shortEntry := tradeEntryShort

// if barstate.islastconfirmedhistory

//     if tradeLong


//         finalLONGPFPROFIT.set(0, historicalLongsPFPFORIT.first())
//         finalLONGPFLOSS  .set(0, historicalLongsPFLOSS.first())


//     if tradeShort


//         finalSHORTPFPROFIT.set(0,  historicalShortPFPFORIT.first())
//         finalSHORTPFLOSS  .set(0,  historicalShortPFLOSS.first())

// if barstate.islast

//     var tableLongs = table.new(tableLoc1, 99, 99, 
//                          bgcolor      = #20222C, 
//                          border_color = #363843, 
//                          frame_color  = #363843, 
//                          border_width = 1, 
//                          frame_width  = 1
//                          )


//     if tradeLong

//         tableLongs.cell(0, 0, text = "Impulse IQ",  text_color = #F2B807, text_size = tableTxtSize1)

//         if tradeShort 

//             tableLongs.merge_cells(0, 0, 2, 0)
            
//         tableLongs.cell(0, 1, text = "Longs Performance",  text_color = #74ffbc, text_size = tableTxtSize1)
//         tableLongs.cell(0, 2, text = "Profit Factor", text_color = #74ffbc, text_size = tableTxtSize1)
//         tableLongs.cell(0, 3, text = str.tostring(tablePFNEnd.first(), "###,###.##"), text_color = color.white, text_size = tableTxtSize1)

//     if tradeShort

//         if not tradeLong

//             var tableShort = table.new(tableLoc1, 99, 99, bgcolor =  #20222C, 
//                                                  border_color = #363843, 
//                                                  frame_color  = #363843, 
//                                                  border_width = 1, 
//                                                  frame_width  = 1 
//                                                  )

//             tableShort.cell(0, 0, text = "Impulse IQ",  text_color = #F2B807, text_size = tableTxtSize1)

//             tableShort.cell(0, 1, text = "Shorts Performance", text_color = color.rgb(255, 116, 116), text_size = tableTxtSize1)
//             tableShort.cell(0, 2, text = "Profit Factor", text_color = color.rgb(255, 116, 116), text_size = tableTxtSize1)

//             tableShort.cell(0, 3, text = str.tostring(tablePFNSEndS.first(), "###,###.##"), 
//                                                  text_color = color.white, text_size = tableTxtSize1
//                                                  )
//             if tablePlace1 == "None"
//                 tableShort.delete()
//         else 


//             tableLongs.cell(1, 1, text = "Shorts Performance", text_color = color.rgb(255, 116, 116), text_size = tableTxtSize1)
//             tableLongs.cell(1, 2, text = "Profit Factor", text_color = color.rgb(255, 116, 116), text_size = tableTxtSize1)

//             tableLongs.cell(1, 3, text = str.tostring(tablePFNSEndS.first(), "###,###.##"), 
//                                              text_color = color.white, text_size = tableTxtSize1
//                                              )
        


//     if tablePlace1 == "None"
//         tableLongs.delete()

 

// longAlertEntryImpulse(float entryPrice, float target, float stop) =>
    
//     message = "Impulse IQ [Trading IQ]\nSymbol: {symbol}\n{type}" + "\nTF: " + timeframe.period + "\nLong Entry At: {entry} \n🎯 Trailing Stop Conversion Target: {target}\n🛑 Initial Stop: {stop}"

//     newAlert = message

//     newAlert := str.replace(newAlert, "{entry}", str.tostring(entryPrice, format.mintick))
//     newAlert := str.replace(newAlert, "{target}", str.tostring(target, format.mintick))
//     newAlert := str.replace(newAlert, "{stop}", str.tostring(stop, format.mintick))
//     newAlert := str.replace(newAlert, "{type}", tradeTypeMaster)
//     newAlert := str.replace(newAlert, "{symbol}", syminfo.ticker)

//     alert(newAlert, alert.freq_once_per_bar)



// shortAlertEntryImpulse(float entryPrice, float target, float stop) =>
//     message = "Impulse IQ [Trading IQ]\nSymbol: {symbol}\n{type}" + "\nTF: " + timeframe.period + "\nShort Entry At: {entry} \n🎯 Trailing Stop Conversion Target: {target}\n🛑 Initial Stop: {stop}"

//     newAlert = message

//     newAlert := str.replace(newAlert, "{entry}", str.tostring(entryPrice, format.mintick))
//     newAlert := str.replace(newAlert, "{target}", str.tostring(target, format.mintick))
//     newAlert := str.replace(newAlert, "{stop}", str.tostring(stop, format.mintick))
//     newAlert := str.replace(newAlert, "{type}", tradeTypeMaster)
//     newAlert := str.replace(newAlert, "{symbol}", syminfo.ticker)

//     alert(newAlert, alert.freq_once_per_bar)

// entryImpulseLongConversion(float stop) =>
//     message = "Impulse IQ [Trading IQ]\nSymbol: {symbol}\n{type}" + "\nTF: " + timeframe.period + "\nLong Conversion Target Hit\n🛑 Trailing Stop Start: {stop}"

//     newAlert = message

//     newAlert := str.replace(newAlert, "{type}", tradeTypeMaster)
//     newAlert := str.replace(newAlert, "{stop}", str.tostring(stop, format.mintick))
//     newAlert := str.replace(newAlert, "{symbol}", syminfo.ticker)

//     alert(newAlert, alert.freq_once_per_bar)

// entryImpulseShortConversion(float stop) =>
//     message = "Impulse IQ [Trading IQ]\nSymbol: {symbol}\n{type}" + "\nTF: " + timeframe.period + "\nShort Conversion Target Hit\n🛑 Trailing Stop Start: {stop}"

//     newAlert = message

//     newAlert := str.replace(newAlert, "{stop}", str.tostring(stop, format.mintick))
//     newAlert := str.replace(newAlert, "{type}", tradeTypeMaster)
//     newAlert := str.replace(newAlert, "{symbol}", syminfo.ticker)

//     alert(newAlert, alert.freq_once_per_bar)

// varip longTrail  = 0

// if inTradeLongFinal == 1 and longTrail != 1
//     longAlertEntryImpulse(tradeEntryLong, longTP, longStop)
//     longEntry := tradeEntryLong
//     longTrail := 1

// if inTradeLongFinal == 2 and longTrail != 2
//     entryImpulseLongConversion(longStop)
//     longTrail := 2


// if inTradeLongFinal == 0 and longTrail != 0

//     getExit = math.min(open, longStop)

//     isprofit = switch 

//         getExit > longEntry => true 
//         => false

//     alertTxt = switch isprofit

//         true => "Impulse IQ [Trading IQ]\nSymbol: " + syminfo.ticker + "\n" + tradeTypeMaster + "\nTF: " + timeframe.period + "\nLong Exit At: " + str.tostring(getExit, format.mintick) + "\n✅ Profit: " + str.tostring((getExit / longEntry - 1) * 100, format.percent) + "\nOriginal Entry Price: " + str.tostring(longEntry, format.mintick)
//         =>      "Impulse IQ [Trading IQ]\nSymbol: " + syminfo.ticker + "\n" + tradeTypeMaster + "\nTF: " + timeframe.period + "\nLong Exit At: " + str.tostring(getExit, format.mintick) + "\n❌ Loss: "   + str.tostring((getExit / longEntry - 1) * 100, format.percent) + "\nOriginal Entry Price: " + str.tostring(longEntry, format.mintick)

//     alert(alertTxt, alert.freq_once_per_bar)
//     longTrail := 0


// varip shortTrail = 0

// if inTradeShortFinal == -1 and shortTrail != -1
//     shortAlertEntryImpulse(tradeEntryShort, shortTP, shortStop)
//     shortEntry := tradeEntryShort
//     shortTrail := -1

// if inTradeShortFinal == -2 and shortTrail != -2
//     entryImpulseShortConversion(shortStop)
//     shortTrail := -2

// if inTradeShortFinal == 0 and shortTrail != 0

//     getExit = math.max(open, shortStop)

//     isprofit = switch 

//         getExit < shortEntry => true 
//         => false
    
//     alertTxt = switch isprofit

//         true => "Impulse IQ [Trading IQ]\nSymbol: " + syminfo.ticker + "\n" + tradeTypeMaster + "\nTF: " + timeframe.period + "\nShort Exit At: " + str.tostring(getExit, format.mintick) + "\n✅ Profit: " + str.tostring((getExit / shortEntry - 1) * 100 * -1, format.percent) + "\nOriginal Entry Price: " + str.tostring(shortEntry, format.mintick)
//         =>      "Impulse IQ [Trading IQ]\nSymbol: " + syminfo.ticker + "\n" + tradeTypeMaster + "\nTF: " + timeframe.period + "\nShort Exit At: " + str.tostring(getExit, format.mintick) + "\n❌ Loss: "   + str.tostring((getExit / shortEntry - 1) * 100 * -1, format.percent) + "\nOriginal Entry Price: " + str.tostring(shortEntry, format.mintick)

//     alert(alertTxt, alert.freq_once_per_bar)
//     shortTrail := 0

// plot(tradeEntryLong, title = "[Automation] Long Entry Price", show_last = 1, display = display.data_window)
// plot(tradeEntryShort, title = "[Automation] Short Entry Price", show_last = 1, display = display.data_window)
// plot(useRR ? rrTPlong : float(na), title = "[Automation] RR Profit Target Long (Optional)", show_last = 1, display = display.data_window)
// plot(longStop, title = "[Automation] Initial Stop And Trailing Stop Price Long", show_last = 1, display = display.data_window)

// plot(useRR ? rrTPshort : float(na), title = "[Automation] RR Profit Target Short (Optional)", show_last = 1, display = display.data_window)
// plot(shortStop, title = "[Automation] Initial Stop And Trailing Stop Price Short", show_last = 1, display = display.data_window)


// getIdealFloat(stopLossAmount, entry, stop) => 

// 	ideal = stopLossAmount / (math.abs(entry - stop))

//     if syminfo.type == "futures"

//         ideal := math.floor(ideal)

//     ideal

// idealLong  = getIdealFloat(stopLossAmount, tradeEntryLong, longStop)
// idealShort = getIdealFloat(stopLossAmount, tradeEntryShort, shortStop)

// plot(idealLong , title = "Ideal Amount Long Position", display = display.data_window)
// plot(idealShort, title = "Ideal Amount Short Position", display = display.data_window)

// alertcondition(close < -1, "--------LONG MKT--------") 

// alertcondition(inTradeLongFinal[1]  == 0 and inTradeLongFinal  == 1 , title = "[MKT] Long Entry")
// alertcondition(inTradeLongFinal[1]   > 0 and inTradeLongFinal  <= 0 , title = "[MKT] Long Stop Loss Hit")
// alertcondition(getDivLong == 2 and getDivLong[1] != 2 , title = "[MKT] Long R Multiple TP Hit")

// alertcondition(close < -1, "--------LONG LMT--------") 

// alertcondition(inTradeLongFinal[1]  == 0 and inTradeLongFinal  == 1 , title = "[LMT] Set Long Exit RR TP (Optional - Must Have R Multiple Enabled)")
// alertcondition(inTradeLongFinal[1]  == 0 and inTradeLongFinal  == 1 , title = "[LMT] Set Initial Stop Loss Long")
// alertcondition(inTradeLongFinal > 0 and longStop != longStop[1] and inTradeLongFinal[1] > 0, title = "[LMT] Update Trailing Stop Price Long")

// alertcondition(close < -1, "--------SHORT MKT--------") 

// alertcondition(inTradeShortFinal[1]  == 0 and inTradeShortFinal  == -1 , title = "[MKT] Short Entry")
// alertcondition(inTradeShortFinal[1]  < 0 and inTradeShortFinal >= 0 , title = "[MKT] Short Stop Loss Hit" )
// alertcondition(getDivShort == 2 and getDivShort[1] != 2 , title = "[MKT] Short R Multiple TP Hit")


// alertcondition(close < -1, "--------SHORT LMT--------") 

// alertcondition(inTradeShortFinal[1]  == 0 and inTradeShortFinal  == -1 , title = "[LMT] Set Short Exit RR TP (Optional - Must Have R Multiple Enabled)")
// alertcondition(inTradeShortFinal[1]  == 0 and inTradeShortFinal  == -1 , title = "[LMT] Set Initial Stop Loss Short")
// alertcondition(inTradeShortFinal < 0 and shortStop != shortStop[1] and inTradeShortFinal[1] < 0, title = "[LMT] Update Trailing Stop Price Short")



// [y1PriceHTFLFinal,  y2PriceHTFLFinal,  y1PriceLTFLFinal,  y2PriceLTFLFinal,  y1xHTFLFinal, y1xLTFLFinal, inTradeLongF, inTradeShortF] = switch tradeTypeMaster
//     =>         [y1PriceHTFL,  y2PriceHTFL,  y1PriceLTFL,  y2PriceLTFL,  y1xHTFL, y1xLTFL, inTradeLongFinal, inTradeShortFinal]

// ZZCRP.lastBarZZ(
//          y1PriceHTFLFinal,  
//          y2PriceHTFLFinal,  
//          y1PriceLTFLFinal,  
//          y2PriceLTFLFinal,  
//          endLoop,  
//          y1xHTFLFinal,   
//          tfhtf,
// 	     tfltf,  
//          y1xLTFLFinal, 
// 	 	 showHTFzz, 
//          fibometer, 
//          ATR, 
//          inTradeLongF, 
//          inTradeShortF
//          )

// condUpH = y2PriceHTFLFinal.last() > y1PriceHTFLFinal.last()[1]
// condUpL = y2PriceLTFLFinal.last() > y1PriceLTFLFinal.last()[1]

// condDnH = y2PriceHTFLFinal.last() < y1PriceHTFLFinal.last()[1]
// condDnL = y2PriceLTFLFinal.last() < y1PriceLTFLFinal.last()[1]

// flipGreen = condUpH and condUpL and (condDnH[1] or condDnL[1])
// flipRed   = condDnH and condDnL and (condUpH[1] or condUpL[1])

// alertcondition(close < -1, "--------METER FLIPS--------")

// alertcondition(flipGreen, title = "IQ Meters Flip Green (Uptrend Strength)")
// alertcondition(flipRed  , title = "IQ Meters Flip Red   (Downtrend Strength)")

// alertcondition(close < -1, "--------BEST STRATEGY NUMBER CHANGE--------")

// alertcondition(tradeLong and not na(bestLongsIndexNow[1]) and bestLongsIndexNow != bestLongsIndexNow[1], title = "Best Long Strategy Number Change")
// alertcondition(tradeShort and not na(bestShortsIndexNow[1]) and bestShortsIndexNow != bestShortsIndexNow[1], title = "Best Short Strategy Number Change")

// plot(bestLongsIndexNow, title = "Best Long Strategy Number", display = display.data_window)
// plot(bestShortsIndexNow, title = "Best Short Strategy Number", display = display.data_window)


// ohlc       = plot(ohlc4, display = display.none)
// longPlot   = plot(inTradeLongFinal  ==  2 ? longStop  : na, color = color.rgb(128, 116, 255), style = plot.style_linebr)
// shortPlot  = plot(inTradeShortFinal == -2 ? shortStop : na, color = color.rgb(255, 116, 116), style = plot.style_linebr)
// longPlot2  = plot(inTradeLongFinal  ==  2 ? longStop  : na, color = color.new(color.rgb(128, 116, 255), 80), linewidth = 5, style = plot.style_linebr)
// shortPlot2 = plot(inTradeShortFinal == -2 ? shortStop : na, color = color.new(color.rgb(255, 116, 116), 80), linewidth = 5, style = plot.style_linebr)

// plot(inTradeLongFinal  == 1  ? longStop  : na, color = color.new(color.rgb(128, 116, 255), 50), style = plot.style_linebr)
// plot(inTradeShortFinal == -1 ? shortStop : na, color = color.new(color.rgb(255, 116, 116), 50), style = plot.style_linebr)
// fill(ohlc, longPlot,  color.new(color.rgb(128, 116, 255), 90))
// fill(ohlc, shortPlot, color.new(color.rgb(255, 116, 116), 90))



// if lastOnly

//     ltfPoly.delete()
//     htfPoly.delete()




// //Library used 

// // This Pine Script™ code is subject to the terms of the Mozilla Public License 2.0 at https://mozilla.org/MPL/2.0/
// // © KioseffTrading
// // 
// //@version=6

// // @description TODO: add library description here
// library("TRADINGIQIMPULSEAUTOLIB__dsfa89sdf435j123_DSAF__PRODUCTION")
// // v15 = attempt at making auto better
// import kaigouthro/hsvColor/16 as kai
// import PineCoders/Time/4 as pct

// isLastBar() => 
// 	time("1D") != time("1D", -1) and barstate.isconfirmed
    
// quick1(point, y2PriceArrMasterOPTI, atrUse, optiATRmult, changeBreak, getBreakoutPointUpArr, y1PriceArrMasterOPTI, masterDirArrOPTI, i) =>


//     y2PriceArrMasterOPTI.set(i, point)
 
//     if low <= point - atrUse * optiATRmult

//         if changeBreak
//             getBreakoutPointUpArr.set(i, point)

//         y1PriceArrMasterOPTI .set(i, point)
//         y2PriceArrMasterOPTI .set(i, low)
//         masterDirArrOPTI	 .set(i, -1)

// quick2(point, y2PriceArrMasterOPTI, atrUse, optiATRmult, changeBreak, getBreakoutPointDnArr, y1PriceArrMasterOPTI, masterDirArrOPTI, i) =>

//     y2PriceArrMasterOPTI.set(i, point)

//     if high >= point + atrUse * optiATRmult

//         if changeBreak
//             getBreakoutPointDnArr.set(i, point)

//         y1PriceArrMasterOPTI .set(i, point)
//         y2PriceArrMasterOPTI .set(i, high)
//         masterDirArrOPTI     .set(i, 1)


// quick3(point, y2PriceArrMasterOPTI, atrUse, optiATRmult, changeBreak, getBreakoutPointDnArr, y1PriceArrMasterOPTI, masterDirArrOPTI, i, getBreakoutPointUpArr) =>


//     if high >= point + atrUse * optiATRmult

//         if changeBreak
//             getBreakoutPointDnArr.set(i, point)

//         y1PriceArrMasterOPTI.set(i, point)
//         y2PriceArrMasterOPTI.set(i, high)

//         masterDirArrOPTI.set(i, 1)

//     else if low <= point - atrUse * optiATRmult

//         if changeBreak
//             getBreakoutPointUpArr.set(i, point)

//         y1PriceArrMasterOPTI.set(i, point)
//         y2PriceArrMasterOPTI.set(i, low)

//         masterDirArrOPTI.set(i, -1)


// export zzOpti(float atrUse, array<float> optiATRmultArr, array<float> y2PriceArrMasterOPTI, array<float> y1PriceArrMasterOPTI, 
// 	 array<int> masterDirArrOPTI, bool changeBreak,
// 	 array<float> getBreakoutPointUpArr, array<float> getBreakoutPointDnArr, int arrSize, bool showOPTI) => 


//     if barstate.isconfirmed and showOPTI

//         for i = 0 to arrSize - 1

//             getDir      = masterDirArrOPTI    .get(i)
//             point       = y2PriceArrMasterOPTI.get(i)
//             optiATRmult = optiATRmultArr      .get(i)

//             switch getDir 

//                 1  =>     

//                      point := math.max(point, high),
//                      quick1(point, y2PriceArrMasterOPTI, atrUse, optiATRmult, changeBreak, getBreakoutPointUpArr, y1PriceArrMasterOPTI, masterDirArrOPTI, i)

//                 -1 =>

//                      point := math.min(low, point),
//                      quick2(point, y2PriceArrMasterOPTI, atrUse, optiATRmult, changeBreak, getBreakoutPointDnArr, y1PriceArrMasterOPTI, masterDirArrOPTI, i)

//                 =>  quick3(point, y2PriceArrMasterOPTI, atrUse, optiATRmult, changeBreak, getBreakoutPointDnArr, y1PriceArrMasterOPTI, masterDirArrOPTI, i, getBreakoutPointUpArr)

//     0


// export optimizeZZ(bool isLastBar, bool showOPTI, string tradeType, array<int> boolArr,
// 	  array<float> y1PriceArrMasterHTF,
// 	 	 array<int> masterDirArrHTF,   array<float> getBreakoutPointUpArrTrail, 
// 		 	 array<float> getBreakoutPointDnArrTrail,   float getLTFclo, 
// 			 	 array<float> entryArr, float atrLTF, float getLTFclo1, array<float> triggerArr, array<float> exitArr, array<int> boolArrS, bool tradeShort, array<float> entryArrS, 
// 				 	 array<float> triggerArrS, array<float> exitArrS, array<float> y2PriceArrMasterHTF, float buySellRange, array<float> zzCurrATR, array<float> atrTarr, array<float> atrPTarr, bool useRR, float RRmult, array<float> RRarr, array<float> RRarrS, array<int> divArr, array<int> divArrS) =>

// 	if isLastBar and showOPTI and barstate.isconfirmed

//         count = 0

// 	    if tradeType == "Breakout"

// 	        for i = 0 to zzCurrATR.size() - 1

//                 for x = 0 to atrTarr.size() - 1

// 	                if boolArr.get(count) == 0

// 	                    if masterDirArrHTF.get(i) == 1 

// 	                    	getBreakoutPointUpCurr = getBreakoutPointUpArrTrail.get(i)

// 	                        if getLTFclo > getBreakoutPointUpCurr and getBreakoutPointUpCurr != 0 and getLTFclo1 <= getBreakoutPointUpCurr 

// 	                    	    gety1htf 			   = y1PriceArrMasterHTF.get(i)
// 				    		    gety2htf		 	   = y2PriceArrMasterHTF.get(i)
// 	                            Range                  = math.abs(gety2htf - gety1htf) * (buySellRange / 100)

//                                 if getBreakoutPointUpCurr <= gety1htf + Range

//                                     atrCalcPT   = atrLTF * atrPTarr.get(x)
//                                     atrCalcStop = atrLTF * atrTarr .get(x)

// 	                                entryArr   .set(count, close)
// 	                                boolArr    .set(count, 1)
// 	                                triggerArr .set(count, math.round_to_mintick(close + atrCalcPT))
// 	                                exitArr    .set(count, math.round_to_mintick(close - atrCalcStop))

//                                     if useRR

//                                         getExit         = math.round_to_mintick(close - atrCalcStop)
//                                         getExitDistance = math.abs(close - getExit) * RRmult

//                                         RRarr      .set(count, close + getExitDistance)
//                                         divArr     .set(count, 1)


//                     if tradeShort
// 	                    if boolArrS.get(count) == 0

// 	                        if masterDirArrHTF.get(i) == -1 

// 	                        	getBreakoutPointDnCurr = getBreakoutPointDnArrTrail.get(i)

// 	                            if getLTFclo < getBreakoutPointDnCurr and getBreakoutPointDnCurr != 20e20 and getLTFclo1 >= getBreakoutPointDnCurr 

// 	                        	    gety1htf = y1PriceArrMasterHTF.get(i)
// 				    	    	    gety2htf = y2PriceArrMasterHTF.get(i)
//                             	    Range    = math.abs(gety2htf - gety1htf) * (buySellRange / 100)

//                                     if getBreakoutPointDnCurr >= gety1htf - Range 
                                    
//                                         atrCalcPT   = atrLTF * atrPTarr.get(x)
//                                         atrCalcStop = atrLTF * atrTarr .get(x)

//                                         entryArrS   .set(count, close)
// 	                                    boolArrS    .set(count, -1)
// 	                                    triggerArrS .set(count, math.round_to_mintick(close - atrCalcPT))
// 	                                    exitArrS    .set(count, math.round_to_mintick(close + atrCalcStop))

//                                         if useRR

//                                             getExit         = math.round_to_mintick(close + atrCalcStop)
//                                             getExitDistance = math.abs(close - getExit) * RRmult

//                                             RRarrS     .set(count, close - getExitDistance)
//                                             divArrS    .set(count, 1)



//                     count += 1

// 	    if tradeType == "Cheap"

// 	        for i = 0 to zzCurrATR.size() - 1

//                 for x = 0 to atrTarr.size() - 1

// 	                if boolArr.get(count) == 0

// 	                    if masterDirArrHTF.get(i) == 1

//                     		getBreakoutPointUpCurr = getBreakoutPointUpArrTrail.get(i)

// 	                        if getLTFclo < getBreakoutPointUpCurr and getLTFclo1 >= getBreakoutPointUpCurr and getBreakoutPointUpCurr != 0 
                            
//                                 gety1htf = y1PriceArrMasterHTF.get(i)
//                                 gety2htf = y2PriceArrMasterHTF.get(i)
//                         	    Range    = math.abs(gety2htf - gety1htf) * (buySellRange / 100)

//                                 if getBreakoutPointUpCurr <= gety1htf + Range 

//                                     atrStop   = atrLTF * atrTarr.get(x)
//                                     atrTarget = atrLTF * atrPTarr.get(x)

// 	                                entryArr   .set(count, close)
// 	                                boolArr    .set(count, 1)
// 	                                triggerArr .set(count, math.round_to_mintick(close + atrTarget))
// 	                                exitArr    .set(count, math.round_to_mintick(close - atrStop))

//                                     if useRR

//                                         getExit         = math.round_to_mintick(close - atrStop)
//                                         getExitDistance = math.abs(close - getExit) * RRmult

//                                         RRarr      .set(count, close + getExitDistance)
//                                         divArr     .set(count, 1)



// 	                if boolArrS.get(count) == 0

// 	                    if masterDirArrHTF.get(i) == -1 and tradeShort 

//                     		getBreakoutPointDnCurr = getBreakoutPointDnArrTrail.get(i)

// 	                        if getLTFclo > getBreakoutPointDnCurr and getLTFclo1 <= getBreakoutPointDnCurr and getBreakoutPointDnCurr != 20e20 
                            
//                                 gety1htf = y1PriceArrMasterHTF.get(i), gety2htf = y2PriceArrMasterHTF.get(i)
//                                 Range    = math.abs(gety2htf - gety1htf) * (buySellRange / 100)

//                                 if getBreakoutPointDnCurr >= gety1htf - Range

//                                     atrStop   = atrLTF * atrTarr.get(x)
//                                     atrTarget = atrLTF * atrPTarr.get(x)

// 	                                entryArrS   .set(count, close)
// 	                                boolArrS    .set(count, -1)
// 	                                triggerArrS .set(count, math.round_to_mintick(close - atrTarget))
// 	                                exitArrS    .set(count, math.round_to_mintick(close + atrStop))

//                                     if useRR

//                                         getExit         = math.round_to_mintick(close + atrStop)
//                                         getExitDistance = math.abs(close - getExit) * RRmult

//                                         RRarrS     .set(count, close - getExitDistance)
//                                         divArrS    .set(count, 1)


//                     count += 1

// boolArr1(getExit, boolArr, PFlossArr, limitArr, getEntry, getTrigger, atrLTF, i, isLastBar, exitArr, atrTarr, x, divArr, useRR, RRarr, PFprofitArr, closer2low, closeEOD) => 
    
//     div = divArr.get(i), getRRtp = RRarr.get(i)

//     if div == 1 and useRR and open > getExit

//         if open >= getRRtp

//             PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs(open - getEntry) / 2))
//             divArr.set(i, 2)
//             div := 2
 
//         else if high >= getRRtp 

//             if low > getExit 

//                 PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs(getRRtp - getEntry) / 2))
//                 divArr.set(i, 2)
//                 div := 2
 
//             else 

//                 if not closer2low
                
//                     PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs(getRRtp - getEntry) / 2))
//                     divArr.set(i, 2) 
//                     div := 2


// 	if open <= getExit

// 	    boolArr  .set(i, 0)
// 	    PFlossArr.set(i, PFlossArr.get(i) + (math.abs((open - getEntry) / div)))
// 	    limitArr .set(i, 0)

// 	else if low <= getExit

// 	    boolArr  .set(i, 0)
// 	    PFlossArr.set(i, PFlossArr.get(i) + (math.abs((getExit - getEntry)) / div))
// 	    limitArr .set(i, 0)

//     else if session.islastbar_regular and closeEOD
 
//         boolArr.set(i, 0)

//         isProfit = close >= getEntry 

//         switch isProfit 

//             true =>   PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs((close - getEntry)) / div))
//             => 	      PFlossArr.set(i, PFlossArr.get(i) + (math.abs((close - getEntry)) / div))

// 	    limitArr .set(i, 0)


// 	else if close >= getTrigger and isLastBar

// 	    boolArr .set(i, 2)
// 	    exitArr .set(i, math.max(getExit, math.round_to_mintick(close - atrLTF * atrTarr.get(x))))
// 	    limitArr.set(i, 0)

//     0

// boolArr2(getExit, boolArr, PFlossArr, limitArr, getEntry, getTrigger, atrLTF, i,  PFprofitArr, isLastBar, exitArr, x, atrTarr, divArr, RRarr, useRR, closer2low, closeEOD) =>


//     div = divArr.get(i), getRRtp = RRarr.get(i)

//     if div == 1 and useRR and open > getExit

//         if open >= getRRtp

//             PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs(open - getEntry) / 2))
//             divArr.set(i, 2)
//             div := 2
 
//         else if high >= getRRtp 

//             if low > getExit 

//                 PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs(getRRtp - getEntry) / 2))
//                 divArr.set(i, 2)
//                 div := 2
 
//             else 

//                 if not closer2low
                
//                     PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs(getRRtp - getEntry) / 2))
//                     divArr.set(i, 2) 
//                     div := 2

// 	if open <= getExit

// 	    boolArr.set(i, 0)

// 	    switch math.sign(open - getEntry) 

// 	        1  => PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs(open - getEntry) / div))
// 	        -1 => PFlossArr  .set(i, PFlossArr  .get(i) + (math.abs(open - getEntry) / div))

// 	    limitArr .set(i, 0)

// 	else if low <= getExit

// 	    boolArr.set(i, 0)

// 	    switch math.sign(getExit - getEntry) 

// 	        1  => PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs(getExit - getEntry) / div))
// 	        -1 => PFlossArr  .set(i, PFlossArr  .get(i) + (math.abs(getExit - getEntry) / div))

// 	    limitArr .set(i, 0)

//     else if session.islastbar_regular and closeEOD
 
//         boolArr.set(i, 0)

//         isProfit = close >= getEntry 

//         switch isProfit 

//             true =>   PFprofitArr.set(i, PFprofitArr.get(i) + (math.abs((close - getEntry)) / div))
//             => 	      PFlossArr.set(i, PFlossArr.get(i) + (math.abs((close - getEntry)) / div))

// 	    limitArr .set(i, 0)


// 	else if isLastBar

// 	    exitArr.set(i, math.max(getExit, math.round_to_mintick(close - atrLTF * atrTarr.get(x))))
	
//     0

// boolS1(getExitArrS, boolArrS,  PFlossArrS, limitArrS, getEntryArrS, getTriggerArrS, atrLTF,  i, isLastBar, exitArrS, x, atrTarr, divArrS, RRarrS, useRR, PFprofitArrS, closer2low, closeEOD)=>


//     div = divArrS.get(i), getRRtpS = RRarrS.get(i)

//     if div == 1 and useRR and open < getExitArrS

//         if open <= getRRtpS

//             PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs(open - getEntryArrS) / 2))
//             divArrS.set(i, 2)
//             div := 2
                
//         else if low <= getRRtpS 

//             if high < getExitArrS

//                 PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs(getRRtpS - getEntryArrS) / 2))
//                 divArrS.set(i, 2)
//                 div := 2
                
//             else 

//                 if closer2low 
                
//                     PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs(getRRtpS - getEntryArrS) / 2))
//                     divArrS.set(i, 2)
//                     div := 2

// 	if open >= getExitArrS 

// 	    boolArrS    .set(i, 0)
// 	    PFlossArrS  .set(i, PFlossArrS.get(i) + (math.abs((open - getEntryArrS)) / div))
// 	    limitArrS   .set(i, 0)


// 	else if high >= getExitArrS 

// 	    boolArrS  .set(i, 0)
// 	    PFlossArrS.set(i, PFlossArrS.get(i) + (math.abs((getExitArrS - getEntryArrS)) / div))
// 	    limitArrS .set(i, 0)


//     else if session.islastbar_regular and closeEOD
 
//         boolArrS.set(i, 0)

//         isProfit = close <= getEntryArrS

//         switch isProfit 

//             true =>   PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs((close - getEntryArrS)) / div))
//             => 	      PFlossArrS.set(i, PFlossArrS.get(i) + (math.abs((close - getEntryArrS)) / div))

// 	    limitArrS .set(i, 0)


// 	else if close <= getTriggerArrS and isLastBar

// 	    boolArrS .set(i, -2)
// 	    exitArrS .set(i,  math.min(getExitArrS, math.round_to_mintick(close + atrLTF * atrTarr.get(x))))
// 	    limitArrS.set(i, 0)

// boolS2(getExitArrS, getEntryArrS, boolArrS,  PFlossArrS, limitArrS, getEntryS, getTriggerArrS, atrLTF, i, isLastBar, exitArrS, PFprofitArrS, x, atrTarr, divArrS, RRarr, useRR, closer2low, closeEOD) =>


//     div = divArrS.get(i), getRRtpS = RRarr.get(i)

//     if div == 1 and useRR and open < getExitArrS

//         if open <= getRRtpS

//             PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs(open - getEntryArrS) / 2))
//             divArrS.set(i, 2)
//             div := 2
                
//         else if low <= getRRtpS 

//             if high < getExitArrS

//                 PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs(getRRtpS - getEntryArrS) / 2))
//                 divArrS.set(i, 2)
//                 div := 2
                
//             else 

//                 if closer2low 
                
//                     PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs(getRRtpS - getEntryArrS) / 2))
//                     divArrS.set(i, 2)
//                     div := 2


// 	if open >= getExitArrS

// 	    boolArrS.set(i, 0)

// 	    switch math.sign(open - getEntryArrS) 

// 	        -1 => PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs(open - getEntryArrS)) / div)
// 	        1  => PFlossArrS  .set(i, PFlossArrS.get(i) + (math.abs(open - getEntryArrS)) / div)

// 	    limitArrS .set(i, 0)


// 	else if high >= getExitArrS

// 	    boolArrS.set(i, 0)

// 	    switch math.sign(getExitArrS - getEntryArrS) 

// 	        -1 => PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs(getExitArrS - getEntryArrS)) / div)
// 	        1  => PFlossArrS  .set(i, PFlossArrS.get(i) + (math.abs(getExitArrS - getEntryArrS)) / div)

// 	    limitArrS .set(i, 0)


//     else if session.islastbar_regular and closeEOD
 
//         boolArrS.set(i, 0)

//         isProfit = close <= getEntryArrS

//         switch isProfit 

//             true =>   PFprofitArrS.set(i, PFprofitArrS.get(i) + (math.abs((close - getEntryArrS)) / div))
//             => 	      PFlossArrS.set(i, PFlossArrS.get(i) + (math.abs((close - getEntryArrS)) / div))

// 	    limitArrS .set(i, 0)


// 	else if isLastBar

// 	    exitArrS.set(i, math.min(getExitArrS, math.round_to_mintick(close + atrLTF * atrTarr.get(x))))



// export optiZZentryExit(bool isLastBar, bool showOPTI,   array<int> boolArr,
// 			 	 array<float> entryArr, float atrLTF, array<float> triggerArr, array<float> exitArr, array<int> boolArrS, array<float> entryArrS, 
// 				 	 array<float> triggerArrS, array<float> exitArrS,  array<float> PFlossArr, array<float> limitArr, array<float> PFprofitArr,
// 					 	   array<float> PFlossArrS, array<float> limitArrS, array<float> PFprofitArrS, array<float> zzCurrArr, array<float> atrTarr, array<int> divArr, bool useRR, array<float> RRarr,
//                              bool closer2low, array<int> divArrS, array<float> RRarrS, bool closeEOD
// 						 	 ) =>

// 	if showOPTI and barstate.isconfirmed

//         count = 0

// 	    for i = 0 to zzCurrArr.size() - 1 // optiExits
			
//             for x = 0 to atrTarr.size() - 1

//                 getBool = boolArr.get(count) 

//                 if getBool != 0

// 			        getExit    = exitArr.get(count), getTrigger = triggerArr.get(count)
// 			        getEntry   = entryArr.get(count), 

//                     switch getBool 

//                         1 => boolArr1(getExit, boolArr, PFlossArr, limitArr, getEntry, getTrigger, atrLTF,  count, isLastBar, exitArr, atrTarr, x, divArr, useRR, RRarr, PFprofitArr, closer2low, closeEOD)
//                         2 => boolArr2(getExit, boolArr, PFlossArr, limitArr, getEntry, getTrigger, atrLTF,  count, PFprofitArr, isLastBar, exitArr, x, atrTarr, divArr, RRarr, useRR, closer2low, closeEOD)

//                 getBoolS = boolArrS.get(count) 

//                 if getBoolS != 0

// 			        getExitArrS = exitArrS.get(count), getEntryArrS = entryArrS.get(count)
// 			        getTriggerArrS = triggerArrS.get(count)

//                     switch getBoolS 

//                         -1 => boolS1(getExitArrS, boolArrS,   PFlossArrS, limitArrS, getEntryArrS, getTriggerArrS, atrLTF,  count, isLastBar, exitArrS, x, atrTarr, divArrS, RRarrS, useRR, PFprofitArrS, closer2low, closeEOD)
//                         -2 => boolS2(getExitArrS, getEntryArrS, boolArrS, PFlossArrS, limitArrS, getEntryArrS, getTriggerArrS, atrLTF,  count, isLastBar, exitArrS, PFprofitArrS, x, atrTarr, divArrS, RRarrS, useRR, closer2low, closeEOD)

//                 count += 1


// export lastBarZZlongLTF (array<float> closeArrEnd, array<float> highArrEnd, array<float>lowArrEnd, array<float>timeArrEnd, array<float> atrArrLTF, float bestATRlongltf,
// 	  array<float> y2PriceFinalLTF, array<float> y2xFinalLTF, array<float> y1PriceFinalLTF, float buffer, 
// 	 	  array<float> y1xFinalLTF,
// 		 	 array<float> y2PriceHistoryLTF, array<float>y1PriceHistoryLTF, array<bool> isHighFirstLongArrLTF, 
// 			 	 array<float> getBreakoutPointUpArrLongLTF, bool showLab, array<float> closeArrEnd1,  string labelSize) => 
    

//     y2PriceHistoryLTF.clear()
//     y1PriceHistoryLTF.clear()
//     isHighFirstLongArrLTF.clear()
//     getBreakoutPointUpArrLongLTF.clear()

// 	pointLTF    = closeArrEnd.first()
//     timePLTF    = int(timeArrEnd.first())
//     dirLTF      = 0
//     breakUpArr  = matrix.new<float>(3, 0)
//     dirArr      = array.new<int>(1)
//     timePArr    = array.new<int>(1)
//     pointArr    = array.new<float>(1)
//     chartPoints = array.new<chart.point>()

//     getBreakoutPointUpLongLoopLTF = 0.

//     for i = 0 to closeArrEnd.size() - 1

//         if chartPoints.size() > 9000
//             chartPoints.shift()

//         getHigh    = highArrEnd.get(i)
//         getLow     = lowArrEnd.get(i)
//         getTime    = int(timeArrEnd.get(i))
//         getAtr     = atrArrLTF.get(i) * bestATRlongltf
//         getLTFclo  = closeArrEnd.get(i)
//         getLTFclo1 = closeArrEnd1.get(i)

//         if dirLTF == 1

//             pointLTF := math.max(pointLTF, getHigh)
//             y2PriceFinalLTF.set(0, pointLTF)

//             timePLTF := switch getHigh == pointLTF 

//                 true => int(getTime) 
//                 =>      int(timePLTF)

//             y2xFinalLTF.set(0, timePLTF)

//             addition = math.abs(pointLTF - y1PriceFinalLTF.first()) * buffer

//             if getLow <= pointLTF - getAtr - addition

//                 chartPoints    .push(chart.point.from_time(timePLTF, pointLTF))
//                 y1PriceFinalLTF.set(0, pointLTF)
//                 y2PriceFinalLTF.set(0, getLow)
//                 y1xFinalLTF    .set(0, timePLTF)
//                 y2xFinalLTF    .set(0, getTime)
//                 dirLTF   := -1 
//                 pointLTF := getLow
//                 timePLTF := getTime

  
//         else if dirLTF == -1

//             pointLTF := math.min(getLow, pointLTF)
//             y2PriceFinalLTF.set(0, pointLTF)

//             timePLTF := switch getLow == pointLTF 
//                 true => getTime
//                 =>      timePLTF

//             y2xFinalLTF.set(0, timePLTF)

//             addition = math.abs(pointLTF - y1PriceFinalLTF.first()) * buffer

//             if getHigh >= pointLTF + getAtr + addition

//                 chartPoints    .push(chart.point.from_time(timePLTF, pointLTF))
//                 y1PriceFinalLTF.set(0, pointLTF)
//                 y2PriceFinalLTF.set(0, getHigh)
//                 y1xFinalLTF    .set(0, timePLTF)
//                 y2xFinalLTF    .set(0, getTime)
//                 dirLTF         := 1 
//                 pointLTF       := getHigh 
//                 timePLTF       := getTime

   
//         if dirLTF == 0 

//             if getHigh >= pointLTF + getAtr

//                 chartPoints.push(chart.point.from_time(timePLTF, pointLTF))

//                 y1PriceFinalLTF.set(0, pointLTF)
//                 y2PriceFinalLTF.set(0, getHigh)
//                 y1xFinalLTF    .set(0, timePLTF)
//                 y2xFinalLTF    .set(0, getTime)
//                 dirLTF        := 1
//                 pointLTF      := getHigh
//                 timePLTF      := getTime 


//             else if getLow <= pointLTF + getAtr
//                 chartPoints.push(chart.point.from_time(timePLTF, pointLTF))

//                 y1PriceFinalLTF.set(0, pointLTF)
//                 y2PriceFinalLTF.set(0, getLow)
//                 y1xFinalLTF    .set(0, timePLTF)
//                 y2xFinalLTF    .set(0, getTime)
//                 dirLTF        := -1 
//                 pointLTF      := getLow
//                 timePLTF      := getTime

//         y2PriceHistoryLTF.push(y2PriceFinalLTF.first())
//         y1PriceHistoryLTF.push(y1PriceFinalLTF.first())

//         getFirstY1           = y1PriceFinalLTF.first(), getFirstY2 = y2PriceFinalLTF.first()
//         isHighFirstLongFinal = getFirstY2 > getFirstY1

//         isHighFirstLongArrLTF.push(isHighFirstLongFinal)

//         if getFirstY1 > getFirstY2
            
//             if getBreakoutPointUpLongLoopLTF != getFirstY1 and getBreakoutPointUpLongLoopLTF != 0
//                 breakUpArr.add_col(breakUpArr.columns(), array.from(y1xFinalLTF.first(), getFirstY1, 0))

//             getBreakoutPointUpLongLoopLTF := getFirstY1

//         if breakUpArr.columns() > 0 
//             getCols = breakUpArr.columns() - 1

//             if breakUpArr.get(2, getCols) == 0

//                 if getLTFclo > breakUpArr.get(1, getCols) and getLTFclo1 <= breakUpArr.get(1, getCols)

//                     line.new(int(breakUpArr.get(0, getCols)), breakUpArr.get(1, getCols), getTime, breakUpArr.get(1, getCols), 
//                                              color = #74ffbc, 
//                                              style = line.style_dotted,
//                                              xloc  = xloc.bar_time
//                                              )

//                     if line.all.size() > 498
//                         line.all.shift().delete()

//                     if showLab

//                         if label.all.size() >= 250 
//                             label.all.shift().delete()

//                         label.new(getTime, getHigh, text = "Break Up", size = labelSize, 
//                                              color     = #00000000, 
//                                              textcolor = #74ffbc, 
//                                              xloc      = xloc.bar_time
//                                              )
                    
//                     breakUpArr.set(2, getCols,  1)

//         getBreakoutPointUpArrLongLTF.push(getBreakoutPointUpLongLoopLTF)

//     pointArr.set(0, pointLTF)
//     timePArr.set(0, int(timePLTF))
//     dirArr  .set(0, dirLTF)

//     [pointArr, timePArr, dirArr, chartPoints]


// export lastBarZZlongHTF (array<float> closeArrEnd, array<float> highArrEnd, array<float>lowArrEnd, array<float>timeArrEnd, array<float> atrArrHTF, float bestATRlonghtf,
// 	  array<float> y2PriceFinalHTF, array<float> y2xFinalHTF, array<float> y1PriceFinalHTF, float buffer, 
// 	 	  array<float> y1xFinalHTF,
// 		 	 array<float> y2PriceHistoryHTF, array<float>y1PriceHistoryHTF, array<bool> isHighFirstLongArrHTF, 
// 			 	 array<float> getBreakoutPointUpArrLongHTF) => 
    

//     pointHTF    = closeArrEnd.first()
//     timePHTF    = int(timeArrEnd.first())
//     dirHTF      = 0
//     dirArr      = array.new<int>(1)
//     timePArr    = array.new<int>(1)
//     pointArr    = array.new<float>(1)
//     chartPoints = array.new<chart.point>()
//     y2PriceHistoryHTF.clear()
//     y1PriceHistoryHTF.clear()
//     isHighFirstLongArrHTF.clear()
//     getBreakoutPointUpArrLongHTF.clear()

//     getBreakoutPointUpLongLoopHTF = 0.

//     for i = 0 to closeArrEnd.size() - 1

//         getHigh = highArrEnd.get(i)
//         getLow  = lowArrEnd.get(i)
//         getTime = int(timeArrEnd.get(i))
//         getAtr  = atrArrHTF.get(i) * bestATRlonghtf
    
//         if dirHTF == 1

//             pointHTF := math.max(pointHTF, getHigh)
//             y2PriceFinalHTF.set(0, pointHTF)

//             timePHTF := switch getHigh == pointHTF 

//                 true => int(getTime )
//                 =>      int(timePHTF)

//             y2xFinalHTF.set(0, timePHTF)

//             addition = math.abs(pointHTF - y1PriceFinalHTF.first()) * buffer

//             if getLow <= pointHTF - getAtr - addition

//                 chartPoints.push(chart.point.from_time(timePHTF, pointHTF))

//                 y1PriceFinalHTF .set(0, pointHTF)
//                 y2PriceFinalHTF .set(0, getLow)
//                 y1xFinalHTF     .set(0, timePHTF)
//                 y2xFinalHTF     .set(0, getTime)
//                 dirHTF   := -1 
//                 pointHTF := getLow
//                 timePHTF := getTime

//         else if dirHTF == -1
    
//             pointHTF := math.min(getLow, pointHTF)
//             y2PriceFinalHTF.set(0, pointHTF)

//             timePHTF := switch getLow == pointHTF 
//                 true => getTime
//                 =>      timePHTF

//             y2xFinalHTF.set(0, timePHTF)

//             addition = math.abs(pointHTF - y1PriceFinalHTF.first()) * buffer

//             if getHigh >= pointHTF + getAtr + addition 

//                 chartPoints.push(chart.point.from_time(timePHTF, pointHTF))

//                 y1PriceFinalHTF.set(0, pointHTF)
//                 y2PriceFinalHTF.set(0, getHigh)
//                 y1xFinalHTF    .set(0, timePHTF)
//                 y2xFinalHTF    .set(0, getTime)
//                 dirHTF   := 1 
//                 pointHTF := getHigh 
//                 timePHTF := getTime

//         if dirHTF == 0 

//             if getHigh >= pointHTF + getAtr
//                 chartPoints.push(chart.point.from_time(timePHTF, pointHTF))

//                 y1PriceFinalHTF.set(0, pointHTF)
//                 y2PriceFinalHTF.set(0, getHigh)
//                 y1xFinalHTF    .set(0, timePHTF)
//                 y2xFinalHTF    .set(0, getTime)
//                 dirHTF   := 1
//                 pointHTF := getHigh
//                 timePHTF := getTime 


//             else if getLow <= pointHTF + getAtr
//                 chartPoints.push(chart.point.from_time(timePHTF, pointHTF))

//                 y1PriceFinalHTF.set(0, pointHTF)
//                 y2PriceFinalHTF.set(0, getLow)
//                 y1xFinalHTF    .set(0, timePHTF)
//                 y2xFinalHTF    .set(0, getTime)
//                 dirHTF   := -1 
//                 pointHTF := getLow
//                 timePHTF := getTime

//         y2PriceHistoryHTF.push(y2PriceFinalHTF.first())
//         y1PriceHistoryHTF.push(y1PriceFinalHTF.first())
        
//         isHighFirstLongFinal = y2PriceFinalHTF.first() > y1PriceFinalHTF.first()

//         isHighFirstLongArrHTF.push(isHighFirstLongFinal)

//         if y1PriceFinalHTF.first() > y2PriceFinalHTF.first()
//             getBreakoutPointUpLongLoopHTF := y1PriceFinalHTF.first()
            
//         getBreakoutPointUpArrLongHTF.push(getBreakoutPointUpLongLoopHTF)

//     pointArr.set(0, pointHTF)
//     timePArr.set(0, int(timePHTF))
//     dirArr  .set(0, dirHTF)

//     [pointArr, timePArr, dirArr, chartPoints]

// export lastBarZZshortLTF (array<float> closeArrEnd, array<float> highArrEnd, array<float>lowArrEnd, array<float>timeArrEnd, array<float> atrArrLTF, float bestATRshortltf,
// 	 array<float> y2PriceFinalLTFS, array<float> y2xFinalLTFS, array<float> y1PriceFinalLTFS, float buffer, 
// 	 	   array<float> y1xFinalLTFS,
// 		 	 array<float> y2PriceHistoryLTFS, array<float>y1PriceHistoryLTFS, array<bool> isHighFirstShortArrLTF, 
// 			 	 array<float> getBreakoutPointDnArrShortLTF, bool showLab, array<float> closeArrEnd1, bool tradeShort,  string labelSize) => 
    


//     getBreakoutPointDnShortLoopLTF = 20e20
//     breakdnArr = matrix.new<float>(3, 0)
//     pointLTFS  = closeArrEnd.first()
//     timePLTFS  = timeArrEnd.first()
//     dirLTFS    = 0
//     dirArr     = array.new<int>(1)
//     timePArr   = array.new<int>(1)
//     pointArr   = array.new<float>(1)
//     y2PriceHistoryLTFS.clear()
//     y1PriceHistoryLTFS.clear()
//     isHighFirstShortArrLTF.clear()
//     getBreakoutPointDnArrShortLTF.clear()

//     for i = 0 to closeArrEnd.size() - 1

//         getHigh    = highArrEnd.get(i)
//         getLow     = lowArrEnd.get(i)
//         getTime    = int(timeArrEnd.get(i))
//         getAtr     = atrArrLTF.get(i) * bestATRshortltf
//         getLTFclo  = closeArrEnd.get(i)
//         getLTFclo1 = closeArrEnd1.get(i)

//         if dirLTFS == 1

//             pointLTFS := math.max(pointLTFS, getHigh)
//             y2PriceFinalLTFS.set(0, pointLTFS)

//             timePLTFS := switch getHigh == pointLTFS 

//                 true => getTime 
//                 =>      timePLTFS

//             y2xFinalLTFS.set(0, timePLTFS)

//             addition = math.abs(pointLTFS - y1PriceFinalLTFS.first()) * buffer

//             if getLow <= pointLTFS - getAtr - addition

//                 y1PriceFinalLTFS.set(0, pointLTFS)
//                 y2PriceFinalLTFS.set(0, getLow)
//                 y1xFinalLTFS    .set(0, timePLTFS)
//                 y2xFinalLTFS    .set(0, getTime)
//                 dirLTFS   := -1 
//                 pointLTFS := getLow
//                 timePLTFS := getTime

//         else if dirLTFS == -1
//             pointLTFS := math.min(getLow, pointLTFS)
//             y2PriceFinalLTFS.set(0, pointLTFS)

//             timePLTFS := switch getLow == pointLTFS 
//                 true => getTime
//                 =>      timePLTFS

//             y2xFinalLTFS.set(0, timePLTFS)

//             addition = math.abs(pointLTFS - y1PriceFinalLTFS.first()) * buffer

//             if getHigh >= pointLTFS + getAtr + addition 

//                 y1PriceFinalLTFS.set(0, pointLTFS)
//                 y2PriceFinalLTFS.set(0, getHigh)
//                 y1xFinalLTFS    .set(0, timePLTFS)
//                 y2xFinalLTFS    .set(0, getTime)
//                 dirLTFS   := 1 
//                 pointLTFS := getHigh 
//                 timePLTFS := getTime

//         if dirLTFS == 0 

//             if getHigh >= pointLTFS + getAtr

//                 y1PriceFinalLTFS.set(0, pointLTFS)
//                 y2PriceFinalLTFS.set(0, getHigh)
//                 y1xFinalLTFS    .set(0, timePLTFS)
//                 y2xFinalLTFS    .set(0, getTime)
//                 dirLTFS   := 1
//                 pointLTFS := getHigh
//                 timePLTFS := getTime 


//             else if getLow <= pointLTFS + getAtr

//                 y1PriceFinalLTFS.set(0, pointLTFS)
//                 y2PriceFinalLTFS.set(0, getLow)
//                 y1xFinalLTFS    .set(0, timePLTFS)
//                 y2xFinalLTFS    .set(0, getTime)
//                 dirLTFS   := -1 
//                 pointLTFS := getLow
//                 timePLTFS := getTime

//         gety1first = y1PriceFinalLTFS.first(), gety2first = y2PriceFinalLTFS.first()

//         y2PriceHistoryLTFS.push(gety2first)
//         y1PriceHistoryLTFS.push(gety1first)
        
//         isHighFirstShortFinal = gety2first > gety1first

//         isHighFirstShortArrLTF.push(isHighFirstShortFinal)

//         if gety1first < gety2first

//             if getBreakoutPointDnShortLoopLTF != gety1first and getBreakoutPointDnShortLoopLTF != 20e20
//                 breakdnArr.add_col(breakdnArr.columns(), array.from(y1xFinalLTFS.first(), gety1first, 0))

//             getBreakoutPointDnShortLoopLTF := gety1first
            
//         getBreakoutPointDnArrShortLTF.push(getBreakoutPointDnShortLoopLTF)

//         if breakdnArr.columns() > 0 and tradeShort 
//             getCols = breakdnArr.columns() - 1
//             if breakdnArr.get(2, getCols) == 0
            
//                 if getLTFclo < breakdnArr.get(1, getCols) and getLTFclo1 >= breakdnArr.get(1, getCols)

//                     line.new(int(breakdnArr.get(0, getCols)), breakdnArr.get(1, getCols), getTime, breakdnArr.get(1, getCols), 
//                                              color = color.rgb(255, 116, 116), 
//                                              style = line.style_dotted, 
//                                              xloc  = xloc.bar_time
//                                              )

//                     if line.all.size() > 498 
//                         line.all.shift().delete()

//                     if showLab

//                         label.new(getTime, getLow, text = "Break Dn", size = labelSize, 
//                                                  color     = #00000000, 
//                                                  textcolor = color.rgb(255, 116, 116), 
//                                                  xloc      = xloc.bar_time, 
//                                                  style     = label.style_label_up)
                    
//                     breakdnArr.set(2, getCols, 1)

//     pointArr.set(0, pointLTFS)
//     timePArr.set(0, int(timePLTFS))
//     dirArr  .set(0, dirLTFS)

//     [pointArr, timePArr, dirArr]


// export lastBarZZshortHTF (array<float> closeArrEnd, array<float> highArrEnd, array<float>lowArrEnd, array<float>timeArrEnd, array<float> atrArrHTF, float bestATRshorthtf,
// 	 array<float> y2PriceFinalHTFS, array<float> y2xFinalHTFS, array<float> y1PriceFinalHTFS, float buffer, 
// 	 	   array<float> y1xFinalHTFS,
// 		 	 array<float> y2PriceHistoryHTFS, array<float>y1PriceHistoryHTFS, array<bool> isHighFirstShortArrHTF, 
// 			 	 array<float> getBreakoutPointDnArrShortHTF) => 
    


//     getBreakoutPointDnShortLoopHTF = 0.
//     pointHTFS = closeArrEnd.first()
//     timePHTFS = timeArrEnd.first()
//     dirHTFS   = 0
//     dirArr    = array.new<int>(1)
//     timePArr  = array.new<int>(1)
//     pointArr  = array.new<float>(1)
//     y2PriceHistoryHTFS.clear()
//     y1PriceHistoryHTFS.clear()
//     isHighFirstShortArrHTF.clear()
//     getBreakoutPointDnArrShortHTF.clear()


//     for i = 0 to closeArrEnd.size() - 1

//         getHigh = highArrEnd.get(i)
//         getLow  = lowArrEnd.get(i)
//         getTime = timeArrEnd.get(i)
//         getAtr  = atrArrHTF.get(i) * bestATRshorthtf
    
//         // zzLineChangeFinalHTFS.first().delete()

//         if dirHTFS == 1

//             pointHTFS := math.max(pointHTFS, getHigh)
//             y2PriceFinalHTFS.set(0, pointHTFS)

//             timePHTFS := switch getHigh == pointHTFS 

//                 true => getTime 
//                 =>      timePHTFS

//             y2xFinalHTFS.set(0, timePHTFS)

//             addition = math.abs(pointHTFS - y1PriceFinalHTFS.first()) * buffer

//             if getLow <= pointHTFS - getAtr - addition

//                 y1PriceFinalHTFS.set(0, pointHTFS)
//                 y2PriceFinalHTFS.set(0, getLow)
//                 y1xFinalHTFS    .set(0, timePHTFS)
//                 y2xFinalHTFS    .set(0, getTime)
//                 dirHTFS   := -1 
//                 pointHTFS := getLow
//                 timePHTFS := getTime

//         else if dirHTFS == -1

//             pointHTFS := math.min(getLow, pointHTFS)
//             y2PriceFinalHTFS.set(0, pointHTFS)

//             timePHTFS := switch getLow == pointHTFS 

//                 true => getTime
//                 =>      timePHTFS

//             y2xFinalHTFS.set(0, timePHTFS)

//             addition = math.abs(pointHTFS - y1PriceFinalHTFS.first()) * buffer

//             if getHigh >= pointHTFS + getAtr + addition 

//                 y1PriceFinalHTFS.set(0, pointHTFS)
//                 y2PriceFinalHTFS.set(0, getHigh)
//                 y1xFinalHTFS    .set(0, timePHTFS)
//                 y2xFinalHTFS    .set(0, getTime)
//                 dirHTFS   := 1 
//                 pointHTFS := getHigh 
//                 timePHTFS := getTime

//         if dirHTFS == 0 

//             if getHigh >= pointHTFS + getAtr

//                 y1PriceFinalHTFS.set(0, pointHTFS)
//                 y2PriceFinalHTFS.set(0, getHigh)
//                 y1xFinalHTFS    .set(0, timePHTFS)
//                 y2xFinalHTFS    .set(0, getTime)
//                 dirHTFS   := 1
//                 pointHTFS := getHigh
//                 timePHTFS := getTime 


//             else if getLow <= pointHTFS + getAtr

//                 y1PriceFinalHTFS.set(0, pointHTFS)
//                 y2PriceFinalHTFS.set(0, getLow)
//                 y1xFinalHTFS    .set(0, timePHTFS)
//                 y2xFinalHTFS    .set(0, getTime)
//                 dirHTFS   := -1 
//                 pointHTFS := getLow
//                 timePHTFS := getTime

//         y2PriceHistoryHTFS.push(y2PriceFinalHTFS.first())
//         y1PriceHistoryHTFS.push(y1PriceFinalHTFS.first())
        
//         isHighFirstShortFinal = y2PriceFinalHTFS.first() > y1PriceFinalHTFS.first()

//         isHighFirstShortArrHTF.push(isHighFirstShortFinal)

//         if y1PriceFinalHTFS.first() < y2PriceFinalHTFS.first()
//             getBreakoutPointDnShortLoopHTF := y1PriceFinalHTFS.first()
            
//         getBreakoutPointDnArrShortHTF.push(getBreakoutPointDnShortLoopHTF)

//     pointArr.set(0, pointHTFS)
//     timePArr.set(0, int(timePHTFS))
//     dirArr  .set(0, dirHTFS)

//     [pointArr, timePArr, dirArr]

// method quickPolyLab(array<label> masterLabels, masterLabelsTimes, getTime, getLow, getOpen, isOpen, getExit, getEntry, I, inTradeCoords, inTradeOHLC,historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedExit, getDiv, labelSize) => 
    
//     y = switch isOpen 

//         true => getOpen 
//         =>      getExit

//     getIndex = masterLabelsTimes.binary_search_rightmost(getTime)

//     masterLabelsTimes.insert(getIndex, getTime)


//     masterLabels.insert(getIndex, label.new(getTime, getLow, text = "▼", style = label.style_text_outline, xloc = xloc.bar_time, 
//                  size      = labelSize, 
//                  yloc      = yloc.abovebar,
//                  textcolor = color.rgb(128, 116, 255), 
//                  color     = chart.fg_color,  
//                  tooltip   = "Trade Entry: " + str.tostring(getEntry, format.mintick) + "\nTrade Exit: " 
//                  + str.tostring(y, format.mintick) 
//                  + "\nProfit: " + str.tostring((y / getEntry - 1) * 100 / getDiv, format.percent
//                  )))


//     switch math.sign(y / getEntry - 1)

//         1  => historicalLongsPFPFORIT.set(0, historicalLongsPFPFORIT.get(0) + (math.abs(y - getEntry) / getDiv))
//         -1 => historicalLongsPFLOSS.set(0, historicalLongsPFLOSS.get(0) + (math.abs(y - getEntry) / getDiv))

//     polyline.new(inTradeCoords,  xloc       = xloc.bar_time, 
//                                  line_color = color.rgb(128, 116, 255)
//                                  )

//     inTradeCoords.clear()

//     getSize = inTradeOHLC.size() - 1

//     for x = I to I - getSize
//         inTradeOHLC.push(chart.point.from_time(timeArrEnd.get(x), ohlc4ArrEnd.get(x)))

//     polyline.new(inTradeOHLC, xloc = xloc.bar_time, 
//                      line_color = #00000000, 
//                      fill_color = color.new(color.rgb(128, 116, 255), 90)
//                      )
    
//     line.new(x1 = getEntryTime, y1 = getFixedExit, x2 = getFStime, y2 = getFixedExit, 
//                                  xloc  = xloc.bar_time, 
//                                  color = color.new(color.rgb(128, 116, 255), 50)
//                                  )

//     inTradeOHLC.clear()

// method labelForRRtpLong(array<label> masterLabels, masterLabelsTimes, getTime, getLow, getOpen, isOpen, getExit, getEntry, I, inTradeCoords, inTradeOHLC,historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedExit, getDiv, labelSize) => 

//     y = switch isOpen 

//         true => getOpen 
//         =>      getExit

//     getIndex = masterLabelsTimes.binary_search_rightmost(getTime)

//     masterLabelsTimes.insert(getIndex, getTime)

//     masterLabels.insert(getIndex, label.new(getTime, getLow, text = "▼", style = label.style_text_outline, xloc = xloc.bar_time, 
//                  size      = labelSize, 
//                  yloc      = yloc.abovebar,
//                  textcolor = color.rgb(128, 116, 255), 
//                  color     = chart.fg_color,  
//                  tooltip   = "Trade Entry: " + str.tostring(getEntry, format.mintick) + "\nTrade Exit: " 
//                  + str.tostring(y, format.mintick) 
//                  + "\nProfit: " + str.tostring((y / getEntry - 1) * 100 / getDiv, format.percent
//                  )))


//     switch math.sign(y / getEntry - 1)

//         1  => historicalLongsPFPFORIT.set(0, historicalLongsPFPFORIT.get(0) + (math.abs(y - getEntry) / getDiv))
//         -1 => historicalLongsPFLOSS.set(0, historicalLongsPFLOSS.get(0) + (math.abs(y - getEntry) / getDiv))



// method quickPolyLabShorts(array<label> masterLabels, masterLabelsTimes, getTime, getLow, getOpen, isOpen, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd, historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize) => 

//     y = switch isOpen 

//         true => getOpen 
//         =>      getExit

//     inTradeCoords.push(chart.point.from_time(getTime, getExit))
//     inTradeOHLC  .push(chart.point.from_time(getTime, getExit))

//     getIndex = masterLabelsTimes.binary_search_rightmost(getTime)

//     masterLabelsTimes.insert(getIndex, getTime)
//     masterLabels     .insert(getIndex, label.new(getTime, getLow, text = "▲", style = label.style_text_outline, 
//                       xloc      = xloc.bar_time, 
//                       size      = labelSize, 
//                       yloc      = yloc.belowbar,
//                       textcolor = color.rgb(128, 116, 255), 
//                       color     = chart.fg_color, 
//                       tooltip   = "Trade Entry: " + str.tostring(getEntry) + "\nTrade Exit: " + str.tostring(y) + "\nProfit: " 
//                       + str.tostring((y / getEntry - 1) * 100 * -1 / getDiv, format.percent
//                       )))
    
//     if polyLineTrail.size() > 40 and polyline.all.size() > 90

//         polyLineTrail.shift().delete()
//         polyLineTrail.shift().delete()

//     polyLineTrail.push(polyline.new(inTradeCoords, 
//                          xloc       = xloc.bar_time, 
//                          line_color = color.rgb(255, 116, 116)))
    
//     inTradeCoords.clear()
    
//     getSize = inTradeOHLC.size() + 1

//     for x = I to I - getSize
//         inTradeOHLC.push(chart.point.from_time(timeArrEnd.get(x), ohlc4ArrEnd.get(x)))
    
//     polyLineTrail.push(polyline.new(inTradeOHLC, 
//                          xloc       = xloc.bar_time, 
//                          line_color = #00000000, 
//                          fill_color = color.new(color.rgb(255, 116, 116), 90)))

//     inTradeOHLC.clear()


//     line.new(x1 = getFStime, y1 = getFixedStop, x2 = getEndTime, y2 = getFixedStop, 
//                          xloc  = xloc.bar_time, 
//                          color = color.new(color.rgb(255, 116, 116), 50))


//     switch math.sign(y / getEntry - 1)
    
//         -1 => historicalShortPFPFORIT.set(0, historicalShortPFPFORIT.get(0) + (math.abs(y - getEntry) / getDiv))
//         1  => historicalShortPFLOSS  .set(0, historicalShortPFLOSS.get(0)   + (math.abs(y - getEntry) / getDiv))
    


// method labelForRRtpShort(array<label> masterLabels, masterLabelsTimes, getTime, getLow, getOpen, isOpen, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd, historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize) => 

//     y = switch isOpen 

//         true => getOpen 
//         =>      getExit

//     inTradeCoords.push(chart.point.from_time(getTime, getExit))
//     inTradeOHLC  .push(chart.point.from_time(getTime, getExit))

//     getIndex = masterLabelsTimes.binary_search_rightmost(getTime)

//     masterLabelsTimes.insert(getIndex, getTime)
//     masterLabels     .insert(getIndex, label.new(getTime, getLow, text = "▲", style = label.style_text_outline, 
//                       xloc      = xloc.bar_time, 
//                       size      = labelSize, 
//                       yloc      = yloc.belowbar,
//                       textcolor = color.rgb(128, 116, 255), 
//                       color     = chart.fg_color, 
//                       tooltip   = "Trade Entry: " + str.tostring(getEntry) + "\nTrade Exit: " + str.tostring(y) + "\nProfit: " 
//                       + str.tostring((y / getEntry - 1) * 100 * -1 / getDiv, format.percent
//                       )))
    
//     switch math.sign(y / getEntry - 1)
    
//         -1 => historicalShortPFPFORIT.set(0, historicalShortPFPFORIT.get(0) + (math.abs(y - getEntry) / getDiv))
//         1  => historicalShortPFLOSS  .set(0, historicalShortPFLOSS.get(0)   + (math.abs(y - getEntry) / getDiv))
    

// export historicalLongTrades(float bestTrailing, float bestTarget, array<float> lowArrEnd, array<float> closeArrEnd, array<float>openArrEnd, array<float> atrArrLTF, 
//      array<int> timeArrEnd,  array<float> ohlc4ArrEnd, bool isLastBar, string tradeType, array<bool> isHighFirstLongArrHTF,
//          bool tradeLong, array<float> y1PriceHistoryHTF, array<float> y2PriceHistoryHTF, array<float> getBreakoutPointUpArrLongLTF, array<float> ltfCloArr, array<float> ltfCloArr1,
//              float buySellRange, array<float> historicalLongsPFPFORIT, array<float> historicalLongsPFLOSS, array<label> masterLabels, array<int> masterLabelsTimes,
//                  bool useRR, array<float> highArrEnd, array<bool> closer2lowArr, float RRmult, float stopLossAmount, string labelSize, bool closeEOD, array<bool> isLastBarArray
//                  )=> 

//     if tradeLong 
 
  
//         getEntry    = 0.
//         getExit     = 0. 
//         getATRTARR  = bestTrailing, 
//         getATRPTARR = bestTarget
//         inTrade     = 0, 
//         getLimit    = 0.
//         getTrigger  = 0.
//         getDiv      = 0 
//         getRRtp     = 0.

//         inTradeCoords = array.new<chart.point>(), inTradeOHLC = array.new<chart.point>()

//         getFStime = 0, getEntryTime = 0, getFixedStop = 0.

//         for I = 0 to closeArrEnd.size() - 1

//             getLow    = lowArrEnd.get(I)
//             getClose  = closeArrEnd.get(I)
//             getOpen   = openArrEnd.get(I)
//             getATRLTF = atrArrLTF.get(I)
//             getTime   = timeArrEnd.get(I)
//             getHigh   = highArrEnd.get(I)
//             closer2low = closer2lowArr.get(I)
//             isLastBarArr = isLastBarArray.get(I)

//             if inTrade == 1
            
//                 getFStime := getTime

//                 if getDiv == 1 and useRR
                    
//                     if getOpen >= getRRtp

//                         getDiv := 2

//                         masterLabels.labelForRRtpLong(masterLabelsTimes, getTime, getLow, getOpen, true, getRRtp, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)

//                     else if getHigh >= getRRtp 
                    
//                         if getLow > getExit 
                        
//                             getDiv := 2

//                             masterLabels.labelForRRtpLong(masterLabelsTimes, getTime, getLow, getOpen, false, getRRtp, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)

    
//                         else 
                        
//                             if not closer2low 
                            
//                                 getDiv := 2
//                                 masterLabels.labelForRRtpLong(masterLabelsTimes, getTime, getLow, getOpen, false, getRRtp, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)


//                 if getOpen <= getExit
                
//                     inTrade  := 0
//                     getLimit := 0

//                     masterLabels.quickPolyLab(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)


//                 else if getLow <= getExit 
                
//                     inTrade  := 0
//                     getLimit := 0

//                     masterLabels.quickPolyLab(masterLabelsTimes, getTime, getLow, getOpen, false, getExit, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)

//                 else if isLastBarArr and closeEOD 

//                     inTrade  := 0 
//                     getLimit := 0


//                     masterLabels.quickPolyLab(masterLabelsTimes, getTime, getLow, getClose, false, getClose, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)


//                 else if getClose >= getTrigger and isLastBar
                
//                     inTrade  := 2
//                     getExit  := math.max(getExit, math.round_to_mintick(getClose - getATRLTF * getATRTARR))
//                     getLimit := 0

//             else if inTrade == 2 
            
//                 if getDiv == 1 and useRR
                    
//                     if getOpen >= getRRtp

//                         masterLabels.labelForRRtpLong(masterLabelsTimes, getTime, getLow, getOpen, true, getRRtp, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)

//                     else if getHigh >= getRRtp 
                    
//                         if getLow > getExit 
                        
//                             getDiv := 2

//                             masterLabels.labelForRRtpLong(masterLabelsTimes, getTime, getLow, getOpen, false, getRRtp, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)

    
//                         else 
                        
//                             if not closer2low 
                            
//                                 getDiv := 2
//                                 masterLabels.labelForRRtpLong(masterLabelsTimes, getTime, getLow, getOpen, false, getRRtp, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)


//                 if getOpen <= getExit
                
//                     inTrade  := 0
//                     getLimit := 0

//                     masterLabels.quickPolyLab(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)


//                 else if getLow <= getExit
                
//                     inTrade  := 0
//                     getLimit := 0

//                     masterLabels.quickPolyLab(masterLabelsTimes, getTime, getLow, getOpen, false, getExit, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)


//                 else if isLastBarArr and closeEOD 

//                     inTrade  := 0 
//                     getLimit := 0


//                     masterLabels.quickPolyLab(masterLabelsTimes, getTime, getLow, getClose, false, getClose, getEntry, I, inTradeCoords, inTradeOHLC, historicalLongsPFPFORIT, historicalLongsPFLOSS, timeArrEnd, ohlc4ArrEnd, getFStime, getEntryTime, getFixedStop, getDiv, labelSize)


//                 else if isLastBar

//                     getExit := math.max(getExit, math.round_to_mintick(getClose - getATRLTF * getATRTARR))


//             if tradeType == "Breakout"

//                 if isHighFirstLongArrHTF.get(I) and tradeLong and inTrade == 0

// 	        		getHistoryY1 = y1PriceHistoryHTF.get(I)
//                     Range 	     = math.abs(y2PriceHistoryHTF.get(I) - getHistoryY1) * (buySellRange / 100)

//                     getBreakPointUp = getBreakoutPointUpArrLongLTF.get(I - 1)

//                     if ltfCloArr.get(I) > getBreakPointUp and getBreakPointUp != 0 and 
//                        ltfCloArr1.get(I) <= getBreakPointUp and getBreakPointUp <= getHistoryY1 + Range 

//                         getEntryTime := getTime
//                         getEntry     := getClose
//                         inTrade      := 1
//                         getTrigger   := math.round_to_mintick(getClose + getATRLTF * getATRPTARR)

//                         exitPrice    = math.round_to_mintick(getClose - getATRLTF * getATRTARR)

//                         getExit      := exitPrice
//                         getFixedStop := getExit

//                         exitDistance = math.abs(exitPrice - getClose) * RRmult

//                         getRRtp      := math.round_to_mintick(getClose + exitDistance)
//                         getDiv       := 1

//                         getIndex = masterLabelsTimes.binary_search_rightmost(getTime)

//                         masterLabelsTimes.insert(getIndex, getTime)
                    
//                         toolTipAddRR = switch useRR

//                             true => "\nTP1: " + str.tostring(getRRtp, format.mintick) + " (" + str.tostring((getRRtp / getClose - 1) * 100, format.percent) + ")"
//                             =>      ""

//                         risk = getEntry - exitPrice

//                         contracts = switch syminfo.type == "futures" 
                            
//                             false => stopLossAmount / risk
//                             =>       math.floor(stopLossAmount / risk)

//                         masterLabels.insert(getIndex, label.new(getTime, getLow, text = "▲", style = label.style_text_outline, 
//                                            xloc      = xloc.bar_time, 
//                                            size      = labelSize, 
//                                            yloc      = yloc.belowbar,
//                                            textcolor = #74ffbc, 
//                                            color     = chart.fg_color, 
//                                            tooltip   = "Entry: " + str.tostring(getClose, format.mintick) 
//                                              + "\nTrailing PT Trigger: " + str.tostring(getTrigger, format.mintick) + " (" + str.tostring((getTrigger / getClose - 1) * 100, format.percent) + ")"
//                                              + "\nInitial SL: " + str.tostring(getExit, format.mintick)             + " (" + str.tostring((getExit / getClose - 1) * 100, format.percent)    + ")"
//                                              + toolTipAddRR + "\n" 
//                                              + "Ideal Amount: " + str.tostring(contracts, "###,###.###")
//                                              ))

//             else if tradeType == "Cheap"

//                 if isHighFirstLongArrHTF.get(I) and tradeLong and inTrade == 0

// 	        		getHistoryY1    = y1PriceHistoryHTF.get(I)
//                     Range           = math.abs(y2PriceHistoryHTF.get(I) - getHistoryY1) * (buySellRange / 100)
//                     getBreakPointUp = getBreakoutPointUpArrLongLTF.get(I - 1)

//                     if   ltfCloArr.get(I) < getBreakPointUp and getBreakPointUp != 0 and 
//                          ltfCloArr1.get(I) >= getBreakPointUp and getBreakPointUp <= getHistoryY1 + Range 

//                         getEntryTime := getTime

//                         getIndex = masterLabelsTimes.binary_search_rightmost(getTime)

//                         masterLabelsTimes.insert(getIndex, getTime)

//                         getEntry     := getClose 
//                         inTrade      := 1
//                         getTrigger   := math.round_to_mintick(getClose + getATRLTF * getATRPTARR)

//                         exitPrice    = math.round_to_mintick(getClose - getATRLTF * getATRTARR)

//                         getExit      := exitPrice
//                         getFixedStop := getExit

//                         getDiv := 1

//                         exitDistance = math.abs(getClose - exitPrice) * RRmult

//                         getRRtp := math.round_to_mintick(getClose + exitDistance)
                    

//                         risk = getEntry - exitPrice

//                         contracts = switch syminfo.type == "futures" 
                            
//                             false => stopLossAmount / risk
//                             =>       math.floor(stopLossAmount / risk)


                      
//                         toolTipAddRR = switch useRR

//                             true => "\nTP1: " + str.tostring(getRRtp, format.mintick) + " (" + str.tostring((getRRtp / getClose - 1) * 100, format.percent) + ")"
//                             =>      ""

//                         masterLabels.insert(getIndex, label.new(getTime, getLow, text = "▲", 
//                                      style     = label.style_text_outline, 
//                                      xloc      = xloc.bar_time, 
//                                      size      = labelSize, 
//                                      yloc      = yloc.belowbar,
//                                      textcolor = #74ffbc, 
//                                      color     = chart.fg_color, 
//                                      tooltip   = "Entry: " + str.tostring(getClose, format.mintick) 
//                                      + "\nTrailing PT Trigger: " + str.tostring(getTrigger, format.mintick) + " (" + str.tostring((getTrigger / getClose - 1) * 100, format.percent) + ")"
//                                      + "\nInitial SL: " + str.tostring(getExit, format.mintick) + " (" + str.tostring((getExit / getClose - 1) * 100, format.percent)    + ")"
//                                      + toolTipAddRR + "\n"
//                                      + "Ideal Amount: " + str.tostring(contracts, "###,###.###")

//                                      ))

//             if inTrade == 2

//                 inTradeCoords.push(chart.point.from_time(getTime, getExit))
//                 inTradeOHLC  .push(chart.point.from_time(getTime, getExit))

//                 if I == closeArrEnd.size() - 1

//                     polyline.new(inTradeCoords, xloc = xloc.bar_time, line_color = color.rgb(128, 116, 255))
                    
//                     inTradeCoords.clear()
//                     getSize = inTradeOHLC.size() - 1
//                     for x = I to I - getSize
//                         inTradeOHLC.push(chart.point.from_time(timeArrEnd.get(x), ohlc4ArrEnd.get(x)))

//                     polyline.new(inTradeOHLC, xloc = xloc.bar_time, 
//                                      line_color = #00000000, 
//                                      fill_color = color.new(color.rgb(128, 116, 255), 90))

//                     inTradeOHLC.clear()

//                     line.new(x1 = getEntryTime, y1 = getFixedStop, x2 = getFStime, y2 = getFixedStop, 
//                                      xloc  = xloc.bar_time, 
//                                      color = color.new(color.rgb(128, 116, 255), 50))

//             if I == closeArrEnd.size() - 1
//                 if inTrade == 1
    
//                     line.new(x1 = getEntryTime, y1 = getFixedStop, x2 = getFStime, y2 = getFixedStop, 
//                                      xloc  = xloc.bar_time, 
//                                      color = color.new(color.rgb(128, 116, 255), 50))


//         [getEntry, getExit, inTrade, getLimit, getTrigger, getDiv, getRRtp]

    
// export historicalShorTrades(float bestTrailingS, float bestTargetS, array<float> highArrEnd, array<float> lowArrEnd, array<float> closeArrEnd, array<float>openArrEnd, array<float> atrArrLTF, 
//      array<int> timeArrEnd,  array<float> ohlc4ArrEnd, bool isLastBar, string tradeType, array<bool> isHighFirstShortArrHTF,
//          bool tradeShort, array<float> y1PriceHistoryHTFS, array<float> y2PriceHistoryHTFS, array<float> getBreakoutPointDnArrShortLTF, array<float> ltfCloArr, array<float> ltfCloArr1,
//              float buySellRange, array<float> historicalShortPFPFORIT, array<float> historicalShortPFLOSS, array<label> masterLabels, array<int> masterLabelsTimes, 
//                  bool useRR, float RRmult, array<bool> closer2LowArr, float stopLossAmount, string labelSize, bool closeEOD, array<bool> isLastBarArray
//                  ) =>


//     if tradeShort 


//         getEntry   = 0., getExit = 0. 
//         getATRTARR = bestTrailingS, getATRPTARR = bestTargetS
    
//         inTrade = 0, getLimit = 0., getTrigger = 0.
//         inTradeCoords = array.new<chart.point>(), inTradeOHLC = array.new<chart.point>()

//         getFStime     = 0, getEndTime = 0, getFixedStop = 0.
//         polyLineTrail = array.new<polyline>()

//         getDiv      = 0 
//         getRRtp     = 0.

//         for I = 0 to closeArrEnd.size() - 1

//             if masterLabels.size() > 450 

//                 for i = 0 to masterLabels.size() - 450 
    
//                     masterLabels.shift().delete()
//                     masterLabelsTimes.shift()
    
//             getLow = lowArrEnd.get(I)
//             getClose = closeArrEnd.get(I)
//             getOpen = openArrEnd.get(I)
//             getATRLTF = atrArrLTF.get(I)
//             getTime = timeArrEnd.get(I)
//             getHigh = highArrEnd.get(I)
//             closer2low = closer2LowArr.get(I)
//             isLastBarArr = isLastBarArray.get(I)
            
    
//             if inTrade == -1
            
//                 getEndTime := getTime

//                 if getDiv == 1 and useRR
                    
//                     if getOpen <= getRRtp

//                         getDiv := 2

//                         masterLabels.labelForRRtpShort(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd,  historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)

//                     else if getLow <= getRRtp 
                    
//                         if getHigh < getExit 
                        
//                             getDiv := 2

//                             masterLabels.labelForRRtpShort(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd,  historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)

    
//                         else 
                        
//                             if closer2low 
                            
//                                 getDiv := 2
//                                 masterLabels.labelForRRtpShort(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd,  historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)


//                 if getOpen >= getExit
                
//                     inTrade := 0
//                     getLimit := 0
    
//                     masterLabels.quickPolyLabShorts(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd,  historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)

//                 else if getHigh >= getExit 
                
//                     inTrade := 0
//                     getLimit := 0
    
//                     masterLabels.quickPolyLabShorts(masterLabelsTimes, getTime, getLow, getOpen, false, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd, historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)
    
//                 else if isLastBarArr and closeEOD
                
//                     inTrade := 0
//                     getLimit := 0
    
//                     masterLabels.quickPolyLabShorts(masterLabelsTimes, getTime, getLow, getClose, false, getClose, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd, historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)



//                 else if getClose <= getTrigger and isLastBar
                
//                     inTrade := -2
//                     getExit := math.min(getExit, math.round_to_mintick(getClose + getATRLTF * getATRTARR))
//                     getLimit := 0
    
//             else if inTrade == -2 
            

//                 if getDiv == 1 and useRR
                    
//                     if getOpen <= getRRtp

//                         getDiv := 2

//                         masterLabels.labelForRRtpShort(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd,  historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)

//                     else if getLow <= getRRtp 
                    
//                         if getHigh < getExit 
                        
//                             getDiv := 2

//                             masterLabels.labelForRRtpShort(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd,  historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)

    
//                         else 
                        
//                             if closer2low 
                            
//                                 getDiv := 2
//                                 masterLabels.labelForRRtpShort(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd,  historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)



//                 if getOpen >= getExit
                
//                     inTrade := 0
//                     getLimit := 0
                        
//                     masterLabels.quickPolyLabShorts(masterLabelsTimes, getTime, getLow, getOpen, true, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd,  historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)

//                 else if getHigh >= getExit
                
//                     inTrade := 0
//                     getLimit := 0
    
//                     masterLabels.quickPolyLabShorts(masterLabelsTimes, getTime, getLow, getOpen, false, getExit, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd, historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)
    
//                 else if isLastBarArr and closeEOD
                
//                     inTrade := 0
//                     getLimit := 0
    
//                     masterLabels.quickPolyLabShorts(masterLabelsTimes, getTime, getLow, getClose, false, getClose, getEntry, I, inTradeCoords, inTradeOHLC, timeArrEnd, ohlc4ArrEnd, historicalShortPFPFORIT, historicalShortPFLOSS, getFStime, getEndTime, getFixedStop, polyLineTrail, getDiv, labelSize)


//                 else if isLastBar
                
                
//                     getExit := math.min(getExit, math.round_to_mintick(getClose + getATRLTF * getATRTARR))
    
    
//             if inTrade == 0 and I >= 1
//                 if tradeType == "Breakout"
                
//                     if not isHighFirstShortArrHTF.get(I) and tradeShort 
                    
//                         Range = math.abs(y2PriceHistoryHTFS.get(I) - y1PriceHistoryHTFS.get(I)) * (buySellRange / 100)
    
//                         getBreakPointDn = getBreakoutPointDnArrShortLTF.get(I - 1)
    
//                         if ltfCloArr.get(I) < getBreakPointDn and getBreakPointDn != 20e20 and ltfCloArr1.get(I) >= getBreakPointDn and getBreakPointDn >= y1PriceHistoryHTFS.get(I) - Range 
//                             getEntry := getClose 
//                             inTrade := -1
//                             getTrigger := math.round_to_mintick(getClose - getATRLTF * getATRPTARR)


//                             exitPrice = math.round_to_mintick(getClose + getATRLTF * getATRTARR)

//                             getExit    := exitPrice
//                             getFixedStop := getExit
//                             getFStime := getTime 

//                             exitDistance = math.abs(getClose - exitPrice) * RRmult


//                             getRRtp := math.round_to_mintick(getClose - exitDistance)
//                             getDiv := 1

//                             getIndex = masterLabelsTimes.binary_search_rightmost(getTime)

//                             masterLabelsTimes.insert(getIndex, getTime)

//                             toolTipAddRR = switch useRR

//                                 true => "\nTP1: " + str.tostring(getRRtp, format.mintick) + " (" + str.tostring((getRRtp / getClose - 1) * 100, format.percent) + ")"
//                                 =>      ""


//                             risk = math.abs(getEntry - exitPrice)

//                             contracts = switch syminfo.type == "futures" 

//                                 false => stopLossAmount / risk
//                                 =>       math.floor(stopLossAmount / risk)


//                             masterLabels.insert(getIndex, label.new(getTime, getLow, text = "▼", style = label.style_text_outline, 
//                                      xloc = xloc.bar_time, 
//                                      size = labelSize, 
//                                      yloc = yloc.abovebar,
//                                      textcolor = color.rgb(255, 116, 116), color = chart.fg_color, 
//                                      tooltip = "Entry: " + str.tostring(getEntry, format.mintick) 
//                                      + "\nTrailing PT Trigger: " + str.tostring(getTrigger, format.mintick) + " (" + str.tostring((getTrigger / getClose - 1) * 100, format.percent) + ")" 
//                                      + "\nInitial SL: " + str.tostring(getExit, format.mintick)  + " (" + str.tostring((getExit / getClose - 1) * 100, format.percent)    + ")"
//                                      + toolTipAddRR + 
//                                          "\nIdeal Amount: " + str.tostring(contracts, "###,###.###")
//                                      ))
    
//                 else if tradeType == "Cheap"
                
//                     if not isHighFirstShortArrHTF.get(I) and tradeShort 
                    
//                         Range = math.abs(y2PriceHistoryHTFS.get(I) - y1PriceHistoryHTFS.get(I)) * (buySellRange / 100)
    
//                         getBreakPointDn = getBreakoutPointDnArrShortLTF.get(I - 1)
    
//                         if ltfCloArr.get(I) > getBreakPointDn and getBreakPointDn != 20e20 and ltfCloArr1.get(I) <= getBreakPointDn and getBreakPointDn >= y1PriceHistoryHTFS.get(I) - Range 
                        
//                             getEntry := getClose 
//                             inTrade := -1
//                             getTrigger := math.round_to_mintick(getClose - getATRLTF * getATRPTARR)

//                             exitPrice = math.round_to_mintick(getClose + getATRLTF * getATRTARR)

//                             getExit := exitPrice
//                             getFixedStop := getExit
//                             getFStime := getTime 

//                             exitDistance = math.abs(getClose - exitPrice) * RRmult

//                             getRRtp := math.round_to_mintick(getClose - exitDistance)

//                             getDiv := 1

//                             getIndex = masterLabelsTimes.binary_search_rightmost(getTime)

//                             masterLabelsTimes.insert(getIndex, getTime)


//                             toolTipAddRR = switch useRR

//                                 true => "\nTP1: " + str.tostring(getRRtp, format.mintick) + " (" + str.tostring((getRRtp / getClose - 1) * 100, format.percent) + ")"
//                                 =>      ""

//                             risk = math.abs(getEntry - exitPrice)

//                             contracts = switch syminfo.type == "futures" 
                            
//                                 false => stopLossAmount / risk
//                                 =>       math.floor(stopLossAmount / risk)


//                             masterLabels.insert(getIndex, label.new(getTime, getLow, text = "▼", style = label.style_text_outline, 
//                                          xloc = xloc.bar_time, 
//                                          size = labelSize, 
//                                          yloc = yloc.abovebar,
//                                          textcolor = color.rgb(255, 116, 116), 
//                                          color = chart.fg_color, 
//                                          tooltip = "Entry: " + str.tostring(getEntry, format.mintick) 
//                                          + "\nTrailing PT Trigger: " + str.tostring(getTrigger, format.mintick) + " (" + str.tostring((getTrigger / getClose - 1) * 100, format.percent) + ")"
//                                          + "\nInitial SL: " + str.tostring(getExit, format.mintick) + " (" + str.tostring((getExit / getClose - 1) * 100, format.percent)    + ")"
//                                          + toolTipAddRR + 
//                                          "\nIdeal Amount: " + str.tostring(contracts, "###,###.###")

//                                           ))
    
//             if inTrade == -2 

//                 inTradeCoords.push(chart.point.from_time(getTime, getExit))
//                 inTradeOHLC  .push(chart.point.from_time(getTime, getExit))

//                 if I == closeArrEnd.size() - 1

//                     polyline.new(inTradeCoords, xloc = xloc.bar_time, 
//                                  line_color = color.rgb(255, 116, 116))

//                     inTradeCoords.clear()
//                     getSize = inTradeOHLC.size() + 1
//                     for x = I to I - getSize
//                         inTradeOHLC.push(chart.point.from_time(timeArrEnd.get(x), ohlc4ArrEnd.get(x)))
                    
//                     polyline.new(inTradeOHLC, xloc = xloc.bar_time, line_color = #00000000, 
//                                      fill_color = color.new(color.rgb(255, 116, 116), 90))
                    
//                     inTradeOHLC.clear()
//                     line.new(x1 = getFStime, y1 = getFixedStop, x2 = getEndTime, y2 = getFixedStop, 
//                                      xloc  = xloc.bar_time, 
//                                      color = color.new(color.rgb(255, 116, 116), 50))

//             if I == closeArrEnd.size() - 1

//                 if inTrade == -1

//                     line.new(x1 = getFStime, y1 = getFixedStop, x2 = getEndTime, y2 = getFixedStop, 
//                                      xloc  = xloc.bar_time, 
//                                      color = color.new(color.rgb(255, 116, 116), 50))

//         [getEntry, getExit, inTrade, getLimit, getTrigger, getDiv, getRRtp]


// export zz(float atr, array<int> y1x, array<int> y2x, array<float> y1Price, array<float> y2Price, color lineCol, float zz1, int zz2, int zz3,
//      array<int> y1xF, array<float> y1pF, array<int> y2xF, array<float> y2pF, bool showLine, bool showProj, bool trainingComplete) => 

//     if trainingComplete

//         var point = zz1//zzltfpL
//         var timeP = zz2//zzltftL
//         var dir   = zz3//zzltfdL

//         if na(y1Price.first())

//             y1Price.set(0, y1pF.first())
//             y1x    .set(0, y1xF.first())

//         var zzLine = array.from(line.new(y1xF.first(), y1pF.first(), y2xF.first(), y2pF.first(), color = lineCol, width = 2, xloc = xloc.bar_time))

//         if not showLine and zzLine.size() > 0 
//             zzLine.shift().delete()

//         var zzLineChange = array.new_line(2)

//         zzLineChange.first().delete()
//         zzLineChange.last().delete()

//         if dir == 1

//             point := math.max(point, high)
//             y2Price.set(0, point)

//             timeP := switch high == point 
//                 true => time 
//                 =>      timeP

//             y2x.set(0, timeP)

//             if showLine

//                 zzLine.last().set_x2(timeP)
//                 zzLine.last().set_y2(point)

//             RangeX = math.abs(point - (point - atr)) * .1

//             if showLine and showProj
//                 zzLineChange.set(0, line.new(time("", -1), point - atr , time("", -3), point - atr + RangeX, 
//                                                  color = color.rgb(255, 116, 116), 
//                                                  width = 2, 
//                                                  xloc  = xloc.bar_time, 
//                                                  style = line.style_dotted
//                                                  ))

//                 zzLineChange.set(1, line.new(zzLine.last().get_x2(), zzLine.last().get_y2(), time("", -1), point - atr , 
//                                                  color = color.rgb(255, 116, 116), 
//                                                  width = 2, 
//                                                  xloc  = xloc.bar_time, 
//                                                  style = line.style_dotted
//                                                  ))

//             if low <= point - atr 

//                 if showLine

//                     zzLine.last().set_style(line.style_dotted)
//                     zzLine.last().set_color(color.new(lineCol, 50))

//                     zzLine.push(line.new(timeP, point, time, low, xloc = xloc.bar_time, 
//                                          color = lineCol, 
//                                          width = 2
//                                          ))

//                 y1Price.set(0, point)
//                 y2Price.set(0, low)
//                 y1x    .set(0, timeP)
//                 y2x    .set(0, time)
//                 dir   := -1 
//                 point := low
//                 timeP := time

//                 if showLine

//                     zzLineChange.first().delete()
//                     zzLineChange.last().delete()

//                 if showLine

//                     Range = math.abs(point - (point + atr)) * .1

//                     if showProj

//                         zzLineChange.set(0, line.new(time("", -1), point + atr , time("", -3), 
//                                               point + atr - Range, 
//                                               color = #74ffbc, 
//                                               width = 2, 
//                                               xloc  = xloc.bar_time, 
//                                               style = line.style_dotted
//                                               ))

//                         zzLineChange.set(1, line.new(zzLine.last().get_x2(), zzLine.last().get_y2(), time("", -1), point + atr, 
//                                              color = #74ffbc, 
//                                              width = 2,
//                                              xloc  = xloc.bar_time, 
//                                              style = line.style_dotted
//                                              ))

//         else if dir == -1

//             point := math.min(low, point)
//             y2Price.set(0, point)

//             timeP := switch low == point 
//                 true => time 
//                 =>      timeP

//             y2x.set(0, timeP)

//             if showLine
//                 zzLine.last().set_x2(timeP)
//                 zzLine.last().set_y2(point)
//                 RangeY = math.abs(point - (point + atr)) * .1
//                 if showProj
//                     zzLineChange.set(0, line.new(time("", -1), point + atr , time("", -3), point + atr - RangeY, 
//                                              color = #74ffbc, 
//                                              width = 2, 
//                                              xloc  = xloc.bar_time, 
//                                              style = line.style_dotted
//                                              ))

//                     zzLineChange.set(1, line.new(zzLine.last().get_x2(), zzLine.last().get_y2(), time("", -1), point + atr , 
//                                              color = #74ffbc, 
//                                              width = 2, 
//                                              xloc  = xloc.bar_time, 
//                                              style = line.style_dotted
//                                              ))

//             if high >= point + atr 

//                 if showLine

//                     zzLine.last().set_style(line.style_dotted)
//                     zzLine.last().set_color(color.new(lineCol, 50))

//                     zzLine.push(line.new(timeP, point, time, high, 
//                                      xloc  = xloc.bar_time, 
//                                      color = lineCol, 
//                                      width = 2
//                                      ))

//                 y1Price.set(0, point)
//                 y2Price.set(0, high)
//                 y1x    .set(0, timeP)
//                 y2x    .set(0, time)

//                 dir   := 1 
//                 point := high 
//                 timeP := time

//                 if showLine

//                     zzLineChange.first().delete()
//                     zzLineChange.last() .delete()

//                     Range = math.abs(point - (point - atr)) * .1

//                     if showProj

//                         zzLineChange.set(0, line.new(time("", -1), point - atr , time("", -3), point - atr + Range, 
//                                                      color = color.rgb(255, 116, 116), 
//                                                      width = 2, 
//                                                      xloc  = xloc.bar_time, 
//                                                      style = line.style_dotted
//                                                      ))

//                         zzLineChange.set(1, line.new(zzLine.last().get_x2(), zzLine.last().get_y2(), time("", -1), point - atr , 
//                                                      color = color.rgb(255, 116, 116), 
//                                                      width = 2, 
//                                                      xloc  = xloc.bar_time, 
//                                                      style = line.style_dotted
//                                                      ))

//         if dir == 0 

//             if high >= point + atr 

//                 if showLine
//                     zzLine.push(line.new(timeP, point, time, high,
//                                          xloc  = xloc.bar_time, 
//                                          color = lineCol, 
//                                          width = 2
//                                          ))

//                 y1Price.set(0, point)
//                 y2Price.set(0, high)
//                 y1x    .set(0, timeP)
//                 y2x    .set(0, time)
//                 dir   := 1
//                 point := high
//                 timeP := time 


//             else if low <= point + atr
                
//                 if showLine
//                     zzLine.push(line.new(timeP, point, time, low, 
//                                      xloc  = xloc.bar_time, 
//                                      color = lineCol, 
//                                      width = 2
//                                      ))

//                 y1Price.set(0, point)
//                 y2Price.set(0, low)
//                 y1x    .set(0, timeP)
//                 y2x    .set(0, time)
//                 dir   := -1 
//                 point := low
//                 timeP := time

//         var getBreakoutPointUp = 0.

//         if y1Price.first() > y2Price.first()
//             getBreakoutPointUp := y1Price.first()
 
//         0



// export zzS(float atr, array<int> y1x, array<int> y2x, array<float> y1Price, array<float> y2Price,  float zz1, int zz2, int zz3,
//      array<int> y1xF, array<float> y1pF, bool trainingComplete) => 

//     if trainingComplete

//         var point = zz1//zzltfpL
//         var timeP = zz2//zzltftL
//         var dir   = zz3//zzltfdL

//         if na(y1Price.first())

//             y1Price.set(0, y1pF.first())
//             y1x    .set(0, y1xF.first())


//         if dir == 1

//             point := math.max(point, high)
//             y2Price.set(0, point)

//             timeP := switch high == point 
//                 true => time 
//                 =>      timeP

//             y2x.set(0, timeP)

//             if low <= point - atr 

//                 y1Price.set(0, point)
//                 y2Price.set(0, low)
//                 y1x    .set(0, timeP)
//                 y2x    .set(0, time)
//                 dir   := -1 
//                 point := low
//                 timeP := time
     
//         else if dir == -1

//             point := math.min(low, point)
//             y2Price.set(0, point)

//             timeP := switch low == point 
//                 true => time 
//                 =>      timeP

//             y2x.set(0, timeP)

//             if high >= point + atr 
//                 y1Price.set(0, point)
//                 y2Price.set(0, high)
//                 y1x    .set(0, timeP)
//                 y2x    .set(0, time)
//                 dir   := 1 
//                 point := high 
//                 timeP := time
 
//         if dir == 0 

//             if high >= point + atr 

//                 y1Price.set(0, point)
//                 y2Price.set(0, high)
//                 y1x    .set(0, timeP)
//                 y2x    .set(0, time)
//                 dir   := 1
//                 point := high
//                 timeP := time 


//             else if low <= point + atr
                
//                 y1Price.set(0, point)
//                 y2Price.set(0, low)
//                 y1x    .set(0, timeP)
//                 y2x    .set(0, time)
//                 dir   := -1 
//                 point := low
//                 timeP := time

//         var getBreakoutPointDn = 20e20

//         if y1Price.first() < y2Price.first()
//             getBreakoutPointDn := y1Price.first()


//         0



// export lastBarZZ(array<float> y1PriceHTFL, array<float> y2PriceHTFL, array<float> y1PriceLTFL, array<float> y2PriceLTFL, int endLoop, array<int> y1xHTFL, string tfhtf,
// 	 string tfltf, array<int> y1xLTFL, 
// 	 	 bool showHTFzz, bool fibometer, float ATR, int inTradeLong, int inTradeShort) =>

// 	if barstate.islast

// 		count = 0 

// 	    var meterBoxes    = array.new_box()
// 	    var meterLine     = array.new_box()
// 	    var meterLabel    = array.new_label()
// 	    var zzChangeBoxes = array.new_box()
// 	    var ZZLines       = array.new_line()

// 	    varip flashCols = matrix.new<color>(16, 2)

// 	    if meterBoxes.size() > 0 
//             size = meterBoxes.size() - 1
// 	        for i = 0 to size
// 	            meterBoxes.shift().delete()

// 	    if meterLine.size() > 0 
//             size = meterLine.size() - 1
// 	        for i = 0 to size
// 	            meterLine.shift().delete()

// 	    if meterLabel.size() > 0 
//             size = meterLabel.size() - 1
// 	        for i = 0 to size
// 	            meterLabel.shift().delete()

// 	    if zzChangeBoxes.size() > 0
//             size = zzChangeBoxes.size() - 1
// 	        for i = 0 to size
// 	            zzChangeBoxes.shift().delete()

// 	    if ZZLines.size() > 0
//             size = ZZLines.size() - 1
// 	        for i = 0 to size
// 	            ZZLines.shift().delete()

// 	    endTop  = 0., endBtm = 20e20
// 	    endTopL = 0., endBtmL = 20e20

//         var stickyRange = 0.

//         getY1PRICEHTFL = y1PriceHTFL.first()
//         getY2PRICEHTFL = y2PriceHTFL.first()

//         getY2PRICELTFL = y2PriceLTFL.first()
//         getY1PRICELTFL = y1PriceLTFL.first()

// 	    if getY1PRICEHTFL != 0 and showHTFzz

// 	        isHigh    = getY2PRICEHTFL > getY1PRICEHTFL
// 	        isHighLTF = getY2PRICELTFL > getY1PRICELTFL

// 	        Range = math.abs(getY2PRICEHTFL - getY1PRICEHTFL) / (endLoop + 1)

//             closeIndex = 0, nearest = 20e20, closeBtm = 0., closeTop = 0.

//             newRange     = math.abs(getY2PRICEHTFL - getY1PRICEHTFL)
//             stickyRange := math.abs(getY2PRICEHTFL - getY1PRICEHTFL)

//             getHigh = math.max(getY2PRICEHTFL, getY1PRICEHTFL)
//             getLow  = math.min(getY2PRICEHTFL, getY1PRICEHTFL)

//             [fib618, fib382, fib236, fib786] = switch isHigh 

//                 true => [getY2PRICEHTFL - newRange * .618, getY2PRICEHTFL - newRange * .382, getY2PRICEHTFL - newRange * .236, getY2PRICEHTFL - newRange * .786] 
//                 =>      [getY2PRICEHTFL + newRange * .618, getY2PRICEHTFL + newRange * .382, getY2PRICEHTFL + newRange * .236, getY2PRICEHTFL + newRange * .786] 
                                
//             bordCol = switch 

//                 isHigh  and isHighLTF or inTradeLong > 0  =>                    #74ffbc

//                 not isHigh and not isHighLTF or inTradeShort < 0 => color.rgb(255, 116, 116)

//                 => color.rgb(128, 116, 255)                

//             meterLine.push(box.new(bar_index + 32, fib382 + newRange * .025, bar_index + 34, fib382 - newRange * .025, 
//                                              text         = ".382", 
//                                              border_color = bordCol, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

//             meterLine.push(box.new(bar_index + 32, fib618 + newRange * .025, bar_index + 34, fib618 - newRange * .025, 
//                                              text         = ".618", 
//                                              border_color = bordCol, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

//             if fibometer
                     
                    
//                 meterLine.push(box.new(bar_index + 32, fib236 + newRange * .025, bar_index + 34, fib236 - newRange * .025, 
//                                              text         = ".236", 
//                                              border_color = bordCol, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

//                 meterLine.push(box.new(bar_index + 32, fib786 + newRange * .025, bar_index + 34, fib786 - newRange * .025, 
//                                              text         = ".786", 
//                                              border_color = bordCol, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

//                 meterLine.push(box.new(bar_index + 32, math.avg(getY2PRICEHTFL, getY1PRICEHTFL) + newRange * .025, bar_index + 34, 
//                      math.avg(getY2PRICEHTFL, getY1PRICEHTFL) - newRange * .025, 
//                                              text         = ".5", 
//                                              border_color = bordCol, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))


//             [fibP, format] = switch fibometer

//                 false => [close, format.mintick]
//                 =>       [(close - getLow) / newRange, "###,###.###"]

//             if fibometer and isHigh
//                 fibP := 1 - fibP

            
//             meterLine.push(box.new(bar_index + 29, fib382 + newRange * .05, bar_index + 32, fib382 - newRange * .05, 
//                                              text         = str.tostring(fib382, format.mintick), 
//                                              border_color = #363843, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

//             meterLine.push(box.new(bar_index + 29, fib618 + newRange * .05, bar_index + 32, fib618 - newRange * .05, 
//                                              text         = str.tostring(fib618, format.mintick), 
//                                              border_color = #363843, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

//             if fibometer

//                 meterLine.push(box.new(bar_index + 29, fib236 + newRange * .05, bar_index + 32, fib236 - newRange * .05, 
//                                              text         = str.tostring(fib236, format.mintick), 
//                                              border_color = #363843, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

//                 meterLine.push(box.new(bar_index + 29, fib786 + newRange * .05, bar_index + 32, fib786 - newRange * .05, 
//                                              text         = str.tostring(fib786, format.mintick), 
//                                              border_color = #363843, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

//                 meterLine.push(box.new(bar_index + 29, math.avg(getY2PRICEHTFL, getY1PRICEHTFL) + newRange * .05, 
//                      bar_index + 32,  math.avg(getY2PRICEHTFL, getY1PRICEHTFL) - newRange * .05, 
//                                              text         = str.tostring(math.avg(getY2PRICEHTFL, getY1PRICEHTFL), format.mintick), 
//                                              border_color = #363843, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto))


//                 for i = meterLine.size() - 1 to meterLine.size() - 5

//                     getBox = meterLine.get(i)

//                     if close >= math.min(getBox.get_bottom(), getBox.get_top()) and close <= math.max(getBox.get_bottom(), getBox.get_top())

//                         getBox.set_border_color(bordCol)

//                     else 

//                         getBox.set_border_color(#363843)


//             meterLine.push(box.new(bar_index + 22, close + newRange * .05, bar_index + 25, close - newRange * .05, 
//                                      text         = str.tostring(fibP, format) + "🌡", 
//                                      border_color = #363843, 
//                                      bgcolor      = #20222C, 
//                                      text_color   = color.white, 
//                                      text_size    = size.auto
//                                      ))

// 	        for i = 0 to endLoop

// 	            [startCol, endCol, btm, top] = switch isHigh 

// 	                true => [color.green, color.red   , getY1PRICEHTFL + i * Range, getY1PRICEHTFL + ((i + 1) * Range)]
// 	                =>      [color.green, color.red   , getY1PRICEHTFL - i * Range, getY1PRICEHTFL - ((i + 1) * Range)]

// 	            endTop := math.max(top, btm, endTop)
// 	            endBtm := math.min(top, btm, endBtm)

//                 middle = math.round(endLoop / 2)


//                 [scol, ecol] = switch 
                    
//                     isHigh and isHighLTF or inTradeLong > 0 =>          [#74ffbc40, #74ffbc]
//                     not isHigh and not isHighLTF or inTradeShort < 0 => [#ff74744d, color.rgb(255, 116, 116)]
//                     =>                                                  [color.rgb(255, 116, 116), #74ffbc]

// 	            col  = kai.hsv_gradient(i, 10, middle, scol, ecol)
// 	            col2 = kai.hsv_gradient(i, middle, 90, ecol, scol)

//                 if inTradeLong == 0 and inTradeShort == 0

//                     if isHigh and not isHighLTF or not isHigh and isHighLTF 

//                         col  := kai.hsv_gradient(i, 10, middle, scol, color.rgb(128, 116, 255))
// 	                    col2 := kai.hsv_gradient(i, middle, 90, color.rgb(128, 116, 255), ecol)

//                 finCol = switch 

//                     i <= middle => col 
//                     =>             col2

// 	            meterBoxes.push(box.new(bar_index + 25, btm, bar_index + 29, top, 
// 	                                 bgcolor      = finCol, 
//                                      border_color = #00000000
//                                      ))

//             if meterLine.last().get_top() > endTop 
//                 meterLine.last().set_top(endTop)

//             if meterLine.last().get_bottom() < endBtm
//                 meterLine.last().set_bottom(endBtm)


// 	        [outCol, outCol2, colClose] = switch

// 	            isHigh and isHighLTF or inTradeLong > 0          => [#14D990, #74ffbc, #000000]
// 	            not isHigh and not isHighLTF or inTradeShort < 0 => [#F24968, color.rgb(255, 116, 116), #000000]
//                 =>                                                  [color.rgb(128, 116, 255),color.rgb(128, 116, 255), #000000]
 
//             meterLine .push(box.new(bar_index + 25, endBtm, bar_index + 29, endTop, bgcolor = #00000000, border_color = outCol2))


//             txt = pct.formattedNoOfPeriods(timeframe.in_seconds(tfhtf) * 1000)
            
//             if str.contains(txt, "minutes") 
//                 txt := str.substring(txt, 0, end_pos = str.pos(txt, "m"))

//             meterLine.push(box.new(bar_index + 25, endTop, bar_index + 29, endTop + newRange * .1, 
//                                          bgcolor      = #00000000, 
//                                          border_color = #00000000, 
//                                          text_color   = chart.fg_color, 
//                                          text_size    = size.auto, 
//                                          text         = txt
//                                          ))

//             meterLabel.push(label.new(bar_index + 27, fib618, text = "⋯", 
//                                          size      = size.normal, 
//                                          style     = label.style_label_center, 
//                                          color     = #00000000, 
//                                          textcolor = #363843
//                                          ))

//             meterLabel.push(label.new(bar_index + 27, fib382, text = "⋯", 
//                                          size      = size.normal, 
//                                          style     = label.style_label_center, 
//                                          color     = #00000000, 
//                                          textcolor = #363843
//                                          ))

// 	        ZZLines.push(line.new(y1xHTFL.first(), getY1PRICEHTFL, time, getY1PRICEHTFL, color = #F24968, xloc = xloc.bar_time))

//             RANGEHIGH = math.abs(getY1PRICEHTFL - (getY1PRICEHTFL - ATR)) / 7
//             RANGELOW  = math.abs(getY1PRICEHTFL - (getY1PRICEHTFL + ATR)) / 7

// 	    if getY1PRICELTFL != 0

// 	        isHigh    = getY2PRICELTFL > getY1PRICELTFL
// 	        isHighHTF = getY2PRICEHTFL > getY1PRICEHTFL

// 	        Range    = math.abs(getY2PRICELTFL - getY1PRICELTFL) / (endLoop + 1) // switch 
//             newRange = math.abs(getY2PRICELTFL - getY1PRICELTFL)

// 	        isHighLTF = getY2PRICELTFL > getY1PRICELTFL

//             [fib618, fib382, fib236, fib786] = switch isHighLTF 

//                 true => [getY2PRICELTFL - newRange * .618, getY2PRICELTFL - newRange * .382,  getY2PRICELTFL - newRange * .236, getY2PRICELTFL - newRange * .786]
//                 =>      [getY2PRICELTFL + newRange * .618, getY2PRICELTFL + newRange * .382,  getY2PRICELTFL + newRange * .236, getY2PRICELTFL + newRange * .786]             

//             getHigh = math.max(getY2PRICELTFL, getY1PRICELTFL)
//             getLow  = math.min(getY2PRICELTFL, getY1PRICELTFL)

//             bordCol = switch 
                
//                 isHigh and isHighHTF or inTradeLong > 0          => #74ffbc
//                 not isHigh and not isHighHTF or inTradeShort < 0 => color.rgb(255, 116, 116)
//                 =>                                                  color.rgb(128, 116, 255)

//             meterLine.push(box.new(bar_index + 17, fib382 + newRange * .025, bar_index + 19, fib382 - newRange * .025, 
//                                                      text         = ".382", 
//                                                      border_color = bordCol, 
//                                                      bgcolor      = #20222C, 
//                                                      text_color   = color.white, 
//                                                      text_size    = size.auto
//                                                      ))

//             meterLine.push(box.new(bar_index + 17, fib618 + newRange * .025, bar_index + 19, fib618 - newRange * .025, 
//                                                      text         = ".618", 
//                                                      border_color = bordCol, 
//                                                      bgcolor      = #20222C, 
//                                                      text_color   = color.white, 
//                                                      text_size    = size.auto
//                                                      ))

//             if fibometer

//                 meterLine.push(box.new(bar_index + 17, fib236 + newRange * .025, bar_index + 19, fib236 - newRange * .025, 
//                                                      text         = ".236", 
//                                                      border_color = bordCol, 
//                                                      bgcolor      = #20222C, 
//                                                      text_color   = color.white, 
//                                                      text_size    = size.auto
//                                                      ))

//                 meterLine.push(box.new(bar_index + 17, fib786 + newRange * .025, bar_index + 19, fib786 - newRange * .025, 
//                                                      text         = ".786", 
//                                                      border_color = bordCol, 
//                                                      bgcolor      = #20222C, 
//                                                      text_color   = color.white, 
//                                                      text_size    = size.auto))

//                 meterLine.push(box.new(bar_index + 17, math.avg(getY2PRICELTFL, getY1PRICELTFL) + newRange * .025, bar_index + 19, 
//                      math.avg(getY2PRICELTFL, getY1PRICELTFL) - newRange * .025, 
//                                                      text         = ".5", 
//                                                      border_color = bordCol, 
//                                                      bgcolor      = #20222C, 
//                                                      text_color   = color.white, 
//                                                      text_size    = size.auto))

//             [fibP, format] = switch fibometer

//                 false => [close, format.mintick] 
//                 =>       [(close - getLow) / newRange, "###,###.###"]

//             if fibometer and isHigh
//                 fibP := 1 - fibP

//             meterLine.push(box.new(bar_index + 14, fib382 + newRange * .05, bar_index + 17, fib382 - newRange * .05, 
//                                          text         = str.tostring(fib382, format.mintick), 
//                                          border_color = #363843, 
//                                          bgcolor      = #20222C, 
//                                          text_color   = color.white, 
//                                          text_size    = size.auto
//                                          ))

//             meterLine.push(box.new(bar_index + 14, fib618 + newRange * .05, bar_index + 17, fib618 - newRange * .05, 
//                                          text         = str.tostring(fib618, format.mintick), 
//                                          border_color = #363843, 
//                                          bgcolor      = #20222C, 
//                                          text_color   = color.white, 
//                                          text_size    = size.auto
//                                          ))


//             if fibometer

//                 meterLine.push(box.new(bar_index + 14, fib236 + newRange * .05, bar_index + 17, fib236 - newRange * .05, 
//                                          text         = str.tostring(fib236, format.mintick), 
//                                          border_color = #363843, 
//                                          bgcolor      = #20222C, 
//                                          text_color   = color.white, 
//                                          text_size    = size.auto
//                                          ))

//                 meterLine.push(box.new(bar_index + 14, fib786 + newRange * .05, bar_index + 17, fib786 - newRange * .05, 
//                                          text         = str.tostring(fib786, format.mintick), 
//                                          border_color = #363843, 
//                                          bgcolor      = #20222C, 
//                                          text_color   = color.white, 
//                                          text_size    = size.auto
//                                          ))

//                 meterLine.push(box.new(bar_index + 14, math.avg(getY2PRICELTFL, getY1PRICELTFL) + newRange * .05, 
//                      bar_index + 17,  math.avg(getY2PRICELTFL, getY1PRICELTFL) - newRange * .05, 
//                                                  text         = str.tostring(math.avg(getY2PRICELTFL, getY1PRICELTFL), format.mintick), 
//                                                  border_color = #363843, 
//                                                  bgcolor      = #20222C, 
//                                                  text_color   = color.white, 
//                                                  text_size    = size.auto))


//                 for i = meterLine.size() - 1 to meterLine.size() - 5

//                     getBox = meterLine.get(i)

//                     if close >= math.min(getBox.get_bottom(), getBox.get_top()) and close <= math.max(getBox.get_bottom(), getBox.get_top())

//                         getBox.set_border_color(bordCol)
                    
//                     else 

//                         getBox.set_border_color(#363843)

//             meterLine.push(box.new(bar_index + 7, close + newRange * .05, bar_index + 10, close - newRange * .05, 
//                                              text         = str.tostring(fibP, format) + "🌡", 
//                                              border_color = #363843, 
//                                              bgcolor      = #20222C, 
//                                              text_color   = color.white, 
//                                              text_size    = size.auto
//                                              ))

// 	        for i = 0 to endLoop

// 	            [startCol, endCol, btm, top] = switch 

// 	                isHigh => [color.red, color.green, getY2PRICELTFL - i * Range, getY2PRICELTFL - ((i + 1) * Range)]
// 	                =>        [color.red, color.green, getY2PRICELTFL + i * Range, getY2PRICELTFL + ((i + 1) * Range)]


// 	            endTopL := math.max(btm, top, endTopL)
// 	            endBtmL := math.min(btm, top, endBtmL)

//                 middle = math.round(endLoop / 2)

//                 [scol, ecol] = switch 
                    

//                     isHigh and isHighHTF or inTradeLong > 0 =>          [#74ffbc40, #74ffbc]
//                     not isHigh and not isHighHTF or inTradeShort < 0 => [#ff74744d, color.rgb(255, 116, 116)]
//                     =>                                                [color.rgb(255, 116, 116), #74ffbc]

// 	            col  = kai.hsv_gradient(i, 10, middle , scol, ecol)
// 	            col2 = kai.hsv_gradient(i, middle, 90, ecol, scol)

//                 if inTradeLong == 0 and inTradeShort == 0
//                     if isHigh and not isHighHTF or not isHigh and isHighHTF

//                         col  := kai.hsv_gradient(i, 10, middle, scol, color.rgb(128, 116, 255))
// 	                    col2 := kai.hsv_gradient(i, middle, 90, color.rgb(128, 116, 255), ecol)


//                 finCol = switch 

//                     i <= middle => col 
//                     =>             col2

// 	            meterBoxes.push(box.new(bar_index + 10, btm, bar_index + 14, top, 
// 	                                     bgcolor      = finCol, 
//                                          border_color = #00000000
//                                          ))


//             if meterLine.last().get_top() > endTopL 
//                 meterLine.last().set_top(endTopL)

//             if meterLine.last().get_bottom() < endBtmL
//                 meterLine.last().set_bottom(endBtmL)


// 	        [outCol, outCol2, colClose] = switch 

// 	            isHighHTF and isHigh or inTradeLong > 0 =>           [#14D990, #74ffbc, #000000]
// 	            not isHighHTF and not isHigh or inTradeShort < 0 =>  [#F24968, color.rgb(255, 116, 116),#000000]
//                 =>                                                   [color.rgb(128, 116, 255),color.rgb(128, 116, 255), #000000]

//             txt = pct.formattedNoOfPeriods(timeframe.in_seconds(tfltf) * 1000)
            
//             if str.contains(txt, "minutes") 
//                 txt := str.substring(txt, 0, end_pos = str.pos(txt, "m"))

//             meterLine.push(box.new(bar_index + 10, endTopL, bar_index + 14, endTopL + stickyRange * .1, 
//                                          bgcolor      = #00000000, 
//                                          border_color = #00000000, 
//                                          text_color   = chart.fg_color, 
//                                          text_size    = size.auto, 
//                                          text = txt
//                                          ))

//             meterLabel.push(label.new(bar_index + 12, fib618, text = "⋯", 
//                                      size      = size.normal, 
//                                      style     = label.style_label_center, 
//                                      color     = #00000000, 
//                                      textcolor = #363843
//                                      ))
//             meterLabel.push(label.new(bar_index + 12, fib382, text = "⋯", 
//                                      size      = size.normal, 
//                                      style     = label.style_label_center, 
//                                      color     = #00000000, 
//                                      textcolor = #363843
//                                      ))

//             meterLine.push(box.new(bar_index + 10, endBtmL, bar_index + 14, endTopL, 
//                                      bgcolor      = #00000000, 
//                                      border_color = outCol2
//                                      ))

// 	        ZZLines.push(line.new(y1xLTFL.first(), getY1PRICELTFL, time, getY1PRICELTFL, color = #F24968, xloc = xloc.bar_time))

//             RANGEHIGH = math.abs(getY1PRICELTFL - (getY1PRICELTFL - ATR)) / 7
//             RANGELOW  = math.abs(getY1PRICELTFL - (getY1PRICELTFL + ATR)) / 7


// export lastBarOpti(bool isLastBar, 
// 				 	    array<float> PFlossArr,  array<float> PFprofitArr,
// 					 	   array<float> PFlossArrS, array<float> PFprofitArrS,
// 						 	  string sort, array<string> stringArr, bool tradeLong, color line_color1, color line_color2,
//                              array<float> closeArrEnd, array<float> highArrEnd, array<float> lowArrEnd, array<int> timeArrEnd, array<float> atrArrLTF, array<float> atrArrHTF,
//                                  array<float> y2PriceFinalHTF, array<float> y2xFinalHTF, array<float> y1PriceFinalHTF, float buffer, 
// 	 	                          array<float> y1xFinalHTF,
// 		                          	 array<bool> isHighFirstLongArrHTF, 
// 		                          	 	 array<float> getBreakoutPointUpArrLongHTF, array<float> y2PriceHistoryHTF, array<float> y1PriceHistoryHTF, array<float> getBreakoutPointDnArrShortLTF, float buySellRange, bool tradeShort, array<float> ltfCloArr, array<float> ltfCloArr1,  array<float> ohlc4ArrEnd, array<float> y2PriceFinalHTFS, array<float> y2xFinalHTFS, array<float> y1PriceFinalHTFS, array<float> y1xFinalHTFS,
// 		 	                              array<float> y2PriceHistoryHTFS, array<float>y1PriceHistoryHTFS, array<bool> isHighFirstShortArrHTF, 
// 			 	                             array<float> getBreakoutPointDnArrShortHTF, array<float> y2PriceFinalLTFS, array<float> y2xFinalLTFS, array<float> y1PriceFinalLTFS,  
// 	 	                         array<float> y1xFinalLTFS,
// 		 	 array<float> y2PriceHistoryLTFS, array<float>y1PriceHistoryLTFS, array<bool> isHighFirstShortArrLTF, array<float> y2PriceFinalLTF, array<float> y2xFinalLTF, array<float> y1PriceFinalLTF, 
// 	 	  array<float> y1xFinalLTF,
// 		 	 array<float> y2PriceHistoryLTF, array<float>y1PriceHistoryLTF, array<bool> isHighFirstLongArrLTF, 
// 			 	 array<float> getBreakoutPointUpArrLongLTF, array<float> closeArrEnd1, array<float> openArrEnd, bool showLab,
// 			 	  array<float>historicalLongsPFPFORIT, array<float>historicalLongsPFLOSS,  
//                   array<float>historicalShortPFPFORIT, array<float>historicalShortPFLOSS,  string tradeTypeMaster, bool showOPTI,
//                       array<bool> closer2lowArr, bool useRR, float RRmult, array<bool> closer2LowArr, float stopLossAmount, string labelSize, bool closeEOD, array<bool> lastBarArray
//                      )=>

//     var bestATRlongltf  = 0., var bestATRlonghtf  = 0.
//     var bestTarget      = 0., var bestTrailing    = 0.
//     var bestTargetS     = 0., var bestTrailingS   = 0.
//     var bestATRshortltf = 0., var bestATRshorthtf = 0.
//     atrPTarrNS = array.new_float(), atrTarrNS = array.new_float(),

//     var timeArr = array.new<int>()
//     timeArr.push(time)
//     var zzltfpL = 0., var zzhtfpL = 0.
//     var zzltftL = 0 , var zzhtftL = 0
//     var zzltfdL = 0 , var zzhtfdL = 0  
    
//     var float bestLongsIndex = na 
//     var float bestShortsIndex = na

//     var zzltfpS = 0., var zzhtfpS = 0.
//     var zzltftS = 0 , var zzhtftS = 0
//     var zzltfdS = 0 , var zzhtfdS = 0  

//     bestATRshortltf := 0 
//     bestATRlongltf := 0 

//     bestTrailingS := 0

//     if showOPTI
//         if barstate.islastconfirmedhistory or barstate.islast and barstate.isconfirmed and isLastBar()

//             var masterLabels = array.new<label>(), var masterLabelsTimes = array.new<int>()

//             finalPF = array.new_float(PFlossArr.size(), 0), finalWR = array.new_float(PFlossArr.size(), 0)

//             for i = 0 to finalPF.size() - 1

//                 getFinPFLOSS = switch PFlossArr.get(i) == 0 

//                     true => 1
//                     =>     PFlossArr.get(i)

//                 finalPF.set(i, nz(PFprofitArr.get(i), 1) / nz(getFinPFLOSS, 1))

//             tablePF = array.new_float(), tableString = matrix.new<string>(0, 4)

//             [copyMetric, stagMetric] = switch sort 

//                 "PF"       => [finalPF.copy(), finalPF.copy()]

            

//             copyMetric.sort(order.descending)

//             includesArr = array.new_string()

//             for i = 0 to math.min(2, copyMetric.size() - 1)
//                 getCopyMetric = copyMetric.get(i)

//                 for x = 0 to copyMetric.size() - 1

//                     if getCopyMetric == stagMetric.get(x) //and not includesArr.includes(stringArr.get(x))

//                         includesArr.push(stringArr.get(x))

//                         tablePF.push(finalPF.get(x))

//                         getString  = stringArr.get(x)

//                         splitString = str.split(getString, "_")

//                         newString  = splitString.first()
//                         newString2 = splitString.get(1)
//                         newString3 = splitString.get(2)
//                         newString4 = splitString.last()

//                         if bestATRlongltf == 0 

//                             bestATRlongltf := str.tonumber(newString)
//                             bestATRlonghtf := str.tonumber(newString2)
//                             bestTarget     := str.tonumber(newString3)
//                             bestTrailing   := str.tonumber(newString4)

//                         tableString.add_row(tableString.rows(), array.from(newString, newString2, newString3, newString4))

//                         break 

//             finalPFS = array.new_float(PFlossArrS.size(), 0)

//             for i = 0 to finalPFS.size() - 1

//                 getFinPFLOSS = switch PFlossArrS.get(i) == 0 
//                     true => 1
//                     =>     PFlossArrS.get(i)

//                 finalPFS.set(i, nz(PFprofitArrS.get(i), 1) / nz(getFinPFLOSS, 1))

            

//             tablePFS = array.new_float(), tableStringS = matrix.new<string>(0, 4),copyMetricS = array.new<float>(), stagMetricS = array.new<float>()

//             for i = 0 to finalPFS.size() - 1

//                 if sort == "PF"
//                     copyMetricS.push(finalPFS.get(i))
//                     stagMetricS.push(finalPFS.get(i))

//             bestLongsIndex := finalPF.indexof(finalPF.max())
//             bestShortsIndex := finalPFS.indexof(finalPFS.max())

//             copyMetricS.sort(order.descending)

//             includesArrS = array.new_string()

//             for i = 0 to math.min(2, copyMetricS.size() - 1)
//                 getCopyMetricS = copyMetricS.get(i)
//                 for x = 0 to copyMetricS.size() - 1

//                     if getCopyMetricS == stagMetricS.get(x) //and not includesArr.includes(stringArr.get(x))

//                         tablePFS  .push(finalPFS.get(x))
    
//                         getString  = stringArr.get(x)

//                         splitString = str.split(getString, "_")

//                         newString  = splitString.first()
//                         newString2 = splitString.get(1)
//                         newString3 = splitString.get(2)
//                         newString4 = splitString.last()

//                         if bestATRshortltf == 0 
//                             bestATRshortltf := str.tonumber(newString)
//                             bestATRshorthtf := str.tonumber(newString2)
//                             bestTargetS     := str.tonumber(newString3)
//                             bestTrailingS   := str.tonumber(newString4)

//                         tableStringS.add_row(tableStringS.rows(), array.from(newString, newString2, newString3, newString4))

//                         break 


//             [z1, z2, z3, CPLTF] = lastBarZZlongLTF(closeArrEnd,  highArrEnd, lowArrEnd, timeArrEnd,  atrArrLTF,  bestATRlongltf, y2PriceFinalLTF,  y2xFinalLTF,  y1PriceFinalLTF,  buffer, y1xFinalLTF,y2PriceHistoryLTF, y1PriceHistoryLTF,  isHighFirstLongArrLTF, getBreakoutPointUpArrLongLTF, showLab, closeArrEnd1, labelSize) 

//             zzltfpL := z1.first()
//             zzltftL := z2.first()
//             zzltfdL := z3.first()

//             [z4, z5, z6, CPHTF] = lastBarZZlongHTF ( closeArrEnd,  highArrEnd, lowArrEnd, timeArrEnd,  atrArrHTF,  bestATRlonghtf,y2PriceFinalHTF,  y2xFinalHTF,  y1PriceFinalHTF,  buffer, y1xFinalHTF,y2PriceHistoryHTF, y1PriceHistoryHTF,  isHighFirstLongArrHTF, getBreakoutPointUpArrLongHTF) 

//             zzhtfpL := z4.first()
//             zzhtftL := z5.first()
//             zzhtfdL := z6.first()


// 	    	var entryLong = 0., var exitLong = 0., var inTradeLong = 0, var limitLong = 0., var triggerLong = 0., var divLong = 0, var RRtpLong = 0.

// 	    	if barstate.islastconfirmedhistory

//             	[entryLongGet, exitLongGet, inTradeLongGet, limitLongGet, triggerLongGet, divLongGet, RRtpLongGet] = historicalLongTrades(bestTrailing, bestTarget,  lowArrEnd,  closeArrEnd, openArrEnd,  atrArrLTF, 
//             	 timeArrEnd,   ohlc4ArrEnd,  isLastBar,  tradeTypeMaster,  isHighFirstLongArrHTF,
//             	  tradeLong,  y1PriceHistoryHTF,  y2PriceHistoryHTF,  getBreakoutPointUpArrLongLTF,  ltfCloArr,  ltfCloArr1,
//             	     buySellRange,  historicalLongsPFPFORIT, historicalLongsPFLOSS, masterLabels, masterLabelsTimes, useRR, highArrEnd, closer2lowArr, RRmult, stopLossAmount, labelSize, closeEOD, lastBarArray)

// 	    		entryLong := entryLongGet 
// 	    		exitLong  := exitLongGet 
// 	    		inTradeLong := inTradeLongGet 
// 	    		limitLong := limitLongGet 
// 	    		triggerLong := triggerLongGet 
//                 divLong    := divLongGet
//                 RRtpLong  := RRtpLongGet


//         // 
//         // -----------------------------------------------------
//         // -----------------------------------------------------
//         // -----------------------------------------------------
//         // -----------------------------------------------------


//             [z7, z8, z9] = lastBarZZshortLTF ( closeArrEnd,  highArrEnd, lowArrEnd, timeArrEnd,  atrArrLTF,  bestATRshortltf,
//         	  y2PriceFinalLTFS,  y2xFinalLTFS,  y1PriceFinalLTFS,  buffer, 
//         	 	    y1xFinalLTFS,
//         		 	  y2PriceHistoryLTFS, y1PriceHistoryLTFS,  isHighFirstShortArrLTF, 
//         			 	  getBreakoutPointDnArrShortLTF, showLab, closeArrEnd1, tradeShort,  labelSize) 

//             zzltfpS := z7.first()
//             zzltftS := z8.first()
//             zzltfdS := z9.first()


//             [z10, z11, z12] = lastBarZZshortHTF ( closeArrEnd,  highArrEnd, lowArrEnd, timeArrEnd,  atrArrHTF,  bestATRshorthtf,
//         	  y2PriceFinalHTFS,  y2xFinalHTFS,  y1PriceFinalHTFS,  buffer, 
//         	 	    y1xFinalHTFS,
//         		 	  y2PriceHistoryHTFS, y1PriceHistoryHTFS,  isHighFirstShortArrHTF, 
//         			 	  getBreakoutPointDnArrShortHTF) 

//             zzhtfpS := z10.first()
//             zzhtftS := z11.first()
//             zzhtfdS := z12.first()

// 	    	var entryShort = 0., var exitShort = 0., var inTradeShort = 0, var limitShort = 0., var triggerShort = 0., var divShort = 0, var RRtpShort = 0.

// 	    	if barstate.islastconfirmedhistory

//             	[entryShortGet, exitShortGet, inTradeShortGet, limitShortGet, triggerShortGet, divShortGet, RRtpShortGet] = historicalShorTrades( bestTrailingS,  bestTargetS,  highArrEnd,  lowArrEnd,  closeArrEnd, openArrEnd,  atrArrLTF, 
//             	 timeArrEnd,   ohlc4ArrEnd,  isLastBar,  tradeTypeMaster,  isHighFirstShortArrHTF,
//             	  tradeShort,  y1PriceHistoryHTFS,  y2PriceHistoryHTFS,  getBreakoutPointDnArrShortLTF,  ltfCloArr,  ltfCloArr1,
//             	      buySellRange, historicalShortPFPFORIT, historicalShortPFLOSS, masterLabels, masterLabelsTimes, useRR, RRmult, closer2LowArr, stopLossAmount, labelSize, closeEOD, lastBarArray)


// 	    		entryShort := entryShortGet 
// 	    		exitShort  := exitShortGet 
// 	    		inTradeShort := inTradeShortGet 
// 	    		limitShort := limitShortGet 
// 	    		triggerShort := triggerShortGet 
//                 divShort    := divShortGet
//                 RRtpShort  := RRtpShortGet

//             var polyline ltfPoly = na 
//             var polyline htfPoly = na 

//             if not na(ltfPoly) 
//                 ltfPoly.delete()
//             if not na(htfPoly)
//                 htfPoly.delete()

//             if CPLTF.size() < 10000

//                 ltfPoly := polyline.new(CPLTF, xloc = xloc.bar_time, line_color = color.new(line_color1, 50), 
//                                              line_style = line.style_dotted, 
//                                              line_width = 2
//                                              )
//             else 

//                 newPoints = CPLTF.slice(CPLTF.size() - 9000, CPLTF.size())

//                 ltfPoly := polyline.new(newPoints, xloc = xloc.bar_time, line_color = color.new(line_color1, 50), 
//                                              line_style = line.style_dotted, 
//                                              line_width = 2
//                                              )


//             if CPHTF.size() < 10000

//                 htfPoly := polyline.new(CPHTF, xloc = xloc.bar_time, line_color = color.new(line_color2, 50), 
//                                              line_style = line.style_dotted, 
//                                              line_width = 2
//                                              )

//             else 

//                 newPoints = CPHTF.slice(CPHTF.size() - 9000, CPHTF.size())

//                 htfPoly := polyline.new(newPoints, xloc = xloc.bar_time, line_color = color.new(line_color2, 50), 
//                                              line_style = line.style_dotted, 
//                                              line_width = 2
//                                              )


//             [bestATRlongltf ,  bestATRlonghtf ,bestTarget ,  bestTrailing ,bestTargetS ,  bestTrailingS ,bestATRshortltf ,  bestATRshorthtf, zzltfpL ,zzltftL ,zzltfdL    ,zzhtfpL ,zzhtftL ,zzhtfdL    ,zzltfpS ,zzltftS ,zzltfdS    ,zzhtfpS ,zzhtftS ,zzhtfdS, tableString,  tableStringS, tablePF,  tablePFS, 
//                  entryLong, exitLong, inTradeLong, limitLong, triggerLong, entryShort, exitShort, inTradeShort, limitShort, triggerShort, divLong, divShort, RRtpLong, RRtpShort, ltfPoly, htfPoly, bestLongsIndex, bestShortsIndex]
 



// export liveTrades(array<float> y1PriceLTFL, array<float> y2PriceLTFL, array<float> y1PriceLTFS, array<float> y2PriceLTFS, array<int> y1xLTFL, array<int> y1xLTFS,
//      bool isLastBar, bool isHighFirst2Long, bool tradeLong, array<float> y2PriceHTFL, array<float> y1PriceHTFL, float buySellRange, float getLTFclo,
//          float getLTFclo1, float bestTrailing, float bestTarget, float atrLTF, bool isHighFirst2Short, bool tradeShort, array<float> y2PriceHTFS, array<float> y1PriceHTFS,
//              float bestTrailingS, float bestTargetS, bool showLab, 
//                   array<float> finalLONGPFPROFIT, array<float> finalLONGPFLOSS,
//                       array<float> finalSHORTPFPROFIT, array<float> finalSHORTPFLOSS, array<float> breakUpGet, array<float> breakDnGet, 
//                          float getLongStop, float getShortStop, float getLongLimit, float getShortLimit, float getInTradeLong, float getEntryLong, float getEntryShort, float getInTradeShort, string tradeTypeMaster, float getRRtpLastTradeLong, int getDivLastTradeLong, float getRRtpLastTradeShort, int getDivLastTradeShort, float RRmult, bool useRR, bool closer2low, float stopLossAmount, string labelSize, bool closeEOD) =>
    

//     var longStop           = 0.
//     var shortStop          = 0.
//     var longTP             = 0. 
//     var shortTP            = 0.
//     var getBreakoutPointUp = 0.
//     var getBreakoutPointDn = 0.
//     var inTradeLongVAR     = 0
//     var inTradeShortVAR    = 0
//     var tradeEntryLong     = 0.
//     var tradeEntryShort    = 0.

//     var getRRtpLong = 0. 
//     var getDivLong = 0


//     var getRRtpShort = 0. 
//     var getDivShort  = 0


//     var trainingComplete = false 

//     if barstate.islastconfirmedhistory

//         getBreakoutPointUp := breakUpGet.last()
//         getBreakoutPointDn := breakDnGet.last()
//         longStop           := getLongStop
//         shortStop          := getShortStop
//         longTP             := getLongLimit
//         shortTP            := getShortLimit
//         inTradeLongVAR     := nz(int(getInTradeLong))
//         inTradeShortVAR    := nz(int(getInTradeShort))
//         tradeEntryLong     := getEntryLong
//         tradeEntryShort    := getEntryShort
//         getRRtpLong        := getRRtpLastTradeLong
//         getDivLong         := getDivLastTradeLong    
//         getRRtpShort       := getRRtpLastTradeShort
//         getDivShort        := getDivLastTradeShort    

//         trainingComplete := true 

//     lastBarOn = trainingComplete or barstate.islast

//     if barstate.islast and barstate.isconfirmed and isLastBar or trainingComplete and barstate.isconfirmed and isLastBar

//         getBreakoutPointUp := breakUpGet.last()
//         getBreakoutPointDn := breakDnGet.last()

//     if lastBarOn
            
//         if isLastBar and barstate.isconfirmed
//             var line longLineEntry = line(na)

//             if not na(longLineEntry)
//                 longLineEntry.set_x2(time("", -1))

//             if tradeTypeMaster == "Breakout"
//                 if isHighFirst2Long and tradeLong

//                     Range = math.abs(y2PriceHTFL.first() - y1PriceHTFL.first()) * (buySellRange / 100)
                    
//                 //     if getBreakoutPointUp != 0 and getBreakoutPointUp <= y1PriceHTFL.first() + Range 
//                 //    and inTradeLongVAR == 0 and inTradeShortVAR == 0

//                         // startBreakLine = 0
                        
//                         // if highArrEnd.size() > 0 
//                         //     for i = highArrEnd.size() - 1 to 0 
//                         //         if highArrEnd.get(i) >= getBreakoutPointUp 

//                         //             startBreakLine := timeArrEnd.get(i)
//                         //             break 

//                         // if startBreakLine != 0 
//                         //     longLineEntry := line.new(startBreakLine, getBreakoutPointUp, time("", -1), time, 
//                         //                          xloc  = xloc.bar_time, 
//                         //                          color = #74ffbc
//                         //                          )

//                     if getLTFclo > getBreakoutPointUp and getBreakoutPointUp != 0 and getLTFclo1 <= getBreakoutPointUp
//                    and getBreakoutPointUp <= y1PriceHTFL.first() + Range and inTradeLongVAR == 0 and inTradeShortVAR == 0

//                         exitPrice      = math.round_to_mintick(close - atrLTF * bestTrailing)

//                         longStop       := exitPrice
//                         longTP         := math.round_to_mintick(close + atrLTF * bestTarget  )
//                         tradeEntryLong := close
//                         inTradeLongVAR := 1

//                         getDivLong         := 1

//                         exitDistance = math.abs(close - exitPrice) * RRmult 

//                         getRRtpLong        := math.round_to_mintick(close + exitDistance)

//                         rrTPtext = switch useRR 

//                             true => "\nTP1: " + str.tostring(getRRtpLong, format.mintick)
//                             =>      ""

//                         risk = close - exitPrice

//                         contracts = switch syminfo.type == "futures" 
                            
//                             false => stopLossAmount / risk
//                             =>       math.floor(stopLossAmount / risk)


//                         label.new(time, low, text = "▲", style = label.style_text_outline, 
//                                              xloc      = xloc.bar_time, 
//                                              size      = labelSize, 
//                                              yloc      = yloc.belowbar,
//                                              textcolor = #74ffbc, 
//                                              color     = chart.fg_color, 
//                                              tooltip   = "Entry: " + str.tostring(close, format.mintick) 
//                                                      + "\nTrailing PT Trigger: " + str.tostring(longTP, format.mintick) + " (" + str.tostring((longTP / close - 1) * 100, format.percent) + ")"
//                                                      + "\nInitial SL: "          + str.tostring(longStop, format.mintick) + " (" + str.tostring((longStop / close - 1) * 100, format.percent)    + ")"
//                                                      + rrTPtext + "\nIdeal Amount: " + str.tostring(contracts, "###,###.##"))
                        
//                         longLineEntry.delete()
//                 else 

//                     longLineEntry.delete()

//             if tradeTypeMaster == "Breakout"  

//                 if not isHighFirst2Short and tradeShort 
                
//                     Range = math.abs(y2PriceHTFS.first() - y1PriceHTFS.first()) * (buySellRange / 100)

//                     if getLTFclo < getBreakoutPointDn and getBreakoutPointDn != 20e20 
//                    and getLTFclo1 >= getBreakoutPointDn  and getBreakoutPointDn >= y1PriceHTFS.first() - Range 
//                    and inTradeShortVAR == 0 and inTradeLongVAR == 0
                        
//                         exitPrice = math.round_to_mintick(close + atrLTF * bestTrailingS)

//                         shortStop       := exitPrice
//                         shortTP         := math.round_to_mintick(close - atrLTF * bestTargetS)
//                         tradeEntryShort := close
//                         inTradeShortVAR := -1 

//                         getDivShort := 1

//                         exitDistance = math.abs(exitPrice - close) * RRmult

//                         getRRtpShort := math.round_to_mintick(close - exitDistance)

//                         rrTPtext = switch useRR 

//                             true => "\nTP1: " + str.tostring(getRRtpShort, format.mintick)
//                             =>      ""

//                         risk = math.abs(close - exitPrice)

//                         contracts = switch syminfo.type == "futures" 
                            
//                             false => stopLossAmount / risk
//                             =>       math.floor(stopLossAmount / risk)


//                         label.new(time, high, text = "▼", style = label.style_text_outline, 
//                                          xloc      = xloc.bar_time, 
//                                          size      = labelSize, 
//                                          yloc      = yloc.abovebar,
//                                          textcolor = color.rgb(255, 116, 116), 
//                                          color     = chart.fg_color, 
//                                          tooltip   = "Entry: " + str.tostring(close, format.mintick) 
//                                                    + "\nTrailing PT Trigger: " + str.tostring(shortTP, format.mintick) + " (" + str.tostring((shortTP / close - 1) * 100, format.percent) + ")"
//                                                    + "\nInitial SL: " + str.tostring(shortStop, format.mintick) + " (" + str.tostring((shortStop / close - 1) * 100, format.percent)    + ")"
//                                                    + rrTPtext  + "\nIdeal Amount: " + str.tostring(contracts, "###,###.##"))
                                                   
                    
//             if tradeTypeMaster == "Cheap" 
            
//                 if isHighFirst2Long and tradeLong
                
//                     Range = math.abs(y2PriceHTFL.first() - y1PriceHTFL.first()) * (buySellRange / 100)

//                     if getLTFclo < getBreakoutPointUp and getLTFclo1 >= getBreakoutPointUp 
//                    and getBreakoutPointUp != 0 and getBreakoutPointUp <= y1PriceHTFL.first() + Range 
//                    and inTradeLongVAR == 0 and inTradeShortVAR == 0
                    
//                         exitPrice      = math.round_to_mintick(close - atrLTF * bestTrailing)

//                         longStop       := exitPrice 
//                         longTP         := math.round_to_mintick(close + atrLTF * bestTarget  )
//                         tradeEntryLong := close
//                         inTradeLongVAR := 1

//                         getDivLong := 1

//                         exitDistance = math.abs(exitPrice - close) * RRmult 

//                         getRRtpLong := math.round_to_mintick(close + exitDistance)

//                         rrTPtext = switch useRR 

//                             true => "\nTP1: " + str.tostring(getRRtpLong, format.mintick)
//                             =>      ""

//                         risk = close - exitPrice

//                         contracts = switch syminfo.type == "futures" 
                            
//                             false => stopLossAmount / risk
//                             =>       math.floor(stopLossAmount / risk)


//                         label.new(time, low, text = "▲", style = label.style_text_outline, 
//                                      xloc      = xloc.bar_time, 
//                                      size      = labelSize, 
//                                      yloc      = yloc.belowbar,
//                                      textcolor = #74ffbc, 
//                                      color     = chart.fg_color, 
//                                      tooltip   = "Entry: " + str.tostring(close, format.mintick) 
//                                      + "\nTrailing PT Trigger: " + str.tostring(longTP, format.mintick) + " (" + str.tostring((longTP / close - 1) * 100, format.percent) + ")"
//                                      + "\nInitial SL: "          + str.tostring(longStop, format.mintick) + " (" + str.tostring((longStop / close - 1) * 100, format.percent)    + ")"
//                                      + rrTPtext 
//                                      + "\nIdeal Amount: " + str.tostring(contracts, "###,###.##")
//                                      )
                
//             if tradeTypeMaster == "Cheap"
            
//                 if not isHighFirst2Short and tradeShort 
                
//                     Range = math.abs(y2PriceHTFS.first() - y1PriceHTFS.first()) * (buySellRange / 100)

//                     if getLTFclo > getBreakoutPointDn and getLTFclo1 <= getBreakoutPointDn 
//                    and getBreakoutPointDn != 20e20 and getBreakoutPointDn >= y1PriceHTFS.first() - Range 
//                    and inTradeShortVAR == 0 and inTradeLongVAR == 0
                    
//                         exitPrice = math.round_to_mintick(close + atrLTF * bestTrailingS)

//                         shortStop       := exitPrice 
//                         shortTP         := math.round_to_mintick(close - atrLTF * bestTargetS)
//                         inTradeShortVAR := -1
//                         tradeEntryShort := close

//                         getDivShort := 1

//                         exitDistance = math.abs(exitPrice - close) * RRmult 

//                         getRRtpShort := math.round_to_mintick(close - exitDistance)

//                         rrTPtext = switch useRR 

//                             true => "\nTP1: " + str.tostring(getRRtpShort, format.mintick)
//                             =>      ""


//                         risk = math.abs(close - exitPrice)

//                         contracts = switch syminfo.type == "futures" 
                            
//                             false => stopLossAmount / risk
//                             =>       math.floor(stopLossAmount / risk)


//                         label.new(time, high, text = "▼", style = label.style_text_outline, 
//                                          xloc      = xloc.bar_time, 
//                                          size      = labelSize, 
//                                          yloc      = yloc.abovebar,
//                                          textcolor = color.rgb(255, 116, 116), 
//                                          color     = chart.fg_color, 
//                                          tooltip   = "Entry: " + str.tostring(close, format.mintick) 
//                                                    + "\nTrailing PT Trigger: " + str.tostring(shortTP, format.mintick) + " (" + str.tostring((shortTP / close - 1) * 100, format.percent) + ")"
//                                                    + "\nInitial SL: " + str.tostring(shortStop, format.mintick) + " (" + str.tostring((shortStop / close - 1) * 100, format.percent)    + ")"
//                                                    + rrTPtext 
//                                                    + "\nIdeal Amount: " + str.tostring(contracts, "###,###.##")
//                                                    )
                        
//     if inTradeLongVAR[1] > 0 and inTradeLongVAR > 0
    
//         if getDivLong == 1 and useRR and open > longStop

//             if open >= getRRtpLong
                    
//                 getDivLong := 2

//                 label.new(time, low, text = "▼", style = label.style_text_outline, 
//                                  xloc      = xloc.bar_time, size = labelSize, 
//                                  yloc      = yloc.abovebar,
//                                  textcolor = color.rgb(128, 116, 255), 
//                                  color     = chart.fg_color, 
//                                  tooltip   = "Trade Entry: " + str.tostring(tradeEntryLong, format.mintick) 
//                                           + "\nTrade Exit: " + str.tostring(open, format.mintick) 
//                                           + "\nProfit At Target 1: " + str.tostring((open / tradeEntryLong - 1) * 100 / getDivLong, format.percent))

//             else if high >= getRRtpLong 

//                 if low > longStop 

//                     getDivLong := 2
    
//                     label.new(time, low, text = "▼", style = label.style_text_outline, 
//                                      xloc      = xloc.bar_time, size = labelSize, 
//                                      yloc      = yloc.abovebar,
//                                      textcolor = color.rgb(128, 116, 255), 
//                                      color     = chart.fg_color, 
//                                      tooltip   = "Trade Entry: " + str.tostring(tradeEntryLong, format.mintick) 
//                                               + "\nTrade Exit: " + str.tostring(getRRtpLong, format.mintick) 
//                                               + "\nProfit At Target 1: " + str.tostring((getRRtpLong / tradeEntryLong - 1) * 100 / getDivLong, format.percent))

//                 else 

//                     if not closer2low 
                    
//                         getDivLong := 2
    
//                         label.new(time, low, text = "▼", style = label.style_text_outline, 
//                                          xloc      = xloc.bar_time, size = labelSize, 
//                                          yloc      = yloc.abovebar,
//                                          textcolor = color.rgb(128, 116, 255), 
//                                          color     = chart.fg_color, 
//                                          tooltip   = "Trade Entry: " + str.tostring(tradeEntryLong, format.mintick) 
//                                                   + "\nTrade Exit: " + str.tostring(getRRtpLong, format.mintick) 
//                                                   + "\nProfit At Target 1: " + str.tostring((getRRtpLong / tradeEntryLong - 1) * 100 / getDivLong, format.percent))

//         if open <= longStop 

//             inTradeLongVAR := 0 

//             label.new(time, low, text = "▼", style = label.style_text_outline, 
//                              xloc      = xloc.bar_time, size = labelSize, 
//                              yloc      = yloc.abovebar,
//                              textcolor = color.rgb(128, 116, 255), 
//                              color     = chart.fg_color, 
//                              tooltip   = "Trade Entry: " + str.tostring(tradeEntryLong, format.mintick) 
//                                       + "\nTrade Exit: " + str.tostring(open, format.mintick) 
//                                       + "\nProfit: " + str.tostring((open / tradeEntryLong - 1) * 100 / getDivLong, format.percent))

//             switch math.sign(open / tradeEntryLong - 1)
            
//                 1  => finalLONGPFPROFIT.set(0, finalLONGPFPROFIT.first() + (math.abs(open - tradeEntryLong) / getDivLong))
//                 -1 => finalLONGPFLOSS  .set(0, finalLONGPFLOSS.first()   + (math.abs(open - tradeEntryLong) / getDivLong))

//         else if low <= longStop

//             inTradeLongVAR := 0

//             label.new(time, low, text = "▼", 
//                              style     = label.style_text_outline, 
//                              xloc      = xloc.bar_time, 
//                              size      = labelSize, 
//                              yloc      = yloc.abovebar,
//                              textcolor = color.rgb(128, 116, 255), 
//                              color     = chart.fg_color,  
//                              tooltip = "Trade Entry: "   + str.tostring(tradeEntryLong, format.mintick) 
//                              +         "\nTrade Exit: "  + str.tostring(longStop, format.mintick) 
//                              +         "\nProfit: "      + str.tostring((longStop / tradeEntryLong - 1) * 100 / getDivLong, format.percent))

//             switch math.sign(longStop / tradeEntryLong - 1)
            
//                 1  => finalLONGPFPROFIT.set(0, finalLONGPFPROFIT.first() + (math.abs(longStop - tradeEntryLong) / getDivLong))
//                 -1 => finalLONGPFLOSS  .set(0, finalLONGPFLOSS  .first() + (math.abs(longStop - tradeEntryLong) / getDivLong))

//         else if session.islastbar_regular and closeEOD

//             inTradeLongVAR := 0

//             label.new(time, low, text = "▼", 
//                              style     = label.style_text_outline, 
//                              xloc      = xloc.bar_time, 
//                              size      = labelSize, 
//                              yloc      = yloc.abovebar,
//                              textcolor = color.rgb(128, 116, 255), 
//                              color     = chart.fg_color,  
//                              tooltip = "Trade Entry: "   + str.tostring(tradeEntryLong, format.mintick) 
//                              +         "\nTrade Exit: "  + str.tostring(close, format.mintick) 
//                              +         "\Return: "      + str.tostring((close / tradeEntryLong - 1) * 100 / getDivLong, format.percent))

//             switch math.sign(close / tradeEntryLong - 1)
            
//                 1  => finalLONGPFPROFIT.set(0, finalLONGPFPROFIT.first() + (math.abs(close - tradeEntryLong) / getDivLong))
//                 -1 => finalLONGPFLOSS  .set(0, finalLONGPFLOSS  .first() + (math.abs(close - tradeEntryLong) / getDivLong))


//     if inTradeShortVAR[1] < 0 and inTradeShortVAR < 0



//         if getDivShort == 1 and useRR and open < shortStop

//             if open <= getRRtpShort
                    
//                 getDivShort := 2

//                 label.new(time, high, text = "▲", style = label.style_text_outline, 
//                                      xloc      = xloc.bar_time, 
//                                      size      = labelSize, 
//                                      yloc      = yloc.belowbar,
//                                      textcolor = color.rgb(128, 116, 255), 
//                                      color     = chart.fg_color, 
//                                      tooltip   = "Trade Entry: " + str.tostring(tradeEntryShort) 
//                                              + "\nTrade Exit: " + str.tostring(open) + "\nProfit: " 
//                                              + str.tostring((open / tradeEntryShort - 1) * 100 * -1 / getDivShort, format.percent
//                                              ))

//             else if low <= getRRtpShort

//                 if high < shortStop 

//                     getDivShort := 2

//                     label.new(time, high, text = "▲", style = label.style_text_outline, 
//                                          xloc      = xloc.bar_time, 
//                                          size      = labelSize, 
//                                          yloc      = yloc.belowbar,
//                                          textcolor = color.rgb(128, 116, 255), 
//                                          color     = chart.fg_color, 
//                                          tooltip   = "Trade Entry: " + str.tostring(tradeEntryShort) 
//                                                  + "\nTrade Exit: " + str.tostring(getRRtpShort) + "\nProfit: " 
//                                                  + str.tostring((getRRtpShort / tradeEntryShort - 1) * 100 * -1 / getDivShort, format.percent
//                                              ))
//                 else 

//                     if closer2low 
                    
//                         getDivShort := 2
    
//                         label.new(time, high, text = "▲", style = label.style_text_outline, 
//                                              xloc      = xloc.bar_time, 
//                                              size      = labelSize, 
//                                              yloc      = yloc.belowbar,
//                                              textcolor = color.rgb(128, 116, 255), 
//                                              color     = chart.fg_color, 
//                                              tooltip   = "Trade Entry: " + str.tostring(tradeEntryShort) 
//                                                      + "\nTrade Exit: " + str.tostring(getRRtpShort) + "\nProfit: " 
//                                                      + str.tostring((getRRtpShort / tradeEntryShort - 1) * 100 * -1 / getDivShort, format.percent
//                                                  ))

//         if open >= shortStop 

//             inTradeShortVAR := 0 

//             label.new(time, high, text = "▲", style = label.style_text_outline, 
//                                  xloc      = xloc.bar_time, 
//                                  size      = labelSize, 
//                                  yloc      = yloc.belowbar,
//                                  textcolor = color.rgb(128, 116, 255), 
//                                  color     = chart.fg_color, 
//                                  tooltip   = "Trade Entry: " + str.tostring(tradeEntryShort) 
//                                          + "\nTrade Exit: " + str.tostring(open) + "\nProfit: " 
//                                          + str.tostring((open / tradeEntryShort - 1) * 100 * -1 / getDivShort, format.percent
//                                          ))

//             switch math.sign(open / tradeEntryShort - 1)
            
//                 -1  => finalSHORTPFPROFIT.set(0, finalSHORTPFPROFIT.first() + (math.abs(open - tradeEntryShort) / getDivShort))
//                 1   => finalSHORTPFLOSS  .set(0, finalSHORTPFLOSS.first() + (math.abs(open - tradeEntryShort) / getDivShort))


//         else if high >= shortStop

//             inTradeShortVAR := 0

//             label.new(time, high, text = "▲", style = label.style_text_outline, 
//                                  xloc      = xloc.bar_time, 
//                                  size      = labelSize, 
//                                  yloc      = yloc.belowbar,
//                                  textcolor = color.rgb(128, 116, 255), 
//                                  color     = chart.fg_color, 
//                                  tooltip   = "Trade Entry: " + str.tostring(tradeEntryShort) 
//                                           + "\nTrade Exit: " + str.tostring(shortStop) + "\nProfit: " 
//                                           + str.tostring((shortStop / tradeEntryShort - 1) * 100 * -1 / getDivShort, format.percent
//                                           ))

//             switch math.sign(shortStop / tradeEntryShort - 1)
            
//                 -1  =>  finalSHORTPFPROFIT.set(0, finalSHORTPFPROFIT.first() + (math.abs(shortStop - tradeEntryShort) / getDivShort))
//                 1   =>  finalSHORTPFLOSS  .set(0, finalSHORTPFLOSS  .first() + (math.abs(shortStop - tradeEntryShort) / getDivShort))

//         else if closeEOD and session.islastbar_regular

//             inTradeShortVAR := 0

//             label.new(time, high, text = "▲", style = label.style_text_outline, 
//                                  xloc      = xloc.bar_time, 
//                                  size      = labelSize, 
//                                  yloc      = yloc.belowbar,
//                                  textcolor = color.rgb(128, 116, 255), 
//                                  color     = chart.fg_color, 
//                                  tooltip   = "Trade Entry: " + str.tostring(tradeEntryShort) 
//                                           + "\nTrade Exit: " + str.tostring(close) + "\nReturn: " 
//                                           + str.tostring((close / tradeEntryShort - 1) * 100 * -1 / getDivShort, format.percent
//                                           ))

//             switch math.sign(close / tradeEntryShort - 1)
            
//                 -1  =>  finalSHORTPFPROFIT.set(0, finalSHORTPFPROFIT.first() + (math.abs(close - tradeEntryShort) / getDivShort))
//                 1   =>  finalSHORTPFLOSS  .set(0, finalSHORTPFLOSS  .first() + (math.abs(close - tradeEntryShort) / getDivShort))


//     var inTrailLong = false 
//     var inTrailShort = false 

//     if inTradeLongVAR == 0 
//         inTrailLong := false

//     if inTradeShortVAR == 0 
//         inTrailShort := false 

//     if isLastBar and barstate.isconfirmed

//         if inTradeLongVAR > 0
//             if not inTrailLong 
//                 if close >= longTP
//                     inTrailLong := true 
//                     inTradeLongVAR := 2

//             if inTrailLong
//                 longStop := math.max(longStop, math.round_to_mintick(close - atrLTF * bestTrailing))

//         if inTradeShortVAR < 0
//             if not inTrailShort 
//                 if close <= shortTP
//                     inTrailShort := true 
//                     inTradeShortVAR := -2

//             if inTrailShort 
//                 shortStop := math.min(shortStop, math.round_to_mintick(close + atrLTF * bestTrailingS))

//     var breakUpArr = matrix.new<float>(3, 0)
//     var breakdnArr = matrix.new<float>(3, 0)

//     if y1PriceLTFL.first() > y2PriceLTFL.first()

//         getBreakoutPointUp := y1PriceLTFL.first()
//         timeRow             = breakUpArr.row(0)

//         if not timeRow.includes(y1xLTFL.first())
//             breakUpArr.add_col(breakUpArr.columns(), array.from(y1xLTFL.first(), y1PriceLTFL.first(), 0))

//     if y1PriceLTFS.first() < y2PriceLTFS.first()

//         getBreakoutPointDn := y1PriceLTFS.first()
//         timeRow             = breakdnArr.row(0)

//         if not timeRow.includes(y1xLTFS.first())
//             breakdnArr.add_col(breakdnArr.columns(), array.from(y1xLTFS.first(), y1PriceLTFS.first(), 0))

//     if breakdnArr.columns() > 1000
//         breakdnArr.remove_col(0)

//     if breakUpArr.columns() > 1000
//         breakUpArr.remove_col(0)


//     var recentBreakUp = 0. 
//     var recentBreakDn = 0.


//     if breakUpArr.columns() > 0 and barstate.isconfirmed

//         getCols = breakUpArr.columns() - 1
//         get1    = breakUpArr.get(1, getCols)

//         if breakUpArr.get(2, getCols) == 0

//             if close > get1 and close[1] <= get1
         

//                 line.new(int(breakUpArr.get(0, getCols)), breakUpArr.get(1, getCols), time, breakUpArr.get(1, getCols), 
//                                                   color = #74ffbc, 
//                                                   style = line.style_dotted, 
//                                                   xloc  = xloc.bar_time
//                                                   )
//                 if showLab

//                     label.new(time, high, text = "Break Up", size = labelSize, 
//                                      color     = #00000000, 
//                                      textcolor = #74ffbc, 
//                                      xloc      = xloc.bar_time
//                                      )
                
//                 breakUpArr.set(2, getCols,  1)

//     if breakdnArr.columns() > 0 and barstate.isconfirmed

//         getCols = breakdnArr.columns() - 1
//         get1  = breakdnArr.get(1, getCols)

//         if breakdnArr.get(2, getCols) == 0
        
//             if close > get1 and close[1] <= get1
            
//                 line.new(int(breakdnArr.get(0, getCols)), breakdnArr.get(1, getCols), time, breakdnArr.get(1, getCols), 
//                                              color = color.rgb(255, 116, 116), 
//                                              style = line.style_dotted, 
//                                              xloc  = xloc.bar_time
//                                              )
                
//                 if showLab

//                     label.new(time, low, text = "Break Dn", size = labelSize, 
//                                              color     = #00000000, 
//                                              textcolor = color.rgb(255, 116, 116), 
//                                              xloc      = xloc.bar_time, 
//                                              style     = label.style_label_up
//                                              )
                
//                 breakdnArr.set(2, getCols, 1)

//     [inTradeLongVAR, inTradeShortVAR, longStop, shortStop, longTP, shortTP, tradeEntryLong, tradeEntryShort, getRRtpLong, getRRtpShort, getDivLong, getDivShort]

