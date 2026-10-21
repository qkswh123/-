# -

## 펭귄 빙하 등반

Unity **6000.6.6f1**, URP 기반의 3D 계단 등반 게임입니다.

### 실행

1. 이 저장소를 내려받습니다.
2. Unity Hub에서 저장소 폴더를 프로젝트로 추가합니다. `Assets`, `Packages`, `ProjectSettings`가 있는 폴더입니다.
3. `Assets/Scenes/Level1.unity`를 열고 Play를 누릅니다.

Mac에서 이어서 작업하는 방법과 현재 인수인계 내용은 [MAC_이어하기.md](MAC_이어하기.md)에 있습니다.

### 조작과 규칙

- 좌우 방향키: 다음 계단이 있는 방향으로 한 칸 이동합니다.
- 정상 착지하면 회전하는 3D 금화를 획득하고 왼쪽 아래 반투명 창의 포인트가 **10점** 증가합니다.
- 같은 코인은 중복 획득되지 않습니다. 재시작하거나 레벨을 바꾸면 해당 레벨 포인트는 0부터 시작합니다.
- 잘못된 방향 또는 빨간 장애물에 착지하면 추락하고 같은 레벨을 다시 시작합니다.
- 장애물은 하늘색(안전), 노란색(경고), 빨간색(위험) 순서로 바뀝니다.
- `R`: 재시작, `Tab`: 레벨 선택, 완료 후 `N`: 다음 레벨.

### 레벨 구성

20개 레벨에 각기 다른 캐릭터, 3D 배경, 통일된 블록 모양과 색상을 배정했습니다.
레벨 1은 펭귄과 빙하 발판이며, 레벨 20은 Low Poly 3D Animated Egg의 에그 캐릭터입니다.
목표는 레벨 1의 50칸부터 매 레벨 50칸씩 증가해 레벨 20은 1,000칸입니다.

### 주요 파일

- `Assets/Scripts/AnimalClimber.cs`: 캐릭터 이동·착지·추락
- `Assets/Scripts/IceStaircase.cs`: 계단 생성과 재사용, 코인·장애물 배치
- `Assets/Scripts/StepCoin.cs`: 3D 코인 회전과 획득 효과
- `Assets/Scripts/StepHazard.cs`: 장애물 표시와 안전·위험 주기
- `Assets/Scripts/ClimbLevel.cs`: 레벨 진행과 포인트 UI
- `Assets/GameData/ClimbGameSettings.asset`: 20개 캐릭터 배정표
- `게임_사용법.txt`: 상세 사용법
- `검증_현황.txt`: 저장 시점의 검증 상태와 남은 확인 사항

Unity가 자동 생성하는 Library, Temp, Logs, UserSettings 및 로컬 검증 이미지는 저장소에서 제외합니다.
첫 실행 시 Unity가 필요한 파일을 다시 생성합니다.
