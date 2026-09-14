#!/usr/bin/env python3
"""One source for the launch posts → docs/launch/posts.md (repo) and a copy-ready HTML page (artifact "Launch Kit").

Usage: python3 docs/launch/build.py [html-output-path]
Refresh FACTS / PREFLIGHT / SCHEDULE from production before a launch day, run, publish the HTML as the artifact."""
import html, json, pathlib, sys

FACTS = {
    "date": "14 September 2026",
    "apps": 17, "windows_apps": 6, "android_apps": 1, "unique_downloaders": 41, "members": 4, "wanted": 5, "ratings": 0,
    "tokens_saved": "16.1 M", "money_saved": "$97",
}

PREFLIGHT = [
    ("Catalogue size", "warn", "17 published apps (target 20–30): Linux desktop tools, 6 of them with Windows builds, 1 Android app. All MIT, all built with Claude (Fable 5.1 / Opus 5), uploaded by three accounts. The drafts say this openly and turn it into the ask."),
    ("Screenshots", "warn", "16 of 17 apps have 3 or more. AdBlock (Android) still has one — needs a phone screenshot from you."),
    ("Ratings", "warn", "None yet. Two or three honest reviews from the second member account on apps you did not upload keep the app pages from looking bare."),
    ("Outgoing e-mail", "ok", "Brevo SMTP live, domain authenticated (DKIM + DMARC), test mail delivered 11 Sep."),
    ("Google sign-in", "ok", "Client id configured; button renders on /login with zero CSP violations."),
    ("Security headers and console", "ok", "Headless crawl of 9 production pages on 12 Sep: no console errors, no CSP refusals, no failed requests."),
    ("Agent endpoints", "ok", "GET /api/v1/agent/check answers with a verdict. MCP server initialises and lists all 7 tools."),
    ("Health", "ok", "API, site, files and MCP hosts answer 200; admin System health all green after the 13 Sep deploy (platform logos)."),
    ("Savings counter", "ok", "Conservative formula: 16.1 M tokens, about $97 from 41 unique downloaders — defensible in a thread."),
    ("Posting account", "todo", "Post from your own Reddit and HN accounts, which have history. Fresh accounts that only post links are removed automatically."),
    ("Be present", "todo", "Block 3–4 hours after each post to answer. Replies matter more than the post text."),
]

SCHEDULE = [
    ("Mon 14 Sep, 15:40 UTC — done", "r/SideProject", "Posted: https://www.reddit.com/r/SideProject/comments/1wg6ucf/ — answer every comment for the first hours."),
    ("Mon 14 Sep, ~16:45 UTC", "Mastodon (fosstodon), X, LinkedIn", "Short version with the screenshot. Link to the Reddit thread."),
    ("Tue 15 Sep", "r/coolgithubprojects", "Title carries the repo link; flair C#. No karma gate seen."),
    ("Wed 16 Sep", "r/opensource", "Lead with the license verification, not the site. Rule: engage in the thread, drive-by posts are removed."),
    ("When the account has > 50 karma", "r/ClaudeAI", "Its showcase posts need OP karma > 50 for the feed, else they go to the megathread. Answer Claude Code questions there for a week to get past it. Text is ready in the kit."),
    ("When HN lifts the new-account brake", "Show HN", "HN blocks Show HN from new accounts (2026 influx notice); exception request to hn@ycombinator.com sent/drafted 14 Sep. Otherwise comment for a couple of weeks first."),
    ("Later", "r/vibecoding", "Rule: vibe-coding dev tools must be approved by the mods first — message them before posting."),
    ("Week of 21 Sep", "Product Hunt, dev.to article, r/selfhosted megathread, awesome-mcp-servers PR", "After the first wave of feedback is folded into the site."),
]

STATE = "There are 17 apps today: Linux desktop tools, six of them with Windows builds too, and one Android app. Most are mine."

HN_COMMENT = f"""Hi HN. I built this because I kept watching people (and my own agents) regenerate the same small apps over and over: a CPU temperature monitor, an audio converter, a paint clone. Every rerun costs tokens, money and energy, and the result usually ends up in a folder nobody else sees.

AI Already Did It is a free repository of applications written by LLMs. Every entry has to pass three rules: open source (SPDX license plus a LICENSE file in the source that actually matches), installable (at least one ready-to-run file per declared platform: .deb, .exe, .dmg, .apk, AppImage, Docker image, web bundle), and free. Each app also records the model that generated it and the original prompt(s), so you can download it or fork it and rerun the prompt with your own changes. Uploaded installers are checked against their magic bytes and scanned with ClamAV.

The part I care most about is the agent side. There is a REST endpoint and an MCP server (https://mcp.aialreadydidit.com/mcp) with a check_before_building tool: an agent describes what it was asked to build and gets a verdict of download, fork or build, with the closest matches and their licenses. The idea is that a coding agent calls it before writing anything. Search is hybrid: Postgres full-text plus pgvector with bge-m3 embeddings, fused with reciprocal rank fusion. No OpenSearch; one database keeps moderation state transactional.

Stack: ASP.NET Core 10, Vue 3, PostgreSQL 18 with pgvector, MinIO, ClamAV, Ollama for embeddings, all in docker-compose. The whole thing is MIT on GitHub: https://github.com/mehmetgoren/aialreadydidit

It is early. {STATE} What I am looking for is (a) people uploading the apps their agents already wrote, for any platform, and (b) feedback on whether the "check before you build" step is something you would actually wire into an agent. Anonymous search and download need no account; uploading does, and everything goes through a moderation queue.

Honest limitations: the similarity threshold is tuned by hand on a small corpus, the savings counter is an estimate (it caps uploader-claimed token totals and counts only half of the unique downloads as an avoided generation), the non-English UI translations were LLM-generated and not reviewed by native speakers, and there is no guarantee that the recorded prompt regenerates the same app."""

HN_QA = [
    ("Isn't this just GitHub?", "GitHub has no installable-file rule, no prompt and model record, no license verification, and no “is there already one of these” verdict for agents. The store links back to the repository when there is one."),
    ("Who moderates?", "Every submission is scanned and reviewed by a human before publishing; trusted uploaders publish new versions of existing apps without review. Reports go to the same queue."),
    ("How is the savings counter computed?", "Source lines × tokens per line × an iteration factor, unless the uploader supplies a real total, which is capped at five times that estimate. Only unique downloaders count, and only half of them are assumed to have skipped a generation. Price, kWh and CO₂ coefficients are visible in the admin settings. It is labelled as an estimate."),
    ("Why not let anyone upload without an account?", "Accountability for the license and for abuse reports."),
    ("Malware?", "Magic-byte check on every installer, ClamAV scan, human review, report button, and the source is public."),
    ("Why is everything built with Claude, and mostly yours?", "Because the first uploads are my own projects. The model list has every major vendor and the platform list covers Windows, macOS, web, Android, iOS, Docker and CLI. That gap is exactly why I am posting."),
    ("Why would an agent call your API instead of just building?", "Because building costs the user tokens and time and the result is usually a worse copy of something that exists. The reuse_first prompt shipped with the MCP server tells the agent to check first; whether agents adopt that is the open question I want feedback on."),
]

SIDEPROJECT_BODY = f"""https://aialreadydidit.com

Every app on it was generated by an LLM. Each listing has the source, an installer per platform, screenshots, the model and the original prompt. Download it, or copy the prompt and make your own variant. Everything is open source and free; uploads go through a license check, a malware scan and a human review.

The twist is an API and an MCP server for coding agents: before an agent builds something, it asks the store and gets "download this", "fork that" or "go ahead and build". The homepage shows a conservative estimate of the tokens, money and energy saved by reuse.

Built with Claude Code over about a week. Stack: .NET 10, Vue 3, Postgres + pgvector, MinIO, ClamAV. Code is MIT on GitHub: https://github.com/mehmetgoren/aialreadydidit

{STATE} Looking for people who have AI-generated apps sitting in a folder, for any platform, and would upload them."""

POSTS = [
    {
        "id": "hn", "channel": "Show HN", "where": "news.ycombinator.com/submit", "flair": None,
        "rules": "Title ≤ 80 chars, starts with “Show HN:”, no “we”, no exclamation marks. Submit the URL, then post the maker comment immediately.",
        "url": "https://aialreadydidit.com",
        "title": "Show HN: AI Already Did It – a free store for LLM-generated apps, with prompts",
        "alt_titles": [
            "Show HN: A free store of LLM-generated apps, so agents check before they build",
            "Show HN: AI Already Did It – check whether an LLM already wrote that app",
        ],
        "body": HN_COMMENT, "body_label": "Maker comment", "qa": HN_QA,
    },
    {
        "id": "claudeai", "channel": "r/ClaudeAI", "where": "reddit.com/r/ClaudeAI/submit", "flair": "Built with Claude",
        "rules": "Showcases built with Claude are explicitly welcome; say how Claude was used. Check the current flair list before posting. Fold in the strongest HN objection before posting.",
        "url": None,
        "title": "I built a free store for apps written by LLMs (with the prompts) so agents can check before they build — the whole thing was built with Claude Code",
        "alt_titles": [],
        "body": f"""**What it is:** https://aialreadydidit.com is a free, open-source repository of applications written by LLMs. Each app ships with its source, an installable file per platform, screenshots, the model that generated it and the original prompt(s). You can download it, or fork the prompt and make your own variant.

**Why:** the same small apps get regenerated thousands of times. Publishing them once saves the next person's tokens and lets an agent reuse instead of rebuild.

**The Claude part:** the app itself (ASP.NET Core 10 backend, Vue 3 frontend, PostgreSQL + pgvector search, MinIO, ClamAV, MCP server, admin panel, 11 UI languages) was built in Claude Code sessions with Claude Fable 5.1, from a written brief and a Q&A round of blocking questions. Claude also wrote the xunit and Vitest suites and the QA pass that found the search-stemming and security-header issues. The apps in the store today are earlier Claude Code projects — Linux and Windows desktop tools built with Fable 5.1 and Opus 5.

**For Claude Code users:** there is an MCP server. Add it with

    claude mcp add --transport http ai-already-did-it https://mcp.aialreadydidit.com/mcp

and use the `reuse_first` prompt. Claude then calls `check_before_building` before writing an app and gets a download / fork / build verdict with the closest matches.

**Rules for uploads:** open source with a matching LICENSE file (verified), at least one ready-to-run installer per declared platform, free. Installers are format-checked and scanned.

Source (MIT): https://github.com/mehmetgoren/aialreadydidit

It is early. {STATE} If your Claude sessions produced an app you would let others install, on any platform, I would love to see it uploaded. Feedback on the agent flow is very welcome.""",
        "body_label": "Post body", "qa": [],
    },
    {
        "id": "opensource", "channel": "r/opensource", "where": "reddit.com/r/opensource/submit", "flair": None,
        "rules": "A LICENSE in the repo is required (MIT, present). Self-promotion is allowed “to a degree”: lead with the license verification and the code, not the site.",
        "url": None,
        "title": "AI Already Did It — an MIT-licensed store for LLM-generated apps that verifies the LICENSE file of every upload",
        "alt_titles": [],
        "body": """I released the source of a web application I run at https://aialreadydidit.com: a free repository of applications written by LLMs.

The relevant part for this subreddit is the rule set. Every upload must declare an SPDX license, and the source archive or linked GitHub/GitLab repository must contain a LICENSE file whose text matches that license (the detector recognises the common families and refuses mismatches, e.g. "declared MIT, file says GPL-3.0"). Non-open licenses are rejected at submit time. Each app also records which model generated it and the prompt(s), which are published alongside the source.

The application itself is MIT: ASP.NET Core 10, Vue 3, PostgreSQL 18 with pgvector for hybrid keyword + semantic search, MinIO, ClamAV, docker-compose for the whole stack, plus an MCP server so coding agents can check whether an app already exists before generating it.

Repository: https://github.com/mehmetgoren/aialreadydidit

Feedback on the license-verification approach is welcome — in particular which SPDX identifiers you would expect to be accepted that currently are not.""",
        "body_label": "Post body", "qa": [],
    },
    {
        "id": "sideproject", "channel": "r/SideProject", "where": "reddit.com/r/SideProject/submit", "flair": None,
        "rules": "Built for exactly this; low friction. Ask for uploads and feedback.",
        "url": None,
        "title": "I made a free app store where every app was written by an AI, with the prompt included",
        "alt_titles": [], "body": SIDEPROJECT_BODY, "body_label": "Post body", "qa": [],
    },
    {
        "id": "vibecoding", "channel": "r/vibecoding", "where": "reddit.com/r/vibecoding/submit", "flair": None,
        "rules": "Allowed only if the post explains how it was built: tools, workflow, code. Same body as r/SideProject with the paragraph below first.",
        "url": None,
        "title": "How I built a store for vibe-coded apps with Claude Code (and why it asks agents to check before they build)",
        "alt_titles": [],
        "body": "How it was built: one written brief, a round of blocking questions the agent asked back (which embedding provider, what to do about a project with no LICENSE, default language), then Claude Code sessions with Claude Fable 5.1. I reviewed and tested in the browser after each session and sent back a list of findings. Tests (xunit + Vitest) were written by the agent as well.\n\n" + SIDEPROJECT_BODY.replace("Built with Claude Code over about a week. ", ""),
        "body_label": "Post body", "qa": [],
    },
    {
        "id": "coolgithub", "channel": "r/coolgithubprojects", "where": "reddit.com/r/coolgithubprojects/submit", "flair": "C#",
        "rules": "Title must contain the repo link; flair is the language.",
        "url": None,
        "title": "[C# / Vue] AI Already Did It — a free store for LLM-generated apps with prompt, license check and an MCP “check before you build” tool — https://github.com/mehmetgoren/aialreadydidit",
        "alt_titles": [],
        "body": "Live: https://aialreadydidit.com\nMCP: claude mcp add --transport http ai-already-did-it https://mcp.aialreadydidit.com/mcp",
        "body_label": "Post body", "qa": [],
    },
    {
        "id": "social", "channel": "Mastodon / X / LinkedIn", "where": "your own accounts", "flair": None,
        "rules": "Post about an hour after the r/SideProject post, attach docs/screenshots/home.png, link the Reddit thread.",
        "url": None,
        "title": "Before you ask an AI to build it, check whether AI already did it.",
        "alt_titles": [],
        "body": """Before you ask an AI to build it, check whether AI already did it.

I built a free, open-source store for apps written by LLMs: source + installer + the original prompt, license-verified and virus-scanned. There is an MCP server so coding agents check the store before they generate.

Site: https://aialreadydidit.com
Code (MIT): https://github.com/mehmetgoren/aialreadydidit
Discussion on Reddit: https://www.reddit.com/r/SideProject/comments/1wg6ucf/

#opensource #mcp #claudecode""",
        "body_label": "Post text", "qa": [],
    },
    {
        "id": "producthunt", "channel": "Product Hunt (week of 21 Sep)", "where": "producthunt.com/posts/new", "flair": None,
        "rules": "Tagline ≤ 60 characters. Gallery: home.png, an app page, the agent-check JSON, the wizard's duplicate warning.",
        "url": None,
        "title": "Check if an AI already wrote that app before you build it",
        "alt_titles": [],
        "body": "\n\n".join(HN_COMMENT.split("\n\n")[i] for i in (0, 1, 2, 4)),
        "body_label": "First comment", "qa": [],
    },
]

AVOID = [
    ("r/ChatGPTCoding", "self-promotion only in the weekly thread, FOSS included"),
    ("r/programming", "bans “I made this” project posts and aggregators"),
    ("r/webdev", "project posts only on Showoff Saturday"),
    ("r/InternetIsBeautiful", "forbids aggregators, collections and stores"),
    ("r/artificial", "first post cannot be promotional"),
    ("r/selfhosted (as a thread)", "new projects go to the New Project Megathread only"),
]

# ------------------------------------------------------------------ markdown
def md():
    out = ["# Launch kit\n", f"Copy-ready posts for the launch of https://aialreadydidit.com, refreshed {FACTS['date']} from the production database and the community-rules research of 8 September (CLAUDE.md, log 24). Generated by `docs/launch/build.py`; the same content is published as the copy-ready artifact “Launch Kit”.\n"]
    out.append("## Pre-flight\n\n| Item | Status | Detail |\n|---|---|---|")
    for name, st, detail in PREFLIGHT:
        out.append(f"| {name} | {'done' if st=='ok' else 'gap' if st=='warn' else 'to do'} | {detail} |")
    out.append("\n## Schedule\n\n| When | Where | Note |\n|---|---|---|")
    for when, where, note in SCHEDULE: out.append(f"| {when} | {where} | {note} |")
    out.append("")
    for p in POSTS:
        out.append(f"---\n\n## {p['channel']}\n")
        out.append((f"Flair: **{p['flair']}**. " if p['flair'] else "") + p['rules'] + "\n")
        if p['url']: out.append(f"URL: `{p['url']}`\n")
        out.append("**Title**\n\n```\n" + p['title'] + "\n```\n")
        for a in p['alt_titles']: out.append("Alternative:\n\n```\n" + a + "\n```\n")
        out.append(f"**{p['body_label']}**\n\n```\n" + p['body'] + "\n```\n")
        if p['qa']:
            out.append("Answers to have ready:\n")
            for q, a in p['qa']: out.append(f"- *{q}* {a}")
            out.append("")
    out.append("---\n\n## dev.to / Hashnode article (week of 21 Sep)\n")
    out.append("Working title: *Teaching a coding agent to check before it builds: an MCP tool, pgvector and reciprocal rank fusion*. Sections: the problem, the three rules, hybrid search (tsvector + HNSW + RRF, thresholds 0.72 / 0.45 and why bge-m3 changed them), the agent verdict, the moderation pipeline (magic bytes, ClamAV, review), the conservative savings formula, what did not work (OpenSearch considered and rejected, ImageSharp 4 licensing, nginx `$1` reset), what is next.\n")
    out.append("## Do not post to\n")
    for s, why in AVOID: out.append(f"- {s}: {why}")
    out.append("")
    return "\n".join(out)

# ------------------------------------------------------------------ html
def esc(s): return html.escape(s, quote=True)

def page():
    pre_rows = "".join(f'<tr><th scope="row">{esc(n)}</th><td><span class="pill pill--{st}">{ {"ok":"done","warn":"gap","todo":"to do"}[st] }</span></td><td>{esc(d)}</td></tr>' for n, st, d in PREFLIGHT)
    sched_rows = "".join(f'<tr><td class="when">{esc(w)}</td><td><strong>{esc(where)}</strong></td><td>{esc(n)}</td></tr>' for w, where, n in SCHEDULE)
    avoid_rows = "".join(f'<li><strong>{esc(s)}</strong> — {esc(w)}</li>' for s, w in AVOID)
    nav = "".join(f'<a href="#post-{p["id"]}">{esc(p["channel"])}</a>' for p in POSTS)
    sections = []
    for p in POSTS:
        alts = "".join(f'<div class="alt"><code>{esc(a)}</code><button class="copy" data-copy-text="{esc(a)}">Copy</button></div>' for a in p['alt_titles'])
        qa = ('<details class="qa"><summary>Answers to have ready</summary><dl>' + "".join(f'<dt>{esc(q)}</dt><dd>{esc(a)}</dd>' for q, a in p['qa']) + '</dl></details>') if p['qa'] else ""
        url = f'<div class="field"><span class="label">URL</span><code>{esc(p["url"])}</code><button class="copy" data-copy-text="{esc(p["url"])}">Copy</button></div>' if p['url'] else ""
        flair = f'<span class="flair">Flair · {esc(p["flair"])}</span>' if p['flair'] else ""
        limit = ' data-limit="80"' if p['id'] == 'hn' else (' data-limit="60"' if p['id'] == 'producthunt' else ' data-limit="300"')
        rows = min(26, max(4, p['body'].count(chr(10)) + 6))
        sections.append(f'''
<section class="post" id="post-{p['id']}">
  <header class="post__head"><div><h2>{esc(p['channel'])}</h2><div class="where">{esc(p['where'])} {flair}</div></div></header>
  <p class="rules">{esc(p['rules'])}</p>
  {url}
  <div class="field">
    <span class="label">Title <span class="count" data-count-for="{p['id']}"{limit}></span></span>
    <textarea id="title-{p['id']}" class="title" rows="2" spellcheck="false">{esc(p['title'])}</textarea>
    <button class="copy" data-copy-from="title-{p['id']}">Copy title</button>
  </div>
  {('<div class="alts"><span class="label">Alternatives</span>' + alts + '</div>') if alts else ''}
  <div class="field field--body">
    <span class="label">{esc(p['body_label'])} <span class="count" data-count-for="body-{p['id']}"></span></span>
    <textarea id="body-{p['id']}" class="body" rows="{rows}" spellcheck="false">{esc(p['body'])}</textarea>
    <button class="copy copy--primary" data-copy-from="body-{p['id']}">Copy {esc(p['body_label'].lower())}</button>
  </div>
  {qa}
</section>''')
    return f'''<title>Launch Kit</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@500;700;800&family=Source+Sans+3:ital,wght@0,400;0,600;1,400&family=IBM+Plex+Mono:wght@400;500&display=swap">
<style>
:root {{
  --bg: #F2F5F9; --surface: #FFFFFF; --surface-2: #E9EEF5; --line: #CBD5E1; --line-soft: #DDE4EE;
  --ink: #16202E; --ink-2: #4A5668; --ink-3: #6B7787;
  --accent: #1D5FD1; --accent-ink: #FFFFFF; --accent-soft: #DCE7FA;
  --ok: #1F7A4D; --ok-bg: #DCF3E6; --warn: #9A6400; --warn-bg: #FBEFD0; --todo: #4A5668; --todo-bg: #E4E9F0;
  --mono: "IBM Plex Mono", ui-monospace, SFMono-Regular, Menlo, monospace;
  --display: "Manrope", "Segoe UI", system-ui, sans-serif;
  --body: "Source Sans 3", "Segoe UI", system-ui, sans-serif;
  --radius: 6px;
}}
@media (prefers-color-scheme: dark) {{
  :root:not([data-theme="light"]) {{
    --bg: #0F141B; --surface: #161C25; --surface-2: #1E2631; --line: #33404F; --line-soft: #27313D;
    --ink: #E7ECF3; --ink-2: #B4BECB; --ink-3: #8B97A7;
    --accent: #6FA0F5; --accent-ink: #0B1220; --accent-soft: #1D2E4A;
    --ok: #7BD8A4; --ok-bg: #163126; --warn: #F0C36A; --warn-bg: #3A2D10; --todo: #B4BECB; --todo-bg: #242D39;
  }}
}}
:root[data-theme="dark"] {{
  --bg: #0F141B; --surface: #161C25; --surface-2: #1E2631; --line: #33404F; --line-soft: #27313D;
  --ink: #E7ECF3; --ink-2: #B4BECB; --ink-3: #8B97A7;
  --accent: #6FA0F5; --accent-ink: #0B1220; --accent-soft: #1D2E4A;
  --ok: #7BD8A4; --ok-bg: #163126; --warn: #F0C36A; --warn-bg: #3A2D10; --todo: #B4BECB; --todo-bg: #242D39;
}}
* {{ box-sizing: border-box; }}
body {{ margin: 0; background: var(--bg); color: var(--ink); font-family: var(--body); font-size: 16px; line-height: 1.5; padding-block: 0 64px; padding-inline: 20px; }}
a {{ color: var(--accent); }}
.wrap {{ max-width: 1120px; margin: 0 auto; display: grid; grid-template-columns: 220px minmax(0, 1fr); gap: 40px; }}
.rail {{ position: sticky; top: 0; align-self: start; padding-block: 28px 0; }}
.rail nav {{ display: flex; flex-direction: column; gap: 2px; border-left: 2px solid var(--line-soft); }}
.rail nav a {{ color: var(--ink-2); text-decoration: none; padding: 6px 12px; font-size: 14px; border-left: 2px solid transparent; margin-left: -2px; }}
.rail nav a:hover, .rail nav a:focus-visible {{ color: var(--ink); border-left-color: var(--accent); outline: none; }}
.rail .eyebrow {{ margin: 0 0 10px; }}
main {{ min-width: 0; padding-block: 28px 0; display: flex; flex-direction: column; gap: 40px; }}
.eyebrow {{ font-family: var(--display); font-weight: 700; font-size: 12px; letter-spacing: .08em; text-transform: uppercase; color: var(--ink-3); }}
h1 {{ font-family: var(--display); font-weight: 800; font-size: 34px; line-height: 1.15; margin: 4px 0 8px; letter-spacing: -.01em; text-wrap: balance; }}
h2 {{ font-family: var(--display); font-weight: 700; font-size: 22px; margin: 0; letter-spacing: -.01em; }}
h3 {{ font-family: var(--display); font-weight: 700; font-size: 17px; margin: 0 0 10px; }}
.lede {{ max-width: 62ch; color: var(--ink-2); margin: 0; }}
.facts {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 12px; margin-top: 20px; }}
.fact {{ background: var(--surface); border: 1px solid var(--line-soft); border-radius: var(--radius); padding: 12px 14px; }}
.fact b {{ display: block; font-family: var(--display); font-size: 24px; font-weight: 800; font-variant-numeric: tabular-nums; }}
.fact span {{ color: var(--ink-3); font-size: 13px; }}
table {{ width: 100%; border-collapse: collapse; font-size: 15px; }}
.table-wrap {{ overflow-x: auto; background: var(--surface); border: 1px solid var(--line-soft); border-radius: var(--radius); }}
th, td {{ text-align: left; vertical-align: top; padding: 10px 14px; border-top: 1px solid var(--line-soft); }}
thead th {{ border-top: 0; font-family: var(--display); font-size: 12px; letter-spacing: .06em; text-transform: uppercase; color: var(--ink-3); background: var(--surface-2); }}
tbody th {{ font-weight: 600; white-space: nowrap; }}
td.when {{ white-space: nowrap; font-family: var(--mono); font-size: 13px; color: var(--ink-2); }}
.pill {{ display: inline-block; font-family: var(--display); font-size: 12px; font-weight: 700; letter-spacing: .04em; padding: 2px 8px; border-radius: 999px; white-space: nowrap; }}
.pill--ok {{ color: var(--ok); background: var(--ok-bg); }} .pill--warn {{ color: var(--warn); background: var(--warn-bg); }} .pill--todo {{ color: var(--todo); background: var(--todo-bg); }}
.post {{ background: var(--surface); border: 1px solid var(--line); border-radius: var(--radius); padding: 22px 24px; display: flex; flex-direction: column; gap: 16px; scroll-margin-top: 16px; }}
.post__head .where {{ color: var(--ink-3); font-size: 14px; font-family: var(--mono); margin-top: 2px; display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }}
.flair {{ font-family: var(--display); font-size: 12px; font-weight: 700; color: var(--accent); background: var(--accent-soft); padding: 2px 8px; border-radius: 999px; }}
.rules {{ margin: 0; color: var(--ink-2); max-width: 70ch; }}
.field {{ display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 8px 12px; align-items: start; }}
.field .label {{ grid-column: 1 / -1; }}
.label {{ font-family: var(--display); font-size: 12px; font-weight: 700; letter-spacing: .06em; text-transform: uppercase; color: var(--ink-3); display: flex; gap: 10px; align-items: baseline; }}
.count {{ font-family: var(--mono); letter-spacing: 0; text-transform: none; font-weight: 400; color: var(--ink-3); }} .count.over {{ color: var(--warn); font-weight: 500; }}
textarea, .field code {{ font-family: var(--mono); font-size: 13.5px; line-height: 1.55; color: var(--ink); background: var(--surface-2); border: 1px solid var(--line-soft); border-radius: var(--radius); padding: 10px 12px; width: 100%; }}
textarea {{ resize: vertical; }} textarea:focus-visible {{ outline: 2px solid var(--accent); outline-offset: 1px; }}
.field code {{ display: block; white-space: pre-wrap; word-break: break-word; }}
.alts {{ display: flex; flex-direction: column; gap: 8px; }}
.alt {{ display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 12px; align-items: start; }}
.alt code {{ font-family: var(--mono); font-size: 13.5px; background: var(--surface-2); border: 1px solid var(--line-soft); border-radius: var(--radius); padding: 8px 12px; display: block; white-space: pre-wrap; word-break: break-word; }}
button.copy {{ font-family: var(--display); font-weight: 700; font-size: 13px; color: var(--ink); background: var(--surface); border: 1px solid var(--line); border-radius: var(--radius); padding: 8px 12px; cursor: pointer; white-space: nowrap; }}
button.copy:hover {{ border-color: var(--accent); color: var(--accent); }} button.copy:focus-visible {{ outline: 2px solid var(--accent); outline-offset: 2px; }}
button.copy--primary {{ background: var(--accent); color: var(--accent-ink); border-color: var(--accent); }} button.copy--primary:hover {{ color: var(--accent-ink); filter: brightness(1.08); }}
button.copy.done {{ border-color: var(--ok); color: var(--ok); background: var(--ok-bg); }}
.qa summary {{ cursor: pointer; font-family: var(--display); font-weight: 700; font-size: 14px; color: var(--ink-2); }}
.qa dl {{ margin: 12px 0 0; display: grid; gap: 10px; }} .qa dt {{ font-weight: 600; }} .qa dd {{ margin: 2px 0 0; color: var(--ink-2); max-width: 70ch; }}
.avoid ul {{ margin: 0; padding-left: 20px; color: var(--ink-2); display: grid; gap: 4px; }}
.toast {{ position: fixed; left: 50%; bottom: 24px; transform: translateX(-50%) translateY(20px); background: var(--ink); color: var(--bg); font-family: var(--display); font-weight: 700; font-size: 14px; padding: 10px 16px; border-radius: 999px; opacity: 0; transition: opacity .18s, transform .18s; pointer-events: none; }}
.toast.show {{ opacity: 1; transform: translateX(-50%) translateY(0); }}
@media (max-width: 860px) {{ .wrap {{ grid-template-columns: 1fr; gap: 24px; }} .rail {{ position: static; }} .rail nav {{ flex-direction: row; flex-wrap: wrap; border-left: 0; gap: 6px; }} .rail nav a {{ border: 1px solid var(--line-soft); border-radius: 999px; margin: 0; padding: 4px 10px; }} .post {{ padding: 16px; }} h1 {{ font-size: 28px; }} }}
@media (prefers-reduced-motion: reduce) {{ .toast {{ transition: none; }} }}
</style>
<div class="wrap">
  <aside class="rail"><p class="eyebrow">Posts</p><nav>{nav}<a href="#preflight">Pre-flight</a><a href="#schedule">Schedule</a><a href="#avoid">Do not post to</a></nav></aside>
  <main>
    <header>
      <p class="eyebrow">aialreadydidit.com · refreshed {esc(FACTS['date'])}</p>
      <h1>Launch Kit</h1>
      <p class="lede">Copy-ready posts for Show HN, five subreddits, social and Product Hunt, with the pre-flight state of the site and the posting order. Edit any text in place before copying; nothing here is saved.</p>
      <div class="facts">
        <div class="fact"><b>{FACTS['apps']}</b><span>published apps</span></div>
        <div class="fact"><b>{FACTS['windows_apps']}</b><span>with Windows builds</span></div>
        <div class="fact"><b>{FACTS['unique_downloaders']}</b><span>unique downloaders</span></div>
        <div class="fact"><b>{esc(FACTS['tokens_saved'])}</b><span>tokens saved (counter)</span></div>
        <div class="fact"><b>{FACTS['ratings']}</b><span>ratings</span></div>
      </div>
    </header>
    <section id="preflight"><h3>Pre-flight</h3><div class="table-wrap"><table><thead><tr><th>Item</th><th>Status</th><th>Detail</th></tr></thead><tbody>{pre_rows}</tbody></table></div></section>
    <section id="schedule"><h3>Schedule</h3><div class="table-wrap"><table><thead><tr><th>When</th><th>Where</th><th>Note</th></tr></thead><tbody>{sched_rows}</tbody></table></div></section>
    {''.join(sections)}
    <section id="avoid" class="avoid"><h3>Do not post to</h3><ul>{avoid_rows}</ul></section>
  </main>
</div>
<div class="toast" id="toast" role="status" aria-live="polite">Copied</div>
<script>
(() => {{
  const toast = document.getElementById('toast'); let timer = 0;
  function say(text) {{ toast.textContent = text; toast.classList.add('show'); clearTimeout(timer); timer = setTimeout(() => toast.classList.remove('show'), 1400); }}
  async function copy(text, btn) {{
    try {{ await navigator.clipboard.writeText(text); }}
    catch {{ const ta = document.createElement('textarea'); ta.value = text; ta.style.position = 'fixed'; ta.style.opacity = '0'; document.body.appendChild(ta); ta.select(); try {{ document.execCommand('copy'); }} catch {{}} ta.remove(); }}
    say('Copied'); btn.classList.add('done'); setTimeout(() => btn.classList.remove('done'), 1400);
  }}
  document.querySelectorAll('button.copy').forEach(btn => btn.addEventListener('click', () => {{ const from = btn.dataset.copyFrom; copy(from ? document.getElementById(from).value : btn.dataset.copyText, btn); }}));
  function refreshCount(ta) {{
    const id = ta.id.startsWith('title-') ? ta.id.slice(6) : ta.id;
    const c = document.querySelector(`[data-count-for="${{id}}"]`); if (!c) return;
    const n = ta.value.length, limit = Number(c.dataset.limit || 0);
    c.textContent = limit ? `${{n}} / ${{limit}} chars` : `${{n}} chars`; c.classList.toggle('over', limit > 0 && n > limit);
  }}
  document.querySelectorAll('textarea').forEach(ta => {{ refreshCount(ta); ta.addEventListener('input', () => refreshCount(ta)); }});
}})();
</script>'''

if __name__ == "__main__":
    root = pathlib.Path(__file__).resolve().parents[2]
    (root / "docs/launch/posts.md").write_text(md(), encoding="utf-8")
    out = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else root / "docs/launch/launch-kit.html"
    out.write_text(page(), encoding="utf-8")
    print("written", root / "docs/launch/posts.md", out)
