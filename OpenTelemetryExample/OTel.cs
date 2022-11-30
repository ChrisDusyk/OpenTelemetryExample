using OpenTelemetry.Trace;
using System.Diagnostics;

namespace OpenTelemetryExample
{
	internal static class OTel
	{
		private static readonly Tracer _tracer = TracerProvider.Default.GetTracer("open-telemetry-example.instrumentation.custom");

		public static TelemetrySpan StartActiveSpan(string name) =>
			_tracer.StartActiveSpan(name);

		public static Activity? SetSpanAttribute(string key, object value)
		{
			var currentSpan = Activity.Current;
			currentSpan?.SetTag(key, value);
			return currentSpan;
		}

		public static Activity? SetSpanAttributes(IEnumerable<KeyValuePair<string, object>> attributes)
		{
			var currentSpan = Activity.Current;
			foreach (var kv in attributes)
			{
				if (kv.Value != null)
				{
					currentSpan?.SetTag(kv.Key, kv.Value);
				}
			}

			return currentSpan;
		}

		public static TelemetrySpan SetSpanError(Exception ex)
		{
			var currentSpan = Tracer.CurrentSpan;
			currentSpan.SetAttribute("error", true);
			currentSpan.SetAttribute("error.message", ex.Message);
			currentSpan.SetAttribute("error.stack_trace", ex.StackTrace);
			return currentSpan;
		}
	}
}
