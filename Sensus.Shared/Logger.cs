//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sensus
{
    public class Logger : ILogger
    {
        private const int MAX_LOG_SIZE_MEGABYTES = 20;

        private string _path;
        private LoggingLevel _level;
        private TextWriter[] _otherOutputs;
        private List<string> _messageBuffer;
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
            _otherOutputs = otherOutputs;
            _messageBuffer = new List<string>();
            _extraWhiteSpace = new Regex(@"\s\s+");
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
                // remove newlines and extra white space, and only log if the result is non-empty
                message = _extraWhiteSpace.Replace(message.Replace('\r', ' ').Replace('\n', ' ').Trim(), " ");
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
                            {
                                ++numLines;
                            }
                        
                            file.BaseStream.Position = 0;
                            file.DiscardBufferedData();

                            int linesToSkip = Math.Max(numLines - maxMessages, 0);
                            for (int i = 1; i <= linesToSkip; ++i)
                            {
                                file.ReadLine();
                            }
                        }

                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            messages.Add(line);
                        }

                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Error reading log file:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                if (mostRecentFirst)
                {
                    messages.Reverse();
                }

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
    }
}
