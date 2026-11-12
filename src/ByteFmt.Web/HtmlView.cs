using System.Net;
using System.Text;

namespace ByteFmt.Web;

/// <summary>Renders the disk-usage dashboard as a self-contained HTML page (no templating engine).</summary>
public static class HtmlView
{
    public static string Dashboard(ScanResponse? result, string? path, string standard, string? error)
    {
        var body = new StringBuilder();
        body.Append("<h1>Disk usage explorer</h1>");
        body.Append("<p class=\"sub\">Scan a directory and see where the space goes — biggest folders, file types and files.</p>");

        body.Append("<div class=\"panel\"><form method=\"get\" action=\"/scan\">");
        body.Append("<label class=\"field\">Directory path</label>");
        body.Append($"<input type=\"text\" name=\"path\" value=\"{Enc(path)}\" placeholder=\"C:\\Users\\me\\projects  or  /var/log\" spellcheck=\"false\">");
        body.Append("<div class=\"row\" style=\"margin-top:12px\">");
        body.Append("<label class=\"field\" style=\"margin:0\">Units</label>");
        body.Append($"<select name=\"standard\">{UnitOption("Binary", standard)}{UnitOption("Decimal", standard)}</select>");
        body.Append("<button type=\"submit\">Scan</button>");
        body.Append("</div></form></div>");

        if (error is not null)
        {
            body.Append($"<div class=\"error\">{Enc(error)}</div>");
        }

        if (result is not null)
        {
            RenderResult(body, result);
        }

        return Page(body.ToString());
    }

    private static void RenderResult(StringBuilder body, ScanResponse r)
    {
        body.Append($"<h2>{Enc(r.Root)}</h2>");
        body.Append("<div class=\"stats\">");
        body.Append(Stat(r.TotalHuman, "Total size"));
        body.Append(Stat(r.Files.ToString("N0"), "Files"));
        body.Append(Stat(r.Directories.ToString("N0"), "Directories"));
        if (r.Skipped > 0)
        {
            body.Append(Stat(r.Skipped.ToString("N0"), "Skipped"));
        }
        body.Append("</div>");

        RenderBarTable(body, "Biggest folders", "Folder", r.TopFolders.Select(f =>
            (Label: f.Name, Sub: $"{f.Files:N0} files", Human: f.Human, Percent: f.Percent)));

        RenderBarTable(body, "By file type", "Extension", r.TopExtensions.Select(e =>
            (Label: e.Extension, Sub: $"{e.Files:N0} files", Human: e.Human, Percent: e.Percent)));

        body.Append("<h2>Largest files</h2><div class=\"panel\">");
        if (r.LargestFiles.Count == 0)
        {
            body.Append("<div class=\"empty\">No files found.</div>");
        }
        else
        {
            body.Append("<table><thead><tr><th>File</th><th class=\"num\">Size</th></tr></thead><tbody>");
            foreach (var f in r.LargestFiles)
            {
                body.Append($"<tr><td title=\"{Enc(f.Path)}\">{Enc(f.Name)}</td><td class=\"num\">{Enc(f.Human)}</td></tr>");
            }

            body.Append("</tbody></table>");
        }

        body.Append("</div>");
    }

    private static void RenderBarTable(
        StringBuilder body, string heading, string firstCol,
        IEnumerable<(string Label, string Sub, string Human, double Percent)> rows)
    {
        var list = rows.ToList();
        body.Append($"<h2>{Enc(heading)}</h2><div class=\"panel\">");
        if (list.Count == 0)
        {
            body.Append("<div class=\"empty\">Nothing to show.</div></div>");
            return;
        }

        body.Append($"<table><thead><tr><th>{Enc(firstCol)}</th><th class=\"num\">Size</th><th>Share</th></tr></thead><tbody>");
        foreach (var row in list)
        {
            body.Append("<tr>");
            body.Append($"<td><strong>{Enc(row.Label)}</strong> <span class=\"muted\">{Enc(row.Sub)}</span></td>");
            body.Append($"<td class=\"num\">{Enc(row.Human)}</td>");
            body.Append("<td><div class=\"bar-row\">");
            body.Append($"<div class=\"bar\"><i style=\"width:{row.Percent.ToString(System.Globalization.CultureInfo.InvariantCulture)}%\"></i></div>");
            body.Append($"<span class=\"muted pct\">{row.Percent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}%</span>");
            body.Append("</div></td></tr>");
        }

        body.Append("</tbody></table></div>");
    }

    private static string Stat(string value, string label) =>
        $"<div class=\"stat\"><div class=\"n\">{Enc(value)}</div><div class=\"l\">{Enc(label)}</div></div>";

    private static string UnitOption(string value, string selected)
    {
        var sel = string.Equals(value, selected, StringComparison.OrdinalIgnoreCase) ? " selected" : "";
        var hint = value == "Binary" ? "KiB, MiB…" : "KB, MB…";
        return $"<option value=\"{value}\"{sel}>{value} ({hint})</option>";
    }

    private static string Enc(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static string Page(string body) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>bytefmt — disk usage explorer</title>
          <style>
            :root {
              --bg:#f7f8fa; --panel:#fff; --ink:#1b1f24; --muted:#5b6672; --border:#e3e7ec;
              --accent:#0d9488; --accent-ink:#fff; --chip:#eef2f7;
              --shadow:0 1px 3px rgba(16,24,40,.06),0 1px 2px rgba(16,24,40,.04);
            }
            @media (prefers-color-scheme: dark) {
              :root {
                --bg:#0e1116; --panel:#161b22; --ink:#e6edf3; --muted:#8b949e; --border:#262c34;
                --accent:#2dd4bf; --accent-ink:#0e1116; --chip:#1c2430; --shadow:none;
              }
            }
            * { box-sizing:border-box; }
            body { margin:0; background:var(--bg); color:var(--ink);
              font:15px/1.55 -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,Helvetica,Arial,sans-serif; }
            header.top { background:var(--panel); border-bottom:1px solid var(--border); padding:14px 16px;
              display:flex; align-items:center; gap:12px; }
            header.top .brand { font-weight:700; font-size:18px; letter-spacing:-.01em; }
            header.top .brand span { color:var(--accent); }
            header.top .tag { color:var(--muted); font-size:13px; }
            main { max-width:900px; margin:0 auto; padding:24px 16px 64px; }
            h1 { font-size:22px; margin:0 0 4px; letter-spacing:-.01em; }
            h2 { font-size:16px; margin:28px 0 10px; word-break:break-all; }
            .sub { color:var(--muted); margin:0 0 20px; }
            .panel { background:var(--panel); border:1px solid var(--border); border-radius:12px;
              padding:16px; box-shadow:var(--shadow); margin-bottom:20px; }
            table { width:100%; border-collapse:collapse; font-size:14px; }
            th,td { text-align:left; padding:8px 10px; border-bottom:1px solid var(--border); }
            th { color:var(--muted); font-weight:600; font-size:12px; text-transform:uppercase; letter-spacing:.04em; }
            td.num,th.num { text-align:right; font-variant-numeric:tabular-nums; white-space:nowrap; }
            tr:last-child td { border-bottom:none; }
            .stats { display:flex; gap:12px; flex-wrap:wrap; }
            .stat { flex:1 1 130px; background:var(--panel); border:1px solid var(--border); border-radius:12px;
              padding:14px 16px; box-shadow:var(--shadow); }
            .stat .n { font-size:24px; font-weight:700; letter-spacing:-.02em; }
            .stat .l { color:var(--muted); font-size:12px; text-transform:uppercase; letter-spacing:.04em; }
            .bar-row { display:flex; align-items:center; gap:10px; }
            .bar { flex:1; height:8px; border-radius:999px; background:var(--chip); overflow:hidden; min-width:80px; }
            .bar > i { display:block; height:100%; background:var(--accent); }
            .pct { min-width:44px; text-align:right; font-variant-numeric:tabular-nums; }
            .muted { color:var(--muted); }
            input[type=text],select { font:inherit; background:var(--bg); color:var(--ink);
              border:1px solid var(--border); border-radius:8px; padding:9px 10px; }
            input[type=text] { width:100%; font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; font-size:13px; }
            label.field { display:block; margin:0 0 4px; font-size:13px; color:var(--muted); }
            button { font:inherit; font-weight:600; cursor:pointer; border:1px solid transparent;
              background:var(--accent); color:var(--accent-ink); padding:9px 16px; border-radius:8px; }
            .row { display:flex; gap:10px; align-items:center; flex-wrap:wrap; }
            .error { background:color-mix(in srgb,#b91c1c 12%,transparent); border:1px solid #b91c1c; color:#b91c1c;
              padding:10px 12px; border-radius:8px; margin-bottom:14px; }
            .empty { color:var(--muted); padding:20px; text-align:center; }
            footer { color:var(--muted); font-size:12px; text-align:center; padding:24px 16px; }
            code { font:12.5px ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; }
          </style>
        </head>
        <body>
          <header class="top">
            <div class="brand"><span>byte</span>fmt</div>
            <div class="tag">disk usage explorer · sizes via the DataSize type</div>
          </header>
          <main>
        {{body}}
          </main>
          <footer>bytefmt — a .NET DataSize value type, and the disk analyzer built on it</footer>
        </body>
        </html>
        """;
}
