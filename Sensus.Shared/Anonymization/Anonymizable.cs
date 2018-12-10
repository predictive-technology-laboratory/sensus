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
using System.Collections.Generic;
using Sensus.Anonymization.Anonymizers;
using Sensus.Exceptions;

namespace Sensus.Anonymization
{
    /// <summary>
    /// Declares a <see cref="Datum"/> property to be anonymizable via the declared anonymizers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Anonymizable : Attribute
    {
        private string _propertyDisplayName;
        private List<Anonymizer> _availableAnonymizers;
        private Anonymizer _defaultAnonymizer;

        public string PropertyDisplayName
        {
            get { return _propertyDisplayName; }
        }

        public List<Anonymizer> AvailableAnonymizers
        {
            get { return _availableAnonymizers; }
        }            

        public Anonymizer DefaultAnonymizer
        {
            get { return _defaultAnonymizer; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Anonymizable"/> class.
        /// </summary>
        /// <param name="propertyDisplayName">Property display name.</param>
        /// <param name="availableAnonymizerTypes">Available anonymizer types.</param>
        /// <param name="defaultAnonymizerIndex">Default anonymizer index. Pass -1 for no anonymization.</param>
        public Anonymizable(string propertyDisplayName, Type[] availableAnonymizerTypes, int defaultAnonymizerIndex)
        {                       
            _propertyDisplayName = propertyDisplayName;

            if (availableAnonymizerTypes == null)
            {
                availableAnonymizerTypes = new Type[0];
            }

            // we're always going to add the value omitting anonymizer at the start of the anonymizers list. if 
            // the default is >= 0 add 1 to produce the result that the caller desires. only do this if the 
            // caller has passed in anonymizer types. if they didn't and they set the default anonymizer to
            // 0, then they are asking for the value omitting anonymizer by default -- in this case we should
            // not increment.
            if (defaultAnonymizerIndex >= 0 && availableAnonymizerTypes.Length > 0)
            {
                ++defaultAnonymizerIndex;
            }

            // instantiate available anonymizers
            _availableAnonymizers = new List<Anonymizer>();
            _availableAnonymizers.Add(new ValueOmittingAnonymizer());  // omitting the value is always an option
            foreach (Type availableAnonymizerType in availableAnonymizerTypes)
            {
                Anonymizer availableAnonymizer = Activator.CreateInstance(availableAnonymizerType) as Anonymizer;

                if (availableAnonymizer == null)
                {
                    throw SensusException.Report("Attempted to create an anonymizer from a type that does not derive from Anonymizer.");
                }
                
                _availableAnonymizers.Add(availableAnonymizer);
            }

            if (defaultAnonymizerIndex < -1 || defaultAnonymizerIndex >= _availableAnonymizers.Count)
            {
                throw SensusException.Report("Attempted to set default anonymizer for property outside the bounds of available types:  " + defaultAnonymizerIndex);
            }

            // set default anonymizer if requested
            if (defaultAnonymizerIndex >= 0)
                _defaultAnonymizer = _availableAnonymizers[defaultAnonymizerIndex];
        }

        public Anonymizable(string propertyDisplayName, Type anonymizerType, bool anonymizeByDefault)
            : this(propertyDisplayName, anonymizerType == null ? new Type[0] : new Type[] { anonymizerType }, anonymizeByDefault ? 0 : -1)
        {
        }
    }
}  
