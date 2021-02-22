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

using Newtonsoft.Json;
using Sensus.UI.UiProperties;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ScoreInput : Input, IVariableDefiningInput
	{
		private string _definedVariable;
		private IEnumerable<Input> _inputs = new List<Input>();
		//private float _maxScore;
		private Label _scoreLabel;

		public override object Value
		{
			get
			{
				return _score;
			}
		}

		public override bool Enabled { get; set; }

		public override string DefaultName => "Score";

		[EntryStringUiProperty("Define Variable:", true, 2, false)]
		public string DefinedVariable
		{
			get
			{
				return _definedVariable;
			}
			set
			{
				_definedVariable = value?.Trim();
			}
		}

		[ListUiProperty("Score Method:", true, 21, new object[] { ScoreMethods.Total, ScoreMethods.Average }, false)]
		public override ScoreMethods ScoreMethod { get; set; } = ScoreMethods.Total;

		[HiddenUiProperty]
		public override float CorrectScore { get; set; }

		[HiddenUiProperty]
		public override object CorrectValue { get; set; }

		[HiddenUiProperty]
		public override int? Retries => 0;

		[JsonIgnore] // TODO: determine if this needs to be serialized or not
		public IEnumerable<Input> Inputs
		{
			get
			{
				return _inputs;
			}
			set
			{
				// remove the ScoreChanged event from each of the original inputs.
				foreach (Input input in _inputs)
				{
					input.PropertyChanged -= ScoreChanged;
				}

				if (string.IsNullOrWhiteSpace(ScoreGroup))
				{
					_inputs = value.OfType<ScoreInput>().Where(x => string.IsNullOrWhiteSpace(x.ScoreGroup) == false);
				}
				else
				{
					_inputs = value.Where(x => x is ScoreInput == false);
				}

				foreach (Input input in _inputs)
				{
					input.PropertyChanged += ScoreChanged;
				}

				SetScore();
			}
		}

		public void SetScore()
		{
			if (ScoreMethod == ScoreMethods.Total)
			{
				Score = _inputs.Sum(x => x.Score);
				CorrectScore = _inputs.Sum(x => x.CorrectScore);
			}
			else if (ScoreMethod == ScoreMethods.Average)
			{
				Score = _inputs.Average(x => x.Score);
				CorrectScore = _inputs.Average(x => x.CorrectScore);
			}

			if (Score == CorrectScore || (Score > 0 && RequireCorrectValue == false))
			{
				Complete = true;
			}

			// if the label has been created, update its text
			UpdateScoreText();
		}

		protected override void SetScore(float score)
		{
			// The ScoreInput does not need to set it's score when set as complete
		}

		private void ScoreChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Score))
			{
				SetScore();
			}
		}

		private void UpdateScoreText()
		{
			if (_scoreLabel != null)
			{
				_scoreLabel.Text = $"{_score}/{CorrectScore}";
			}
		}

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				_scoreLabel = CreateLabel(-1);

				UpdateScoreText();

				base.SetView(_scoreLabel);
			}

			return base.GetView(index);
		}
	}
}
