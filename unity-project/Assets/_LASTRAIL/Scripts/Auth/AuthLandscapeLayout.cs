using UnityEngine;
using static LastRail.Auth.AuthFormVisuals;

namespace LastRail.Auth
{
    // Geometry only: authentication state, callbacks and secrets stay with their existing owners.
    public static class AuthLandscapeLayout
    {
        public static void Item(Transform parent, string name, float x, float y, float width, float height)
        {
            var target = parent.Find(name) as RectTransform;
            if (target != null) Place(target, x, y, width, height);
        }

        public static void EntryMethods(RectTransform page, float width)
        {
            float leftWidth = width * .45f - 100;
            float right = width * .595f;
            float rightWidth = width - right - 70;
            Item(page, "Brand", width*.09f, 130, leftWidth, leftWidth / 3);
            Item(page, "Welcome", width*.09f, 315, leftWidth, 60);
            Item(page, "MethodActions", right, 220, rightWidth, 820);
            var actions = page.Find("MethodActions");
            Item(actions, "ChooseMethodPrompt", 0, 0, rightWidth, 70);
            Item(actions, "Guest", 0, 100, rightWidth, 115);
            Item(actions, "ChooseIdLogin", 0, 235, rightWidth, 115);
            Item(actions, "Google", 0, 370, rightWidth, 115);
            Item(actions, "GuestStorageNotice", 0, 520, rightWidth, 116);
            float linkWidth = rightWidth / 3;
            Item(actions, "Terms", 0, 664, linkWidth, 64);
            Item(actions, "Privacy", linkWidth, 664, linkWidth, 64);
            Item(actions, "Support", linkWidth * 2, 664, linkWidth, 64);
            Item(page, "FooterNotice", 48, 650, leftWidth, 130);
            Item(page, "Retry", 48, 800, leftWidth, 120);
        }

        public static void EntryCredentials(RectTransform page, float width)
        {
            float leftWidth = width * .42f - 80, right = width * .46f, rightWidth = width - right - 56;
            Item(page, "BackToMethods", 48, 28, leftWidth, 80);
            Item(page, "Eyebrow", 48, 230, leftWidth, 50);
            Item(page, "IdHeading", 48, 300, leftWidth, 180);
            Item(page, "IdDescription", 48, 510, leftWidth, 130);
            Item(page, "IdLabel", right, 100, rightWidth, 50);
            Item(page, "LoginId", right, 160, rightWidth, 114);
            Item(page, "PasswordLabel", right, 300, rightWidth, 50);
            Item(page, "Password", right, 360, rightWidth, 114);
            Item(page, "ResetPassword", right, 500, rightWidth, 68);
            Item(page, "Login", right, 598, rightWidth, 120);
            Item(page, "RegisterPrompt", right, 750, rightWidth * .65f, 76);
            Item(page, "Register", right + rightWidth * .65f, 750, rightWidth * .35f, 76);
        }

        public static void EntryNickname(RectTransform page, float width)
        {
            float leftWidth = width * .42f - 80, right = width * .46f, rightWidth = width - right - 56;
            Item(page, "NicknameBack", 48, 28, leftWidth, 80);
            Item(page, "CommanderProfileCrop", 48, 260, leftWidth, leftWidth * 410 / 912 * 492 / 512);
            var crop = page.Find("CommanderProfileCrop");
            Item(crop, "CommanderProfile", 0, 0, leftWidth, leftWidth * 410 / 912);
            Item(page, "NicknameEyebrow", right, 70, rightWidth, 50);
            Item(page, "NicknameHeading", right, 140, rightWidth, 110);
            Item(page, "NicknameDescription", right, 270, rightWidth, 90);
            Item(page, "NicknameLabel", right, 390, rightWidth, 50);
            Item(page, "Nickname", right, 450, rightWidth, 114);
            Item(page, "PublicNameNotice", right, 592, rightWidth, 120);
            foreach (RectTransform child in page)
                if (child.name.StartsWith("NicknameNoticeDivider"))
                    Place(child, right, child.name.EndsWith("1284") ? 582 : 722, rightWidth, 2);
            Item(page, "SubmitNickname", right, 756, rightWidth, 120);
        }

        public static void EntryState(RectTransform page, float width)
        {
            float leftWidth = width * .5f - 96, right = width * .54f, rightWidth = width - right - 56;
            Item(page, "Title", 48, 200, leftWidth, 120);
            Item(page, "Status", 48, 350, leftWidth, 440);
            Item(page, "Retry", right, 300, rightWidth, 120);
            Item(page, "Cancel", right, 460, rightWidth, 120);
        }
    }
}
