using System.Net;
using System.Text;
using AnalyzeTimeline.Domain;

namespace AnalyzeTimeline.Application;

public sealed class ReportHtmlGenerator : IReportHtmlGenerator
{
    public string Generate(TimelineAnalysisResult result)
    {
        var yearlyCodes = result.YearlyVisits.Select(visit => visit.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var monthlyCodes = result.MonthlyVisits.Select(visit => visit.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"ja\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>Timeline Visit Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Inter,'Segoe UI',sans-serif;margin:0;background:#f7f8fb;color:#20242c}main{max-width:1120px;margin:auto;padding:32px}h1{font-size:28px;margin:0 0 8px}h2{font-size:20px;margin:28px 0 12px}.summary{display:flex;gap:12px;flex-wrap:wrap;margin:20px 0}.metric{background:white;border:1px solid #e3e7ef;border-radius:8px;padding:14px 16px;min-width:180px}.metric b{display:block;font-size:24px}.panel{background:white;border:1px solid #e3e7ef;border-radius:8px;padding:18px;margin:16px 0}.map{display:grid;grid-template-columns:repeat(auto-fit,minmax(116px,1fr));gap:8px}.tile{border:1px solid #dce1ea;border-radius:6px;background:#fff;color:#687184;padding:10px;min-height:52px}.tile.hit{background:#0f766e;color:white;border-color:#0f766e}.tile small{display:block;opacity:.75}table{width:100%;border-collapse:collapse;background:white}th,td{padding:10px 12px;border-bottom:1px solid #e8ebf1;text-align:left}th{background:#eef2f7;font-size:13px}tr:hover td{background:#fafbff}.tabs{display:flex;gap:8px;margin:24px 0 8px}.tab{padding:8px 12px;border:1px solid #cfd6e3;border-radius:6px;background:white}.tab.active{background:#25324a;color:white;border-color:#25324a}@media(max-width:640px){main{padding:18px}.summary{display:grid}.metric{min-width:0}}");
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
        AppendSection(html, "年別訪問先", result.YearlyVisits, yearlyCodes);
        AppendSection(html, "月別訪問先", result.MonthlyVisits, monthlyCodes);
        html.AppendLine("</main></body></html>");
        return html.ToString();
    }

    private static void AppendSection(StringBuilder html, string title, IReadOnlyList<VisitRegion> visits, ISet<string> activeCodes)
    {
        html.AppendLine($"<h2>{WebUtility.HtmlEncode(title)}</h2>");
        html.AppendLine("<div class=\"panel\"><h3>世界の白地図</h3><div class=\"map\">");
        foreach (var country in RegionCatalog.Countries)
        {
            AppendTile(html, country, activeCodes);
        }

        html.AppendLine("</div></div>");
        html.AppendLine("<div class=\"panel\"><h3>日本の白地図</h3><div class=\"map\">");
        foreach (var prefecture in RegionCatalog.JapanesePrefectures)
        {
            AppendTile(html, prefecture, activeCodes);
        }

        html.AppendLine("</div></div>");
        html.AppendLine("<div class=\"panel\"><table><thead><tr><th>分類</th><th>訪問先</th><th>最終訪問</th><th>地点数</th></tr></thead><tbody>");
        foreach (var visit in visits)
        {
            html.AppendLine("<tr>");
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

    private static void AppendTile(StringBuilder html, RegionDefinition region, ISet<string> activeCodes)
    {
        var activeClass = activeCodes.Contains(region.Code) ? " hit" : string.Empty;
        html.AppendLine($"<div class=\"tile{activeClass}\"><strong>{WebUtility.HtmlEncode(region.Name)}</strong><small>{WebUtility.HtmlEncode(region.Code)}</small></div>");
    }

    private static string FormatDate(VisitRegion visit)
    {
        return visit.Granularity == VisitGranularity.Year
            ? visit.LastVisitedOn.Year.ToString()
            : $"{visit.LastVisitedOn.Year}-{visit.LastVisitedOn.Month:00}";
    }
}
