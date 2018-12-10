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

namespace Sensus.Dispose
{
    /// <summary>
    /// This implements the Dispose pattern suggested by Microsoft at http://msdn.microsoft.com/en-us/library/fs2xkftw.aspx
    /// </summary>
    public class Disposable : IDisposable
    {
        #region Fields
        private bool _disposed;
        #endregion

        #region Constructor

        protected Disposable()
        { }

        #endregion Constructor

        #region Public Methods
        public void Dispose()
        {
            if (!_disposed) Dispose(true);

            if(!_disposed) throw new InvalidOperationException("One of the inheritng classes didn't correctly implement the pattern. This may cause unexpected errors.");

            //Skip the finalizer, defined below
            GC.SuppressFinalize(this); 
        }
        #endregion

        #region Protected Methods
        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
        }

        /// <summary>
        /// In theory, all public methods should call this internally
        /// </summary>
        protected void DisposeCheck()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
        }
        #endregion

        #region Finalizer
        ~Disposable()
        {
            Dispose(false);
        }
        #endregion
    }
}
