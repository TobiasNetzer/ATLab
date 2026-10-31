#!/usr/bin/env python3
"""Build the ATLab user guide PDF from the Markdown chapters in docs/user-guide.

Usage:
    python scripts/build_user_guide_pdf.py --lang en --output ATLab-User-Guide-en.pdf
    python scripts/build_user_guide_pdf.py --lang de --output ATLab-User-Guide-de.pdf --version v1.4.0

Requirements (see scripts/requirements-docs.txt):
    pip install -r scripts/requirements-docs.txt
    python -m playwright install chromium
"""

from __future__ import annotations

import argparse
import datetime
import html
import os
import pathlib
import re
import sys

import markdown
from playwright.sync_api import sync_playwright

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent
GUIDE_ROOT = REPO_ROOT / "docs" / "user-guide"

LANGUAGES = {
    "en": {
        "source": GUIDE_ROOT / "en",
        "title": "User Guide",
        "tagline": "Automated testing framework for the Test Interface Adapter",
        "contents": "Contents",
        "version": "Version",
        "months": ["January", "February", "March", "April", "May", "June", "July",
                   "August", "September", "October", "November", "December"],
    },
    "de": {
        "source": GUIDE_ROOT / "de",
        "title": "Benutzerhandbuch",
        "tagline": "Framework für automatisierte Prüfungen mit dem Test Interface Adapter",
        "contents": "Inhalt",
        "version": "Version",
        "months": ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli",
                   "August", "September", "Oktober", "November", "Dezember"],
    },
}

CSS = """
@page { size: A4; }
:root { --accent:#1f6feb; --ink:#1b1f24; --muted:#5b6470; --rule:#d8dde3; --code:#f3f5f8; }
* { box-sizing:border-box; }
body { font-family: Inter, "DejaVu Sans", Arial, sans-serif; color:var(--ink); font-size:10pt; line-height:1.5; margin:0; }
.cover { height:250mm; display:flex; flex-direction:column; justify-content:center; page-break-after:always; }
.cover .logo { width:28mm; margin-bottom:10mm; }
.cover h1 { font-size:40pt; margin:0; letter-spacing:-0.5pt; border:0; }
.cover .sub { font-size:16pt; color:var(--muted); margin-top:3mm; }
.cover .bar { width:40mm; height:2mm; background:var(--accent); margin:10mm 0; }
.cover .meta { color:var(--muted); font-size:10pt; }
.toc { page-break-after:always; }
.toc h2 { font-size:20pt; border:0; margin-top:0; }
.toc ol { list-style:none; padding:0; }
.toc li a { display:block; text-decoration:none; color:var(--ink); padding:2.2mm 0; border-bottom:0.3pt solid var(--rule); font-size:11pt; }
.chapter { page-break-before:always; }
h1 { font-size:22pt; margin:0 0 6mm; padding-bottom:3mm; border-bottom:1.5pt solid var(--accent); }
h2 { font-size:14pt; margin:8mm 0 3mm; page-break-after:avoid; }
h3 { font-size:11.5pt; margin:6mm 0 2mm; page-break-after:avoid; }
h4 { font-size:10.5pt; margin:5mm 0 2mm; page-break-after:avoid; }
p, li { orphans:3; widows:3; }
a { color:var(--accent); text-decoration:none; }
code { font-family:"DejaVu Sans Mono", Consolas, monospace; font-size:8.6pt; background:var(--code); padding:0.3mm 1mm; border-radius:1mm; }
pre { background:var(--code); padding:3mm 4mm; border-radius:1.5mm; white-space:pre-wrap; overflow-wrap:anywhere; page-break-inside:avoid; }
pre code { background:none; padding:0; }
table { border-collapse:collapse; width:100%; margin:3mm 0 4mm; font-size:9pt; }
tr { page-break-inside:avoid; }
thead { display:table-header-group; }
th { text-align:left; background:#eef2f7; font-weight:600; }
th, td { border:0.4pt solid var(--rule); padding:1.6mm 2.2mm; vertical-align:top; }
blockquote { margin:4mm 0; padding:2mm 4mm; border-left:1mm solid var(--accent); background:#f1f6fe; page-break-inside:avoid; }
blockquote p { margin:1mm 0; }
figure { margin:4mm 0; text-align:center; page-break-inside:avoid; }
figure img { max-width:100%; max-height:120mm; border:0.4pt solid var(--rule); }
figcaption { font-size:8.5pt; color:var(--muted); margin-top:1.5mm; }
hr { border:0; border-top:0.4pt solid var(--rule); margin:6mm 0; }
"""


def slug(text: str) -> str:
    """GitHub-style heading anchor, so Markdown links work on GitHub and in the PDF."""
    t = html.unescape(re.sub(r"<[^>]+>", "", text)).strip().lower()
    t = re.sub(r"[^\w\- ]", "", t)
    return t.replace(" ", "-")


def chapter_id(filename: str) -> str:
    return "ch" + filename[:2]


class GuideBuilder:
    def __init__(self, lang: str, repo_url: str, ref: str):
        self.cfg = LANGUAGES[lang]
        self.lang = lang
        self.source: pathlib.Path = self.cfg["source"]
        self.blob_base = f"{repo_url.rstrip('/')}/blob/{ref}/"
        self.chapters = sorted(self.source.glob("[0-9][0-9]-*.md"))
        if not self.chapters:
            sys.exit(f"No chapters found in {self.source}")
        self.chapter_names = {p.name for p in self.chapters}

    def _resolve(self, md_file: pathlib.Path, target: str) -> pathlib.Path:
        return (md_file.parent / target).resolve()

    def _rewrite_link(self, md_file: pathlib.Path, text: str, target: str) -> str:
        if re.match(r"^[a-z]+:", target):            # http:, https:, mailto:
            return f"[{text}]({target})"
        path, _, anchor = target.partition("#")
        if not path:                                   # same-chapter anchor
            cid = chapter_id(md_file.name)
            return f"[{text}](#{cid}-{anchor})" if anchor else f"[{text}](#{cid})"
        resolved = self._resolve(md_file, path)
        if resolved.parent == self.source.resolve() and resolved.name in self.chapter_names:
            cid = chapter_id(resolved.name)
            return f"[{text}](#{cid}-{anchor})" if anchor else f"[{text}](#{cid})"
        # Any other file in the repository: link to it on GitHub
        try:
            rel = resolved.relative_to(REPO_ROOT).as_posix()
        except ValueError:
            return text
        url = self.blob_base + rel + (f"#{anchor}" if anchor else "")
        return f"[{text}]({url})"

    def _rewrite_image(self, md_file: pathlib.Path, alt: str, target: str) -> str:
        src = target if re.match(r"^[a-z]+:", target) else self._resolve(md_file, target).as_uri()
        return f'<figure><img src="{src}" alt="{html.escape(alt)}"><figcaption>{html.escape(alt)}</figcaption></figure>'

    def chapter_html(self, md_file: pathlib.Path) -> tuple[str, str]:
        text = md_file.read_text(encoding="utf-8")
        text = re.sub(r"!\[([^\]]*)\]\(([^)]+)\)",
                      lambda m: self._rewrite_image(md_file, m.group(1), m.group(2)), text)
        text = re.sub(r"(?<!!)\[([^\]]*)\]\(([^)]+)\)",
                      lambda m: self._rewrite_link(md_file, m.group(1), m.group(2)), text)
        body = markdown.markdown(text, extensions=["tables", "fenced_code", "sane_lists"])

        cid = chapter_id(md_file.name)
        title = ""

        def add_id(m: re.Match) -> str:
            nonlocal title
            level, inner = m.group(1), m.group(2)
            if level == "1" and not title:
                title = html.unescape(re.sub(r"<[^>]+>", "", inner))
                return f'<h1 id="{cid}">{inner}</h1>'
            return f'<h{level} id="{cid}-{slug(inner)}">{inner}</h{level}>'

        body = re.sub(r"<h([1-6])>(.*?)</h\1>", add_id, body)
        return title, f'<section class="chapter">{body}</section>'

    def document(self, version: str | None) -> str:
        sections, toc = [], []
        for md_file in self.chapters:
            title, section = self.chapter_html(md_file)
            sections.append(section)
            toc.append(f'<li><a href="#{chapter_id(md_file.name)}">{html.escape(title)}</a></li>')

        today = datetime.date.today()
        date_text = f"{self.cfg['months'][today.month - 1]} {today.year}"
        meta = [self.cfg["tagline"]]
        if version:
            meta.append(f"{self.cfg['version']} {html.escape(version)}")
        meta += [date_text, "github.com/TobiasNetzer/ATLab"]

        icon = REPO_ROOT / "src" / "ATLab" / "Assets" / "AppIcon.png"
        logo = f'<img class="logo" src="{icon.as_uri()}">' if icon.exists() else ""

        return f"""<!doctype html>
<html lang="{self.lang}"><head><meta charset="utf-8">
<title>ATLab {self.cfg['title']}</title><style>{CSS}</style></head><body>
<div class="cover">{logo}<h1>ATLab</h1><div class="sub">{self.cfg['title']}</div><div class="bar"></div>
<div class="meta">{'<br>'.join(meta)}</div></div>
<div class="toc"><h2>{self.cfg['contents']}</h2><ol>{''.join(toc)}</ol></div>
{''.join(sections)}
</body></html>"""

    def build(self, output: pathlib.Path, version: str | None, keep_html: bool) -> None:
        output = output.resolve()
        output.parent.mkdir(parents=True, exist_ok=True)
        html_path = output.with_suffix(".html")
        html_path.write_text(self.document(version), encoding="utf-8")

        footer = (
            '<div style="font-family:Inter,\'DejaVu Sans\',sans-serif;font-size:7.5pt;color:#5b6470;'
            'width:100%;padding:0 18mm;display:flex;justify-content:space-between">'
            f'<span>ATLab {self.cfg["title"]}{" " + html.escape(version) if version else ""}</span>'
            '<span><span class="pageNumber"></span> / <span class="totalPages"></span></span></div>'
        )
        with sync_playwright() as pw:
            browser = pw.chromium.launch()
            page = browser.new_page()
            page.goto(html_path.as_uri(), wait_until="networkidle")
            page.pdf(
                path=str(output),
                format="A4",
                print_background=True,
                display_header_footer=True,
                header_template="<div></div>",
                footer_template=footer,
                margin={"top": "20mm", "bottom": "18mm", "left": "18mm", "right": "18mm"},
                outline=True,
                tagged=True,
            )
            browser.close()

        if not keep_html:
            html_path.unlink()
        print(f"Built {output}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--lang", choices=sorted(LANGUAGES), default="en")
    parser.add_argument("--output", type=pathlib.Path, help="PDF file to write")
    parser.add_argument("--version", help="Version shown on the cover and in the footer, e.g. v1.4.0")
    parser.add_argument("--repo-url", default=os.environ.get(
        "GITHUB_REPO_URL", "https://github.com/TobiasNetzer/ATLab"),
        help="Base URL used for links to other repository files")
    parser.add_argument("--ref", default="main", help="Git ref used for links to repository files")
    parser.add_argument("--keep-html", action="store_true", help="Keep the intermediate HTML file")
    args = parser.parse_args()

    output = args.output or REPO_ROOT / f"ATLab-User-Guide-{args.lang}.pdf"
    GuideBuilder(args.lang, args.repo_url, args.ref).build(output, args.version, args.keep_html)


if __name__ == "__main__":
    main()
