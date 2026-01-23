AI Generated:
---

# Dash Game Prototype (2D)

A simple top-down 2D prototype focused on **dash-based movement** and **strong game feel** through sound and visual feedback.

---

## Controls

* **WASD** — Move
* **Dash Button** (Space / Shift) — Dash in movement direction

---

## Core Mechanics

### Movement

* Top-down camera
* WASD movement
* Normalized input to prevent faster diagonal movement
* Rigidbody2D-based movement
* Camera follows player with smoothing

### Dash System

* Dash triggered by button press
* Dash direction cached from last movement input
* Normal movement disabled during dash
* Dash uses impulse or timed position interpolation
* Dash duration and cooldown
* Optional input buffering for responsiveness

### Enemies

* Simple dash targets
* Tagged as `"Enemy"`
* Collider2D with trigger detection
* Disappear when dashed into or sliced by effects
* Optional object pooling instead of destroy

---

## Visual Effects

* Motion trail during dash
* Screen shake on impact
* Camera zoom during dash
* Sprite stretch / squash
* Directional slash or impact sprite
* Hit-stop (brief pause on successful hit)

---

## Sound Design (Game Feel Focus)

### Dash Sounds

* **Dash button pressed**

  * Short charge or “whoosh start” sound
  * Plays immediately on input
  * Slight pitch variation

* **Dash in progress**

  * Looping wind / air-slice sound
  * Fades in at dash start, fades out at dash end
  * Volume scales with dash speed

* **Dash hit**

  * Sharp impact / slice sound
  * Layered audio:

    * Impact thud
    * Blade / slash sound
  * Pitch or volume based on hit speed

* **Dash miss (empty space)**

  * Softer air-cut or whiff sound
  * Lower volume than hit
  * Optional reverb tail

---

## Additional Sound Opportunities

* Subtle movement loop (footstep / hover sound)
* Dash cooldown ready sound
* Enemy spawn sound
* Enemy death sound variations (3–5 randomized)
* Low-health or danger ambience
* Audio ducking during dash impact
* Directional audio panning based on hit location

---

## Design Goals

* Make every player action feel responsive
* Reinforce success and failure through sound
* Avoid silent or feedback-less interactions
* Prioritize clarity and punch over realism

---

## Notes

* Focused on rapid iteration and feel over complexity
* Designed to be easily expandable with new effects or enemy types

---

Just say which 👍
