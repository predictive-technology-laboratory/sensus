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

namespace Sensus.Probes.Apps
{
	public class AccessibilityDatum : Datum
	{
		private bool _enabled;
		private int _currentItemIndex;
		private bool _checked;
		private string _contentChangeTypes;
		private long _eventTime;
		private string _eventType;
		private string _movementGranularity;
		private bool _fullScreen;
		private int _itemCount;
		private string _packageName;
		private string _applicationName;
		private object _parcelableData;
		private bool _password;
		private int _recordCount;
		private int _fromIndex;
		private int _addedCount;
		private string _text;
		private string _contentDescription;
		private string _className;
		private string _beforeText;
		private string _action;
		private string _windowChanges;
		private int _removedCount;
		private int _maxScrollX;
		private int _maxScrollY;
		private int _scrollDeltaY;
		private int _scrollX;
		private int _scrollY;
		private bool _scrollable;
		private object _source;
		private int _scrollDeltaX;
		private int _toIndex;
		private int _windowId;

		public override string DisplayDetail
		{
			get
			{
				return EventType;
			}
		}

		public override object StringPlaceholderValue
		{
			get
			{
				return EventType;
			}
		}

		public AccessibilityDatum(bool enabled, int currentItemIndex, bool @checked, string contentChangeTypes, long eventTime, string eventType, string movementGranularity, bool fullScreen, int itemCount, string packageName, string applicationName, object parcelableData, bool password, int recordCount, int fromIndex, int addedCount, string text, string contentDescription, string className, string beforeText, string action, string windowChanges, int removedCount, int maxScrollX, int maxScrollY, int scrollDeltaY, int scrollX, int scrollY, bool scrollable, object source, int scrollDeltaX, int toIndex, int windowId, DateTimeOffset timestamp) : base(timestamp)
		{
			_enabled = enabled;
			_currentItemIndex = currentItemIndex;
			_checked = @checked;
			_contentChangeTypes = contentChangeTypes;
			_eventTime = eventTime;
			_eventType = eventType;
			_movementGranularity = movementGranularity;
			_fullScreen = fullScreen;
			_itemCount = itemCount;
			_packageName = packageName;
			_parcelableData = parcelableData;
			_password = password;
			_recordCount = recordCount;
			_fromIndex = fromIndex;
			_addedCount = addedCount;
			_text = text;
			_contentDescription = contentDescription;
			_className = className;
			_beforeText = beforeText;
			_action = action;
			_windowChanges = windowChanges;
			_removedCount = removedCount;
			_maxScrollX = maxScrollX;
			_maxScrollY = maxScrollY;
			_scrollDeltaY = scrollDeltaY;
			_scrollX = scrollX;
			_scrollY = scrollY;
			_scrollable = scrollable;
			_source = source;
			_scrollDeltaX = scrollDeltaX;
			_toIndex = toIndex;
			_windowId = windowId;
		}

		public bool Enabled
		{
			get
			{
				return _enabled;
			}
			set
			{
				_enabled = value;
			}
		}
		public int CurrentItemIndex
		{
			get
			{
				return _currentItemIndex;
			}
			set
			{
				_currentItemIndex = value;
			}
		}
		public bool Checked
		{
			get
			{
				return _checked;
			}
			set
			{
				_checked = value;
			}
		}
		public string ContentChangeTypes
		{
			get
			{
				return _contentChangeTypes;
			}
			set
			{
				_contentChangeTypes = value;
			}
		}
		public long EventTime
		{
			get
			{
				return _eventTime;
			}
			set
			{
				_eventTime = value;
			}
		}
		public string EventType
		{
			get
			{
				return _eventType;
			}
			set
			{
				_eventType = value;
			}
		}
		public string MovementGranularity
		{
			get
			{
				return _movementGranularity;
			}
			set
			{
				_movementGranularity = value;
			}
		}
		public bool FullScreen
		{
			get
			{
				return _fullScreen;
			}
			set
			{
				_fullScreen = value;
			}
		}
		public int ItemCount
		{
			get
			{
				return _itemCount;
			}
			set
			{
				_itemCount = value;
			}
		}
		public string PackageName
		{
			get
			{
				return _packageName;
			}
			set
			{
				_packageName = value;
			}
		}
		public string ApplicationName
		{
			get
			{
				return _applicationName;
			}
			set
			{
				_applicationName = value;
			}
		}
		public object ParcelableData
		{
			get
			{
				return _parcelableData;
			}
			set
			{
				_parcelableData = value;
			}
		}
		public bool Password
		{
			get
			{
				return _password;
			}
			set
			{
				_password = value;
			}
		}
		public int RecordCount
		{
			get
			{
				return _recordCount;
			}
			set
			{
				_recordCount = value;
			}
		}
		public int FromIndex
		{
			get
			{
				return _fromIndex;
			}
			set
			{
				_fromIndex = value;
			}
		}
		public int AddedCount
		{
			get
			{
				return _addedCount;
			}
			set
			{
				_addedCount = value;
			}
		}
		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
				_text = value;
			}
		}
		public string ContentDescription
		{
			get
			{
				return _contentDescription;
			}
			set
			{
				_contentDescription = value;
			}
		}
		public string ClassName
		{
			get
			{
				return _className;
			}
			set
			{
				_className = value;
			}
		}
		public string BeforeText
		{
			get
			{
				return _beforeText;
			}
			set
			{
				_beforeText = value;
			}
		}
		public string Action
		{
			get
			{
				return _action;
			}
			set
			{
				_action = value;
			}
		}
		public string WindowChanges
		{
			get
			{
				return _windowChanges;
			}
			set
			{
				_windowChanges = value;
			}
		}
		public int RemovedCount
		{
			get
			{
				return _removedCount;
			}
			set
			{
				_removedCount = value;
			}
		}
		public int MaxScrollX
		{
			get
			{
				return _maxScrollX;
			}
			set
			{
				_maxScrollX = value;
			}
		}
		public int MaxScrollY
		{
			get
			{
				return _maxScrollY;
			}
			set
			{
				_maxScrollY = value;
			}
		}
		public int ScrollDeltaY
		{
			get
			{
				return _scrollDeltaY;
			}
			set
			{
				_scrollDeltaY = value;
			}
		}
		public int ScrollX
		{
			get
			{
				return _scrollX;
			}
			set
			{
				_scrollX = value;
			}
		}
		public int ScrollY
		{
			get
			{
				return _scrollY;
			}
			set
			{
				_scrollY = value;
			}
		}
		public bool Scrollable
		{
			get
			{
				return _scrollable;
			}
			set
			{
				_scrollable = value;
			}
		}
		public object Source
		{
			get
			{
				return _source;
			}
			set
			{
				_source = value;
			}
		}
		public int ScrollDeltaX
		{
			get
			{
				return _scrollDeltaX;
			}
			set
			{
				_scrollDeltaX = value;
			}
		}
		public int ToIndex
		{
			get
			{
				return _toIndex;
			}
			set
			{
				_toIndex = value;
			}
		}
		public int WindowId
		{
			get
			{
				return _windowId;
			}
			set
			{
				_windowId = value;
			}
		}

		public override string ToString()
		{
			return base.ToString() + Environment.NewLine + $"{_packageName}: {_eventType}";
		}
	}
}
