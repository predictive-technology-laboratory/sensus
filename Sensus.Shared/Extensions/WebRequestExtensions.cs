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
using System.Net;
using System.Threading.Tasks;

namespace Sensus.Extensions
{
    public static class WebRequestExtensions
    {
        public static async Task<WebResponse> GetResponseAsync(this WebRequest webRequest, TimeSpan? timeout = null)
        {
            Task<WebResponse> webResponseTask = webRequest.GetResponseAsync();
            Task timeoutTask = Task.Delay((int)(timeout?.TotalMilliseconds ?? int.MaxValue));

            Task completedTask = await Task.WhenAny(webResponseTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Timed out.");
            }

            return await webResponseTask;
        }
    }
}