# Versioned Balance

Published balance versions are immutable. Admin edits happen only in drafts. Publish validates expected revision and checksum, inserts a new version, records activation history, and moves the active pointer atomically.

Rollback never edits a version; it changes the active pointer and appends activation history.
