# PangYa-Suite-Tools
Advanced PangYa File Suite Editor written in C#

[![Build Status](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://microsoft.com/windows)
[![Project Stage](https://img.shields.io/badge/Stage-In--Development-orange.svg)]()

---

## 🇺🇸 English

**Pangya Suite Tools** (or *Pangya Studio Tools*) is an integrated ecosystem for advanced reverse engineering and modification, developed entirely in **C# (.NET 10)**. This solution centralizes reading, editing, converting, and compiling multiple native file formats used by both the client and server of the game **PangYa** (such as `.PAK` structures, `.IFF` tables, and patch lists).

### Server database configuration

The server data layer uses Entity Framework Core while retaining the existing PangYa stored procedures. Select SQL Server, MySQL, or PostgreSQL with `Database:Engine` in `PangyaAPI/PangyaAPI.Network/appsettings.json` and provide the connection through `ConnectionStrings:Pangya`. Prefer the `ConnectionStrings__Pangya` environment variable for credentials. Database migrations are never applied during server startup; SQL Server baseline generation and adoption instructions are in `PangyaAPI/PangyaAPI.Migrations.SqlServer/README.md`.

The project is built on top of a high-performance API (`PangyaAPI`) and a rich **Windows Forms** graphical interface, utilizing modern asynchronous Task-based operations to ensure heavy disk I/O and cryptographic tasks run smoothly in the background, keeping the UI fully responsive.

### 🗺️ Module Overview
- [x] **PangyaAPI.PAK (`FrmPakMaker.cs`)**: Surgical data package manipulation. Individual or batch extraction, dynamic file injection/merging, and full multi-region XTEA algorithm support.
- [x] **PangyaAPI.PAK Sync (`FrmPakDiff.cs`)**: Cross-client Multi-PAK structural synchronization tool to compare and isolate missing, modified, or identical files between different clients.
- [x] **PangyaAPI.IFF**: Multi-region, schema-driven parser, indexed catalog, and editor for game data tables (`Character.iff`, `Item.iff`, etc.). The former JP-only marshalled model package has been replaced by validated `IffRecord` schemas.
- [x] **PangyaAPI.UpdateList**: Utility for viewing, verifying, generating, and signing encrypted XML patch lists for the game Launcher/Updater. In the Viewer tab, load an update list, select the PangYa root folder, and choose **Verify files** to compare every listed file's CRC32; matches, mismatches, missing files, invalid paths, and read errors are shown without scanning unrelated local files. The generator can optionally create optimal-compression packages beside `updatelist`, flattening client-relative paths with underscores and appending each file's deterministic 1-based scan order (for example, the second file `GameGuard/GameGuard.des` becomes `GameGuard_GameGuard.des_2.zip`). The Package column records that exact ZIP filename. Unchanged packages are retained by SHA-256 comparison, changed packages are replaced atomically, and the preference also applies to monitored regenerations.
- [x] **PangyaAPI.UI**: Reusable PangYa UI and shop XML document models, atomic editing, resource resolution, image decoding, rendering, geometry, and hit-testing.
- [x] **PangYa UI Editor**: Opens extracted `ui/*.xml` layouts in an element tree, renders image assets and reusable frame/macro definitions referenced across XML files on a zoomable canvas, preserves intrinsic image dimensions unless `stretch=1` is declared, previews button states, debug bounds, and checkbox-selected `#ifdef` elements, tolerates common legacy XML defects, and atomically edits source-backed element properties. Its layout behavior is compatible with [Saeroun](https://github.com/retreev/Saeroun).

  To inspect a Fresh viewer, open the extracted Japanese client's `ui/alchemy.xml` and select `AlchemyHelp`. The `desc_view` item has `use_wheel="true"` and shows a scrollbar guide when the scrollbar sprites are available. In a copy of the XML, add `<param name="image" var="viewer_example.png" />` inside that item and place a 458 × 900 PNG with that filename in the extracted asset tree. Reopen the copy and use the mouse wheel or drag the scrollbar thumb to reveal the lower part of the image.

  API callers can supply a logical canvas rectangle in `PangyaUiRenderOptions.CaptionClips` for a `TEXTBUTTON` to reproduce `FrTextButton::SetClippingArea`. The clip limits drawing while preserving the control's layout and segment selection geometry.

  For runtime `FrStatic::SetTextColor` changes, set the node's foreground and outline pair in `PangyaUiRenderOptions.StaticTextColors`. The disabled appearance uses a white outline. Fresh's manager text path forces caption alpha to 255 while preserving RGB colors.
  `StaticTextStyles` supplies runtime `SetTextStyle` values while preserving the initialized outline color. `SmallFontCaptions` selects the `SetCaption11` path, whose text bypasses the normal 100-byte caption limit.

  `PangyaUiImageButtonState.Images` stores independent `normal`, `over`, `selected`, and `blink` image overrides for `FrButton::SetButtonImg`. Use `ReplacementMode` to identify the most recently replaced slot when its dimensions should become the control's dimensions. `ReplacementImage` can supply that slot directly, with the stored slot image retained if the named image is unavailable. An active `Blink` uses the overridden blink image when available.
  Its optional `Style` models `FrButton::SetStyle`, using 1 for normal tinting, 2 for state images, and 3 for always-on drawing.

  Add an open modal form to `PangyaUiRenderOptions.ModalForms` to preview Fresh's black backdrop at alpha 110 before the form is drawn. The backdrop covers the logical canvas. Forms declaring the XML `modal` parameter follow Fresh's modeless flag behavior and omit the backdrop.
  `FormFlags` supplies runtime flags from `FrForm::Open` or `SetFlag`. Its `0x4` modeless bit overrides XML modeless state when deciding whether to draw an active modal backdrop.

  `PangyaUiRenderOptions.EditOpacity` controls each edit control's background, border, and scrollbar alpha. Text and caret colors follow the separate rules in `FrEdit::OnDraw`.

  `PangyaUiRenderOptions.HiddenNodes` reproduces runtime window hiding for drawing and hit-testing. Hiding a parent also hides its descendants, selection outline, and tooltip.
  `VisibleNodes` shows controls initialized as invisible in XML. Parent visibility still applies, and `HiddenNodes` takes precedence when a node occurs in both sets.

  `PangyaUiRenderOptions.TextButtonCaptions` supplies runtime `FrTextButton::SetCaption` text, including pipe-separated segments. Drawing, measured bounds, and segment hit-testing use the override.
  `ContextMenuItemTexts` changes individual context-menu labels without rebuilding their original geometry, matching `SetSelectMenuText`. Overrides retain the existing 31-byte display limit and preserve complete Shift-JIS characters.

  `PangyaUiRenderOptions.Tooltips` maps controls to `PangyaUiTooltipPreview` values for runtime tooltip text, frame style, and fixed placement. For example, `new PangyaUiTooltipPreview("Preview", Style: 1, Position: new Point(40, 30))` places the tooltip at logical canvas position (40, 30). Omit `Position` for pointer-relative placement. An empty override text suppresses the tooltip.

  `PangyaUiRenderOptions.AreaBitmapImages` supplies caller-owned images for `FrArea::SetBgImg(const Bitmap*)`. A bitmap override takes precedence over the area's named resource and participates in intrinsic sizing, centering, stretching, clipping, and bounds measurement. Keep supplied images alive while rendering and dispose them when their owner is finished.
  `AreaClippingEnabled` models `FrArea::EnableClipping` independently of `AreaClipOverrides`. An explicit false disables both XML and override clipping. An explicit true enables the override rectangle, XML clip rectangle, or control bounds in that order.

  `PangyaUiRenderOptions.EditLineStyles` supplies the per-line styles stored by `FrEdit::AddLine` and `SetLine`. Styles are indexed alongside `EditLines`. Each line starts with its supplied style, falling back to the control's font style for callers supplying text alone.
  `EditEmoticonStyles` models `ES_EMOTICON` for edit text measurement and caret placement. Disabling it measures literal token text; read-only drawing still uses emoticon images, matching Fresh.
  `PangyaUiListPreview.ItemSize` changes item dimensions without recalculating column capacity, matching `FrListBox::SetItemSize`. Supply `ColumnCapacity` when representing an explicit column recalculation. Owner-draw callbacks receive every item in the retained columns, including cells extending beyond the control width.
  Edit text also supports Fresh's comma-separated style and raw hexadecimal color controls, such as `\cT,ff00ff00\c`. Measurement, password masking, and caret positioning exclude these controls.

  `PangyaUiRenderOptions.EditKeyEvents` models the key-event flag changed by `FrEdit::SetReadOnly` and controls caret eligibility. `EditReadOnlyStyles` overrides the XML read-only text style, matching enabling or disabling `ES_READ_ONLY`; it changes alignment and emoticon rendering independently of keyboard input.
  `EditTextColors` supplies independent `SetFontColor` and `SetFontColor2` overrides through `PangyaUiEditTextColors`. Edit text, inline color resets, and combo dropdown rows use the supplied colors.

  `PangyaUiRenderOptions.WindowOffsets` supplies runtime movement relative to XML positions. Parent offsets move their descendants, and child offsets add to that movement. Drawing, geometry, and hit-testing use the same offsets. Macro instances keep Fresh's behavior of creating children directly under the parent and do not apply an instance offset.

  `PangyaUiRenderOptions.WindowSizes` supplies runtime window sizes for layout, clipping, and hit-testing. Viewer and edit scroll ranges use the resized height. Explicit button sizes take precedence over image dimensions, and area stretching uses the resized bounds. Negative dimensions are treated as zero.
  `ScrollbarGuides` models `FrScrollBar::SetGuideVisible`, including empty edit controls and lists. Viewers show guides by default, while other controls default to hiding idle guides.
  `TabSeparatorWidths` models `FrTabButton::SetSepWidth` with fractional spacing shared by drawing, bounds, and hit-testing. Negative widths are clamped to zero, and non-finite overrides use the configured spacing.
  Tabs can declare a `PangyaUiTab.Identifier`. `SelectedTabIdentifiers` and `UnselectedTabIdentifiers` model `Select` and `UnSelect` using the first matching identifier; missing identifiers preserve the existing state. Undeclared identifiers default to the tab position. Positional selection overrides take priority over identifier selection, and deselection is applied afterward.
  `PangyaUiTab.FromImagePrefix(prefix, identifier, size)` represents Fresh's single-name `AddTab` overload, resolving `prefix_n`, `prefix_o`, and `prefix_d`. Omit `size` to use the normal image dimensions.
  Resource catalogs follow nested INCLUDE elements even when the included files were not separately supplied. Includes expand in place, with cycle/depth guards. `Load(paths, includeRoot)` bounds implicit file reads to the selected resource root; without it, supplied files' directories define the bounds.
  Reusable resource identifiers match exact names, including case and surrounding spaces, as in Fresh. Windows file paths remain case-insensitive. For duplicate names of the same type, the first visible definition in catalog/include order wins.
  Catalog lookup rejects unrelated element types with the same name. Supported preview mappings include frame references for forms/context menus and bitmap aliases for image areas/buttons.
  `PangyaUiResourceCatalog.LoadEntry(entryPath, availablePaths, includeRoot)` limits definitions to the entry XML and its include chain. Available files assist path validation and optional emoticon loading; unrelated files do not become global definitions. `Load` remains available for explicit multi-file catalogs.
  The editor uses `all.xml` as its resource entry when available, falling back to the initially opened file. Browsing source files does not register unrelated definitions, and referenced-file windows retain the originating resource entry.
  After saving a referenced XML in another window, use **Reload** in the original window to rebuild its include-scoped catalog and display the saved definitions. Catalog loading runs off the UI thread.
  Conditional-symbol checkboxes include symbols from the resource entry's include chain as well as the open file, allowing referenced variants to be enabled even when their conditions are declared elsewhere.
  Enabled symbols remain selected when reloading or browsing another file in the same resource context, provided those symbols still exist in that context.
  Select an INCLUDE or an element with a resolved reusable definition and choose **Open reference in new window** in the XML editor toolbar. The referenced XML opens in an independent editor, leaving the current document and unsaved changes in place. Each window retains its own save/discard workflow.
  `PangyaUiMenuState.DisabledTexts` represents `FrContextMenuCtrl::UnableMenuItem`: every entry with an exact, case-sensitive text match is disabled for drawing and hit-testing, in addition to entries in `DisabledRows`.
  `PangyaUiMenuState.TextReplacements` represents `ToggleMenuItem` text changes using exact, case-sensitive keys. Replacement preserves the original icon, separator classification, and measured menu width; use `ContextMenuCaptions` to rebuild a menu instead.
  `PangyaUiText.GetPrintData` exposes Fresh text output with `All` preserving the source or `ExcludeTags` removing recognized formatting tokens. Tag-excluded output and tooltip rendering share Fresh's line composition, including blank lines and literal `\n` separators.
  `PangyaUiText.GetLongestLineWidth` measures complete tag-excluded lines through a supplied font metric callback, matching `FrTEXT::GetWidthLong`; tooltip sizing uses this path.
  `PangyaUiEditText` models edit line addition, one-based replacement/retrieval, clearing, end deletion, byte limits, per-line styles, and `AddText` wrapping. `ApplyTo(node, options)` snapshots its lines, styles, and justification into renderer options without modifying the XML.
  `renderer.SetFormMessage(graphics, form, text, spaceAlign, options)` returns updated preview options for Fresh's `FrForm::SetMessage`, targeting the last direct STATIC and EDIT children named `message`. The edit message uses its width, margins, font metrics, and scrollbar width for wrapping.
  Message replacement clears the old edit scroll offset and follows new lines to the bottom by default. Set `EditFollowBottom[node]` to false to model a scrollbar whose follow mode was disabled; replacement then starts at the first row.
  `ViewFocusedNodes` and `FixedNodes` expose Fresh's per-window view focus: focus inherits through ancestors but stops at a form, and fixed windows count as focused. Supplying either set enables this model for frames, buttons, scrollbar artwork, and tooltips. Leaving both unset preserves the global `IsViewFocused` preview setting.
  `TryOpenViewer(node, filename, options, out updatedOptions)` models runtime `FrViewer::Open`: failed loads preserve the previous options, a successful different filename resets scroll offsets, and reopening the same filename preserves them.
  `TooltipEnabled[node]` models per-window `EnableToolTip` independently of stored tooltip text and the global `ShowTooltip` preview switch. Disabled tooltips retain their text and fixed-position state for re-enabling.
  `PangyaUiRenderer.SetWindowTopmost(node, value, options)` returns updated topmost flags. Clearing a parent also clears its descendants, matching `FrWnd::SetTopmost(false)`; enabling a parent does not restore cleared child flags.
  This helper also maintains `TopmostOrder` in registration order for drawing and hit-testing. Enabling an already registered window leaves its order unchanged; removing and re-enabling appends it.
  `SetDesktopWallpaper` respects `LockedDesktopWallpapers`; an empty filename clears and hides the wallpaper. `SetDesktopWallpaperFromBitmap` bypasses that lock, matching Fresh; a null bitmap changes visibility while retaining the image. Supplied bitmap ownership remains with the caller.
  `IsDesktopWallpaperVisible` shares wallpaper selection with drawing and follows Fresh's packed-color check: color zero is false, while a nonzero color can report true even when its alpha is zero.
  `TryMoveCursorToChild` models `FrWnd::MoveCursor` in preview state: it finds the first direct child with an exact name, centers the preview pointer using runtime geometry, and respects an optional active modal form. It never moves the system mouse.
  `WindowAlpha` and `WindowAlpha2` supply native window alpha channels. Areas, image buttons, tabs, frames, and edit backgrounds combine both; viewers and menu text use primary alpha. `SetWindowSecondaryAlphaToChildren` snapshots Fresh's recursive secondary-alpha setter. Existing control opacity overrides multiply these values.
  Scrollbars have independent child-window alpha through `ScrollbarAlpha` and `ScrollbarAlpha2`, keyed by their owning control. Direct parent alpha changes do not fade them; the recursive secondary-alpha setter updates them too.
  Generated form frames likewise use `FormFrameAlpha` and `FormFrameAlpha2`, keyed by the form. Direct form alpha does not fade the child frame; recursive secondary-alpha updates reach it.
  Combo unfold buttons use `ComboButtonAlpha` and `ComboButtonAlpha2` for the same independent-child behavior.
  Extended gauge arrows use `GaugeButtonAlpha` and `GaugeButtonAlpha2`, keyed by `(node, Right)` with `Right=false` selecting the left arrow. Recursive secondary-alpha updates include both arrows; changing only the gauge's own alpha leaves them unchanged.
  `SelectTextButton` clamps the requested segment index to the first/last parsed segment; pass `selected: false` to unselect only that segment. `SetTextButtonCaption` replaces the caption, clears selection, and centers within the retained current geometry using Fresh's integer rounding. Both return independent option snapshots. Supply `MeasureCaptionText` to use the preview font's metrics.
  `GetSelectedTextButtonText` and `GetTextButtonTextUnderCursor` return the matching segment or null. `SetCaptionClippingArea(node, rectangle, options)` replaces the caption clip; passing null removes it and explicitly disables caption clipping, while an empty rectangle retains an enabled empty clip.
  `SetStaticCaption` applies Fresh's 100-byte Shift-JIS limit and resets scrolling for normal replacements. With `smallFont: true`, it stores the full caption and retains small-font mode across later normal replacements, matching `SetCaption11`.
  `PangyaUiListSelection.AddItem`, `DeleteItem`, and `Clear` update item counts and selection; pass a matching `PangyaUiScrollState` to update scrolling too. Deleting the current single-selection item selects its next neighbor, or the previous neighbor at the end. Deleted previous-item references become null instead of retaining native dangling pointers.
  `SortItems(comparison)` performs a stable sort over current item indices, preserving selected/current/previous item identity. It returns the old index at each new position: reorder the caller-owned drawing data using that mapping before rendering. `ReorderItems(order)` applies an explicit permutation to selection state and rejects missing, duplicate, or out-of-range entries before changing state. Sorting retains the scrollbar position.
  List selection also tracks `ItemUnderCursor` and per-item rollover flags. `SetCursorToFirstItem` and `SetCursorToLastItem` retain earlier flags as Fresh does; `SetItemUnderCursor` replaces them. Sorting/deletion remap this state, and `ApplyTo` supplies an independent `UnderCursorItems` snapshot to drawing. An explicit empty snapshot suppresses pointer-derived rollover. `MouseEventsEnabled` controls automatic mouse handling without clearing retained rollover flags.
  `NavigateCursor(direction, columnCapacity)` models list keyboard rollover: previous/next move one item, row-up/down move by the column capacity, and the first navigation with no current rollover chooses the first item. It leaves selection and scrolling unchanged. The helper returns whether the cursor moved and safely rejects movement beyond either end.
  `HitTestListItem(node, point, options)` returns the visible item index or -1 using the list's columns, item dimensions, scrolling, and explicit top-item override. It rejects incomplete trailing rows and disabled mouse handling. Set `ExcludeDummyItemsFromHitTesting` to reproduce Fresh's dummy-item exclusion; dummy cells remain available to owner drawing.
  `PangyaUiRenderOptions.GraphicsAlpha` supplies Fresh's shared graphics multiplier (default 1). It applies after window/color alpha to renderer-managed textures and text, including frame slices, combo skins, inline emoticons, and image gauges. Primitive fills/lines, editor debug overlays, and the cursor overlay retain their separate drawing behavior. Alpha is scoped to the graphics surface for each render and restored after nested renders or exceptions. Caller-owned drawing callbacks that directly use `Graphics` remain responsible for their own colors and images; caption callbacks receive the multiplied brush.
  Tooltips use their own `TooltipAlpha`: their skin bypasses shared graphics alpha and their text replaces it temporarily, matching Fresh. A dimmed or zero-alpha UI does not multiply tooltip opacity a second time.
  Desktop wallpapers, desktop background overlays, and viewer images use Fresh's separate background drawing paths, so shared `GraphicsAlpha` does not fade them. Their own tint and window alpha still apply where defined by the control.
  Tooltip frame image suffixes `00`–`08` follow Fresh's row-major order: top-left/top-middle/top-right, middle-left/center/middle-right, then bottom-left/bottom-middle/bottom-right.
  Additional frame layers normalize nonzero `bottompos` to `pos = -bottompos`, as Fresh does. Negative normalized positions anchor against the frame's bottom using the actual slice height; positive positions are top-relative, including negative `bottompos` values.
  Context-menu skins use their native layer types 0–2; type 3 remains available to normal frames and is skipped for context menus.
  `SetContextMenuList` rebuilds menu rows from a caption and clears previous selection, disabled entries, replacements, and current hover. It preserves independent appearance settings such as opacity and returns a new options snapshot.
  Scrollbars retain fractional proportional thumb heights through drawing and travel calculations. Public thumb rectangles use enclosing integer dimensions for pointer handling.
  `DeferredDisplayContainers` models Fresh's `FWF_UNK4000_983` rendering flag. A selected standalone form, or a layout's direct child, is treated as a desktop child: its own visuals are skipped and its children draw in a later pass. Nested flagged containers remain skipped. Hidden containers suppress that pass, and independently topmost children retain their normal topmost pass. The visualiser has no external RF scene pass between these stages.
  `PangyaUiTabState` provides stateful tab addition, clearing, identifier selection, hovering, and button-down/up activation. Supply each tab's explicit size or its normal-image size when adding it; those initialized dimensions are retained. The bar accumulates tab widths and separator widths and keeps its first tab's height. `ApplyTo(node, options)` returns an independent render snapshot, replacing that bar's older selection overrides. Identifier selection uses the first duplicate, and missing identifiers preserve selection. Button-up activation selects only when pressed and released tabs match; no animations are introduced.
  Tab snapshots include authoritative hover indices, where null explicitly means nothing is hovered. `TabMouseEventsEnabled` controls automatic pointer rollover and tab hit testing; `PangyaUiTabState.ApplyTo` disables it for `Activation.None`, matching Fresh's behavior when no activation handler exists. Retained hover state can still be drawn independently of input enablement.
  `PangyaUiComboList` tracks dropdown visibility and opening `Direction`. `SelectNext`/`SelectPrevious` wrap selection without changing visibility; `ClickRow` closes the dropdown after attempting selection. `LoseDropdownFocus` closes it only when the pointer is outside both the list and unfold button. `ApplyTo` uses this state unless an explicit `isOpen` override is supplied. Disabled combos reject unfold-button hit tests.
  `MoveWindow(node, absolutePosition, options)` moves a runtime window and its ordinary children while keeping nested forms stationary, matching `FrWnd::MoveWindow`. Scrolling static captions restore their clipping rectangles at the new positions without resetting their scroll offsets. The returned snapshot preserves prior options.
  Automatic combo opening direction uses the original XML position rather than flipping when the window moves. Call `InitializeComboOpeningSide(node, viewportHeight, options)` to capture a runtime initialization position explicitly. `AutomaticOpensAbove` retains that side through later movement and row updates; forced `Above`/`Below` direction still overrides it.
  Image coordinates use their parent window's origin, not the preceding sibling image. Frame slices and tabs use their own cumulative layout rules. Macro definitions expand directly under the macro instance's parent, so a macro wrapper's `pos` does not translate its children. Referenced-form visual bounds include children outside the frame, keeping selection and hit testing aligned with their parent-relative drawing positions.
  Legacy item rectangles with reversed edges retain their declared origin instead of falling back to `(0, 0)`. This fixes tiled backgrounds such as `bongdarishop.xml`'s `about_shop`: unstretched AREA images draw at their natural bitmap size and can extend beyond the declared area unless clipping is explicitly enabled. Empty or reversed explicit clips produce no visible image; malformed or overflowing clip values are handled the same way rather than falling back to the AREA bounds.
  The property editor preserves signed widths/heights for rectangle-based items, so editing an image's position does not silently flatten reversed edges. Size-based elements and layout dimensions remain nonnegative; rectangle writes reject overflowing edges.
  `SetAreaClipArea` stores an absolute clip rectangle without enabling it; use `EnableAreaClipping` separately. AREA clips retain their original absolute position when `MoveWindow` moves the image, matching Fresh. Disabling clipping retains the stored rectangle for later reuse.
  Referenced form/macro bounds carry the instance's parent origin when applying absolute AREA clips. Selection and hit testing therefore match clipped images drawn inside nested referenced containers.
  AREA controls with a nonzero `mouse_event` parameter show the hand cursor in the preview. `SetAreaMouseEvent` supplies the runtime equivalent without editing XML; disabled or hidden controls do not request this cursor. Editor selection and dragging remain available for inspecting AREA images.
  `SetWindowViewFocus` targets the containing form (or root window), can clear previous focus, and honors an optional modal-form guard. `ResetWindowViewFocus` clears a subtree while optionally preserving a stop subtree. These return explicit focus snapshots used by focus-sensitive drawing; native input capture is handled separately.
  `PangyaUiFontState(height, measure, draw)` adapts a font's existing callbacks for scaling. Call `SetScale` and `ApplyTo(options)` for normal captions, or `ApplyTo(options, smallFont: true)` for small text/tooltips. Each snapshot scales measured widths and drawing together and restores the graphics transform afterward. Reported cell height remains unscaled, matching `WFixedFont.GetFontHeight`.
  The bitmap-font adapter indexes WFT glyphs using Shift-JIS codes, including half-width katakana. Its native default spacing is 1: non-space single-byte glyphs advance by stored width plus spacing, double-byte glyphs subtract one, and spaces use `cellWidth * 14 / 36`. `SetSpace` replaces spacing and returns the previous value. Unrepresentable Unicode scalars use a single question-mark fallback.
  Drawing detects reference cycles by resolved definition identity. Distinct resource types may share a name, and names that differ only in case remain distinct, so valid nested references are drawn rather than mistaken for cycles. Actual cycles remain suppressed, including in deferred topmost drawing.
  `SetWindowRectangle` combines native movement with a size change and immediate-child resize callbacks. Loaded viewers rebuild their scroll rows: they return to the top unless `ViewerFollowBottom` is enabled. `PangyaUiScrollState.ApplyToViewer` preserves this follow mode, which also applies when opening a replacement image.
  `SetWindowVisible(node, visible, options)` updates preview visibility and models the context-menu override: showing a menu re-enables every item while retaining its text and selection.
  `SetWindowEnabled` synchronizes disabled flags with runtime button state, including combo buttons and extended gauge arrows. As in `FrButton::Enable`, either transition resets hover to normal while preserving selected state.
  `TrySetButtonImage` updates one main image slot and resizes the button on success. `TrySetButtonBelowImage` updates a below-image slot without resizing and accepts null to clear it. Missing images preserve previous state; accepted modes are `normal`, `over`, `selected`, and `blink`.
  `InitializeExternalButton` models `FrButton::InitExtern` with caller-owned normal, hover, and selected bitmaps, native styles, and normal-image sizing. Null external state images suppress XML fallback. Filename replacement can subsequently replace an external slot. `GetButtonPushStyle` reads runtime or XML push style.
  `SetButtonPushDelay` stores a runtime delay and resets elapsed press time to `delay + 0.1`, matching Fresh's immediately-ready setter behavior. The options-aware `GetButtonPushDelay` reads this override.
  `PangyaUiComboList` provides byte-limited strings, one-based search results, zero-based deletion, monotonic maximum-row increases, and selection state. Deleting or clearing list selection retains the displayed edit text. `ApplyTo` snapshots both the dropdown and edit caption into renderer options.
  `PangyaUiScrollState` models scrollbar item mutations, capacities, end scrolling, and follow-bottom transitions. `ApplyToViewer` snapshots its row and guide state into renderer options.
  `DragToRow` preserves fractional drag position until a native-style recalculation. `ApplyToEdit` supplies integer text rows and follow mode; `ApplyToList` supplies item count, columns, position, and scrollbar side while preserving owner-draw callbacks.
  `PangyaUiListSelection` models list selection flags, current/previous selection, multi-select, and toggling. `ApplyTo` snapshots flags into an owner-drawn list preview. The `SelectItem` overload accepting `PangyaUiScrollState` also applies Fresh's half-page scrolling to current selections.
  Image combos can supply `PangyaUiComboPreview.ImageSelection` independently of their rows. `ClearComboImageList` clears rows and scrolling while retaining the selected texture and current byte index, matching `FrComboCtlEx::ClearAllListItem`.
  `SelectComboImage` selects among successfully loaded row textures, stores the native byte-sized current index, and clears the selected image for an invalid index without changing dropdown row selection.
  `AddComboTexture` adds a numbered row even when its image is missing. Image-list clearing retains `StoredTextRowCount`, so later row numbering continues as in Fresh. Missing textures remain safe empty image entries in the preview.
  `CaptionClippingEnabled[node] = false` explicitly clears static/text-button clipping, including automatic scrolling-caption clipping, while retaining the configured `CaptionClips` rectangle for later use.
  Small-font static captions follow Fresh's separate 11-pixel font path and do not inherit the static control's 12-pixel caption clip.
  Static font clipping also leaves emoticon overlays unaffected, matching Fresh's separate font/overlay state; icons still respect the outer drawing surface clip.
  Tab whole-control owner drawing receives accumulated tab width plus separators and the first tab's height, matching Fresh's AddTab geometry. An empty tab list has zero size; explicit `WindowSizes` overrides remain available.
  `TextureImages` stores the compact image vector separately from dropdown rows. Rows index that vector as Fresh does, so failed loads can shift which row displays an image. Out-of-range image accesses render blank rather than reproducing native undefined behavior.
  `AdjustFormToViewport(form, viewport, options)` applies `FrForm::Adjust` through runtime offsets: right and bottom overflow moves the form inward, while negative left/top positions remain. Oversized forms can retain negative coordinates, matching Fresh.
  `TrySetAreaImage` and `TrySetAreaBitmap` model the distinct `FrArea::SetBgImg` overloads. A null filename clears the image; a null bitmap fails without changing it. Filename failures clear the displayed bitmap and retain the attempted name, so repeating that name is a successful no-op as in Fresh. Always use the returned options, including on filename failure.

  `PangyaUiRenderOptions.GaugeStepCounts` supplies `FrGaugeBarEx::SetSteps` values. Counts are clamped to at least one and affect both step-based positions and separator drawing.
  `GaugeAppearances` supplies runtime fill colors, border thickness, and positive/negative expansion colors through `PangyaUiGaugeAppearance`.

- [x] **PangYa Shop Editor**: Renders the live shop skin and catalog records only from IFF tables that inherit the `Common` base, including disabled, non-saleable, zero-price, and missing-icon items. Category and side-tab controls use the image resources declared by `shop.xml`/`predefined.xml`; unresolved skin resources are skipped and reported once in the shared Log Viewer instead of preventing the shop from opening. Administrators can navigate visually and edit each item’s icon, prices, flags, duration, and availability dates directly in the discovered `pangya_*.iff`; loading, warnings, and saves appear in the shared Log Viewer. It uses the extraction `pakpath.json` sidecar to recover the original data root and requires `ui/shop.xml`, `ui/predefined.xml`, and exactly one matching PangYa IFF archive. The shop uses delimited TH, JP, GB/Global filename hints or known IFF headers to select its schema and asks for the region when neither source is conclusive.
- [x] **PangYa WFT Font Viewer and TrueType Exporter**: Safely reads `WFNT` bitmap fonts, browses their full BMP glyph range in a virtualized grid, inspects advances and coverage, renders zoomable sample text, and exports installable Windows `.ttf` fonts without loading the entire source font into memory.

Open a WFT font and choose **Export TTF** to select its Windows family and standard style. Export preserves the original pixel design by converting covered bitmap regions into square TrueType outlines; 4-bit coverage uses a 50% threshold. WFT files do not contain a baseline, kerning, family name, or style metadata, so exported glyph cells are placed above the baseline, have no kerning, and use the metadata selected during export.

### 🚀 Advanced Features
- **Application Log Viewer:** A shared logging interface retains tool activity for the current session and exposes it from the main menu; PAK audit activity is also written to `activity_log.txt`.
- **Multi-Region XTEA Cryptography:** Full support for official and custom header encryptions: Global (GB), Thailand (TH), Japan (JP), Korea (KR), Indonesia (ID), Europe (EU), and Super SS Dev (Custom).
- **Advanced Tree View Interaction:** Create persistent empty folders from the right-click menu, select them as file-injection targets, and use keyboard shortcuts or context actions for rename, extraction, and subtree removal.
- **From-Scratch PAK Creation:** Use **New** in the PAK operations toolbar to configure, create, and immediately open a valid empty PAK, then build its archive layout by adding persistent folders and injecting files from the manager.
- **PSP PAK pairs:** Keep `pspdata.pak` and `psppackfilelist.pak` in the same folder and open either file to browse and extract. Use **Update** to replace a uniquely named entry with a file of the same byte length. The API also supports replacements by entry position through `PspPakArchive.ReplaceEntries`, including duplicate names. PSP replacements preserve both indexes and the original data layout. Creating PSP pairs, adding or removing entries, renaming entries, and resizing payloads are unsupported.
- **PAK Settings and Diagnostics:** Use **Settings** on a loaded PAK to update its header version and ASCII author or optionally recompress all regular entries with a selected format and level. Updates are validated and applied atomically with a `.bak` backup. The manager also displays the detected key, keeps entry-list scrolling available, and highlights unsafe or incorrectly decoded entry names so the filename encoding can be corrected before editing or extraction.

### 📦 NuGet Packages

The PangyaAPI libraries are published as independent .NET 10 packages:

| Package | Target | Package dependencies |
| --- | --- | --- |
| `PangyaAPI.Utilities` | `net10.0` | None |
| `PangyaAPI.PAK` | `net10.0` | `PangyaAPI.Utilities` |
| `PangyaAPI.IFF` | `net10.0` | `PangyaAPI.PAK`, `PangyaAPI.Utilities` |
| `PangyaAPI.UpdateList` | `net10.0` | `PangyaAPI.Utilities` |
| `PangyaAPI.WFT` | `net10.0` | None |
| `PangyaAPI.UI` | `net10.0-windows` | `System.Drawing.Common` |

Install only the modules your application needs:

```powershell
dotnet add package PangyaAPI.PAK
dotnet add package PangyaAPI.IFF
dotnet add package PangyaAPI.UpdateList
dotnet add package PangyaAPI.WFT
dotnet add package PangyaAPI.UI
```

GitHub Actions restores from NuGet.org and builds and tests the projects in `PangYa-Suite-Tools.CI.slnf`. The filter excludes the Network project and its tests because they require the local `PangyaAPI.IFF.JP` package. Use `PangYa-Suite-Tools.slnx` with the local feed to build and test those projects.

Repository builds retain project references for source-level development. Run `.\build-local-nugets.ps1` to build the PangyaAPI solution and create packages and symbols in the shared `D:\devtest\.nuget-local` directory. Each invocation assigns the entire package set one sortable UTC timestamp version such as `1.0.0-local.20260802010130123`, so rebuilt packages do not reuse a previous version and their PangyaAPI dependency versions remain synchronized. Add the directory as a package source in the server repository with `dotnet nuget add source D:\devtest\.nuget-local --name PangyaAPI-local`.

Tags in the form `vX.Y.Z` or `vX.Y.Z-prerelease` build matching package versions, attach the packages and symbols to the GitHub release, and publish them to NuGet.org using the repository's `NUGET_API_KEY` secret. Branch and pull-request builds produce uniquely versioned CI package artifacts without publishing them.

### 🛠️ Technical Snippet (PAK Compilation Example)
To code with this API to compile a folder using Japanese specification (V3):

```csharp
using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using PangyaAPI.Utilities.Cryptography;

var writer = new PakWriter
{
    EntryVersion = PakFileEntryVersion.V3,
    EntryType = PakFileEntryType.LZ772,
    CompressLevel = 5,
    LocationKeys = PakKeys.JP,// xtea_key
    Author = "SuiteTools"
};

// Compiles recursively while preserving offsets.
writer.CreateFromDirectory(@"C:\Modding\data", @"C:\Games\PangYa\ProjectG.pak");
```

### IFF Editor / Manager

Open **IFF Editor / Manager** from the main menu to work with a folder of loose `.iff` files, a single loose IFF, or a ZIP-based PangYa IFF container. Encrypted containers are detected automatically, and the container key selector can preserve the detected XTEA key or save the archive as plain ZIP or with another supported key.

The editor provides two complementary views:

- **Form view** groups schema fields into tabs, provides record search, and uses appropriate controls for text, numbers, flags, dates, and item references. When a PangYa data folder is selected, supported references can display names and icons and open a reference picker.
- **Grid view** is intended for comparing and editing many records at once. Its columns come from the active JSON schema, while unmapped bytes remain available through the raw-record viewer.

Use the toolbar to add, copy, or delete records; save changes; extract one original IFF or every entry in a container; and patch the current table from a same-named loose IFF. The patch workflow matches records by item ID, lets you choose records and compatible fields, previews the changes, and converts values for the target region and string widths.

For the safest workflow, select **Auto** region detection first and choose the string encoding before loading a file. The editor recognizes known TH, JP, and Global headers and uses filename or container hints when available; if the region is still ambiguous, it displays the header details and asks which schema to use. PangYa UI XML loading also tolerates legacy bare ampersands in text and attribute values while preserving valid XML entities and writing standards-compliant XML when saved. Back up game data before editing. **Extract IFF** and **Extract all IFFs** export the original stored bytes, so unsaved edits are not included.

### JSON IFF schemas

The IFF Manager also reads and writes PSP version-1 tables in `PangYaPSP.iff` and loose PSP tables. Auto detects their 32-bit count/version header; **PSP** is available in the region selector. The 17 PSP schemas expose confirmed IDs, names, models, textures, equipment slots, stat contributions, and course assets while preserving unknown bytes. The shipped headerless `Title.iff` variant retains its original layout. PSP text defaults to code page 949 and respects the editor's encoding selection. See [PSP IFF format and validation](docs/psp-iff.md) for evidence, limits, and reusable schema-generation/verification scripts.

`PangyaAPI.IFF` is the single IFF package. Use `IffCatalog.LoadAsync` to load and index a loose table or PangYa IFF container with JP, TH, or Global schemas. The removed `PangyaAPI.IFF.JP` package and its `IFFFile<T>`/`sIff` APIs have no compatibility layer; consumers should query `IffTable` and `IffRecord` fields instead.

IFF editor layouts are defined by versioned JSON files in
`%LocalAppData%\PangYa-Suite-Tools\schemas`. Default TH, JP, and Global schemas are copied there on first use without overwriting existing files. Schema files are matched by IFF filename and region (for example, `Item.TH.json`), with `.default.json` as the optional fallback. Version 2 schemas can inherit a `Common` base selected from the IFF header's revision and magic; version 1 flat custom schemas remain supported. The editor's **Schema columns** dialog shows inherited base fields as read-only schema definitions and saves local column changes back to the matching JSON file.
JP schemas whose records use the 192-byte `Common` base expose their format-specific fields after that base instead of treating the remaining bytes only as raw data.

The IFF editor and catalog support PangYa JP 983 version 13 tables, including the JP 983 common item layout and its 40-byte icon field. Auto recognizes version 13 with linkage 0 and uses JP archive names such as `pangya_jp.iff` and `pangya_jp2w.iff` to resolve other linkage values. For a loose table with an ambiguous header, choose **Japan (983)** in the editor or pass `IffHeader.Japan983Region` as the API schema region. The JP 983 layouts are based on the [pinned client definitions](https://github.com/lucaspef/pangya-jp983-src/blob/81a6969942fe007bcaccbb33b53b3081e9cee67f/src/shared/classdefine.h).

JP schemas use `ImageResource` for texture, sprite, and image names and `ItemIdReference` for fields that point to another record. Existing custom schemas can still use the legacy `Icon` type. If you already have local JP schemas, check **Schema updates** to review the bundled revisions.
Bundled schemas carry a `defaultRevision`. When a newer bundled revision differs from a local schema, the IFF Manager offers to replace the local file, keep the complete local definition as the preferred default for that revision, or defer the choice. Replaced or acknowledged files are backed up under `%LocalAppData%\PangYa-Suite-Tools\schemas\backups`; embedded defaults remain read-only and the preferred definition is always stored as a user-local override. The **Schema updates...** toolbar command reopens deferred updates. Select a row and click **Use this action for all items** to copy its action to every row before applying updates.
The region selector can be set before opening an IFF; delimited `TH` or `JP` tokens in the container filename automatically update the selector, while the manual choice is used for files whose region cannot be detected.
The schema editor can clone fields from the current or another schema, reorder fields, configure a default width for new strings, and control each field's visibility. Embedded defaults are used whenever no matching user schema exists. Raw hexadecimal bytes are colored by their defining schema fields, with overlaps shown in red. The editor reports bytes whose bits are not fully represented by schema fields.
Double-click a Raw grid cell to open its byte-range picker. Select one contiguous range and choose **Define column** to create a schema field prefilled with the selected offset and width.
ZIP-based IFF containers expose their detected XTEA key and can be saved as plain ZIP or with another supported key. Delimited `TH`/`JP` filename tokens select the schema region, while encryption keys are detected independently by trying the supported keys.
The editor detects the IFF region from known header revision and magic combinations, or from an explicit filename/container region hint when necessary. Only when none of those sources resolves a region does it display the record count, revision, magic, reserved bytes, and record size and require a TH, JP, or Global schema choice. The form-view search includes the full `Description` text in `Desc.iff`. The **Extract IFF** action exports the selected entry's original bytes without applying unsaved editor changes. **Extract all IFFs** exports every archive entry into one selected folder, flattening internal paths and replacing existing files atomically after checking for filename collisions. The **Patch IFF** action copies compatible values from a same-named loose IFF by matching item IDs, previewing selected records, and converting values through the target region's schema and string widths.

#### Column-header scrolling

Hover an IFF grid column header and use the mouse wheel to adjust its schema field. Wheel up increases the value by one byte; wheel down decreases it.

- **Ctrl + wheel:** adjust the field offset and move following fields with it.
- **Alt + wheel:** adjust the field width and update following offsets.
- Hold **Shift** with either shortcut to change only the hovered field, leaving following fields unchanged.

Valid changes are saved immediately to the matching user JSON schema. Invalid changes, such as moving a field outside the fixed record size, are rejected.

---

## 🇧🇷 Português

**Pangya Suite Tools** (ou *Pangya Studio Tools*) é uma solução integrada de engenharia reversa e modificação avançada, desenvolvida inteiramente em **C# (.NET 10)**. O objetivo deste ecossistema é centralizar a leitura, edição, conversão e compilação de múltiplos formatos de arquivos nativos utilizados pelo cliente e servidor do jogo **PangYa** (como estruturas `.PAK`, tabelas `.IFF` e listas de atualização).

O projeto é estruturado sobre uma API de alto desempenho (`PangyaAPI`) e uma interface gráfica rica em **Windows Forms**, utilizando operações assíncronas modernas baseadas em Tasks para garantir que tarefas pesadas de I/O de disco e criptografia ocorram em background, mantendo a UI totalmente responsiva.

### 🗺️ Visão Geral dos Módulos
- [x] **PangyaAPI.PAK (`FrmPakMaker.cs`)**: Manipulação cirúrgica de pacotes de dados. Extração individual ou em lote, injeção/mesclagem dinâmica de arquivos e suporte total ao algoritmo XTEA multiregião.
- [x] **PangyaAPI.PAK Sync (`FrmPakDiff.cs`)**: Ferramenta de sincronização estrutural Multi-PAK entre clientes para comparar e isolar arquivos ausentes, modificados ou idênticos.
- [x] **PangyaAPI.IFF**: Parser, catálogo indexado e editor multirregional orientado por esquemas para tabelas do jogo (`Character.iff`, `Item.iff`, etc.). O antigo pacote de modelos JP foi substituído por esquemas validados e registros `IffRecord`.
- [x] **PangyaAPI.UpdateList**: Utilitário para visualizar, verificar, gerar e assinar listas criptografadas em XML para o Launcher/Updater do jogo. Na aba do visualizador, carregue uma lista, selecione a pasta raiz do PangYa e escolha **Verificar arquivos** para comparar o CRC32 de cada arquivo listado; arquivos iguais, diferentes, ausentes, com caminho inválido ou erro de leitura são exibidos sem varrer arquivos locais não relacionados. Opcionalmente, o gerador cria pacotes com compressão ótima ao lado de `updatelist`, achatando caminhos relativos com sublinhados e acrescentando a ordem determinística de varredura, iniciada em 1 (por exemplo, o segundo arquivo `GameGuard/GameGuard.des` se torna `GameGuard_GameGuard.des_2.zip`). A coluna Pacote registra exatamente esse nome ZIP. Pacotes inalterados são preservados por comparação SHA-256, pacotes alterados são substituídos atomicamente e a preferência também vale para regenerações monitoradas.
- [x] **PangyaAPI.UI**: Modelos reutilizáveis de documentos XML da interface e loja do PangYa, edição atômica, resolução de recursos, decodificação de imagens, renderização, geometria e detecção de elementos.
- [x] **Editor de UI PangYa**: Abre layouts `ui/*.xml` extraídos em uma árvore de elementos, renderiza os recursos em uma tela com zoom preservando o tamanho original das imagens salvo quando `stretch=1`, visualiza estados de botões e limites de depuração e edita propriedades de forma atômica. Elementos com apenas `pos` usam limites padrão de 10 × 10 pixels, conforme o Fresh do PangYa JP 983. Instâncias `MACROITEM` expandem os elementos do recurso referenciado nas coordenadas do contêiner pai. A prévia de `LAYOUT` usa as dimensões, a cor e a imagem de fundo de `<base>` e exibe as legendas de `STATIC`. O comportamento do layout é compatível com o [Saeroun](https://github.com/retreev/Saeroun).
- **Exemplo de VIEWER.** Selecione [examples/frviewer](examples/frviewer) como pasta de dados no Editor de UI PangYa e abra `viewer.xml`. Cada painel mostra três faixas de cor. Posicione o ponteiro sobre a imagem da esquerda e gire a roda para baixo para avançar uma faixa de 20 pixels por passo. A imagem da direita fica parada com `mouse_event="0"` e aparece com cerca de 50% de opacidade devido a `bgcolor="80FFFFFF"`.
- [x] **Editor da Loja PangYa**: Renderiza a interface real e todos os registros de catálogo compatíveis de uma pasta `data` extraída, incluindo itens desativados, não vendáveis, sem preço e sem ícone. Administradores podem navegar visualmente e editar ícone, preços, flags, duração e datas diretamente no arquivo `pangya_*.iff`; carregamentos, avisos e salvamentos aparecem no Visualizador de Log compartilhado. Usa `pakpath.json` para recuperar a raiz original e requer `ui/shop.xml`, `ui/predefined.xml` e exatamente um arquivo IFF PangYa correspondente.
- [x] **Visualizador e exportador TrueType de fontes WFT do PangYa**: Lê fontes bitmap `WFNT` com segurança, navega por todos os glifos BMP em uma grade virtualizada, inspeciona avanços e cobertura, renderiza texto de amostra com zoom e exporta fontes `.ttf` instaláveis no Windows sem carregar toda a fonte de origem na memória.

Abra uma fonte WFT e escolha **Exportar TTF** para definir a família e o estilo padrão usados pelo Windows. A exportação preserva o desenho em pixels convertendo regiões cobertas em contornos TrueType quadrados; a cobertura de 4 bits usa um limite de 50%. Como arquivos WFT não armazenam linha de base, kerning, família ou estilo, as células exportadas ficam acima da linha de base, não possuem kerning e usam os metadados escolhidos durante a exportação.

### 🚀 Recursos Avançados
- **Visualizador de Log do Aplicativo:** Uma interface de log compartilhada mantém a atividade das ferramentas durante a sessão e pode ser aberta pelo menu principal; a auditoria de PAK também é gravada em `activity_log.txt`.
- **Criptografia por Região (XTEA):** Suporte completo ao algoritmo XTEA para criptografia de cabeçalhos utilizando chaves oficiais e customizadas: Global (GB), Tailândia (TH), Japão (JP), Coreia (KR), Indonésia (ID), Europa (EU) e Super SS Dev (Custom).
- **Interação Avançada em Árvore:** Crie pastas vazias persistentes pelo menu de contexto, selecione-as como destino para injeção de arquivos e use atalhos ou ações de contexto para renomear, extrair e remover subárvores.
- **Criação de PAK do Zero:** Crie e abra imediatamente um PAK vazio válido e monte sua estrutura adicionando pastas persistentes e injetando arquivos pelo gerenciador.
- **Configurações e Diagnóstico de PAK:** Use **Configurações** em um PAK carregado para alterar a versão do cabeçalho e o autor ASCII ou, opcionalmente, recomprimir todas as entradas normais com o formato e nível selecionados. As alterações são validadas e aplicadas de forma atômica com backup `.bak`. O gerenciador também exibe a chave detectada, mantém a rolagem da lista e destaca nomes inseguros ou decodificados incorretamente para que a codificação seja corrigida antes da edição ou extração.

### Editor / Gerenciador de IFF

Abra o **Editor / Gerenciador de IFF** pelo menu principal para trabalhar com uma pasta de arquivos `.iff` soltos, um único IFF ou um contêiner IFF do PangYa baseado em ZIP. Contêineres criptografados são detectados automaticamente, e o seletor de chave permite manter a chave XTEA detectada ou salvar o arquivo como ZIP simples ou com outra chave compatível.

O editor oferece duas visualizações complementares:

- **Visualização em formulário:** organiza os campos do esquema em abas, permite pesquisar registros e usa controles apropriados para textos, números, flags, datas e referências de itens. Ao selecionar uma pasta de dados do PangYa, as referências compatíveis podem exibir nomes e ícones e abrir um seletor de itens.
- **Visualização em grade:** facilita a comparação e edição de vários registros. As colunas são definidas pelo esquema JSON ativo, e os bytes não mapeados continuam acessíveis pelo visualizador de registro bruto.

Use a barra de ferramentas para adicionar, copiar ou excluir registros; salvar alterações; extrair o IFF original selecionado ou todas as entradas do contêiner; e aplicar um patch a partir de um IFF solto com o mesmo nome. O assistente de patch relaciona registros pelo ID do item, permite selecionar registros e campos compatíveis, mostra uma prévia e converte os valores para a região e as larguras de texto do arquivo de destino.

Para um fluxo mais seguro, mantenha a detecção de região em **Automático** e escolha a codificação de texto antes de carregar o arquivo. O editor reconhece cabeçalhos conhecidos das regiões TH, JP e Global e usa dicas do nome do arquivo ou contêiner quando disponíveis; se a região continuar ambígua, ele exibe os detalhes do cabeçalho e solicita o esquema correto. Faça backup dos dados do jogo antes de editar. **Extrair IFF** e **Extrair todos os IFFs** exportam os bytes originais armazenados, portanto alterações ainda não salvas não são incluídas.

### Rolagem nos cabeçalhos das colunas IFF

Posicione o cursor sobre o cabeçalho de uma coluna da grade IFF e use a roda do mouse para ajustar o campo do esquema. Rolar para cima aumenta o valor em um byte; rolar para baixo diminui.

- **Ctrl + roda:** ajusta o deslocamento do campo e move os campos seguintes.
- **Alt + roda:** ajusta a largura do campo e atualiza os deslocamentos seguintes.
- Segure **Shift** com qualquer atalho para alterar somente o campo sob o cursor, sem modificar os campos seguintes.

Alterações válidas são salvas imediatamente no esquema JSON do usuário. Alterações inválidas, como mover um campo para fora do tamanho fixo do registro, são rejeitadas.

### 🛠️ Trecho Técnico (Exemplo de Compilação PAK)
Código base para compilar uma pasta de modificações usando a especificação do cliente Japonês (V3):

 ```csharp

using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using PangyaAPI.Utilities.Cryptography;

var writer = new PakWriter
{
    EntryVersion = PakFileEntryVersion.V3,
    EntryType = PakFileEntryType.LZ772,
    CompressLevel = 5,
    LocationKeys = PakKeys.JP, // xtea_key
    Author = "SuiteTools"

};
// Compila recursivamente mantendo a integridade dos offsets
writer.CreateFromDirectory(@"C:\Modding\data", @"C:\Games\PangYa\ProjectG.pak"); ```

```
