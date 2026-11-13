# Umbraco.Community.AIProvenance

Umbraco 17 package that records AI involvement in content, per property. `docs/design.md` holds the decisions and data model, `docs/roadmap.md` the build phases, and `docs/development.md` how to run and test.

- Build the roadmap phases in order. Meet each phase's acceptance criteria before the next one starts.
- Ask the user before changing a decision in `docs/design.md`.
- Check an Umbraco or Umbraco.AI API in its source, its docs or the running test site before using it. Record findings in `docs/discovery.md`.
- Use Umbraco.AI's `v17/main` branch and the Umbraco 17 docs. The default branch and docs.umbraco.com show newer versions.
- Write all code from scratch. Don't copy code, samples or API names from the products under Prior art in `docs/design.md`.
- Only the `Umbraco.Community.AIProvenance.UmbracoAI` add-on (Phase 4) references Umbraco.AI. The main package must work without it.
- Keep Management API code in one folder. Umbraco 18 breaks it, and that folder is what changes for the 18 build.
- After a change in `Client/`, run `npm run build` there. The test site serves the built files from `wwwroot`, so unbuilt changes don't show.
