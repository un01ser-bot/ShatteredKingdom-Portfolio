# Shattered Kingdom

스토리와 퀘스트를 따라 왕국을 탐험하며 다양한 적과 보스를 상대하는 3D 액션 RPG입니다.

- 개발 기간 : 2025.12.29 ~ 2026.02.02
- 인원 : 4인 팀 프로젝트
- Unity / C#

## 프로젝트 소개

정통 RPG의 전투와 성장에서 오는 재미를 목표로 제작한 팀 프로젝트입니다.

저는 메인 스토리와 퀘스트 기획을 담당했으며,
개발에서는 일반 몬스터와 보스의 전투 시스템을 중심으로 구현했습니다.

## 담당 영역

- 메인 스토리 및 퀘스트 기획
- 근거리 / 원거리 / 하이브리드 일반 몬스터 구현
- Wolf Boss 및 Final Boss 전투 구현
- 사냥터 및 마을 일부 콘텐츠 구현

## 주요 구현

### 일반 몬스터

`EnemyStandard`에서 탐지, 추적, 공격, 피격, 사망 등의 공통 동작을 관리하고
각 몬스터 타입이 이를 상속받아 서로 다른 전투 방식을 가지도록 구성했습니다.

근거리형은 플레이어에게 접근하여 공격하고,
원거리형은 일정 거리에서 공격하면서 체력이 낮아지면 플레이어와 거리를 벌립니다.
하이브리드형은 거리에 따라 근거리와 원거리 공격을 전환합니다.

관련 코드
- `EnemyStandard.cs` : 일반 몬스터 공통 상태 및 전투 흐름
- `Enemymelee.cs` : 근거리 공격
- `EnemyRanged.cs` : 원거리 공격 및 거리 유지
- `Enemy_Hybrid.cs` : 거리에 따른 공격 방식 전환
- `EnemyData.cs` : 몬스터 능력치 데이터
- `Enemybullet.cs`, `EnemybulletPool.cs` : 원거리 공격 투사체 관리

### Boss

각 공격 패턴이 `IBossPattern`을 구현하도록 분리하고,
Boss Base에서 현재 사용할 수 있는 패턴을 검사한 뒤 하나를 선택해 실행하도록 구성했습니다.

Wolf Boss에는 범위 공격, 도약, 낙석 등 7개의 패턴을 구현했습니다.

Final Boss는 동일한 패턴 관리 구조를 활용하면서
구슬을 통한 체력 회복과 사망 직전 Rage Totem을 이용한 발악 패턴 등
전용 기믹을 추가했습니다.

관련 코드
- `IBossPattern.cs` : 보스 패턴 공통 인터페이스
- `Wolf_Boss_Base.cs` : Wolf Boss 패턴 선택 및 실행
- `Final_Bose_Base.cs` : Final Boss 전투 및 전용 기믹 관리
- `Wolf_Pattern_*.cs`, `WolfPattern_*.cs` : Wolf Boss 개별 공격 패턴
- `FinalPattern_OrbCollect.cs` : 구슬 회복 기믹
- `FinalPattern_RageTotem.cs` : Rage Totem 기믹
- `Final_Orb.cs`, `RageTotem.cs` : 기믹 오브젝트 동작

## 폴더 구성

```text
Scripts/
├── Enemy/
└── Boss/
    ├── Core/
    ├── Wolf/
    └── Final/
```

이 저장소는 전체 프로젝트가 아닌 포트폴리오에 소개한 담당 기능의 주요 코드를 정리한 저장소입니다.
팀 프로젝트 통합 과정에서 일부 파일에는 다른 팀원의 연동 및 수정 코드가 포함되어 있을 수 있습니다.
