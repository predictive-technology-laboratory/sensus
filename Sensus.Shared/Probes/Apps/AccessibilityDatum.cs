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

		/// <summary>
		/// The view is enabled.
		/// </summary>
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
		/// <summary>
		/// The index of the current item.
		/// </summary>
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
		/// <summary>
		/// The view is checked.
		/// </summary>
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
		/// <summary>
		/// The type of content change.
		/// </summary>
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
		/// <summary>
		/// The event time.
		/// </summary>
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
		/// <summary>
		/// The accessibility event type.
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
		/// <summary>
		/// The movement granularity.
		/// </summary>
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
		/// <summary>
		/// The view is full screen.
		/// </summary>
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
		/// <summary>
		/// The item count.
		/// </summary>
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
		/// <summary>
		/// The name of the app package that created the event.
		/// </summary>
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
		/// <summary>
		/// The name of the app that created the event.
		/// </summary>
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
		/// <summary>
		/// The extra data attached to the event.
		/// </summary>
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
		/// <summary>
		/// The view contains a password.
		/// </summary>
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
		/// <summary>
		/// The record count.
		/// </summary>
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
		/// <summary>
		/// The starting index.
		/// </summary>
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
		/// <summary>
		/// The the number of items added.
		/// </summary>
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
		/// <summary>
		/// The view text.
		/// </summary>
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
		/// <summary>
		/// The description of content.
		/// </summary>
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
		/// <summary>
		/// The name of the class that created the event.
		/// </summary>
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
		/// <summary>
		/// The previous view text.
		/// </summary>
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
		/// <summary>
		/// The action described by the event.
		/// </summary>
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
		/// <summary>
		/// The window changes described by the event.
		/// </summary>
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
		/// <summary>
		/// The number of items removed.
		/// </summary>
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
		/// <summary>
		/// The maximum horizontal scroll value.
		/// </summary>
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
		/// <summary>
		/// The maximum vertical scroll value.
		/// </summary>
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
		/// <summary>
		/// The scroll vertical change.
		/// </summary>
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
		/// <summary>
		/// The horizontal scroll.
		/// </summary>
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
		/// <summary>
		/// The vertical scroll.
		/// </summary>
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
		/// <summary>
		/// The view is scrollable.
		/// </summary>
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
		/// <summary>
		/// Additional event information.
		/// </summary>
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
		/// <summary>
		/// The horizontal scroll change.
		/// </summary>
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
		/// <summary>
		/// The end index.
		/// </summary>
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
		/// <summary>
		/// The id of the window that created the event.
		/// </summary>
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
