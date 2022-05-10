//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.TraceProcessorHelpers
{
	internal class ConsoleSymbolLoadingProgress : IProgress<SymbolLoadingProgress>
	{
		public static ConsoleSymbolLoadingProgress Instance
		{
			get;
		} = new ConsoleSymbolLoadingProgress();


		private ConsoleSymbolLoadingProgress()
		{
		}

		public void Report(SymbolLoadingProgress progress)
		{
			Console.WriteLine("{0:N1}% ({1} of {3}; {2} loaded)", (decimal)progress.ImagesProcessed / (decimal)progress.ImagesTotal * 100m, progress.ImagesProcessed, progress.ImagesLoaded, progress.ImagesTotal);
		}
	}

}
