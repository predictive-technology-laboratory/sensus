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
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Sensus
{
    public class Logger : ILogger
    {
        private const int MAX_LOG_SIZE_MEGABYTES = 20;

        private string _path;
        private LoggingLevel _level;
        private List<string> _messageBuffer;
		private List<TextWriter> _otherOutputs;
        private Regex _extraWhiteSpace;

        public LoggingLevel Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public Logger(string path, LoggingLevel level, params TextWriter[] otherOutputs)
        {
            _path = path;
            _level = level;
            _messageBuffer = new List<string>();
            _extraWhiteSpace = new Regex(@"\s\s+");

			_otherOutputs = otherOutputs?.ToList() ?? new List<TextWriter>();
        }

        public void Log(string message, LoggingLevel level, Type callingType, bool throwException = false)
        {
            // if we're throwing an exception, use the caller's version of the message instead of our modified version below.
            Exception ex = null;
            if (throwException)
            {
                ex = new Exception(message);
            }
            
            if (level <= _level)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    // add timestamp and calling type type
                    message = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ":  " + (callingType == null ? "" : "[" + callingType.Name + "] ") + message;

                    lock (_messageBuffer)
                    {
                        _messageBuffer.Add(message);

                        if (_otherOutputs != null)
                        {
                            foreach (TextWriter otherOutput in _otherOutputs)
                            {
                                try
                                {
                                    otherOutput.WriteLine(message);
                                }
                                catch (Exception writeException)
                                {
                                    Console.Error.WriteLine("Failed to write to output:  " + writeException.Message);
                                }
                            }
                        }

                        // append buffer to file periodically
                        if (_messageBuffer.Count % 100 == 0)
                        {
                            try
                            {
                                CommitMessageBuffer();
                            }
                            catch (Exception commitException)
                            {
                                // try switching the log path to a random file, since access violations might prevent us from writing the current _path (e.g., in the case of crashes)
                                _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Guid.NewGuid().ToString() + ".txt");
                                _messageBuffer.Add("Switched log path to \"" + _path + "\" due to exception:  " + commitException.Message);
                            }
                        }
                    }
                }
            }

            if (ex != null)
            {
                throw ex;
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
                            {
                                file.WriteLine(bufferedMessage);
                            }
                        }

                        // keep log file under a certain size by reading the most recent MAX_LOG_SIZE_MEGABYTES.
                        long currSizeBytes = new FileInfo(_path).Length;
                        if (currSizeBytes > MAX_LOG_SIZE_MEGABYTES * 1024 * 1024)
                        {
                            int newSizeBytes = (MAX_LOG_SIZE_MEGABYTES - 5) * 1024 * 1024;
                            byte[] newBytes = new byte[newSizeBytes];

                            using (FileStream file = new FileStream(_path, FileMode.Open, FileAccess.Read))
                            {
                                file.Position = currSizeBytes - newSizeBytes;
                                file.Read(newBytes, 0, newSizeBytes);
                            }

                            File.Delete(_path);
                            File.WriteAllBytes(_path, newBytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Error committing message buffer:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }

                    _messageBuffer.Clear();
                }
            }
        }

        public List<string> Read(int maxLines, bool mostRecentFirst)
        {
            lock (_messageBuffer)
            {
                CommitMessageBuffer();

                List<string> lines = new List<string>();

                try
                {
                    using (StreamReader file = new StreamReader(_path))
                    {
						if (maxLines > 0)
						{
							int numLines = 0;
							while (file.ReadLine() != null)
							{
								++numLines;
							}

							file.BaseStream.Position = 0;
							file.DiscardBufferedData();

							int linesToSkip = Math.Max(numLines - maxLines, 0);
							for (int i = 1; i <= linesToSkip; ++i)
							{
								file.ReadLine();
							}
						}

						string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }

                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Error reading log file:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                if (mostRecentFirst)
                {
                    lines.Reverse();
                }

                return lines;
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
                    SensusServiceHelper.Get().Logger.Log("Failed to copy log file to \"" + path + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
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

		public void AddOtherOutput(TextWriter writer)
		{
			lock(_otherOutputs)
			{
				_otherOutputs.Add(writer);
			}
		}

		public void RemoveOtherOutput(TextWriter writer)
		{
			lock (_otherOutputs)
			{
				_otherOutputs.Remove(writer);
			}
		}
	}
}