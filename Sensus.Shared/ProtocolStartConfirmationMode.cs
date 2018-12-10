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

namespace Sensus
{
    /// <summary>
    /// Protocol start confirmation mode. Different ways of confirming that the
    /// participant would indeed like to start the protocol
    /// </summary>
    public enum ProtocolStartConfirmationMode
    {
        /// <summary>
        /// Do not ask the participant to input anything.
        /// </summary>
        None,

        /// <summary>
        /// Ask the participant to enter a string of random digits.
        /// </summary>
        RandomDigits,

        /// <summary>
        /// Ask the participant to enter their ID (text).
        /// </summary>
        ParticipantIdText,

        /// <summary>
        /// Ask the participant to enter their ID (digits).
        /// </summary>
        ParticipantIdDigits,

        /// <summary>
        /// Ask the participant to scan a QR code containing their ID.
        /// </summary>
        ParticipantIdQrCode
    }
}
