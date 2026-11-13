using System.Collections.Generic;

namespace LastRail.Narrative
{
    public static class ActOneMissionNarrativeFixtures
    {
        public static IReadOnlyList<MissionNarrativeDefinition> All()
        {
            return new[]
            {
                Opn01LastDeparture(),
                Opn02FirstSignal(),
                Opn03WaterStop(),
                Opn04ScrapstarShow(),
                Opn05NightBoarders(),
                Opn06BrokenLine(),
                RaidRaiderKingEngine()
            };
        }

        public static MissionNarrativeDefinition Opn01LastDeparture()
        {
            return new MissionNarrativeDefinition(
                "opn_01_last_departure",
                "마지막 출발",
                "무너지는 플랫폼에서 마지막 사람들을 태울 시간을 벌어야 합니다.",
                new[]
                {
                    "열차는 아직 준비되지 않았지만, 역은 이미 무너지고 있습니다.",
                    "조금 더 기다리는 위험이 누군가를 살릴 수 있습니다."
                },
                new[]
                {
                    "문을 조금만 더 열어 둔다.",
                    "출발 준비를 유지한다."
                },
                "마지막 출발 시작",
                "상단 전장은 선로 밖 위협, 하단은 현재 칸 내부 상황입니다.",
                new[]
                {
                    "플랫폼 쪽 접근을 막아 주세요.",
                    "엔진 점화까지 시간이 필요합니다."
                },
                new[]
                {
                    "분대는 다섯 명이 함께 칸 단위로 이동합니다.",
                    "포격칸은 멀리 있는 위협을 먼저 끊어낼 수 있습니다."
                },
                new[]
                {
                    "더 많은 발소리가 들립니다. 아직 닫기엔 이릅니다."
                },
                new[]
                {
                    "플랫폼 끝의 생존자 접근로를 지켜 주세요."
                },
                new[]
                {
                    "마지막 공세입니다. 이 파도만 넘기면 출발할 수 있습니다."
                },
                new[]
                {
                    "문 닫습니다. 탑승자 확인, 엔진 전진."
                },
                new[]
                {
                    "역을 잃었습니다. 하지만 아직 열차는 움직일 수 있습니다."
                },
                "마지막 출발",
                "열차는 떠났고, 기다림은 몇 사람의 생존이 되었습니다.",
                "첫 클리어 보상은 서버 미리보기에서 확인합니다.",
                "반복 보상은 서버 미리보기에서 확인합니다.",
                "Last Rail의 첫 기록: 준비되지 않은 출발은 구조의 시작이 되었다.");
        }

        public static MissionNarrativeDefinition Opn02FirstSignal()
        {
            return Create(
                "opn_02_first_signal",
                "첫 신호",
                "끊긴 통신탑을 복원해 신호가 희망인지 함정인지 확인합니다.",
                "첫 신호 복원",
                "신호는 길을 열 수도 있고, 적을 부를 수도 있습니다.",
                "통신탑 전원을 되살려 주세요.",
                "잡음 속에서 사람 목소리가 들립니다.",
                "왕의 엔진이라는 말이 스쳐 지나갑니다.",
                "마지막 송신을 붙잡았습니다.",
                "신호는 살아 있는 구조 요청이 아니라, 남겨진 부탁이었습니다.");
        }

        public static MissionNarrativeDefinition Opn03WaterStop()
        {
            return Create(
                "opn_03_water_stop",
                "급수 정차",
                "급수 정거장에서 물을 확보하되 생존자들을 적으로 만들지 않아야 합니다.",
                "급수 정거장 진입",
                "물은 전리품이 아니라 생존 기반입니다.",
                "펌프 주변 접근을 막아 주세요.",
                "겁먹은 생존자들이 정거장 안에 있습니다.",
                "약탈 없이도 보급을 얻을 수 있다는 걸 보여 줘야 합니다.",
                "마지막 밸브가 열립니다.",
                "신뢰가 작동했고, 열차는 물을 얻었습니다.");
        }

        public static MissionNarrativeDefinition Opn04ScrapstarShow()
        {
            return Create(
                "opn_04_scrapstar_show",
                "스크랩스타 쇼",
                "방송과 투기장으로 사람을 구경거리로 삼는 레이더 쇼를 끊습니다.",
                "스크랩스타 진입",
                "구조는 쇼가 아니라 방해입니다.",
                "방송 중계차를 끊어 주세요.",
                "포로 구역 쪽 신호가 잡혔습니다.",
                "그들은 무관심해질 허락을 팔고 있습니다.",
                "마지막 송신기를 장악합니다.",
                "구경거리로 팔리던 사람들이 이름을 되찾았습니다.");
        }

        public static MissionNarrativeDefinition Opn05NightBoarders()
        {
            return Create(
                "opn_05_night_boarders",
                "야간 승선전",
                "밤 사이 위협이 열차 내부로 들어오고, 승객들은 각자의 역할을 배우기 시작합니다.",
                "야간 승선전 시작",
                "위협은 선로 밖이 아니라 칸 안쪽에서도 시작됩니다.",
                "문과 조명을 먼저 확인해 주세요.",
                "의무칸 쪽에서 도움 요청이 들어왔습니다.",
                "승객들이 각자 맡을 일을 찾고 있습니다.",
                "마지막 문을 잠급니다.",
                "열차 안의 사람들은 승객에서 공동체가 되기 시작했습니다.");
        }

        public static MissionNarrativeDefinition Opn06BrokenLine()
        {
            return Create(
                "opn_06_broken_line",
                "끊어진 선로",
                "무너진 선로와 교량을 복구하며 이전 승무원들이 남긴 기록을 회수합니다.",
                "끊어진 선로 복구",
                "끊어진 선로는 막힌 길이 아니라 세계의 상처입니다.",
                "임시 지지대를 보호해 주세요.",
                "기록 장치가 아직 살아 있습니다.",
                "왕의 엔진은 소문이 아니라 가까운 위협입니다.",
                "마지막 기록을 회수합니다.",
                "선로는 이어졌고, 부탁은 다음 열차로 넘어왔습니다.");
        }

        public static MissionNarrativeDefinition RaidRaiderKingEngine()
        {
            return Create(
                "raid_raider_king_engine",
                "약탈왕 기관차",
                "왕관 기관차를 추격해 포로칸과 방송 장치를 분리하고 지휘 엔진을 정지시킵니다.",
                "왕관 기관차 요격",
                "엔진을 터뜨리는 것이 아니라 멈춰 세워야 합니다.",
                "호위 차량을 먼저 떼어냅니다.",
                "방송 중계차와 포로칸 연결부가 보입니다.",
                "연료 코어를 건드리지 말고 지휘 장치를 끊어야 합니다.",
                "마지막 추진음이 낮아집니다.",
                "왕관 기관차는 멈췄고, 선로 위 명령도 함께 끊겼습니다.");
        }

        private static MissionNarrativeDefinition Create(
            string missionId,
            string displayName,
            string briefingSummary,
            string missionStartButtonCopy,
            string loadingHint,
            string openingCallout,
            string tutorialAdjacentLine,
            string escalationCallout,
            string finalWaveCallout,
            string resultSummary)
        {
            return new MissionNarrativeDefinition(
                missionId,
                displayName,
                briefingSummary,
                new[] { briefingSummary },
                new[] { "계속 진행한다." },
                missionStartButtonCopy,
                loadingHint,
                new[] { openingCallout },
                new[] { tutorialAdjacentLine },
                new[] { escalationCallout },
                new[] { "선택 목표 상태를 확인합니다." },
                new[] { finalWaveCallout },
                new[] { "작전 목표 달성." },
                new[] { "후퇴 경로를 유지합니다." },
                displayName,
                resultSummary,
                "첫 클리어 보상은 서버 미리보기에서 확인합니다.",
                "반복 보상은 서버 미리보기에서 확인합니다.",
                displayName + " 기록: " + resultSummary);
        }
    }
}
