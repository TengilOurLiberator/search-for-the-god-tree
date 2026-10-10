# 0012: TRELLIS.2 approved

- **Date:** 2026-10-10
- **Decided by:** Owner

**Decision.** The owner accepted Meta's DINOv3 licence (access requested and granted on Hugging Face under the owner's account; confirmed to the lead in session). That was the last condition from decision 0006, so **TRELLIS.2 is approved**, in this form only:

- Run through **ComfyUI's built-in TRELLIS.2 nodes** (ComfyUI 0.34.0 or newer; 0.39.0 here), with the weights from `Comfy-Org/TRELLIS.2`.
- Background removal by **BiRefNet**. Never RMBG.
- **Not** Microsoft's own repository or any wrapper that installs `nvdiffrast` or `nvdiffrec`; those are non-commercial.

**Evidence.** No Nvidia rasteriser appears anywhere in the ComfyUI source or its Python packages (searched 2026-10-10). The workflow is `stylekit/workflows/trellis2_image_to_glb.api.json`. The DINOv3 licence (updated 2025-08-19) grants a worldwide royalty-free licence with no limit on commercial use and no terms on outputs; it bars military and similar uses, requires the licence to travel with the model if the model is redistributed (we never commit weights), and lets Meta change its terms.

**What contributors must know.** Using TRELLIS.2 means each contributor accepts the DINOv3 licence themselves. It needs about 24 GB of video memory and up to 30 GB for detailed shapes.

**Known limit.** Spiky, furry, thatched and leafy shapes still break during mesh reduction. Such models are not published until they pass the triangle budget cleanly.

**Affects.** `data/approved-generators.json`, THIRD_PARTY_LICENCES.md, ROADMAP.md, docs/pipeline.md.
