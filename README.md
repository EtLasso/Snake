How to Start Snake v0.8
Prerequisites

    Visual Studio installed

    Windows operating system

Download the contents of the publish folder directly, which contains everything needed to run the game without building it yourself.


    Download all files from the publish folder together to the same directory on your PC.

    Double-click [Snake.exe] to launch the Snake game window and start playing immediately.

Steps with Visual Studio

    Clone the repository

    bash
    git clone https://github.com/EtLasso/Snake.git

    Open the project

        Open the cloned folder in Visual Studio.

    Restore dependencies

        Use the NuGet Package Manager to install any missing libraries if needed.

    Build the solution

        Press Ctrl + Shift + B or click on "Build Solution" to compile the project.

    Run the game

        Press F5 or click "Start" to launch the Snake game window.

Project Structure

    Controllers/ – Contains the logic and control code

    Models/ – Manages the game state

    Views/ – Handles the user interface

    Other key files: Program.cs, Form1.cs

Notes

    The project follows the Model-View-Controller (MVC) pattern for a clean separation of concerns.

    After starting the application, you can play Snake in the opened window.
