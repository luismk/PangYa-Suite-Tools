# Fresh rendering parity worklist

Reference: `lucaspef/pangya-jp983-src`, commit
`81a6969942fe007bcaccbb33b53b3081e9cee67f`, `src/fresh/source` and
`src/fresh/include`. Animation progression is excluded from this work.

This is a coverage index, not a declaration of full parity. Compilation does not
prove pixel fidelity. Regression fixtures have been added but have not been run,
following the user's instruction.

## Method inventory

`fresh-method-coverage.csv` inventories 598 out-of-line Fr-class definitions from
the 27 cached `fr*.cpp` files. `fresh-method-coverage.sources.json` records source
hashes and the pinned reference commit. Regenerate from the repository root with:

```powershell
python scripts/inventory_fresh_methods.py
```

The script preserves existing audit columns for unchanged declaration identities.
It performs text inventory only: it neither builds nor runs tests. `unreviewed`
means no method-level contract audit has been recorded, even when related renderer
code already exists. `partial` records source comparisons and explicitly lists
remaining work; compiled fixtures are not executed evidence. Inline header methods
are excluded from this first inventory and must be audited separately before full
coverage can be established. This count is not a completion percentage.

The scanner includes nested Fr-owned classes. This adds the five
`FrToolTip::cToolTipFrame` definitions that the initial 593-method inventory missed.
Their asset/style/render contracts are now partially audited against `DrawTooltip`;
compiled spatial and alpha fixtures remain unexecuted.

| Native source | Current implementation area | Remaining audit |
| --- | --- | --- |
| frarea.cpp | Area image setter transitions, clipping, centering, stretching | Runtime clipping combinations and byte parsing edge cases |
| frbutton.cpp | Image button runtime state, images, styles, below images | Complete setter and geometry review |
| frcmdtarget.cpp | Owner-draw callbacks | Distinguish application commands from rendering effects |
| frcombobox.cpp | Combo geometry, text/image list mutations, separate texture vector and selected image | Input-driven selection, retained initialization geometry |
| frcontextmenuctrl.cpp | Menu rows, icons, disabled text, replacements | Full geometry and draw-state comparison |
| frcursor.cpp | Cursor sprite snapshots | Source and cursor-state coverage audit |
| frdesktop.cpp | Wallpaper snapshots, bitmap setters, filename locks, visibility query | Screen reset semantics |
| fredit.cpp | Text snapshots, per-line styles, caret, runtime styles | AddText wrapping and line mutation contracts |
| frelement.cpp | XML document, exact typed resource lookup, entry-scoped include expansion | Full parameter inventory, include failure semantics |
| fremoticon.cpp | Emoticon catalog and inline rendering | Measurement versus drawing edge cases |
| fresh.cpp | Editor initialization and startup resources | Full initialization sequence comparison |
| frform.cpp | Frame text, flags, active modal backdrop, messages, viewport adjustment | Form initialization variants |
| frframe.cpp | Layered frame slices and captions | Complete layer geometry comparison |
| frgaugebar.cpp | Gauge ranges, values, steps, appearance | Derived geometry after movement and resizing |
| frgraphicinterface.cpp | Scoped global alpha, graphics helpers and font callbacks | Font scaling, spacing, text metric and clipping contracts |
| frgroupbox.cpp | Child container handling | Verify creation flags and child coordinates |
| frlistbox.cpp | Owner drawing, dimensions, columns, selection state and half-page selection scrolling | Stateful capacity recalculation and item mutations |
| frmacroitem.cpp | Parent-attached macro expansion | Conditional resource and child ordering coverage |
| frscrollbar.cpp | Drawing, state mutations, follow mode, fractional drag snapshots | Input integration and independent child-window alpha |
| frstatic.cpp | Static captions, clipping, style, colors | Setter ordering and small-font state |
| frtabbutton.cpp | Stateful tabs, retained add/clear geometry, duplicate identifiers, down/up activation, prefix images | Editor input integration and full owner-draw comparison |
| frtext.cpp | Tagged caption and tooltip text helpers | FrTEXT print options and token contracts |
| frtextbutton.cpp | Text segments, colors, clipping | Complete geometry and setter review |
| frtooltip.cpp | Tooltip snapshots and fixed positions | Full sizing and clipping comparison |
| frviewer.cpp | Viewer image, offset, owner drawing, scrollbar, TryOpenViewer | Event-driven cursor state and source-format coverage |
| frwnd.cpp | Visibility/enable transitions, geometry, focus, alpha channels, ordered topmost state | Complete non-animation rendering flag review |
| frwndmanager.cpp | Resource resolution and render ordering | Creation, focus, and ordering contracts |

## Confirmed work still needed

- Extended the pinned source audit to `src/engine/wfont.cpp` and
  `include/engine/wfont.h`. WFixedFont indexes glyphs by Shift-JIS code, adds
  native spacing to advances, treats spaces separately, and reports unscaled cell
  height. The preview adapter now follows those rules, and font-state height no
  longer scales. Synthetic WFT glyph/index/advance and transform fixtures compile
  but remain unexecuted. Native coordinate-mode scaling and raster parity remain
  open. These engine files are outside the Fr-only method inventory.

- `PangyaUiFontState` now scales font callbacks, measured widths and reported
  height together for independent normal/small-font snapshots, with graphics
  transform restoration after exceptions. Its fixture compiles but remains
  unexecuted. The adapter requires a supplied font backend; native font scaling
  rasterization and coordinate-mode behavior remain open. Engine source review
  confirmed that reported cell height stays unscaled.

- Text outline drawing now follows native axial-offset passes followed by
  diagonals and the final foreground pass for normal and small-font paths. Two
  callback-order fixtures compile but remain unexecuted. Native font metrics,
  scaling/spacing and bitmap-target printing remain open.

- Frame descriptions now use zero initialized slice height when no skin geometry
  was loaded, rather than substituting the control height. Missing/present skin
  positioning cases compile but remain unexecuted. Partially missing slice sets
  and retained geometry across layers still require comparison.

- View-focus setter/reset methods now model containing-form targeting, optional
  focus takeover, modal guards, and preserved stop subtrees. Focus snapshots drive
  existing frame/button dimming. The transition fixture compiles but remains
  unexecuted; native capture release and complete editor focus routing remain open.

- AREA cursor handling now honors nonzero `mouse_event`, including a runtime
  setter, and the preview canvas requests the native hand cursor for enabled
  areas. Three state/STA cursor cases compile but remain unexecuted. The method
  inventory records this partial audit; command routing remains open.

- Referenced bounds now carry parent-instance origins through macro/form expansion
  when intersecting absolute AREA clips. Previously correct drawing could report
  an empty or misplaced aggregate bound. XML/runtime clip cases covering drawing,
  aggregate bounds and hit testing compile but remain unexecuted.

- Explicit AREA clips with reversed/empty edges now yield no visible region,
  matching native clipping arithmetic instead of falling back to window bounds.
  Malformed/overflowing values are safely suppressed as well (native malformed
  parsing is undefined). Intersection uses wide edge arithmetic for runtime clip
  snapshots. Five drawing/bounds/hit-test cases compile but remain unexecuted.

- Scrollbar thumb travel now uses the unrounded proportional thumb height,
  matching native `SetBarHnY` before its final position rounding. The fractional
  travel fixtures compile but remain unexecuted. Texture fragments now retain
  fractional heights and positions; public thumb bounds enclose the floating-point
  geometry. Rasterized comparison with the native graphics backend remains open.

- `SetContextMenuList` now clears old per-row selection, disabled state, text
  overrides and hover when rebuilding menu entries, matching native list creation.
  Appearance state is retained. A state/geometry/drawing fixture compiles but
  remains unexecuted.

- Context-menu frame drawing now rejects type-3 layers, matching its distinct
  native draw branches, while ordinary frames retain type-3 support. Two compiled
  rendering cases cover the distinction; they remain unexecuted.

- Additional frame layers now normalize `bottompos` by negating it before anchor
  selection. Negative `bottompos` therefore produces a positive top offset instead
  of incorrectly anchoring below the frame. Five compiled spatial cases cover
  both signs, precedence, and image-imposed minimum layer height; none were run.

- AREA clip storage and enablement are now independent. Moving a window retains
  its initialized absolute AREA clip rather than translating it with the image;
  explicit clips are retained too. The state/drawing/hit-test fixture compiles but
  remains unexecuted. Resource-instance-specific movement still needs review.

- Shared graphics alpha no longer affects desktop wallpapers/background overlays
  or viewer images: native desktop/viewer code draws those outside the GDI texture
  path. Viewer primary alpha and explicit background tint remain in effect. A
  fixture comparing background paths with an ordinary AREA at zero shared alpha
  compiles but remains unexecuted.

- Tooltip nine-slice indexing now follows native `eFrameIdx` row-major order;
  the former column-major indexing transposed edge textures. A nine-color spatial
  regression fixture compiles but remains unexecuted.

- Tooltip drawing now bypasses incoming shared graphics alpha for skins and uses
  its own text alpha, matching `FrToolTip::OnDisplay`'s temporary GDI alpha
  replacement. The prior graphics alpha is restored in a finally block. A fixture
  covering global alpha 0, 0.5 and 1 compiles but remains unexecuted.

- Property editing now preserves signed rectangle extents when moving legacy
  image items; selection no longer clamps negative dimensions to zero. Rectangle
  writes check edge overflow. Size-based elements remain nonnegative. STA
  regression fixtures for both reversed axes compile but remain unexecuted.

- Verified `bongdarishop.xml` contains reversed item edges such as
  `rect="256 20 192 256"` and `rect="0 276 256 61"`. Native `FrGuiItem::Init`
  preserves their origin and signed extents; `FrArea::ClippingArea` uses bitmap
  dimensions when unstretched. Window-rectangle parsing now preserves those
  values, while explicit clip parsing remains strict. Parent-relative tiled-image
  drawing, overflow, bounds and hit-test fixtures compile but remain unexecuted.

- Rendering reference-cycle guards now use resolved definition identity, matching
  exact typed resource lookup. Shared names across types and case-distinct names
  no longer suppress valid nested images. Topmost queues retain the same guard;
  actual cycles are still stopped. Four regression cases compile but remain
  unexecuted, covering normal/topmost expansion and actual cyclic siblings.

- Coordinate audit: `FrWnd::Create` adds the parent origin; ordinary siblings do
  not accumulate image offsets. `FrMacroItem::Init` creates under its parent,
  whereas frame slices and tabs accumulate their own layout widths/heights.
  Referenced-form bounds now include parent-translated children outside the frame,
  and aggregate bounds translate all non-macro parent windows. Macro ancestors do
  not add wrapper offsets. The cross-file drawing/bounds/hit-test fixture compiles
  but remains unexecuted.
- `SetWindowRectangle` models movement, size changes, and immediate-child resize
  notifications. Loaded viewers rebuild scrolling with retained follow-bottom
  behavior, also retained through replacement opens. Fixtures compile but remain
  unexecuted; complete resize contracts for other generated children remain open.

- Combo automatic opening side now survives runtime movement. The default uses
  original XML placement; explicit initialization captures a runtime position.
  Row snapshots preserve that side, while forced directions override it. The
  movement/row-update fixture compiles but remains unexecuted. Native setter
  ordering after forced-direction resizing still needs review.

- `MoveWindow` now moves ordinary XML child windows while compensating nested
  forms so they remain stationary, as native `FrWnd::MoveWindow` requires.
  Scrolling static captions update their window clips without resetting scroll.
  The repeated-movement fixture compiles but remains unexecuted. Resource-expanded
  instance-specific children and complete native resize behavior remain open.

- Text combo state now includes wrapped next/previous selection, independent
  dropdown visibility, row-click closing, focus-loss containment, and opening
  direction. Unfold hit testing rejects disabled combo/button states. The state
  fixture compiles but remains unexecuted; image-combo-specific input transitions
  and editor routing still require review.

- Stateful tab snapshots now retain authoritative hover indices, including null,
  and disable automatic mouse handling when activation is `None`, matching native
  precreation without an activation handler. Tab hit testing also honors disabled
  windows. The rendering/input snapshot fixture compiles but remains unexecuted.

- `PangyaUiTabState` now represents native initialized dimensions, accumulated
  separator widths, first-tab height, clear/reset, first-identifier selection,
  and press/release activation. Applying a snapshot removes conflicting selection
  overrides for that bar. State/geometry fixtures compile but remain unexecuted;
  editor input integration and visual comparison are still outstanding.

- `DeferredDisplayContainers` now models normal-pass suppression and the desktop
  child-only late pass from `FrWnd::DisplayFlag4000_983`. Standalone selected forms
  represent desktop children; layout direct children do likewise. Nested flagged
  containers stay suppressed, and independently topmost children use the normal
  topmost pass. Compiled fixtures cover overlap, skipped container visuals, and
  hidden parents, but remain unexecuted. External RF scene integration and fading
  branches are not implemented (animations remain outside scope).

- Shared graphics alpha is now exposed through `GraphicsAlpha`, with native
  color-alpha truncation applied at managed texture/text draw calls. Primitive
  fills and lines are unaffected. Per-surface state is restored in a finally
  block, including nested renders; default-alpha behavior retains direct image
  paths. Compiled fixtures cover text/window composition, zero-alpha textures,
  unaffected gauge fills, and nested scopes, but remain unexecuted. Caller-owned
  Graphics drawing and cursor-overlay parity require separate review.

- List item hit testing now uses scrolled cell geometry, explicit top-item
  overrides, mouse-event enablement, and optional native dummy-item exclusion.
  General hit testing uses it for populated list previews. Fixtures compile but
  remain unexecuted; editor list data/input wiring remains open.

- List cursor-first/cursor-last helpers retain native per-item rollover flags
  separately from the current under-cursor pointer. Sorting/deletion remap both;
  rendering accepts explicit rollover snapshots, including empty ones. Regression
  fixtures compile but remain unexecuted. Keyboard rollover now models the four
  native navigation directions without changing selection/scrolling; out-of-bounds
  iterator movement is rejected safely. Editor input routing and right-button
  exclusion rectangles still need integration.

- List selection now supports add/delete/clear mutations and optional synchronized
  scrollbar state. Single-selection deletion chooses the next or previous item;
  multi-selection deletion retains other selections. Deleted previous pointers
  are cleared safely rather than reproducing native dangling pointers. Fixtures
  compile but remain unexecuted; data-pointer lookup remains open. Stable sorting
  and explicit permutations now preserve selection identity. Caller-owned drawing
  payloads must be reordered with the returned mapping; scrollbar position stays
  unchanged, matching native sorting. Its fixture is compiled, not executed.

- Frame drawing now rejects blink requests unless the base layer is type 3 and
  its first blink bitmap exists, matching `FrFrame::Init` and `SetBlink`.
  Unsupported blink requests preserve normal focus dimming and base textures.
  Regression fixtures compile but remain unexecuted; no animation was added.

- Normal static-caption replacement resets scrolling and restores the window
  clip even for hidden/previously empty captions. `SetFormMessage` now uses this
  setter, including byte truncation when small-font mode was previously enabled.
  The regression fixture compiles but has not been executed.

- Text-button selection now clamps indices as native `Select(int)` and
  `Unselect(int)` do, stopping segment parsing at the first empty token.
  Caption replacement clears selection and updates retained geometry, including
  the native integer-centering drift after repeated replacements. Geometry and
  state regression fixtures are compiled, not executed. Fractional font metrics
  and native token parsing edge cases still need comparison.
- Text-button selected-text and under-cursor queries now expose the rendered
  segment state. `SetCaptionClippingArea` supports replacing and clearing explicit
  clips; empty clips remain distinct from null. Regression fixtures are compiled
  but unexecuted.

- Extended-gauge arrows now retain independent primary/secondary alpha, matching
  the separate `FrButton` children created in `frgaugebar.cpp`. Recursive secondary
  alpha updates include both arrows. The regression fixture compiles but has not
  been executed.

- `SetFormMessage` now targets the last direct STATIC and EDIT children named
  `message`, using the edit text model to replace and wrap lines. Its regression
  fixtures are compiled but unexecuted.
- `PangyaUiEditText` implements line addition, replacement, retrieval, insertion,
  deletion, character byte limits, and AddText wrapping. Stateful IME width limits,
  automatic input editing, and byte-indexed edge cases still need review. Native
  word-wrap measurement continuation has a compiled, unexecuted fixture.
- `TryOpenViewer` handles failed replacement retention, initial-load failures,
  same-name opens, and scroll reset on successful replacement. Its fixtures are
  compiled but unexecuted; file existence alone does not prove native image-decoder
  equivalence.
- `PangyaUiText.GetPrintData` now exposes literal output and recognized-tag
  exclusion. Tooltip drawing shares its line composition and recognized-token
  predicate. Prefix color parsing and unknown literal tokens have rendering
  fixtures, which remain unexecuted. Native token mutation still needs review.
- Generic runtime resizing currently recomputes some derived control geometry.
  Native setters that retain initialized child rectangles need separate review.
- End-to-end visual verification remains outstanding. Do not infer it from a
  successful build or from unexecuted regression fixtures.

For every remaining method, record its rendering effect, implementation location,
and evidence before marking it covered. Event and command methods may be outside
rendering scope only after their effects on visible state have been checked.
