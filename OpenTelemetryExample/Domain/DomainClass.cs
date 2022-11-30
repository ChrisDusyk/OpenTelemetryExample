namespace OpenTelemetryExample.Domain
{
	public static class DomainClass
	{
		public static void DoThing(string param)
		{
			using var span = OTel.StartActiveSpan(nameof(DoThing));
			OTel.SetSpanAttribute("param", param);

			Console.WriteLine("This is terrible code");
		}
	}
}
