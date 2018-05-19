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

namespace Sensus
{
    /// <summary>
    /// Protocol start confirmation mode. Different ways of confirming that the
    /// user would indeed like to start the protocol
    /// </summary>
    public enum ProtocolStartConfirmationMode
    {
        /// <summary>
        /// Do not ask the user to input anything.
        /// </summary>
        None,

        /// <summary>
        /// Ask the user to enter a string of random digits.
        /// </summary>
        RandomDigits,

        /// <summary>
        /// Ask the user to enter their ID (text).
        /// </summary>
        UserIdText,

        /// <summary>
        /// Ask the user to enter their ID (digits).
        /// </summary>
        UserIdDigits,

        /// <summary>
        /// Ask the user to scan a QR code containing their ID.
        /// </summary>
        UserIdQrCode
    }
}
