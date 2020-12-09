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
using Xamarin.Forms;
using Sensus.UI.UiProperties;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Sensus.UI.Inputs;
using Sensus.Probes.User.Scripts;
using Sensus.Exceptions;
using System.ComponentModel;
using static Sensus.UI.InputGroupPage;
using System.Timers;

// register the input effect group
[assembly: ResolutionGroupName(Input.EFFECT_RESOLUTION_GROUP_NAME)]

namespace Sensus.UI.Inputs
{
	public abstract class Input : INotifyPropertyChanged
	{
		private const double PROGRESS_INCREMENT = 0.005;

		public event PropertyChangedEventHandler PropertyChanged;

		public const string EFFECT_RESOLUTION_GROUP_NAME = "InputEffects";

		private string _name;
		private string _id;
		private string _groupId;
		private string _labelText;
		private int _labelFontSize;
		private View _view;
		private bool _displayNumber;
		private bool _complete;
		private double? _latitude;
		private double? _longitude;
		private DateTimeOffset? _locationUpdateTimestamp;
		private bool _required;
		private bool _viewed;
		private DateTimeOffset? _completionTimestamp;
		private List<InputDisplayCondition> _displayConditions;
		private Color? _backgroundColor;
		private Thickness? _padding;
		private bool _frame;
		private List<InputCompletionRecord> _completionRecords;
		private DateTimeOffset? _submissionTimestamp;
		private bool _correct;
		private int _attempts;
		protected float _score;
		private InputFeedbackView _feedbackView;
		private Timer _delayTimer;

		public InputGroupPage InputGroupPage { get; set; }

		/// <summary>
		/// The name by which this input will be referred to within the Sensus app.
		/// </summary>
		/// <value>The name.</value>
		[EntryStringUiProperty("Name:", true, 0, true)]
		public string Name
		{
			get { return _name; }
			set
			{
				if (value != _name)
				{
					_name = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Caption)));
				}
			}
		}

		public string Id
		{
			get
			{
				return _id;
			}
			set
			{
				_id = value;
			}
		}

		public string GroupId
		{
			get
			{
				return _groupId;
			}
			set
			{
				_groupId = value;
			}
		}

		/// <summary>
		/// The text to display next to the input when showing the field to the user for completion. If you would like to 
		/// use the value of a survey-triggering <see cref="Script.CurrentDatum"/> within the input's label, you can do so 
		/// by placing a <c>{0}</c> within <see cref="LabelText"/> as a placeholder. The placeholder will be replaced with
		/// the value of the triggering <see cref="Datum"/> at runtime. You can read more about the format of the 
		/// placeholder [here](https://msdn.microsoft.com/en-us/library/system.string.format(v=vs.110).aspx).
		/// </summary>
		/// <value>The label text.</value>
		[EntryStringUiProperty("Label Text:", true, 1, true)]
		public string LabelText
		{
			get
			{
				return _labelText;
			}
			set
			{
				_labelText = value;
			}
		}

		public int LabelFontSize
		{
			get
			{
				return _labelFontSize;
			}
			set
			{
				_labelFontSize = value;
			}
		}

		public bool DisplayNumber
		{
			get
			{
				return _displayNumber;
			}
			set
			{
				_displayNumber = value;
			}
		}

		/// <summary>
		/// The value that needs to be provided for the input to be considered correct.
		/// </summary>
		/// <value>Any string, or a Protocol variable name prefixed with an =, e.g. =SomeVariable.</value>
		[EntryStringUiProperty("Correct Value:", true, 20, false)]
		public virtual object CorrectValue { get; set; }

		/// <summary>
		/// Set whether a correct value is required to mark the <see cref="Input"/> complete.
		/// </summary>
		/// <value>Any string, or a Protocol variable name prefixed with an =, e.g. =SomeVariable.</value>
		[OnOffUiProperty("Require Correct Value:", true, 21)]
		public virtual bool RequireCorrectValue { get; set; }

		/// <summary>
		/// The delay in milliseconds to wait when a correct value is provided before navigating, allowing navigation or allowing the user to retry.
		/// </summary>
		[EntryIntegerUiProperty("Correct Delay (MS):", true, 22, false)]
		public virtual int CorrectDelay { get; set; }

		/// <summary>
		/// The message to provide as feedback when a correct value is provided.
		/// </summary>
		[EntryStringUiProperty("Correct Feedback:", true, 23, false)]
		public virtual string CorrectFeedbackMessage { get; set; }

		/// <summary>
		/// The delay in milliseconds to wait when an incorrect value is provided before navigating, allowing navigation or allowing the user to retry.
		/// </summary>
		[EntryIntegerUiProperty("Incorrect Delay (MS):", true, 24, false)]
		public virtual int IncorrectDelay { get; set; }

		/// <summary>
		/// The message to provide as feedback when an incorrect value is provided.
		/// </summary>
		[EntryStringUiProperty("Incorrect Feedback:", true, 25, false)]
		public virtual string IncorrectFeedbackMessage { get; set; }

		/// <summary>
		/// The <see cref="NavigationResult"/> that is set for the <see cref="UI.InputGroupPage"/> when the <see cref="Input"/> is set as complete with a correct value.
		/// </summary>
		[ListUiProperty("Navigate when Correct:", true, 26, new object[] { NavigationResult.None, NavigationResult.Forward, NavigationResult.Backward, NavigationResult.Cancel }, false)]
		public NavigationResult NavigationOnCorrect { get; set; }

		/// <summary>
		/// The <see cref="NavigationResult"/> that is set for the <see cref="UI.InputGroupPage"/> when the <see cref="Input"/> is set as complete with an incorrect value.
		/// </summary>
		[ListUiProperty("Navigate when Incorrect:", true, 27, new object[] { NavigationResult.None, NavigationResult.Forward, NavigationResult.Backward, NavigationResult.Cancel }, false)]
		public NavigationResult NavigationOnIncorrect { get; set; }

		/// <summary>
		/// The number of times the user can retry the <see cref="Input"/> to get a correct answer or improve their score.
		/// </summary>
		[EntryIntegerUiProperty("Allowed Retries:", true, 28, false)]
		public virtual int? Retries { get; set; }

		/// <summary>
		/// The number of attempts the user has made to provide the correct value to the <see cref="Input"/>. The input is disabled after (<see cref="Retries"/> + 1) attempts.
		/// </summary>
		public int Attempts
		{
			get
			{
				return _attempts;
			}
			set
			{
				int retries = Math.Max(0, Retries ?? int.MaxValue);

				_attempts = value;

				// if the maximum number of attempts have been made, then disable the view
				if (_attempts >= retries && _view != null)
				{
					_view.IsEnabled = false;
				}
			}
		}

		/// <summary>
		/// The score group to associate the <see cref="ScoreInput"/> with the <see cref="Input"/>s it keeps score.
		/// A <see cref="ScoreInput"/> with a ScoreGroup of <c>null</c> will accumulate the scores of every 
		/// <see cref="Input"/> in the collection of <see cref="InputGroup"/>s being displayed.
		/// </summary>
		[EntryStringUiProperty("Score Group:", true, 29, false)]
		public string ScoreGroup { get; set; }

		/// <summary>
		/// The method used to accumulate the score for the <see cref="Input"/>.
		/// </summary>
		[ListUiProperty("Score Method:", true, 30, new object[] { ScoreMethods.None, ScoreMethods.First, ScoreMethods.Last, ScoreMethods.Maximum, ScoreMethods.Average }, false)]
		public virtual ScoreMethods ScoreMethod { get; set; } = ScoreMethods.Last;

		/// <summary>
		/// The score that the user will get for providing a correct value.
		/// </summary>
		/// <value>A positive real number to make the <see cref="Input"/> scored or <c>0</c> to make it unscored.</value>
		[EntryFloatUiProperty("Correct Score:", true, 31, false)]
		public virtual float CorrectScore { get; set; }

		/// <summary>
		/// The score that the user will get for providing an incorrect value.
		/// </summary>
		/// <value>A positive real number to make the <see cref="Input"/> scored or <c>0</c> to make it unscored.</value>
		[EntryFloatUiProperty("Incorrect Score:", true, 32, false)]
		public virtual float IncorrectScore { get; set; }

		/// <summary>
		/// The current score of the <see cref="Input"/>.
		/// </summary>
		public virtual float Score
		{
			get
			{
				return _score;
			}
			protected set
			{
				if (_score != value)
				{
					_score = value;

					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score)));
				}
			}
		}

		/// <summary>
		/// A boolean value indicating whether the <see cref="Value"/> is equal to <see cref="CorrectValue"/>.
		/// </summary>
		public bool Correct
		{
			get
			{
				return _correct;
			}
		}

		[JsonIgnore]
		public abstract object Value { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the user has interacted with this <see cref="Input"/>,
		/// leaving it in a state of completion. Contrast with Valid, which merely indicates that the 
		/// state of the input will not prevent the user from moving through an input request (e.g., in the case
		/// of inputs that are not required).
		/// </summary>
		/// <value><c>true</c> if complete; otherwise, <c>false</c>.</value>
		[JsonIgnore]
		public bool Complete
		{
			get
			{
				return _complete;
			}
			protected set
			{
				_complete = value;

				DateTimeOffset timestamp = DateTimeOffset.UtcNow;
				object inputValue = null;
				_completionTimestamp = null;
				_correct = false;

				if (_complete)
				{
					// get a deep copy of the value. some inputs have list values, and simply using the list reference wouldn't track the history, since the most up-to-date list would be used for all history values.
					inputValue = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(Value, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);

					_completionTimestamp = timestamp;

					_correct = IsCorrect(inputValue);

					if (_correct)
					{
						SetScore(CorrectScore);
					}
					else
					{
						SetScore(IncorrectScore);
					}

					_feedbackView?.SetFeedback(_correct);
				}

				if (StoreCompletionRecords)
				{
					// TODO: determine if completion records need to record whether the input was correct and the correct value.
					_completionRecords.Add(new InputCompletionRecord(timestamp, inputValue));
				}

				// if this input defines a protocol variable, set that variable here.
				if (this is IVariableDefiningInput)
				{
					IVariableDefiningInput input = this as IVariableDefiningInput;
					string definedVariable = input.DefinedVariable;
					if (definedVariable != null)
					{
						Protocol protocolForInput = GetProtocol();

						if (protocolForInput != null)
						{
							// if the input is complete, set the variable on the protocol
							if (_complete)
							{
								protocolForInput.VariableValue[definedVariable] = inputValue.ToString();
							}
							// if the input is incomplete, set the value to null on the protocol
							else
							{
								protocolForInput.VariableValue[definedVariable] = null;
							}
						}
					}
				}

				if (_correct)
				{
					NavigateOrDelay(NavigationOnCorrect, CorrectDelay);
				}
				else
				{
					NavigateOrDelay(NavigationOnIncorrect, IncorrectDelay);
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Input"/> is valid. A valid <see cref="Input"/> 
		/// is one that is complete, one that has been viewed but is not required, or one that isn't 
		/// displayed. It is an <see cref="Input"/> in a state that should not prevent the user from 
		/// proceeding through an <see cref="Input"/> request.
		/// </summary>
		/// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
		[JsonIgnore]
		public bool Valid
		{
			get
			{
				return (_complete && (_correct || !RequireCorrectValue)) || (_viewed && !_required) || !Display;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Sensus.UI.Inputs.Input"/> should be stored in the <see cref="LocalDataStore"/>.
		/// </summary>
		/// <value><c>true</c> if store; otherwise, <c>false</c>.</value>
		public virtual bool Store => true;

		public double? Latitude
		{
			get { return _latitude; }
			set { _latitude = value; }
		}

		public double? Longitude
		{
			get { return _longitude; }
			set { _longitude = value; }
		}

		public DateTimeOffset? LocationUpdateTimestamp
		{
			get
			{
				return _locationUpdateTimestamp;
			}
			set
			{
				_locationUpdateTimestamp = value;
			}
		}

		[JsonIgnore]
		public abstract bool Enabled { get; set; }

		[JsonIgnore]
		public abstract string DefaultName { get; }

		/// <summary>
		/// Whether or not a valid value is required for this input. Also see <see cref="InputGroup.ForceValidInputs"/>.
		/// </summary>
		/// <value><c>true</c> if required; otherwise, <c>false</c>.</value>
		[OnOffUiProperty(null, true, 5)]
		public bool Required
		{
			get
			{
				return _required;
			}
			set
			{
				if (value != _required)
				{
					_required = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Caption)));
				}
			}
		}

		public bool Viewed
		{
			get
			{
				return _viewed;
			}
			set
			{
				_viewed = value;
			}
		}

		[JsonIgnore]
		public DateTimeOffset? CompletionTimestamp
		{
			get
			{
				return _completionTimestamp;
			}
		}

		public List<InputDisplayCondition> DisplayConditions
		{
			get
			{
				return _displayConditions;
			}
		}

		public Color? BackgroundColor
		{
			get
			{
				return _backgroundColor;
			}
			set
			{
				_backgroundColor = value;
			}
		}

		public Thickness? Padding
		{
			get
			{
				return _padding;
			}
			set
			{
				_padding = value;
			}
		}

		public bool Frame
		{
			get
			{
				return _frame;
			}
			set
			{
				_frame = value;
			}
		}

		public List<InputCompletionRecord> CompletionRecords
		{
			get
			{
				return _completionRecords;
			}
		}

		/// <summary>
		/// Whether or not to record a trace of all input values from the first to the final.
		/// </summary>
		/// <value><c>true</c> if store completion records; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Store Completion Records:", true, 6)]
		public bool StoreCompletionRecords
		{
			get; set;
		}

		[JsonIgnore]
		public bool Display
		{
			get
			{
				List<InputDisplayCondition> conjuncts = _displayConditions.Where(displayCondition => displayCondition.Conjunctive).ToList();
				if (conjuncts.Count > 0 && conjuncts.Any(displayCondition => !displayCondition.Satisfied))
				{
					return false;
				}

				List<InputDisplayCondition> disjuncts = _displayConditions.Where(displayCondition => !displayCondition.Conjunctive).ToList();
				if (disjuncts.Count > 0 && disjuncts.All(displayCondition => !displayCondition.Satisfied))
				{
					return false;
				}

				return true;
			}
		}

		public DateTimeOffset? SubmissionTimestamp
		{
			get
			{
				return _submissionTimestamp;
			}

			set
			{
				_submissionTimestamp = value;
			}
		}

		[JsonIgnore]
		public string Caption
		{
			get
			{
				return _name + (_name == DefaultName ? "" : " -- " + DefaultName) + (_required ? "*" : "");
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Datum"/> that triggered the deployment of this <see cref="Input"/>. This
		/// is what will be used when formatting placeholder text in the input <see cref="LabelText"/>.
		/// </summary>
		/// <value>The triggering datum.</value>
		[JsonIgnore]
		public Datum TriggeringDatum { get; set; }

		protected virtual void SetScore(float score)
		{
			Attempts += 1;

			if (Attempts == 1 && ScoreMethod == ScoreMethods.First)
			{
				Score = score;
			}
			else if (ScoreMethod == ScoreMethods.Last)
			{
				Score = score;
			}
			else if (ScoreMethod == ScoreMethods.Maximum && (score > Score))
			{
				Score = score;
			}
			else if (ScoreMethod == ScoreMethods.Average)
			{
				Score = ((Score * (Attempts - 1)) + score) / Attempts;
			}
		}

		protected virtual bool IsCorrect(object deserializedValue)
		{
			if (CorrectValue != null)
			{
				if (CorrectValue is string stringValue && stringValue.StartsWith("=") && (stringValue.StartsWith("==") == false))
				{
					Protocol protocol = GetProtocol();

					if (protocol.VariableValue.TryGetValue(stringValue.Substring(1), out string value) && CorrectValue.ToString() == value)
					{
						return true;
					}

					return false;
				}
				else if (CorrectValue.ToString() == Value.ToString() || CorrectValue.ToString() == deserializedValue?.ToString())
				{
					return true;
				}

				return false;
			}

			return true;
		}

		protected virtual void NavigateOrDelay(NavigationResult navigationResult, int delay)
		{
			if (InputGroupPage != null)
			{
				if (delay > 0)
				{
					_delayTimer?.Dispose();

					_delayTimer = new Timer(delay) { AutoReset = false };

					_view.IsEnabled = false;

					_delayTimer.Elapsed += (o, s) =>
					{
						_view.IsEnabled = true;

						if (navigationResult != NavigationResult.None)
						{
							InputGroupPage.Navigate(this, navigationResult);
						}

						InputGroupPage.SetNavigationVisibility(this);
					};

					_delayTimer.Start();
				}
				else
				{
					if (navigationResult != NavigationResult.None)
					{
						InputGroupPage.Navigate(this, navigationResult);
					}

					InputGroupPage.SetNavigationVisibility(this);
				}
			}
		}

		public Input()
		{
			_name = DefaultName;
			_id = Guid.NewGuid().ToString();
			_displayNumber = true;
			_complete = false;
			_required = true;
			_viewed = false;
			_completionTimestamp = null;
			_labelFontSize = 20;
			_displayConditions = new List<InputDisplayCondition>();
			_backgroundColor = null;
			_padding = null;
			_frame = true;
			_completionRecords = new List<InputCompletionRecord>();
			_submissionTimestamp = null;

			StoreCompletionRecords = true;
		}

		public Input(string labelText)
			: this()
		{
			_labelText = labelText;
		}

		public Input(string labelText, int labelFontSize)
			: this(labelText)
		{
			_labelFontSize = labelFontSize;
		}

		public Input(string labelText, string name)
			: this(labelText)
		{
			_name = name;
		}

		protected Label CreateLabel(int index)
		{
			return new Label
			{
				Text = GetLabelText(index),
				FontSize = _labelFontSize

				// set the style ID on the label so that we can retrieve it when UI testing
#if UI_TESTING
                , StyleId = Name + " Label"
#endif
			};
		}

		protected string GetLabelText(int index)
		{
			if (string.IsNullOrWhiteSpace(_labelText))
			{
				return "";
			}
			else
			{
				string requiredStr = _required ? "*" : "";
				string indexStr = index > 0 && _displayNumber ? index + ") " : "";
				string labelTextStr = _labelText;

				// get the protocol that contains the current input in a script runner (if any)
				Protocol protocolForInput = GetProtocol();

				if (protocolForInput != null)
				{
					// replace all variable references in the input label text with the variables' values
					foreach (string variable in protocolForInput.VariableValue.Keys)
					{
						// get the value for the variable as defined on the protocol
						string variableValue = protocolForInput.VariableValue[variable];

						// if the variable's value has not been defined, then just use the variable name as a fallback.
						if (variableValue == null)
						{
							variableValue = variable;
						}

						// replace variable references with its value
						labelTextStr = labelTextStr.Replace("{" + variable + "}", variableValue);
					}
				}

				// if this input is being shown as part of a datum-triggered script, format the label 
				// text of the input to replace any {0} references with the triggering datum's placeholder
				// value.
				if (TriggeringDatum != null)
				{
					labelTextStr = string.Format(labelTextStr, TriggeringDatum.StringPlaceholderValue.ToString().ToLower());
				}

				return requiredStr + indexStr + labelTextStr;
			}
		}

		/// <summary>
		/// Gets the <see cref="Protocol"/> containing the current input.
		/// </summary>
		/// <returns>The <see cref="Protocol"/>.</returns>
		private Protocol GetProtocol()
		{
			try
			{
				return SensusServiceHelper.Get().RegisteredProtocols.SingleOrDefault(protocol => protocol.Probes.OfType<ScriptProbe>()             // get script probes
																								 .Single()                                         // must be only 1 script probe
																								 .ScriptRunners                                    // get runners
																								 .SelectMany(runner => runner.Script.InputGroups)  // get input groups for each runner
																								 .SelectMany(inputGroup => inputGroup.Inputs)      // get inputs for each input group
																								 .Any(input => input.Id == _id));                  // check if any inputs are the current one. must check ids rather than object references, as we make deep copies of inputs when instantiating scripts to run.
			}
			catch (Exception ex)
			{
				// there is only one known situation in which multiple protocols may contain inputs with the same
				// ids:  when the user manually set the protocol id and loads both protocols (one with the original
				// id and one with the new id. when manually setting the protocol id, any script inputs do not receive
				// a new id. this is by design in the use case where authentication servers wish to push out an updated
				// protocol.
				SensusServiceHelper.Get().Logger.Log("Exception while getting protocol for input:  " + ex.Message, LoggingLevel.Normal, GetType());
				return null;
			}
		}

		public virtual View GetView(int index)
		{
			return _view;
		}

		protected virtual void SetView(View value)
		{
			View view = value;

			if (CorrectDelay > 0 || IncorrectDelay > 0 || string.IsNullOrWhiteSpace(CorrectFeedbackMessage) == false || string.IsNullOrWhiteSpace(IncorrectFeedbackMessage) == false)
			{
				_feedbackView = new InputFeedbackView(PROGRESS_INCREMENT, CorrectFeedbackMessage, CorrectDelay, IncorrectFeedbackMessage, IncorrectDelay);

				view = new StackLayout()
				{
					Children = { value, _feedbackView }
				};
			}

			ContentView viewContainer = new ContentView
			{
				Content = view
			};

			if (_backgroundColor != null)
			{
				viewContainer.BackgroundColor = _backgroundColor.GetValueOrDefault();
			}

			if (_padding != null)
			{
				viewContainer.Padding = _padding.GetValueOrDefault();
			}

			_view = viewContainer;
		}

		public void Reset()
		{
			_view = null;
			_complete = false;
			_latitude = null;
			_longitude = null;
			_locationUpdateTimestamp = null;
			_viewed = false;
			_completionTimestamp = null;
			_backgroundColor = null;
			_padding = null;

			_attempts = 0;
			Score = 0;

			_feedbackView?.Reset();
		}

		public virtual void OnDisappearing(NavigationResult result)
		{
			_delayTimer?.Dispose();
		}

		public virtual bool ValueMatches(object conditionValue, bool conjunctive)
		{
			// if either is null, both must be null to be equal
			if (Value == null || conditionValue == null)
			{
				return Value == null && conditionValue == null;
			}
			// if they're of the same type, compare
			else if (Value.GetType().Equals(conditionValue.GetType()))
			{
				return Value.Equals(conditionValue);
			}
			else
			{
				// this should never happen
				SensusException.Report(new Exception("Called Input.ValueMatches with conditionValue of type " + conditionValue.GetType() + ". Comparing with value of type " + Value.GetType() + "."));

				return false;
			}
		}

		public Input Copy(bool newId)
		{
			Input copy = JsonConvert.DeserializeObject<Input>(JsonConvert.SerializeObject(this, SensusServiceHelper.JSON_SERIALIZER_SETTINGS), SensusServiceHelper.JSON_SERIALIZER_SETTINGS);

			copy.Reset();

			// the reset on the previous line only resets the state of the input. it does not assign it a new/unique ID, which all inputs normally require.
			if (newId)
			{
				copy.Id = Guid.NewGuid().ToString();
			}

			return copy;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:Sensus.UI.Inputs.Input"/>. This is needed
		/// when adding display conditions.
		/// </summary>
		/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:Sensus.UI.Inputs.Input"/>.</returns>
		public override string ToString()
		{
			return _name;
		}
	}
}