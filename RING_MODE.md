# Ring texturing mode

Ring mode is an opt-in, Modddif-style way to texture one model from several
angles. The normal cloned-mesh / single-image workflow is unchanged.

## How it works

1. Select only the model you want to texture (not its cloned copies).
2. Position the viewport roughly the way you want the first view to look.
3. Open the `RING TEXTURING` panel at the top of the viewport in `SD` mode.
4. Turn `Ring mode: ON`, choose 2-8 views, then press `Generate ring` (or the
   normal Generate button).
5. The app frames the selected model, orbits one real view camera around it,
   and runs one full-resolution generation per saved pose. All views use the
   same seed and one image per request.
6. Every result is projected back from the pose that produced it. The cameras
   and the camera-count UI are restored when the ring finishes.

`Stop` cancels the current request and ends the ring. The panel status and the
viewport status show which view is being generated.

## Requirements

- A working Stable Diffusion WebUI connection, as for normal generation.
- At least one ControlNet model (Depth or Normal). Ring mode does not bypass
  the normal generation checks.
- Enough VRAM for the chosen resolution. The views are generated sequentially,
  not in parallel.

## Known limitations

- The first build has not been played back on the target machine yet. Projection
  seams between adjacent views may need mask/blending tuning after the first
  real test.
- The same prompt is used for every angle. View-specific prompts are not part
  of this first version.
- The ring is fitted to the bounding sphere of the selected meshes. Very
  non-spherical models will have some empty space in the frame.

## Windows build (GitHub Actions / GameCI)

The workflow is `.github/workflows/build-windows.yml`. It builds Unity
`6000.2.6f2` for `StandaloneWindows64` (Mono) with the method
`spz.BuildTools.BuildScript.BuildWindows` and uploads `build/Windows` as an
artifact.

Before the first build, add these repository secrets under
`Settings > Secrets and variables > Actions`:

- `UNITY_LICENSE` - the contents of the Unity personal `.ulf` file
- `UNITY_EMAIL` - the Unity account email
- `UNITY_PASSWORD` - the Unity account password

For a free personal license, install Unity Hub, sign in, open
`Preferences > Licenses > Add > Get a free personal license`, then copy
`C:\ProgramData\Unity\Unity_lic.ulf` (Windows) into the `UNITY_LICENSE`
secret. The license is not tied to the editor version or the build platform.

Then run `Actions > Build Windows > Run workflow`, or push to `ring-mode`.
Download the `StableProjectorz-Windows` artifact when the run is green.
