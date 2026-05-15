# EcoHub Wiki — Upload Guide

This folder contains the full **GitHub Wiki** for the EcoHub project, ready to be pushed to your repository's wiki.

---

## 📁 Files in this folder

| File | GitHub Wiki page |
|---|---|
| `Home.md` | The landing page (required name — don't rename) |
| `Getting-Started.md` | Getting Started |
| `Architecture.md` | Architecture |
| `Database-Schema.md` | Database Schema |
| `API-Reference.md` | API Reference |
| `Blazor-Web-App.md` | Blazor Web App |
| `WPF-Admin-App.md` | WPF Admin App |
| `Configuration.md` | Configuration |
| `Deployment.md` | Deployment |
| `FAQ.md` | FAQ & Troubleshooting |
| `_Sidebar.md` | Custom sidebar (special name — don't rename) |

All internal links use the GitHub Wiki format `[Page Title](Page-Name)` (spaces become hyphens).

---

## 🚀 Upload to GitHub — Option A (clone wiki repo)

GitHub hosts every wiki as a separate git repo at `https://github.com/<owner>/<repo>.wiki.git`.

### Step 1 — Enable Wiki on the repository

On GitHub: **Settings → Features → Wikis** (must be checked). Create the very first page once via the web UI (e.g. click "Create the first page" → Save). This initialises the wiki git repo.

### Step 2 — Clone the wiki repo somewhere separate

```powershell
cd C:\Users\victo\Desktop
git clone https://github.com/<you>/EcoHub.wiki.git
```

### Step 3 — Copy the wiki files

```powershell
Copy-Item C:\Users\victo\Desktop\EcoHub\wiki\*.md C:\Users\victo\Desktop\EcoHub.wiki\ -Force
```

### Step 4 — Commit and push

```powershell
cd C:\Users\victo\Desktop\EcoHub.wiki
git add .
git commit -m "docs(wiki): full EcoHub project wiki"
git push origin master     # or 'main' depending on your account
```

Refresh your repository's **Wiki** tab — all pages will be live.

---

## 🚀 Upload to GitHub — Option B (manual paste)

If you prefer the web UI:

1. Go to **your-repo → Wiki → New Page**.
2. Set the page title to the file name without `.md` (e.g. `Getting Started`).
3. Copy-paste the file's content.
4. Click **Save Page**.
5. Repeat for every file.
6. For `_Sidebar.md`: create a page titled exactly `_Sidebar` and paste its content — GitHub renders it as the right-hand navigation.

---

## 🧭 Updating the wiki

After your first push, edits can be made either by:

- **Web UI** — click "Edit" on any page; or
- **Locally** — `cd EcoHub.wiki`, edit files, `git commit`, `git push`.

---

## 💡 Tips

- Wiki files cannot be placed in subfolders — everything must be in the root of the `.wiki` repo.
- `Home.md` is the landing page. Don't rename it.
- `_Sidebar.md` is the custom sidebar. `_Footer.md` is the custom footer.
- Images can be dragged into any wiki page in the GitHub UI — they're stored in the same wiki repo.
- Relative links like `[Program.cs](../EcoHub.API/Program.cs)` won't work on the wiki (wiki is a separate repo). Replace them with absolute GitHub URLs if needed, or keep them only in the **main** repo's copy.
