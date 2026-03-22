<p align="center">
  <img src="FolderDiff/Assets/logo.svg" width="100" alt="FolderDiff"/>
</p>

<h1 align="center">FolderDiff</h1>

<p align="center">
  <a href="https://github.com/donzasto/FolderDiff/releases/latest">
    <img src="https://img.shields.io/github/v/release/donzasto/FolderDiff?color=4A90D9&label=release" alt="Release"/>
  </a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/Avalonia-11-7B3FDB?logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xMiAyQTEwIDEwIDAgMCAwIDIgMTJhMTAgMTAgMCAwIDAgMTAgMTAgMTAgMTAgMCAwIDAgMTAtMTBBMTAgMTAgMCAwIDAgMTIgMloiLz48L3N2Zz4=" alt="Avalonia"/>
  <img src="https://img.shields.io/badge/Windows%20%7C%20Linux%20%7C%20macOS-cross--platform-1B2A4A" alt="Platforms"/>
</p>

<br/>

<p align="center"><i>Compare folders and find duplicate files</i></p>

<p align="center">UI is available in English and Russian</p>

<p align="center">
  <img src="FolderDiff/docs/run.png" width="49%" alt="FolderDiff — run"/>
  &nbsp;
  <img src="FolderDiff/docs/result.png" width="49%" alt="FolderDiff — result"/>
</p>

<br/>

### Download

<p align="center">
  <a href="https://github.com/donzasto/FolderDiff/releases/latest/download/FolderDiff-win-x64.zip">
    <img src="https://img.shields.io/badge/Windows-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Windows"/>
  </a>
  &nbsp;
  <a href="https://github.com/donzasto/FolderDiff/releases/latest/download/FolderDiff-linux-x64.tar.gz">
    <img src="https://img.shields.io/badge/Linux-E95420?style=for-the-badge&logo=linux&logoColor=white" alt="Linux"/>
  </a>
  &nbsp;
  <a href="https://github.com/donzasto/FolderDiff/releases/latest/download/FolderDiff-osx-x64.tar.gz">
    <img src="https://img.shields.io/badge/macOS-000000?style=for-the-badge&logo=apple&logoColor=white" alt="macOS"/>
  </a>
</p>

<p align="center">
  <a href="https://github.com/donzasto/FolderDiff/releases">All releases →</a>
</p>

<br/>

### Features

**Modes**

| | Mode | Description |
|---|---|---|
| 📁 | **Compare two folders** | Finds files that differ in content or are missing from one of the folders |
| 🔍 | **Find duplicates** | Searches for identical files inside a selected folder and all its subfolders |

**Comparison methods**

| | Method | Description |
|---|---|---|
| #️⃣ | **By hash (MD5)** | Exact byte-level comparison — slower but reliable |
| 🏷️ | **By name** | Fast check without reading file contents |

**Working with results**
- Checkboxes next to each found file
- **Select old files** — marks all copies except the newest (by modification date)
- **Delete selected** — deletes checked files from disk
- **Delete empty folders** — after deletion, cleans up empty folders recursively

**Exclusions** — folder names to skip during scanning (`.git`, `node_modules`, `bin`, `obj`, etc.)

### Build & run

```bash
dotnet build
dotnet run --project FolderDiff.Desktop
```
