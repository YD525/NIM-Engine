# 🔥 NIM Engine

**NIM Engine** is a high-performance, multi-threaded language translation engine.  
It combines AI-powered translation with **context generation**, text segmentation, and **priority-based ordering** to deliver natural and context-aware results.  
It also implements **Placeholder Logic**, allowing users to define custom dictionaries and placeholders for specific words, names, or terms, ensuring consistent translation of key terms across multiple contexts.

---

## ⭐ Aggregation-based Translation

Unlike traditional translation engines that rely on simple batching or brute-force concurrency, **NIM Translator** introduces an **aggregation-based translation model** at the engine level.

Before any AI request is issued, the engine analyzes the **structure and semantic relationships** of the source content.  
Text units that are contextually related, structurally similar, or semantically repetitive are grouped into a single **UnitGroup** and translated as one coherent semantic unit.

As a result:

- Multiple independent translation tasks are merged into a **single AI request**.
- Shared context is fully utilized.
- Redundant token usage is significantly reduced.

Even when explicit “context translation” is disabled by the user, **contextual awareness still exists implicitly**, because related content has already been aggregated and submitted together by the engine.

---

## ⚡ Fine-grained Unit Control

Each **text unit (`BaseUnit`)** is individually tracked and can be controlled via **signals and states**:

- **Created** – unit has been created but not yet processed.
- **Preparing** – unit is being prepared for translation.
- **Translating** – unit is currently being translated.
- **Queued** – unit has been submitted to the output queue.
- **Skipped** – unit is intentionally skipped.
- **Completed** – translation has finished successfully.
- **Failed** – translation failed.

This design allows:

- Precise handling of special cases.
- Skipped translations without affecting unrelated units.
- Real-time user overrides.
- Seamless integration with aggregation logic.

---

## 🚀 Performance & Scalability

With aggregation-based translation, **translation performance no longer scales linearly with the number of text lines**.  
Instead, it scales with **semantic complexity**, making it especially effective for:

- Large-scale scripts.
- Game localization.
- Content with high repetition.

In short: **aggregation-based translation improves performance by eliminating redundant AI work, not by forcing the AI to work faster**.

## Building from source

NIMEngine requires Visual Studio 2022 and the .NET Framework 4.8.1 Developer Pack. `NIMEngine.sln` is
the canonical build entry point; it contains the supported `NIMEngine\NIMEngine.csproj` product project
and its test project.

Restore the locked PackageReference dependencies, run analyzers, and build the x64 Release configuration:

```powershell
.\scripts\Test-RepositoryLayout.ps1
.\scripts\Invoke-Build.ps1 -Configuration Release -Platform x64
.\scripts\Test-PackageAdvisories.ps1
.\scripts\Run-Tests.ps1 -Configuration Release -Platform x64
```

Use `Invoke-Build.ps1 -UpdateLockFiles` only when intentionally changing package versions, then review and commit
both generated `packages.lock.json` files. Generated build outputs are not tracked.

Push a version tag matching `v*` to create `NIMEngine-win-x64.zip` and its SHA256 checksum as GitHub Release
assets. The archive contains the complete x64 Release output required by consuming applications.

---

## ✅ Summary

NIMEngine combines **semantic aggregation**, **fine-grained unit control**, and **placeholder logic** to deliver a translation engine that is:

- Fast
- Context-aware
- Highly customizable
- Scalable to large and repetitive datasets


## Licensing

NIMEngine uses different licenses for different projects within this repository. This repository contains multiple Solutions and Projects, and their licensing terms are not necessarily the same.

- **NIM Engine Plugins:** Licensed under the **Phoenix Plugin License (PPL) 1.0**, a custom license written by YD525 specifically for the NIM Engine plugin ecosystem. See [PLUGIN-LICENSE.txt](NIMEngine.Plugin.Official.NodePack/PLUGIN-LICENSE.txt) for the complete license terms.
- **All other projects:** Licensed under the **GNU Lesser General Public License v3.0 (LGPL-3.0)**.

### Plugin Development Requirements

Anyone developing, modifying, or distributing plugins for NIM Engine MUST comply with the Phoenix Plugin License (PPL) 1.0.

In particular:

- All plugins and derivative plugins must be open-source, with their complete corresponding source code publicly available.
- All plugins and derivative plugins must be distributed free of charge.
- Derivative plugins must continue to comply with PPL 1.0.

These requirements apply to all third-party plugins developed for NIM Engine, not just the official plugins included in this repository.

**Using a different license for a plugin does not exempt it from these requirements.** Please read the complete PPL 1.0 before developing or distributing NIM Engine plugins.