using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.Extensions;

namespace Uno.UWPSyncGenerator
{
	partial class DocGenerator
	{
		/// <summary>
		/// 
		/// </summary>
		public void WriteWASMReport()
		{
			_sb = new MarkdownStringBuilder();
			AppendGenerationTag();

			var classCount = 0;
			var propertyCount = 0;
			var methodCount = 0;
			var eventCount = 0;
			var classMemberCount = 0;

			using (_sb.Section("WASM parity"))
			{
				_sb.AppendParagraph("This document details the implementation gap between WASM and Android/iOS in Uno. Missing classes are listed first; following that, missing members per-class are listed.");

				_sb.AppendParagraph("Specifically, classes and members are included if they are marked as implemented for both Android and iOS, but not for WASM.");

				_sb.AppendParagraph();

				using (_sb.Section("Missing classes"))
				{
					foreach (var ns in AllGrouped)
					{
						if (ns.None(IsMissingOnWasm))
						{
							continue;
						}

						using (_sb.Table(ns.Key.ToDisplayString()))
						{
							foreach (var type in ns)
							{
								if (IsMissingOnWasm(type))
								{
									classCount++;
									_sb.AppendRow(type.UAPSymbol.ToDisplayString());
								}
							} 
						}

						_sb.AppendParagraph();
					}
				}

				using (_sb.Section("Missing members"))
				{
					foreach (var ns in AllGrouped)
					{
						IDisposable section = null;
						foreach (var type in ns)
						{
							var missingProperties = GetProperties(type).Where(IsMissingOnWasm);
							var missingMethods = GetMethods(type).Where(IsMissingOnWasm);
							var missingEvents = GetEvents(type).Where(IsMissingOnWasm);

							if (missingProperties.None() && missingMethods.None() && missingEvents.None())
							{
								continue;
							}

							section = section ?? _sb.Section(ns.Key.ToDisplayString());
							classMemberCount++;

							using (_sb.Table(type.UAPSymbol.Name))
							{
								foreach (var member in missingProperties)
								{
									_sb.AppendRow(member.UAPSymbol.ToDisplayString());
									propertyCount++;
								}

								foreach (var member in missingMethods)
								{
									_sb.AppendRow(member.UAPSymbol.ToDisplayString());
									methodCount++;
								}
								foreach (var member in missingEvents)
								{
									_sb.AppendRow(member.UAPSymbol.ToDisplayString());
									eventCount++;
								}
							}
						}

						section?.Dispose();
					}
				}

				_sb.AppendParagraph($"Stats: {classCount} missing classes.");

				_sb.AppendParagraph($"{classMemberCount} classes missing {propertyCount} properties, {methodCount} methods, and {eventCount} events.");
			}
		}

		private bool IsMissingOnWasm<T>(PlatformSymbols<T> symbol) where T : ISymbol
			=> symbol.ImplementedFor.HasFlag(ImplementedFor.Mobile) && !symbol.ImplementedFor.HasFlag(ImplementedFor.WASM);
	}
}
