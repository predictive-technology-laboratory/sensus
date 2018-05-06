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

namespace Sensus.DataStores
{
    /// <summary>
    /// Implemented by <see cref="DataStore"/>s that are able to clear their content. For example,
    /// the <see cref="Local.FileLocalDataStore"/> accumulates data within files on the device's local storage. Periodically,
    /// these files are transferred to a <see cref="Remote.RemoteDataStore"/> and the files are deleted. If the user wishes
    /// to free up space prior to such deletions, he or she may do so via user interface elements provided by classes that
    /// implement this interface.
    /// </summary>
    public interface IClearableDataStore
    {
        void Clear();
    }
}
