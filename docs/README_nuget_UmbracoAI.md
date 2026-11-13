# AI Provenance for Umbraco.AI

[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.AIProvenance.UmbracoAI?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.AIProvenance.UmbracoAI)
[![GitHub license](https://img.shields.io/github/license/liamgold/umbraco-ai-provenance?color=8AB803)](https://github.com/liamgold/umbraco-ai-provenance/blob/main/LICENSE)

![AI Provenance for Umbraco: editors mark properties in the backoffice, Umbraco.AI agents' changes are marked automatically, and readers see an AI label](https://raw.githubusercontent.com/liamgold/umbraco-ai-provenance/main/docs/screenshots/hero.png)

An add-on for [`Umbraco.Community.AIProvenance`](https://www.nuget.org/packages/Umbraco.Community.AIProvenance). When an Umbraco.AI agent changes content, it marks each changed property:

- A value that was empty becomes **AI generated**.
- A value that had content becomes **AI modified**.
- An AI generated mark never changes to AI modified automatically.

Each mark records the AI provider and model.

It works for agents in the Copilot chat, and for Automate workflows that are allowed to change content. Prompt suggestions that an editor applies in the browser are not marked automatically, because Umbraco.AI raises no event for them.

Requires Umbraco 17.8.1 or later and Umbraco.AI 17.8 or later.

Read the [full documentation on GitHub](https://github.com/liamgold/umbraco-ai-provenance#readme).
