Architecture.md: Build a simple cross-platform synchronized clipboard application named **Omnia**.

Purpose:
Help developers, designers, and digital workers instantly transfer short text, URLs, and temporary notes across multiple devices. The application provides a zero-friction input field and synchronizes data in real-time across any device connected via a shared 4-digit room PIN, without requiring user registration.

Use this stack:

* Frontend: Flutter + Dart
* Backend & Database: Supabase (PostgreSQL + Realtime)
* State Management: Riverpod
* API style: Supabase Client SDK (WebSockets for real-time, REST for CRUD)
* Use `.env` or `config.json` for Supabase URL and Anon Key

Code rules:

* Do not add comments unless truly necessary.
* Use PascalCase for all classes, types, models, widgets, and database entities.
* Local variables and methods may use camelCase.
* Keep code lines below 150 characters where practical.
* Use a clean and simple modular folder structure inside the `lib/` directory.
* Do not add traditional authentication (OAuth/Email) in this version. Assume users authenticate anonymously via a 4-digit room code.

Main entities:

1. Room

    * PinCode (Primary Key, String, 4 digits)
    * LastActiveAt
    * CreatedAt

2. Clip

    * Id (UUID)
    * RoomPinCode (Foreign Key)
    * Content (Text)
    * DeviceId (Optional string to identify sender)
    * CreatedAt

Database rules:

* A `Room` is uniquely identified by its 4-digit `PinCode`.
* A `Clip` must be linked to a valid `RoomPinCode`.
* When a clip is swiped away, it is permanently deleted from the database.
* Enable Supabase Realtime for the `Clip` table to broadcast inserts and deletes to all subscribed clients instantly.
* Setup Row Level Security (RLS) policies allowing read/write/delete access only for users providing the correct `RoomPinCode`.

Backend features (Supabase Config):

1. Room Management

    * Create a new room with a random 4-digit PIN if the user does not have one.
    * Update `LastActiveAt` when a room is accessed.

2. Clip Synchronization

    * Insert a new clip with text content and associate it with the active `RoomPinCode`.
    * Delete a specific clip by `Id`.
    * Subscribe to real-time changes on the `Clip` table filtered by `RoomPinCode`.

Frontend screens:

1. Main Workspace

    * Opens instantly to an active, autofocus text field with the keyboard deployed.
    * Simple list below the input showing chronological clips.
    * Tap a clip: Copies text to the device clipboard automatically and shows a success toast.
    * Swipe right: Deletes the clip from the list and the Supabase database.
    * Read-only connection status indicator (Online/Offline).

2. Room Configuration Modal (Overlay/Dialog)

    * Displays the current 4-digit PIN.
    * Input field to enter a different 4-digit PIN to join another device's room.
    * Button: `Gabung Ruangan`.

UI requirements:

* Use Indonesian language for all labels, buttons, messages, and validation.
* Create a hyper-minimalist, distraction-free interface (Dark Theme default: pure black background, white text).
* Remove unnecessary app bars, floating action buttons, or bottom navigation.
* Use status indicators:
    * Connected: Green dot
    * Reconnecting: Orange dot
* Show a transient Snackbar notification when text is successfully copied to the clipboard.

Required SDK Operations (Equivalent to API Routes):

* `POST /rest/v1/room` (Create Room)
* `GET /rest/v1/room?PinCode=eq.{pin}` (Validate Room)
* `GET /rest/v1/clip?RoomPinCode=eq.{pin}` (Fetch initial clips)
* `POST /rest/v1/clip` (Insert new clip)
* `DELETE /rest/v1/clip?Id=eq.{id}` (Delete clip on swipe)
* `REALTIME /clip?RoomPinCode=eq.{pin}` (WebSocket subscription for instant sync)

Deliverables:

* Complete Flutter cross-platform source code (Android, iOS, Web, Windows).
* Supabase SQL schema setup file (`schema.sql`) including Table definitions and RLS policies.
* `.env.example` containing required Supabase keys.
* README with installation, Supabase project setup, Flutter run instructions, and build commands.
* Ensure the application builds successfully and real-time synchronization works across at least two different platforms (e.g., Web and Android) under 2 seconds.

Project structure:

* Use a modular feature-first or layer-first structure inside `lib/`.
* Structure:

```text
omnia/
  lib/
    models/
      Room.dart
      Clip.dart
    screens/
      OmniaHome.dart
    services/
      SupabaseService.dart
      ClipboardService.dart
    providers/
      RoomProvider.dart
      ClipListProvider.dart
    widgets/
      ClipListItem.dart
      RoomPinDialog.dart
    main.dart
  supabase/
    schema.sql
  .env.example
  pubspec.yaml