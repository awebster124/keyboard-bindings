# Keyboard Key Remapping Service

A small ASP.NET Core web service that stores and serves key remappings for the
**Apex Pro Gen 3** keyboard. Keys are identified by their USB HID usage codes
(Usage Page 0x07, per the USB HID Usage Tables specification).

## Stack

- **.NET 9** / ASP.NET Core minimal API
- **SQLite** persistence via **Entity Framework Core**

## Status

Early scaffolding. Persistence, endpoints, and tests to follow.
