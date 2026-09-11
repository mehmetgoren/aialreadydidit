# Launch post drafts

Drafts for the launch of https://aialreadydidit.com, written 2026-09-11 from the community-rules research of 2026-09-08
(see CLAUDE.md, session log 24). Before posting anything:

- [ ] Grow the catalogue to 20–30 published apps (the front page currently shows 5). Every reader will click "browse".
- [ ] Post from an account with history (fresh accounts are removed by AutoModerator on most subreddits).
- [ ] Fill the `EMAIL_*` values so sign-up verification mails actually arrive.
- [ ] Re-run the CSP crawl and health check after the last deploy.
- [ ] Be online for the first 3–4 hours after each post to answer questions.

Order: Show HN on a Tuesday–Thursday, 14:00–16:00 UTC. Reddit two or three days later, one subreddit per day
(cross-posting the same text the same day gets flagged as spam). Product Hunt and a dev.to article can follow the week after.

---

## 1. Show HN

**Title** (80 chars max; no "we", no exclamation marks, no emoji):

```
Show HN: AI Already Did It – a free store for LLM-generated apps, with prompts
```

Alternatives if the first reads as too long in the preview:

```
Show HN: A free store of LLM-generated apps, so agents check before they build
Show HN: AI Already Did It – check whether an LLM already wrote that app
```

**URL:** `https://aialreadydidit.com`

**First comment** (post it immediately after submitting; HN readers expect the maker's context there):

```
Hi HN. I built this because I kept watching people (and my own agents) regenerate the same small
apps over and over: a CPU temperature monitor, an audio converter, a paint clone. Every rerun
costs tokens, money and energy, and the result usually ends up in a folder nobody else sees.

AI Already Did It is a free repository of applications written by LLMs. Every entry has to pass
three rules: open source (SPDX license plus a LICENSE file in the source that actually matches),
installable (at least one ready-to-run file per declared platform: .deb, .exe, .dmg, .apk,
AppImage, Docker image, web bundle), and free. Each app also records the model that generated it
and the original prompt(s), so you can download it or fork it and rerun the prompt with your own
changes. Uploaded installers are checked against their magic bytes and scanned with ClamAV.

The part I care most about is the agent side. There is a REST endpoint and an MCP server
(https://mcp.aialreadydidit.com/mcp) with a `check_before_building` tool: an agent describes
what it was asked to build and gets a verdict of download, fork or build, with the closest
matches and their licenses. The idea is that a coding agent calls it before writing anything.
Search is hybrid: Postgres full-text plus pgvector with bge-m3 embeddings, fused with reciprocal
rank fusion. No OpenSearch, one database keeps moderation state transactional.

Stack: ASP.NET Core 10, Vue 3, PostgreSQL 18 with pgvector, MinIO, ClamAV, Ollama for embeddings,
all in docker-compose. The whole thing is MIT on GitHub:
https://github.com/mehmetgoren/aialreadydidit

It is early. The catalogue is small and I seeded it with my own generated projects, so what I am
looking for is (a) people uploading the apps their agents already wrote and (b) feedback on
whether the "check before you build" step is something you would actually wire into an agent.
Anonymous search and download need no account; uploading does, and everything goes through a
moderation queue.

Honest limitations: the similarity threshold is tuned by hand on a small corpus, non-English UI
translations were LLM-generated and not reviewed by native speakers, and there is no
reproducibility guarantee that the recorded prompt regenerates the same app.
```

Answers to have ready:

- *"Isn't this just GitHub?"* GitHub has no installable-file rule, no prompt/model record, no license verification, and no
  "is there already one of these" verdict for agents. The store links back to the repository when there is one.
- *"Who moderates?"* Every submission is scanned and reviewed by a human before publishing; trusted uploaders publish new
  versions of existing apps without review. Reports go to the same queue.
- *"How is the savings counter computed?"* Source lines × tokens-per-line × iteration factor unless the uploader supplies
  real numbers; cost, kWh and CO₂ coefficients are visible in the admin settings. It is an estimate and labelled as one.
- *"Why not let anyone upload without an account?"* Accountability for the license and abuse reports.
- *"Malware?"* Magic-byte check on every installer, ClamAV scan, human review, report button, and the source is public.

---

## 2. r/ClaudeAI

Flair: **Built with Claude** (check the current flair list before posting). The subreddit explicitly welcomes
Claude-built showcases; it expects you to say how Claude was used.

**Title:**

```
I built a free store for apps written by LLMs (with the prompts) so agents can check before they build — the whole thing was built with Claude Code
```

**Body:**

```
**What it is:** https://aialreadydidit.com is a free, open-source repository of applications
written by LLMs. Each app ships with its source, an installable file per platform, screenshots,
the model that generated it and the original prompt(s). You can download it, or fork the prompt
and make your own variant.

**Why:** the same small apps get regenerated thousands of times. Publishing them once saves the
next person's tokens and lets an agent reuse instead of rebuild.

**The Claude part:** the app itself (ASP.NET Core 10 backend, Vue 3 frontend, PostgreSQL +
pgvector search, MinIO, ClamAV, MCP server, admin panel, 11 UI languages) was built in Claude
Code sessions with Claude Fable 5.1, from a written brief and a Q&A round of blocking questions.
Claude also wrote the xunit and Vitest suites and the QA pass that found the search-stemming and
security-header issues. The seed apps in the store are earlier Claude Code projects of mine.

**For Claude Code users:** there is an MCP server. Add it with

    claude mcp add --transport http ai-already-did-it https://mcp.aialreadydidit.com/mcp

and use the `reuse_first` prompt. Claude then calls `check_before_building` before writing an
app and gets a download / fork / build verdict with the closest matches.

**Rules for uploads:** open source with a matching LICENSE file (verified), at least one
ready-to-run installer per declared platform, free. Installers are format-checked and scanned.

Source (MIT): https://github.com/mehmetgoren/aialreadydidit

It is early and the catalogue is small. If your Claude sessions produced an app you would let
others install, I would love to see it uploaded. Feedback on the agent flow is very welcome.
```

---

## 3. r/opensource

Rules: a LICENSE is required in the project (MIT, present); self-promotion is allowed "to a degree", so keep it factual
and lead with the license and the code, not the site.

**Title:**

```
AI Already Did It — an MIT-licensed store for LLM-generated apps that verifies the LICENSE file of every upload
```

**Body:**

```
I released the source of a web application I run at https://aialreadydidit.com: a free
repository of applications written by LLMs.

The relevant part for this subreddit is the rule set. Every upload must declare an SPDX license,
and the source archive or linked GitHub/GitLab repository must contain a LICENSE file whose text
matches that license (the detector recognises the common families and refuses mismatches, e.g.
"declared MIT, file says GPL-3.0"). Non-open licenses are rejected at submit time. Each app also
records which model generated it and the prompt(s), which are published alongside the source.

The application itself is MIT: ASP.NET Core 10, Vue 3, PostgreSQL 18 with pgvector for hybrid
keyword + semantic search, MinIO, ClamAV, docker-compose for the whole stack, plus an MCP server
so coding agents can check whether an app already exists before generating it.

Repository: https://github.com/mehmetgoren/aialreadydidit

Feedback on the license-verification approach is welcome — in particular which SPDX identifiers
you would expect to be accepted that currently are not.
```

---

## 4. r/SideProject

**Title:**

```
I made a free app store where every app was written by an AI, with the prompt included
```

**Body:**

```
https://aialreadydidit.com

Every app on it was generated by an LLM. Each listing has the source, an installer per platform,
screenshots, the model and the original prompt. Download it, or copy the prompt and make your own
variant. Everything is open source and free; uploads go through a license check, a malware scan
and a human review.

The twist is an API and an MCP server for coding agents: before an agent builds something, it
asks the store and gets "download this", "fork that" or "go ahead and build". The homepage shows
an estimate of the tokens, money and energy saved by reuse.

Built with Claude Code over about a week. Stack: .NET 10, Vue 3, Postgres + pgvector, MinIO,
ClamAV. Code is MIT on GitHub: https://github.com/mehmetgoren/aialreadydidit

Looking for people who have AI-generated apps sitting in a folder and would upload them.
```

---

## 5. r/vibecoding

Rule: explain how it was built. Same body as r/SideProject with this paragraph prepended:

```
How it was built: one written brief, a round of blocking questions the agent asked back (which
embedding provider, what to do about a project with no LICENSE, default language), then Claude
Code sessions with Claude Fable 5.1. I reviewed and tested in the browser after each session and
sent back a list of findings. Tests (xunit + Vitest) were written by the agent as well.
```

---

## 6. r/coolgithubprojects

Title must contain the repo link; flair is the language.

```
[C# / Vue] AI Already Did It — a free store for LLM-generated apps with prompt, license check and an MCP "check before you build" tool — https://github.com/mehmetgoren/aialreadydidit
```

Body: two lines, the site URL and the MCP add command.

---

## 7. Product Hunt (later)

- Tagline (60 chars): `Check if an AI already wrote that app before you build it`
- First comment: the Show HN comment, trimmed to the first three paragraphs plus the "it is early" paragraph.
- Gallery: `docs/screenshots/home.png`, an app page, the agent check JSON, the wizard's duplicate warning.

## 8. dev.to / Hashnode article (later)

Working title: *"Teaching a coding agent to check before it builds: an MCP tool, pgvector and reciprocal rank fusion"*.
Sections: the problem, the three rules, hybrid search (tsvector + HNSW + RRF, thresholds 0.72 / 0.45 and why bge-m3
changed them), the agent verdict, moderation pipeline (magic bytes, ClamAV, review), what did not work (OpenSearch
considered and rejected, ImageSharp 4 licensing, nginx `$1` reset), what is next.

## Do not post to

r/ChatGPTCoding (weekly promo thread only), r/programming (no "I made this"), r/webdev (Showoff Saturday only),
r/InternetIsBeautiful (no aggregators or stores), r/artificial, r/selfhosted outside the New Project Megathread.
