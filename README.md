# Guribo Udon Utils

Scripts that make developing with UDON for VRChat safer and more fun :)

## Features

* automatically checks the scene for empty public variables (*null*) and logs to the console (every time assets get saved)
* allows checking manually via menu entry to allow instant navigation to errors

## Usage

Use via menu or silently by saving the scene (Ctrl+s):
![Menu](README/menu.png)

Skip or show errors:
![Error Dialog](README/errorMessage.png)

Locate empty variable by name and allow easy fixing of variable (optionally (¬‿¬) ):
![Error Location](README/errorLocation.png)

Warns about potential errors is the console and clicking on the message will alos highlight the affected gameobject in the hierarchy:
![Error Navigation in Console](README/errorNavigationInConsole.png)

In case all errors get skipped a conclusion will be presented:
![Conclusion on Skipping](README/conclusion.png)