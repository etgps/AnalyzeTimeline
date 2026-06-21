using System.Net;
using System.Text;
using System.Text.Json;
using AnalyzeTimeline.Domain;

namespace AnalyzeTimeline.Application;

public sealed class ReportHtmlGenerator : IReportHtmlGenerator
{
    public string Generate(TimelineAnalysisResult result)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"ja\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>Timeline Visit Report</title>");
        html.AppendLine("<script src=\"https://www.gstatic.com/charts/loader.js\"></script>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Inter,'Segoe UI',sans-serif;margin:0;background:#f7f8fb;color:#20242c}main{max-width:1120px;margin:auto;padding:32px}h1{font-size:28px;margin:0 0 8px}h2{font-size:20px;margin:28px 0 12px}.section-header{align-items:center;display:flex;gap:12px;justify-content:space-between;margin-top:28px}.section-header h2{margin:0}.year-select{align-items:center;display:flex;gap:8px;color:#4b5563;font-size:14px}.year-select select{border:1px solid #cbd5e1;border-radius:6px;font:inherit;min-height:36px;padding:0 10px;background:#fff}.summary{display:flex;gap:12px;flex-wrap:wrap;margin:20px 0}.metric{background:white;border:1px solid #e3e7ef;border-radius:8px;padding:14px 16px;min-width:180px}.metric b{display:block;font-size:24px}.panel{background:white;border:1px solid #e3e7ef;border-radius:8px;padding:18px;margin:16px 0}.geo-map{width:100%;height:420px}.geo-map.japan{height:520px}.map-note{color:#687184;font-size:13px;margin:8px 0 0}.map-error{align-items:center;color:#687184;display:flex;height:100%;justify-content:center;text-align:center}table{width:100%;border-collapse:collapse;background:white}th,td{padding:10px 12px;border-bottom:1px solid #e8ebf1;text-align:left}th{background:#eef2f7;font-size:13px}tr:hover td{background:#fafbff}tr.hidden{display:none}@media(max-width:640px){main{padding:18px}.summary{display:grid}.metric{min-width:0}.geo-map{height:320px}.geo-map.japan{height:420px}.section-header{align-items:flex-start;flex-direction:column}}");
        html.AppendLine("</style>");
        html.AppendLine("</head><body><main>");
        html.AppendLine("<h1>Timeline Visit Report</h1>");
        html.AppendLine("<p>Google Timeline export から抽出した訪問地域の年別・月別集計です。</p>");
        html.AppendLine("<section class=\"summary\">");
        html.AppendLine($"<div class=\"metric\"><span>抽出地点</span><b>{result.ParsedLocationCount}</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>分類済み地点</span><b>{result.ClassifiedLocationCount}</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>年別訪問先</span><b>{result.YearlyVisits.Count}</b></div>");
        html.AppendLine($"<div class=\"metric\"><span>月別訪問先</span><b>{result.MonthlyVisits.Count}</b></div>");
        html.AppendLine("</section>");
        AppendYearlySection(html, result.YearlyVisits);
        AppendMonthlySection(html, result.MonthlyVisits);
        AppendChartScript(html, result);
        html.AppendLine("</main></body></html>");
        return html.ToString();
    }

    private static void AppendYearlySection(StringBuilder html, IReadOnlyList<VisitRegion> visits)
    {
        html.AppendLine("<div class=\"section-header\"><h2>年別訪問先</h2></div>");
        AppendMaps(html, "yearly");
        AppendTable(html, visits, includeYearData: false);
    }

    private static void AppendMonthlySection(StringBuilder html, IReadOnlyList<VisitRegion> visits)
    {
        var years = visits
            .Select(visit => visit.LastVisitedOn.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToList();

        html.AppendLine("<div class=\"section-header\"><h2>月別訪問先</h2>");
        html.AppendLine("<label class=\"year-select\" for=\"monthly-year-select\">年度<select id=\"monthly-year-select\">");
        foreach (var year in years)
        {
            html.AppendLine($"<option value=\"{year}\">{year}</option>");
        }

        html.AppendLine("</select></label></div>");
        AppendMaps(html, "monthly");
        AppendTable(html, visits, includeYearData: true);
    }

    private static void AppendMaps(StringBuilder html, string sectionId)
    {
        html.AppendLine($"<div class=\"panel\"><h3>世界の白地図</h3><div id=\"{sectionId}-world-map\" class=\"geo-map\" role=\"img\" aria-label=\"世界の訪問地域地図\"></div><p class=\"map-note\">Google Charts GeoChart を使用して表示します。</p></div>");
        html.AppendLine($"<div class=\"panel\"><h3>日本の白地図</h3><div id=\"{sectionId}-japan-map\" class=\"geo-map japan\" role=\"img\" aria-label=\"日本の訪問都道府県地図\"></div><p class=\"map-note\">Google Charts GeoChart を使用して表示します。</p></div>");
    }

    private static void AppendTable(StringBuilder html, IReadOnlyList<VisitRegion> visits, bool includeYearData)
    {
        html.AppendLine("<div class=\"panel\"><table><thead><tr><th>分類</th><th>訪問先</th><th>最終訪問</th><th>地点数</th></tr></thead><tbody>");
        foreach (var visit in visits)
        {
            var yearData = includeYearData ? $" data-year=\"{visit.LastVisitedOn.Year}\"" : string.Empty;
            html.AppendLine($"<tr{yearData}>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(visit.Group)}</td>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(visit.Name)}</td>");
            html.AppendLine($"<td>{FormatDate(visit)}</td>");
            html.AppendLine($"<td>{visit.VisitCount}</td>");
            html.AppendLine("</tr>");
        }

        if (visits.Count == 0)
        {
            html.AppendLine("<tr><td colspan=\"4\">分類できる訪問先がありませんでした。</td></tr>");
        }

        html.AppendLine("</tbody></table></div>");
    }

    private static void AppendChartScript(StringBuilder html, TimelineAnalysisResult result)
    {
        var yearlyChartDefinitions = new[]
        {
            new
            {
                elementId = "yearly-world-map",
                region = "world",
                resolution = "countries",
                rows = BuildRows(result.YearlyVisits, "World")
            },
            new
            {
                elementId = "yearly-japan-map",
                region = "JP",
                resolution = "provinces",
                rows = BuildRows(result.YearlyVisits, "Japan")
            }
        };

        var monthlyRows = result.MonthlyVisits
            .Select(visit => new
            {
                visit.Code,
                visit.Group,
                Year = visit.LastVisitedOn.Year
            })
            .ToList();

        html.AppendLine("<script>");
        html.AppendLine("const yearlyChartDefinitions = ");
        html.AppendLine(JsonSerializer.Serialize(yearlyChartDefinitions));
        html.AppendLine(";");
        html.AppendLine("const monthlyRows = ");
        html.AppendLine(JsonSerializer.Serialize(monthlyRows));
        html.AppendLine(";");
        html.AppendLine("""
if (window.google && google.charts) {
  google.charts.load('current', { packages: ['geochart'] });
  google.charts.setOnLoadCallback(drawAllCharts);
} else {
  showMapFallback();
}

function drawAllCharts() {
  yearlyChartDefinitions.forEach(drawGeoChart);
  updateMonthlyYear();
}

function drawGeoChart(definition) {
  const element = document.getElementById(definition.elementId);
  if (!element) return;

  const data = new google.visualization.DataTable();
  data.addColumn('string', 'Region');
  data.addColumn('number', 'Visited');
  data.addRows(definition.rows);

  const chart = new google.visualization.GeoChart(element);
  chart.draw(data, {
    region: definition.region,
    resolution: definition.resolution,
    displayMode: 'regions',
    backgroundColor: '#ffffff',
    datalessRegionColor: '#ffffff',
    defaultColor: '#ffffff',
    colorAxis: { minValue: 0, maxValue: 1, colors: ['#dff5f2', '#0f766e'] },
    legend: 'none',
    keepAspectRatio: true
  });
}

function updateMonthlyYear() {
  const selector = document.getElementById('monthly-year-select');
  if (!selector) return;

  const selectedYear = Number(selector.value);
  document.querySelectorAll('tr[data-year]').forEach(row => {
    row.classList.toggle('hidden', Number(row.dataset.year) !== selectedYear);
  });

  if (window.google && google.visualization) {
    drawGeoChart({
      elementId: 'monthly-world-map',
      region: 'world',
      resolution: 'countries',
      rows: buildMonthlyRows('World', selectedYear)
    });
    drawGeoChart({
      elementId: 'monthly-japan-map',
      region: 'JP',
      resolution: 'provinces',
      rows: buildMonthlyRows('Japan', selectedYear)
    });
  }
}

function buildMonthlyRows(group, year) {
  return monthlyRows
    .filter(row => row.Group === group && row.Year === year)
    .map(row => [row.Code, 1]);
}

function showMapFallback() {
  document.querySelectorAll('.geo-map').forEach(element => {
    element.innerHTML = '<div class="map-error">Google Charts を読み込めませんでした。表形式の結果を確認してください。</div>';
  });
}

document.getElementById('monthly-year-select')?.addEventListener('change', updateMonthlyYear);

window.addEventListener('resize', () => {
  if (window.google && google.visualization) {
    drawAllCharts();
  }
});
""");
        html.AppendLine("</script>");
    }

    private static IReadOnlyList<object[]> BuildRows(IReadOnlyList<VisitRegion> visits, string group)
    {
        return visits
            .Where(visit => visit.Group == group)
            .Select(visit => new object[] { visit.Code, 1 })
            .ToList();
    }

    private static string FormatDate(VisitRegion visit)
    {
        return visit.Granularity == VisitGranularity.Year
            ? visit.LastVisitedOn.Year.ToString()
            : $"{visit.LastVisitedOn.Year}-{visit.LastVisitedOn.Month:00}";
    }
}
