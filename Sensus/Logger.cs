using Sensus.Exceptions;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Sensus
{
    /// <summary>
    /// Alternative writer for standard out. Pass to Console.SetOut to use.
    /// </summary>
    public class Logger : TextWriter
    {
        private static Logger _logger;

        public static LoggingLevel Level
        {
            get { return _logger._level; }
        }

        public static void Init(string path, bool writeTimestamp, bool append, LoggingLevel level, params TextWriter[] otherOutputs)
        {
            _logger = new Logger(path, writeTimestamp, append, level, otherOutputs);
        }

        public static void Log(string message)
        {
            _logger.WriteLine(message.Trim() + Environment.NewLine);
        }

        private string _path;
        private StreamWriter _file;
        private bool _writeTimestamp;
        private TextWriter[] _otherOutputs;
        private bool _previousWriteNewLine;
        private LoggingLevel _level;

        /// <summary>
        /// Gets the encoding used by this writer (always UTF8)
        /// </summary>
        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">Path to file to write output to</param>
        /// <param name="writeTimestamp">Whether or not to write a timestamp with each message</param>
        /// <param name="append">Whether or not to append to the output file</param>
        /// <param name="level">Logging level</param>
        /// <param name="otherOutputs">Other outputs to use</param>
        private Logger(string path, bool writeTimestamp, bool append, LoggingLevel level, params TextWriter[] otherOutputs)
        {
            _path = path;
            _writeTimestamp = writeTimestamp;
            _otherOutputs = otherOutputs;
            _file = new StreamWriter(path, append);
            _file.AutoFlush = true;
            _level = level;
            _previousWriteNewLine = true;
        }

        /// <summary>
        /// Truncates the output file and restarts the log
        /// </summary>
        public virtual void Clear()
        {
            lock (_file)
            {
                _file.Close();
                _file = new StreamWriter(_path);
                _file.AutoFlush = true;
            }
        }

        /// <summary>
        /// Writes a string to output. If newlines are present in the passed value they will be written.
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string value)
        {
            if (value.Trim() == "")
                return;

            Write(value, false);
        }

        /// <summary>
        /// Writes a string to output. If newlines are present in the passed value they will be removed.
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(string value)
        {
            value = new Regex(@"\s\s+").Replace(value.Replace('\r', ' ').Replace('\n', ' ').Trim(), " ");
            if (value == "")
                return;

            Write(value, true);
        }

        /// <summary>
        /// Writes a string to output, optionally followed by a newline
        /// </summary>
        /// <param name="value"></param>
        /// <param name="newLine"></param>
        /// <returns>Value that was written</returns>
        protected virtual string Write(string value, bool newLine)
        {
            value = (_writeTimestamp && _previousWriteNewLine ? DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":  " : "") + value + (newLine ? Environment.NewLine : "");

            lock (this) { base.Write(value); }

            lock (_file) { _file.Write(value); }

            foreach (TextWriter otherOutput in _otherOutputs)
            {
                lock (otherOutput)
                {
                    try { otherOutput.Write(value); }
                    catch (Exception ex) { throw new SensusException("Failed to write to other output from LogWriter:  " + ex.Message + Environment.NewLine + ex.StackTrace); }
                }
            }

            _previousWriteNewLine = newLine;

            return value;
        }

        /// <summary>
        /// Closes this writer
        /// </summary>
        public override void Close()
        {
            base.Close();

            _file.Close();
        }
    }
}
