# PhotoCull

PhotoCull is a tool for choosing the best images from a set of similar ones.
It is intended for people who, like me, tend to take potentially dozens of
pictures of the same thing and hope one of them comes out well in the end.  It
helps the process of picking that "one good one" by allowing you to compare
them in pairs, side-by-side, until you're down to just the ones you want to
keep.

## Install
All that's needed is PhotoCull.exe.  Either build from source using
Visual Studio or download a pre-built binary from one of the tagged
releases.

## Usage

Open the application, and press the "Choose images..." button in the lower
left corner.  Select a set of images from which you probably only want to
keep a small number, and hit OK.  Wait for the images to load (it may take a
while, especially if the images are high resolution - sometimes up to a second
per image in the set).

You will be presented with two images side by side.  Usually you'll want to
maximize the window for this comparison.

### Zooming
Right-click either image to zoom in to "actual pixels" size, and move the
mouse around the image to pan both sides (for example, to compare focus
quality).  Right-click again to zoom back out.

### Choosing a preferred image
Left-click the image you prefer.  The other image will then be crossed out in
red in the list on the left-hand side.  If you want to keep both images (for
example, if they are capturing different things), hit the "Keep both, for now"
button on the bottom.  As you select preferred images, it will continue to find
images you have not compared before indefinitely.  If you find yourself
comparing images which have been crossed out in the list, you've gone through
all of them already but may continue if there's a comparison you want to
double-check.

If you right-click an image in the list on the left-hand side of the window,
you can check or un-check the "Rejected" box to un-reject an image if you made
a mistake.

Once the comparisons are done, the "Delete rejects..." button will, after a
confirmation dialog, delete the crossed-out files from the disk.

## Known issues

* PhotoCull ignores image orientation metadata.  Most cameras will detect
if they were being held vertically when the photo was taken and record that
in the metadata for the image.  Many programs will rotate the image
accordingly.  PhotoCull doesn't know how to do that.

* If you edit an image, PhotoCull may not reload the edited version until it is
completely restarted.

* Unchecking the "Rejected" checkbox for an image sometimes doesn't properly
update the display of the red cross-out properly.

* After deleting all but one image, sometimes the selection panes will show an
image which is no longer in the set.
