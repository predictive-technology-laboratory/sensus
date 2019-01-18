// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Sensus.Probes.Device;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Device
{
    public class AndroidProcessorUtilizationProbe : ProcessorUtilizationProbe
    {
        private string _cmd;
        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            int pid = global::Android.OS.Process.MyPid();

            //run the top terminal command -n 1 (only run 1 iteration) 
            //-b (use batch mode) 
            //-q (don't show the header info)  
            //-p {pid} (only return for the sensus) 
            //-o %CPU (only return the CPU%
            //this is a different version of top than most of the documentation i found.  The best thing to do is to run "top --help" on the device
            _cmd = $"top -n 1 -b -q -p {pid} -o %CPU";//TODO:  We could get mem% or process time from this command if we wanted using the follow in cmd $"top -n 1 -b -q -p {pid} -o %CPU,%MEM,TIME+"
        }

        protected override async Task StartListeningAsync()
        {
            while (Running)
            {
                await RecordUtilization(); //remove the await
                await Task.Delay(MinDataStoreDelay.HasValue ? MinDataStoreDelay.Value.Milliseconds : 1000);
            }
        }

        protected override Task StopListeningAsync()
        {
            return Task.CompletedTask;
        }

        //https://stackoverflow.com/questions/2467579/how-to-get-cpu-usage-statistics-on-android  This shows the basic idea i used but i had to modify the command quite a bit
        private async Task RecordUtilization()
        {

            try
            {
                Java.Lang.Process p = Java.Lang.Runtime.GetRuntime().Exec(_cmd);


                var inputVal = await ReadStream(p.InputStream);
                if(inputVal == null)
                {
                    var errorOutput = await ReadStream(p.ErrorStream);
                    if(string.IsNullOrWhiteSpace(errorOutput) == false)
                    {
                        throw new NotSupportedException(errorOutput);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                
                if(double.TryParse(inputVal.Trim(), out double cpuPercent) == false)
                {
                    throw new InvalidDataException(inputVal + " cannot be converted into a double");
                }

                await StoreDatumAsync(new ProcessorUtilizationDatum(DateTimeOffset.UtcNow, cpuPercent));

            }
            catch (Exception exc)
            {
                SensusServiceHelper.Get().Logger.Log("Error getting CPU Utilization. msg:"+exc.Message, LoggingLevel.Normal, GetType());
            }
        }

        private async Task<string> ReadStream(Stream s)
        {
            string rVal = null;
            try
            {
                using (var streamReader = new StreamReader(s))
                {
                    if (streamReader.EndOfStream == false)
                    {
                        rVal = await streamReader.ReadToEndAsync();
                        System.Diagnostics.Trace.Write(rVal);
                    }
                }
            }
            finally
            {
                s.Close();
                s.Dispose();
            }
            return rVal;
        }


        //The link in the issue didn't show cpu utilization but just the cpu hardware info.  I left the code here if that info would would useful for you.
        //https://stackoverflow.com/questions/46714396/how-to-find-cpu-load-of-any-android-device-programmatically
        //private async Task GetProcessorHardwareInfo()
        //{
        //    try
        //    {
        //        string holder = "";
        //        string[] DATA = { "/system/bin/cat", "/proc/cpuinfo" };
        //        Java.Lang.ProcessBuilder processBuilder = new Java.Lang.ProcessBuilder(DATA);
        //        Java.Lang.Process process = processBuilder.Start();
        //        using (var inputStream = process.InputStream)
        //        {
        //            using (var streamReader = new StreamReader(inputStream))
        //            {
        //                holder = await streamReader.ReadToEndAsync();
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

    }
}
