# Launch kit

Copy-ready posts for the launch of https://aialreadydidit.com, refreshed 12 September 2026 from the production database and the community-rules research of 8 September (CLAUDE.md, log 24). Also published as a copy-ready page (artifact “Launch Kit”).

## Pre-flight

| Item | Status | Detail |
|---|---|---|
| Catalogue size | gap | 16 published apps (target 20–30). All are Linux desktop tools, all MIT, all built with Claude, all uploaded by two accounts. Import a few well-known GitHub projects with other platforms or models before posting, or say so openly in the post — the drafts below do. |
| Screenshots | gap | 14 of 16 apps have 3 or more. AdBlock and Audio Format Selector have one each. |
| Ratings | gap | None yet. Rate and review a few apps you did not upload from the demo-style member account so the app pages are not bare. |
| Outgoing e-mail | done | Brevo SMTP live, domain authenticated (DKIM + DMARC), test mail delivered 11 Sep. |
| Google sign-in | done | Client id configured; button renders on /login with zero CSP violations. |
| Security headers and console | done | Headless crawl of 9 production pages on 12 Sep: no console errors, no CSP refusals, no failed requests. |
| Agent endpoints | done | GET /api/v1/agent/check answers with a verdict (fork, HWMonitor 0.687). MCP server initialises and lists all 7 tools. |
| Health | done | API, site, files and MCP hosts answer 200; admin System health all green after the 11 Sep deploys. |
| Savings counter | done | Conservative formula deployed 11 Sep: 10.0 M tokens, about $60 — defensible in a thread. |
| Posting account | to do | Post from your own Reddit and HN accounts, which have history. Fresh accounts that only post links are removed automatically. |
| Be present | to do | Block 3–4 hours after each post to answer. Replies matter more than the post text. |

## Schedule

| When | Where | Note |
|---|---|---|
| Day 0, Tue–Thu, 14:00–16:00 UTC | Show HN | Post the link, then the maker comment within a minute. Do not ask anyone to upvote. |
| Day 0 + 1 h | Mastodon (fosstodon), X, LinkedIn | Short version with the screenshot. Link to the HN thread, not only the site. |
| Day 2 | r/ClaudeAI | Flair “Built with Claude”. Fold in what the HN thread asked about. |
| Day 3 | r/opensource | Lead with the license verification, not the site. |
| Day 4 | r/SideProject | Broad audience, ask for uploads. |
| Day 5 | r/vibecoding | Same body as r/SideProject plus the how-it-was-built paragraph. |
| Day 6 | r/coolgithubprojects | Title carries the repo link; flair C# / Vue. |
| Week 2 | Product Hunt, dev.to article, r/selfhosted megathread, awesome-mcp-servers PR | After the first wave of feedback is folded into the site. |
| Later | r/LocalLLaMA, r/mcp | Only with a local-model angle (bge-m3 on Ollama) and a short technical post. |

---

## Show HN

Title ≤ 80 chars, starts with “Show HN:”, no “we”, no exclamation marks. Submit the URL, then post the maker comment immediately.

URL: `https://aialreadydidit.com`

**Title**

```
Show HN: AI Already Did It – a free store for LLM-generated apps, with prompts
```

Alternative:

```
Show HN: A free store of LLM-generated apps, so agents check before they build
```

Alternative:

```
Show HN: AI Already Did It – check whether an LLM already wrote that app
```

**Maker comment**

```
Hi HN. I built this because I kept watching people (and my own agents) regenerate the same small apps over and over: a CPU temperature monitor, an audio converter, a paint clone. Every rerun costs tokens, money and energy, and the result usually ends up in a folder nobody else sees.

AI Already Did It is a free repository of applications written by LLMs. Every entry has to pass three rules: open source (SPDX license plus a LICENSE file in the source that actually matches), installable (at least one ready-to-run file per declared platform: .deb, .exe, .dmg, .apk, AppImage, Docker image, web bundle), and free. Each app also records the model that generated it and the original prompt(s), so you can download it or fork it and rerun the prompt with your own changes. Uploaded installers are checked against their magic bytes and scanned with ClamAV.

The part I care most about is the agent side. There is a REST endpoint and an MCP server (https://mcp.aialreadydidit.com/mcp) with a check_before_building tool: an agent describes what it was asked to build and gets a verdict of download, fork or build, with the closest matches and their licenses. The idea is that a coding agent calls it before writing anything. Search is hybrid: Postgres full-text plus pgvector with bge-m3 embeddings, fused with reciprocal rank fusion. No OpenSearch; one database keeps moderation state transactional.

Stack: ASP.NET Core 10, Vue 3, PostgreSQL 18 with pgvector, MinIO, ClamAV, Ollama for embeddings, all in docker-compose. The whole thing is MIT on GitHub: https://github.com/mehmetgoren/aialreadydidit

It is early. There are 16 apps today, all Linux desktop tools and mostly mine, so what I am looking for is (a) people uploading the apps their agents already wrote, for any platform, and (b) feedback on whether the "check before you build" step is something you would actually wire into an agent. Anonymous search and download need no account; uploading does, and everything goes through a moderation queue.

Honest limitations: the similarity threshold is tuned by hand on a small corpus, the savings counter is an estimate (it caps uploader-claimed token totals and counts only half of the unique downloads as an avoided generation), the non-English UI translations were LLM-generated and not reviewed by native speakers, and there is no guarantee that the recorded prompt regenerates the same app.
```

Answers to have ready:

- *Isn't this just GitHub?* GitHub has no installable-file rule, no prompt and model record, no license verification, and no “is there already one of these” verdict for agents. The store links back to the repository when there is one.
- *Who moderates?* Every submission is scanned and reviewed by a human before publishing; trusted uploaders publish new versions of existing apps without review. Reports go to the same queue.
- *How is the savings counter computed?* Source lines × tokens per line × an iteration factor, unless the uploader supplies a real total, which is capped at five times that estimate. Only unique downloaders count, and only half of them are assumed to have skipped a generation. Price, kWh and CO₂ coefficients are visible in the admin settings. It is labelled as an estimate.
- *Why not let anyone upload without an account?* Accountability for the license and for abuse reports.
- *Malware?* Magic-byte check on every installer, ClamAV scan, human review, report button, and the source is public.
- *Why all Linux, why all Claude?* Because the first uploads are my own projects. The platform list covers Windows, macOS, web, Android, iOS, Docker and CLI, and the model list has every major vendor. That gap is exactly why I am posting.

---

## r/ClaudeAI

Flair: **Built with Claude**. 
Showcases built with Claude are explicitly welcome; say how Claude was used. Check the current flair list before posting.

**Title**

```
I built a free store for apps written by LLMs (with the prompts) so agents can check before they build — the whole thing was built with Claude Code
```

**Post body**

```
**What it is:** https://aialreadydidit.com is a free, open-source repository of applications written by LLMs. Each app ships with its source, an installable file per platform, screenshots, the model that generated it and the original prompt(s). You can download it, or fork the prompt and make your own variant.

**Why:** the same small apps get regenerated thousands of times. Publishing them once saves the next person's tokens and lets an agent reuse instead of rebuild.

**The Claude part:** the app itself (ASP.NET Core 10 backend, Vue 3 frontend, PostgreSQL + pgvector search, MinIO, ClamAV, MCP server, admin panel, 11 UI languages) was built in Claude Code sessions with Claude Fable 5.1, from a written brief and a Q&A round of blocking questions. Claude also wrote the xunit and Vitest suites and the QA pass that found the search-stemming and security-header issues. The 16 apps in the store today are earlier Claude Code projects of mine — Linux desktop tools built with Fable 5.1 and Opus 5.

**For Claude Code users:** there is an MCP server. Add it with

    claude mcp add --transport http ai-already-did-it https://mcp.aialreadydidit.com/mcp

and use the `reuse_first` prompt. Claude then calls `check_before_building` before writing an app and gets a download / fork / build verdict with the closest matches.

**Rules for uploads:** open source with a matching LICENSE file (verified), at least one ready-to-run installer per declared platform, free. Installers are format-checked and scanned.

Source (MIT): https://github.com/mehmetgoren/aialreadydidit

It is early and the catalogue is small and Linux-only so far. If your Claude sessions produced an app you would let others install, on any platform, I would love to see it uploaded. Feedback on the agent flow is very welcome.
```

---

## r/opensource

A LICENSE in the repo is required (MIT, present). Self-promotion is allowed “to a degree”: lead with the license verification and the code, not the site.

**Title**

```
AI Already Did It — an MIT-licensed store for LLM-generated apps that verifies the LICENSE file of every upload
```

**Post body**

```
I released the source of a web application I run at https://aialreadydidit.com: a free repository of applications written by LLMs.

The relevant part for this subreddit is the rule set. Every upload must declare an SPDX license, and the source archive or linked GitHub/GitLab repository must contain a LICENSE file whose text matches that license (the detector recognises the common families and refuses mismatches, e.g. "declared MIT, file says GPL-3.0"). Non-open licenses are rejected at submit time. Each app also records which model generated it and the prompt(s), which are published alongside the source.

The application itself is MIT: ASP.NET Core 10, Vue 3, PostgreSQL 18 with pgvector for hybrid keyword + semantic search, MinIO, ClamAV, docker-compose for the whole stack, plus an MCP server so coding agents can check whether an app already exists before generating it.

Repository: https://github.com/mehmetgoren/aialreadydidit

Feedback on the license-verification approach is welcome — in particular which SPDX identifiers you would expect to be accepted that currently are not.
```

---

## r/SideProject

Built for exactly this; low friction. Ask for uploads and feedback.

**Title**

```
I made a free app store where every app was written by an AI, with the prompt included
```

**Post body**

```
https://aialreadydidit.com

Every app on it was generated by an LLM. Each listing has the source, an installer per platform, screenshots, the model and the original prompt. Download it, or copy the prompt and make your own variant. Everything is open source and free; uploads go through a license check, a malware scan and a human review.

The twist is an API and an MCP server for coding agents: before an agent builds something, it asks the store and gets "download this", "fork that" or "go ahead and build". The homepage shows a conservative estimate of the tokens, money and energy saved by reuse.

Built with Claude Code over about a week. Stack: .NET 10, Vue 3, Postgres + pgvector, MinIO, ClamAV. Code is MIT on GitHub: https://github.com/mehmetgoren/aialreadydidit

16 apps so far, all Linux desktop tools and mostly mine. Looking for people who have AI-generated apps sitting in a folder, for any platform, and would upload them.
```

---

## r/vibecoding

Allowed only if the post explains how it was built: tools, workflow, code. Same body as r/SideProject with the paragraph below first.

**Title**

```
How I built a store for vibe-coded apps with Claude Code (and why it asks agents to check before they build)
```

**Post body**

```
How it was built: one written brief, a round of blocking questions the agent asked back (which embedding provider, what to do about a project with no LICENSE, default language), then Claude Code sessions with Claude Fable 5.1. I reviewed and tested in the browser after each session and sent back a list of findings. Tests (xunit + Vitest) were written by the agent as well.

https://aialreadydidit.com

Every app on it was generated by an LLM. Each listing has the source, an installer per platform, screenshots, the model and the original prompt. Download it, or copy the prompt and make your own variant. Everything is open source and free; uploads go through a license check, a malware scan and a human review.

The twist is an API and an MCP server for coding agents: before an agent builds something, it asks the store and gets "download this", "fork that" or "go ahead and build". The homepage shows a conservative estimate of the tokens, money and energy saved by reuse.

Stack: .NET 10, Vue 3, Postgres + pgvector, MinIO, ClamAV. Code is MIT on GitHub: https://github.com/mehmetgoren/aialreadydidit

16 apps so far, all Linux desktop tools and mostly mine. Looking for people who have AI-generated apps sitting in a folder, for any platform, and would upload them.
```

---

## r/coolgithubprojects

Flair: **C#**. 
Title must contain the repo link; flair is the language.

**Title**

```
[C# / Vue] AI Already Did It — a free store for LLM-generated apps with prompt, license check and an MCP “check before you build” tool — https://github.com/mehmetgoren/aialreadydidit
```

**Post body**

```
Live: https://aialreadydidit.com
MCP: claude mcp add --transport http ai-already-did-it https://mcp.aialreadydidit.com/mcp
```

---

## Mastodon / X / LinkedIn

Post one hour after Show HN, attach docs/screenshots/home.png, link the HN thread.

**Title**

```
Before you ask an AI to build it, check whether AI already did it.
```

**Post text**

```
Before you ask an AI to build it, check whether AI already did it.

I built a free, open-source store for apps written by LLMs: source + installer + the original prompt, license-verified and virus-scanned. There is an MCP server so coding agents check the store before they generate.

Site: https://aialreadydidit.com
Code (MIT): https://github.com/mehmetgoren/aialreadydidit
Discussion on HN: <paste the thread link>

#opensource #mcp #claudecode
```

---

## Product Hunt (week 2)

Tagline ≤ 60 characters. Gallery: home.png, an app page, the agent-check JSON, the wizard's duplicate warning.

**Title**

```
Check if an AI already wrote that app before you build it
```

**First comment**

```
Hi HN. I built this because I kept watching people (and my own agents) regenerate the same small apps over and over: a CPU temperature monitor, an audio converter, a paint clone. Every rerun costs tokens, money and energy, and the result usually ends up in a folder nobody else sees.

AI Already Did It is a free repository of applications written by LLMs. Every entry has to pass three rules: open source (SPDX license plus a LICENSE file in the source that actually matches), installable (at least one ready-to-run file per declared platform: .deb, .exe, .dmg, .apk, AppImage, Docker image, web bundle), and free. Each app also records the model that generated it and the original prompt(s), so you can download it or fork it and rerun the prompt with your own changes. Uploaded installers are checked against their magic bytes and scanned with ClamAV.

The part I care most about is the agent side. There is a REST endpoint and an MCP server (https://mcp.aialreadydidit.com/mcp) with a check_before_building tool: an agent describes what it was asked to build and gets a verdict of download, fork or build, with the closest matches and their licenses. The idea is that a coding agent calls it before writing anything. Search is hybrid: Postgres full-text plus pgvector with bge-m3 embeddings, fused with reciprocal rank fusion. No OpenSearch; one database keeps moderation state transactional.

It is early. There are 16 apps today, all Linux desktop tools and mostly mine, so what I am looking for is (a) people uploading the apps their agents already wrote, for any platform, and (b) feedback on whether the "check before you build" step is something you would actually wire into an agent. Anonymous search and download need no account; uploading does, and everything goes through a moderation queue.
```

---

## dev.to / Hashnode article (week 2)

Working title: *Teaching a coding agent to check before it builds: an MCP tool, pgvector and reciprocal rank fusion*. Sections: the problem, the three rules, hybrid search (tsvector + HNSW + RRF, thresholds 0.72 / 0.45 and why bge-m3 changed them), the agent verdict, the moderation pipeline (magic bytes, ClamAV, review), the conservative savings formula, what did not work (OpenSearch considered and rejected, ImageSharp 4 licensing, nginx `$1` reset), what is next.

## Do not post to

- r/ChatGPTCoding: self-promotion only in the weekly thread, FOSS included
- r/programming: bans “I made this” project posts and aggregators
- r/webdev: project posts only on Showoff Saturday
- r/InternetIsBeautiful: forbids aggregators, collections and stores
- r/artificial: first post cannot be promotional
- r/selfhosted (as a thread): new projects go to the New Project Megathread only
