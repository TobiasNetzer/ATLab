using System.Collections.Generic;
using ATLab.Services;

namespace ATLab.Reporting;

/// <summary>A failed step drawn on the board for the PDF test report: SVG of the board and the legend of its roles.</summary>
public sealed record ReportBoardView(int StepNumber, string StepName, IReadOnlyList<BoardLegendItem> Legend, string Svg, bool IsBottom);