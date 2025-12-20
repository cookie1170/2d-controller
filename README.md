# 2d-controller
A Unity character controller template for 2D

It currently includes:
- A base `Controller2D` class, which utilizes [`Rigidbody2D.Slide()`](https://docs.unity3d.com/ScriptReference/Rigidbody2D.Slide.html) for movement
- A sidescroller controller with some basic features:
  - Movement and acceleration
  - Intuitive jump configuration
  - Coyote time and jump buffering
  - (Optionally) variable jump height
- A simple topdown controller

NOTE: This is intended to be modified, most games would require changes. This simply serves as a template to get started faster
