# Buttonboard Documentation 🎛️

Buttonboard is a modular show-control system for running interactive setups, Halloween shows, and custom automation workflows with timed scenes.

## Current Features

- Scene control using human-readable DSL files (`*.scene`) with continued support for JSON scenes.
- Timed execution of audio, video, GPIO, MQTT, and LCD actions.
- Group definitions and target expansion in scenes, for example for multiple VLC players, MQTT topics, or LEDs.
- Audio playback, pause/resume, and volume control through Lyrion/Squeezebox players.
- VLC-based video control for playlist next, pause/resume, and playing specific playlist entries.
- GPIO control for button, process, and system LEDs, including blink support.
- LCD output with clear, text write, one-line or two-line views, alignment, and backlight control.
- MQTT integration with online/offline status, publish actions, and reset payloads for configured devices.
- Central configuration via `appsettings.json` for runtime mode, scenes, OpenHAB, Lyrion, VLC, LCD, MQTT, and logging.
- Structured logs with stable event IDs for the asset loader, runtime, actions, and integrations.
- Raspberry Pi mediaplayer setup for fullscreen VLC kiosk playback with HTTP control and autostart.

## Table of Contents

- [Configuration](./1-%20Configuration.md) – central configuration, integrations, device mappings, and logging.
- [Scenarios](./2%20-%20Scenarios.md) – scene formats, DSL rules, groups, and target expansion.
- [Actions](./3%20-%20Actions.md) – available audio, video, GPIO, MQTT, and LCD actions.
- [Logging Events](./4%20-%20LoggingEvents.md) – event ID structure and log categories.
- [Mediaplayer](./5%20-%20Mediaplayer.md) – Raspberry Pi VLC kiosk setup for video output.
