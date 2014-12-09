using SensusService.Exceptions;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SensusService
{
    /// <summary>
    /// Logger for Sensus.
    /// </summary>
    public class Logger : TextWriter
    {
        private string _path;
        private StreamWriter _file;
        private bool _writeTimestamp;
        private TextWriter[] _otherOutputs;
        private bool _previousWriteNewLine;
        private LoggingLevel _level;

        public LoggingLevel Level
        {
            get { return _level; }
        }

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
        public Logger(string path, bool writeTimestamp, bool append, LoggingLevel level, params TextWriter[] otherOutputs)
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
            lock (this)
            {
                value = (_writeTimestamp && _previousWriteNewLine ? DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ":  " : "") + value + (newLine ? Environment.NewLine : "");

                base.Write(value);

                try { _file.Write(value); }
                catch (Exception) { }

                foreach (TextWriter otherOutput in _otherOutputs)
                    try { otherOutput.Write(value); }
                    catch (Exception ex) { throw new SensusException("Failed to write to other output from LogWriter:  " + ex.Message + Environment.NewLine + ex.StackTrace); }

                _previousWriteNewLine = newLine;

                return value;
            }
        }

        public void Log(string message, LoggingLevel level, params string[] tags)
        {
            if (level > _level)
                return;

            StringBuilder tagString = null;
            if (tags != null && tags.Length > 0)
            {
                tagString = new StringBuilder();
                foreach (string tag in tags)
                    if (!string.IsNullOrWhiteSpace(tag))
                        tagString.Append("[" + tag.ToUpper() + "]");
            }

            WriteLine((tagString == null || tagString.Length == 0 ? "" : tagString.ToString() + ":") + message);
        }

        /// <summary>
        /// Closes this logger.
        /// </summary>
        public override void Close()
        {
            lock (this)
            {
                base.Close();
                _file.Close();
            }
        }
    }
}
