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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xamarin;

namespace SensusService
{
    public class Logger
    {
        private string _path;
        private LoggingLevel _level;
        private TextWriter[] _otherOutputs;
        private List<string> _messageBuffer;

        public LoggingLevel Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public Logger(string path, LoggingLevel level, params TextWriter[] otherOutputs)
        {
            _path = path;
            _level = level;
            _otherOutputs = otherOutputs;
            _messageBuffer = new List<string>();
        }

        public void Log(string message, LoggingLevel level, Type callingType)
        {
            if (level > _level)
                return;

            // remove newlines and extra white space
            message = new Regex(@"\s\s+").Replace(message.Replace('\r', ' ').Replace('\n', ' ').Trim(), " ");
            if (string.IsNullOrWhiteSpace(message))
                return;

            // add timestamp and type
            message = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ":  " + (callingType == null ? "" : "[" + callingType.Name + "] ") + message;

            lock (_messageBuffer)
            {
                _messageBuffer.Add(message);

                if (_otherOutputs != null)
                    foreach (TextWriter otherOutput in _otherOutputs)
                    {
                        try
                        {
                            otherOutput.WriteLine(message);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Failed to write to output:  " + ex.Message);
                        }
                    }

                // append buffer to file periodically
                if (_messageBuffer.Count % 100 == 0)
                {
                    try
                    {
                        CommitMessageBuffer();
                    }
                    catch (Exception ex)
                    {
                        // try switching the log path to a random file, since access violations might prevent us from writing the current _path (e.g., in the case of crashes)
                        _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Guid.NewGuid().ToString() + ".txt");
                        _messageBuffer.Add("Switched log path to \"" + _path + "\" due to exception:  " + ex.Message);
                    }
                }
            }
        }

         public void CommitMessageBuffer()
        {
            lock (_messageBuffer)
            {
                if (_messageBuffer.Count > 0)
                {
                    try
                    {
                        using (StreamWriter file = new StreamWriter(_path, true))
                        {
                            foreach (string bufferedMessage in _messageBuffer)
                                file.WriteLine(bufferedMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Error committing message buffer:  " + ex.Message, SensusService.LoggingLevel.Normal, GetType());
                    }

                    _messageBuffer.Clear();
                }
            }
        }

        public List<string> Read(int maxMessages, bool mostRecentFirst)
        {            
            lock (_messageBuffer)
            {
                CommitMessageBuffer();

                List<string> messages = new List<string>();

                try
                {
                    using (StreamReader file = new StreamReader(_path))
                    {       
                        if (maxMessages > 0)
                        {
                            int numLines = 0;
                            while (file.ReadLine() != null)
                                ++numLines;
                        
                            file.BaseStream.Position = 0;
                            file.DiscardBufferedData();

                            int linesToSkip = Math.Max(numLines - maxMessages, 0);
                            for (int i = 1; i <= linesToSkip; ++i)
                                file.ReadLine();
                        }

                        string line;
                        while ((line = file.ReadLine()) != null)
                            messages.Add(line);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Error reading log file:  " + ex.Message, SensusService.LoggingLevel.Normal, GetType());
                }

                if (mostRecentFirst)
                    messages.Reverse();

                return messages;
            }
        }

        public void CopyTo(string path)
        {
            lock (_messageBuffer)
            {
                CommitMessageBuffer();

                try
                {                    
                    File.Copy(_path, path);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to copy log file to \"" + path + "\":  " + ex.Message, SensusService.LoggingLevel.Normal, GetType());
                }
            }
        }

        public virtual void Clear()
        {
            lock (_messageBuffer)
            {
                try
                {
                    File.Delete(_path);
                }
                catch (Exception)
                {
                }

                _messageBuffer.Clear();
            }
        }
    }
}