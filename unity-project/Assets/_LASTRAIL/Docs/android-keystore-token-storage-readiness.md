# Android Keystore Token Storage Readiness

Act 1 launch requires Game API auth tokens to be stored through Android Keystore backed secure storage.

Current Unity boundary:
- `IGameApiAuthTokenStore` remains the game-facing token store port.
- `AndroidKeystoreGameApiAuthTokenStore` adapts that port to `IAndroidKeystoreAuthTokenBridge`.
- The native bridge is compile-safe and testable, but no external plugin or Android native implementation is imported in this slice.
- Save calls are overwrite-safe so refresh token rotation can replace the stored access/refresh token bundle.
- Clear supports logout/session reset.
- Availability/readiness is exposed without raw token values.

Required approval before real device storage:
- Select and import a vetted Unity secure storage plugin with Android Keystore support.
- Confirm IL2CPP Android build compatibility.
- Confirm clear/delete behavior for logout.
- Confirm migration behavior if storage keys change.
- Confirm token values are never logged by the plugin wrapper.

Launch security boundaries:
- Do not store tokens in PlayerPrefs.
- Do not add account shortcut headers or body fields.
- Do not write raw access, refresh, or Google ID token values to debug JSON or logs.
- Do not add Google OAuth client id/audience values until app registration/PRD setup.
