# LAST RAIL Unity Android Keystore Secure Token Storage Spec

Updated: 2026-08-29 KST

## Scope

This document defines the launch-facing secure token storage direction for the Unity client. It is a specification slice only.

No Unity package, Google Sign-In SDK, secure storage plugin, UnityWebRequest transport, OAuth client id, or production secret is added in this slice.

## Decision

Act 1 launch token storage must be backed by Android Keystore-protected secret storage. PlayerPrefs, encrypted PlayerPrefs, and preference-only storage are not acceptable for LAST RAIL auth tokens.

Recommended path:

1. Prefer a vetted Unity secure storage plugin only if it proves Android Keystore support on Android, optional Keychain support on iOS, IL2CPP Android compatibility, overwrite/delete support, active maintenance, and no raw token logging.
2. If no plugin passes review, implement a small first-party Android native bridge behind the existing Unity port. The bridge should use Android Keystore-protected encryption keys and store only encrypted token payloads in app-private storage.
3. Keep the Unity-facing API behind `IGameApiAuthTokenStore` so auth flows remain testable and token storage can be swapped without touching gameplay, gacha, reward, or story flows.

Unity's Android signing keystore is not runtime auth token storage. It is used for signing Android builds and must not be treated as a replacement for Android Keystore-backed secret storage.

## Current Unity Boundary

Existing pure C# auth flows already converge on `GameApiAuthSessionSnapshot`.

The real storage implementation must support:

- saving the current auth session snapshot after guest auth, Google auth, Google link, or refresh success
- loading the current auth session snapshot at app start
- replacing access token and refresh token values atomically after refresh rotation
- clearing stored auth state on logout, unrecoverable auth reuse, or account reset flows
- returning failure without logging token values

Current port:

- `IGameApiAuthTokenStore.SaveAsync(GameApiAuthSessionSnapshot value)`
- `IGameApiAuthTokenStore.LoadAsync()`

Spec gap for the implementation slice:

- Add or adapt an explicit clear/delete operation before wiring real logout or `refresh_token_reused` handling.
- Decide whether delete failure should block logout UX or only show a retryable storage warning.

## Candidate Evaluation

### Vetted Unity Secure Storage Plugin

Status: preferred if it passes review.

Required evidence:

- Android stores encryption key material through Android Keystore or an equivalent platform keystore wrapper.
- Token payloads are not stored as plaintext PlayerPrefs.
- iOS Keychain support is available or cleanly optional.
- Works in IL2CPP Android builds, ARM64, release signing, and min SDK target chosen for launch.
- Supports save, load, overwrite, delete, and clear.
- Does not print token values, raw auth bodies, account shortcuts, or secrets to logs.
- Provides a small API that can be wrapped by `IGameApiAuthTokenStore`.
- Has maintained source, release history, license compatibility, and reproducible package installation steps.

### First-Party Native Android Bridge

Status: fallback if no plugin passes review.

Recommended shape:

- Unity C# adapter implements `IGameApiAuthTokenStore`.
- Android native plugin owns secure persistence.
- Android Keystore holds non-exportable key material.
- Encrypted payload storage remains app-private.
- C# receives success/failure metadata only, never raw debug payloads.
- Logs include storage operation status and error code only.

This path costs more implementation time but gives stronger auditability and removes reliance on an unclear third-party package.

### Xamarin, Capacitor, Or Non-Unity Secure Storage Packages

Status: design references only.

These packages can inform API shape and platform behavior, but they should not be imported directly unless they are explicitly compatible with Unity IL2CPP Android builds.

### PlayerPrefs-Based Storage

Status: rejected for auth tokens.

Rejected examples include PlayerPrefs wrappers, encrypted preference helpers, and convenience packages that do not prove Android Keystore-backed key protection. They may be acceptable for non-sensitive preferences, but not for access tokens or refresh tokens.

## Storage Semantics

Save:

- Accept exactly one current `GameApiAuthSessionSnapshot`.
- Store access token, refresh token, expirations, account id, account status, and token type as one logical payload.
- Never split refresh token replacement into a partial write that can leave a mismatched token pair.

Load:

- Return the stored snapshot when present and readable.
- Return null or a typed missing-state result when absent.
- Return a typed failure when the encrypted payload cannot be decrypted.

Rotate:

- After successful `POST /v1/auth/refresh`, replace the stored snapshot with the new access token and refresh token bundle.
- If secure storage write fails after a server refresh succeeds, surface a storage failure state and avoid logging token values.
- Do not retry refresh with an already rotated refresh token without the existing auth flow policy deciding it is safe.

Clear:

- Remove the encrypted payload and any storage metadata owned by the token store.
- Required for logout and `refresh_token_reused` recovery.
- Must not clear unrelated player settings, graphics preferences, or local narrative presentation data.

## Debug And Logging Rules

Debug JSON, diagnostics, exceptions, and logs must not include:

- raw access token
- raw refresh token
- raw Google id token
- Authorization header value
- account shortcut header
- raw auth request body
- client secret
- signing key
- raw RNG material
- reward internals

Allowed diagnostic fields:

- storage operation kind
- outcome
- safe error code
- retryable flag
- requires reauth flag
- token presence boolean
- expiry presence boolean

Do not include token prefixes, token lengths, hashes, or partial token values unless a later security review explicitly approves a redaction standard.

## Future Wrapper Contract

Recommended implementation boundary:

```csharp
public interface ISecureAuthTokenStorageBackend
{
    Task<SecureAuthTokenStorageResult> SaveAsync(string encryptedPayloadLabel, string serializedSnapshot);
    Task<SecureAuthTokenStorageLoadResult> LoadAsync(string encryptedPayloadLabel);
    Task<SecureAuthTokenStorageResult> DeleteAsync(string encryptedPayloadLabel);
}
```

`IGameApiAuthTokenStore` should remain the game-facing port. Any native plugin, Java bridge, or platform-specific implementation should sit behind a small adapter and be tested with fake backends first.

## Verification Checklist

Before shipping a real storage implementation:

- Android IL2CPP release build can save, load, overwrite, and delete tokens.
- App restart preserves the token snapshot.
- Refresh rotation replaces both access and refresh tokens.
- Logout clears the snapshot.
- `refresh_token_reused` path clears or quarantines the session according to auth UX policy.
- Logcat contains no token, Google id token, Authorization value, account shortcut, raw auth body, or secret.
- Android backup/restore behavior is reviewed so encrypted payloads are not restored into an undecryptable or unsafe state.
- iOS Keychain behavior is reviewed if iOS enters launch scope.
- Unit tests continue to use fake storage and never require the real plugin.

## Decision Queue

- Select a specific Unity secure storage plugin, or approve first-party Android native bridge work.
- Confirm launch Unity version, Android min SDK, target SDK, ARM64, IL2CPP, and managed stripping level.
- Decide whether to add `ClearAsync` directly to `IGameApiAuthTokenStore`.
- Decide storage failure UX after successful refresh rotation.
- Decide Android backup exclusion or migration behavior for encrypted token payloads.
- Decide whether iOS Keychain support is launch required or backlog.

## References

- Android Keystore system: https://developer.android.com/privacy-and-security/keystore
- Android cryptography guidance: https://developer.android.com/privacy-and-security/cryptography
- AndroidX Security Crypto release notes: https://developer.android.com/jetpack/androidx/releases/security
- EncryptedSharedPreferences API status: https://developer.android.com/reference/androidx/security/crypto/EncryptedSharedPreferences
- Apple Keychain Services: https://developer.apple.com/documentation/security/keychain-services/
- Unity Android keystore signing documentation: https://docs.unity.cn/Manual/android-keystore.html

## Boundaries

- No plugin import or package installation in this slice.
- No Google SDK import.
- No UnityWebRequest or HttpClient implementation.
- No OAuth client id, audience, secret, or production credential.
- No PlayerPrefs token storage.
- No account id request shortcut.
- No reward, economy, story, unlock, offline reward, gacha probability, pity, ledger, RNG, or server truth calculation.
- Rail Choice Events remain outside Act 1 launch scope.
