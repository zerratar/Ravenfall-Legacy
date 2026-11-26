# Tower Places: reviewing and fixing the tower by hand

Ravenfall > Tower Places is a window for going through every place of the tower one by one, fixing what stands in a
weird spot, and keeping those fixes when a place is built again by its script (tools/tower/*-build.cs).

## Going through the places

- The list on the left has every floor with its town first and then its parts, in climbing order, and at the end the
  tower scenes that are not in the climb yet (the great hall). New floors and parts show up by themselves.
- Click a place: its scene opens beside MainWorld and the Scene view looks at it from above where players arrive.
  MainWorld stays open and is never saved, closed or switched by the window. The place is the active scene while it is
  open, so a prop you drag in lands in the place, not in MainWorld.
- All tower scenes are built on the same spot, so one is open at a time. Opening another closes the one before (it asks
  first when that one has changes that are not saved).
- Previous and Next (or the [ and ] keys) step through the list; Next not reviewed jumps to the next place without a
  tick.
- Several parts share one scene (parts 1, 2, 5 and 6 are all the courtyard). The window says so under the part's name.
  Fixing the scene once fixes it for all of them.
- Tick Reviewed and write a note if you like. Both are saved straight away in tools/tower/layouts/review.json (one line
  per place), so commit that file. The top of the window counts how many places are reviewed.

## Markers

While the window is open, the Scene view shows where players arrive (green), the monster spots (red), the boss (orange),
gates (yellow), town people (blue), work stands (cyan), rest spots (purple) and strolls (pale green). Each kind can be
switched off in the window. Big things always have a label; small ones show theirs when the camera is near. Click a label
to select its object.

## Save, Check, Rebake walking, Play

- Save place saves the open tower scene only. Ctrl+S works too.
- Check runs the tower audit: can every monster spot, the boss, the town's people, rest spots, work stands, strolls
  and road points be walked to from the arrival? Problems are listed in red; click one to look at it.
- Rebake walking bakes the place's walking ground (its navmesh) again, the same way its build script does, then runs
  Check. Do this after moving anything people walk on or around (rocks, walls, trees with colliders). The navmesh file
  (Assets/Scenes/Tower/<Scene>/NavMesh.asset) is in a gitignored folder: commit it with `git add -f`.
- Play this part starts the game and sends one test bot straight to that part or town, and the camera follows it. The
  local server must be running and the game must sign in by itself (the saved device token); without that, the place
  is loaded without a bot and the window says why. With Quiet chat ticked (the default), nothing from the test reaches
  a real chat. Stop play mode as usual when done.

## Your edits survive a rebuild

Every object a build script makes has a fixed id (kept in each scene on an object called "Tower Place Ids (editor only)",
which is left out of player builds; do not delete it). When you save a place, the window writes what you changed to
tools/tower/layouts/<Scene>.edits.json. Commit that file with the scene.

When a build script runs again it gives the new scene the same ids and then puts your edits back, and bakes the
walking ground again if anything changed. You can also do it yourself with Reapply my edits.

What is kept:

- objects moved, turned or scaled, put under another parent, renamed, hidden or shown
- objects deleted
- objects added: a prefab, a copy (Ctrl+D) of an object of the place, or an empty object
- the same for the pieces inside a prefab copy (a wall piece of a house)

What is not kept (do these in the build script instead, or redo them after a rebuild):

- terrain sculpting and painting
- material changes, and a component's own values (a light's color, a collider's size, a script's settings)
- the order of objects in the hierarchy
- changes inside something you added (the pieces of an added copy)
- an object made from scratch (not a prefab and not a copy) comes back as an empty object with only its name and place
- anything outside the scene (meshes, prefabs, materials)

Show my edits lists every edit of the open place, with Look (select it and look at it) and Revert (put it back as it was
built; a deleted object comes back from the scene as it is in git, or as a fresh copy of its prefab).

If a build script changes so much that an edit's object is no longer built, the edit is not lost: it stays in the file
under "unapplied" and shows in red in the list, with Forget to drop it.

A place built before 2026-11-26 got its ids from how it stood that day; a hand edit made before then is not in any
change list and goes away on a rebuild.
