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

using SensusService.Probes;
using SensusUI.UiProperties;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#if __ANDROID__
using Java.IO;
using Java.Util.Zip;
#elif __IOS__
using MiniZip.ZipArchive;
#else
#error "Unrecognized platform."
#endif

namespace SensusService.DataStores.Local
{
    /// <summary>
    /// Responsible for transferring data from probes to local media.
    /// </summary>
    public abstract class LocalDataStore : DataStore
    {
        private bool _uploadToRemoteDataStore;

        [OnOffUiProperty("Upload to Remote:", true, 3)]
        public bool UploadToRemoteDataStore
        {
            get { return _uploadToRemoteDataStore; }
            set { _uploadToRemoteDataStore = value; }
        }

        protected LocalDataStore()
        {
            _uploadToRemoteDataStore = true;

#if DEBUG || UNIT_TESTING
            CommitDelayMS = 5000;  // 5 seconds...so we can see debugging output quickly
#else
            CommitDelayMS = 1000 * 60 * 15;  // 15 minutes
#endif
        }

        protected sealed override List<Datum> GetDataToCommit(CancellationToken cancellationToken)
        {
            List<Datum> dataToCommit = new List<Datum>();

            foreach (Probe probe in Protocol.Probes)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // the collected data object comes directly from the probe, so lock it down before working with it.
                ICollection<Datum> collectedData = probe.GetCollectedData();
                if (collectedData != null)
                    lock (collectedData)
                        if (collectedData.Count > 0)
                            dataToCommit.AddRange(collectedData);
            }

            SensusServiceHelper.Get().Logger.Log("Retrieved " + dataToCommit.Count + " data elements from probes.", LoggingLevel.Normal, GetType());

            return dataToCommit;
        }

        protected sealed override void ProcessCommittedData(List<Datum> committedData)
        {
            SensusServiceHelper.Get().Logger.Log("Clearing " + committedData.Count + " committed data elements from probes.", LoggingLevel.Normal, GetType());

            foreach (Probe probe in Protocol.Probes)
                probe.ClearDataCommittedToLocalDataStore(committedData);

            SensusServiceHelper.Get().Logger.Log("Done clearing committed data elements from probes.", LoggingLevel.Normal, GetType());
        }

        public abstract List<Datum> GetDataForRemoteDataStore(CancellationToken cancellationToken);

        public abstract void ClearDataCommittedToRemoteDataStore(List<Datum> dataCommittedToRemote);

        public int WriteDataToZipFile(string zipPath, CancellationToken cancellationToken, Action<string, double> progressCallback)
        {
            // create a zip file to hold all data
#if __ANDROID__
            ZipOutputStream zipFile = null;
#elif __IOS__
            ZipArchive zipFile = null;
#endif

            // write all data to separate JSON files. zip files for convenience.
            string directory = null;
            Dictionary<string, StreamWriter> datumTypeFile = new Dictionary<string, StreamWriter>();

            try
            {
                string directoryName = Protocol.Name + "_Data_" + DateTime.UtcNow.ToShortDateString() + "_" + DateTime.UtcNow.ToShortTimeString();
                directoryName = new Regex("[^a-zA-Z0-9]").Replace(directoryName, "_");
                directory = Path.Combine(SensusServiceHelper.SHARE_DIRECTORY, directoryName);

                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);

                Directory.CreateDirectory(directory);

                if (progressCallback != null)
                    progressCallback("Gathering data...", 0);

                int totalDataCount = 0;

                foreach (Tuple<string, string> datumTypeLine in GetDataLinesToWrite(cancellationToken, progressCallback))
                {
                    string datumType = datumTypeLine.Item1;
                    string line = datumTypeLine.Item2;

                    StreamWriter file;
                    if (datumTypeFile.TryGetValue(datumType, out file))
                        file.WriteLine(",");
                    else
                    {
                        file = new StreamWriter(Path.Combine(directory, datumType + ".json"));
                        file.WriteLine("[");
                        datumTypeFile.Add(datumType, file);
                    }

                    file.Write(line);
                    ++totalDataCount;
                }

                // close all files
                foreach (StreamWriter file in datumTypeFile.Values)
                {
                    file.Write(Environment.NewLine + "]");
                    file.Close();
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (progressCallback != null)
                    progressCallback("Compressing data...", 0);

#if __ANDROID__

                directoryName += '/';
                zipFile = new ZipOutputStream(new FileStream(zipPath, FileMode.Create, FileAccess.Write));
                zipFile.PutNextEntry(new ZipEntry(directoryName));

                int dataWritten = 0;

                foreach (string path in Directory.GetFiles(directory))
                {
                    // start json file for data of current type
                    zipFile.PutNextEntry(new ZipEntry(directoryName + Path.GetFileName(path)));

                    using (StreamReader file = new StreamReader(path))
                    {
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (progressCallback != null && totalDataCount >= 10 && (dataWritten % (totalDataCount / 10)) == 0)
                                progressCallback(null, dataWritten / (double)totalDataCount);

                            cancellationToken.ThrowIfCancellationRequested();

                            zipFile.Write(file.CurrentEncoding.GetBytes(line + Environment.NewLine));

                            if (line != "[" && line != "]")
                                ++dataWritten;
                        }
                    }

                    zipFile.CloseEntry();
                    System.IO.File.Delete(path);
                }

                // close entry for directory
                zipFile.CloseEntry();

#elif __IOS__
                zipFile = new ZipArchive();
                zipFile.CreateZipFile(zipPath);
                zipFile.AddFolder(directory, null);
#endif

                if (progressCallback != null)
                    progressCallback(null, 1);

                return totalDataCount;
            }
            finally
            {
                // ensure that zip file is closed.
                try
                {
                    if (zipFile != null)
                    {
#if __ANDROID__
                        zipFile.Close();
#elif __IOS__
                        zipFile.CloseZipFile();
#endif
                    }
                }
                catch (Exception)
                {
                }

                // ensure that all temporary files are closed/deleted.
                foreach (string datumType in datumTypeFile.Keys)
                {
                    try
                    {
                        datumTypeFile[datumType].Close();
                    }
                    catch (Exception)
                    {
                    }
                }

                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception)
                {
                }
            }
        }

        protected abstract IEnumerable<Tuple<string, string>> GetDataLinesToWrite(CancellationToken cancellationToken, Action<string, double> progressCallback);
    }
}
