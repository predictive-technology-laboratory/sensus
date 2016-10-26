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