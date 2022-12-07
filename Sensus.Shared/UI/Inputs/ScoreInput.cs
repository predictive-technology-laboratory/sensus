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

using Microcharts;
using Microcharts.Forms;
using Newtonsoft.Json;
using Sensus.UI.UiProperties;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ScoreInput : Input, IVariableDefiningInput
	{
		private string _definedVariable;
		private List<Input> _inputs;
		private Label _label;
		private Span _scoreSpan;
		private Span _maxScoreSpan;
		private DonutChart _chart;

		private static SKColor _scoreColor;
		private static SKColor _scoreRemainingColor;
		private static Color _scoreLabelColor;
		private static Color _scoreLabelDividerColor;
		private static Color _maxScoreLabelColor;

		static ScoreInput()
		{
			if (Application.Current.Resources.TryGetValue("ScoreColor", out object scoreColor))
			{
				_scoreColor = SKColor.Parse(((Color)scoreColor).ToHex());
			}
			else
			{
				_scoreColor = SKColor.Parse(Color.Accent.ToHex());
			}

			if (Application.Current.Resources.TryGetValue("ScoreRemainingColor", out object scoreRemainingColor))
			{
				_scoreRemainingColor = SKColor.Parse(((Color)scoreRemainingColor).ToHex());
			}
			else
			{
				_scoreRemainingColor = SKColor.Empty;
			}

			if (Application.Current.Resources.TryGetValue("ScoreLabelColor", out object scoreLabelColor))
			{
				_scoreLabelColor = (Color)scoreLabelColor;
			}
			else
			{
				_scoreLabelColor = Color.Default;
			}

			if (Application.Current.Resources.TryGetValue("ScoreLabelDividerColor", out object scoreLabelDividerColor))
			{
				_scoreLabelDividerColor = (Color)scoreLabelDividerColor;
			}
			else
			{
				_scoreLabelDividerColor = _scoreLabelColor;
			}

			if (Application.Current.Resources.TryGetValue("ScoreRemainingLabelColor", out object scoreRemainingLabelColor))
			{
				_maxScoreLabelColor = (Color)scoreRemainingLabelColor;
			}
			else
			{
				_maxScoreLabelColor = _scoreLabelColor;
			}
		}

		public ScoreInput()
		{
			_inputs = new List<Input>();

			Required = false;
		}

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

		[OnOffUiProperty("Show Score as Percent:", true, 22)]
		public bool ShowScoreAsPercent { get; set; }

		[HiddenUiProperty]
		public override float CorrectScore { get; set; }

		[HiddenUiProperty]
		public override object CorrectValue { get; set; }

		[HiddenUiProperty]
		public override int? Retries => 0;

		[JsonIgnore]
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

				if (value != null)
				{
					if (string.IsNullOrWhiteSpace(ScoreGroup))
					{
						_inputs = value.Where(x => x is ScoreInput && string.IsNullOrWhiteSpace(x.ScoreGroup) == false).ToList();
					}
					else
					{
						_inputs = value.Where(x => x is not ScoreInput).ToList();
					}
				}
				else
				{
					_inputs.Clear();
				}

				foreach (Input input in _inputs)
				{
					input.PropertyChanged += ScoreChanged;
				}

				SetScore();
			}
		}

		public void ClearInputs()
		{
			foreach (Input input in _inputs)
			{
				input.PropertyChanged -= ScoreChanged;
			}

			_inputs.Clear();
		}

		public void AddInput(Input input)
		{
			input.PropertyChanged -= ScoreChanged;

			if (string.IsNullOrWhiteSpace(ScoreGroup))
			{
				if (input is ScoreInput)
				{
					_inputs.Add(input);
				}
			}
			else if (ScoreGroup == input.ScoreGroup)
			{
				_inputs.Add(input);
			}

			input.PropertyChanged += ScoreChanged;

			SetScore();
		}

		private IEnumerable<ChartEntry> GetChartEntries()
		{
			return new[] { new ChartEntry(Score) { Color = _scoreColor }, new ChartEntry(CorrectScore - Score) { Color = _scoreRemainingColor } };
		}

		public string ScoreText
		{
			get
			{
				if (ShowScoreAsPercent && CorrectScore > 0)
				{
					return $"{Score * 100 / CorrectScore:0.##}";
				}
				else
				{
					return Score.ToString();
				}
			}
		}

		private float _scoreOffset;
		private float _correctScoreOffset;

		public void OffsetScore(float scoreOffset, float correctScoreOffset)
		{
			_scoreOffset = scoreOffset;
			_correctScoreOffset = correctScoreOffset;
		}

		public void SetScore()
		{
			if (ScoreMethod == ScoreMethods.Total)
			{
				Score = _scoreOffset + _inputs.Sum(x => x.Score);
				CorrectScore = _correctScoreOffset + _inputs.Sum(x => x.CorrectScore);
			}
			else if (ScoreMethod == ScoreMethods.Average)
			{
				Score = (_scoreOffset + _inputs.Average(x => x.Score)) / 2;
				CorrectScore = (_correctScoreOffset + _inputs.Average(x => x.CorrectScore)) / 2;
			}

			if (Score == CorrectScore || (Score > 0 && RequireCorrectValue == false))
			{
				Complete = true;
			}

			if (_scoreSpan != null)
			{
				_scoreSpan.Text = ScoreText;
			}

			if (_maxScoreSpan != null)
			{
				_maxScoreSpan.Text = CorrectScore.ToString();
			}

			if (_chart != null)
			{
				_chart.Entries = GetChartEntries();
			}
		}

		protected override void SetScore(float score)
		{
			// The ScoreInput does not need to set its score when set as complete
		}

		private void ScoreChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Score))
			{
				SetScore();
			}
		}

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				_label = CreateLabel(index);

				FormattedString scoreString = new FormattedString();

				if (ShowScoreAsPercent)
				{
					_scoreSpan = new Span()
					{
						Text = ScoreText,
						ForegroundColor = _scoreLabelColor,
						FontSize = 50
					};

					scoreString.Spans.Add(_scoreSpan);
					scoreString.Spans.Add(new Span() { Text = "%", FontSize = 25, ForegroundColor = _scoreLabelDividerColor });
				}
				else
				{
					_scoreSpan = new Span()
					{
						Text = Score.ToString(),
						ForegroundColor = _scoreLabelColor,
						FontSize = 50,
					};

					_maxScoreSpan = new Span()
					{
						Text = CorrectScore.ToString(),
						ForegroundColor = _maxScoreLabelColor,
						FontSize = 25,
					};

					scoreString.Spans.Add(_scoreSpan);
					scoreString.Spans.Add(new Span() { Text = "\n/", FontSize = 25, ForegroundColor = _scoreLabelDividerColor });
					scoreString.Spans.Add(_maxScoreSpan);
				}

				Label scoreLabel = new Label()
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					FormattedText = scoreString
				};

				_chart = new DonutChart()
				{
					HoleRadius = .6f,
					BackgroundColor = SKColor.Empty,
					Entries = GetChartEntries()
				};

				ChartView chartView = new ChartView()
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					Chart = _chart,
				};

				chartView.SizeChanged += (s, e) =>
				{
					chartView.HeightRequest = chartView.Width;
				};

				AbsoluteLayout.SetLayoutBounds(scoreLabel, new Rectangle(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutFlags(scoreLabel, AbsoluteLayoutFlags.All);

				AbsoluteLayout.SetLayoutBounds(chartView, new Rectangle(0, 0, 1, AbsoluteLayout.AutoSize));
				AbsoluteLayout.SetLayoutFlags(chartView, AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.WidthProportional);

				AbsoluteLayout layout = new AbsoluteLayout
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					Children = { _label, chartView, scoreLabel }
				};

				base.SetView(layout);
			}
			else
			{
				_label.Text = GetLabelText(index);
			}

			return base.GetView(index);
		}
	}
}
