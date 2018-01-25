# shiplike
Tile-based MonoGame demo using Tiled Map Editor

# Intro
This is a simple tile-based 2d platformer written in C# using the MonoGame framework

I build the levels using an open-source tile editor called Tiled http://www.mapeditor.org/
It's pretty excellent but I haven't used it for anything but this project.

I designed the tiles using PyxelEdit http://pyxeledit.com/get.php 

I intend to use this for a tutorial. I retain all the copyrights on the files here, except where
they belong to other people, but you can use them for any purpose, commercial or otherwise. 
If a lawyer wants a name for the license use the 3-clause BSD license for the source code and
the graphics are released under Creative Commons. 

The cat sprite is not my work. I found it here https://opengameart.org/users/dogchicken

# Windows

I have mostly worked on this project using Visual Studio for the Mac (formerly known as MonoDevelop), 
but it will build on Windows using VS 2017 Community Edition. You need to do the following:

1. Install Visual C++ 2013 redistributable
2. Install MonoGame
3. Rebuild the assets using the MonoGame pipeline tool

It seems that Monogame 3.6 changed the pipeline format so the Windows version doesn't load PNG
files directly from the content directory. You need to fire up the Monogame pipeline tool and
execute the build command. This will generate the XNA assets required on Windows, which will then
need to be added to the build directory (there's a property editor in VS to do this).

The Monogame pipeline tool will fail if you don't have the VC++ 2013 redistributable installed.

# Mac / Linux

Seems to just work, but I haven't loaded it from scratch. Let me know if you have problems and 
I will help.

# Dependencies 

There is a github project called TiledSharp that wraps the Tiled editor XML files in C# classes.
I added the depdendency using NuGet and Visual Studio 2017 on Windows seemed happy enough with that.
I also added MonoGame Desktop runtime but I'm not sure about that.

# Other platforms

I'm sure you could get this code working on Android or iOS but you'd have to do something about 
the keyboard. Maybe there's a D-pad component out there that will make it easier on mobile platforms.
