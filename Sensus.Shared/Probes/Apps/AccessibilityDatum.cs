using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Apps
{
	public class AccessibilityDatum : Datum
	{
		public override string DisplayDetail => EventType;

		public override object StringPlaceholderValue => EventType;

		public AccessibilityDatum(bool enabled, int currentItemIndex, bool @checked, string contentChangeTypes, long eventTime, string eventType, string movementGranularity, bool fullScreen, int itemCount, string packageName, object parcelableData, bool password, int recordCount, int fromIndex, int addedCount, string text, string contentDescription, string className, string beforeText, string action, string windowChanges, int removedCount, int maxScrollX, int maxScrollY, int scrollDeltaY, int scrollX, int scrollY, bool scrollable, object source, int scrollDeltaX, int toIndex, int windowId, DateTimeOffset timestamp) : base(timestamp)
		{
			Enabled = enabled;
			CurrentItemIndex = currentItemIndex;
			Checked = @checked;
			ContentChangeTypes = contentChangeTypes;
			EventTime = eventTime;
			EventType = eventType;
			MovementGranularity = movementGranularity;
			FullScreen = fullScreen;
			ItemCount = itemCount;
			PackageName = packageName;
			ParcelableData = parcelableData;
			Password = password;
			RecordCount = recordCount;
			FromIndex = fromIndex;
			AddedCount = addedCount;
			Text = text;
			ContentDescription = contentDescription;
			ClassName = className;
			BeforeText = beforeText;
			Action = action;
			WindowChanges = windowChanges;
			RemovedCount = removedCount;
			MaxScrollX = maxScrollX;
			MaxScrollY = maxScrollY;
			ScrollDeltaY = scrollDeltaY;
			ScrollX = scrollX;
			ScrollY = scrollY;
			Scrollable = scrollable;
			Source = source;
			ScrollDeltaX = scrollDeltaX;
			ToIndex = toIndex;
			WindowId = windowId;
		}

		public bool Enabled { get; set; }
		public int CurrentItemIndex { get; set; }
		public bool Checked { get; set; }
		public string ContentChangeTypes { get; set; }
		public long EventTime { get; set; }
		public string EventType { get; set; }
		public string MovementGranularity { get; set; }
		public bool FullScreen { get; set; }
		public int ItemCount { get; set; }
		public string PackageName { get; set; }
		public object ParcelableData { get; set; }
		public bool Password { get; set; }
		public int RecordCount { get; }
		public int FromIndex { get; set; }
		public int AddedCount { get; set; }
		public string Text { get; }
		public string ContentDescription { get; set; }
		public string ClassName { get; set; }
		public string BeforeText { get; set; }
		public string Action { get; set; }
		public string WindowChanges { get; }
		public int RemovedCount { get; set; }
		public int MaxScrollX { get; set; }
		public int MaxScrollY { get; set; }
		public int ScrollDeltaY { get; set; }
		public int ScrollX { get; set; }
		public int ScrollY { get; set; }
		public bool Scrollable { get; set; }
		public object Source { get; }
		public int ScrollDeltaX { get; set; }
		public int ToIndex { get; set; }
		public int WindowId { get; }
	}
}
