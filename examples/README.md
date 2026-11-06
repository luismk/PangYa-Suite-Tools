# Fresh message preview

`fresh-message.xml` is a self-contained layout with no game image dependencies.
Open it in the PangYa UI Editor to inspect the initial static message and edit
background. Runtime message replacement can be exercised through the API below
in a Windows application referencing `PangyaAPI.UI`.

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using PangyaAPI.UI;

string path = Path.GetFullPath("examples/fresh-message.xml");
PangyaUiDocument document = PangyaUiDocument.Load(path);
PangyaUiNode form = document.Nodes.Single(node => node.IsForm);
using var images = new PangyaFileImageProvider(Path.GetDirectoryName(path)!);
var renderer = new PangyaUiRenderer(images);
using var bitmap = new Bitmap(360, 180);
using Graphics graphics = Graphics.FromImage(bitmap);
string message = "Replacement message\n" +
    "This longer paragraph should wrap inside the edit control without " +
    "changing the XML or the heading above it.";
PangyaUiRenderOptions options = renderer.SetFormMessage(
    graphics, form, message, spaceAlign: true);
renderer.Render(graphics, document, form, options);
bitmap.Save("fresh-message-preview.png", ImageFormat.Png);
```

Expected: the heading remains unchanged; the yellow static message receives the
replacement; the edit displays wrapped white text inside its blue background.
Both message controls are direct children named `message`. Replacing the message
with an empty string clears both. The static caption retains Fresh's normal
100-byte truncation behavior, so a long replacement has a shortened yellow copy.

The example has not been executed as part of the current implementation work.
Font metrics and pixel parity still require Windows visual verification. Generated
PNGs should stay outside version control.
