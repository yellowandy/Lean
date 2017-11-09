/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash. This is a skeleton
    /// framework you can use for designing an algorithm.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class BasicTemplateAlgorithm : QCAlgorithm
    {
        private Symbol _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
		private int sliceCount = 0;

        private MyAapl a1 = null;
        private MyAapl a2 = null;
        private MyAapl a3 = null;
        private MyAapl a4 = null;

        private MyAapl tradeBarBeforeBuy = null;

        private decimal largestLoss = 0;
        private DateTime largestLossDate;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
			SetStartDate(2017, 01, 01);  //Set Start Date
			SetEndDate(2017, 11, 03);    //Set End Date
			SetCash(100000);             //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.
            AddData<MyAapl>("AAPL", Resolution.Minute);

            // There are other assets with similar methods. See "Selecting Options" etc for more details.
            // AddFuture, AddForex, AddCfd, AddOption
        }

		private bool isVolumeIncreasing()
		{
            bool isFirstOk = a2.Volume > (a1.Volume * 1.4m); //at least 40% bigger;
            bool isSecondOk = a3.Volume > (a2.Volume * 1.4m);

            return  isFirstOk && isSecondOk;
		}

		private bool isPriceDescreasing()
		{
            //Ensure our bars are red bars.
            if(a1.Close < a1.Open && a2.Close < a2.Open && a3.Close < a3.Open)
            {
                return a2.Close < a1.Close && a3.Close < a2.Close;
            }
            else
            {
                return false;
            }
			
		}

		private bool isHVT()
		{
			bool isPriceAndVolumeConfirming = isPriceDescreasing() && isVolumeIncreasing();
            bool isLastBarGreen = a4.Close > a4.Open && (a4.Close - a4.Open > .1m);
            bool isGreenBarAboveLast = a4.Close > a3.Close;

			if (isPriceAndVolumeConfirming && isGreenBarAboveLast && isLastBarGreen)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

        private bool isWithinMarketHours(DateTime time)
        {
            if (time.Hour < 10 || (time.Hour > 15 && time.Minute > 30 || time.Hour >= 16))
            {
                return false;
            }
            else 
            {
                return true;
            }

        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            var bar = data["AAPL"];

            if (!isWithinMarketHours(bar.Time))
            {
                //Log(bar.Time + " Not doing anything outside of market hours....");
                return;
            }

            Log("Close IS: " + bar.Open + ", Volume is: " + bar.Volume + ", time is: " + bar.Time);

			a1 = a2;
			a2 = a3;
			a3 = a4;
			a4 = bar; //Always our latest

			sliceCount++;
			if (sliceCount <= 4) return;


			if (!Portfolio.Invested && isHVT())
			{
				Debug("Volumes- v1:" + a1.Volume + ", v2: " + a2.Volume + ", v3: " + a3.Volume + ", v4: " + a4.Volume);
				Debug("Price- p1:" + a1.Close + ", p2: " + a2.Close + ", p3: " + a3.Close + ", p4:" + a4.Close);
				SetHoldings("AAPL", 1);
				Debug(bar.Time + " Purchased Stock at: " + a4.Close);
                tradeBarBeforeBuy = a3;
			}
			else if (Portfolio.Invested)
			{
                if(a4.Close - tradeBarBeforeBuy.Close < -.15m)
                {
                    var tmpLoss = a4.Close - tradeBarBeforeBuy.Close;
                    Error("'" + bar.Time + "','LOSS','" + a4.Close + "','" + tmpLoss + "'\n");

                    if(tmpLoss < largestLoss)
                    {
                        largestLoss = tmpLoss;
                        largestLossDate = bar.Time;
                        Log(largestLossDate + " Largest loss is: " + largestLoss);
                    }

					Liquidate();
                }
				if (a4.Close - tradeBarBeforeBuy.Close > .30m)
				{
                    var gain = a4.Close - tradeBarBeforeBuy.Close;
                    Error("'"+bar.Time + "','WON','" + a4.Close + "','" + gain + "'\n");
					Liquidate();
				}
				//else if (a4.Close - a3.Close < -.10m)
				//{
    //                Debug(bar.Time + "Selling stock for somekind of gain: " + a4.Close + ", a3: "+ a3.Close + "\n");
				//	Liquidate();
				//}
			}
        }

        public override void OnEndOfDay()
        {
            Liquidate();
        }
    }

	public class MyAapl : BaseData
	{
		public int Timestamp = 0;
		public decimal Open = 0;
		public decimal High = 0;
		public decimal Low = 0;
		public decimal Close = 0;
		public decimal Volume = 0;

		/// <summary>
		/// 1. DEFAULT CONSTRUCTOR: Custom data types need a default constructor.
		/// We search for a default constructor so please provide one here. It won't be used for data, just to generate the "Factory".
		/// </summary>
		public MyAapl()
		{
            Symbol = "AAPL";
		}

		/// <summary>
		/// 2. RETURN THE STRING URL SOURCE LOCATION FOR YOUR DATA:
		/// This is a powerful and dynamic select source file method. If you have a large dataset, 10+mb we recommend you break it into smaller files. E.g. One zip per year.
		/// We can accept raw text or ZIP files. We read the file extension to determine if it is a zip file.
		/// </summary>
		/// <param name="config">Configuration object</param>
		/// <param name="date">Date of this source file</param>
		/// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
		/// <returns>String URL of source file.</returns>
		public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
		{
            var formattedDate = date.ToString(DateFormat.EightCharacter);

			var src = string.Format("/Users/asandberg/Downloads/order_394917/allstocks_{0}/table_aapl.csv",formattedDate);

            return new SubscriptionDataSource(src, SubscriptionTransportMedium.LocalFile);
		}

        /// <summary>
        /// 3. READER METHOD: Read 1 line from data source and convert it into Object.
        /// Each line of the CSV File is presented in here. The backend downloads your file, loads it into memory and then line by line
        /// feeds it into your algorithm
        /// </summary>
        /// <param name="line">string line from the data source file submitted above</param>
        /// <param name="config">Subscription data, symbol name, data type</param>
        /// <param name="date">Current date we're requesting. This allows you to break up the data source into daily files.</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>New Bitcoin Object which extends BaseData.</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
			if (line == null)
			{
				return null;
			}


            var aaple = new MyAapl();

			//date | time | open | high | low | close | volume | splits | earnings | dividends

			string[] data = line.Split(',');

            var tmpTimeString = data[1];
            var hourString = "";
            var minuteString = "";
            if(tmpTimeString.Length == 3)
            {
                hourString = "0" + tmpTimeString.Substring(0, 1);
                minuteString = tmpTimeString.Substring(1, 2);
            }
            else 
            {
				hourString = tmpTimeString.Substring(0, 2);
				minuteString = tmpTimeString.Substring(2, 2);
            }

            var timeString = data[0] + " " + hourString + ":" + minuteString;

			
            var time = DateTime.ParseExact(timeString, "yyyyMMdd HH:mm", null);

            aaple.Time = time;
            aaple.Open = data[2].ToDecimal();
            aaple.High = data[3].ToDecimal();
            aaple.Low = data[4].ToDecimal();
            aaple.Close = data[5].ToDecimal();
            aaple.Volume = data[6].ToDecimal();
            aaple.Value = aaple.Close;


            return aaple;
        }
	}


}