namespace AiAlreadyDidIt.Api.Data.Seed;

/// <summary>Static reference lists inserted (idempotently) at startup by <see cref="ReferenceDataSeeder"/>.</summary>
public static class ReferenceData
{
    public sealed record CategoryDef(string En, string Tr, string? Icon = null, CategoryDef[]? Children = null);

    /// <summary>Three-level software category tree (root → subcategory → leaf). Slugs derive from the English names.</summary>
    public static readonly CategoryDef[] Categories =
    [
        new("Developer Tools", "Geliştirici Araçları", "Cpu",
        [
            new("Code Editors & IDEs", "Kod Editörleri ve IDE'ler"),
            new("CLI Utilities", "Komut Satırı Araçları"),
            new("Build & CI", "Derleme ve CI"),
            new("API & Testing", "API ve Test"),
            new("Database Tools", "Veritabanı Araçları"),
            new("Git & Version Control", "Git ve Sürüm Kontrolü"),
            new("Code Generators & Scaffolding", "Kod Üreteçleri"),
            new("Debugging & Profiling", "Hata Ayıklama ve Profilleme"),
            new("Documentation Tools", "Dokümantasyon Araçları"),
        ]),
        new("System & Utilities", "Sistem ve Araçlar", "Monitor",
        [
            new("Hardware & Sensors", "Donanım ve Sensörler", null,
            [
                new("System Information", "Sistem Bilgisi"),
                new("Hardware Monitoring", "Donanım İzleme"),
                new("Benchmarking", "Performans Testi"),
            ]),
            new("File Management", "Dosya Yönetimi"),
            new("Backup & Sync", "Yedekleme ve Eşitleme"),
            new("Disk & Storage", "Disk ve Depolama"),
            new("Clipboard & Launchers", "Pano ve Başlatıcılar"),
            new("Automation & Scripting", "Otomasyon ve Betikler"),
            new("Package Managers & Installers", "Paket Yöneticileri"),
            new("Terminal & Shell", "Terminal ve Kabuk"),
            new("Power & Battery", "Güç ve Pil"),
        ]),
        new("Productivity & Office", "Verimlilik ve Ofis", "Document",
        [
            new("Notes & Knowledge", "Notlar ve Bilgi"),
            new("To-do & Task Management", "Yapılacaklar ve Görev Yönetimi"),
            new("Calendar & Time Tracking", "Takvim ve Zaman Takibi"),
            new("Documents & Spreadsheets", "Belgeler ve Tablolar"),
            new("PDF Tools", "PDF Araçları"),
            new("Email & Contacts", "E-posta ve Kişiler"),
            new("Presentations & Diagrams", "Sunumlar ve Diyagramlar"),
        ]),
        new("Media", "Medya", "Film",
        [
            new("Audio & Music", "Ses ve Müzik", null,
            [
                new("Players", "Oynatıcılar"),
                new("Editors & Recording", "Düzenleme ve Kayıt"),
                new("Converters", "Dönüştürücüler"),
            ]),
            new("Video", "Video", null,
            [
                new("Players", "Oynatıcılar"),
                new("Editors & Converters", "Düzenleme ve Dönüştürme"),
                new("Screen Recording", "Ekran Kaydı"),
            ]),
            new("Graphics & Design", "Grafik ve Tasarım", null,
            [
                new("Image Editors", "Görüntü Editörleri"),
                new("Viewers & Organisers", "Görüntüleyiciler"),
                new("Vector & Illustration", "Vektör ve İllüstrasyon"),
                new("3D & CAD", "3B ve CAD"),
            ]),
            new("Photography", "Fotoğrafçılık"),
            new("Streaming & Podcasts", "Yayın ve Podcast"),
        ]),
        new("Games", "Oyunlar", "Trophy",
        [
            new("Puzzle & Board", "Bulmaca ve Masa Oyunları"),
            new("Arcade & Action", "Arcade ve Aksiyon"),
            new("Strategy & Simulation", "Strateji ve Simülasyon"),
            new("Card & Casino", "Kart Oyunları"),
            new("Educational Games", "Eğitici Oyunlar"),
            new("Game Tools & Mods", "Oyun Araçları ve Modlar"),
            new("Emulators", "Emülatörler"),
        ]),
        new("Education & Reference", "Eğitim ve Referans", "Reading",
        [
            new("Language Learning", "Dil Öğrenme"),
            new("Flashcards & Study", "Kartlar ve Çalışma"),
            new("Math & Calculators", "Matematik ve Hesap Makineleri"),
            new("Dictionaries & Translation", "Sözlükler ve Çeviri"),
            new("Kids", "Çocuklar"),
            new("Courses & Tutorials", "Kurslar ve Eğitimler"),
        ]),
        new("Finance & Business", "Finans ve İş", "Coin",
        [
            new("Personal Finance & Budgeting", "Kişisel Finans ve Bütçe"),
            new("Invoicing & Accounting", "Fatura ve Muhasebe"),
            new("Inventory & POS", "Envanter ve Satış Noktası"),
            new("CRM & Sales", "CRM ve Satış"),
            new("Crypto & Trading", "Kripto ve Alım Satım"),
            new("Project Management", "Proje Yönetimi"),
            new("HR & Payroll", "İK ve Bordro"),
        ]),
        new("Communication & Social", "İletişim ve Sosyal", "ChatDotRound",
        [
            new("Chat & Messaging", "Sohbet ve Mesajlaşma"),
            new("Video Calls & Meetings", "Görüntülü Görüşme"),
            new("Forums & Communities", "Forumlar ve Topluluklar"),
            new("Social Media Tools", "Sosyal Medya Araçları"),
            new("Bots", "Botlar"),
        ]),
        new("Internet & Networking", "İnternet ve Ağ", "Connection",
        [
            new("Browsers & Extensions", "Tarayıcılar ve Eklentiler"),
            new("Download Managers", "İndirme Yöneticileri"),
            new("Network Tools", "Ağ Araçları"),
            new("Web Servers & Proxies", "Web Sunucuları ve Proxy"),
            new("RSS & Bookmarks", "RSS ve Yer İmleri"),
            new("Web Scrapers & Crawlers", "Web Kazıyıcılar"),
            new("Remote Access", "Uzaktan Erişim"),
        ]),
        new("Security & Privacy", "Güvenlik ve Gizlilik", "Lock",
        [
            new("Password Managers", "Parola Yöneticileri"),
            new("Encryption", "Şifreleme"),
            new("VPN & Firewall", "VPN ve Güvenlik Duvarı"),
            new("Authentication & 2FA", "Kimlik Doğrulama ve 2FA"),
            new("Security Scanners", "Güvenlik Tarayıcıları"),
            new("Privacy Tools", "Gizlilik Araçları"),
        ]),
        new("Science & Engineering", "Bilim ve Mühendislik", "DataAnalysis",
        [
            new("Data Analysis & Visualisation", "Veri Analizi ve Görselleştirme"),
            new("Scientific Computing", "Bilimsel Hesaplama"),
            new("Electronics & Embedded", "Elektronik ve Gömülü"),
            new("GIS & Maps", "CBS ve Haritalar"),
            new("Simulation & Modelling", "Simülasyon ve Modelleme"),
            new("Astronomy & Weather", "Astronomi ve Hava Durumu"),
        ]),
        new("AI & Machine Learning", "Yapay Zeka ve Makine Öğrenmesi", "MagicStick",
        [
            new("Chat Clients & Assistants", "Sohbet İstemcileri ve Asistanlar"),
            new("Agents & Automation", "Ajanlar ve Otomasyon"),
            new("MCP Servers & Tools", "MCP Sunucuları ve Araçları"),
            new("RAG & Knowledge Bases", "RAG ve Bilgi Tabanları"),
            new("Image & Audio Generation", "Görüntü ve Ses Üretimi"),
            new("Model Training & Evaluation", "Model Eğitimi ve Değerlendirme"),
            new("Prompt Tools", "Prompt Araçları"),
        ]),
        new("Home & Lifestyle", "Ev ve Yaşam", "House",
        [
            new("Smart Home & IoT", "Akıllı Ev ve IoT"),
            new("Recipes & Cooking", "Tarifler ve Yemek"),
            new("Shopping & Lists", "Alışveriş ve Listeler"),
            new("Travel & Navigation", "Seyahat ve Navigasyon"),
            new("Hobbies & Collections", "Hobiler ve Koleksiyonlar"),
            new("Family & Pets", "Aile ve Evcil Hayvanlar"),
        ]),
        new("Health & Fitness", "Sağlık ve Fitness", "FirstAidKit",
        [
            new("Workout & Training", "Antrenman"),
            new("Nutrition & Diet", "Beslenme ve Diyet"),
            new("Sleep & Meditation", "Uyku ve Meditasyon"),
            new("Medical & Tracking", "Tıbbi Takip"),
            new("Habits", "Alışkanlıklar"),
        ]),
        new("Web Apps & Templates", "Web Uygulamaları ve Şablonlar", "Compass",
        [
            new("Landing Pages & Portfolios", "Açılış Sayfaları ve Portfolyolar"),
            new("Dashboards & Admin Panels", "Panolar ve Yönetim Panelleri"),
            new("E-commerce & Marketplaces", "E-ticaret ve Pazar Yerleri"),
            new("Blogs & CMS", "Bloglar ve İçerik Yönetimi"),
            new("Forms & Surveys", "Formlar ve Anketler"),
            new("Widgets & Components", "Bileşenler"),
        ]),
        new("Other", "Diğer", "MoreFilled",
        [
            new("Uncategorised", "Kategorisiz"),
        ]),
    ];

    public sealed record PlatformDef(string Code, string Name, string Icon, string Extensions, bool AllowsExternal, string Hint);

    public static readonly PlatformDef[] Platforms =
    [
        new("windows", "Windows", "windows", ".exe,.msi,.msix,.appx,.zip", false, "Download and run the installer. Windows SmartScreen may ask for confirmation for unsigned builds."),
        new("linux", "Linux", "linux", ".deb,.rpm,.appimage,.tar.gz,.tgz,.tar.xz,.flatpak,.snap,.sh,.run", false, "Debian/Ubuntu/Mint: sudo apt install ./file.deb · Fedora: sudo dnf install ./file.rpm · AppImage: chmod +x then run."),
        new("macos", "macOS", "apple", ".dmg,.pkg,.zip,.app.zip", false, "Open the .dmg and drag the app to Applications. Unsigned builds: right-click › Open the first time."),
        new("web", "Web", "web", ".zip,.tar.gz,.tgz", true, "A static bundle to host yourself, or a link to a hosted instance."),
        new("android", "Android", "android", ".apk,.aab", false, "Enable “Install unknown apps” for your browser/file manager, then open the .apk."),
        new("ios", "iOS", "ios", ".ipa", false, "Install with AltStore / Sideloadly or through TestFlight if the uploader provides a link."),
        new("docker", "Docker", "docker", ".tar,.tar.gz,.tgz", true, "docker pull <image> — or load a saved image with docker load -i file.tar."),
        new("cli", "CLI / Script", "terminal", ".zip,.tar.gz,.tgz,.tar.xz,.sh,.ps1,.py,.js,.jar,.whl,.gem,.exe,.deb,.rpm", false, "Extract and run from a terminal. See the README for the exact command."),
    ];

    public sealed record LicenseDef(string SpdxId, string Name, string Family, bool Osi, bool Fsf, bool Allowed = true);

    /// <summary>Common SPDX identifiers. Everything OSI-approved or FSF-libre is allowed; a few restrictive ids are listed but blocked.</summary>
    public static readonly LicenseDef[] Licenses =
    [
        new("MIT", "MIT License", "MIT", true, true),
        new("Apache-2.0", "Apache License 2.0", "Apache-2.0", true, true),
        new("GPL-3.0-only", "GNU General Public License v3.0 only", "GPL-3.0", true, true),
        new("GPL-3.0-or-later", "GNU General Public License v3.0 or later", "GPL-3.0", true, true),
        new("GPL-2.0-only", "GNU General Public License v2.0 only", "GPL-2.0", true, true),
        new("GPL-2.0-or-later", "GNU General Public License v2.0 or later", "GPL-2.0", true, true),
        new("LGPL-3.0-only", "GNU Lesser General Public License v3.0 only", "LGPL-3.0", true, true),
        new("LGPL-3.0-or-later", "GNU Lesser General Public License v3.0 or later", "LGPL-3.0", true, true),
        new("LGPL-2.1-only", "GNU Lesser General Public License v2.1 only", "LGPL-2.1", true, true),
        new("LGPL-2.1-or-later", "GNU Lesser General Public License v2.1 or later", "LGPL-2.1", true, true),
        new("AGPL-3.0-only", "GNU Affero General Public License v3.0 only", "AGPL-3.0", true, true),
        new("AGPL-3.0-or-later", "GNU Affero General Public License v3.0 or later", "AGPL-3.0", true, true),
        new("BSD-2-Clause", "BSD 2-Clause \"Simplified\" License", "BSD-2-Clause", true, true),
        new("BSD-3-Clause", "BSD 3-Clause \"New\" or \"Revised\" License", "BSD-3-Clause", true, true),
        new("0BSD", "BSD Zero Clause License", "0BSD", true, false),
        new("ISC", "ISC License", "ISC", true, true),
        new("MPL-2.0", "Mozilla Public License 2.0", "MPL-2.0", true, true),
        new("EPL-2.0", "Eclipse Public License 2.0", "EPL-2.0", true, true),
        new("EUPL-1.2", "European Union Public License 1.2", "EUPL-1.2", true, true),
        new("Unlicense", "The Unlicense", "Unlicense", true, true),
        new("CC0-1.0", "Creative Commons Zero v1.0 Universal", "CC0-1.0", false, true),
        new("BSL-1.0", "Boost Software License 1.0", "BSL-1.0", true, true),
        new("Zlib", "zlib License", "Zlib", true, true),
        new("WTFPL", "Do What The F*ck You Want To Public License", "WTFPL", false, true),
        new("Artistic-2.0", "Artistic License 2.0", "Artistic-2.0", true, true),
        new("PostgreSQL", "PostgreSQL License", "PostgreSQL", true, true),
        new("MIT-0", "MIT No Attribution", "MIT", true, false),
        new("CC-BY-4.0", "Creative Commons Attribution 4.0 International", "CC-BY-4.0", false, true),
        new("CC-BY-SA-4.0", "Creative Commons Attribution Share Alike 4.0 International", "CC-BY-SA-4.0", false, true),
        new("OSL-3.0", "Open Software License 3.0", "OSL-3.0", true, true),
        new("UPL-1.0", "Universal Permissive License v1.0", "UPL-1.0", true, true),
        new("NCSA", "University of Illinois/NCSA Open Source License", "NCSA", true, true),
        new("Vim", "Vim License", "Vim", false, true),
        new("MulanPSL-2.0", "Mulan Permissive Software License, Version 2", "MulanPSL-2.0", true, false),
        new("CC-BY-NC-4.0", "Creative Commons Attribution Non Commercial 4.0 (not open source)", "CC-BY-NC-4.0", false, false, Allowed: false),
        new("SSPL-1.0", "Server Side Public License v1 (not OSI approved)", "SSPL-1.0", false, false, Allowed: false),
        new("BUSL-1.1", "Business Source License 1.1 (not open source)", "BUSL-1.1", false, false, Allowed: false),
    ];

    public sealed record LlmModelDef(string Vendor, string Name, string? Version, string? Released);

    public static readonly LlmModelDef[] LlmModels =
    [
        new("Anthropic", "Claude Fable", "5.1", "2026-08-01"),
        new("Anthropic", "Claude Opus", "5", "2026-06-01"),
        new("Anthropic", "Claude Sonnet", "5", "2026-06-01"),
        new("Anthropic", "Claude Opus", "4.6", "2026-02-01"),
        new("Anthropic", "Claude Sonnet", "4.6", "2026-02-01"),
        new("Anthropic", "Claude Opus", "4.1", "2025-08-05"),
        new("Anthropic", "Claude Sonnet", "4.5", "2025-09-29"),
        new("Anthropic", "Claude Haiku", "4.5", "2025-10-15"),
        new("Anthropic", "Claude Opus", "4", "2025-05-22"),
        new("Anthropic", "Claude Sonnet", "4", "2025-05-22"),
        new("OpenAI", "GPT-5.4", null, "2026-03-01"),
        new("OpenAI", "GPT-5", null, "2025-08-07"),
        new("OpenAI", "GPT-5 mini", null, "2025-08-07"),
        new("OpenAI", "GPT-4.1", null, "2025-04-14"),
        new("OpenAI", "o3", null, "2025-04-16"),
        new("OpenAI", "Codex", null, "2025-05-16"),
        new("Google", "Gemini 3.1 Pro", null, "2026-02-01"),
        new("Google", "Gemini 3 Flash", null, "2025-12-01"),
        new("Google", "Gemini 2.5 Pro", null, "2025-06-17"),
        new("Google", "Gemini 2.5 Flash", null, "2025-06-17"),
        new("xAI", "Grok 4", null, "2025-07-09"),
        new("Meta", "Llama 4", null, "2025-04-05"),
        new("DeepSeek", "DeepSeek V4", null, "2026-01-01"),
        new("DeepSeek", "DeepSeek R1", null, "2025-01-20"),
        new("Alibaba", "Qwen3 Coder", null, "2025-07-22"),
        new("Alibaba", "Qwen3.8", null, "2026-06-01"),
        new("Mistral", "Devstral 2", null, "2025-12-01"),
        new("Mistral", "Mistral Large", null, "2025-01-01"),
        new("Google", "Gemma 4", null, "2026-03-01"),
        new("OpenAI", "gpt-oss 120b", null, "2025-08-05"),
        new("Other", "Other / unknown", null, null),
    ];

    public sealed record MenuDef(string Name, string? Route, string? Icon, MenuDef[]? Children = null);

    /// <summary>Admin drawer, also seeded as <c>menus</c> rows so role → menu delegation can reference them.</summary>
    public static readonly MenuDef[] AdminMenus =
    [
        new("admin_dashboard", "/admin", "Odometer"),
        new("adm_group_moderation", null, "Stamp",
        [
            new("adm_moderation_queue", "/admin/moderation", "Checked"),
            new("adm_reports", "/admin/reports", "WarningFilled"),
            new("adm_requests", "/admin/requests", "QuestionFilled"),
        ]),
        new("adm_group_catalog", null, "Goods",
        [
            new("adm_apps", "/admin/apps", "Box"),
            new("admin_categories", "/admin/categories", "Files"),
            new("adm_tags", "/admin/tags", "CollectionTag"),
            new("adm_licenses", "/admin/licenses", "Document"),
            new("adm_platforms", "/admin/platforms", "Monitor"),
            new("adm_llm_models", "/admin/llm-models", "MagicStick"),
        ]),
        new("adm_group_members", null, "UserFilled",
        [
            new("app_users", "/admin/users", "User"),
            new("adm_api_keys", "/admin/api-keys", "Key"),
            new("adm_identity_sessions", "/admin/sessions", "Monitor"),
            new("adm_identity_roles", "/admin/roles", "Avatar"),
            new("adm_identity_menus", "/admin/menus", "Menu"),
            new("adm_identity_role_menus", "/admin/role-menus", "Link"),
            new("adm_identity_role_actions", "/admin/role-actions", "Operation"),
        ]),
        new("adm_group_content", null, "Picture",
        [
            new("adm_content_featured", "/admin/featured", "Star"),
            new("adm_content_banners", "/admin/banners", "Picture"),
        ]),
        new("adm_group_stats", null, "DataLine",
        [
            new("adm_stats_overview", "/admin/stats", "DataLine"),
            new("adm_stats_search", "/admin/search-analytics", "Search"),
            new("adm_stats_savings", "/admin/savings", "Coin"),
        ]),
        new("adm_group_system", null, "Setting",
        [
            new("adm_system_settings", "/admin/settings", "Setting"),
            new("adm_identity_audit_log", "/admin/audit-log", "Document"),
            new("adm_system_jobs", "/admin/jobs", "Timer"),
            new("adm_system_health", "/admin/health", "FirstAidKit"),
        ]),
    ];

    public sealed record SettingDef(string Key, string Value, string Group, string Type, string Description);

    public static readonly SettingDef[] Settings =
    [
        new(SettingKeys.AnnouncementText, "", "content", "string", "Announcement bar text shown on every storefront page (empty = hidden)."),
        new(SettingKeys.AnnouncementLink, "", "content", "string", "Optional link for the announcement bar."),
        new(SettingKeys.MinScreenshots, "1", "uploads", "int", "Minimum screenshots per app (the wizard recommends 3)."),
        new(SettingKeys.MaxScreenshots, "10", "uploads", "int", "Maximum screenshots per app."),
        new(SettingKeys.RecommendedScreenshots, "3", "uploads", "int", "Recommended screenshot count shown in the wizard."),
        new(SettingKeys.MaxTagsPerApp, "10", "uploads", "int", "Maximum tags per app."),
        new(SettingKeys.RequireReviewForNewApps, "true", "uploads", "bool", "New apps wait in the moderation queue before they are published."),
        new(SettingKeys.RequireReviewForNewVersions, "true", "uploads", "bool", "New versions of published apps wait for review (trusted uploaders skip this)."),
        new(SettingKeys.AutoRejectInfected, "true", "uploads", "bool", "Automatically reject submissions when ClamAV reports an infection."),
        new(SettingKeys.DuplicateThreshold, "0.72", "search", "decimal", "Cosine similarity above which the uploader is warned about a near duplicate."),
        new(SettingKeys.SearchKeywordWeight, "1.0", "search", "decimal", "Weight of keyword rank in hybrid fusion."),
        new(SettingKeys.SearchSemanticWeight, "1.0", "search", "decimal", "Weight of semantic rank in hybrid fusion."),
        new(SettingKeys.SearchMinSimilarity, "0.35", "search", "decimal", "Semantic results below this cosine similarity are dropped."),
        new(SettingKeys.TokensPerLine, "12", "savings", "decimal", "Estimated tokens an LLM emits per source line (heuristic)."),
        new(SettingKeys.IterationFactor, "3", "savings", "decimal", "Multiplier for the back-and-forth (planning, fixes, re-reads) around the final code."),
        new(SettingKeys.PricePerMillionTokens, "15", "savings", "decimal", "Blended USD price per million tokens used for the savings counter."),
        new(SettingKeys.KwhPerMillionTokens, "0.4", "savings", "decimal", "Estimated kWh per million generated tokens."),
        new(SettingKeys.Co2GramsPerKwh, "400", "savings", "decimal", "Grid CO₂ intensity (g/kWh) used for the savings counter."),
        new(SettingKeys.SavingsBaseTokens, "0", "savings", "int", "Tokens saved before this store existed (added to the counter)."),
        new(SettingKeys.HomeFeaturedCount, "8", "content", "int", "Featured apps shown on the home page."),
        new(SettingKeys.HomeTrendingDays, "7", "content", "int", "Window (days) for the trending list."),
        new(SettingKeys.SeoDefaultDescription, "A free repository of applications written by LLMs. Search before you generate — someone (or some model) may already have built it.", "seo", "text", "Default meta description."),
        new(SettingKeys.ContactEmail, "", "general", "string", "Public contact e-mail shown in the footer."),
        new(SettingKeys.AllowAnonymousReports, "true", "general", "bool", "Let visitors without an account report an app."),
        new(SettingKeys.MaxApiKeysPerUser, "5", "limits", "int", "API keys a member may create."),
        new(SettingKeys.MaxDraftsPerUser, "10", "limits", "int", "Unsubmitted drafts a member may keep."),
    ];
}

public static class SettingKeys
{
    public const string AnnouncementText = "content.announcement_text";
    public const string AnnouncementLink = "content.announcement_link";
    public const string MinScreenshots = "uploads.min_screenshots";
    public const string MaxScreenshots = "uploads.max_screenshots";
    public const string RecommendedScreenshots = "uploads.recommended_screenshots";
    public const string MaxTagsPerApp = "uploads.max_tags";
    public const string RequireReviewForNewApps = "uploads.require_review_new_apps";
    public const string RequireReviewForNewVersions = "uploads.require_review_new_versions";
    public const string AutoRejectInfected = "uploads.auto_reject_infected";
    public const string DuplicateThreshold = "search.duplicate_threshold";
    public const string SearchKeywordWeight = "search.keyword_weight";
    public const string SearchSemanticWeight = "search.semantic_weight";
    public const string SearchMinSimilarity = "search.min_similarity";
    public const string TokensPerLine = "savings.tokens_per_line";
    public const string IterationFactor = "savings.iteration_factor";
    public const string PricePerMillionTokens = "savings.price_per_million_tokens";
    public const string KwhPerMillionTokens = "savings.kwh_per_million_tokens";
    public const string Co2GramsPerKwh = "savings.co2_grams_per_kwh";
    public const string SavingsBaseTokens = "savings.base_tokens";
    public const string HomeFeaturedCount = "content.home_featured_count";
    public const string HomeTrendingDays = "content.home_trending_days";
    public const string SeoDefaultDescription = "seo.default_description";
    public const string ContactEmail = "general.contact_email";
    public const string AllowAnonymousReports = "general.allow_anonymous_reports";
    public const string MaxApiKeysPerUser = "limits.max_api_keys_per_user";
    public const string MaxDraftsPerUser = "limits.max_drafts_per_user";
}
