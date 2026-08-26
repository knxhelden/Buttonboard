# Buttonboard Documentation

Buttonboard is a modular show-control system for running interactive setups, Halloween shows, and custom automation workflows with timed scenes.

## Current Features

- Scene control using human-readable DSL files (`*.scene`) with continued support for JSON scenes.
- Timed execution of audio, video, GPIO, MQTT, and LCD actions.
- Group definitions and target expansion in scenes, for example for multiple VLC players, MQTT topics, or LEDs.
- Playback, pause/resume, and volume control through Lyrion players.
- VLC-based video control for playlist next, pause/resume, and playing specific playlist entries.
- GPIO control for button, process, and system LEDs, including blink support.
- LCD output with clear, text write, one-line or two-line views, alignment, and backlight control.
- MQTT integration with online/offline status, publish actions, and reset payloads for configured devices.
- Global configuration via `appsettings.json` plus scenario-specific device inventories in `hardware.json`.
- Structured logs with stable event IDs for the asset loader, runtime, actions, and integrations.
- Raspberry Pi mediaplayer setup for fullscreen VLC kiosk playback with HTTP control and autostart.

## Table of Contents

- [Installation](./Installation.md) - Raspberry Pi setup and Buttonboard runtime installation.
- [Hardware](./Hardware.md) - required hardware components and complete GPIO wiring.
- [Configuration](./Configuration.md) - central configuration, integrations, device mappings, and logging.
- [Scenarios](./Scenarios.md) - scene formats, DSL rules, groups, and target expansion.
- [Actions](./Actions.md) - available Lyrion, video, GPIO, MQTT, and LCD actions.
- [Logging Events](./LoggingEvents.md) - event ID structure and log categories.
- [Mediaplayer](./Mediaplayer.md) - Raspberry Pi VLC kiosk setup for video output.
