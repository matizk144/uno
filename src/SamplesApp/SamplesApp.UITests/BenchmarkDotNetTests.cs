using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Runtime
{
	[TestFixture]
	public partial class BenchmarkDotNetTests : SampleControlUITestBase
	{
		private const string PendingTestsText = "Pending...";
		private const string BenchmarkOutputPath = "UNO_UITEST_BENCHMARKS_PATH";
		private readonly TimeSpan TestRunTimeout = TimeSpan.FromMinutes(2);

		[Test]
		[AutoRetry(tryCount: 1)]
		[Timeout(300000)] // Adjust this timeout based on average test run duration
		public async Task RunRuntimeTests()
		{
			Run("Benchmarks.Shared.Controls.BenchmarkDotNetTestsPage");

			IAppQuery AllQuery(IAppQuery query)
				// .All() is not yet supported for wasm.
				=> AppInitializer.GetLocalPlatform() == Platform.Browser ? query : query.All();

			var runButton = new QueryEx(q => AllQuery(q).Marked("runButton"));
			var runStatus = new QueryEx(q => AllQuery(q).All().Marked("runStatus"));
			var runCount = new QueryEx(q => AllQuery(q).All().Marked("runCount"));
			var benchmarkControl = new QueryEx(q => q.All().Marked("benchmarkControl"));

			bool IsTestExecutionDone()
				=> runStatus.GetDependencyPropertyValue("Text")?.ToString().Equals("Finished", StringComparison.OrdinalIgnoreCase) ?? false;

			_app.WaitForElement(runButton);

			_app.FastTap(runButton);

			var lastChange = DateTimeOffset.Now;
			var lastValue = "";

			while(DateTimeOffset.Now - lastChange < TestRunTimeout)
			{
				var newValue = runCount.GetDependencyPropertyValue("Text")?.ToString();

				if (lastValue != newValue)
				{	
					lastChange = DateTimeOffset.Now;
				}

				await Task.Delay(TimeSpan.FromSeconds(.5));

				if (IsTestExecutionDone())
				{
					break;
				}
			}

			if (!IsTestExecutionDone())
			{
				Assert.Fail("A test run timed out");
			}

			var base64 = benchmarkControl.GetDependencyPropertyValue<string>("ResultsAsBase64");

			var file = Path.GetTempFileName();
			File.WriteAllBytes(file, Convert.FromBase64String(base64));

			if (Environment.GetEnvironmentVariable(BenchmarkOutputPath) is { } path)
			{

				ZipFile.ExtractToDirectory(file, path);
			}
			else
			{
				Console.WriteLine($"The environment variable {BenchmarkOutputPath} is not defined, skipping file system extraction");
			}

			TestContext.AddTestAttachment(file, "benchmark-results.zip");

			TakeScreenshot("Runtime Tests Results",	ignoreInSnapshotCompare: true);
		}
	}
}
