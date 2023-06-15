# wks_converter
Takes a pcbnew file, and converts the straight lines and text on the User.Drawings layer to the kicad_wks format.

So you could import a DXF format template/title block to pcbnew, then save the file and use this program to convert it for use in eeschema and pcbnew. You will need to add fields manually using pl_editor still.

Curves are ignored because pl_editor doesn't support them, I ended up replacing the curves with straight segments in a separate program and inserting the result into pcbnew, although in hindsight it would have been easier to draw the straight line segments directly in pcbnew. Naturally it would be better if this program could do that for you, but it can't yet.

Only suitable for KiCad 7.0 files and possibly later versions. It might work on older versions of KiCad but I haven't tested that.