﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno;
using System.Diagnostics;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlockMeasureCache
	{
		internal static int MaxMeasureKeyEntries { get; set; } = 500;
		internal static int MaxMeasureSizeKeyEntries { get; set; } = 50;

		public static bool IsEnabled { get; set; } = true;

		private static Stopwatch _timer = Stopwatch.StartNew();

		private Dictionary<MeasureKey, MeasureEntry> _entries = new Dictionary<MeasureKey, MeasureEntry>(new MeasureKey.Comparer());
		private LinkedList<MeasureKey> _queue = new LinkedList<MeasureKey>();


		public Size? FindMeasuredSize(TextBlock source, Size availableSize)
		{
			var key = new MeasureKey(source);

			if (IsEnabled && _entries.TryGetValue(key, out var entry))
			{
				var measuredSize = entry.FindMeasuredSize(key, availableSize);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					// {0} is used to avoid parsing errors caused by formatting a "{}" in the text
					this.Log().LogDebug("{0}", $"TextMeasure-cached [{source.Text} / {source.TextWrapping} / {source.MaxLines}]: {availableSize} -> {measuredSize}");
				}

				return measuredSize;

			}

			return null;
		}

		public void CacheMeasure(TextBlock source, Size availableSize, Size measuredSize)
		{
			var key = new MeasureKey(source);

			if (!_entries.TryGetValue(key, out var entry))
			{
				Scavenge();
				var node = _queue.AddLast(key);
				_entries[key] = entry = new MeasureEntry(node);
			}
			else
			{
				if (_queue.Count != 0 && _queue.Last.Value != key)
				{
					_queue.Remove(entry.ListNode);
					_queue.AddLast(entry.ListNode);
				}
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				// {0} is used to avoid parsing errors caused by formatting a "{}" in the text
				this.Log().LogDebug("{0}", $"TextMeasure-new [{source.Text} / {source.TextWrapping} / {source.MaxLines}]: {availableSize} -> {measuredSize}");
			}

			entry.CacheMeasure(availableSize, measuredSize);
		}

		private void Scavenge()
		{
			if (_queue.Count >= MaxMeasureKeyEntries)
			{
				_entries.Remove(_queue.First.Value);
				_queue.RemoveFirst();
			}
		}
	}
}