using System;
using LastRail.Auth;
using LastRail.Presentation.AuthGate;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AuthGateUxStateTests
    {
        [Test]
        public void FirstLaunch_ShowsGoogleAndGuestEntryActions()
        {
            var state = AuthGateUxState.FirstLaunch();

            Assert.That(state.Kind, Is.EqualTo(AuthGateUxStateKind.StartOptions));
            Assert.That(state.Title, Is.EqualTo("LAST RAIL 시작"));
            Assert.That(state.PrimaryActionLabel, Is.EqualTo("Google로 시작"));
            Assert.That(state.SecondaryActionLabel, Is.EqualTo("게스트로 계속"));
            Assert.That(state.CanStartGoogleSignIn, Is.EqualTo(true));
            Assert.That(state.CanContinueAsGuest, Is.EqualTo(true));
            Assert.That(state.CanLinkGoogle, Is.EqualTo(false));
            Assert.That(state.HasSession, Is.EqualTo(false));
            Assert.That(state.HasRawTokenPayload, Is.EqualTo(false));
            Assert.That(state.HasAccountShortcut, Is.EqualTo(false));
            Assert.That(state.IsDisplayOnly, Is.EqualTo(true));
        }

        [Test]
        public void FromGuestSession_EnablesGuestContinueAndGoogleLinkPromptWithoutTokenValues()
        {
            var state = AuthGateUxState.FromGuestSession(Session("ACTIVE"));

            Assert.That(state.Kind, Is.EqualTo(AuthGateUxStateKind.GuestReady));
            Assert.That(state.Title, Is.EqualTo("게스트 진행 중"));
            Assert.That(state.PrimaryActionLabel, Is.EqualTo("계속하기"));
            Assert.That(state.SecondaryActionLabel, Is.EqualTo("Google 계정 연결"));
            Assert.That(state.CanContinueAsGuest, Is.EqualTo(true));
            Assert.That(state.CanLinkGoogle, Is.EqualTo(true));
            Assert.That(state.HasSession, Is.EqualTo(true));
            Assert.That(state.AccountStatus, Is.EqualTo("ACTIVE"));
            Assert.That(state.HasRawTokenPayload, Is.EqualTo(false));
            Assert.That(state.HasAccountShortcut, Is.EqualTo(false));
        }

        [Test]
        public void FromGoogleAuthFlow_MapsSignedInConflictCancelledProviderAndNetworkStates()
        {
            var signedIn = AuthGateUxState.FromGoogleAuthFlow(GameApiGoogleAuthFlowResult.FromClientResult(
                GoogleIdTokenProviderOutcome.Success,
                GameApiGoogleAuthClientResult.SignedIn(GoogleAuthResponse())));
            var conflict = AuthGateUxState.FromGoogleAuthFlow(GameApiGoogleAuthFlowResult.FromClientResult(
                GoogleIdTokenProviderOutcome.Success,
                GameApiGoogleAuthClientResult.Conflict("google_identity_already_linked", "이미 연결된 Google 계정입니다.")));
            var cancelled = AuthGateUxState.FromGoogleAuthFlow(GameApiGoogleAuthFlowResult.FromProviderFailure(
                GoogleIdTokenProviderResult.Cancelled("사용자가 취소했습니다.")));
            var providerError = AuthGateUxState.FromGoogleAuthFlow(GameApiGoogleAuthFlowResult.FromProviderFailure(
                GoogleIdTokenProviderResult.ProviderError("google_sign_in_failed", "Google 로그인을 완료하지 못했습니다.")));
            var network = AuthGateUxState.FromGoogleAuthFlow(GameApiGoogleAuthFlowResult.FromProviderFailure(
                GoogleIdTokenProviderResult.NetworkError("네트워크 오류")));

            Assert.That(signedIn.Kind, Is.EqualTo(AuthGateUxStateKind.GoogleSignedIn));
            Assert.That(signedIn.HasSession, Is.EqualTo(true));
            Assert.That(conflict.Kind, Is.EqualTo(AuthGateUxStateKind.AccountRecoveryRequired));
            Assert.That(conflict.RequiresAccountRecovery, Is.EqualTo(true));
            Assert.That(conflict.ErrorCode, Is.EqualTo("google_identity_already_linked"));
            Assert.That(cancelled.Kind, Is.EqualTo(AuthGateUxStateKind.Cancelled));
            Assert.That(providerError.Kind, Is.EqualTo(AuthGateUxStateKind.ProviderError));
            Assert.That(network.Kind, Is.EqualTo(AuthGateUxStateKind.RetryableNetworkError));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
        }

        [Test]
        public void FromGoogleLinkFlow_MapsLinkedAlreadyLinkedAuthRequiredConflictAndNetworkStates()
        {
            var linked = AuthGateUxState.FromGoogleLinkFlow(GameApiGoogleLinkFlowResult.FromClientResult(
                GoogleIdTokenProviderOutcome.Success,
                GameApiGoogleLinkClientResult.Linked(GoogleLinkResponse(GameApiGoogleLinkStatus.Linked))));
            var alreadyLinked = AuthGateUxState.FromGoogleLinkFlow(GameApiGoogleLinkFlowResult.FromClientResult(
                GoogleIdTokenProviderOutcome.Success,
                GameApiGoogleLinkClientResult.AlreadyLinked(GoogleLinkResponse(GameApiGoogleLinkStatus.AlreadyLinked))));
            var authRequired = AuthGateUxState.FromGoogleLinkFlow(GameApiGoogleLinkFlowResult.FromClientResult(
                GoogleIdTokenProviderOutcome.Success,
                GameApiGoogleLinkClientResult.AuthRequired("auth_required", "인증이 필요합니다.")));
            var conflict = AuthGateUxState.FromGoogleLinkFlow(GameApiGoogleLinkFlowResult.FromClientResult(
                GoogleIdTokenProviderOutcome.Success,
                GameApiGoogleLinkClientResult.Conflict("google_identity_already_linked", "이미 연결된 Google 계정입니다.")));
            var network = AuthGateUxState.FromGoogleLinkFlow(GameApiGoogleLinkFlowResult.FromProviderFailure(
                GoogleIdTokenProviderResult.NetworkError("네트워크 오류")));

            Assert.That(linked.Kind, Is.EqualTo(AuthGateUxStateKind.GoogleLinked));
            Assert.That(linked.LinkStatus, Is.EqualTo("LINKED"));
            Assert.That(alreadyLinked.Kind, Is.EqualTo(AuthGateUxStateKind.GoogleAlreadyLinked));
            Assert.That(alreadyLinked.LinkStatus, Is.EqualTo("ALREADY_LINKED"));
            Assert.That(authRequired.Kind, Is.EqualTo(AuthGateUxStateKind.AuthRequired));
            Assert.That(authRequired.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(conflict.Kind, Is.EqualTo(AuthGateUxStateKind.AccountRecoveryRequired));
            Assert.That(network.Kind, Is.EqualTo(AuthGateUxStateKind.RetryableNetworkError));
        }

        [Test]
        public void FromRefreshResult_MapsRestoreRefreshReuseRejectedAndNetworkStates()
        {
            var restored = AuthGateUxState.FromRefreshResult(GameApiRefreshAuthClientResult.Refreshed(GuestResponse()));
            var reused = AuthGateUxState.FromRefreshResult(GameApiRefreshAuthClientResult.RefreshTokenReused("refresh_token_reused", "세션이 만료되었습니다."));
            var rejected = AuthGateUxState.FromRefreshResult(GameApiRefreshAuthClientResult.Rejected("auth_required", "다시 로그인해 주세요."));
            var network = AuthGateUxState.FromRefreshResult(GameApiRefreshAuthClientResult.NetworkError("timeout"));

            Assert.That(restored.Kind, Is.EqualTo(AuthGateUxStateKind.Restored));
            Assert.That(restored.HasSession, Is.EqualTo(true));
            Assert.That(reused.Kind, Is.EqualTo(AuthGateUxStateKind.ReLoginRequired));
            Assert.That(reused.RequiresGuestReauth, Is.EqualTo(true));
            Assert.That(reused.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(rejected.Kind, Is.EqualTo(AuthGateUxStateKind.AuthRequired));
            Assert.That(network.Kind, Is.EqualTo(AuthGateUxStateKind.RetryableNetworkError));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
        }

        [Test]
        public void ToJson_WritesStableDebugFieldsWithoutTokensAccountShortcutOrClientTruth()
        {
            var state = AuthGateUxState.FromGoogleLinkFlow(GameApiGoogleLinkFlowResult.FromClientResult(
                GoogleIdTokenProviderOutcome.Success,
                GameApiGoogleLinkClientResult.Conflict("google_identity_already_linked", "이미 연결된 Google 계정입니다.")));

            var json = AuthGateUxStateJsonAdapter.ToJson(state);

            Assert.That(json, Is.EqualTo(
                "{\"kind\":\"AccountRecoveryRequired\"," +
                "\"title\":\"계정 확인 필요\"," +
                "\"primaryActionLabel\":\"기존 Google 계정으로 로그인\"," +
                "\"secondaryActionLabel\":\"게스트로 돌아가기\"," +
                "\"providerOutcome\":\"Success\"," +
                "\"googleAuthOutcome\":null," +
                "\"googleLinkOutcome\":\"Conflict\"," +
                "\"refreshOutcome\":null," +
                "\"linkStatus\":null," +
                "\"errorCode\":\"google_identity_already_linked\"," +
                "\"message\":\"이미 연결된 Google 계정입니다.\"," +
                "\"canRetryNetwork\":false," +
                "\"requiresAuthRefreshOrGuestLogin\":false," +
                "\"requiresGuestReauth\":false," +
                "\"requiresAccountRecovery\":true," +
                "\"canStartGoogleSignIn\":true," +
                "\"canContinueAsGuest\":false," +
                "\"canLinkGoogle\":false," +
                "\"hasSession\":false," +
                "\"accountStatus\":null," +
                "\"isDisplayOnly\":true," +
                "\"hasRawTokenPayload\":false," +
                "\"hasAccountShortcut\":false," +
                "\"hasRewardCalculation\":false," +
                "\"hasEconomyMutation\":false," +
                "\"hasClientSideRng\":false}"));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("Authorization"), Is.EqualTo(false));
            Assert.That(json.Contains("X-Account-Id"), Is.EqualTo(false));
            Assert.That(json.Contains("X-Lastrail-Account-Id"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
        }

        [Test]
        public void Factories_RejectNullInputs()
        {
            Assert.Throws<ArgumentNullException>(() => AuthGateUxState.FromGuestSession(null));
            Assert.Throws<ArgumentNullException>(() => AuthGateUxState.FromGoogleAuthFlow(null));
            Assert.Throws<ArgumentNullException>(() => AuthGateUxState.FromGoogleLinkFlow(null));
            Assert.Throws<ArgumentNullException>(() => AuthGateUxState.FromRefreshResult(null));
            Assert.Throws<ArgumentNullException>(() => AuthGateUxStateJsonAdapter.ToJson(null));
        }

        private static GameApiAuthSessionSnapshot Session(string accountStatus)
        {
            return new GameApiAuthSessionSnapshot(
                "acct_001",
                accountStatus,
                new GameApiAccessToken("lst1.access.payload"),
                new DateTime(2026, 8, 29, 1, 0, 0, DateTimeKind.Utc),
                new GameApiRefreshToken("refresh_plain_once"),
                new DateTime(2026, 9, 29, 1, 0, 0, DateTimeKind.Utc));
        }

        private static GameApiGuestAuthResponseDto GuestResponse()
        {
            return GameApiGuestAuthResponseJsonAdapter.FromJson(TokenBundleJson());
        }

        private static GameApiGoogleAuthResponseDto GoogleAuthResponse()
        {
            return GameApiGoogleAuthResponseJsonAdapter.FromJson(TokenBundleJson());
        }

        private static GameApiGoogleLinkResponseDto GoogleLinkResponse(GameApiGoogleLinkStatus linkStatus)
        {
            var status = linkStatus == GameApiGoogleLinkStatus.Linked ? "LINKED" : "ALREADY_LINKED";
            return GameApiGoogleLinkResponseJsonAdapter.FromJson(TokenBundleJson().TrimEnd('}') + ",\"linkStatus\":\"" + status + "\"}");
        }

        private static string TokenBundleJson()
        {
            return "{\"tokenType\":\"Bearer\"," +
                "\"account\":{\"id\":\"acct_001\",\"status\":\"ACTIVE\"}," +
                "\"accessToken\":\"lst1.google.payload\"," +
                "\"accessTokenExpiresAt\":\"2026-08-29T01:00:00.123Z\"," +
                "\"refreshToken\":\"refresh_plain_once\"," +
                "\"refreshTokenExpiresAt\":\"2026-09-29T01:00:00.0000000Z\"}";
        }
    }
}
