<h1 align="center">StableProjectorz: Generation of 3D and textures via StableDiffusion</h1>

<p align="center">
  <img src="Assets/_gm/Art/Previews/StableProjectorz-opensource-preview-wide.png" width="100%" alt="StableProjectorz Banner" />
</p>

<p align="center">
  <img src="Assets/_gm/Art/Previews/Github-title.png" width="100%" alt="StableProjectorz Banner" />
</p>

**StableProjectorz** is a tool for texturing 3D models using StableDiffusion.<br>
It also supports generating 3D models from 2D images.<br>

Official page: [StableProjectorz](https://stableprojectorz.com/) <br>
Our Discord server: [here](https://discord.gg/aWbnX2qan2)

---

## 🛠️ Setup & Building
*   **Requirements:** Unity 6000.
*   **Codebase:** Located entirely in `Assets/_gm`.
*   **Build:** Press `Ctrl + Shift + B` -> **Build and Run**.
*   **Note:** Keep "Development Build" unchecked for performance.

## 📝 Contribution Rules
1.  **Nested Prefabs:** Keep UI separated in Prefabs. Don't modify prefabs externally. Save your work directly inside a deepest prefab itself, to prevent merge conflicts.
2.  **Communication:** Prefer singleton-pattern, to talk between scripts. For example  `MyCoolScript.instance`; Avoid drag-and-drop references. Initialize Singletons in `Awake()`.
3.  **Folders:** keep prefabs, scenes, scripts inside subfolders of `Features`. Do not use separate `Prefabs`, `Scenes`, `Scripts` folders.
4.  **Memory:** If you spawned a `RenderTexture` make sure to release it, to aviod memory leaks.
5.  **Variables:** Use `[SerializeField] private` instead of `public`.
6.  **Naming:** `_underscoreForMemberVariables`.

---

## 📂 1. Architecture & Patterns
*   **Scene Management:** We use **Additive Loading**. `Start_Scene_Global` scene loads UI and manager-scenes separately to prevent merge conflicts.
*   **Update Loop:** `Update_callbacks_MGR` dispatches events (Navigation, Depth, Rendering) in a strict order. Using `Update()` for other things.
*   **Workflow:** Use **Nested Prefabs**. Avoid editing Scene files directly; edit the Prefab to keep Git diffs clean.

## 🧊 2. The 3D World
*   **Access:** `ModelsHandler_3D.instance` is the singleton entry point for all 3D data.
*   **Structure:**
    *   `Objs3D_Container`: Maps every sub-mesh to a unique ID for later retrieval.
    *   `UDIMs_Helper`: Scans UVs to detect tiles (1001, 1002, etc.).
*   **Importer:** `ModelsHandler3D_ImportHelper` uses **AssimpNet** to load OBJ/FBX/GLB at runtime.

## 🎥 3. The Camera System
Managed by `UserCameras_MGR`. Culling Planes are fixed (0.25–1000) for float precision.<br>
*   **View Camera**: Renders high-res image for user to see. Also renders a viewport depth texture, for mesh-clicking, etc.
*   **Depth Camera:** Renders black and white image to send to StableDiffusion (not an exact viewport depth).
*   **Content Camera:** Renders the square resolution to send to StableDiffusion (e.g., 512x512).
*   **Normals Camera:** Renders View-Space normals to send to StableDiffusion (Red=Right, Green=Up).
*   **Vertex-Colors Camera:** Renders Colors encoded in mesh-vertexes to send to StableDiffusion.
*   **Multi-View Logic:** We use **Perspective Shift** (via UI Pins) to arrange multiple cameras in one viewport.

## 🎨 4. Rendering Pipeline
*   **ProjectorCamera:** Spawns at the user's viewpoint. "Shines" the AI image inside the `_accumulation_uv_RT` texture.
*   **Target:** `Objects_Renderer_MGR` renders into `_accumulation_uv_RT`.
*   **Texture Arrays:** The target is a **RenderUDIMs** object (Texture Array). Each slice represents one UDIM tile.
*   **The Loop:** Clear Black → Bake Projections (respecting depth) → Dilate (Anti-Seam) → Display on Mesh.

## 🧠 5. AI Integration
*   **Pipeline:** `StableDiffusion_Hub` → `SD_Generate_PayloadMaker` → `SD_Generate_NetworkSender` (Talks to A1111/Forge via JSON).
*   **Storage:** `GenData2D_Archive` stores **Information Objects**, not loose files.
    *   **`GenData2D`**: Remembers the Camera-params (POV), Prompts, Result Textures, and User Masks.
    *   This allows "Reloading" the exact state of a generation later.

## 🧊 6. 3D Generation
Managed by `Gen3D_MGR`.
1.  **Handshake:** Queries external WebUI (Trellis/Hunyuan) for required parameters.
2.  **Dynamic UI:** Parses response to spawn sliders/inputs automatically (`Gen3D_InputPanelBuilder_UI`).
3.  **Generates:** Submits a request to the Webui, with the inputs as JSON.
4.  **Import:** waits for resulting GLB mesh and feeds it to `ModelsHandler_3D`.

## 🖱️ 8. Input System
*   **`KeyMousePenInput`:** Static helper class.
*   **Abstraction:** Unifies Mouse clicks and Tablet Pen pressure into a single API.
*   **Coords:** Tracks cursor in Screen Pixels and Viewport Space (0–1).
