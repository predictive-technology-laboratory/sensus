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

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// Status of scripts within Sensus
    /// </summary>
    public enum ScriptState
    {
        /// <summary>
        /// The script was delivered to the user and made available for taking. On Android this will be exactly
        /// the scheduled/triggered time. On iOS, this will be exactly the triggered time, but when it comes to
        /// scheduled scripts the timing depends on whether push notifications are enabled. Without push notifications
        /// the delivery time will be when the user brings the app to the foreground. With push notifications
        /// enabled, the delivery time will correspond to the arrival of the push notification, which causes the
        /// script to run.
        /// </summary>
        Delivered,

        /// <summary>
        /// The script was opened by the user after it was <see cref="Delivered"/>.
        /// </summary>
        Opened,

        /// <summary>
        /// The script was <see cref="Opened"/> by the user, but the user subsequently cancelled its completion.
        /// </summary>
        Cancelled,

        /// <summary>
        /// The script was submitted by the user after it was <see cref="Opened"/>, though the user might not necessarily have completed all fields.
        /// </summary>
        Submitted,

        /// <summary>
        /// The script was deleted by the user after it was <see cref="Delivered"/>.
        /// </summary>
        Deleted,

        /// <summary>
        /// The script expired after it was <see cref="Delivered"/>.
        /// </summary>
        Expired
    }
}
